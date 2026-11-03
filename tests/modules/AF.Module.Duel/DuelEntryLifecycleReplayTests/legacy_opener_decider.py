"""Compare actual pre-typed openers/decider with current production, not hand-written legacy expectations.
Mission construction and successful combat setup are synthetic boundaries. No game launch.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet', default='dotnet')
parser.add_argument('--output')
args = parser.parse_args()
here = Path(__file__).resolve().parent
root = here.parents[3]
out = Path(args.output) if args.output else root / '.tmp/duel-legacy-opener-decider'
out.mkdir(parents=True, exist_ok=True)
revision = '2d1a1264dc226775d9a09dcdbcd28d9c7669e1af'
legacy = subprocess.check_output(['git', '-c', 'safe.directory=' + root.as_posix(), '-C', str(root), 'show', revision + ':DuelBehavior.cs']).decode('utf-8-sig').replace('\r\n', '\n')
source = root / 'src/modules/AF.Module.Duel/Host/DuelBehavior.cs'
current_bytes = source.read_bytes()
current = current_bytes.decode('utf-8-sig').replace('\r\n', '\n')
provenance = []


def extract(text, signature, label):
    start = text.index(signature)
    brace = text.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        if text[end] == '{':
            depth += 1
        elif text[end] == '}':
            depth -= 1
        end += 1
    provenance.append({'source': label, 'signature': signature, 'line': text[:start].count('\n') + 1})
    return text[start:end]


shared = (here / 'LegacyOpenerDeciderHarness.cs').read_text(encoding='utf-8')
global_code, host_template, main = shared.split('// SPLIT')
pieces = [global_code]
for label, text in [('Legacy', legacy), ('Current', current)]:
    signatures = [
        'private static bool TryOpenStandaloneWildernessDuelMission(',
        'private sealed class DuelPlayerDeathAgentStateDeciderLogic',
        'private bool IsCurrentDuelTargetAgent(',
    ]
    signatures.append('private bool TryTeleportToArenaForDuel(CharacterObject targetCharacter)' if label == 'Legacy' else 'private bool TryTeleportToArenaForDuel(\n\t\tCharacterObject targetCharacter,')
    if label == 'Current':
        signatures += ['private bool TryTeleportToArenaForDuel(CharacterObject targetCharacter)',
                       'private bool IsInPlaceDuelCombatParticipant(', 'private static bool TryGetCombatDuelTargetHero(']
    methods = '\n'.join(extract(text, signature, label) for signature in signatures)
    code = host_template.replace('// PRODUCTION_METHODS', methods)
    arena_text = current[current.index('private class ArenaDuelMissionBehavior'):]
    code = code.replace('// ARENA_GATE', extract(arena_text, 'internal bool IsCombatParticipant(', 'Current.ArenaGate'))
    pieces.append('namespace ' + label + ' {\n' + code + '\n}\n')
pieces.append(main)
(out / 'Program.cs').write_text('\n'.join(pieces), encoding='utf-8')
(out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414;CS0169</NoWarn></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
(out / 'source.json').write_text(json.dumps({
    'legacy_revision': revision, 'legacy_path': 'DuelBehavior.cs',
    'current_path': str(source), 'current_sha256': hashlib.sha256(current_bytes).hexdigest(),
    'methods': provenance,
    'limits': ['Actual old/current ordinary and standalone opener methods run. Mission construction and eligibility/encounter services are synthetic.',
               'Successful combat setup is injected after opening; it does not mutate global duel-active flags.',
               'Old/current actual deciders run at probabilities 0/1 for player/opponent. Generic/local death callback ordering is covered by the companion legacy_callback_order replay.',
               'Current exact combat participant gate is available in both fake Arena shells, but legacy decider never calls it.']}, indent=2), encoding='utf-8')
result = subprocess.run([args.dotnet, 'run', '--project', str(out / 'Replay.csproj')], capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'result.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
