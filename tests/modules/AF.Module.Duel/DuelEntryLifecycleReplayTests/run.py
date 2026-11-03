"""Replay current production entry/tick/death-gate methods against synthetic engine objects.
Usage: python run.py --dotnet <dotnet.exe> [--output <dir>]
This executes extracted method bodies, not copied algorithms. Dispatch transitions,
settlement effects, restoration internals, and native engine callbacks are test doubles.
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
out = Path(args.output) if args.output else root / '.tmp/duel-entry-lifecycle-replay'
out.mkdir(parents=True, exist_ok=True)
source = root / 'src/modules/AF.Module.Duel/Host/DuelBehavior.cs'
source_bytes = source.read_bytes()
text = source_bytes.decode('utf-8-sig').replace('\r\n', '\n')
provenance = []


def extract(signature, within=text):
    start = within.index(signature)
    brace = within.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        if within[end] == '{':
            depth += 1
        elif within[end] == '}':
            depth -= 1
        end += 1
    body = within[start:end]
    absolute = text.index(body)
    provenance.append({'signature': signature, 'line': text[:absolute].count('\n') + 1})
    return body


native = extract('private sealed class WildernessDuelBattleMissionLogic')
arena = extract('private class ArenaDuelMissionBehavior')
host_signatures = [
    'private void StartDuelViaAI(\n\t\tHero target,',
    'private void StartDuelViaAI(\n\t\tAgent targetAgent,',
    'private Agent GetAgent(Hero hero)',
    'private Agent GetTargetAgent()',
    'private bool TrySetCurrentDuelTargetFromAgent(Agent agent)',
    'public void OnEngineTick()',
    'private bool IsInPlaceDuelCombatParticipant(',
    'private static bool TryGetCombatDuelTargetHero(',
    'private sealed class DuelPlayerDeathAgentStateDeciderLogic',
    'private sealed class DuelMainHeroDeathMissionBehavior',
    'private sealed class DuelTargetDeathMissionBehavior',
    'private void AbortInPlaceDuel(',
]
native_signatures = [
    'public override void OnMissionTick(float dt)',
    'public override void OnAgentRemoved(',
    'public override void OnAgentFleeing(',
    'private bool IsCombatSessionActive()',
    'internal bool IsCombatParticipant(',
    'private bool IsDuelParticipant(',
    'private bool IsPlayerParticipant(',
    'private bool IsTargetParticipant(',
    'private static bool IsDuelDefeatRemovalState(',
    'private static bool IsAgentDefeatedOrFleeing(',
    'private bool TrySettleRemovedParticipant(',
    'private bool TrySettleFleeingParticipant(',
    'private Agent FindTargetAgent()',
    'private void TryResolveResultFromAgents(',
]
program = (here / 'Harness.cs').read_text(encoding='utf-8')
program = program.replace('// PRODUCTION_HOST_METHODS', '\n'.join(extract(x) for x in host_signatures))
program = program.replace('// PRODUCTION_NATIVE_METHODS', '\n'.join(extract(x, native) for x in native_signatures))
program = program.replace('// PRODUCTION_ARENA_GATE', extract('internal bool IsCombatParticipant(', arena))
(out / 'Program.cs').write_text(program, encoding='utf-8')
(out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414</NoWarn></PropertyGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
(out / 'source.json').write_text(json.dumps({
    'source': str(source), 'sha256': hashlib.sha256(source_bytes).hexdigest(),
    'extracted': provenance,
    'limits': [
        'Synthetic Mission/Agent/Campaign; no actual game or native event ordering.',
        'StartDuelInternal is a recording boundary; owner dispatch and setup effects are not replayed here.',
        'EndDuel, SettleWildernessDuelRuntime, ForceKillMainHero and restoration internals are recording boundaries.',
        'Global resolver, pending tuple/mission, actual death gates, native defeat recognition and resolver run production bodies.'
    ]}, indent=2), encoding='utf-8')
result = subprocess.run([args.dotnet, 'run', '--project', str(out / 'Replay.csproj')], capture_output=True, text=True, encoding='utf-8', errors='replace')
(out / 'result.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
