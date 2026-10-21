"""Run production UpdatePosition with engine-projection stubs; no live renderer."""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
parser.add_argument('--baseline-ref')
args = parser.parse_args()
out = new_run_root(ROOT, 'floating-text-position', args.run_root)
path = 'src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextMissionView.cs'
raw = subprocess.check_output(['git', 'show', args.baseline_ref + ':' + path], cwd=ROOT) if args.baseline_ref else (ROOT / path).read_bytes()
source = raw.decode('utf-8-sig')

def method(signature):
    start = source.index(signature)
    opening = source.index('{', start)
    depth = 1
    cursor = opening + 1
    while depth:
        depth += (source[cursor] == '{') - (source[cursor] == '}')
        cursor += 1
    return source[start:cursor]

methods = '\n'.join(method(s) for s in [
    'private void UpdatePosition(', 'private static float EstimateBubbleHeight(',
    'private static bool IsFinite(Vec2', 'private static bool IsFinite(Vec3',
    'private static bool IsFinite(float', 'private static void HideBubble('])
template = Path(__file__).with_name('Program.cs.template').read_text(encoding='utf-8')
(out / 'Program.cs').write_text(template.replace('@@METHODS@@', methods), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'source.json').write_text(json.dumps({'source': path, 'sha256': hashlib.sha256(raw).hexdigest(), 'baseline': args.baseline_ref, 'scope': 'actual production position and height methods; engine projection and UI scale are stubs; live rendering not tested'}, indent=2), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=ROOT, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log, end='')
raise SystemExit(result.returncode)
