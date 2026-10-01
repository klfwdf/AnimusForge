from pathlib import Path
import argparse,importlib.util,subprocess,sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument("--out",type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,"weekly-runtime",args.out);dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
spec=importlib.util.spec_from_file_location("extract",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py");extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
host=(ROOT/"src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
declarations=[]
for name in ["WeeklyReportPromptProfile","WeeklyReportBatchExecutionResult","WeeklyReportGenerationResult","PendingWeeklyPromptPreparationContext","PendingWeeklyReportCommitContext","PendingWeeklyReportBlockCommit","WeeklyReportRetryContext"]:
 declarations.append(extract.declaration(host,"internal sealed class "+name))
declarations.append(extract.declaration(host,"internal enum WeeklyPromptPreparationResult"))
declarations.append(extract.declaration(host,"internal sealed class ApiCallResult"))
declarations.append(extract.declaration(host,"internal sealed class EventRecordEntry"))
declarations.append(extract.declaration(host,"internal sealed class DevWeeklyReportBatchPreviewEntry"))
shim="using System;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;namespace AnimusForge { public partial class MyBehavior {"+"\n".join(declarations)+(HERE/"CompositionLeafShims.cs.txt").read_text(encoding="utf-8")+"}}"
shim+="namespace AnimusForge { internal static class Logger {internal static void Log(string a,string b){} } internal static class PerfProbe {internal static IDisposable Scope(string s)=>new ScopeToken();private sealed class ScopeToken:IDisposable { public void Dispose(){} }} }"
failure=(ROOT/"src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs").read_text(encoding="utf-8-sig")
shim += "namespace AnimusForge { internal static class LlmRetryPrompt {"+extract.declaration(failure,"public static string BuildFailureDetail")+extract.declaration(failure,"private static string NormalizeFullText")+"}}"
shim="using System.Text;"+shim
shim += "namespace TaleWorlds.Library {internal static class TWParallel {internal static bool IsMainThread()=>true;} internal sealed class InformationMessage {internal InformationMessage(string text){} } internal static class InformationManager {internal static void DisplayMessage(InformationMessage text){} }}"
shim += "namespace AnimusForge { internal static class DuelSettings {internal sealed class Settings {internal int WeeklyReportLengthPreset=2,WeeklyReportRequestsPerMinute=5;internal string WeeklyReportWritingRequirements=\"\";} internal static Settings Current=new();internal static Settings GetSettings()=>Current;}}"

(out/"Shims.cs").write_text(shim,encoding="utf-8")
(out/"OriginalPromptOracle.cs").write_text((HERE/"OriginalPromptOracle.cs.txt").read_text(encoding="utf-8"),encoding="utf-8")
(out/"Program.cs").write_text((HERE/"Program.cs.txt").read_text(encoding="utf-8"),encoding="utf-8")
paths=["src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyRuntime.cs","src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitQueueOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitTargetOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportBlockMaterialCursor.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportWaveCoordinator.cs","src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs","src/modules/AF.Module.Weekly/Generation/WeeklyGenerationModels.cs","src/modules/AF.Module.Weekly/Materials/WeeklyMaterialStageCursor.cs","src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs","src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.SystemPrompt.cs","src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.MaterialCopies.cs","src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs"]
links="".join('<Compile Include="'+str(ROOT/path)+'"/>' for path in paths)
jsondll=ROOT/"local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
(out/"Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Compile Include="Program.cs"/><Compile Include="Shims.cs"/><Compile Include="OriginalPromptOracle.cs"/><Reference Include="Newtonsoft.Json"><HintPath>'+str(jsondll)+'</HintPath></Reference></ItemGroup></Project>',encoding="utf-8")
(out/"NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding="utf-8")
for command,log in [([str(dotnet),"build",str(out/"Proof.csproj"),"--nologo","-p:RestoreConfigFile="+str(out/"NuGet.Config")],"build.log"),([str(dotnet),str(out/"bin/Debug/net8.0/Proof.dll")],"run.log")]:
 result=subprocess.run(command,cwd=out,env=env,capture_output=True,text=True,encoding="utf-8",errors="replace");(out/log).write_text(result.stdout+result.stderr,encoding="utf-8");print(result.stdout+result.stderr)
 if result.returncode:raise SystemExit(result.returncode)
print("OUTPUT",out)
