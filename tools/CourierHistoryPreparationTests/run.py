from pathlib import Path
import argparse,importlib.util
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['worker_capture','main_resolve','skip_accept','wrong_input']);args=p.parse_args()
source=(ROOT/'CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig')
phase=ex.declaration((ROOT/'CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(')
phase=phase.replace('Task.Delay(30000)','Task.Delay(180)')
if args.mutate=='worker_capture':phase=phase.replace('if (mainThread) Invoke(); else MainThreadActions.Enqueue(Invoke);','Invoke();')
if args.mutate=='main_resolve':
 source=source.replace('string history = await Task.Run(() =>','string history = await RunCourierOwnerPhaseAsync(generation, source + "_unsafe_resolve", () =>').replace('            }).ConfigureAwait(false);','            }, CancellationToken.None).ConfigureAwait(false);')
if args.mutate=='skip_accept':source=source.replace('return current ? new CourierPreparedHistory(work.ExtraFact, history) : null;','return new CourierPreparedHistory(work.ExtraFact, history);')
if args.mutate=='wrong_input':source=source.replace('inbound ? null : expectedSession.LetterText','inbound ? expectedSession.LetterText : null')
out=HERE/'.generated'/(args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'History.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@OWNER_PHASE@@',phase),encoding='utf-8')
project=util.project(out,'CourierHistoryChecks',[out/'History.cs',out/'Program.cs'],executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
