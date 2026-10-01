from pathlib import Path
import importlib.util, os, subprocess, sys, argparse
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('extract', ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
m=importlib.util.module_from_spec(s); s.loader.exec_module(m)
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=(args.run_root or ROOT/'artifacts/af2-host-terminal-closeout/line-b/scene-history-tests').resolve()
out.mkdir(parents=True,exist_ok=True)
create=m.declaration((ROOT/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text(encoding='utf-8-sig'),'internal static object CreateCourierChatMessage(')
metadata=m.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs').read_text(encoding='utf-8-sig'),'public static string StripConversationMetadataPrefix(')
(out/'Program.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8').replace('@@CREATE@@',create).replace('@@METADATA@@',metadata),encoding='utf-8')
paths=['src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs']
paths += ['src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(ROOT/p)+'" />' for p in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=ROOT/'local/dotnet/8.0.425/dotnet.exe'
env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8'); print(r.stdout+r.stderr,end=''); raise SystemExit(r.returncode)
