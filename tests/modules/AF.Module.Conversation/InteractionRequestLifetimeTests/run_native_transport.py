"""Execute Native's actual API timeout consumer with controllable transport, no game/provider."""
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
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--mutate', choices=['drop-timeout-token', 'drop-stream-caller', 'publish-after-cancel'])
args = parser.parse_args()
out = new_run_root(ROOT, 'native-transport-lifetime', args.run_root)
source = (ROOT / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
method = extract.declaration(source, 'private static async Task<string> CallNativeConversationApiAsync(')
if args.mutate == 'drop-timeout-token':
    method = method.replace('cancellationToken: requestTimeout.Token', 'cancellationToken: CancellationToken.None')
elif args.mutate == 'drop-stream-caller':
    method = method.replace('CreateTimeout(NativeConversationMainReplyTimeoutMs, cancellationToken)',
                            'CreateTimeout(NativeConversationMainReplyTimeoutMs, CancellationToken.None)')
elif args.mutate == 'publish-after-cancel':
    method = method.replace('timeoutCts.IsCancellationRequested || string.IsNullOrEmpty(delta)', 'string.IsNullOrEmpty(delta)')
template = (HERE / 'NativeTransportHarness.cs.txt').read_text(encoding='utf-8-sig')
(out / 'Program.cs').write_text(template.replace('@@NATIVE@@', method), encoding='utf-8')
for file in ['Transport/LlmNonStreamingTransport.cs', 'Streaming/LlmStreamingTransport.cs',
             'Protocol/LlmApiCompat.cs', 'Protocol/LlmVisibleReplyNormalizer.cs']:
    (out / Path(file).name).write_text((ROOT / 'src/modules/AF.Module.Llm' / file).read_text(encoding='utf-8-sig'), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
newtonsoft = dotnet.parent / 'sdk/8.0.425/Newtonsoft.Json.dll'
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup>'
    '<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(newtonsoft) +
    '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
    cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
