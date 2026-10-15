"""Real tone/normalizer/history/TTS/Courier text code and extracted streaming/action-strip methods.
Game name/metadata leaves are identity substitutes; no live game, provider or paid call.
"""
from pathlib import Path
import argparse,importlib.util,json,subprocess,sys
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);a=p.parse_args()
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=new_run_root(ROOT,'tts-tone-presentation',a.run_root)
def read(p):return (ROOT/p).read_text(encoding='utf-8-sig')
def method(p,sig):return ex.declaration(read(p),sig)
for rel in ['src/modules/AF.Module.Llm/Tts/LocalTtsToneTag.cs','src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierVisibleLetterSanitizer.cs']:(out/Path(rel).name).write_text(read(rel),encoding='utf-8')
stream=method('src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs','internal static string BuildNativeConversationStreamingVisibleText(')
prepare=method('src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs','internal static string PrepareSceneMainReplySpeechText(')
action=method('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs','internal static string StripActionTagsForSceneSpeech(')
(out/'Leaves.cs').write_text('''using System;using System.Text.RegularExpressions;using System.Collections.Generic;using System.IO;using Newtonsoft.Json;
namespace AnimusForge {
static class ShoutUtils { internal static string StripConversationMetadataPrefix(string s)=>s; internal static string StripNamePrefixedLineSafely(string s,int n)=>s; }
static class GiveAssetTagCodec { internal static string StripTags(string s)=>s; }
static class ConversationRuntimeTexts { static readonly Dictionary<string,string> data=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText("content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimeCompatibility.json"));internal static string Raw(string key)=>data[key]; }
static class PromptComposerTexts { static readonly Dictionary<string,string> data=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText("content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.zh-CN.json"));internal static string Get(string key)=>data[key]; }
static class ConversationActionPostprocessOwner { '''+action+''' }
static class ScenePromptMessageProjectionComposer { '''+stream+'\n'+prepare+''' }
}
''',encoding='utf-8')
(out/'Program.cs').write_text('''using AnimusForge;
class Program {
 static int count; static void Check(bool x,string why){if(!x)throw new Exception(why);count++;}
 static int Main(){try {Run();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static void Run(){
 var options=new ConversationSpeechTextOptions(false,false);
 foreach(var name in new[]{"neutral","happy","angry","sad"}) {
  var tag="[TTS_TONE:"+name+"]";var raw="你好，旅行者。"+tag+"[ACTION:MOOD:ANGRY][AD:1][GUI:2][END]";
  Check(ConversationSpeechTextRules.SanitizeSceneSpeechText(raw,options)=="你好，旅行者。","visible sanitization");
  Check(ConversationSpeechTextRules.PrepareSceneHistorySpeechText(raw)=="你好，旅行者。","history sanitization");
  Check(!ConversationSpeechTextRules.SanitizeSceneSpeechTextForTts(raw).Contains("TTS_TONE"),"speech sanitization");
  Check(CourierVisibleLetterSanitizer.Clean("信件正文"+tag)=="信件正文","courier does not show accidental tone");
  string queued=ScenePromptMessageProjectionComposer.PrepareSceneMainReplySpeechText(raw,false,false);
  string clean=LocalTtsToneTag.Extract(queued,out var selected);
  Check(queued.Contains(tag) && (int)selected==Array.IndexOf(new[]{"neutral","happy","angry","sad"},name),"main scene projection preserves until enqueue capture");
  Check(!clean.Contains("TTS_TONE") && !clean.Contains("ACTION"),"no metadata or main-action authorization reaches effects");
  for(int i=0;i<=tag.Length;i++)Check(ScenePromptMessageProjectionComposer.BuildNativeConversationStreamingVisibleText("你好。"+tag[..i],options)=="你好。","streaming metadata flash");
 }
 var foreign="[ACTION:MOOD:HAPPY][MOOD:ANGRY][AD:3][GUI:4][RELAY:2][END]";
 Check(LocalTtsToneTag.Extract(foreign,out var t)==foreign&&t==LocalTtsTone.Neutral,"foreign gameplay tags do not drive speech tone");
 Console.WriteLine($"PASS {count} real text-boundary assertions; game/name leaves substituted.");
 }
}''',encoding='utf-8')
# Actual runtime injection and typed propagation edges; guards against accidentally moving into action/Volc ownership.
source_checks={
 'local-prompt-only':('src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs',['speechSettings?.EnableTtsSpeech == true && speechSettings.TtsUseGptSoVits && !GptSoVitsVoiceRuntime.IsBattleBlocked','ConversationRuntimeTexts.Prompt("local_tts_tone_metadata")']),
 'native-capture-before-actions':('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs',['LocalTtsToneTag.Extract(nativeMainReply.PostprocessReply, out localTtsTone)','nativeMainVisibleForTts, localTtsTone)']),
 'scene-capture-before-effects':('src/AF.GameAdapter.Bannerlord/Scene/SceneSpeechEnqueueAdapter.cs',['LocalTtsToneTag.Extract(content, out var localTtsTone)','LocalTtsTone = localTtsTone']),
 'native-completion':('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeCompletion.cs',['visible, scope.Request.LocalTtsTone)']),
 'scene-completion':('src/modules/AF.Module.Conversation/Channels/Scene/SceneSpeechEffectController.cs',['suppressInteractionTimeoutArm, item.LocalTtsTone)'])}
for key,(file,needles) in source_checks.items():assert all(x in read(file) for x in needles),key
for name in ['LegacyVolcTtsGateway','VolcV3TtsGateway']:
 assert 'LocalTtsTone' not in read('src/modules/AF.Module.Llm/Tts/'+name+'.cs')
for suffix in ['', '.en']:
 prompts=json.loads(read('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimePrompts'+suffix+'.json'))
 assert all('[TTS_TONE:'+name+']' in prompts['local_tts_tone_metadata'] for name in ['neutral','happy','angry','sad'])
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+str(resolve_dotnet(ROOT).parent/'sdk/8.0.426/Newtonsoft.Json.dll')+'</HintPath></Reference></ItemGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);raise SystemExit(r.returncode)
