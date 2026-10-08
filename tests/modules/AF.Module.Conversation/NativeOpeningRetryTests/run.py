from pathlib import Path
import argparse, importlib.util, subprocess, sys, hashlib, json
ROOT=Path(__file__).resolve().parents[4]; HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
out=new_run_root(ROOT,'native-opening-retry',args.run_root)
paths=['src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationAdmissionOwner.cs','src/modules/AF.Module.Conversation/Channels/Native/NativeConversationDispatchClaim.cs','src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs','src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs','src/modules/AF.Module.Conversation/Proactive/NpcInitiatedOpeningRouter.cs','src/modules/AF.Module.Social/Proactive/ProactiveOpeningOwner.cs']
for path in paths:(out/Path(path).name).write_bytes((ROOT/path).read_bytes())
read=lambda path:(ROOT/path).read_text(encoding='utf-8-sig')
shape='src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs'
lord='src/modules/AF.Module.Conversation/Proactive/ProactiveNpcRequestBehavior.cs';companion='src/modules/AF.Module.Conversation/Proactive/CompanionProactiveChatBehavior.cs'
code=(HERE/'Harness.cs.txt').read_text(encoding='utf8')
for token,path,signature in [('@@ADMISSION@@',shape,'internal sealed class NativeConversationAdmission'),('@@ERROR@@',shape,'internal sealed class NativeConversationAdmissionException'),('@@LORD_CONSUME@@',lord,'private bool TryConsumePendingOpening('),('@@COMPANION_CONSUME@@',companion,'private bool TryConsumePendingOpening(')]:
 code=code.replace(token,ex.declaration(read(path),signature))
# Production producer must retire the obligation before any accepted raw reply effects.
presentation=read('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs')
assert presentation.index('_ports.RetireOpeningForAcceptedReply?.Invoke(admission);') < presentation.index('TryProcessNativeConversationRawMeetingTauntTags(')
composition=read('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs')
assert 'RetireOpeningForAcceptedReply = NativeAdmissions.RetireOpeningForAcceptedReply,' in composition
reset=ex.declaration(read('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs'),'private void ResetInstanceTransientRuntimeForLoadedSave(')
assert 'NativeAdmissions.EndConversation();' in reset
assert '@@' not in code
(out/'Program.cs').write_text(code,encoding='utf8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',encoding='utf8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf8')
manifest={path:hashlib.sha256((ROOT/path).read_bytes()).hexdigest() for path in paths+[shape,lord,companion]};(out/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
p=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf8',errors='replace',timeout=120)
log=p.stdout+p.stderr;(out/'run.log').write_text(log,encoding='utf8');print(log,end='');raise SystemExit(p.returncode)
