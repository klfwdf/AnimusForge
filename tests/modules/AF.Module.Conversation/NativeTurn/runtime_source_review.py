"""Exact inverse of the Native turn owner move; never replace an algorithm digest."""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[4]
FOLDER = ROOT / 'src/modules/AF.Module.Conversation/Channels/Native'
BASELINE = 'd95159ec'
CAPABILITIES = {
    'ApplyNativeConversationGameActionsOnMainThreadAsync', 'BuildNativePromptContextScheduledAsync',
    'BuildRuntimeSceneMechanismPostprocessRulesForScene', 'BuildSceneFollowControlPromptInstruction',
    'BuildSceneSummonClosurePromptInstruction', 'BuildStrictSceneMessagesForNpc',
    'CaptureNativeConversationPreparation', 'EnsureNativeConversationPersonaReadyAsync',
    'IsNativeConversationAdmissionCurrent', 'LogTtsReport', 'PrepareNativeConversationPendingHistoryAsync',
    'RollbackNativeConversationPendingPlayerHistoryAsync', 'TrySpeakNativeConversationReplyWithTts',
}

def restore_runtime_move(file, source):
    assert source.count('internal sealed partial class NativeConversationTurnRuntime : INativeConversationTurnHost') == 1
    if file == 'ShoutBehavior.NativeTurn.cs':
        marker = '\npublic partial class ShoutBehavior\n{\n    private NativeConversationTurnPorts CreateNativeConversationTurnPorts()'
        assert source.count(marker) == 1
        source, factory = source.split(marker)
        bound = dict(re.findall(r'^        (\w+) = (\w+),$', factory, re.M))
        expected = {name: name for name in CAPABILITIES}
        expected.update({'Dispatch' + suffix: 'RunNativeConversationMainThreadFuncAsync'
                         for suffix in ['Validation', 'Preparation', 'HistoryWork', 'WeeklySnapshot']})
        assert bound == expected, 'native capability binding drift'
        assert factory.count('new NativeConversationMainReplyHost(this, admission, target, key, sequence)') == 1
        source = source.replace('private readonly NativeConversationTurnPorts _ports;', 'private readonly ShoutBehavior _owner;')
        source = source.replace('internal NativeConversationTurnRuntime(NativeConversationTurnPorts ports,',
                                'internal NativeConversationTurnHost(ShoutBehavior owner,')
        source = source.replace('_ports = ports ?? throw new ArgumentNullException(nameof(ports));', '_owner = owner;')
    source = source.replace('_ports.CreateMainReplyHost(admission, nativeTargetLog,',
                            'new NativeConversationMainReplyHost(_owner, admission, nativeTargetLog,')
    source = source.replace('_ports.', '_owner.')
    source = source.replace('using static AnimusForge.ShoutBehavior;\n\nnamespace AnimusForge;', 'namespace AnimusForge;')
    source = source.replace('    internal sealed partial class NativeConversationTurnRuntime : INativeConversationTurnHost',
                            '    private sealed partial class NativeConversationTurnHost : INativeConversationTurnHost')
    source = source.replace('namespace AnimusForge;\n\n', 'namespace AnimusForge;\n\npublic partial class ShoutBehavior\n{\n', 1)
    return source.rstrip() + '\n}\n'

if __name__ == '__main__':
    for file in sorted(FOLDER.glob('ShoutBehavior.NativeTurn*.cs')):
        current = file.read_text(encoding='utf-8-sig')
        old = subprocess.check_output(['git', 'show', BASELINE + ':' + file.relative_to(ROOT).as_posix()], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
        restored = restore_runtime_move(file.name, current)
        assert restored == old, 'native algorithm/arguments/order changed: ' + file.name
        # Fault insertion survives the inverse instead of disappearing in a hash refresh.
        broken = current.replace('ConfigureAwait(false)', 'ConfigureAwait(true)', 1)
        assert restore_runtime_move(file.name, broken) != old
        print('PASS byte-exact owner inverse + fault survives ' + file.name)
    ports = (FOLDER / 'NativeConversationTurnPorts.cs').read_text()
    ports = re.sub(r'//[^\n]*', '', ports)
    assert not re.search(r'\bShoutBehavior\s+\w+', ports)
    assert not re.search(r'(PrepareAsync|BuildPromptAsync|ReceiveAndPresentAsync|PostprocessAndCommitAsync)\s*[;(]', ports)
    print('PASS no host reference / whole-phase callback in Native ports')
