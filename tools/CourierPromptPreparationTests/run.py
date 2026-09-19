from pathlib import Path
import argparse, importlib.util, os, subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def load(name,path):
 spec=importlib.util.spec_from_file_location(name,path);result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result
ex=load('decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
util=load('util',ROOT/'tools/ModuleFrameworkApiTests/run.py')
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=['worker_assembly','main_preprocess','skip_accept','wrong_direction','skip_source','skip_knowledge_final_guard','drop-knowledge-text','drop-entity-text','drop-rule-text']);p.add_argument('--old-worker',action='store_true');args=p.parse_args()
source=(ROOT/'CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig')
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
phase=ex.declaration((ROOT/'CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(').replace('Task.Delay(30000)','Task.Delay(180)')
if args.mutate=='worker_assembly':
 source=source.replace('return await RunCourierOwnerPhaseAsync(generation, source + "_assemble", () =>','return await Task.Run(() =>').replace('                return assemble(input, prepared);\n            }, CancellationToken.None).ConfigureAwait(false);','                return assemble(input, prepared);\n            }).ConfigureAwait(false);')
schedule=(ROOT/'CourierDeliveryBehavior.PromptSchedule.cs').read_text(encoding='utf-8-sig')
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
 'private static bool IsCourierMemorySpeakerRecipient(',
 'private static string BuildCourierMemoryMetadataPrefix(',
 'private static string StripCourierPromptScopeLabel(',
 'private static string StripCourierSpeakerPrefix(',
]
current_host=(ROOT/'CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig').replace('\r\n','\n')
old_messages=[ex.declaration(old_host,marker) for marker in message_markers]
new_messages=[ex.declaration(current_host,marker) for marker in message_markers]
assert old_messages==new_messages, 'Courier final message builders changed since 77a3d234; extract both independently before comparing'
message_builders='\n'.join(new_messages)
for method in ('BuildCourierReplyMessages','BuildInboundNpcLetterMessages'):
 message_builders=message_builders.replace('private static List<object> '+method+'(', 'private static List<object> '+method+'Production(',1)
reqs='\n'.join(ex.declaration(old_host,'private sealed class '+name) for name in ['CourierReplyGenerationRequest','InboundLetterGenerationRequest'])
history=(ROOT/'CourierDeliveryBehavior.HistoryPreparation.cs').read_text(encoding='utf-8-sig')
owner=ex.declaration(history,'private bool IsCourierHistoryOwnerCurrent(')
historytype=ex.declaration(history,'private sealed class CourierPreparedHistory')
harness=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@OWNER_PHASE@@',phase).replace('@@BASELINE@@',methods).replace('@@REQUESTS@@',reqs).replace('@@HISTORY@@',historytype+'\n'+owner).replace('@@MESSAGE_BUILDERS@@',message_builders)
if args.old_worker:harness='#define OLD_WORKER\n'+harness
out=HERE/'.generated'/('old-worker' if args.old_worker else args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
(out/'Prompt.cs').write_text(source,encoding='utf-8');(out/'Program.cs').write_text(harness,encoding='utf-8')
(out/'Schedule.cs').write_text(schedule,encoding='utf-8')
project=util.project(out,'CourierPromptChecks',[out/'Prompt.cs',out/'Schedule.cs',out/'Program.cs',ROOT/'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',ROOT/'src/modules/AF.Module.Prompt/Composition/PromptExtrasComposer.cs'],executable=True)
dotnet=os.environ.get('AF_DOTNET') or str(ROOT/'local/dotnet/8.0.425/dotnet.exe')
code,log=util.run_dotnet(dotnet,['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
