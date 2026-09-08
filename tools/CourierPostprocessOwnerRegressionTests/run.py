"""Execute extracted Courier owners with real contracts, ports and ActionTagParser."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import os
import re
import subprocess
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('boundary_extractor', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)
SIGNATURES = [
    'public static LegacyInteractionPipelinePorts CreateCourierDetachedPortsForExternal(',
    'private static LegacyInteractionPipelinePorts CreateCourierDetachedPorts(',
    'private static IReadOnlyList<string> ReadCourierCsvFact(',
    'private ShoutBehavior.CourierActionPostprocessWorkItem PrepareCourierDetachedPostprocessWorkItem(',
    'private InteractionEnvelope CapturePreparedCourierReplyEnvelope(',
    'private static string PrepareNpcReplyForActionPostprocess(',
    'private static bool LooksLikeApiError(',
    'private static string CleanNpcReply(',
    'private static string StripCourierActionTags(',
    'private static bool HasPreprocessRuleHit(',
    'private sealed class CourierReplyGenerationRequest',
]
LINKS = [
    'CourierVisibleLetterSanitizer.cs', 'LlmVisibleReplyNormalizer.cs',
    'Refactor/Contracts/InteractionContracts.cs', 'Refactor/Contracts/LlmContracts.cs',
    'Refactor/Contracts/ProfileConfigContracts.cs', 'Refactor/Contracts/InteractionPipeline.cs',
    'Refactor/Contracts/FullInteractionPipeline.cs', 'Refactor/Runtime/InteractionRequestCoordinator.cs',
    'Refactor/Adapters/LegacyInteractionPipelineComposition.cs', 'Refactor/Adapters/LegacyActionTagParser.cs',
    'Refactor/Adapters/LegacyDetachedPromptComposer.cs', 'Refactor/Adapters/LegacyPromptPackageAdapter.cs',
]

def extract():
    courier = ex.source('CourierDeliveryBehavior.cs', None)
    shout = ex.source('ShoutBehavior.cs', None)
    return {
        'METHODS': '\n\n'.join(ex.declaration(courier, signature) for signature in SIGNATURES),
        'PARTIAL': ex.source('CourierDeliveryBehavior.DetachedPostprocess.cs', None),
        'WORK_ITEM': ex.declaration(shout, 'internal sealed class CourierActionPostprocessWorkItem'),
    }

MUTATIONS = {
    'visible-protocol-bypass': [('PARTIAL', 'LegacyActionTagParser.RemoveProtocolTags(reply, _ => true)', 'reply')],
    'raw-parser': [('PARTIAL', 'string normalized = owner.WorkItem.CompleteOnMainThread(rawText);', 'string normalized = rawText;')],
    'sync-authority': [('METHODS', '(rawText, context) => new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),', '(rawText, context) => actionParser.Parse(rawText, context),')],
    'prepared-main-recompose': [('METHODS', 'preparedMainPrompt ?? mainComposer.Compose', 'mainComposer.Compose')],
    'post-budget': [('PARTIAL', '}, 5000, "legacy-courier-postprocess");', '}, 4096, "legacy-courier-postprocess");')],
    'late-callback': [('PARTIAL', 'Interlocked.CompareExchange(ref state, 1, 0) != 0', 'false')],
    'same-id-recipient': [('PARTIAL', '!ReferenceEquals(owner.Recipient, owner.Behavior.RequireCurrentCourierPostprocessRecipient(owner.Envelope))', 'owner.Behavior.RequireCurrentCourierPostprocessRecipient(owner.Envelope) == null')],
    'owner-one-shot': [('PARTIAL', '!owners.Remove(context)', 'false'), ('WORK_ITEM', 'Interlocked.Exchange(ref _completeOnMainThread, null)', '_completeOnMainThread')],
}
EXPECTED_FAILURES = {
    'visible-protocol-bypass': 'raw-evidence-visible-cleanup',
    'raw-parser': 'normalize-before-parser-once', 'sync-authority': 'unbound-sync-and-async-zero-actions',
    'prepared-main-recompose': 'prepared-exact-main-prompt-no-rebuild', 'post-budget': 'owner-real-input-reply-rules-5000',
    'late-callback': 'phase-timeout-late-callback', 'same-id-recipient': 'complete-reject-same-id-instance',
    'owner-one-shot': 'normalize-before-parser-once',
}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    parser.add_argument('--output-name', default='current')
    parser.add_argument('--mutation', choices=sorted(MUTATIONS))
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.output_name): parser.error('Invalid output name')
    output = HERE / '.generated' / args.output_name
    output.mkdir(parents=True, exist_ok=True)
    blocks = extract()
    original = dict(blocks)
    if args.mutation:
        for name, old, new in MUTATIONS[args.mutation]:
            if blocks[name].count(old) != 1: raise ValueError('Mutation anchor is not unique: ' + args.mutation + '/' + name)
            blocks[name] = blocks[name].replace(old, new)
    harness = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8-sig')
    for name in ('METHODS', 'WORK_ITEM'):
        assert harness.count('@@' + name + '@@') == 1
        harness = harness.replace('@@' + name + '@@', blocks[name])
    (output / 'Program.cs').write_text(harness, encoding='utf-8')
    # Only the scheduler deadline is accelerated; source and instrumentation are fingerprinted separately.
    assert blocks['PARTIAL'].count('Task.Delay(30000)') == 1
    instrumented = blocks['PARTIAL'].replace('Task.Delay(30000)', 'Task.Delay(180)')
    (output / 'CourierOwner.cs').write_text(instrumented, encoding='utf-8')
    includes = '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(ROOT / '.tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll')) + '</HintPath></Reference>'
    includes += ''.join('<Compile Include="' + escape(str(ROOT / item)) + '" Link="' + escape(Path(item).name) + '"/>' for item in LINKS)
    (output / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + includes + '</ItemGroup></Project>', encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    meta = '\n'.join(name + ' SHA256=' + hashlib.sha256(value.encode()).hexdigest() for name, value in original.items())
    meta += '\nMutation=' + (args.mutation or 'none')
    meta += '\nInstrumentation=Task.Delay(30000) -> Task.Delay(180); generated copy only\n'
    (output / 'source-fingerprints.txt').write_text(meta, encoding='utf-8')
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(args.dotnet).parent), DOTNET_CLI_HOME=str(output / 'cli'), DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_CLI_UI_LANGUAGE='en')
    result = subprocess.run([args.dotnet, 'run', '--project', str(output / 'Tests.csproj'), '-c', 'Release'], cwd=output, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    log = meta + result.stdout + result.stderr
    (output / 'run.log').write_text(log, encoding='utf-8')
    print(log)
    return result.returncode

if __name__ == '__main__': raise SystemExit(main())
