from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, 'native-rewrite-integration', args.run_root)
inputs = [
    ROOT / 'src/modules/AF.Module.Conversation/Internal/History/ConversationRewriteTurn.cs',
    ROOT / 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeRewrite.cs',
    ROOT / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Rewrite.cs',
    HERE / 'Program.cs',
]
items = ''.join('<Compile Include="' + str(p) + '" />' for p in inputs)
(out / 'source-manifest.json').write_text(json.dumps({p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs}, indent=2), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
(out / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
