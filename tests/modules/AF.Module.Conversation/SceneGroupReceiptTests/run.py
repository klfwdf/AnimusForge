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
from output_isolation import new_run_root, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--run-root', type=Path)
    parser.add_argument('--source-only', action='store_true')
    parser.add_argument('--mutate', choices=['drop-primary', 'drop-dispatch'])
    args = parser.parse_args()
    scene = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs', None)
    post = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs', None)
    host = extract.source('ShoutBehavior.cs', None)
    chains = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs', None)
    history = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs', None)
    stages = extract.source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneExecutionStages.cs', None)
    passive = extract.declaration(chains, 'private async Task<string> GetPassiveNpcResponseCoreAsync(')
    fallback = extract.declaration(chains, 'private async Task<string> GenerateGroupConversationTurnLineCoreAsync(')
    group = extract.declaration(chains, 'internal async Task HandleGroupResponsePerHeroIndependent(')
    assert passive.count('currentInputAlreadyRecorded: true') == 1, 'passive input ownership changed'
    assert group.count('BuildStrictSceneMessagesForNpc(currentSpeaker.AgentIndex, layeredPrompt') == 1, 'group prompt call changed'
    assert 'BuildGroupSpeakingCandidates(allNpcData, primaryNpc)' in group, 'group candidate owner changed'
    assert 'await RecordSceneReplyHistoryOnMainThreadAsync(' in group, 'group history owner changed'
    assert group.count('GenerateGroupConversationTurnLineAsync(') == 1, 'group fallback call changed'
    assert 'SaveRuntimeGuard.IsCurrentGeneration(sceneReplyGeneration)' in group.split('GenerateGroupConversationTurnLineAsync(', 1)[1].split(');', 1)[0], 'fallback generation guard missing'
    assert 'sceneReplySessionId == _ports.SceneSessionId()' in group.split('GenerateGroupConversationTurnLineAsync(', 1)[1].split(');', 1)[0], 'fallback session guard missing'
    assert 'IsSceneConversationEpochCurrent(conversationEpoch)' in group.split('GenerateGroupConversationTurnLineAsync(', 1)[1].split(');', 1)[0], 'fallback epoch guard missing'
    for await_marker in ('await _ports.EnsurePersonaForCandidatesAsync(', 'await _ports.AwaitPrecomputedPersistedHistoryContextAsync(', 'await LegacyShoutNetworkGateway.SendLegacyMessagesAsync('):
        assert fallback.find('if (!IsSceneRequestSourceCurrent(promptGeneration, promptSession, promptEpoch) || (isCurrent != null && !isCurrent())) return "";', fallback.index(await_marker)) > fallback.index(await_marker), 'fallback stale guard missing after ' + await_marker
    if args.source_only:
        print('PASS Scene fallback generation/session/epoch and post-await stale guards')
        return 0
    candidate = extract.declaration((ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneRosterPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig'), 'internal static List<NpcDataPacket> BuildGroupSpeakingCandidates(')
    capture_source=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs').read_text(encoding='utf-8-sig')
    current_capture=extract.declaration(capture_source,'internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(')
    current_capture+='\n'+extract.declaration(capture_source,'internal List<ConversationMessage> CaptureNpcConversationHistory(')
    limit_source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs').read_text(encoding='utf-8-sig')
    limit_start=limit_source.index('internal static int ResolveHistoryLineLimit(')
    current_limit=limit_source[limit_start:limit_source.index(';',limit_start)+1]
    assert current_limit.count('=>')==1
    if args.mutate == "drop-primary":
        assert candidate.count('speakingCandidates.Add(primaryNpc);') == 1
        candidate = candidate.replace('speakingCandidates.Add(primaryNpc);', '// mutation: primary speaker lost', 1)
    wrapper = extract.declaration(chains, 'internal async Task HandleGroupResponse(')
    if args.mutate == 'drop-dispatch':
        call = 'await HandleGroupResponsePerHeroIndependent(playerText, allNpcData, sceneDesc, primaryNpc, extraFact, precomputedContexts, resolvedHeroes, conversationEpoch, conversationScope, framedNpcData, receipt);'
        assert wrapper.count(call) == 1
        wrapper = wrapper.replace(call, 'await Task.CompletedTask;', 1)
    declarations = '\n'.join([
        wrapper,
        extract.declaration(post, 'internal enum ScenePostprocessStatus'),
        extract.declaration(post, 'internal sealed class ScenePostprocessOutcome'),
        extract.declaration(scene, 'internal sealed class SceneGroupReceipt'),
        candidate,
        extract.declaration(history, 'private List<object> BuildStrictSceneMessagesForNpc('),
        extract.declaration(history, 'internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc('),
        extract.declaration(stages, 'internal Task<bool> RecordSceneReplyHistoryOnMainThreadAsync('),
    ])
    output = new_run_root(ROOT, 'scene-group-receipt', args.run_root)
    program = (HERE / 'Harness.cs.txt').read_text(encoding='utf-8').replace('@@DECLARATIONS@@', declarations).replace('@@CURRENT_CAPTURE@@',current_capture).replace('@@CURRENT_LIMIT@@',current_limit)
    assert '@@DECLARATIONS@@' not in program
    (output / 'Program.cs').write_text(program, encoding='utf-8')
    history_paths = ['src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs','src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs','src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs']
    history_paths += ['src/modules/AF.Module.Conversation/Internal/History/SceneConversationHistoryOwner.cs','src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs','src/modules/AF.Module.Conversation/Channels/Scene/ScenePendingAfefFactsOwner.cs']
    history_items = ''.join('<Compile Include="' + str(ROOT/path) + '" />' for path in history_paths)
    contracts = ROOT / 'src/modules/AF.Module.Conversation/Internal/CoreDialogueContracts.cs'
    operation = ROOT / 'src/modules/AF.Module.Conversation/Internal/CoreDialogueOperation.cs'
    (output / 'Tests.csproj').write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework>'
        '<OutputType>Exe</OutputType><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings>'
        '</PropertyGroup><ItemGroup><Compile Include="' + str(contracts) + '" /><Compile Include="' + str(operation) + '" />' + history_items + '</ItemGroup></Project>',
        encoding='utf-8')
    receipt_paths=history_paths+['src/AF.GameAdapter.Bannerlord/Prompt/SceneRosterPromptCaptureAdapter.cs','src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs','src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs','src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs','src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ModuleSceneSubmission.cs']
    (output/'current-consumer-source.json').write_text(__import__('json').dumps({'sources':{x:__import__('hashlib').sha256((ROOT/x).read_bytes()).hexdigest() for x in receipt_paths},'scope':'actual group receipt/wrapper, candidate and strict capture declarations; real history/pending/assembly stores; game identity/context leaves controlled'},indent=2),encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    env = minimal_test_environment(Path(args.dotnet).resolve(), output)
    result = subprocess.run([args.dotnet, 'run', '--project', str(output / 'Tests.csproj'), '-c', 'Release'],
                            cwd=output, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
    log = result.stdout + result.stderr
    (output / 'run.log').write_text(log, encoding='utf-8')
    print(log, end='')
    return result.returncode

if __name__ == '__main__':
    raise SystemExit(main())
