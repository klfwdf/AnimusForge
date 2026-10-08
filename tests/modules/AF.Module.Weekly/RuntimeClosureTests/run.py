from pathlib import Path
import argparse,importlib.util,subprocess,sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument("--out",type=Path,required=True);args=parser.parse_args()
out=new_run_root(ROOT,"weekly-runtime",args.out);dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
# Resolve the installed net8 SDK and JSON assembly in this checkout's selected runtime.
import re,json
sdk_lines=subprocess.check_output([str(dotnet),'--list-sdks'],text=True).splitlines()
sdk_options=[(m.group(1),Path(m.group(2))) for line in sdk_lines if (m:=re.fullmatch(r'(8\.0\.\d+) \[(.+)\]',line))]
if not sdk_options: raise SystemExit('BLOCKED_ENV: the selected dotnet host has no net8 SDK')
sdk_version,sdk_root=max(sdk_options,key=lambda x:tuple(map(int,x[0].split('.'))))
(out/'global.json').write_text(json.dumps({'sdk':{'version':sdk_version,'rollForward':'latestPatch'}}),encoding='utf-8')
spec=importlib.util.spec_from_file_location("extract",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py");extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
host=(ROOT/"src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
declarations=[]
for name in ["WeeklyReportPromptProfile","WeeklyReportBatchExecutionResult","WeeklyReportGenerationResult","PendingWeeklyPromptPreparationContext","PendingWeeklyReportCommitContext","PendingWeeklyReportBlockCommit","WeeklyReportRetryContext"]:
 declarations.append(extract.declaration(host,"internal sealed class "+name))
declarations.append(extract.declaration(host,"internal enum WeeklyPromptPreparationResult"))
for name in ["PendingWeeklyWaveLaunchContext","PendingWeeklyBatchApiAttemptContext"]:declarations.append(extract.declaration(host,"internal sealed class "+name))
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

# Original fixture covers detached burst/prepare/commit and exact prompt composition, not
# later-added automatic world capture and full-by-event entry. Actual wave/API pumps are included. Extract complete
# current declarations for that original responsibility; no tested algorithm is substituted.
import re,hashlib,json
runtime_path="src/modules/AF.Module.Weekly/Generation/WeeklyReportRuntimeOwner.cs"
actual_runtime=(ROOT/runtime_path).read_text(encoding="utf-8-sig")
port=extract.declaration(actual_runtime,"internal sealed class WeeklyReportRuntimePort")
port_lines=port.splitlines();port_lines=[line for line in port_lines if not any(re.search(r'\b'+marker+r'\b',line) for marker in ['FullSystem','FullUser','FullPreview','FullFailure','FullProgress','HideInquiry','FullCompletions','FullResponseParser','ParseFullResponse','MemoryState','MaterialState','AutoSchedule','RebellionFlow','EpicWeekLabel','QueueDeferredWeekly','WeeklyRecords','RecordCapture','ProximityOrder','AggregateMaterials','BatchSystem','BatchUser','BatchPreview'])]
port='\n'.join(port_lines)
first=actual_runtime.index(' private readonly WeeklyReportRuntimePort _port;')
last=actual_runtime.index('internal void ProcessPendingWeeklyReportAggregationBudget(')
core=actual_runtime[first:last]
core='\n'.join(line for line in core.splitlines() if not any(x in line for x in ['private PendingAutoWeeklyReportBuild _pendingAutoWeeklyReportBuild','private bool _weeklyReportGenerationInProgress','private int _lastAutoGeneratedWeeklyReportWeek']))
# The clone operation is now a named late declaration but remains part of commit semantics.
core+='\n'+extract.declaration(actual_runtime,'internal static List<EventMaterialReference> CloneWeeklyReportMaterials(')
core+='\n'+actual_runtime[actual_runtime.index(' private WeeklyReportCommitQueueOwner<PendingWeeklyWaveLaunchContext'):actual_runtime.index('internal bool ProcessPendingWeeklyPromptPreparations(')]
(out/'ActualRuntime.cs').write_text('using System;using System.Collections.Generic;using System.Linq;using System.Diagnostics;using System.Threading.Tasks;using Newtonsoft.Json;using static AnimusForge.MyBehavior;namespace AnimusForge;'+port+' internal sealed class WeeklyReportRuntimeOwner {'+core+'}',encoding='utf-8')
composition_path='src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyRuntime.cs'
composition=(ROOT/composition_path).read_text(encoding='utf-8-sig')
factory=composition[composition.index(' private WeeklyReportRuntimeOwner _weeklyRuntime;'):composition.index(' private static int CaptureWeeklyReportLengthPreset()')]
cut=factory.index('  MemoryState =');late=factory[factory.index('  WaveLaunchQueue ='):factory.index(' },_weeklyReportMaterialRevisions')];factory=factory[:cut]+late+' },_weeklyReportMaterialRevisions,_weeklyReportCommitQueue,_weeklyPromptPreparationQueue); }'
wrappers=composition[composition.index(' private static int CaptureWeeklyReportLengthPreset()'):composition.rfind('}')]
# Current root forwards the same original named methods; all these still execute actual core.
(out/'ActualComposition.cs').write_text('using System;using System.Collections.Generic;using System.Text;using System.Threading.Tasks;using TaleWorlds.Library;namespace AnimusForge;public partial class MyBehavior {'+factory+wrappers+'}',encoding='utf-8')
(out/'source-manifest.json').write_text(json.dumps({'sources':{runtime_path:hashlib.sha256((ROOT/runtime_path).read_bytes()).hexdigest(),composition_path:hashlib.sha256((ROOT/composition_path).read_bytes()).hexdigest()},'extraction':'complete current original burst/prepare/commit methods and exercised typed root bindings; later automatic/full-event NOT_IN_THIS_ENTRY; actual attempt/wave/API pumps run; game/UI/network response only are controlled'},indent=2),encoding='utf-8')
network_text="[REPORT_BLOCK_BEGIN]\nreport_id=kingdom:A\nmode=full_report\nkind=kingdom\nkingdom_id=A\n[TITLE]\ntitle\n[SHORT]\nshort\n[REPORT]\nreport\n[TAGS]\nSTAB_FLAT\n[REPORT_BLOCK_END]"
network_literal=json.dumps(network_text)
queue_fields='private readonly WeeklyReportCommitQueueOwner<PendingWeeklyWaveLaunchContext,List<Task<WeeklyReportBatchExecutionResult>>> _weeklyWaveLaunchQueue=new((c,r)=>c.CompletionSource.TrySetResult(r),()=>null); private readonly WeeklyReportCommitQueueOwner<PendingWeeklyBatchApiAttemptContext,Task<ApiCallResult>> _weeklyBatchApiAttemptQueue=new((c,r)=>c.CompletionSource.TrySetResult(r),()=>null); private readonly WeeklyGenerationAttemptOwner _weeklyGenerationAttemptOwner=new(new WeeklyGenerationRules(x=>x)); private Task<ApiCallResult> CallWeeklyReportApiDetailed(string s,string u)=>Task.FromResult(new ApiCallResult{Success=true,Content='+network_literal.replace('title','composition')+'}); private WeeklyGenerationAttemptPort CreateWeeklyGenerationAttemptPort()=>new(){CallBatch=WeeklyRuntime.CallWeeklyReportBatchApiAttemptAsync,CallGroup=CallWeeklyReportApiDetailed,Log=Logger.Log,LogExchange=(s,u,r)=>{},Delay=_=>Task.CompletedTask}; private void FlushNetwork(){if(!_weeklyWaveLaunchQueue.HasPending)return;WeeklyRuntime.ProcessPendingWeeklyWaveLaunches();var deadline=DateTime.UtcNow.AddSeconds(8);while(!_weeklyReportCommitQueue.HasPending){WeeklyRuntime.ProcessPendingWeeklyBatchApiAttempts();if(DateTime.UtcNow>deadline)throw new Exception("fixture wave did not commit");System.Threading.Thread.Sleep(1);}}'
shim=shim.replace('h._weeklyPromptPreparationQueue.Complete(prompt,WeeklyPromptPreparationResult.Prepared);','while(prompt.Cursor.TryTake(out var preparedBatch)){preparedBatch.SystemPrompt="system";preparedBatch.UserPrompt="user";}h._weeklyPromptPreparationQueue.Complete(prompt,WeeklyPromptPreparationResult.Prepared);')
shim=shim.replace('private static MyBehavior Instance;', 'private static MyBehavior Instance;'+queue_fields).replace('h._weeklyPromptPreparationQueue.CompleteProcessed(prompt);','h._weeklyPromptPreparationQueue.CompleteProcessed(prompt);h.FlushNetwork();')
program=(HERE/"Program.cs.txt").read_text(encoding='utf-8').replace('sealed class RuntimeFixture','sealed partial class RuntimeFixture')
program=re.sub(r'(\b(\w+)\.Prompts\.CompleteProcessed\([^;]+\);)',r'\1\2.FlushNetwork();',program)
program=re.sub(r'(\b(\w+)\.Prompts\.Complete\((\w+),WeeklyPromptPreparationResult.Prepared\);)',r'\2.PreparePrompt(\3);\1',program)
program=program.replace('var executions=await waves.Owner.CoordinateWeeklyReportWavesAsync(', 'foreach(var preparedBatch in batches){preparedBatch.SystemPrompt="system";preparedBatch.UserPrompt=preparedBatch.Groups[0].KingdomId;}var executions=await waves.Owner.CoordinateWeeklyReportWavesAsync(')
program=program.replace('var executions=await waves.Owner.CoordinateWeeklyReportWavesAsync(', 'var executions=await waves.PumpNetwork(waves.Owner.CoordinateWeeklyReportWavesAsync(').replace('ms=>{delays++;return Task.CompletedTask;});','ms=>{delays++;return Task.CompletedTask;}));')
program=program.replace('internal RuntimeFixture(){','internal RuntimeFixture(){while(SaveRuntimeGuard.CaptureGeneration()<7)SaveRuntimeGuard.AdvanceGeneration("runtime-fixture");')
program=program.replace('Owner=new(new(){','Owner=new(new(){WaveLaunchQueue=Waves,BatchApiAttemptQueue=ApiAttempts,GenerationAttempt=new(new WeeklyGenerationRules(x=>x)),CallApi=CallApi,AttemptPort=()=>new(){CallBatch=Owner.CallWeeklyReportBatchApiAttemptAsync,CallGroup=CallApi,Log=Logger.Log,LogExchange=(s,u,r)=>{},Delay=_=>Task.CompletedTask},')
program+='sealed partial class RuntimeFixture {internal void PreparePrompt(PendingWeeklyPromptPreparationContext p){while(p.Cursor.TryTake(out var b)){b.SystemPrompt="system";b.UserPrompt="user";}} internal async Task<T> PumpNetwork<T>(Task<T> task){var deadline=DateTime.UtcNow.AddSeconds(8);while(!task.IsCompleted){Owner.ProcessPendingWeeklyWaveLaunches();Owner.ProcessPendingWeeklyBatchApiAttempts();if(DateTime.UtcNow>deadline)throw new Exception("fixture network did not complete");await Task.Delay(1);}return await task;} internal readonly WeeklyReportCommitQueueOwner<PendingWeeklyWaveLaunchContext,List<Task<WeeklyReportBatchExecutionResult>>> Waves=new((c,r)=>c.CompletionSource.TrySetResult(r),()=>null);internal readonly WeeklyReportCommitQueueOwner<PendingWeeklyBatchApiAttemptContext,Task<ApiCallResult>> ApiAttempts=new((c,r)=>c.CompletionSource.TrySetResult(r),()=>null);private Task<ApiCallResult> CallApi(string s,string u){Launches++;return Task.FromResult(new ApiCallResult{Success=true,Content=('+network_literal+').Replace("kingdom:A","kingdom:"+(u=="B"?"B":"A")).Replace("kingdom_id=A","kingdom_id="+(u=="B"?"B":"A"))});} internal void FlushNetwork(){if(!Waves.HasPending)return;Owner.ProcessPendingWeeklyWaveLaunches();var deadline=DateTime.UtcNow.AddSeconds(8);while(!Commits.HasPending){Owner.ProcessPendingWeeklyBatchApiAttempts();if(DateTime.UtcNow>deadline)throw new Exception("fixture wave did not commit");Thread.Sleep(1);}}}'
(out/"Shims.cs").write_text(shim,encoding="utf-8")
(out/"OriginalPromptOracle.cs").write_text((HERE/"OriginalPromptOracle.cs.txt").read_text(encoding="utf-8"),encoding="utf-8")
(out/"Program.cs").write_text(program,encoding="utf-8")
original_program=(HERE/"Program.cs.txt").read_text(encoding="utf-8")
assert re.findall(r"Check\((.*?)\);",program,re.S)==re.findall(r"Check\((.*?)\);",original_program,re.S),"original assertions changed"
paths=["src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitQueueOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportCommitTargetOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportMaterialRevisionOwner.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportBlockMaterialCursor.cs","src/modules/AF.Module.Weekly/Generation/WeeklyReportWaveCoordinator.cs","src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs","src/modules/AF.Module.Weekly/Generation/WeeklyGenerationModels.cs","src/modules/AF.Module.Weekly/Generation/WeeklyGenerationAttemptOwner.cs","src/modules/AF.Module.Weekly/Materials/WeeklyMaterialStageCursor.cs","src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs","src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.SystemPrompt.cs","src/modules/AF.Module.Weekly/Materials/WeeklyPromptMaterialOwner.MaterialCopies.cs","src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs"]
manifest=json.loads((out/'source-manifest.json').read_text())
manifest['sources'].update({rel:hashlib.sha256((ROOT/rel).read_bytes()).hexdigest() for rel in paths+['tests/modules/AF.Module.Weekly/RuntimeClosureTests/run.py','tests/modules/AF.Module.Weekly/RuntimeClosureTests/Program.cs.txt','tests/modules/AF.Module.Weekly/RuntimeClosureTests/CompositionLeafShims.cs.txt','tests/modules/AF.Module.Weekly/RuntimeClosureTests/OriginalPromptOracle.cs.txt','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs','src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs']})
manifest['originalChecksPreserved']=len(re.findall(r'Check\((.*?)\);',original_program,re.S))
manifest['generated']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.glob('*.cs')}
(out/'source-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
links='<Compile Include="ActualRuntime.cs"/><Compile Include="ActualComposition.cs"/>'+"".join('<Compile Include="'+str(ROOT/path)+'"/>' for path in paths)
jsondll=sdk_root/sdk_version/'Newtonsoft.Json.dll'
(out/"Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+links+'<Compile Include="Program.cs"/><Compile Include="Shims.cs"/><Compile Include="OriginalPromptOracle.cs"/><Reference Include="Newtonsoft.Json"><HintPath>'+str(jsondll)+'</HintPath></Reference></ItemGroup></Project>',encoding="utf-8")
(out/"NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding="utf-8")
for command,log in [([str(dotnet),"build",str(out/"Proof.csproj"),"--nologo","-p:RestoreConfigFile="+str(out/"NuGet.Config")],"build.log"),([str(dotnet),str(out/"bin/Debug/net8.0/Proof.dll")],"run.log")]:
 result=subprocess.run(command,cwd=out,env=env,capture_output=True,text=True,encoding="utf-8",errors="replace",timeout=180);(out/log).write_text(result.stdout+result.stderr,encoding="utf-8");print(result.stdout+result.stderr)
 if result.returncode:raise SystemExit(result.returncode)
print("OUTPUT",out)
