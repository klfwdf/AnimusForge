from pathlib import Path
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
# Explicit legacy oracle; current owner build/replay inputs are not projected.
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-oracle-extraction', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerReplayProjected': False}
import argparse, importlib.util, os, re, subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys,json
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root

def load(name,path):
 spec=importlib.util.spec_from_file_location(name,path);result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result
ex=load('decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
util=load('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py')
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['worker_assembly','main_preprocess','skip_accept','wrong_direction','skip_source','skip_knowledge_final_guard','drop-knowledge-text','drop-entity-text','drop-rule-text','preflight-implies-delivery']);p.add_argument('--old-worker',action='store_true');p.add_argument('--output-name');p.add_argument('--run-root',type=Path);args=p.parse_args()
if args.output_name is not None and not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name): p.error('Invalid output name')
source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig')
if args.mutate=='drop-knowledge-text':
 needle='string extras = (ctx?.Extras ?? "").Trim();'
 assert source.count(needle)==2
 source=source.replace(needle,'string extras = (ctx?.Extras ?? "").Trim().Replace("【Lore】命中正文", "").Replace("【Lore】兼容回退正文", "");')
if args.mutate=='drop-entity-text':
 needle='string extras = (ctx?.Extras ?? "").Trim();'
 assert source.count(needle)==2
 source=source.replace(needle,'string extras = (ctx?.Extras ?? "").Trim().Replace("【实体】完整事实块", "");')
if args.mutate=='drop-rule-text':
 needle='string extras = (ctx?.Extras ?? "").Trim();'
 assert source.count(needle)==2
 source=source.replace(needle,'string extras = (ctx?.Extras ?? "").Trim().Replace("【附加规则:trade】预选正文", "");')
phase=ex.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
if args.mutate=='worker_assembly':
 source=source.replace('return await RunCourierOwnerPhaseAsync(generation, source + "_assemble", () =>','return await Task.Run(() =>').replace('                return assemble(input, prepared);\n            }, CancellationToken.None).ConfigureAwait(false);','                return assemble(input, prepared);\n            }).ConfigureAwait(false);')
schedule=historical_source('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs')
if args.mutate=='skip_knowledge_final_guard':
 needle='if (!IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)) return null;'
 start=schedule.rfind(needle)
 assert start>=0 and schedule.count(needle)==4
 schedule=schedule[:start]+'if (false) return null;'+schedule[start+len(needle):]
# J04f: the unsafe mutation moves the Courier preprocess retrieval (network/ONNX) onto an owner phase.
if args.mutate=='main_preprocess':
 old_run='CourierPreprocessRetrievalResult retrieved = await Task.Run(() =>';assert schedule.count(old_run)==1
 schedule=schedule.replace(old_run,'CourierPreprocessRetrievalResult retrieved = await RunCourierOwnerPhaseAsync(generation, source + "_unsafe_preprocess", () =>')
 nl='\r\n' if '\r\n' in schedule else '\n'
 tail='\t\t\t}).ConfigureAwait(false);'+nl+'\t\t\tpreprocessRuleHits'
 assert schedule.count(tail)==1;schedule=schedule.replace(tail,tail.replace('}).ConfigureAwait','}, CancellationToken.None).ConfigureAwait'),1)
if args.mutate=='skip_accept':source=source.replace('if (!IsCourierPromptInputCurrent(input))','if (false)')
if args.mutate=='wrong_direction':source=source.replace('inbound ? "[NPC主动写信意图] " + Seed : LetterText','inbound ? LetterText : "[NPC主动写信意图] " + Seed')
if args.mutate=='skip_source':source=source.replace('return string.Equals(input.Session.LetterText, input.LetterText, StringComparison.Ordinal)','return true || string.Equals(input.Session.LetterText, input.LetterText, StringComparison.Ordinal)')
old=subprocess.check_output(['git','show','77a3d234:CourierDeliveryBehavior.PromptPreparation.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
baseline_specs=[
 ('CourierReplyGenerationRequest','BuildCourierReplyGenerationRequestOnMainThread','BuildReplyRequestFromPreparedPrompt'),
 ('InboundLetterGenerationRequest','BuildInboundLetterGenerationRequestOnMainThread','BuildInboundRequestFromPreparedPrompt'),
]
methods='\n'.join(
 ex.declaration(old,'private '+typ+' '+wrapper+'(').replace(wrapper,wrapper+'Baseline',1).replace(final+'(input,',final+'Baseline(input,')
 + '\n' + ex.declaration(old,'private '+typ+' '+final+'(').replace(final,final+'Baseline',1)
 for typ,wrapper,final in baseline_specs
)
old_host=subprocess.check_output(['git','show','77a3d234:CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
message_markers=[
 'private static List<object> BuildCourierReplyMessages(',
 'private static List<object> BuildInboundNpcLetterMessages(',
 'private static object CreateCourierChatMessage(',
 'private static void AppendCourierRawUserSection(',
 'private static void AppendCourierUserSection(',
 'private static void AppendCourierPersistentMemoryRoleMessages(',
 'private static bool TryConvertCourierMemoryMessageToChatMessage(',
 'private static string BuildCourierMemoryMetadataPrefix(',
 'private static string StripCourierPromptScopeLabel(',
 'private static string StripCourierSpeakerPrefix(',
]
current_host=ex.courier_source(None).replace('\r\n','\n')
old_messages=[ex.declaration(old_host,marker) for marker in message_markers]
import main_assembly_projection as main_projection
projected = main_projection.projected_messages()
new_messages=[ex.declaration(projected,marker) for marker in message_markers]
seam='ConversationRoleClassificationOwner.IsViewerAssistant(message, npcName, null, -1, useStableIdentity: false)'
legacy='role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && IsCourierMemorySpeakerRecipient(speaker, npcName)'
assert sum(method.count(seam) for method in new_messages)==1, 'Courier role owner seam changed'
canonical_new=[method.replace(seam,legacy) for method in new_messages]
assert old_messages==canonical_new, 'Courier final message builders changed beyond the reviewed role-owner seam'
message_builders=main_projection.production_builders()
for method in ('BuildCourierReplyMessages','BuildInboundNpcLetterMessages'):
 message_builders=message_builders.replace('private static List<object> '+method+'(', 'private static List<object> '+method+'Production(',1)
reqs='\n'.join(ex.declaration(current_host,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
# Compare every historical prompt field, excluding only the new runtime-only source handle and
# delivery snapshot. They are validated by the lifecycle tests, not prompt JSON parity.
reqs=reqs.replace('public CourierPromptRun SourceRun;', '[System.Text.Json.Serialization.JsonIgnore] public CourierPromptRun SourceRun;').replace('public bool DeliveryAppliedAtCapture;', '[System.Text.Json.Serialization.JsonIgnore] public bool DeliveryAppliedAtCapture;')
generation=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs').read_text(encoding='utf-8-sig')
finalize=ex.declaration(generation,'private void FinalizeCourierReplyGenerationOnMainThread(')
if args.mutate=='preflight-implies-delivery':
 needle='session.ReplyGenerated = true;';assert finalize.count(needle)==1
 finalize=finalize.replace(needle,needle+' session.DeliveryApplied = true;',1)
campaign=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs').read_text(encoding='utf-8-sig')
lifetime_field=campaign[campaign.index('    private readonly Dictionary<CourierSession, ConversationRequestLifetime>'):campaign.index('    private ConversationRequestLifetime BeginCourierRequestLifetime(')]
lifetime_methods=lifetime_field+'\n'+'\n'.join(ex.declaration(campaign,sig) for sig in ['private ConversationRequestLifetime BeginCourierRequestLifetime(', 'private void RetireCourierRequestLifetime(', 'private void RetireCourierRequestLifetimes('])
history=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig')
owner=ex.declaration(history,'private bool IsCourierHistoryOwnerCurrent(')
historytype=ex.declaration(history,'private sealed class CourierPreparedHistory')
harness=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@OWNER_PHASE@@',phase).replace('@@BASELINE@@',methods).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historytype+'\n'+owner).replace('@@MESSAGE_BUILDERS@@',message_builders).replace('@@FINALIZE_REPLY@@',finalize).replace('@@LIFETIME@@',lifetime_methods)
if args.old_worker:harness='#define OLD_WORKER\n'+harness
if args.output_name and args.run_root: p.error('Use either --output-name or --run-root')
out=new_run_root(ROOT,'courier-prompt',args.run_root or (HERE/'.generated'/args.output_name if args.output_name else None))
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text(harness,encoding='utf-8')
(out/'Schedule.cs').write_text(schedule,encoding='utf-8')
project=util.project(out,'CourierPromptChecks',[out/'Prompt.cs',out/'Schedule.cs',out/'Program.cs',ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs',ROOT/'src/AF.Contracts/Internal/InteractionContracts.cs',ROOT/'src/AF.Contracts/Internal/LlmContracts.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs'],executable=True)
dotnet=os.environ.get('AF_DOTNET') or str(ROOT/'local/dotnet/8.0.425/dotnet.exe')
code,log=util.run_dotnet(dotnet,['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');(out/'outputs.json').write_text(json.dumps({'courierDll':str(out/'bin/Release/net8.0/CourierPromptChecks.dll')}),encoding='utf-8');print(log,end='');print('OUTPUTS='+str(out/'outputs.json'));raise SystemExit(code)
