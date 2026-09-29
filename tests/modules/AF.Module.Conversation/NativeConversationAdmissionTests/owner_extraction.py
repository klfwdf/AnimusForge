"""Strict J07b admission ownership inverse; current behavior is tested with the real owner."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[4]
REVIEW=json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeTicket/source-review.json').read_text(encoding='utf-8'))
CLAIM_REVIEW=json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeDispatchClaim/source-review.json').read_text(encoding='utf-8'))
MAIN_REPLY_REVIEW=json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeMainReply/source-review.json').read_text(encoding='utf-8'))
OBSERVATION_REVIEW=json.loads((ROOT/'tests/modules/AF.Module.Conversation/NativeObservation/source-review.json').read_text(encoding='utf-8'))
def restore_packet(review,path,source):
    evidence=review['files'].get(path)
    if evidence is None: return source
    original=subprocess.check_output(['git','show',review['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(original.encode()).hexdigest()==evidence['beforeSha256'], 'J07b original digest: '+path
    if source == original: return source
    expected=original
    for edit in evidence['edits']:
        assert expected.count(edit['before'])==edit['count'], 'J07b exact replacement count: '+path
        expected=expected.replace(edit['before'],edit['after'])
    assert hashlib.sha256(expected.encode()).hexdigest()==evidence['afterSha256'], 'J07b candidate digest: '+path
    assert source==expected, 'Unreviewed J07b source drift: '+path
    return original
def restore(path,source):
    return restore_packet(REVIEW,path,restore_claim(path,source))
def restore_claim(path,source):
    return restore_packet(CLAIM_REVIEW,path,restore_main_reply(path,source))
def restore_main_reply(path,source):
    for file, digest in MAIN_REPLY_REVIEW.get('addedFiles',{}).items():
        assert hashlib.sha256((ROOT/file).read_text(encoding='utf-8-sig').encode()).hexdigest()==digest, 'Unreviewed main-reply dependency: '+file
    return restore_packet(MAIN_REPLY_REVIEW,path,restore_observation(path,source))
# Accepted-reply deferral: the completion request carries game-thread side effects that
# run only for a non-discarded dispatch (NativeTurn/run.py executes that). Project exactly
# these two reviewed additions away; everything else must still match byte-for-byte.
ACCEPTED_REPLY_ADDITIONS={'ShoutBehavior.NativeCompletion.cs':[
    '        // Game-thread side effects that must only follow an accepted (not stale, not\n'
    '        // discarded) reply: scene-action directive submit and ceremony execution order.\n'
    '        internal Action AcceptedReplySideEffects;\n',
    '    // Runs once, on the game thread, only after the reply passed stale/target checks and the\n'
    '    // action dispatch did not discard it. Failures are logged and never block memory commit.\n'
    '    private static void RunNativeAcceptedReplySideEffects(NativeConversationCompletionRequest request)\n'
    '    {\n'
    '        Action effects = request?.AcceptedReplySideEffects;\n'
    '        if (effects == null)\n'
    '            return;\n'
    '        request.AcceptedReplySideEffects = null;\n'
    '        try\n'
    '        {\n'
    '            effects();\n'
    '        }\n'
    '        catch (Exception ex)\n'
    '        {\n'
    '            Logger.Log("ShoutBehavior", "[NativeConversation] accepted-reply side effect failed open: " + ex.Message);\n'
    '        }\n'
    '    }\n'
    '\n']}
def restore_accepted_reply(path,source):
    for block in ACCEPTED_REPLY_ADDITIONS.get(path,[]):
        assert source.count(block)==1, 'Unreviewed accepted-reply drift: '+path
        source=source.replace(block,'')
    return source
def restore_observation(path,source):
    source=restore_accepted_reply(path,source)
    if path == "ShoutBehavior.cs":
        import importlib.util
        spec=importlib.util.spec_from_file_location("turn_projection",ROOT/"tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/turn_extraction.py")
        turn=importlib.util.module_from_spec(spec);spec.loader.exec_module(turn)
        source=turn.projected_source(source)
    return restore_packet(OBSERVATION_REVIEW,path,source)
if __name__=='__main__':
    for path in REVIEW['files']:
        restore(path,(ROOT/path).read_text(encoding='utf-8-sig'))
        print('PASS exact admission-owner inverse '+path)
