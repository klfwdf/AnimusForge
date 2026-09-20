"""J07 whole-turn movement proof, not another executable legacy implementation.

Reconstruct the old algorithm from CURRENT phase bodies. Only the reviewed new
thread captures / typed returns / field storage are projected away. Existing old
boundary fixtures use this projection; NativeTurn executes the new scheduling.
"""
from pathlib import Path
import hashlib, importlib.util, json, re, subprocess

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('turn_decl', ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
REVIEW = json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeTurn/source-review.json').read_text())
OLD_SIGNATURE = 'private async Task<string> SubmitNativeConversationTextInternalAsync('
NEW_SIGNATURE = 'private Task<string> SubmitNativeConversationTextInternalAsync('

def tokens(text):
    # Preserve literals and token order; ignore only whitespace/comments after movement.
    return re.findall(r'@?"(?:\\.|[^"\\])*"|\w+|[^\s]', re.sub(r'//[^\n]*', '', text))

def projected_source(source):
    original = subprocess.check_output(['git','show',REVIEW['baseline']+':ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    if source == original: return source
    for file,digest in REVIEW['addedFiles'].items():
        assert hashlib.sha256((ROOT/file).read_text(encoding='utf-8-sig').encode()).hexdigest()==digest, 'Unreviewed J07b source drift: turn dependency '+file
    old = ex.declaration(original, OLD_SIGNATURE)
    entry = old.split('\n')[0].replace('private async Task<string>','private Task<string>')+'\n\t{\n\t\treturn NativeConversationTurnCoordinator.RunAsync(new NativeConversationTurnHost(this, admission,\n\t\t\tplayerText, onStreamText, onPostprocessStarted, onMainReplyReady, npcInitiatedOpening));\n\t}'
    assert source == original.replace(old,entry,1), 'Unreviewed J07b source drift: turn entry/surroundings'
    parts = {}
    for file in REVIEW['addedFiles']:
        if not file.startswith('ShoutBehavior.NativeTurn'): continue
        text = (ROOT/file).read_text(encoding='utf-8-sig')
        for name in re.findall(r'(?:public async Task<NativeConversationTurnStep>|private void) (\w+)\(\)',text):
            signature = ('public async Task<NativeConversationTurnStep> ' if name.endswith('Async') else 'private void ')+name+'()'
            body = ex.declaration(text,signature).split('{',1)[1].rsplit('}',1)[0]
            body = body.replace('_owner.','').replace('new NativeConversationMainReplyHost(_owner,','new NativeConversationMainReplyHost(this,')
            body = re.sub(r'return NativeConversationTurnStep.Stop\((.*?)\);',r'return \1;',body)
            body = body.replace('return NativeConversationTurnStep.Continue();','')
            parts[name] = body
    for method in ['BuildPromptAsync','PostprocessAndCommitAsync']:
        for capture in ['CapturePromptRules','CapturePromptMessages','CapturePostprocessRules','CapturePostprocessTargets','CaptureDirectSceneCommand']:
            pattern = r'if \(!await CaptureOnGameThreadAsync\("[^"]+", '+capture+r'\).ConfigureAwait\(false\)\) return "";'
            parts[method] = re.sub(pattern,lambda _: parts[capture],parts[method])
    # Preserve the original postprocess call's complete argument vector, while the
    # executable NativeTurn test covers prepare -> network -> guarded completion.
    commit = parts['PostprocessAndCommitAsync']
    start = commit.index('SceneActionPostprocessWorkItem workItem = null;')
    end_marker = ').ConfigureAwait(false)) return "";'
    end = commit.index(end_marker,commit.index('postprocessed = CompleteSceneUnifiedActionPostprocess',start))+len(end_marker)
    call = re.search(r'workItem = PrepareSceneUnifiedActionPostprocess\(([^\n]+)\);',commit[start:end]).group(1)
    commit = commit[:start]+'postprocessed = TryRunSceneUnifiedActionPostprocess('+call+');'+commit[end:]
    parts['PostprocessAndCommitAsync'] = commit
    current = ''.join(parts[name] for name in ['PrepareAsync','BuildPromptAsync','ReceiveAndPresentAsync','PostprocessAndCommitAsync'])
    # The weekly capture gains the same admission guard as the adjacent captures.
    current = current.replace('() => IsNativeConversationAdmissionCurrent(admission, out _)\n                ? MyBehavior.CaptureWeeklyPromptSnapshotForExternal(targetHero, targetCharacter)\n                : MyBehavior.WeeklyPromptSnapshot.Empty,','() => MyBehavior.CaptureWeeklyPromptSnapshotForExternal(targetHero, targetCharacter),')
    # Name resolution is captured once on the game thread instead of consulting a
    # possibly cold Culture/NameGenerator cache again from the presentation worker.
    current = re.sub(r'nativeHistoryDisplayName = GetSceneNpcHistoryNameForPrompt\(npc\);', '', current)
    current = current.replace('BuildSceneSingleNpcTaskSystemBlock(nativeHistoryDisplayName,', 'BuildSceneSingleNpcTaskSystemBlock(GetSceneNpcHistoryNameForPrompt(npc),')
    current = current.replace('string postprocessNpcName = nativeHistoryDisplayName;', 'string postprocessNpcName = GetSceneNpcHistoryNameForPrompt(npc);')
    expected = old.split('{',1)[1].rsplit('}',1)[0]
    # Both sides use captured identity for diagnostics. This is not prompt content.
    expected = expected.replace('(targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown")','nativeTargetLog')
    # Moving request-local variables to phase-owned storage changes declarations,
    # not the expressions, order, or data passed to domain owners.
    fields = {}
    for file in REVIEW['addedFiles']:
        if file.startswith('ShoutBehavior.NativeTurn'):
            text=(ROOT/file).read_text(encoding='utf-8-sig')
            fields.update({name:typ for typ,name in re.findall(r'^        private ([\w.]+(?:<[^;=\n]+>)?(?:\[\])?) (\w+);$',text,re.M)})
    for name,typ in fields.items():
        expected = re.sub(r'(?m)^(\s*)'+re.escape(typ)+r' '+name+r' = ',r'\1'+name+' = ',expected)
        expected = re.sub(r'out var '+name+r'\b','out '+name,expected)
    assert tokens(current)==tokens(expected), 'Native turn algorithm/argument/order drift outside reviewed scheduling changes'
    return original

if __name__ == '__main__':
    projected_source((ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig'))
    print('PASS current phase algorithms reconstruct the pre-extraction turn; new scheduling tested separately')
