from pathlib import Path
import argparse,sys,json,hashlib,subprocess,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',required=True,type=Path);parser.add_argument('--baseline',action='store_true');parser.add_argument('--lifecycle',action='store_true');args=parser.parse_args()
out=new_run_root(ROOT,'scene-speech-presentation-sink',args.run_root)
old=ROOT/'tests/modules/AF.Module.Conversation/ScenePresentationLifecycleTests'
project=ET.parse(old/'ScenePresentationLifecycleTests.csproj').getroot()
manifest={}
for node in project.iter('Compile'):
 path=(old/node.attrib['Include']).resolve();target=out/path.name
 data=subprocess.check_output(['git','show','44510c91f:'+path.relative_to(ROOT).as_posix()],cwd=ROOT) if args.baseline else path.read_bytes()
 target.write_bytes(data);manifest[path.relative_to(ROOT).as_posix()]=hashlib.sha256(data).hexdigest()
for name in ['Stubs.cs','SceneSpeechOutputOracle.cs','SceneSpeechOutputContract.cs','SceneInteractionContract.cs','SceneAudienceSettingsStubs.cs','SceneAudienceToggleCases.cs']:
 text=(old/name).read_text(encoding='utf-8-sig')
 if args.baseline and name=='SceneSpeechOutputContract.cs':
  text='\n'.join(line for line in text.splitlines() if 'PublishFeedImmediately=' not in line)
 if name=='Stubs.cs':
  text=text.replace('public sealed class InformationMessage { public InformationMessage(string text, Color color=default) { } }','public sealed class InformationMessage { public readonly string Text; public InformationMessage(string text, Color color=default) { Text=text; } }')
  text=text.replace('public static int Messages; public static void DisplayMessage(InformationMessage message) { Messages++; }','public static int Messages; public static readonly List<string> Captured=new(); public static void DisplayMessage(InformationMessage message) { Messages++; Captured.Add(message.Text); }')
 (out/name).write_text(text,encoding='utf8');manifest['fixture:'+name]=hashlib.sha256(text.encode()).hexdigest()
(out/'Program.cs').write_bytes(((old if args.lifecycle else HERE)/'Program.cs').read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',encoding='utf8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf8')
(out/'sources.json').write_text(json.dumps({'baseline':args.baseline,'sources':manifest},indent=2),encoding='utf8')
if not args.baseline:
 binding=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneAudio.cs').read_text(encoding='utf-8-sig')
 assert 'PublishFeedImmediately = (index, name, text, info) => _j17SceneSpeechOutputQueueController.PublishNpcSpeechToMessageFeedImmediately(index, name, text, info),' in binding
 composition=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
 assert 'CaptureConversationEpoch = () => _sceneConversationEpoch,' in composition
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
cmd=[str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release']
if args.baseline:cmd+=['--','--baseline']
p=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf8',errors='replace',timeout=120)
log=p.stdout+p.stderr;(out/'run.log').write_text(log,encoding='utf8');print(log,end='');raise SystemExit(p.returncode)
