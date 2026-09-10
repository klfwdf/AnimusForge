import importlib.util,subprocess,os
from pathlib import Path
root=Path(__file__).resolve().parents[2];here=Path(__file__).parent;out=here/'.generated/presentation-original';out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('extractor',root/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
s=subprocess.check_output(['git','show','36e04059:AnimusForgeNativeConversationOverlay.cs'],cwd=root).decode('utf-8-sig')
method=ex.declaration(s,'private async Task SubmitAsync(string text)')
callback=ex.declaration(method,'RunOnMainThread(delegate')+');'
helper=ex.declaration(s,'private bool IsSubmitGenerationActive(')
code='''using System; using System.Collections.Generic;
class Hero {} class CharacterObject {}
static class ShoutBehavior { public static bool IsNativeConversationResponseTargetAvailableForExternal() => true; }
static class EncyclopediaEntityLinkFormatter { public class StreamingDisplaySession { public string FormatStreamingText(string text, Hero h, CharacterObject c)=>text; } public static StreamingDisplaySession CreateStreamingDisplaySession()=>new StreamingDisplaySession(); }
static class ConversationHelper { public static string Text="new target B"; public static void UpdateDialogText(string text) {Text=text;} }
class Vm { public bool IsCustomAnswerVisible=true; }
class Proof {
 bool _isClosed=false; int _submitGeneration=1; Vm _dataSource=new Vm(); List<Action> queue=new List<Action>();
 void StopWaitingDotsAnimation(int g){} void ClearPendingPostprocessNotice(){} void RunOnMainThread(Action a){queue.Add(a);}
@@HELPER@@
 void EnqueueOldReply() {
 int generation=1; string partial="reply for A",originalDialogText="old target A";
 bool receivedVisibleText=false,suppressReadyNotice=false,suppressVisibleStreamingForTts=false;
 EncyclopediaEntityLinkFormatter.StreamingDisplaySession streamingLinkDisplaySession=null;
 Hero streamLinkTargetHero=null; CharacterObject streamLinkTargetCharacter=null;
 @@CALLBACK@@
 }
 static void Main(){ var p=new Proof();p.EnqueueOldReply();ConversationHelper.Text="new target B";
 foreach(var a in p.queue)a();if(ConversationHelper.Text!="reply for A")throw new Exception("Baseline reproduction changed");
 Console.WriteLine("REPRODUCED: actual old queued stream callback replaced target B display with reply for A; unchanged UI generation and both targets available.");
 Console.WriteLine("Source=36e04059 actual callback/guard; game/link formatter=STUBBED; LIVE=NOT_RUN."); }
}'''.replace('@@HELPER@@',helper).replace('@@CALLBACK@@',callback)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
env=os.environ.copy();env.update(DOTNET_ROOT=r'G:\AFMOD\.dotnet-sdk',DOTNET_CLI_HOME=str(root/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(root/'.tmp/nuget-packages'),DOTNET_GENERATE_ASPNET_CERTIFICATE='false',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([r'G:\AFMOD\.dotnet-sdk\dotnet.exe','run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=root,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr);raise SystemExit(r.returncode)
