"""Real Courier network workers, cancellation registry and source-acceptance predicates."""
from pathlib import Path
import argparse
import importlib.util
import json
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
parser.add_argument('--mutate', choices=['drop-registry-cancel', 'drop-inbound-source', 'drop-worker-scope'])
args = parser.parse_args()
out = new_run_root(ROOT, 'courier-delivery-lifetime', args.run_root)
def read(path): return (ROOT / path).read_text(encoding='utf-8-sig')
courier = 'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.'
lifetime = read(courier + 'CampaignLifetime.cs')
prompt = read(courier + 'PromptPreparation.cs')
generation = read(courier + 'GenerationLifecycle.cs')
def declaration(source, signature):
    start = source.index(signature)
    arrow, brace = source.find('=>', start), source.find('{', start)
    if arrow >= 0 and arrow < brace:
        return source[start:source.index(';', arrow) + 1]
    return extract.declaration(source, signature)
methods = '\n'.join(declaration(prompt, signature) for signature in [
    'private sealed class CourierPromptRun', 'private CourierPromptRun BeginCourierPromptRun(',
    'private bool IsCourierPromptRunCurrent(', 'private bool IsCourierReplyRequestCurrent(',
    'private bool IsCourierInboundRequestCurrent(', 'private void FailCourierReplyRequest('])
workers = '\n'.join(extract.declaration(generation, signature) for signature in [
    'private async Task GenerateNpcReplyAsync(', 'private async Task GenerateInboundNpcLetterAsync('])
facade = extract.declaration(read('src/modules/AF.Module.Llm/Transport/LegacyShoutNetworkGateway.cs'),
                             'public static Task<string> SendLegacyMessagesAsync(')
# Execute the real leading acceptance block. The downstream commit/game adapters
# are intentionally absent: this fixture asserts rejection before they are entered.
guards = []
for method in ['CompleteCourierReplyGenerationOnMainThread', 'CompleteInboundLetterGenerationOnMainThread']:
    code = extract.declaration(generation, 'private void ' + method + '(')
    prefix = code.split('\n\t\t\tHero ')[0]
    guards.append(prefix + '\n            Writes++;\n        } catch { throw; }\n    }')
if args.mutate == 'drop-registry-cancel':
    lifetime = lifetime.replace('lifetime.Retire();', '/* mutation */;')
elif args.mutate == 'drop-inbound-source':
    guards[1] = guards[1].replace('IsCourierInboundRequestCurrent(request) ? request.SourceRun.Session : null', 'GetSessionById(request.SessionId)')
elif args.mutate == 'drop-worker-scope':
    workers = workers.replace('LlmNonStreamingTransport.PushOwnerCancellation(request.SourceRun.Token)',
                              'LlmNonStreamingTransport.PushOwnerCancellation(CancellationToken.None)')
    workers = workers.replace('cancellationToken: request.SourceRun.Token', 'cancellationToken: CancellationToken.None')
template = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8-sig')
(out / 'Program.cs').write_text(template.replace('@@METHODS@@', methods).replace('@@WORKERS@@', workers)
    .replace('@@GUARDS@@', '\n'.join(guards)).replace('@@FACADE@@', facade), encoding='utf-8')
(out / 'CourierLifetime.cs').write_text(lifetime, encoding='utf-8')
sources = ['src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',
           'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs',
           'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs',
           'src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs']
for path in sources:
    (out / Path(path).name).write_text(read(path), encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
    cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
(out / 'result.json').write_text(json.dumps({'exitCode': result.returncode, 'mutation': args.mutate}), encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
