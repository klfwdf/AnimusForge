"""Actual production transports; deterministic pending send/read, no provider or credentials."""
from pathlib import Path
import argparse
import os
import subprocess
import sys
import json

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--mutate', choices=['drop-owner-token', 'ignore-pending-read', 'accept-late-response'])
parser.add_argument("--run-root",type=Path)
args = parser.parse_args()
out = new_run_root(ROOT,'request-cancellation-transport',args.run_root)
files = ['src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs',
         'src/modules/AF.Module.Llm/Streaming/LlmStreamingTransport.cs',
         'src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs',
         'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',
         'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs']
for file in files:
    text = (ROOT / file).read_text(encoding='utf-8-sig')
    if file.endswith('LlmNonStreamingTransport.cs'):
        if args.mutate == 'drop-owner-token':
            text = text.replace('CancellationToken ownerToken = OwnerCancellation.Value;',
                                'CancellationToken ownerToken = CancellationToken.None;', 1)
        elif args.mutate == 'ignore-pending-read':
            text = text.replace('try { response.Dispose(); }', 'try { /* mutation */ }', 1)
        elif args.mutate == 'accept-late-response':
            text = text.replace('cancellationToken.ThrowIfCancellationRequested();', '/* mutation */')
    (out / Path(file).name).write_text(text, encoding='utf-8')
(out / 'Program.cs').write_text((HERE / 'Harness.cs.txt').read_text(encoding='utf-8-sig'), encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
newtonsoft = dotnet.parent / 'sdk/8.0.425/Newtonsoft.Json.dll'
(out / 'Proof.csproj').write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup>'
    '<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(newtonsoft) +
    '</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
env = minimal_test_environment(dotnet,out)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
                        cwd=out, env=env, capture_output=True, text=True, encoding='utf-8',
                        errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
(out / 'result.json').write_text(json.dumps({'exitCode':result.returncode,'mutation':args.mutate}),encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
