from pathlib import Path
import argparse, ast, importlib.util, json, re, subprocess, sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--out", type=Path, required=True)
args = parser.parse_args()
out = new_run_root(ROOT, "weekly-terminal", args.out)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, out)
# Reuse the production-source closure established by the existing Memory fixture.
tree = ast.parse((ROOT / "tests/modules/AF.Module.Memory/F3BusinessStateOwnerTests/run.py").read_text(encoding="utf-8-sig"))
paths = next(ast.literal_eval(node.value) for node in tree.body if isinstance(node, ast.Assign)
             and any(isinstance(target, ast.Name) and target.id == "paths" for target in node.targets))
project = ET.parse(HERE / "WeeklyMemoryMaterialOutcomeContractTests.csproj")
paths += [str((HERE / node.attrib["Include"].replace("\\", "/")).resolve().relative_to(ROOT))
          for node in project.findall(".//Compile")]
paths += ["src/modules/AF.Module.Weekly/Materials/WorldBulletinCampaignMaterialPolicy.cs",
          "src/modules/AF.Module.Memory/Records/CampaignMaterialRecordOwner.cs",
          "src/modules/AF.Module.Memory/Records/NpcActionRecordOwner.cs",
          "src/modules/AF.Module.Memory/Records/EventSourceMaterialIndex.cs",
          "src/modules/AF.Module.Weekly/Materials/WeeklyMemoryMaterialValuePolicy.cs",
          "src/modules/AF.Module.Economy/Host/TransferQuantitySpec.cs",
          "src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.WeeklyTriggers.cs",
          "src/modules/AF.Module.Weekly/Materials/WeeklyMemoryMaterialPolicy.cs",
          "src/modules/AF.Module.Weekly/Materials/WeeklyPoliticalMaterialPolicy.cs",
          "src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs",
          "AnimusForge.SiegeAftermathIntervention/SiegeActionTagCatalog.cs",
          "AnimusForge.SiegeAftermathIntervention/SiegeInterventionActionKind.cs",
          "AnimusForge.SiegeAftermathIntervention/LegacyTownTagAdapter.cs"]
# Memory owner authority fields now live in explicit partials; compile those real declarations.
paths += ["src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Identity.cs",
          "src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs",
          "src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs"]
paths = list(dict.fromkeys(path.replace("\\", "/") for path in paths))
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
host = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
day = extract.declaration(host, "internal class DialogueDay")
recovery = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs").read_text(encoding="utf-8-sig")
record_host = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs").read_text(encoding="utf-8-sig")
guards = [day, extract.declaration(record_host, "internal sealed class EventSourceMaterialEntry")]
for name in ["IsValidMemoryCommitMarker", "IsMemoryRecoveryHexDigest"]:
    match = re.search(r"internal static bool " + name + r"\([^;]+;", recovery)
    assert match and "=>" in match.group()
    guards.append(match.group())
shim = "using System;using System.Linq;namespace AnimusForge { public partial class MyBehavior {" + "\n".join(guards) + "}}\n"
shim += "namespace AnimusForge { public static class Logger { public static void Log(string area,string message){} } }\n"
shim += "namespace TaleWorlds.Library { public static class TWParallel { public static bool IsMainThread()=>true; } public static class MBMath { public static int ClampInt(int x,int min,int max)=>Math.Clamp(x,min,max); } }\n"
receipt_host = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyActionOutcomeReceipts.cs").read_text(encoding="utf-8-sig")
prepare = extract.declaration(receipt_host,"internal static WeeklyMemoryMaterialOutcomeOperationStatus PrepareWeeklyActionOutcomeForExternal")
shim = "using AnimusForge.Refactor.Runtime;using AnimusForge.Refactor.Contracts;using TaleWorlds.CampaignSystem;using TaleWorlds.Library;" + shim
shim += "namespace AnimusForge { public partial class MyBehavior {" + prepare + (HERE / "OutcomePrepareGate.cs.txt").read_text(encoding="utf-8") + "}}"
# Compile the actual record adapter against real authority fields and original non-ref aliases.
reset_methods=[]
for name in ["ResetNpcActionRecordContainers", "EnsureNpcActionRecordContainers"]:
 match=re.search(r"private void " + name + r"\([^;]+;",record_host)
 assert match and "=>" in match.group()
 reset_methods.append(match.group())
aliases=[]
for name in ["_npcMajorActions", "_npcMajorActionStorage", "_npcRecentActions", "_npcRecentActionStorage"]:
 match=re.search(r"private Dictionary<[^\r\n]+ " + name + r"[^\r\n]+",host)
 assert match
 aliases.append(match.group())
shim += "namespace AnimusForge { public partial class MyBehavior { private readonly MemoryBusinessStateOwner _memoryBusinessState=new();"+"\n".join(aliases+reset_methods)+"internal void VerifyActualRecordContainerAdapter(){EnsureNpcActionRecordContainers();ResetNpcActionRecordContainers();if(_npcMajorActions.Count!=0||_npcRecentActions.Count!=0||_npcMajorActionStorage.Count!=0||_npcRecentActionStorage.Count!=0)throw new System.Exception(\"actual alias adapter\");}}}"
shim += "namespace TaleWorlds.CampaignSystem { public class Campaign { public static Campaign Current=new(); public AnimusForge.MyBehavior Owner; public T GetCampaignBehavior<T>() where T:class => Owner as T; }}"
shim += "namespace AnimusForge.Refactor.Runtime { internal static class FeatureBridgeRuntime { internal static bool IsEnabled(string id)=>true; } internal static class FeatureBridgeIds { internal const string MemorySocialReports=\"weekly\"; }}"
(out / "Guards.cs").write_text(shim, encoding="utf-8")
(out / "Program.cs").write_text("using AnimusForge;\n" + (HERE / "Program.cs").read_text(encoding="utf-8-sig")
                               + "\n" + (HERE / "TerminalCases.cs.txt").read_text(encoding="utf-8-sig"), encoding="utf-8")
links = "".join('<Compile Include="' + str(ROOT / path) + '"/>' for path in paths)
newtonsoft = ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>' + links + '<Compile Include="Program.cs"/><Compile Include="Guards.cs"/><Reference Include="Newtonsoft.Json"><HintPath>' + str(newtonsoft) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
for command, log in [([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], "build.log"),
                     ([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll")], "run.log")]:
    result = subprocess.run(command, cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (out / log).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
print("OUTPUT", out)
