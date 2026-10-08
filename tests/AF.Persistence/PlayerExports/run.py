"""Build and run the PlayerExportsStore / NpcDataFileName contract against the production file (needs Newtonsoft: AF_NEWTONSOFT).

SDK resolution: AF_DOTNET env var, then repository local/dotnet/8.0.425, then dotnet on PATH.
Exit code is the harness exit code; mutation switches prove the assertions are live.
"""
from __future__ import annotations

import argparse
import ast
import re
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["name-mismatch-passes","latest-oldest"], help="apply a source mutation that must fail")
parser.add_argument("--run-root", type=Path, help="new output directory; refuses to overwrite an existing path")
parser.add_argument("--api", choices=["1.3", "1.4"], default="1.3")
parser.add_argument("--synthetic-root", type=Path, help="new approved H7 A synthetic child, never a real player directory")
parser.add_argument("--terminal-red", action="store_true", help="approved terminal contract against original baseline; expected failure")
args = parser.parse_args()


def resolve_dotnet() -> Path:
    candidates = [os.environ.get("AF_DOTNET", ""), str(ROOT / "local/dotnet/8.0.425/dotnet.exe"), shutil.which("dotnet") or ""]
    for candidate in candidates:
        if candidate and Path(candidate).exists():
            return Path(candidate)
    raise SystemExit("NOT-RUN: no dotnet SDK found (set AF_DOTNET)")


dotnet = resolve_dotnet()
output = new_run_root(ROOT, "persistence-player-exports", args.run_root)
for name in ("Program.cs", "PlayerExportsTests.csproj"):
    shutil.copy(HERE / name, output / name)
project = (output / "PlayerExportsTests.csproj").read_text(encoding="utf-8").replace("../../../", (str(ROOT) + "/").replace("\\", "/"))
newtonsoft = os.environ.get("AF_NEWTONSOFT") or str(ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll")
project = project.replace("@@NEWTONSOFT@@", newtonsoft.replace("\\", "/"))
if args.mutate:
    src_dir = output / "mutated"; src_dir.mkdir()
    if args.mutate == "name-mismatch-passes":
        rel = "src/AF.Persistence/NpcDataFileName.cs"
        text = (ROOT / rel).read_text(encoding="utf-8")
        needle = "return !string.IsNullOrWhiteSpace(text2) && string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);"
    else:
        rel = "src/AF.Persistence/PlayerExportsStore.cs"
        text = (ROOT / rel).read_text(encoding="utf-8")
        needle = "orderby d.LastWriteTimeUtc descending"
    assert text.count(needle) == 1, needle
    replacement = "return true;" if args.mutate == "name-mismatch-passes" else "orderby d.LastWriteTimeUtc ascending"
    mutated = src_dir / Path(rel).name
    mutated.write_text(text.replace(needle, replacement), encoding="utf-8")
    project = project.replace(str(ROOT).replace("\\", "/") + "/" + rel, str(mutated).replace("\\", "/"))
# H7 links actual format/state/import owners; only engine registry/UI leaves are controlled.
# Live identity leaves and legacy nested profile DTO are source-extracted from current production.
extract_source = ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"
node = next(n for n in ast.parse(extract_source.read_text(encoding="utf-8-sig")).body
            if isinstance(n, ast.FunctionDef) and n.name == "declaration")
scope = {"re": re}
exec(compile(ast.Module(body=[node], type_ignores=[]), str(extract_source), "exec"), scope)
extract = scope["declaration"]
identity = (ROOT / "src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs").read_text(encoding="utf-8-sig")
profile = extract((ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig"), "internal class NpcPersonaProfile")
leaf = "using System; using System.Collections.Generic; using System.Linq; using TaleWorlds.CampaignSystem; namespace AnimusForge { internal static partial class MemoryEntityIdentityBannerlordAdapter {" + "\n".join(extract(identity, "internal static Hero " + name + "(") for name in ["ResolveHeroByIdForNpcData", "ResolveUniqueHeroByNpcFileDisplayName"]) + "} public partial class MyBehavior {" + profile + extract((ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig"), "internal class DialogueDay") + extract((ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig"), "class UnnamedPersonaSingleJson") .replace("class UnnamedPersonaSingleJson", "internal class UnnamedPersonaSingleJson",1) + "}}"
(output / "IdentityLeaves.cs").write_text(leaf, encoding="utf-8")
# Historical original baseline is frozen evidence, not current accepting production.
import hashlib
fixture_source=(HERE / "Program.cs").read_text(encoding="utf-8")
baseline = fixture_source.split("/* AF_ONBOARDING_BASELINE_SOURCE_BEGIN\n",1)[1].split("\nAF_ONBOARDING_BASELINE_SOURCE_END */",1)[0].replace("\n","\r\n").encode("utf-8")
if hashlib.sha256(baseline).hexdigest() != "d22eca4bc76fc77881c227bd5aa23410e6181faa1d9e6a9d01bed453863b982b":
    raise SystemExit("original baseline evidence changed; refusing to refresh its hash")
(output / "OnboardingReflection.cs").write_bytes(baseline)

# Actual Voice JSON parser and same pool mutation; only loading/cache/feedback engine leaves controlled.
voice_source=(ROOT / "src/modules/AF.Module.Llm/Tts/VoiceMapper.cs").read_text(encoding="utf-8-sig")
voice_fields="private static Dictionary<string,List<string>> _voicePools; private static string _fallbackVoice; public static readonly string[] AllGroupKeys = new string[6] { \"male_young\", \"male_middle\", \"male_old\", \"female_young\", \"female_middle\", \"female_old\" }; private static void EnsureLoaded(){} private static void ClearSceneCache(){}"
(output / "VoiceAccepting.cs").write_text("using System; using System.IO; using System.Text; using System.Collections.Generic; using Newtonsoft.Json.Linq; namespace AnimusForge; internal static partial class VoiceMapper {"+voice_fields+"\n"+"\n".join(extract(voice_source,m) for m in ["public static bool ImportMappingFromFile(","public static bool ImportMappingJson(","private static Dictionary<string, List<string>> ParseVoiceMappingJson(","public static int GetTotalVoiceCount(","public static string GetFallbackVoice("])+"}",encoding="utf-8")
project=project.replace("</ItemGroup>",'<Compile Include="VoiceAccepting.cs" /></ItemGroup>')

# Actual shared unnamed partial parser/runtime application, no-op save is not disk durability.
unnamed_source=(ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs").read_text(encoding="utf-8-sig")
unnamed_fields="private static readonly object _unnamedProfilesLock=new object(); private static Dictionary<string,UnnamedNpcPersonaProfile> _unnamedProfiles=new(); private static void LoadUnnamedProfilesIfNeeded(){} "
(output / "UnnamedAccepting.cs").write_text("using System; using System.IO; using System.Collections.Generic; using Newtonsoft.Json.Linq; namespace AnimusForge; internal static partial class ShoutUtils {"+unnamed_fields+extract(unnamed_source,"private class UnnamedNpcPersonaProfile")+extract(unnamed_source,"internal static bool TryImportUnnamedPersonaFromDir(")+extract(unnamed_source,"private static void SaveUnnamedProfilesUnsafe(")+"}",encoding="utf-8")
project=project.replace("</ItemGroup>",'<Compile Include="UnnamedAccepting.cs" /></ItemGroup>')

# Actual rule validation/source and bulk-to-per-rule partial policy; existing rule store endpoints controlled.
validation=(ROOT / "src/modules/AF.Module.Knowledge/Import/KnowledgeImportValidationOwner.cs").read_text(encoding="utf-8-sig")
support=(ROOT / "src/modules/AF.Module.Knowledge/Import/KnowledgeImportSupport.cs").read_text(encoding="utf-8-sig")
commit=(ROOT / "src/modules/AF.Module.Knowledge/Import/KnowledgeRuleImportOwner.cs").read_text(encoding="utf-8-sig")
(output / "KnowledgeAccepting.cs").write_text("using System; using System.IO; using System.Text; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json; namespace AnimusForge; internal static class KnowledgeImportValidationOwner {"+"\n".join(extract(validation,m) for m in ["internal static string NormalizeKeywordForCompare(","internal static bool ValidateKnowledgeKeywordsForImport("])+"} internal static partial class KnowledgeImportSupport {"+"\n".join(extract(support,m) for m in ["internal static IEnumerable<string> GetKnowledgeKeywordsForCompare(","internal static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir("])+"} internal static class KnowledgeRuleImportOwner {"+extract(commit,"internal static bool TryImportKnowledgeFileWithFallback(")+"internal static string BuildKnowledgeRuleImportFailureMessage(KnowledgeLibraryBehavior kb,KnowledgeLibraryBehavior.LoreRule rule,bool replace)=>\"controlled rule-store rejection\";} internal static class LoreCandidateRetriever {internal static string NormalizeKeywordForCompare(string key)=>KnowledgeImportValidationOwner.NormalizeKeywordForCompare(key);} internal static class AIConfigHandler{internal static void ReloadConfig(){throw new Exception(\"Not authorized to reload live module config\");}}",encoding="utf-8")
project=project.replace("</ItemGroup>",'<Compile Include="KnowledgeAccepting.cs" /></ItemGroup>')

# Actual complete Rebellion UI controller, current legacy DTOs and two actual pure naming predicates.
main_source=(ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
runtime_source=(ROOT / "src/AF.GameAdapter.Bannerlord/Kingdom/KingdomRebellionRuntimeController.cs").read_text(encoding="utf-8-sig")
(output / "RebellionUiDto.cs").write_text("using System; using System.Collections.Generic; using static AnimusForge.MyBehavior; namespace AnimusForge; public partial class MyBehavior {"+"\n".join(extract(main_source,"internal sealed class "+n) for n in ["RebelKingdomNamingResult","PendingDevForcedKingdomRebellionContext","PendingAutomaticKingdomRebellionContext"])+"} internal sealed partial class KingdomRebellionRuntimeController {"+"\n".join(extract(runtime_source,"internal static "+t+" "+n+"(") for t,n in [("bool","IsRebelKingdomNamingSuccess"),("RebelKingdomNamingResult","BuildFailedRebelKingdomNamingResult")])+"}",encoding="utf-8")
project=project.replace("</ItemGroup>",'<Compile Include="RebellionUiDto.cs" /><Compile Include="'+str(ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/KingdomRebellionEditorController.cs").replace("\\","/")+'" /></ItemGroup>')

# Reuse the existing J17 format fixture leaves, not a second production memory implementation.
fixture = (ROOT / "tests/modules/AF.Module.Memory/J17ImportExportTests/OwnerChecks.cs").read_text(encoding="utf-8-sig")
fixture_types = fixture[fixture.index("internal sealed class DailyMemoryDraft"):fixture.index("internal static class Program")]
state_shape = extract(fixture, "internal sealed class MemoryBusinessStateOwner")
(output / "MemoryFixture.cs").write_text("using System; using System.Collections.Generic; using System.Linq; namespace AnimusForge;\n" + fixture_types + state_shape, encoding="utf-8")

# Existing runner extends to real whole package export coordination with exact source-extracted
# four format export bodies. Their external domain queries are controlled; file algorithms are not stubs.
export_types=[]
for name,methods,fields in [
    ("DebtImportExportAdapter",["internal void ExportDebtToDirectory(","internal Dictionary<string,RewardSystemBehavior.DebtExportEntry> PrepareDebtDirectory(","internal void ApplyPreparedDebt("],""),
    ("VoicePersonaImportExportAdapter",["internal void ExportVoiceMappingToDirectory(","internal void PrepareVoiceMapping(","internal void ApplyPreparedVoice(","internal void ImportVoiceMappingDataScoped("],"internal Action<string,string,Action,Action,Action> _showDuplicate; internal Action<string> _persistVoiceJson; internal VoicePersonaImportExportAdapter(Action<string,string,Action,Action,Action> inquiry=null,Action<string> persist=null){_showDuplicate=inquiry;_persistVoiceJson=persist;}"),
    ("KnowledgeImportExportAdapter",["internal bool TryExportKnowledgeToDir(","internal void PrepareKnowledgeCounts(","internal void ImportKnowledgeDataScoped(","internal static bool ValidateKnowledgeKeywordsForImport(","internal bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting, out string detailMessage)"],"internal Action<string,string,Action,Action,Action> _showDuplicate; internal KnowledgeImportExportAdapter(Action<string,string,Action,Action,Action> inquiry=null){_showDuplicate=inquiry;}"),
    ("WeeklyEventImportExportAdapter",["internal void ExportEventDataToDir(","internal Dictionary<string, string> BuildEventKingdomSummaryExportMap(","internal void PrepareEventPayload(","internal void ImportEventDataScoped(","internal void ApplyImportedEventData("],"internal Action<string,string,Action,Action,Action> _showDuplicate; internal Action _openEventMenu=()=>{}; internal WeeklyEventImportExportAdapter(Action<string,string,Action,Action,Action> inquiry=null){_showDuplicate=inquiry;} internal Action _markOpening=()=>{},_notifyTimeline=()=>{}; internal Func<string> _fingerprint=()=>\"\"; internal WeeklyEventRecordStateOwner _weekly = new(); internal Func<List<MyBehavior.EventRecordEntry>,List<MyBehavior.EventRecordEntry>> _sanitize = x => x; internal bool TryLoadEventDataFromImportDir(string dir,out MyBehavior.EventImportPayload p,out string error)=>WeeklyEventDataImportOwner.TryLoadEventDataFromImportDir(dir,out p,out error,path=>PlayerExportsStore.ReadJson<MyBehavior.EventWorldOpeningSummaryJson>(path)?.Summary,PlayerExportsStore.ReadJson<Dictionary<string,string>>,PlayerExportsStore.ReadJson<List<MyBehavior.EventRecordEntry>>,_sanitize);")]:
    text=(ROOT / ("src/AF.GameAdapter.Bannerlord/ImportExport/"+name+".cs")).read_text(encoding="utf-8-sig")
    export_types.append("internal sealed class "+name+" {"+fields+"\n"+"\n".join(extract(text,m) for m in methods)+"}")
(output / "PackageExportFormats.cs").write_text("using System; using System.IO; using System.Text; using System.Threading; using System.Collections.Generic; using System.Linq; using Newtonsoft.Json; using TaleWorlds.Library; using TaleWorlds.CampaignSystem; using static AnimusForge.MyBehavior; namespace AnimusForge;\n"+"\n".join(export_types),encoding="utf-8")
weekly_source=(ROOT / "src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs").read_text(encoding="utf-8-sig")
payload=extract((ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs").read_text(encoding="utf-8-sig"),"internal sealed class EventImportPayload")
(output / "PackageWeeklyInput.cs").write_text("using System; using System.IO; using System.Linq; using System.Collections.Generic; using static AnimusForge.MyBehavior; namespace AnimusForge; public partial class MyBehavior {"+payload+"} internal static class WeeklyEventDataImportOwner {"+"\n".join(extract(weekly_source,m) for m in ["internal static bool TryLoadEventDataFromImportDir(","internal static void ApplyOpening(","internal static void ApplyRecords("])+"}",encoding="utf-8")
project=project.replace("</ItemGroup>",'<Compile Include="PackageWeeklyInput.cs" />'+''.join('<Compile Include="'+str(ROOT / f).replace("\\","/")+'" />' for f in ["src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperImportController.cs","src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperPackageImportController.cs","src/modules/AF.Module.Economy/Debt/DebtImportMergePolicy.cs"])+"</ItemGroup>")
project=project.replace("</ItemGroup>",'<Compile Include="PackageExportFormats.cs" /><Compile Include="'+str(ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperPackageExportController.cs").replace("\\","/")+'" /></ItemGroup>')

links = ["src/AF.GameAdapter.Bannerlord/UI/Onboarding/OnboardingDatabaseImportController.cs",
         "src/AF.GameAdapter.Bannerlord/ImportExport/NpcDataIdentityFileAdapter.cs",
         "src/AF.GameAdapter.Bannerlord/ImportExport/PersonaProfileImportExportAdapter.cs",
         "src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs",
         "src/modules/AF.Module.Persona/Import/PersonaImportOwner.cs",
         "src/modules/AF.Module.Persona/Import/UnnamedPersonaImportValidationOwner.cs",
         "src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs",
         "src/AF.GameAdapter.Bannerlord/ImportExport/MemoryHistoryImportExportAdapter.cs"]
project = project.replace("</ItemGroup>", ''.join('<Compile Include="' + str(ROOT / f).replace("\\", "/") + '" />' for f in links) + '<Compile Include="IdentityLeaves.cs" /><Compile Include="MemoryFixture.cs" /><Compile Include="OnboardingReflection.cs" /></ItemGroup>')
if args.api == "1.4":
    project = project.replace("</PropertyGroup>", "<DefineConstants>BANNERLORD_1_4_OR_GREATER</DefineConstants></PropertyGroup>")
(output / "PlayerExportsTests.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
temp = output / "temp"
temp.mkdir()
env = minimal_test_environment(dotnet, output)
if args.synthetic_root is not None:
    approved = Path("E:/af-j17-synthetic-20261004/j17-16750c0b-20261004/a").resolve()
    data_root = args.synthetic_root.resolve()
    if data_root.parent != approved or data_root.exists():
        raise SystemExit("synthetic root must be a new direct child of the approved H7 A root")
    # No cleanup: the unique synthetic data is retained as evidence.
    data_root.mkdir(parents=True, exist_ok=False)
    env["AF_H7_PERSONA_FORMATS"] = "1"
else:
    data_root = ROOT.parent / "AF-J15-PathOnly-PlayerExports"
    if data_root.exists():
        raise SystemExit("synthetic path-only data root already exists; refusing to read it")
if args.terminal_red: env["AF_ONBOARDING_TDD_RED"]="1"
env.update(AF_PLAYER_EXPORTS_TEST_TEMP=str(temp), ANIMUSFORGE_DATA_ROOT=str(data_root))
build = subprocess.run([str(dotnet), "build", str(output / "PlayerExportsTests.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")], cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode != 0:
    print(build.stdout[-3000:])
    raise SystemExit("build failed: " + str(build.returncode))
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/PlayerExportsTests.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
print((run.stdout + run.stderr).strip()[-2000:])
sys.exit(run.returncode)
