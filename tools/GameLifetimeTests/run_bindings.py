from pathlib import Path
import importlib.util
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=HERE/'.generated/bindings';out.mkdir(parents=True,exist_ok=True)
shout=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');courier=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig')
code=(HERE/'Bindings.cs.txt').read_text(encoding='utf-8-sig').replace('@@NATIVE@@',ex.declaration(shout,'private Task<T> RunNativeConversationMainThreadFuncAsync<T>(')).replace('@@WAIT@@',ex.declaration(shout,'private static async Task<T> AwaitNativeConversationMainThreadFuncAsync<T>(')).replace('@@COURIER@@',ex.declaration(courier,'private async Task<T> RunCourierOwnerPhaseAsync<T>('))
(out/'Program.cs').write_text('using System.Diagnostics;\n'+code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs']+[ROOT/p for p in ['src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs','PreprocessFormatException.cs','MyBehavior.CampaignLifetime.cs','ShoutBehavior.CampaignLifetime.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs']]
project=util.project(out,'Bindings',files,executable=True);code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
