"""Replay production player-summary scheduling with a recording HTTP/game boundary.
No game build, deployment, external HTTP or real save files. Source members are
compiled unchanged; fake clock/game/render leaves are listed in the harness.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

# Ignore comment/literal braces while extracting unchanged C# members.
TOKEN = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|=>|[{};]')


def member(source, signature):
    start = source.index(signature)
    depth, opened, expression = 0, False, False
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
                end = token.end()
                if source[end:end + 1] == ';':
                    end += 1
                return source[start:end]
    raise ValueError('Unterminated production member: ' + signature)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dotnet')
    parser.add_argument('--run-root', type=Path)
    args = parser.parse_args()
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    source_path = ROOT / 'src/modules/AF.Module.Social/Host/PlayerNotorietyBehavior.cs'
    source = source_path.read_text(encoding='utf-8-sig')
    harness_path = HERE / 'PlayerSummaryRecoveryReplay.Harness.cs.txt'
    harness = harness_path.read_text(encoding='utf-8-sig')
    signatures = [
        'public override void RegisterEvents(', 'public override void SyncData(', 'private string BuildStorageDiagnostics(',
        'private void OnDailyTick(', 'private void TryStartSummaryProcessing(', 'private bool HasSummaryWorkDue(',
        'private static bool IsEligibleSummaryMaterial(', 'private bool HasPendingMajorMaterials(',
        'private sealed class PlayerSummaryRun', 'private static PlayerHistoryMaterial CloneSummaryMaterial(',
        'private bool IsSummaryOwnerCurrent(', 'private bool IsSummarySourceCurrent(', 'private void StartSummaryAttempt(',
        'internal void OnEngineTick(', 'private void RecordSummaryAttemptFailure(', 'private void ApplySummaryFallback(',
        'internal void RetireCampaignRuntime(', 'private void ResetSummaryProcessing(', 'private void DiscardSummaryRun(', 'private void FinishSummaryRun(',
        'private void FaultSummaryProcessing(', 'private static string SummaryErrorForDiagnostics(', 'private void LogSummarySchedule(',
        'private bool TryParseSummaryResponse(', 'private void ApplySummarySuccess(',
        'private void ApplyPlayerMajorHistoryEditorInput(', 'private static string NormalizeMajorSummaryForStorage(',
        'private static string BuildFallbackMajorSummary(', 'private bool IsMajorSummaryOverPromptLimit(',
        'private static string TrimMajorSummaryKeepNewest(', 'private static string TrimMajorSummaryFallback(',
        'private void RememberSummarizedMaterialKey(PlayerHistoryMaterial',
        'private void RememberSummarizedMaterialKey(string', 'private static void RememberSummarizedMaterialKey(',
        'private void PruneSummarizedMajorMaterials(', 'private static PlayerNotorietyState PruneSummarizedMajorMaterials(',
        'private static List<string> NormalizeSummarizedMaterialKeys(', 'private static PlayerNotorietyState NormalizeState(',
        'private static PlayerHistoryMaterial NormalizeHistoryMaterial(', 'private static string NormalizeLine(',
        'private static string NormalizeStableKey(', 'private static double ClampDouble(',
        'private static int GetSummaryIntervalDays(', 'private static int GetMajorPromptChars(',
        'private static JObject TryParseJsonObject(', 'private static JToken GetJsonToken(', 'private static string GetJsonString(',
        'private sealed class PlayerNotorietyState', 'private sealed class PlayerNpcKnowledgeState',
        'private sealed class PlayerHistoryMaterial',
    ]
    fields = source[source.index('private PlayerSummaryRun _summaryRun;'):source.index('public override void RegisterEvents(')]
    production = ('using System; using System.Collections.Generic; using System.Diagnostics; using System.Linq; '
                  'using System.Net.Http; using System.Text; using System.Threading; using System.Threading.Tasks; '
                  'using Newtonsoft.Json; using Newtonsoft.Json.Linq; using TaleWorlds.CampaignSystem; '
                  'using AnimusForge.Refactor.Adapters; using Stopwatch = AnimusForge.FakeSummaryClock;\nnamespace AnimusForge { public sealed partial class PlayerNotorietyBehavior : CampaignBehaviorBase {\n'
                  + fields + '\n' + '\n'.join(member(source, signature) for signature in signatures) + '\n} }\n')
    policy = ROOT / 'src/modules/AF.Module.Memory/Records/ActionHistoryVisibilityPolicy.cs'
    transport = ROOT / 'src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs'
    out = new_run_root(ROOT, 'player-summary-recovery', args.run_root)
    (out / 'Production.cs').write_text(production, encoding='utf-8')
    (out / 'Policy.cs').write_bytes(policy.read_bytes())
    (out / 'LlmNonStreamingTransport.cs').write_bytes(transport.read_bytes())
    (out / 'Harness.cs').write_text(harness, encoding='utf-8')
    (out / 'sources.json').write_text(json.dumps({str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                                                 for p in (source_path, policy, transport, harness_path)}, indent=2), encoding='utf-8')
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    dependencies = sorted((dotnet.parent / 'sdk').glob('8.*/Newtonsoft.Json.dll'))
    if not dependencies:
        raise SystemExit('BLOCKED_ENV: selected SDK has no net8 Newtonsoft.Json.dll')
    dependency = dependencies[-1]
    (out / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>'
        '<LangVersion>latest</LangVersion><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>'
        '<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(dependency)
        + '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
    (out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
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
