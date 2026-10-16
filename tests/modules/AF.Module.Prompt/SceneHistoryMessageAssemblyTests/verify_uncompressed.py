from pathlib import Path
import sys,importlib.util,subprocess,argparse,json,hashlib,shutil
from xml.sax.saxutils import escape
R=Path(__file__).resolve().parents[4];H=Path(__file__).parent
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment, resolve_dotnet, new_run_root
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--dotnet');p.add_argument('--target-framework',default='net8.0');args=p.parse_args();out=new_run_root(R,'uncompressed-history',args.run_root);d=resolve_dotnet(R,args.dotnet)
main=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
for declaration in ['private static List<ConversationMessage> KeepUncompressedFactsAndRecentConversationMessages(', 'private static ConversationMessage BuildUncompressedMemoryConversationMessage(', 'private static bool IsDailyMemoryLineNpcSpeech(', 'private static bool IsDailyMemoryLinePlayerSpeech(', 'private static bool TryParseDailyMemorySceneShoutLine(', 'private static bool IsCurrentActiveMemorySessionLine(']:
 assert declaration not in main,declaration
for forwarding in ['=> ResolveCapturedMemoryLineSceneForPrompt(line);','=> CaptureAndBuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);','=> CaptureAndBuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);','=> ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(line);','=> ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker);','=> UncompressedMemoryMessageAssemblyOwner.IsUnknownMemorySceneLabel(scene);']:
 assert main.count(forwarding)==1,forwarding
golden=(H/'LegacyUncompressed.cs.txt').read_text(encoding='utf-8')
role=(R/'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs').read_text(encoding='utf-8-sig')
assembly=(R/'src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig')
signatures=['List<ConversationMessage> KeepUncompressedFactsAndRecentConversationMessages(','ConversationMessage BuildUncompressedMemoryConversationMessage(','bool IsDailyMemoryLineNpcSpeech(','bool IsDailyMemoryLinePlayerSpeech(','bool TryParseDailyMemorySceneShoutLine(','bool IsCurrentActiveMemorySessionLine(','bool IsUnknownMemorySceneLabel(','int FindDialogueHistorySpeakerDelimiter(','bool IsLikelyPlayerHistorySpeaker(']
bilingual_extensions = {
 'ConversationMessage BuildUncompressedMemoryConversationMessage(',
 'bool IsDailyMemoryLinePlayerSpeech(',
 'bool IsDailyMemoryLineNpcSpeech(',
 'bool TryParseDailyMemorySceneShoutLine(',
 'bool IsUnknownMemorySceneLabel(',
 'bool IsLikelyPlayerHistorySpeaker(',
}
for sig in signatures:
 original=ex.declaration(golden,'private static '+sig).replace('private static','internal static',1)
 original=original.replace('int maxConversationMessages)','int maxConversationMessages, int minimum, int maximum)').replace('DuelSettings.DailyConversationHistoryLineLimitMin','minimum').replace('DuelSettings.DailyConversationHistoryLineLimitMax','maximum')
 original=original.replace('DailyMemoryLine line, string memoryName, int targetAgentIndex)','DailyMemoryLine line, string memoryName, int targetAgentIndex, int currentDay, string currentScene)').replace('MBMath.ClampInt(line.GameHour, 0, 23)','Math.Max(0, Math.Min(23, line.GameHour))').replace('ResolveMemoryLineSceneForPrompt(line)','ResolveMemoryLineSceneForPrompt(line, currentDay, currentScene)')
 original=original.replace('IsLikelyPlayerHistorySpeaker(speaker)','ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker(speaker)').replace('FindDialogueHistorySpeakerDelimiter(body)','ConversationRoleClassificationOwner.FindDialogueHistorySpeakerDelimiter(body)')
 target=role if sig.startswith(('int FindDialogue','bool IsLikelyPlayer','bool IsDailyMemoryLine','bool TryParseDailyMemorySceneShout')) else assembly
 # Public role/text behavior is unchanged; prompt-only session provenance is an approved additive scalar.
 comparison=target.replace('PromptMemorySessionKey = line.MemorySessionKey ?? "",\n            ','')
 # These four parsers intentionally accept English input now. The executable
 # harness still compares legacy Chinese fixtures and exercises the new cases.
 if sig not in bilingual_extensions:
  assert comparison.count(original)==1,sig
 else:
  assert ex.declaration(target,'internal static '+sig),sig
assert 'PromptMemorySessionKey = line.MemorySessionKey ?? ""' in assembly
assert 'ProjectDailyMemorySpeech(' in assembly
assert 'TryStripAddressedSpeech(content, speaker, target, out content)' in (R/'src/modules/AF.Module.Prompt/Composition/NativeMemoryHistoryMergeOwner.cs').read_text('utf-8-sig')
adapter=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs').read_text(encoding='utf-8-sig')
history_path='src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs'
history=(R/history_path).read_text(encoding='utf-8-sig')
for call in ['=> _memoryHistoryCommit.BuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);','=> _memoryHistoryCommit.BuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);']:
 assert adapter.count(call)==1,call
capture=ex.declaration(history,'internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(')
assert 'UncompressedMemoryMessageAssemblyOwner.Assemble(snapshot, out int rawMessageCount)' in capture
assert 'CopyForSummary()' in capture
assert capture.count('SaveRuntimeGuard.IsCurrentGeneration(generation)')==2
assert 'PlayerName = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(MemoryEntityIdentityBannerlordAdapter.FindHeroById(normalizedMemoryId))' in capture
capture_signatures=['internal List<ConversationMessage> BuildUncompressedMemoryRoleMessages(', 'internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById(', 'internal static int GetCurrentSceneSessionIdForDailyMemorySuppression(', 'internal static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(']
actual_capture='\n'.join(ex.declaration(history,sig) for sig in capture_signatures)
assert 'MyBehavior' not in assembly and 'DuelSettings' not in assembly and 'TaleWorlds' not in assembly
day=ex.declaration(main,'internal class DialogueDay')
daily=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryDeveloperDailyEdit.cs').read_text(encoding='utf-8-sig')
consumer=ex.declaration(daily,'private static List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEntriesForDailyMemoryEdit(').replace('private static','internal static',1)
assert 'MemoryHistoryCommitBannerlordAdapter.BuildNativeConversationHistoryEntriesForDailyMemoryEdit(npc, lines)' in consumer
assert 'UncompressedMemoryMessageAssemblyOwner.BuildUncompressedMemoryConversationMessage(' in actual_capture
assert 'currentDay, currentScene, playerName)' in actual_capture
actual_producer=ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig'),'internal static string BuildPlayerAddressedInputForName(')
(out/'Producer.cs').write_text('using TaleWorlds.CampaignSystem;namespace AnimusForge.Refactor.Adapters {internal static class PersonaIdentityPromptCaptureAdapter {internal static string BuildPlayerPublicDisplayNameForPrompt(Hero observer)=>AnimusForge.FixtureState.PlayerName;'+actual_producer+'}}',encoding='utf-8')
(out/'Program.cs').write_text((H/'UncompressedHarness.cs.txt').read_text(encoding='utf-8').replace('@@DAY@@',day).replace('@@DAILY_CONSUMER@@',consumer).replace('@@ACTUAL_HISTORY_CAPTURE@@',actual_capture).replace('@@ACTUAL_RECALL_SCENE@@',ex.declaration((R/'src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs').read_text(encoding='utf-8-sig'),'internal static string ResolveCapturedMemoryLineSceneForPrompt(')),encoding='utf-8')
(out/'Legacy.cs').write_text((H/'LegacyUncompressed.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs','src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs','src/modules/AF.Module.Memory/Runtime/MemoryTextResources.cs']
paths.extend(['src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs','src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs','src/modules/AF.Module.Prompt/Composition/NativeMemoryHistoryMergeOwner.cs'])
source_resource=R/'content/modules/AF.Module.Memory/ModuleData/MemorySourceTexts.json'
major=args.target_framework.removeprefix('net').split('.')[0]
newtonsoft=sorted((d.parent/'sdk').glob(major+'.*/Newtonsoft.Json.dll'))[-1]
assert newtonsoft.is_file() and source_resource.is_file()
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>'+escape(args.target_framework)+'</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(R/x))+'" />' for x in paths)+'<EmbeddedResource Include="'+escape(str(source_resource))+'"><LogicalName>AnimusForge.Memory.SourceTexts.json</LogicalName></EmbeddedResource><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
f=out/'Tests.csproj';value=f.read_text('utf-8');value=value.replace('</Project>','<ItemGroup><EmbeddedResource Include="'+escape(str(R/'content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json'))+'"><LogicalName>AnimusForge.Prompt.Compatibility.json</LogicalName></EmbeddedResource></ItemGroup></Project>');f.write_text(value,encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')

env=minimal_test_environment(d,out)
build=subprocess.run([str(d),'build',str(out/'Tests.csproj'),'-c','Release','--nologo'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
if build.returncode:
 print(build.stdout+build.stderr,end='');sys.exit(build.returncode)
package_results=[]
for language, filename in [('CN','MemoryPromptTexts.json'),('EN','MemoryPromptTexts.en.json')]:
 package=out/language;package.mkdir(exist_ok=True)
 prompt_source=R/'content/modules/AF.Module.Memory/ModuleData'/filename
 shutil.copyfile(prompt_source,package/'MemoryPromptTexts.json')
 shutil.copyfile(R/('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.'+('en' if language=='EN' else 'zh-CN')+'.json'),package/'AdapterPromptCapture.json')
 shutil.copyfile(R/('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.'+('en' if language=='EN' else 'zh-CN')+'.json'),package/'PromptComposerTexts.json')
 run=subprocess.run([str(d),str(out/('bin/Release/'+args.target_framework+'/Tests.dll')),str(package),language],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
 (out/(language+'.log')).write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr,end='')
 package_results.append({'language':language,'exit_code':run.returncode,'prompt_sha256':hashlib.sha256(prompt_source.read_bytes()).hexdigest()})
 if run.returncode:break
r=run
(out/'run.log').write_text('\n'.join((out/(item['language']+'.log')).read_text(encoding='utf-8') for item in package_results),encoding='utf-8')
(out/'receipt.json').write_text(json.dumps({'exit_code':r.returncode,'packages':package_results,'source_resource_sha256':hashlib.sha256(source_resource.read_bytes()).hexdigest(),'sources':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in paths},'test_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'main_sha256':hashlib.sha256((R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_bytes()).hexdigest(),'golden_sha256':hashlib.sha256((H/'LegacyUncompressed.cs.txt').read_bytes()).hexdigest(),'terminal_proof':'6 host algorithms absent; 6 sole forwarding consumers present; unchanged algorithms compared with legacy bodies; role/parser changes tested through captured producer, assembly, editor and Native merge; detached values only','baseline':'fixed pre-move 9 declarations plus 3 exact pure leaves; complete actual History capture declarations and current My thin wrappers; scalar engine/config and memory-read boundary controlled','capture_sha256':hashlib.sha256(history.encode()).hexdigest(),'producer_sha256':hashlib.sha256(actual_producer.encode()).hexdigest(),'packages_sha256':{x:hashlib.sha256((R/x).read_bytes()).hexdigest() for x in ['content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.zh-CN.json','content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.en.json']},'excluded':'AFEF merge branches throw in this role/user-overlap fixture; engine identity and scene queries are controlled leaves','live':'NOT_RUN'},indent=2),encoding='utf-8')
sys.exit(r.returncode)
