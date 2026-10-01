from pathlib import Path
import argparse, importlib.util, subprocess, sys, re
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument("--out",type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,"weekly-event-import",args.out);dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
spec=importlib.util.spec_from_file_location("extract",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py");extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
def source(path):return (ROOT/path).read_text(encoding="utf-8-sig")
host=source("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs");record=source("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs");legacy=source("src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs");rules=source("src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs");debt=source("src/modules/AF.Module.Economy/Debt/RewardSystemBehavior.DebtLedger.cs")
shim="using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;namespace AnimusForge { public partial class MyBehavior {"+extract.declaration(host,"internal sealed class EventRecordEntry")+extract.declaration(legacy,"internal sealed class EventMaterialReference")+extract.declaration(record,"internal sealed class EventImportPayload")+"} public partial class RewardSystemBehavior {"+extract.declaration(debt,"public class DebtExportEntry")+extract.declaration(debt,"public class DebtLineExportEntry")+"} internal static class WeeklyGenerationRules {"+"\n".join(extract.declaration(rules,x) for x in ["internal static string BuildFallbackWeeklyReportShortSummary","internal static string NormalizeWeeklyReportTagText","internal static string NeutralizeWeeklyReportScenarioName"])+"}}"
methods=[extract.declaration(record,x) for x in ["private void ApplyImportedEventData", "private void ReplaceDatabaseOpeningKnowledge", "private void RestoreDatabaseReloadWeeklyData"]]
methods.append(re.search(r"private static List<EventRecordEntry> SanitizeEventRecordEntries\([^;]+;",record).group())
for name in ["NormalizeEventRecordEntriesInPlace", "NormalizeEventMaterialReferencesInPlace", "NormalizeEventMaterialIdListInPlace"]:
 match=re.search(r"private static [^\r\n]+ " + name + r"\([^;]+;",record)
 assert match and "=>" in match.group()
 methods.append(match.group())
shim="using System.Threading;"+shim+"namespace AnimusForge { public partial class MyBehavior {"+"\n".join(methods)+(HERE/"HostLifecycle.cs.txt").read_text(encoding="utf-8")+"} public static class Logger { public static void Log(string a,string b){} }}"
# Compile the exact pre-extraction implementation as a parity oracle, not a clone sanitizer.
original=(HERE/"OriginalNormalization.cs.txt").read_text(encoding="utf-8").replace("private static", "internal static")
shim += "namespace AnimusForge { using EventRecordEntry=MyBehavior.EventRecordEntry;using EventMaterialReference=MyBehavior.EventMaterialReference; internal static class OriginalNormalizer {"+original+"internal static string NeutralizeWeeklyReportScenarioName(string x)=>WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(x);internal static string BuildFallbackWeeklyReportShortSummary(string x)=>WeeklyGenerationRules.BuildFallbackWeeklyReportShortSummary(x);internal static string NormalizeWeeklyReportTagText(string x)=>WeeklyGenerationRules.NormalizeWeeklyReportTagText(x);}}"
(out/"Shims.cs").write_text(shim,encoding="utf-8");(out/"Program.cs").write_text((HERE/"Program.cs.txt").read_text(encoding="utf-8"),encoding="utf-8")
paths=["src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs","src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs","src/modules/AF.Module.Economy/Debt/DebtImportMergePolicy.cs"]
links="".join('<Compile Include="'+str(ROOT/path)+'"/>' for path in paths)
(out/"Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Compile Include="Shims.cs"/><Compile Include="Program.cs"/></ItemGroup></Project>',encoding="utf-8")
(out/"NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding="utf-8")
for command,log in [([str(dotnet),"build",str(out/"Proof.csproj"),"--nologo","-p:RestoreConfigFile="+str(out/"NuGet.Config")],"build.log"),([str(dotnet),str(out/"bin/Debug/net8.0/Proof.dll")],"run.log")]:
 result=subprocess.run(command,cwd=out,env=env,capture_output=True,text=True,encoding="utf-8",errors="replace");(out/log).write_text(result.stdout+result.stderr,encoding="utf-8");print(result.stdout+result.stderr)
 if result.returncode:raise SystemExit(result.returncode)
print("OUTPUT",out)
