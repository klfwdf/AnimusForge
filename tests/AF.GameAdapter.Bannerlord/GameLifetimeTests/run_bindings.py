from pathlib import Path
import os
import argparse
import importlib.util
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path, new_run_root
spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);parser.add_argument('--dotnet');parser.add_argument('--mutate', choices=['skip-public-execution-reset','skip-execution-unsubscribe']);args=parser.parse_args()
out=new_run_root(ROOT,'game-lifetime-bindings',args.run_root)
shout=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig');courier=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig')
signature='private Task<T> RunNativeConversationMainThreadFuncAsync<T>('
native_facade=shout[shout.index(signature):shout.index(';',shout.index(signature))+1]
assert '_conversationGameThreadDispatcher.RunAsync(operationName, targetLog, targetAgentIndex, func, fallback)' in native_facade
code=(HERE/'Bindings.cs.txt').read_text(encoding='utf-8-sig').replace('@@NATIVE@@',native_facade).replace('@@WAIT@@','').replace('@@COURIER@@',ex.declaration(courier,'private async Task<T> RunCourierOwnerPhaseAsync<T>('))
# Real public-execution retirement algorithms; only manager/permit storage types are fixtures.
runtime=(ROOT/'src/bridges/Vengeance/Host/PublicExecutionOrderRuntime.cs').read_text(encoding='utf-8-sig')
reset=ex.declaration(runtime,'internal static void Reset(');clear=ex.declaration(runtime,'private static void ClearConversation(')
if args.mutate=='skip-public-execution-reset':
 assert reset.count('ClearConversation();')==1;reset=reset.replace('ClearConversation();','',1)
if args.mutate=='skip-execution-unsubscribe':
 anchor='if (_manager != null && _handler != null) _manager.ConversationEndOneShot -= _handler;'
 assert clear.count(anchor)==1;clear=clear.replace(anchor,'',1)

code=code.replace('  private const int NativeConversationMainThreadPreprocessTimeoutMs=30000;', '  public ShoutBehavior(){_mainThreadActionDrain=new(_mainThreadActions);PublicExecutionOrderRuntime.BindFixture(() => CeremonyClears++);}\n  internal const int NativeConversationMainThreadPreprocessTimeoutMs=30000;\n  private ConversationRequestLifetime _sceneRequestLifetime=new();\n  internal static void ResetTransientRuntimeForLoadedSaveExternal(string reason){}',1)
code=code.replace('internal static long Generation=1;', 'internal static long Generation=1;internal static long AdvanceGeneration(string reason)=>++Generation;',1)
code=code.replace(' public partial class CourierDeliveryBehavior {',' public partial class CourierDeliveryBehavior {\n  internal static void ResetTransientRuntimeForLoadedSaveExternal(string reason){}',1)
code+='\nnamespace AnimusForge { internal static class PublicExecutionOrderRuntime { private static readonly object Gate=new object(); private static readonly System.Collections.Generic.Dictionary<string,object> Permits=new(); private static FixtureConversationManager _manager; private static Action _handler; internal static void BindFixture(Action onUnsubscribe){_manager=new FixtureConversationManager(onUnsubscribe);_handler=()=>{};_manager.ConversationEndOneShot+=_handler;Permits["fixture"]=new object();} '+clear+' '+reset+' } internal sealed class FixtureConversationManager { private readonly Action _onUnsubscribe; private Action _handler; internal FixtureConversationManager(Action onUnsubscribe){_onUnsubscribe=onUnsubscribe;} internal event Action ConversationEndOneShot {add{_handler+=value;} remove{_handler-=value;_onUnsubscribe();}} } }'
(out/'Program.cs').write_text('using System.Diagnostics;\n'+code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs']+[current_source_path(ROOT, p) for p in ['src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs','PreprocessFormatException.cs','MyBehavior.CampaignLifetime.cs','ShoutBehavior.CampaignLifetime.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs','src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs']]
files += [ROOT/'src/modules/AF.Module.Conversation/Channels/Native'/name for name in ['ConversationGameThreadDispatcher.cs','ShoutBehavior.NativeGameThreadDispatch.cs','ConversationMainThreadActionDrain.cs']]
project=util.project(out,'Bindings',files,executable=True);code,log=util.run_dotnet((args.dotnet or os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[3] / "local/dotnet/8.0.425/dotnet.exe")),['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
