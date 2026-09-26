"""Compile the production Scene group receipt and deferred outcome against detached tasks."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    args = parser.parse_args()
    scene = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs', None)
    post = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs', None)
    declarations = '\n'.join([
        extract.declaration(post, 'private enum ScenePostprocessStatus'),
        extract.declaration(post, 'private sealed class ScenePostprocessOutcome'),
        extract.declaration(scene, 'private sealed class SceneGroupReceipt'),
    ])
    output = HERE / '.generated/current'
    output.mkdir(parents=True, exist_ok=True)
    program = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8').replace('@@DECLARATIONS@@', declarations)
    assert '@@DECLARATIONS@@' not in program
    (output / 'Program.cs').write_text(program, encoding='utf-8')
    contracts = ROOT / 'Refactor/Modules/CoreDialogueContracts.cs'
    operation = ROOT / 'Refactor/Modules/CoreDialogueOperation.cs'
    (output / 'Tests.csproj').write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework>'
        '<OutputType>Exe</OutputType><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>'
        '</PropertyGroup><ItemGroup><Compile Include="' + str(contracts) + '" /><Compile Include="' + str(operation) + '" /></ItemGroup></Project>',
        encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(args.dotnet).resolve().parent), DOTNET_CLI_HOME=str(ROOT / '.tmp/dotnet-cli'),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
    result = subprocess.run([args.dotnet, 'run', '--project', str(output / 'Tests.csproj'), '-c', 'Release'],
                            cwd=output, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
    log = result.stdout + result.stderr
    (output / 'run.log').write_text(log, encoding='utf-8')
    print(log, end='')
    return result.returncode

if __name__ == '__main__':
    raise SystemExit(main())
