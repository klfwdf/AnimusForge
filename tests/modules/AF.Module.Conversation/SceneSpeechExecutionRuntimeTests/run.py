"""Compile the current production runtime; deterministic dispatcher/delay substitutes."""
import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
args = parser.parse_args()
out = new_run_root(ROOT, 'scene-speech-execution-runtime', args.run_root)
source = ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene'
for name in ('SceneSpeechQueueOwner.cs', 'SceneSpeechExecutionRuntime.cs'):
    (out / name).write_bytes((source / name).read_bytes())
(out / 'Program.cs').write_bytes((HERE / 'Program.cs').read_bytes())
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=ROOT,
                        env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
