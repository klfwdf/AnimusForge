from pathlib import Path
import argparse, importlib.util, subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def load(name,path):
 spec=importlib.util.spec_from_file_location(name,path);result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result
ex=load('decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
util=load('util',ROOT/'tools/ModuleFrameworkApiTests/run.py')
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['worker_assembly','main_preprocess','skip_accept','wrong_direction','skip_source']);p.add_argument('--old-worker',action='store_true');args=p.parse_args()
source=(ROOT/'CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig')
phase=ex.declaration((ROOT/'CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
if args.mutate=='worker_assembly':
 source=source.replace('return await RunCourierOwnerPhaseAsync(generation, source + "_assemble", () =>','return await Task.Run(() =>').replace('                return assemble(input, prepared);\n            }, CancellationToken.None).ConfigureAwait(false);','                return assemble(input, prepared);\n            }).ConfigureAwait(false);')
if args.mutate=='main_preprocess':source=source.replace('await Task.Run(() => BuildCourierPreparedPrompt(input))','await RunCourierOwnerPhaseAsync(generation, source + "_unsafe_preprocess", () => BuildCourierPreparedPrompt(input), CancellationToken.None)')
if args.mutate=='skip_accept':source=source.replace('if (!IsCourierPromptInputCurrent(input))','if (false)')
if args.mutate=='wrong_direction':source=source.replace('inbound ? "[NPC主动写信意图] " + Seed : LetterText','inbound ? LetterText : "[NPC主动写信意图] " + Seed')
if args.mutate=='skip_source':source=source.replace('return string.Equals(input.Session.LetterText, input.LetterText, StringComparison.Ordinal)','return true || string.Equals(input.Session.LetterText, input.LetterText, StringComparison.Ordinal)')
old=subprocess.check_output(['git','show','154f7206:CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
methods='\n'.join(ex.declaration(old,'private '+typ+' '+name+'(').replace(name,name+'Baseline',1) for typ,name in [('CourierReplyGenerationRequest','BuildCourierReplyGenerationRequestOnMainThread'),('InboundLetterGenerationRequest','BuildInboundLetterGenerationRequestOnMainThread')])
reqs='\n'.join(ex.declaration(old,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
history=(ROOT/'CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig')
owner=ex.declaration(history,'private bool IsCourierHistoryOwnerCurrent(')
historytype=ex.declaration(history,'private sealed class CourierPreparedHistory')
harness=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@OWNER_PHASE@@',phase).replace('@@BASELINE@@',methods).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historytype+'\n'+owner)
if args.old_worker:harness='#define OLD_WORKER\n'+harness
out=HERE/'.generated'/('old-worker' if args.old_worker else args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text(harness,encoding='utf-8')
project=util.project(out,'CourierPromptChecks',[out/'Prompt.cs',out/'Program.cs',ROOT/'Refactor/Runtime/PendingOperationRegistry.cs'],executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
