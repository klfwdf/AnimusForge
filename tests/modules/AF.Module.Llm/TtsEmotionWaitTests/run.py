"""Actual ProcessJob waiting/text/tone behavior; HTTP, game and audio leaves are replaced."""
from pathlib import Path
import argparse, importlib.util, subprocess, sys
ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path, required=True)
a = p.parse_args()
out = new_run_root(ROOT, 'tts-emotion-wait', a.run_root)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
source = (ROOT / 'src/modules/AF.Module.Llm/Tts/TtsEngine.cs').read_text(encoding='utf-8-sig')
method = ex.declaration(source, 'private void ProcessJob(')
for name in ['LocalTtsSpeech.cs', 'LocalTtsToneTag.cs', 'GptSoVitsLanguage.cs']:
    (out / name).write_bytes((ROOT / 'src/modules/AF.Module.Llm/Tts' / name).read_bytes())
(out / 'Program.cs').write_text('''
using System;using System.Threading;using System.Threading.Tasks;using AnimusForge;
namespace AnimusForge {
static class LlmSourceTexts { public static string Get(string key)=>throw new Exception("Unexpected legacy lookup"); }
class Request { public CancellationToken CancellationToken; public int SegmentCount; }
class Settings { public bool WaitForPostprocessEmotion=true, OpenAi=true; public GptSoVitsLanguage SpeechLanguage=GptSoVitsLanguage.Auto; }
class LocalRequest { public GptSoVitsLanguage Language=GptSoVitsLanguage.Auto; public LocalTtsTone Tone;
 public LocalRequest WithTone(LocalTtsTone t)=>new LocalRequest{Language=Language,Tone=t}; public LocalRequest WithText(string s)=>this; }
class TtsJob { public LocalRequest LocalRequest;public Settings Settings=new Settings(); public Request Request=new Request();
 public bool BypassEnabledCheck,KeepOriginalSubtitle,TerminalPublished;public LocalTtsSpeech LocalSpeech=LocalTtsSpeech.Neutral;
 public Task<LocalTtsSpeech> PendingLocalSpeech;public string Text="Original reply";public string[] Segments;public int SegmentIndex;public LocalTtsTone Tone; }
static class TtsSentenceChunker { public static string[] Split(string s)=>new[]{s}; }
class Engine {
 public int Calls;public string Spoken,Failure;public LocalTtsTone Tone;
 void NotifyPlaybackFailed(TtsJob j,string e){Failure=e;}
 bool IsJobCurrent(TtsJob j)=>!j.Request.CancellationToken.IsCancellationRequested;
 void ProcessSentence(TtsJob j,string s,LocalRequest r){Calls++;Spoken=s;Tone=r?.Tone??j.Tone;}
 public void Run(TtsJob j)=>ProcessJob(j);
''' + method + '''
}
}
class Program {
 static int checks;static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static TaskCompletionSource<LocalTtsSpeech> Pending()=>new TaskCompletionSource<LocalTtsSpeech>(TaskCreationOptions.RunContinuationsAsynchronously);
 static void Main(){
 foreach(bool local in new[]{false,true})foreach(bool forced in new[]{false,true}) {
  var pending=Pending();var e=new Engine();var j=new TtsJob();
  j.Settings.WaitForPostprocessEmotion=false;j.Settings.SpeechLanguage=forced?GptSoVitsLanguage.Parse("en"):GptSoVitsLanguage.Auto;
  if(local)j.LocalRequest=new LocalRequest{Language=j.Settings.SpeechLanguage};
  j.PendingLocalSpeech=pending.Task;
  j.LocalSpeech=new LocalTtsSpeech(LocalTtsTone.Angry,GptSoVitsLanguage.Parse("en"),"Translated reply",true);
  var run=Task.Run(()=>e.Run(j));
  Check(run.Wait(1000),"Direct mode must not await pending postprocess: local="+local+" forced="+forced);
  Check(!pending.Task.IsCompleted&&e.Calls==1&&e.Spoken==j.Text&&e.Tone==LocalTtsTone.Neutral&&e.Failure==null,"Direct original/neutral, including forced language");
  pending.SetResult(j.LocalSpeech);Check(e.Calls==1,"Late postprocess must not replay direct speech");
 }
 {
  var pending=Pending();var e=new Engine();var j=new TtsJob{PendingLocalSpeech=pending.Task};
  var started=new ManualResetEventSlim();var run=Task.Run(()=>{started.Set();e.Run(j);});
  Check(started.Wait(1000)&&!run.Wait(100),"Enabled mode waits for pending emotion");
  pending.SetResult(new LocalTtsSpeech(LocalTtsTone.Happy));Check(run.Wait(1000),"Enabled mode resumes");
  Check(e.Calls==1&&e.Spoken==j.Text&&e.Tone==LocalTtsTone.Happy,"Enabled mode consumes actual mood");
 }
 {
  var e=new Engine();var j=new TtsJob();j.Settings.SpeechLanguage=GptSoVitsLanguage.Parse("en");
  j.PendingLocalSpeech=Task.FromResult(new LocalTtsSpeech(LocalTtsTone.Happy,GptSoVitsLanguage.Parse("en"),"Translated reply",true));
  e.Run(j);Check(e.Calls==1&&e.Spoken=="Translated reply"&&j.KeepOriginalSubtitle,"Enabled forced translation preserved");
 }
 {
  var e=new Engine();var j=new TtsJob();j.Settings.SpeechLanguage=GptSoVitsLanguage.Parse("en");e.Run(j);
  Check(e.Calls==0&&e.Failure=="tts_local_translation_missing","Enabled missing translation retains original failure");
 }
 Console.WriteLine("PASS "+checks+" production ProcessJob assertions; no HTTP/game/audio.");
 }
}
''', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>$(MSBuildToolsPath)/Newtonsoft.Json.dll</HintPath></Reference></ItemGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet = resolve_dotnet(ROOT)
r = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=ROOT, env=minimal_test_environment(dotnet,out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
(out / 'run.log').write_text(r.stdout+r.stderr, encoding='utf-8');print(r.stdout+r.stderr);raise SystemExit(r.returncode)
