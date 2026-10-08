from pathlib import Path
import sys,importlib.util,subprocess,argparse,json,hashlib
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);args=p.parse_args();out=args.run_root.resolve();out.relative_to(R);out.mkdir(parents=True,exist_ok=True)
main=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
for declaration in ['private static List<ConversationMessage> KeepUncompressedFactsAndRecentConversationMessages(', 'private static ConversationMessage BuildUncompressedMemoryConversationMessage(', 'private static bool IsDailyMemoryLineNpcSpeech(', 'private static bool IsDailyMemoryLinePlayerSpeech(', 'private static bool TryParseDailyMemorySceneShoutLine(', 'private static bool IsCurrentActiveMemorySessionLine(']:
 assert declaration not in main,declaration
for forwarding in ['=> ResolveCapturedMemoryLineSceneForPrompt(line);','=> CaptureAndBuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);','=> CaptureAndBuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);','=> ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(line);','=> ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker);','=> UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene);']:
 assert main.count(forwarding)==1,forwarding
golden=(H/'LegacyUncompressed.cs.txt').read_text(encoding='utf-8')
role=(R/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs').read_text(encoding='utf-8-sig')
assembly=(R/'src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig')
signatures=['List<ConversationMessage> KeepUncompressedFactsAndRecentConversationMessages(','ConversationMessage BuildUncompressedMemoryConversationMessage(','bool IsDailyMemoryLineNpcSpeech(','bool IsDailyMemoryLinePlayerSpeech(','bool TryParseDailyMemorySceneShoutLine(','bool IsCurrentActiveMemorySessionLine(','bool IsUnknownMemorySceneLabel(','int FindDialogueHistorySpeakerDelimiter(','bool IsLikelyPlayerHistorySpeaker(']
for sig in signatures:
 original=ex.declaration(golden,'private static '+sig).replace('private static','internal static',1)
 original=original.replace('int maxConversationMessages)','int maxConversationMessages, int minimum, int maximum)').replace('DuelSettings.DailyConversationHistoryLineLimitMin','minimum').replace('DuelSettings.DailyConversationHistoryLineLimitMax','maximum')
 original=original.replace('DailyMemoryLine line, string memoryName, int targetAgentIndex)','DailyMemoryLine line, string memoryName, int targetAgentIndex, int currentDay, string currentScene)').replace('MBMath.ClampInt(line.GameHour, 0, 23)','Math.Max(0, Math.Min(23, line.GameHour))').replace('ResolveMemoryLineSceneForPrompt(line)','ResolveMemoryLineSceneForPrompt(line, currentDay, currentScene)')
 original=original.replace('IsLikelyPlayerHistorySpeaker(speaker)','ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker)').replace('FindDialogueHistorySpeakerDelimiter(body)','ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(body)')
 target=role if sig.startswith('int FindDialogue') or sig.startswith('bool IsLikelyPlayer') else assembly
 assert target.count(original)==1,sig
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs').read_text(encoding='utf-8-sig')
history_path='src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs'
history=(R/history_path).read_text(encoding='utf-8-sig')
for call in ['=> _memoryHistoryCommit.BuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);','=> _memoryHistoryCommit.BuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);']:
 assert adapter.count(call)==1,call
capture=ex.declaration(history,'internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(')
assert 'UncompressedMemoryMessageAssemblyOwner.Assemble(snapshot, out int rawMessageCount)' in capture
assert 'CopyForSummary()' in capture
assert capture.count('SaveRuntimeGuard.IsCurrentGeneration(generation)')==2
capture_signatures=['internal List<ConversationMessage> BuildUncompressedMemoryRoleMessages(', 'internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(', 'internal static int GetCurrentSceneSessionIdForDailyMemorySuppression(', 'internal static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(']
actual_capture='\n'.join(ex.declaration(history,sig) for sig in capture_signatures)
assert 'MyBehavior' not in assembly and 'DuelSettings' not in assembly and 'TaleWorlds' not in assembly
day=ex.declaration(main,'internal class DialogueDay')
daily=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryDeveloperDailyEdit.cs').read_text(encoding='utf-8-sig')
consumer=ex.declaration(daily,'private static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(').replace('private static','internal static',1)
assert 'MemoryHistoryCommitBannerlordAdapter.BuildNativeConversationHistoryEntriesForDailyMemoryEdit(npc, lines)' in consumer
assert 'UncompressedMemoryMessageAssemblyOwner.BuildUncompressedMemoryConversationMessage(' in actual_capture
(out/'Program.cs').write_text((H/'UncompressedHarness.cs.txt').read_text(encoding='utf-8').replace('@@DAY@@',day).replace('@@DAILY_CONSUMER@@',consumer).replace('@@ACTUAL_HISTORY_CAPTURE@@',actual_capture).replace('@@ACTUAL_RECALL_SCENE@@',ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs').read_text(encoding='utf-8-sig'),'internal static string ResolveCapturedMemoryLineSceneForPrompt(')),encoding='utf-8')
(out/'Legacy.cs').write_text((H/'LegacyUncompressed.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs','src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/x)+'" />' for x in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
(out/'receipt.json').write_text(json.dumps({'exit_code':r.returncode,'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'test_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'main_sha256':hashlib.sha256((R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_bytes()).hexdigest(),'golden_sha256':hashlib.sha256((H/'LegacyUncompressed.cs.txt').read_bytes()).hexdigest(),'terminal_proof':'6 host algorithms absent; 6 sole forwarding consumers present; 9 pure bodies exact approved substitutions; owner receives only detached values','baseline':'fixed pre-move 9 declarations plus 3 exact pure leaves; complete actual History capture declarations and current My thin wrappers; scalar engine/config and memory-read boundary controlled','live':'NOT_RUN'},indent=2),encoding='utf-8')
sys.exit(r.returncode)
