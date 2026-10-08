"""Prepare the original package fixture with current constructor and actual format methods.

No player data, cleanup, registry changes or substituted package acceptance algorithm.
"""
from pathlib import Path
import argparse
import ast
import hashlib
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--out", type=Path, help="new isolated output; default creates a fresh workspace artifact child")
parser.add_argument("--prepare-only", action="store_true")
args = parser.parse_args()
out = new_run_root(ROOT, "package-import", args.out)
dotnet = resolve_dotnet(ROOT)
sources = {}


def read(rel):
    path = ROOT / rel
    sources[rel] = hashlib.sha256(path.read_bytes()).hexdigest()
    return path.read_text(encoding="utf-8-sig")


extract_path = "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"
node = next(n for n in ast.parse(read(extract_path)).body
            if isinstance(n, ast.FunctionDef) and n.name == "declaration")
scope = {"re": re}
exec(compile(ast.Module(body=[node], type_ignores=[]), extract_path, "exec"), scope)
declaration = scope["declaration"]
prefix = "tests/modules/AF.Module.Persona/PackageImportLifecycleTests/"
read(prefix + "run.py")
read(prefix + "PackageImportLifecycleTests.csproj")
program = read(prefix + "Program.cs")
original_checks = re.findall(r"Check\((.*?)\);", program, re.S)
old_ctor = "new DeveloperPackageImportController(port,execution)"
assert program.count(old_ctor) == 1
program = program.replace(old_ctor, "FixtureFormats.Create(port,execution)")
old_root = 'Path.GetFullPath("artifacts/af2-host-terminal-closeout/line-d/packageimport/fixtures/"+Guid.NewGuid().ToString("N"))'
assert program.count(old_root) == 1
program = program.replace(old_root, json.dumps(str(out / "fixtures")))
assert re.findall(r"Check\((.*?)\);", program, re.S) == original_checks
(out / "Program.cs").write_text(program, encoding="utf-8")
(out / "Stubs.cs").write_text(read(prefix + "Stubs.cs"), encoding="utf-8")

# These are read-only domain/game capabilities of the original fixture, not new stores.
# Every directory enumeration, tolerant decode and prepared-data merge below is production.
fields = {
    "PersonaProfileImportExportAdapter": "private FixtureProfileQueries _profiles;internal PersonaProfileImportExportAdapter(DeveloperPackageImportPort p){_profiles=new(p);} private string ReadUnnamedPersonaImportKey(string f)=>FixtureFormats.Port.ReadUnnamedPersonaKey(f);",
    "MemoryHistoryImportExportAdapter": "private FixtureMemoryQueries _memory;internal MemoryHistoryImportExportAdapter(DeveloperPackageImportPort p){_memory=new(p);} private bool HasCompressedMemoryDataForHero(string id)=>FixtureFormats.Port.HasCompressedMemoryDataForHero(id);private bool ApplyCompressedMemoryExportBundle(string id,CompressedMemoryExportBundle b,bool overwriteExisting)=>FixtureFormats.Port.ApplyCompressedMemoryExportBundle(id,b,overwriteExisting);",
    "DebtImportExportAdapter": "private Action<string,string,Action,Action,Action> _showDuplicate;internal DebtImportExportAdapter(Action<string,string,Action,Action,Action> show){_showDuplicate=show;}",
    "KnowledgeImportExportAdapter": "",
    "VoicePersonaImportExportAdapter": "",
    "WeeklyEventImportExportAdapter": "private FixtureWeeklyQueries _weekly;internal WeeklyEventImportExportAdapter(DeveloperPackageImportPort p){_weekly=new(p);}private bool TryLoadEventDataFromImportDir(string d,out EventImportPayload p,out string error)=>FixtureFormats.Port.TryLoadEventDataFromImportDir(d,out p,out error);",
}
methods = {
    "PersonaProfileImportExportAdapter": ["internal Dictionary<string,NpcPersonaProfile> PreparePersonaDirectory(", "internal void PrepareUnnamedPersonaCounts(", "internal void ApplyUnnamedPersonaDirectory("],
    "MemoryHistoryImportExportAdapter": ["internal Dictionary<string,List<DialogueDay>> PrepareRawHistoryDirectory(", "internal Dictionary<string,CompressedMemoryExportBundle> PrepareCompressedDirectory(", "internal void ApplyCompressedDirectory("],
    "DebtImportExportAdapter": ["internal Dictionary<string,RewardSystemBehavior.DebtExportEntry> PrepareDebtDirectory(", "internal void ApplyPreparedDebt(", "internal void ImportSingleNpcDebtData("],
    "KnowledgeImportExportAdapter": ["internal void PrepareKnowledgeCounts("],
    "VoicePersonaImportExportAdapter": ["internal void PrepareVoiceMapping(", "internal void ApplyPreparedVoice("],
    "WeeklyEventImportExportAdapter": ["internal void PrepareEventPayload("],
}
formats = []
for name, signatures in methods.items():
    text = read("src/AF.GameAdapter.Bannerlord/ImportExport/" + name + ".cs")
    formats.append("internal sealed class " + name + " {" + fields[name]
                   + "\n".join(declaration(text, signature) for signature in signatures) + "}")
header = "using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Text;using Newtonsoft.Json;using TaleWorlds.CampaignSystem;using TaleWorlds.Library;using static AnimusForge.MyBehavior;namespace AnimusForge;"
(out / "ActualFormats.cs").write_text(header + "\n".join(formats), encoding="utf-8")

leaves = r'''
internal static class FixtureFormats {
 internal static DeveloperPackageImportPort Port;
 internal static DeveloperPackageImportController Create(DeveloperPackageImportPort p,DeveloperImportController execution){
  Port=p;return new(p,execution,new(p),new(p),new(p.ShowDuplicateImportInquiry.Invoke),new(),new(),new(p));
 }
}
internal sealed class FixtureLookup {private Func<string,bool> _contains;internal FixtureLookup(Func<string,bool> c){_contains=c;}internal bool ContainsKey(string id)=>_contains(id);}
internal sealed class FixtureProfileQueries {internal FixtureLookup Profiles;internal FixtureProfileQueries(DeveloperPackageImportPort p){Profiles=new(p.ContainsPersona);}}
internal sealed class FixtureMemoryQueries {internal FixtureLookup History;internal FixtureMemoryQueries(DeveloperPackageImportPort p){History=new(p.ContainsHistory);}}
internal sealed class FixtureWeeklyQueries {private DeveloperPackageImportPort _p;internal FixtureWeeklyQueries(DeveloperPackageImportPort p){_p=p;}internal string WorldOpening=>_p.WorldOpeningSummary();internal FixtureLookup KingdomOpenings=>new(_p.ContainsKingdomSummary);internal List<EventRecordEntry> Records=>_p.EventRecords();}
internal static class NpcDataIdentityFileAdapter {
 internal sealed class LookupScope {
  internal bool TryResolveNpcDataFileHeroIdForImport(string f,out string id,out string warning)=>FixtureFormats.Port.TryResolveNpcDataFileHeroIdForImport(f,out id,out warning);
  internal void StampNpcPersonaProfile(string id,NpcPersonaProfile p)=>FixtureFormats.Port.StampNpcPersonaProfile(id,p);
 }
 // The original single-debt fixture deliberately controls this engine identity lookup.
 internal static string FindNpcJsonByHeroId(string dir,string id)=>null;
}
internal static class MemoryRecordRules {
 internal static string NormalizeMemoryHeroId(string id)=>FixtureFormats.Port.NormalizeMemoryHeroId(id);
}
public partial class MyBehavior {
 internal DeveloperImportController FixtureConfirmation;internal Action[] FixtureChoices;
 internal void RunSingleDebt(string folder,string id)=>new DebtImportExportAdapter((t,s,yes,no,cancel)=>FixtureChoices=FixtureConfirmation.BeginConfirmation(yes,no,cancel)).ImportSingleNpcDebtData(folder,id);
}
'''
(out / "FixtureCapabilities.cs").write_text(header + leaves, encoding="utf-8")
# Full coordination/controller and the actual debt mutation policy, not a surrogate.
links = ["src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperPackageImportController.cs",
         "src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperImportController.cs",
         "src/modules/AF.Module.Economy/Debt/DebtImportMergePolicy.cs",
         "src/modules/AF.Module.Memory/ImportExport/CompressedMemoryExportBundleReader.cs"]
for rel in links:
    read(rel)
    (out / Path(rel).name).write_bytes((ROOT / rel).read_bytes())
read(prefix + "HostSingleDebtAdapter.cs")  # old fixture identity retained in the manifest
manifest = {"physicalInputs": sources, "assertionsPreserved": len(original_checks),
            "actualWhole": links, "actualExtractedFormatDeclarations": methods,
            "controlledLeaves": "original game/domain capability port and read-only queries; no duplicated state; no format/coordination acceptance stub",
            "generated": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in out.glob("*.cs")}}
(out / "source-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
if args.prepare_only:
    print("PREPARED", out)
    raise SystemExit(0)
newtonsoft = dotnet.parent / "sdk/8.0.425/Containers/tasks/net8.0/Newtonsoft.Json.dll"
assert newtonsoft.is_file(), "Newtonsoft dependency missing"
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(newtonsoft) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(dotnet, out)
for cmd, log in [([str(dotnet), "build", str(out / "Proof.csproj"), "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], "build.log"),
                 ([str(dotnet), str(out / "bin/Debug/net8.0/Proof.dll")], "run.log")]:
    result = subprocess.run(cmd, cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
    (out / log).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
