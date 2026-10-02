from pathlib import Path
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
# Explicit legacy oracle; current owner build/replay inputs are not projected.
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-oracle-extraction', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerReplayProjected': False}
import argparse, importlib.util, os, re
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
import sys
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root
p=argparse.ArgumentParser();p.add_argument('--old',action='store_true');p.add_argument('--mutate',choices=['drop-failure','ignore-run','old-fallback','keep-stale-tags']);p.add_argument('--output-name');p.add_argument('--run-root',type=Path);a=p.parse_args()
if a.output_name is not None and not re.fullmatch(r'[A-Za-z0-9_-]+',a.output_name):p.error('Invalid output name')
def load(n,p):
 sp=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m);return m
ex=load('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');util=load('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py')
inverse=load('liveness_inverse',HERE/'liveness_review.py')
def source(path):return inverse.old_source(path) if a.old else historical_source(path)
courier=source('CourierDeliveryBehavior.cs') if a.old else ex.courier_source(None);partial=source('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs')
phase=ex.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
base=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').split('internal static class Program {')[0]
message_markers=['private static List<object> BuildCourierReplyMessages(','private static List<object> BuildInboundNpcLetterMessages(',
 'private static object CreateCourierChatMessage(','private static void AppendCourierRawUserSection(',
 'private static void AppendCourierUserSection(','private static void AppendCourierPersistentMemoryRoleMessages(',
 'private static bool TryConvertCourierMemoryMessageToChatMessage(',
 'private static string BuildCourierMemoryMetadataPrefix(','private static string StripCourierPromptScopeLabel(',
 'private static string StripCourierSpeakerPrefix(']
message_source=ex.courier_source(None)
import main_assembly_projection as main_projection
main_projection.projected_messages()
message_builders=main_projection.production_builders()
for name in ('BuildCourierReplyMessages','BuildInboundNpcLetterMessages'):
 message_builders=message_builders.replace('private static List<object> '+name+'(', 'private static List<object> '+name+'Production(',1)
base=base.replace('@@MESSAGE_BUILDERS@@',message_builders).replace('@@FINALIZE_REPLY@@','')
base=base.replace('  private CourierPromptRun TestRun;\n  internal void ReserveTestRun()=>TestRun=BeginCourierPromptRun(Session,1);','')
base=base.replace(ex.declaration(base,'internal async Task<string> Start('),'')
base=base.replace('internal static long Generation=1;','internal static long Generation=1; internal static long CaptureGeneration()=>Generation;')
base=base.replace('ReplyGenerationStarted=true,PostprocessConsumed,DeliveryApplied;internal string ReplyText="",ReplyPostprocessedText="";','ReplyGenerationStarted=true,PostprocessConsumed,DeliveryApplied=true,ReplyWaitPopupShown=true;internal string Stage="GeneratingReply",ReplyText="",ReplyPostprocessedText="",RecipientWaitReason="";')
base=base.replace('static ManualResetEventSlim Entered=new(),Release=new(true);','static ManualResetEventSlim Entered=new(),Release=new(true);')
# The held provider snapshots its release event before a replacement Start can install the new test request.
base=base.replace('Probe.Entered.Set();\n   if(!Probe.Release.Wait(5000))','var release=Probe.Release;Probe.Entered.Set();\n   if(!release.Wait(5000))')
base=base.replace('if(Probe.ThrowRouting)throw new PreprocessFormatException();','if(Probe.ThrowRouting)throw new PreprocessFormatException();')
reqs='\n'.join(ex.declaration(courier,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
campaign=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs').read_text(encoding='utf-8-sig')
lifetime_field=campaign[campaign.index('    private readonly Dictionary<CourierSession, ConversationRequestLifetime>'):campaign.index('    private ConversationRequestLifetime BeginCourierRequestLifetime(')]
lifetime_methods=lifetime_field+'\n'+'\n'.join(ex.declaration(campaign,sig) for sig in ['private ConversationRequestLifetime BeginCourierRequestLifetime(', 'private void RetireCourierRequestLifetime(', 'private void RetireCourierRequestLifetimes('])
history=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig');historyDecl='\n'.join(ex.declaration(history,sig) for sig in ['private sealed class CourierPreparedHistory','private bool IsCourierHistoryOwnerCurrent('])
base=base.replace('@@LIFETIME@@',lifetime_methods).replace('@@OWNER_PHASE@@',phase).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historyDecl).replace('@@BASELINE@@','')
# Sync baseline comparison belongs to run.py; the liveness suite uses actual Start -> caller instead.
base=base.replace(ex.declaration(base,'internal string Sync('),'')
methods='\n'.join(ex.declaration(courier,sig) for sig in ['private void StartCourierReplyGeneration(','private void StartInboundLetterGeneration(','private void BeginCourierReplyGenerationOnMainThread(','private void BeginInboundLetterGenerationOnMainThread(','private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(','private async Task PrepareAndGenerateInboundLetterOffMainThreadAsync(','private void FailCourierReplyGenerationOnMainThread(','private void FailInboundLetterGenerationOnMainThread(','private void ProcessInboundToPlayerSession('])
# Observability only: retain the actual background task handle, without replacing its delegate.
methods=methods.replace('_ = Task.Run(() => PrepareAndGenerate','Liveness.Background = Task.Run(() => PrepareAndGenerate')
process=ex.declaration(courier,'private void ProcessSessionCore(' if 'private void ProcessSessionCore(' in courier else 'private void ProcessSession(')
replytick=ex.declaration(process,'if (stage == CourierStage.GeneratingReply)')
deliver=ex.declaration(courier,'private void DeliverInboundLetterToPlayer(');cut=deliver.index('\n\t\tstring letter = (session.LetterText')
inboundprefix=deliver[:cut]+'\n\t\tLiveness.Deliveries++;\n\t}\n'
if a.mutate=='drop-failure':partial=partial.replace('CompleteCourierPromptSourceChanged(promptRun, input);',';')
if a.mutate=='ignore-run':partial=partial.replace('&& _courierPromptRuns.TryGetValue(run.Session, out CourierPromptRun current) && ReferenceEquals(current, run)','')
if a.mutate=='keep-stale-tags':
 partial=partial.replace('input.Session.ReplyText = string.Empty;','').replace('input.Session.ReplyPostprocessedText = string.Empty;','').replace('input.Session.PostprocessConsumed = true;','')
if a.mutate=='old-fallback':partial=partial.replace('input.Session.InboundFallbackLetter, "inbound_prompt_source_changed"','input.FallbackLetter, "inbound_prompt_source_changed"')
commit=ex.declaration(courier,'private void CommitGeneratedReplyActionsAtRecipientCore(' if a.old else 'private bool CommitGeneratedReplyActionsAtRecipientCore(');commit=commit[:commit.index('\n\t\tif (recipient == null')]+'\n\t\tif (text.Contains("[ACTION:")) Liveness.StaleTagEffects++;\n'+('' if a.old else '\t\treturn true;\n')+'\t}\n'
commit+='\n\tprivate void CommitGeneratedReplyAtRecipient(CourierSession session, Hero recipient, bool persistHistory = true) => CommitGeneratedReplyActionsAtRecipientCore(session, recipient, persistHistory);\n'
transport=(ROOT/'src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs').read_text(encoding='utf-8-sig')
field=transport[transport.index('    private static readonly AsyncLocal<CancellationToken> OwnerCancellation'):];field=field[:field.index(';')+1]
transport_lifetime=field+'\n'+'\n'.join(ex.declaration(transport,sig) for sig in ['internal static IDisposable PushOwnerCancellation(', 'private sealed class OwnerCancellationScope', 'internal static CancellationTokenSource CreateTimeout('])
hooks=(HERE/'LivenessHooks.cs.txt').read_text(encoding='utf-8-sig').replace('@@METHODS@@',methods).replace('@@REPLY_TICK@@',replytick).replace('@@INBOUND_PREFIX@@',inboundprefix).replace('@@COMMIT_GUARD@@',commit).replace('@@TRANSPORT_LIFETIME@@',transport_lifetime)
if a.output_name and a.run_root:p.error('Use either --output-name or --run-root')
out=new_run_root(ROOT,'courier-prompt-liveness',a.run_root or (HERE/'.generated'/a.output_name if a.output_name else None))
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(partial,encoding='utf-8');(out/'Schedule.cs').write_text(historical_source('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs'),encoding='utf-8');(out/'Host.cs').write_text('#define LIVENESS\n'+base,encoding='utf-8');(out/'Hooks.cs').write_text(hooks,encoding='utf-8')
(out/'Program.cs').write_text((HERE/'LivenessCases.cs.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
files=[out/'Prompt.cs',out/'Host.cs',out/'Hooks.cs',out/'Program.cs',ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs',ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs',ROOT/'src/AF.Contracts/Internal/LlmContracts.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs']
if not a.old:files.append(out/'Schedule.cs')
project=util.project(out,'CourierPromptLiveness',files,executable=True)
dotnet=os.environ.get('AF_DOTNET') or str(ROOT/'local/dotnet/8.0.425/dotnet.exe')
code,log=util.run_dotnet(dotnet,['run','--project',str(project),'-c','Release'],out);(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
