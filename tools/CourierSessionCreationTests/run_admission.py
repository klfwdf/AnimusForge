"""Execute production draft UI callbacks; only game UI and dispatch effects are fixtures."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--ref')
    parser.add_argument('--mutate', choices=['ignore-revision', 'ignore-generation', 'ignore-owner'])
    args = parser.parse_args()
    spec = importlib.util.spec_from_file_location('extract', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)

    def read(path):
        if args.ref:
            return subprocess.check_output(['git', 'show', args.ref + ':' + path], cwd=ROOT).decode('utf-8-sig')
        return (ROOT / path).read_text(encoding='utf-8-sig')

    creation = read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionCreation.cs')
    host = read('CourierDeliveryBehavior.cs')
    mutations = {
        'ignore-revision': ('flow.Revision == revision', 'true'),
        'ignore-generation': ('SaveRuntimeGuard.IsCurrentGeneration(flow.RuntimeGeneration)', 'true'),
        'ignore-owner': ('ReferenceEquals(Instance, this)', 'true'),
    }
    if args.mutate:
        before, after = mutations[args.mutate]
        assert creation.count(before) == 1, 'Mutation anchor drift: ' + args.mutate
        creation = creation.replace(before, after)
    declarations = [extract.declaration(creation, s) for s in (
        'private void ShowLetterInput(', 'private void OnLetterConfirmed(')]
    declarations += [extract.declaration(host, s) for s in (
        'private sealed class PendingCourierFlow', 'private void ResetPendingFlow(')]
    for signature in ('private bool IsPendingCourierFlowCurrent(', 'private long BeginCourierDraftStep(',
                      'private void ResetPendingCourierFlow('):
        if signature in creation:
            declarations.append(extract.declaration(creation, signature))
    output = HERE / '.generated' / ('admission-baseline' if args.ref else 'admission-' + (args.mutate or 'current'))
    output.mkdir(parents=True, exist_ok=True)
    harness = (HERE / 'AdmissionHarness.cs.txt').read_text(encoding='utf-8')
    (output / 'Program.cs').write_text(harness.replace('@@DECLARATIONS@@', '\n'.join(declarations)), encoding='utf-8')
    (output / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>', encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(args.dotnet).resolve().parent), DOTNET_CLI_HOME=str(ROOT / '.tmp/dotnet-cli'),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1')
    result = subprocess.run([args.dotnet, 'run', '--project', str(output / 'Tests.csproj'), '-c', 'Release'],
                            cwd=ROOT, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    log = result.stdout + result.stderr
    (output / 'run.log').write_text(log, encoding='utf-8')
    print(log, end='')
    return result.returncode


if __name__ == '__main__':
    raise SystemExit(main())
