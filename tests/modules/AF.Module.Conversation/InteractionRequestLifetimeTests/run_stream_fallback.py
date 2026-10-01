"""Execute the real primary stream method's non-stream fallback and empty-reply retry under cancellation, no game/provider."""
from pathlib import Path
import argparse
import importlib.util
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

FALLBACK = 'string fallback = await CallApiWithMessages(messages, maxTokens, recordTokenStats: false, promptRetryOnError: false, cancellationToken: cancellationToken);'
RETRY_TOKEN = 'forceDisableThinking: true, promptRetryOnError: false, cancellationToken: cancellationToken);'
FALLBACK_GUARD = 'if (!cancellationToken.IsCancellationRequested && !SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_fallback_complete"))'
RETRY_GUARD = 'if (!cancellationToken.IsCancellationRequested && !SaveRuntimeGuard.IsStale(runtimeGeneration, "primary_chat_stream_empty_retry_complete"))'

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--mutate', choices=['fallback-drops-token', 'retry-drops-token', 'fallback-publishes-after-cancel', 'retry-publishes-after-cancel'])
parser.add_argument('--newtonsoft', type=Path, help='Newtonsoft.Json.dll; defaults to the one shipped with the selected SDK')
args = parser.parse_args()

source = (ROOT / 'src/modules/AF.Module.Llm/ShoutNetwork.cs').read_text(encoding='utf-8-sig')
method = extract.declaration(source, 'public static async Task CallApiWithMessagesStream(')
for anchor in (FALLBACK, RETRY_TOKEN, FALLBACK_GUARD, RETRY_GUARD):
    if method.count(anchor) != 1:
        raise SystemExit('source shape changed; re-review the fallback/retry anchors: ' + anchor[:60])
if args.mutate == 'fallback-drops-token':
    method = method.replace(FALLBACK, FALLBACK.replace('cancellationToken: cancellationToken', 'cancellationToken: CancellationToken.None'))
elif args.mutate == 'retry-drops-token':
    method = method.replace(RETRY_TOKEN, RETRY_TOKEN.replace('cancellationToken: cancellationToken', 'cancellationToken: CancellationToken.None'))
elif args.mutate == 'fallback-publishes-after-cancel':
    method = method.replace(FALLBACK_GUARD, 'if (true)')
elif args.mutate == 'retry-publishes-after-cancel':
    method = method.replace(RETRY_GUARD, 'if (true)')

out = new_run_root(ROOT, 'stream-fallback-lifetime', args.run_root)
template = (HERE / 'StreamFallbackHarness.cs.txt').read_text(encoding='utf-8-sig')
(out / 'Program.cs').write_text(template.replace('@@STREAM@@', method), encoding='utf-8')
for relative in ['src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs',
                 'src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs',
                 'src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs',
                 'src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs',
                 'src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs',
                 'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']:
    (out / Path(relative).name).write_text((ROOT / relative).read_text(encoding='utf-8-sig'), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
newtonsoft = args.newtonsoft
if newtonsoft is None:
    shipped = sorted((dotnet.parent / 'sdk').glob('8.*/Newtonsoft.Json.dll'))
    if not shipped:
        raise SystemExit('BLOCKED_ENV: no SDK 8 Newtonsoft.Json.dll; pass --newtonsoft')
    newtonsoft = shipped[-1]
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><Nullable>disable</Nullable></PropertyGroup>'
    '<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(newtonsoft) +
    '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
    cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
