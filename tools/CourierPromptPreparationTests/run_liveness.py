from pathlib import Path
import argparse, importlib.util
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--old',action='store_true');p.add_argument('--mutate',choices=['drop-failure','ignore-run','old-fallback','keep-stale-tags']);a=p.parse_args()
def load(n,p):
 sp=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);return m
ex=load('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');util=load('util',ROOT/'tools/ModuleFrameworkApiTests/run.py')
inverse=load('liveness_inverse',HERE/'liveness_review.py')
def source(path):return inverse.old_source(path) if a.old else (ROOT/path).read_text(encoding='utf-8-sig')
courier=source('CourierDeliveryBehavior.cs');partial=source('CourierDeliveryBehavior.PromptPreparation.cs')
phase=ex.declaration((ROOT/'CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
base=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('internal static class Program {')[0]
base=base.replace('  private CourierPromptRun TestRun;\n  internal void ReserveTestRun()=>TestRun=BeginCourierPromptRun(Session,1);','')
base=base.replace(ex.declaration(base,'internal async Task<string> Start('),'')
base=base.replace('internal static long Generation=1;','internal static long Generation=1; internal static long CaptureGeneration()=>Generation; internal static bool IsStale(long g,string s)=>!IsCurrentGeneration(g);')
base=base.replace('ReplyGenerationStarted=true,PostprocessConsumed;internal string ReplyText="",ReplyPostprocessedText="";','ReplyGenerationStarted=true,PostprocessConsumed,DeliveryApplied=true,ReplyWaitPopupShown=true;internal string Stage="GeneratingReply",ReplyText="",ReplyPostprocessedText="",SenderName="sender",RecipientWaitReason="";')
base=base.replace('static ManualResetEventSlim Entered=new(),Release=new(true);','static ManualResetEventSlim Entered=new(),Release=new(true);')
# The held provider snapshots its release event before a replacement Start can install the new test request.
base=base.replace('Probe.Entered.Set();\n   if(!Probe.Release.Wait(5000))','var release=Probe.Release;Probe.Entered.Set();\n   if(!release.Wait(5000))')
base=base.replace('if(Probe.ThrowRouting)throw new PreprocessFormatException();','if(Probe.ThrowRouting)throw new PreprocessFormatException();')
reqs='\n'.join(ex.declaration(courier,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
history=(ROOT/'CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig');historyDecl='\n'.join(ex.declaration(history,sig) for sig in ['private sealed class CourierPreparedHistory','private bool IsCourierHistoryOwnerCurrent('])
base=base.replace('@@OWNER_PHASE@@',phase).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historyDecl).replace('@@BASELINE@@','')
# Sync baseline comparison belongs to run.py; the liveness suite uses actual Start -> caller instead.
base=base.replace(ex.declaration(base,'internal string Sync('),'')
methods='\n'.join(ex.declaration(courier,sig) for sig in ['private void StartCourierReplyGeneration(','private void StartInboundLetterGeneration(','private void BeginCourierReplyGenerationOnMainThread(','private void BeginInboundLetterGenerationOnMainThread(','private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(','private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync(','private void FailCourierReplyGenerationOnMainThread(','private void FailInboundLetterGenerationOnMainThread(','private void ProcessInboundToPlayerSession('])
# Observability only: retain the actual background task handle, without replacing its delegate.
methods=methods.replace('_ = Task.Run(() => PrepareAndGenerate','Liveness.Background = Task.Run(() => PrepareAndGenerate')
process=ex.declaration(courier,'private void ProcessSession(')
replytick=ex.declaration(process,'if (stage == CourierStage.GeneratingReply)')
deliver=ex.declaration(courier,'private void DeliverInboundLetterToPlayer(');cut=deliver.index('\n\t\tstring letter = (session.LetterText')
inboundprefix=deliver[:cut]+'\n\t\tLiveness.Deliveries++;\n\t}\n'
if a.mutate=='drop-failure':partial=partial.replace('CompleteCourierPromptSourceChanged(promptRun, input);',';')
if a.mutate=='ignore-run':partial=partial.replace('&& _courierPromptRuns.TryGetValue(run.Session, out CourierPromptRun current) && ReferenceEquals(current, run)','')
if a.mutate=='keep-stale-tags':
 partial=partial.replace('input.Session.ReplyText = string.Empty;','').replace('input.Session.ReplyPostprocessedText = string.Empty;','').replace('input.Session.PostprocessConsumed = true;','')
if a.mutate=='old-fallback':partial=partial.replace('input.Session.InboundFallbackLetter, "inbound_prompt_source_changed"','input.FallbackLetter, "inbound_prompt_source_changed"')
commit=ex.declaration(courier,'private void CommitGeneratedReplyAtRecipient(');commit=commit[:commit.index('\n\t\tif (recipient == null')]+'\n\t\tif (text.Contains("[ACTION:")) Liveness.StaleTagEffects++;\n\t}\n'
hooks=(HERE/'LivenessHooks.cs.txt').read_text(encoding='utf-8-sig').replace('@@METHODS@@',methods).replace('@@REPLY_TICK@@',replytick).replace('@@INBOUND_PREFIX@@',inboundprefix).replace('@@COMMIT_GUARD@@',commit)
out=HERE/'.generated'/('liveness-old' if a.old else 'liveness-'+(a.mutate or 'current'));out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(partial,encoding='utf-8');(out/'Host.cs').write_text('#define LIVENESS\n'+base,encoding='utf-8');(out/'Hooks.cs').write_text(hooks,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'LivenessCases.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
project=util.project(out,'CourierPromptLiveness',[out/'Prompt.cs',out/'Host.cs',out/'Hooks.cs',out/'Program.cs',ROOT/'Refactor/Runtime/PendingOperationRegistry.cs'],executable=True)
code,log=util.run_dotnet(r'G:\AFMOD\.dotnet-sdk\dotnet.exe',['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
