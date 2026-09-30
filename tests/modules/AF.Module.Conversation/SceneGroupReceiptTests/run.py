"""Compile the production Scene group receipt and deferred outcome against detached tasks."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--run-root', type=Path)
    args = parser.parse_args()
    scene = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs', None)
    post = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs', None)
    host = extract.source('ShoutBehavior.cs', None)
    chains = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs', None)
    passive = extract.declaration(chains, 'private async Task<string> GetPassiveNpcResponse(')
    group = extract.declaration(chains, 'private async Task HandleGroupResponsePerHeroIndependent(')
    assert passive.count('currentInputAlreadyRecorded: true') == 1, 'passive input ownership changed'
    assert group.count('BuildStrictSceneMessagesForNpc(currentSpeaker.AgentIndex, layeredPrompt') == 1, 'group prompt call changed'
    assert 'BuildGroupSpeakingCandidates(allNpcData, primaryNpc)' in group, 'group candidate owner changed'
    assert 'await RecordSceneReplyHistoryOnMainThreadAsync(' in group, 'group history owner changed'
    declarations = '\n'.join([
        extract.declaration(post, 'private enum ScenePostprocessStatus'),
        extract.declaration(post, 'private sealed class ScenePostprocessOutcome'),
        extract.declaration(scene, 'private sealed class SceneGroupReceipt'),
        extract.declaration(host, 'private static List<NpcDataPacket> BuildGroupSpeakingCandidates('),
        extract.declaration(host, 'private List<object> BuildStrictSceneMessagesForNpc('),
        extract.declaration(host, 'private Task<bool> RecordSceneReplyHistoryOnMainThreadAsync('),
    ])
    output = new_run_root(ROOT, 'scene-group-receipt', args.run_root)
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
