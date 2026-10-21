"""Execute production chronicle projection and generated interior XML; no native UI."""
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
parser.add_argument('--run-root', required=True, type=Path)
parser.add_argument('--baseline-ref')
args = parser.parse_args()
out = new_run_root(ROOT, 'interior-chronicle', args.run_root)
paths = ['src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.cs', 'src/AF.GameAdapter.Bannerlord/UI/Kingdom/KingdomInteriorPanel.cs']
sources = [subprocess.check_output(['git', 'show', args.baseline_ref + ':' + p], cwd=ROOT) if args.baseline_ref else (ROOT / p).read_bytes() for p in paths]
owner, layout = [s.decode('utf-8-sig') for s in sources]
signature = 'private static void AddRecentChronicle('
if signature in owner:
    start = owner.index(signature)
    i = owner.index('{', start) + 1
    depth = 1
    while depth:
        depth += (owner[i] == '{') - (owner[i] == '}')
        i += 1
    projection = owner[start:i]
else:
    # Baseline executes the original panel's unmodified projection loop.
    start = owner.index('for (int i = history.Count - 1; i >= 0 && panel.Chronicle.Count < 3; i--)')
    end = owner.index('\n', owner.index('panel.Chronicle.Add(', start))
    projection = 'private static void AddRecentChronicle(CivilWarPanelKingdom panel, List<KingdomCivilWarHistoryEntry> history) { history ??= new(); ' + owner[start:end] + ' }'
(out / 'Layout.cs').write_text(layout, encoding='utf-8')
template = Path(__file__).with_name('Program.cs.template').read_text(encoding='utf-8')
(out / 'Program.cs').write_text(template.replace('@@PROJECTION@@', projection), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649</NoWarn></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'sources.json').write_text(json.dumps({'baseline': args.baseline_ref, 'sources': dict(zip(paths, [hashlib.sha256(s).hexdigest() for s in sources]))}, indent=2), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
exits = []
for api, symbol in [('13', 'AF_TEST_API13'), ('14', 'BANNERLORD_1_4_OR_GREATER')]:
    result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release', '-p:DefineConstants=' + symbol], cwd=ROOT, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    log = result.stdout + result.stderr
    (out / ('api' + api + '.log')).write_text(log, encoding='utf-8')
    print('API ' + api + ':\n' + log, end='')
    exits.append(result.returncode)
raise SystemExit(1 if any(exits) else 0)
