"""Run the real Native transient state owner; no game or persistent storage objects."""
from pathlib import Path
import argparse,sys,subprocess,importlib.util
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--dotnet');p.add_argument('--mutate', choices=['fact-role','drop-distance','ignore-limit','ignore-preserve','history-stage-loss','drop-clear']);a=p.parse_args()
out=new_run_root(ROOT,'native-session-owner',a.run_root);dotnet=resolve_dotnet(ROOT,a.dotnet)
spec=importlib.util.spec_from_file_location('native_text_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
utils=ex.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs',None)
helpers='using System; using System.Text; using System.Text.RegularExpressions; namespace AnimusForge; internal static class ShoutUtils { '+ '\n'.join(ex.declaration(utils,s) for s in ['public static bool TrySplitNamePrefixedLineSafely(', 'public static string StripNamePrefixedLineSafely(', 'public static string StripConversationMetadataPrefix('])+' }\ninternal static class ConversationActionPostprocessOwner { '+ex.declaration(ex.source('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs',None),'internal static string StripActionTagsForSceneSpeech(')+' }'

legacy=ex.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs','c3a6c24434c5695b6d437da508d6aab99dfe3b2f')
names=['IsNpcInventorySummaryHeader','IsLeakedPromptLineForShout','StripLeakedPromptFragmentsForShout','StripLeakedPromptContentForShout','StripStageDirectionsForPassiveShout','SanitizeSceneSpeechText','PrepareSceneHistorySpeechText','StripAutoGroupStopSignal','StripAutoGroupRelaySignal','StripAfefPromptScopeLabel','NormalizeNativeConversationFactLineForPrompt','NormalizeNativeConversationVisibleTextKey','NormalizeNativeConversationHistoryTextForPostprocess']
import re
oracle=[]
for name in names:
 m=re.search(r'(?:private|internal) static [^\n]+?\b'+name+r'\(',legacy)
 block=ex.declaration(legacy,m.group()).replace('private static','internal static',1).replace('StripNpcNamePrefixSafely(', 'ShoutUtils.StripNamePrefixedLineSafely(').replace('StripActionTagsForSceneSpeech(', 'ConversationActionPostprocessOwner.StripActionTagsForSceneSpeech(')
 oracle.append(block)
helpers+='\ninternal static class LegacySpeechTextOracle { internal static bool Detailed; internal static bool PreserveAsterisk; private static bool IsDetailedSceneSpeechPromptEnabled()=>Detailed; private static bool ShouldPreserveSceneAsteriskActions()=>PreserveAsterisk; '+'\n'.join(oracle)+' }'
(out/'TextDependencies.cs').write_text(helpers,encoding='utf-8')
paths=[Path(__file__).with_name('Program.cs'),ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs',ROOT/'src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs',ROOT/'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs', ROOT/'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs', ROOT/'src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs', ROOT/'src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs', out/'TextDependencies.cs']
if a.mutate:
 target=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs'
 rules=ROOT/'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs'
 mutations={
  'fact-role':(target,'Role = "system"','Role = "assistant"'),
  'drop-distance':(target,'? distance : -1f','? -1f : -1f'),
  'ignore-limit':(target,'requested > 0 ? Math.Max(1, Math.Min(maximum, requested)) : configured','configured'),
  'ignore-preserve':(rules,'if (!options.PreserveAsterisk)','if (true)'),
  'history-stage-loss':(rules,'string text2 = StripLeakedPromptContentForShout(text);','string text2 = StripStageDirectionsForPassiveShout(StripLeakedPromptContentForShout(text), default);'),
  'drop-clear':(target,'_history.Remove(key); _recordedDialog.Remove(key); return;','_recordedDialog.Remove(key); return;'),
 }
 target,before,after=mutations[a.mutate];text=target.read_text(encoding='utf-8-sig');assert text.count(before)==1,'Mutation anchor not unique'
 candidate=out/target.name;candidate.write_text(text.replace(before,after,1),encoding='utf-8');paths[paths.index(target)]=candidate
newtonsoft=dotnet.parent/'sdk/8.0.425/Newtonsoft.Json.dll'
assert newtonsoft.is_file(), 'Missing existing Newtonsoft.Json.dll'
items='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference>'+''.join('<Compile Include="'+escape(str(x))+'" Link="'+x.name+'" />' for x in paths)
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);print('OUTPUT',out);raise SystemExit(r.returncode)
