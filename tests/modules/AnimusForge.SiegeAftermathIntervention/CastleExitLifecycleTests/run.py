"""Replay extracted prisoner settlement methods; native APIs are recording stubs."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import minimal_test_environment, new_run_root

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
parser.add_argument('--source-ref')
args = parser.parse_args()
out = new_run_root(ROOT, 'CastleExitLifecycle', args.run_root)
paths = {
    'inspection': 'src/AF.GameAdapter.Bannerlord/TroopInspection/TroopInspectionBehavior.cs',
    'action': 'src/bridges/Siege/Host/CastleAftermathActionRuntimeBridge.cs',
    'scene': 'src/bridges/Siege/Host/CastleAftermathRuntimeBridge.cs',
    'host': 'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs',
}
texts = {k: (subprocess.check_output(['git', 'show', f'{args.source_ref}:{p}'], cwd=ROOT).decode('utf-8-sig')
             if args.source_ref else (ROOT / p).read_text(encoding='utf-8-sig')) for k, p in paths.items()}
methods = []

def method(kind, signature):
    text = texts[kind]
    start = text.index(signature)
    i = text.index('{', start) + 1
    depth = 1
    while depth:
        depth += (text[i] == '{') - (text[i] == '}')
        i += 1
    methods.append({'path': paths[kind], 'symbol': signature,
                    'start': text[:start].count('\n') + 1, 'end': text[:i].count('\n') + 1})
    return text[start:i]

groups = {
    'TroopInspectionBehavior': method('inspection', 'internal static void CleanupRuntime('),
    'CastleAftermathActionRuntimeBridge': '\n'.join(method('action', s) for s in (
        'internal static CastleAftermathActionApplyResult ResolveRegularPrisonersForSettlementEffect(',
        'private static CastleAftermathActionApplyResult RemoveRegularPrisoners(')),
    'CastleAftermathRuntimeBridge': method('scene', 'internal static void RemoveResolvedRegularPrisoners('),
    'CastleAftermathPrisonerCommandMissionBehavior': '\n'.join(method('scene', s) for s in (
        'internal void ResolveRegularPrisoners(', 'internal int ResolveLordPrisoner(',
        'internal int DetachExecutedLordPrisoner(', 'private int ResolveLordPrisonerAgents(')),
}
host = method('host', 'private static void FinalizeInterventionMissionEnd(')
assert host.index('TroopInspectionBehavior.CleanupRuntime(') < host.index('FinalizePendingAftermath(')
(out / 'ActualMethods.cs').write_text(
    'using System;using System.Linq;using System.Collections.Generic;namespace Probe {\n' +
    '\n'.join('partial class ' + name + '{' + body + '}' for name, body in groups.items()) + '}', encoding='utf-8')
(out / 'Program.cs').write_text((HERE / 'Program.cs.txt').read_text(encoding='utf-8-sig'), encoding='utf-8')
owner = ROOT / 'src/modules/AF.Module.Settlement/TroopInspectionSessionOwner.cs'
(out / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="' + str(owner) +
    '" Link="ActualSessionOwner.cs"/></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
dotnet = ROOT / 'local/dotnet/8.0.425/dotnet.exe'
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Probe.csproj')], cwd=ROOT,
    env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8')
(out / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
(out / 'receipt.json').write_text(json.dumps({
    'sourceRef': args.source_ref or 'working-tree', 'methods': methods,
    'sourceSha256': {paths[k]: hashlib.sha256(v.encode('utf-8')).hexdigest() for k, v in texts.items()},
    'exitCode': result.returncode, 'nativeMissionCrash': 'NOT_RUN',
}, indent=2), encoding='utf-8')
result.check_returncode()
