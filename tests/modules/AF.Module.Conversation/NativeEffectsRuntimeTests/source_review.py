"""Exact method-body inverse against the reviewed pre-extraction payloads; no hash refresh."""
import importlib.util,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
base=ROOT/'artifacts/af2-host-terminal-closeout/line-c'
source=ROOT/'src/modules/AF.Module.Conversation/Channels/Native'
pairs=[('ShoutBehavior.NativeGameEffects.cs','NativeConversationGameEffectsRuntime.cs'),('ShoutBehavior.NativeActionCommit.cs','ShoutBehavior.NativeActionCommit.cs'),('ShoutBehavior.NativeCompletion.cs','ShoutBehavior.NativeCompletion.cs'),('ShoutBehavior.NativeActionDispatch.cs','ShoutBehavior.NativeActionDispatch.cs')]
def tokens(s):
    return re.findall(r'"(?:[^"\\]|\\.)*"|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|[^\s]',s)
count=0
for oldfile,newfile in pairs:
    before=(base/(oldfile+'.effects-baseline.txt')).read_text(encoding='utf-8-sig')
    after=(source/newfile).read_text(encoding='utf-8-sig')
    # Preserve method body exactly while reviewing only explicit capability routing and exit claim.
    after=after.replace('_ports.PostMainThread(', '_mainThreadActions.Enqueue(')
    after=after.replace('_ports.RollbackPendingPlayerHistory(', 'RollbackNativeConversationPendingPlayerHistory(this, ')
    after=after.replace('_ports.IsPresentationCurrent(', '_nativeAdmissionOwner.IsPresentationCurrent(')
    after=after.replace('_ports.', '')
    after=after.replace('NativeConversationGameEffectsRuntime.NativeConversationHistoryCommitException','NativeConversationHistoryCommitException')
    after=after.replace('if (Interlocked.Exchange(ref scope.ExitClaimed, 1) != 0)\n                    return;','')
    pattern=r'(?:private|internal)\s+(?:static\s+)?(?:async\s+)?[A-Za-z_][\w<>,.\[\]? ]*?\s+(\w+)\('
    for m in re.finditer(pattern,before):
        name=m.group(1)
        # Constructors and nested local functions are not selected by this declaration matcher.
        declaration=ex.declaration(before,m.group(0))
        a=re.search(r'(?:private|internal)\s+(?:static\s+)?(?:async\s+)?[A-Za-z_][\w<>,.\[\]? ]*?\s+'+name+r'\(',after)
        assert a, 'missing method '+name
        current=ex.declaration(after,a.group(0))
        assert tokens(declaration[declaration.index('{'):])==tokens(current[current.index('{'):]), 'unreviewed body drift '+name
        count+=1
    assert 'ShoutBehavior _' not in after and '_owner' not in after, 'whole host retained'
for file,types in [('ShoutBehavior.NativeCompletion.cs',['internal sealed class NativeConversationHistoryCommitException','private sealed class NativeConversationCompletionScope','internal sealed class NativeConversationCompletionRequest']),('ShoutBehavior.NativeActionDispatch.cs',['internal sealed class NativeConversationActionDispatchException'])]:
    before=(base/(file+'.effects-baseline.txt')).read_text(encoding='utf-8-sig').replace('private sealed class NativeConversationHistoryCommitException','internal sealed class NativeConversationHistoryCommitException')
    after=(source/file).read_text(encoding='utf-8-sig').replace('NativeConversationGameEffectsRuntime.NativeConversationHistoryCommitException','NativeConversationHistoryCommitException').replace('internal int ExitClaimed;','')
    for typ in types:
        assert tokens(ex.declaration(before,typ))==tokens(ex.declaration(after,typ)), 'unreviewed compatibility type drift '+typ
print('PASS exact effects/completion/dispatch method inverses='+str(count)+'; reviewed ExitClaimed prevents duplicate exits')
