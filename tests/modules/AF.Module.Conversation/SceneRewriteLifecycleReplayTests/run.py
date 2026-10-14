from pathlib import Path
import argparse
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, 'scene-rewrite-lifecycle', args.run_root)
dotnet = resolve_dotnet(ROOT)
files = [Path(__file__).with_name('Program.cs'),
         ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneRewrite.cs',
         ROOT / 'src/modules/AF.Module.Conversation/Internal/History/ConversationRewriteTurn.cs']
items = ''.join('<Compile Include="' + escape(str(path)) + '" />' for path in files)
(out / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
# Verify the actual channel consumers still scope only their own synchronous writes.
player = (ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePlayerExecution.cs').read_text(encoding='utf-8-sig')
reply = (ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneExecutionStages.cs').read_text(encoding='utf-8-sig')
composition = (ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneSessionComposition.cs').read_text(encoding='utf-8-sig')
presentation = (ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePresentationSession.cs').read_text(encoding='utf-8-sig')
for source, token in [(player, 'ConversationRewriteTurn.CaptureScope(_sceneRewriteTurn)'),
                      (player, 'CompleteSceneRewriteTurn(rewriteTurn)'),
                      (player, 'if (receipt == null) BeginSceneRewriteTurn('),
                      (reply, 'ConversationRewriteTurn.CaptureScope(CaptureSceneRewriteTurn(conversationEpoch))'),
                      (composition, 'InvalidateOtherChannelRewrite = InvalidateNativeRewriteTurn'),
                      (presentation, '_sceneConversationRuntime?.InvalidateRewriteTurn()')]:
    assert token in source, 'Missing real consumer: ' + token
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Tests.csproj'), '-c', 'Release'],
                        cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True,
                        text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
print('OUTPUT', out)
raise SystemExit(result.returncode)
