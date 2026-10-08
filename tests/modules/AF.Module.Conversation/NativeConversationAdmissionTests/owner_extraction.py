"""Strict J07b admission ownership inverse; current behavior is tested with the real owner."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[4]
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
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
ADMISSION_APPLICATION_BEFORE = '4fec662a60b022f6f16faffcec1c26117ea27bf5'
ADMISSION_APPLICATION_FREEZE = 'c4de2460fbefdc4b2769a9dbb4451376da56b2dc'
ADMISSION_FACADE_PATH = 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs'
ADMISSION_APPLICATION_PATH = 'src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs'

def restore_admission_application(source):
    """Strict contextual owner-move inverse before the unchanged lifetime oracle.

    The live adapter is verified even though its algorithms no longer reside in
    the ABI facade. This is historical proof only, never a current behavior input.
    """
    import difflib
    def committed(revision, path):
        return subprocess.check_output(['git','show',revision+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    adapter=current_source_path(ROOT,ADMISSION_APPLICATION_PATH).read_bytes().decode('utf-8-sig').replace('\r\n','\n')
    assert adapter == committed(ADMISSION_APPLICATION_FREEZE,ADMISSION_APPLICATION_PATH), 'Unreviewed J17 Native lifetime delta: actual admission application'
    before=committed(ADMISSION_APPLICATION_BEFORE,ADMISSION_FACADE_PATH)
    after=committed(ADMISSION_APPLICATION_FREEZE,ADMISSION_FACADE_PATH)
    before_lines=before.splitlines(keepends=True);after_lines=after.splitlines(keepends=True)
    groups=list(difflib.SequenceMatcher(a=before_lines,b=after_lines,autojunk=False).get_grouped_opcodes(3))
    for group in reversed(groups):
        old=''.join(before_lines[group[0][1]:group[-1][2]])
        new=''.join(after_lines[group[0][3]:group[-1][4]])
        assert new and source.count(new)==1, 'Unreviewed J17 Native lifetime delta: admission facade context'
        source=source.replace(new,old,1)
    assert source==before, 'Unreviewed J17 Native lifetime delta: unrelated admission facade drift'
    return source

def restore_request_lifetime(path,source):
    # Approved J17 request cancellation is tested by the real transport suite.
    # Keep the original admission/main-reply hashes; reverse exact additions only.
    if path == 'ShoutBehavior.NativeMainReply.cs':
        after='CallNativeConversationApiAsync(messages, onStreamText, _admission.Lifetime.Token)'
        assert source.count(after)==1, 'Unreviewed J17 Native lifetime delta: main reply token'
        return source.replace(after,'CallNativeConversationApiAsync(messages, onStreamText)',1)
    if path != 'ShoutBehavior.NativeAdmission.cs':return source
    if 'private readonly NativeAdmissionApplicationAdapter NativeAdmissions;' in source:
        source=restore_admission_application(source)
    # These two approved read-only UI observations postdate the unchanged claim oracle.
    # Remove complete, uniquely matched declarations only; never weaken action admission.
    observation_blocks=[
        '    // UI tick observation only. Submission still uses the full target validation\n'
        '    // above; never run its target/agent resolution every frame just to grey a button.\n'
        '    internal static bool IsNativeConversationBackendBusyForUi()\n'
        '    {\n'
        '        ShoutBehavior owner = CurrentInstance;\n'
        '        NativeConversationAdmission admission = owner?._nativeAdmissionOwner.Current;\n'
        '        return admission != null && admission.Lifetime?.Token.IsCancellationRequested != true\n'
        '            && owner._nativeAdmissionOwner.Owns(admission)\n'
        '            && owner.IsNativeConversationContextStampCurrent(admission);\n'
        '    }\n\n',
        '        // Mode text is scoped to the conversation/NPC, not to one request revision.\n'
        '        // Read-only display observation: never use this weaker check for action dispatch.\n'
        '        internal bool HasCurrentConversationContext()\n'
        '            => _snapshot != null && _owner.IsNativeConversationContextCurrent(_snapshot, out _);\n',
    ]
    if 'IsNativeConversationBackendBusyForUi()' in source or 'HasCurrentConversationContext()' in source:
        for block in observation_blocks:
            assert source.count(block)==1, 'Unreviewed J17 Native lifetime delta: read-only observation declaration'
            source=source.replace(block,'',1)
    edits=[
        ('        internal AnimusForge.Refactor.Runtime.ConversationRequestLifetime Lifetime;\n','',1),
        ('            owner._nativeAdmissionOwner.Current?.Lifetime?.Retire();\n','',1),
        ('                using IDisposable requestWorker = admission.Lifetime.Enter();\n'
         '                using IDisposable cancellationScope = LlmNonStreamingTransport.PushOwnerCancellation(admission.Lifetime.Token);\n','',1),
        ('\n            admission.Lifetime.Retire();\n','\n',1),
        ('        _nativeAdmissionOwner.Current?.Lifetime?.Retire();\n'
         '        admission.Lifetime = new AnimusForge.Refactor.Runtime.ConversationRequestLifetime();\n','',1),
        ('\n                admission.Lifetime.Retire();\n','\n',1),
        ('\n            admission.Lifetime.Retire();\n','\n',1),
        ('return admission != null && admission.Lifetime?.Token.IsCancellationRequested != true\n'
         '            && _nativeAdmissionOwner.Owns(admission)', 'return _nativeAdmissionOwner.Owns(admission)',1),
    ]
    # Two identically indented cleanup additions belong to finally and catch.
    for index,(after,before,count) in enumerate(edits):
        if index==3:count=2
        assert source.count(after)==count, 'Unreviewed J17 Native lifetime delta: '+after
        source=source.replace(after,before,1)
    return source

from af2_terminal_migration_review import terminal_review

@terminal_review
def restore(path,source):
    return restore_packet(REVIEW,path,restore_claim(path,restore_request_lifetime(path,source)))
@terminal_review
def restore_claim(path,source):
    return restore_packet(CLAIM_REVIEW,path,restore_main_reply(path,source))
@terminal_review
def restore_main_reply(path,source):
    for file, digest in MAIN_REPLY_REVIEW.get('addedFiles',{}).items():
        assert hashlib.sha256(restore_request_lifetime(file,(current_source_path(ROOT, file)).read_text(encoding='utf-8-sig')).encode()).hexdigest()==digest, 'Unreviewed main-reply dependency: '+file
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
@terminal_review
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
        restore(path,(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig'))
        print('PASS exact admission-owner inverse '+path)
