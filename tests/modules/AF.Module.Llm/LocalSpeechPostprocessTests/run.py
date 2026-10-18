"""Exercise the actual production work-item and completion boundary with real CN/EN prompts.
Game-thread admission, gameplay normalizer and speech sanitizer are explicit test leaves.
No provider, game install or player settings are touched.
"""
from pathlib import Path
import argparse,importlib.util,json,subprocess,sys,hashlib
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path,required=True);p.add_argument('--language',choices=['CN','EN'],default='CN');a=p.parse_args()
out=new_run_root(ROOT,'speech-postprocess',a.run_root)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
source=(ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs').read_text(encoding='utf-8-sig')
work=ex.declaration(source,'internal sealed class SceneActionPostprocessWorkItem')
network=ex.declaration(source,'internal sealed class PostprocessNetworkRequest')
complete=ex.declaration(source,'internal static string CompleteSceneUnifiedActionPostprocess(')
(out/'Boundary.cs').write_text('using System;using System.Threading; namespace AnimusForge;\n'+network+'\n'+work+'\ninternal static class ConversationActionPostprocessOwner { internal static void RequireMainThread(){}\n'+complete+'\n}',encoding='utf-8')
for name in ['GptSoVitsLanguage','LocalTtsSpeech','LocalTtsToneTag']:
 (out/(name+'.cs')).write_bytes((ROOT/('src/modules/AF.Module.Llm/Tts/'+name+'.cs')).read_bytes())
(out/'ConversationRuntimeTexts.cs').write_bytes((ROOT/'src/modules/AF.Module.Conversation/Internal/ConversationRuntimeTexts.cs').read_bytes())
(out/'ConversationRuntimePrompts.json').write_bytes((ROOT/('content/modules/AF.Module.Conversation/ModuleData/ConversationRuntimePrompts'+('.en.json' if a.language=='EN' else '.json'))).read_bytes())
(out/'Program.cs').write_text(r'''using System;using AnimusForge;
class Program {
 static int count;static void Check(bool x,string why){if(!x)throw new Exception(why);count++;}
 static void Main(string[] args){AnimusForgeModulePaths.Root=args[0];
 var language=GptSoVitsLanguage.Parse("日语");string actions="[ACTION:MOOD:ANNOYED][ACTION:GIVE_GOLD:4]";
 string hidden="[AF_TTS_SPEECH]{\"language\":\"ja\",\"mode\":\"translate\",\"text\":\"こんにちは。\"}[/AF_TTS_SPEECH]";
 string seen=null;
 var w=new SceneActionPostprocessWorkItem("gameplay system","gameplay user","原始正文",raw=>{seen=raw;return "原始正文"+raw;},language);
 Check(w.NetworkRequest.SystemPrompt.StartsWith("gameplay system")&&w.NetworkRequest.SystemPrompt.Contains("AF_TTS_SPEECH")&&!w.NetworkRequest.SystemPrompt.Contains("{LANGUAGE}"),"real resource appended without replacing actions");
 Check(w.NetworkRequest.UserPrompt.StartsWith("gameplay user")&&w.NetworkRequest.UserPrompt.Contains("原始正文"),"source separate from original context");
 var final=ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(w,true,actions+hidden,null);
 Check(seen==actions&&final=="原始正文"+actions,"actual normalizer sees original action tags only");
 Check(w.SpeechResult.Text=="こんにちは。"&&w.SpeechResult.Tone==LocalTtsTone.Angry&&w.SpeechResult.Language==language,"speech and normalized mood share a per-turn result");
 try{ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(w,true,actions,null);Check(false,"duplicate completion");}catch(InvalidOperationException){Check(true,"completion is once");}
 var same=new SceneActionPostprocessWorkItem("s","u","原文不能改写",x=>x,GptSoVitsLanguage.Parse("中文"));
 ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(same,true,"[AF_TTS_SPEECH]{\"language\":\"zh\",\"mode\":\"keep\"}[/AF_TTS_SPEECH]",null);
 Check(same.SpeechResult.Text=="原文不能改写"&&!same.SpeechResult.Translated,"matching-language keep");
 foreach(bool force in new[]{false,true}){
 var bad=new SceneActionPostprocessWorkItem("s","u","body",raw=>{seen=raw;return raw;},force?language:null);
 ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(bad,true,actions+"[AF_TTS_SPEECH]{\"language\":\"ja\",\"mode\":\"translate\",\"text\":\"[ACTION:DUEL]\"}[/AF_TTS_SPEECH]",null);
 Check(seen==actions&&bad.SpeechResult.Text==null,"authorized or unsolicited sidecar cannot inject actions");
 if(!force)Check(bad.SystemPrompt=="s"&&bad.UserPrompt=="u","cloud/courier/auto prompts unchanged");
 }
 var failed=new SceneActionPostprocessWorkItem("s","u","body",x=>throw new Exception("must not normalize failure"),language);
 Check(ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(failed,false,null,"fixture")=="body\n[ACTION:MOOD:NEUTRAL]"&&failed.SpeechResult.Text==null,"network failure preserves body and has no speech authority");
 var immediate=new SceneActionPostprocessWorkItem("body");Check(ConversationActionPostprocessOwner.CompleteSceneUnifiedActionPostprocess(immediate,true,null,null)=="body"&&immediate.SpeechResult.Text==null,"postprocess disabled does not invent translation");
 Console.WriteLine("PASS "+count+" actual work-item/completion assertions; no real provider or game.");
 }
}
namespace AnimusForge {
 static class AnimusForgeModulePaths {internal static string Root; internal static string GetModuleDataFilePath(string p)=>System.IO.Path.Combine(Root,p);}
 static class ConversationSpeechTextRules {internal static string SanitizeSceneSpeechTextForTts(string text)=>text;}
 static class AIConfigHandler {internal static string ActionPostprocessFallbackMoodTag=>"[ACTION:MOOD:NEUTRAL]";}
 static class Logger {internal static void Log(string owner,string text){}}
}
''',encoding='utf-8')
from xml.sax.saxutils import escape
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(resolve_dotnet(ROOT).parent/'sdk/8.0.426/Newtonsoft.Json.dll'))+'</HintPath></Reference></ItemGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'--',str(out)],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);raise SystemExit(r.returncode)
