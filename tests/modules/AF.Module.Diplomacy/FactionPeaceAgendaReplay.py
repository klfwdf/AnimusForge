"""Compile actual diplomacy production members in an isolated recording game host.
No main-project build, deployment, game installation or save files are used.
Run: python -B <this-file> [--dotnet <sdk-host>] [--run-root <fresh-directory>]
Artifacts remain under artifacts/tests unless --run-root selects another repo path.
The fake IDataStore transports cloned native references; it is not TaleWorlds SaveSystem.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'src/modules/AF.Module.Diplomacy'
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

# Lexical scanner: ignore braces in comments, literals and interpolated diagnostics.
TOKEN = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|=>|[{};]')


def member(source, signature):
    start = source.index(signature)
    depth = 0
    opened = False
    expression = False
    for token in TOKEN.finditer(source, start):
        value = token.group()
        if value == '=>' and not opened:
            expression = True
        elif value == ';' and expression and not opened:
            return source[start:token.end()]
        elif value == '{':
            depth += 1
            opened = True
        elif value == '}':
            depth -= 1
            if opened and depth == 0:
                return source[start:token.end()]
    raise ValueError('Unterminated production member: ' + signature)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet')
    parser.add_argument('--run-root', type=Path)
    args = parser.parse_args()
    paths = [BASE / 'Agenda/VoteDealBehavior.Propose.cs', BASE / 'Agenda/VoteDealBehavior.cs',
             BASE / 'Agenda/VoteDealBehavior.Agenda.cs', BASE / 'World/WorldDiplomacyBehavior.NativeAdmission.cs',
             BASE / 'World/WorldDiplomacyBehavior.NativeDecisionPort.cs', BASE / 'World/WorldDiplomacyBehavior.cs',
             HERE / 'FactionPeaceAgendaReplay.Harness.cs.txt']
    propose, core, agenda, admission, native_port, world, harness = [p.read_text(encoding='utf-8-sig') for p in paths]
    # All faction API fields/helpers, unchanged. The unrelated dialogue implementation is excluded.
    prefix = propose[propose.index('private readonly System.Collections.Generic.Dictionary'):propose.index('private static string ExecutePropose(')]
    signatures = [
        'private sealed class VoteDealRecord', 'private enum BilateralDiplomacyKind',
        'private sealed class BilateralDiplomacyRecord', 'public override void SyncData(',
        'private static bool Patch_BilateralDiplomacy_ApplyChosenOutcome_Prefix(',
        'private bool HandleBilateralDiplomacyOutcome(', 'private bool CreateBilateralCounterpartDecision(',
        'private static bool QueueBilateralCounterpartDecision(', 'internal static bool IsDecisionPendingInKingdom(',
        'private static float GetAgendaDelayDays(', 'private static bool IsPlayerInvolvedInDecision(',
        'private bool ApplyFinalBilateralDiplomacyEffect(', 'private BilateralDiplomacyRecord CreateBilateralDiplomacyRecord(',
        'private void OnKingdomDecisionCancelled(', 'private static bool TryGetBilateralDiplomacyDecisionDetails(',
        'private static bool TryGetBilateralDiplomacyApproval(', 'private BilateralDiplomacyRecord FindCounterpartRecordForDecision(',
        'private BilateralDiplomacyRecord FindExistingBilateralRecord(', 'private bool IsDuplicateWhileBilateralDiplomacyPending(',
        'private static bool DoesDecisionMatchCounterpartRecord(', 'private static bool CanCreateBilateralCounterpart(',
        'private static bool IsBilateralCounterpartDecisionHardValid(', 'private bool IsBilateralRecordStillPendingValid(',
        'private void CleanupBilateralDiplomacyRecords(', 'internal static Kingdom ResolveKingdom(',
        'private static string KindToString(', 'private static BilateralDiplomacyKind ParseKind(',
    ]
    production = ('using System; using System.Collections.Generic; using System.Linq; using System.Reflection;\n'
                  'using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Actions;\n'
                  'using TaleWorlds.CampaignSystem.Election; using TaleWorlds.CampaignSystem.CampaignBehaviors;\n'
                  'using TaleWorlds.Library; using HarmonyLib;\nnamespace AnimusForge { public partial class VoteDealBehavior {\n'
                  + prefix + '\n' + '\n'.join(member(core, s) for s in signatures)
                  + '\n' + member(agenda, 'private static bool IsWorldDiplomacyControlledAgendaDecision(') + '\n} }\n')
    gates = ('using System; using System.Collections.Generic; using System.Linq; using TaleWorlds.CampaignSystem;\n'
             'using TaleWorlds.CampaignSystem.Election; using AnimusForge.Refactor.Domain;\n'
             'namespace AnimusForge { public partial class WorldDiplomacyBehavior {\n'
             + member(admission, 'internal static void RunAuthorizedEventPeace(') + '\n'
             + member(admission, 'private static bool HasAuthorizedPeace(') + '\n'
             + member(admission, 'private static bool AllowsNativePeace(') + '\n'
             + member(world, 'private static bool Patch_Kingdom_AddDecision_Prefix(') + '\n'
             + member(native_port, 'private sealed class NativeDecisionPort') + '\n} }\n')
    out = new_run_root(ROOT, 'faction-peace-agenda', args.run_root)
    (out / 'Production.cs').write_text(production, encoding='utf-8')
    (out / 'ProductionGates.cs').write_text(gates, encoding='utf-8')
    (out / 'Harness.cs').write_text(harness, encoding='utf-8')
    (out / 'sources.json').write_text(json.dumps({str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}, indent=2), encoding='utf-8')
    (out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><LangVersion>latest</LangVersion><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup></Project>', encoding='utf-8')
    (out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    dotnet = Path(args.dotnet).resolve() if args.dotnet else resolve_dotnet(ROOT)
    env = minimal_test_environment(dotnet, out)
    for name, command in [
        ('build.log', [str(dotnet), 'build', str(out / 'Replay.csproj'), '-v:q', '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')]),
        ('run.log', [str(dotnet), str(out / 'bin/Debug/net8.0/Replay.dll')]),
    ]:
        result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
        (out / name).write_text(result.stdout + result.stderr, encoding='utf-8')
        print(result.stdout + result.stderr, end='')
        if result.returncode:
            print('OUTPUT', out)
            raise SystemExit(result.returncode)
    print('OUTPUT', out)


if __name__ == '__main__':
    main()
