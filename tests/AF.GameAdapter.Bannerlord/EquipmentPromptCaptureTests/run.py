"""Run unchanged production reader spans against bounded equipment fakes; no game engine."""
from pathlib import Path
import argparse
import subprocess

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument('--baseline', action='store_true')
args = parser.parse_args()
task = ROOT / 'artifacts/equipment-prompt-slot-fix-20261021'
output = task / ('replay-before' if args.baseline else 'replay-after')
output.mkdir(parents=True, exist_ok=True)


def method(file, signature):
    path = (task / 'before' if args.baseline else ROOT) / file
    source = path.read_text(encoding='utf-8-sig')
    start = source.index(signature)
    opening = source.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


methods = [method('src/AF.GameAdapter.Bannerlord/Composition/EquipmentPromptCaptureAdapter.cs',
                  'internal static string BuildNonHeroEquipmentSummaryForPrompt('),
           method('src/AF.GameAdapter.Bannerlord/Prompt/PersonaIdentityPromptCaptureAdapter.cs',
                  'internal static ItemObject TryGetAgentEquipmentItemForPrompt(')]
(output / 'Readers.cs').write_text(
    'using System; using System.Collections.Generic; using System.Linq;\n'
    'internal static partial class Readers {\n' + '\n'.join(methods) + '\n}', encoding='utf-8')
for name in ('Program.cs', 'Stubs.cs'):
    (output / name).write_bytes((HERE / name).read_bytes())
(output / 'Replay.csproj').write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net10.0</TargetFramework><NuGetAudit>false</NuGetAudit>'
    '</PropertyGroup></Project>', encoding='utf-8')
raise SystemExit(subprocess.call(['dotnet', 'run', '--project', str(output / 'Replay.csproj')]))
