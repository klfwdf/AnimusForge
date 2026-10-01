from pathlib import Path
import sys,importlib.util,subprocess,json,hashlib,argparse
R=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path,required=True);args=parser.parse_args()
out=args.run_root.resolve();out.relative_to(R);out.mkdir(parents=True,exist_ok=True)
owner_path='src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs'
adapter_path='src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs'
ledger_path='src/modules/AF.Module.Memory/Recovery/InteractionMemoryRecoveryLedger.cs'
owner=(R/owner_path).read_text(encoding='utf-8-sig');adapter=(R/adapter_path).read_text(encoding='utf-8-sig')
main_path='src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'
main=(R/main_path).read_text(encoding='utf-8-sig')
clear=ex.declaration(main,'private void ClearAllDataForCurrentSave(')
order=['RetireConversationRequestsForDeveloperClear();','OnDeveloperClearWeeklyActionOutcomes(SaveRuntimeGuard.CurrentGeneration);','ClearInteractionMemoryRecoveryForDeveloperClear();','_memoryBusinessState.ClearHistoryAndDailyForCurrentSave();']
positions=[clear.index(x) for x in order]
assert positions==sorted(positions)
assert main.count('ClearInteractionMemoryRecoveryForDeveloperClear();')==1
transition=ex.declaration(owner,'internal void ClearVisibleMemoryForDeveloperClear(')
ensure=ex.declaration(owner,'internal InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger(')
capture=ex.declaration(adapter,'private void ClearInteractionMemoryRecoveryForDeveloperClear(')
seed=ex.declaration((R/'tests/modules/AF.Module.Memory/MemoryCommitRecoveryContractTests/Program.cs').read_text(encoding='utf-8-sig'),'static InteractionMemoryRecoverySeed Seed(')
program='''using System;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using System.Linq;using AnimusForge.Refactor.Runtime;
namespace AnimusForge {
internal static class Logger {internal static void Log(string c,string m) {} }
internal sealed class MemoryRecoveryStateOwner {
 internal InteractionMemoryRecoveryLedger Ledger=new();internal int HasWork,LoadConfirmed;internal long LoadedGeneration,NextAttemptTicks;
'''+ensure+transition+'''}
internal sealed class AdapterProbe {
 internal MemoryRecoveryStateOwner MemoryRecoveryState=new();internal Dictionary<string,string> _interactionMemoryRecoveryStorage=new(StringComparer.Ordinal);
 private InteractionMemoryRecoveryLedger EnsureInteractionMemoryRecoveryLedger()=>MemoryRecoveryState.EnsureInteractionMemoryRecoveryLedger();
 internal void Clear()=>ClearInteractionMemoryRecoveryForDeveloperClear();
'''+capture+'''}
internal static class Program {
 static int count;static void C(bool ok,string why){count++;if(!ok)throw new Exception(why);}
'''+seed+'''
 static async Task Main(){
 var p=new AdapterProbe();var o=p.MemoryRecoveryState;var l=o.Ledger;o.LoadConfirmed=1;o.LoadedGeneration=SaveRuntimeGuard.CurrentGeneration;o.HasWork=1;o.NextAttemptTicks=999;
 C(l.Begin(Seed("old"),out var oldId,out _)==InteractionMemoryRecoveryBeginStatus.Began,"old pending seeded");C(l.TryGetNextWork(out var oldWork),"old started work");
 C(l.Begin(Seed("completed"),out var completedId,out _)==InteractionMemoryRecoveryBeginStatus.Began,"completed candidate seeded");while(l.TryGetNextWorkFor(completedId,out var done))l.MarkApplied(done);
 C(l.CompletedCount==1,"completed tombstone present before clear");C(l.Begin(Seed("quarantined"),out var quarantineId,out _)==InteractionMemoryRecoveryBeginStatus.Began&&l.QuarantineEntry(quarantineId,"fixture"),"quarantine and blocked id seeded");
 p._interactionMemoryRecoveryStorage=l.Export();C(p._interactionMemoryRecoveryStorage.Count==3,"save storage includes all visible journal states");
 var captured=SaveRuntimeGuard.CaptureGeneration();var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
 var late=Task.Run(async()=>{entered.SetResult(true);await release.Task;return SaveRuntimeGuard.IsCurrentGeneration(captured)&&l.MarkApplied(oldWork);});await entered.Task;
 SaveRuntimeGuard.AdvanceGeneration("dev_clear_all_data");p.Clear();release.SetResult(true);
 C(!await late,"strong yield late result rejected");C(!l.MarkApplied(oldWork)&&!l.MarkPending(oldWork)&&!l.MarkUnknown(oldWork),"old work absent from same journal");
 C(ReferenceEquals(l,o.Ledger),"same journal authority retained");C(o.LoadConfirmed==1&&o.LoadedGeneration==SaveRuntimeGuard.CurrentGeneration,"confirmed active generation explicitly rebound");
 C(o.HasWork==0&&o.NextAttemptTicks==0&&!l.HasPendingWork&&!l.HasUnresolvedWork,"transient work retired");C(p._interactionMemoryRecoveryStorage.Count==0&&l.PendingCount==0&&l.CompletedCount==0&&l.QuarantineCount==0,"visible payload and storage cleared");
 var imported=new InteractionMemoryRecoveryLedger();imported.Import(p._interactionMemoryRecoveryStorage);C(imported.PendingCount==0&&!imported.HasPendingWork&&!imported.TryGetNextWork(out _),"save/import does not revive visible memory");
 C(l.Begin(Seed("new"),out _,out _)==InteractionMemoryRecoveryBeginStatus.Began&&l.TryGetNextWork(out var fresh)&&l.MarkApplied(fresh),"new generation request can progress");
 foreach(bool confirmed in new[]{false,true}){
 var q=new AdapterProbe();q.MemoryRecoveryState.LoadConfirmed=confirmed?1:0;q.MemoryRecoveryState.LoadedGeneration=SaveRuntimeGuard.CurrentGeneration;
 q.MemoryRecoveryState.Ledger.DisableForCurrentCampaign("original_failed_load");q.Clear();
 C(q.MemoryRecoveryState.LoadConfirmed==0&&q.MemoryRecoveryState.LoadedGeneration==0&&q.MemoryRecoveryState.Ledger.IsDisabled,"disabled never cured "+confirmed);
 C(q._interactionMemoryRecoveryStorage.Count==1&&q._interactionMemoryRecoveryStorage["!disabled"]=="original_failed_load","original disabled reason persisted "+confirmed);
 var restored=new InteractionMemoryRecoveryLedger();restored.Import(q._interactionMemoryRecoveryStorage);C(restored.IsDisabled&&restored.Export()["!disabled"]=="original_failed_load","disabled survives save/import "+confirmed);
 }
 var failed=new AdapterProbe();failed.MemoryRecoveryState.LoadConfirmed=0;failed.Clear();C(failed.MemoryRecoveryState.LoadConfirmed==0&&failed.MemoryRecoveryState.LoadedGeneration==0,"unconfirmed load not manufactured");
 p.Clear();C(l.PendingCount==0&&p._interactionMemoryRecoveryStorage.Count==0&&o.LoadedGeneration==SaveRuntimeGuard.CurrentGeneration,"repeated explicit clear stable");
 Console.WriteLine("PASS explicit recovery developer clear checks="+count+" live=NOT_RUN");
 }
}}
'''
(out/'Program.cs').write_text(program,encoding='utf-8')
paths=[ledger_path,'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/p)+'" />' for p in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
receipt={'exit_code':r.returncode,'source_sha256':{p:hashlib.sha256((R/p).read_bytes()).hexdigest() for p in [ledger_path,owner_path,adapter_path,main_path]},'test_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'main_consumer_order':order,'main_consumer_unique':True,'boundary':'ledger and SaveRuntimeGuard whole source linked; exact owner transition/adapter extracted; game leaves not executed'}
(out/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');sys.exit(r.returncode)
