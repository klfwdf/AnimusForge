"""Actual public consumer -> service -> Native queue/admission/commit receipt; game/provider ports are fixtures."""
from pathlib import Path
import argparse, importlib.util, os, shutil, subprocess, sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[2]; HERE=Path(__file__).parent
sys.stdout.reconfigure(encoding='utf-8')
p=argparse.ArgumentParser();p.add_argument('--reorder-core-enums',action='store_true');p.add_argument('--mutate',choices=['ignore-cancel','text-success','drop-receipt','replace-confirmed','replay-id','skip-generation','skip-conversation','skip-revision','skip-channel','skip-context']);p.add_argument('--dotnet',default=os.environ.get('DOTNET_EXE','dotnet'));a=p.parse_args()
dotnet=Path(a.dotnet) if Path(a.dotnet).is_absolute() else Path(shutil.which(a.dotnet) or '')
if not dotnet.is_file():p.error('dotnet executable not found: '+a.dotnet)
dotnet=dotnet.resolve()
spec=importlib.util.spec_from_file_location('extract',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=HERE/'.generated'/('reordered' if a.reorder_core_enums else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig')
sigs=['private Task<T> RunNativeConversationMainThreadFuncAsync<T>(', 'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(', 'private sealed class NativeConversationGameActionResult','private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(']
host=(HERE/'Host.cs.txt').read_text().replace('@@REAL_DECLARATIONS@@','\n'.join(ex.declaration(s,sig) for sig in sigs))
scene=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs').read_text(encoding='utf-8-sig')
post=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs').read_text(encoding='utf-8-sig')
scene_sigs=['private void RegisterModuleSceneGroup(', 'private void RetireModuleSceneGroup(',
            'private void ReleaseModuleSceneGroup(', 'private sealed class SceneGroupReceipt',
            'internal static void SubmitModuleSceneDialogue(', 'private async Task RunModuleSceneDialogueAsync(']
scene_declarations='\n'.join([ex.declaration(post,'private enum ScenePostprocessStatus'),
                              ex.declaration(post,'private sealed class ScenePostprocessOutcome')]
                             +[ex.declaration(scene,sig) for sig in scene_sigs])
host=host.replace('@@SCENE_DECLARATIONS@@',scene_declarations)
(out/'Host.cs').write_text(host,encoding='utf-8')
contracts=(ROOT/'Refactor/Contracts/InteractionContracts.cs').read_text(encoding='utf-8-sig')
(out/'Contracts.cs').write_text('namespace AnimusForge.Refactor.Contracts;\n'+'\n'.join(ex.declaration(contracts,x) for x in ['public enum ActionExecutionEffectState','public enum MemoryCommitStatus','public sealed class MemoryCommitResult']),encoding='utf-8')
paths=['src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs','ShoutBehavior.NativeAdmission.cs','ShoutBehavior.NativeCompletion.cs','ShoutBehavior.NativeActionDispatch.cs','ShoutBehavior.ModuleNativeSubmission.cs','src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs','src/modules/AF.Module.PublicApi/V1/AfApi.cs','src/AF.Contracts/PublicApi/V1/AfApiContracts.cs','src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs','src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs','src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs','Refactor/Modules/CoreDialogueContracts.cs','Refactor/Modules/CoreDialogueOperation.cs','Refactor/Modules/CoreDialogueClient.cs','Refactor/Modules/CoreDialogueServices.cs','src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs','src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs','src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs','src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs','src/AF.GameAdapter.Bannerlord/Composition/TeamModuleRegistration.cs','Refactor/Contracts/FeatureBridgeContracts.cs']
mutations={
 'ignore-cancel':('Refactor/Modules/CoreDialogueOperation.cs','if (_snapshot.State != CoreDialogueState.Queued) return false;','if (_snapshot.State == CoreDialogueState.Running) return false;'),
 'text-success':('ShoutBehavior.ModuleNativeSubmission.cs','if (execution != null) await execution.ConfigureAwait(false);','if (execution != null) operation.RecordOwnerCompletion(await execution.ConfigureAwait(false));'),
 'drop-receipt':('ShoutBehavior.NativeCompletion.cs','scope.Admission.ModuleOperation?.RecordOwnerCompletion(finalVisible);',';'),
 'replace-confirmed':('Refactor/Modules/CoreDialogueOperation.cs','if (_ownerCompleted)','if (false)'),
 'replay-id':('Refactor/Modules/CoreDialogueClient.cs','? operation : Rejected(requestId, "dialogue.request_id_conflict")','? Rejected(requestId, "mutant.replayed_id") : Rejected(requestId, "dialogue.request_id_conflict")'),
 'skip-channel':('Refactor/Modules/CoreDialogueClient.cs','operation.Channel == channel','true'),
 'skip-context':('Refactor/Modules/CoreDialogueClient.cs','string.Equals(operation.ContextIdentity, contextIdentity, StringComparison.Ordinal)','true'),
 'skip-generation':('ShoutBehavior.ModuleNativeSubmission.cs','|| !SaveRuntimeGuard.IsCurrentGeneration(generation)','|| false'),
 'skip-conversation':('ShoutBehavior.ModuleNativeSubmission.cs','|| !_nativeAdmissionOwner.IsConversationEpochCurrent(conversationEpoch)','|| false'),
 'skip-revision':('ShoutBehavior.ModuleNativeSubmission.cs','|| !_nativeAdmissionOwner.IsPresentationCurrent(presentationRevision)','|| false')}
sources=[out/'Host.cs',out/'Contracts.cs',ROOT/'tools/ModuleFrameworkApiTests/HostStubs.cs']
for path in paths:
 text=(ROOT/path).read_text(encoding='utf-8-sig')
 if a.reorder_core_enums and path=='Refactor/Modules/CoreDialogueContracts.cs':
  text=text.replace('Queued, Running, Completed, Rejected, Cancelled, Failed','Queued=100, Running=20, Completed=50, Rejected=1, Cancelled=30, Failed=6').replace('NoConfirmedEffect, UnknownAfterStart, CompletedByOwner','NoConfirmedEffect=4, UnknownAfterStart=8, CompletedByOwner=2').replace('CancelledBeforeStart, AlreadyTerminal, TooLate','CancelledBeforeStart=7, AlreadyTerminal=2, TooLate=4')
 if a.mutate and path==mutations[a.mutate][0]:
  _,before,after=mutations[a.mutate];assert text.count(before)==1,(a.mutate,path);text=text.replace(before,after)
 target=out/Path(path).name;target.write_text(text,encoding='utf-8');sources.append(target)
def project(name,srcs,refs=(),exe=False):
 file=out/(name+'.csproj');file.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems><OutputType>'+('Exe' if exe else 'Library')+'</OutputType></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(x.resolve()))+'" />' for x in srcs)+''.join('<ProjectReference Include="'+escape(str(x.resolve()))+'" />' for x in refs)+'</ItemGroup></Project>',encoding='utf-8');return file
library=project('NativeModuleUnderTest',sources)
(out/'Client.cs').write_text((HERE/'Client.cs.txt').read_text(),encoding='utf-8');client=project('NativeModuleClient',[out/'Client.cs'],[library],True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy();env.update(DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
result=subprocess.run([str(dotnet),'run','--project',str(client),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=result.stdout+result.stderr
if result.returncode==0:
 (out/'Denied.cs').write_text('class Denied { static void Main() { AnimusForge.Refactor.Modules.CoreDialogueServices.CreateClient(); } }',encoding='utf-8')
 denied=project('NativeModuleDenied',[out/'Denied.cs'],[library],True)
 probe=subprocess.run([str(dotnet),'build',str(denied),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
 denied_log=probe.stdout+probe.stderr;(out/'denied.log').write_text(denied_log,encoding='utf-8')
 assert probe.returncode!=0 and 'CS0122' in denied_log and 'CoreDialogueServices' in denied_log, 'Internal service was not rejected from unrelated external assembly'
 log+='PASS unrelated external client cannot use internal CoreDialogueServices (CS0122)\n'
(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(result.returncode)
