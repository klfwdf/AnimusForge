"""Exact inverse of the six Courier history capture wiring changes; no blanket file/hash waiver."""
from pathlib import Path
import importlib.util,subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[2]
BASELINE='73774a94fc1d2fcbebc69ea221e9a906a4e70b8e'
spec=importlib.util.spec_from_file_location('decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
def old():return subprocess.check_output(['git','show',BASELINE+':CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def restore(source):
    persona_spec=importlib.util.spec_from_file_location('channel_persona_inverse',ROOT/'tools/ChannelPersonaPreparationTests/source_parity.py');persona=importlib.util.module_from_spec(persona_spec);persona_spec.loader.exec_module(persona)
    source=persona.restore('CourierDeliveryBehavior.cs',source)
    review=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
    for path,expected_hash in review['files'].items():
        text=(ROOT/path).read_text(encoding='utf-8-sig')
        if path=='tools/CourierHistoryPreparationTests/run.py':
            new='src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs'
            assert text.count(new)==1,'Courier history runner path drift'
            text=text.replace(new,'Refactor/Runtime/PendingOperationRegistry.cs',1)
        assert hashlib.sha256(text.encode()).hexdigest()==expected_hash,'Unreviewed Courier history dependency: '+path
    phase=m.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs').read_text(encoding='utf-8-sig'),'private async Task<T> RunCourierOwnerPhaseAsync<T>(')
    owner_spec=importlib.util.spec_from_file_location('courier_owner_phase_inverse',ROOT/'tools/CourierOwnerPhaseTests/source_parity.py');owner=importlib.util.module_from_spec(owner_spec);owner_spec.loader.exec_module(owner)
    phase=owner.restore_method(phase)
    assert hashlib.sha256(phase.encode()).hexdigest()==review['ownerPhaseSha256'],'Owner phase changed without history regression review'
    before=old();expected=before
    for inbound,subject,call in [(False,'recipient','BuildCourierReplyGenerationRequestOnMainThread(session, recipient, runtimeGeneration)'),(True,'sender','BuildInboundLetterGenerationRequestOnMainThread(session, sender, fallbackLetter, runtimeGeneration)')]:
        request='InboundLetterGenerationRequest' if inbound else 'CourierReplyGenerationRequest'
        needle=f'{request} request = {call};'
        new=f'''CourierPreparedHistory preparedHistory = await PrepareCourierHistoryAsync(sessionId, session, {subject}, {str(inbound).lower()}, runtimeGeneration).ConfigureAwait(false);
\t\t\tif (preparedHistory == null) return;
\t\t\t{request} request = {call[:-1]}, preparedHistory);'''
        assert expected.count(needle)==1;expected=expected.replace(needle,new)
        sig='private '+request+' '+call.split('(')[0]+'('
        prior=m.declaration(expected,sig);method=prior.replace('long runtimeGeneration)','long runtimeGeneration, CourierPreparedHistory preparedHistory)',1)
        begin=method.index('\t\tstring extraFact = ');end=method.index('\n\t\tList<string> preprocessRuleHits',begin)
        method=method[:begin]+'''\t\tstring extraFact = preparedHistory.ExtraFact;
\t\tstring historyText = preparedHistory.Text;'''+method[end:]
        expected=expected.replace(prior,method,1)
    for subject,inbound in [('recipient',False),('sender',True)]:
        needle='\t\t\tsession,\n\t\t\t'+subject+',\n'+('\t\t\tfallbackLetter,\n' if inbound else '')+'\t\t\tSaveRuntimeGuard.CaptureGeneration());'
        replacement=needle[:-2]+',\n\t\t\tCaptureCourierHistoryForLegacyEnvelope(session, '+subject+', '+str(inbound).lower()+'));'
        assert expected.count(needle)==1
        expected=expected.replace(needle,replacement,1)
    assert source==expected,'Unreviewed Courier source change beyond history wiring'
    return before

def verify():
    restore((ROOT/'CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig'))
    # The existing renderer ignores maxLines; reusing the captured path's zero preserves that behavior.
    current=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig')
    body=m.declaration(current,'private string BuildHistoryContextById(')
    assert body.count('maxLines')==1,'History maxLines semantics changed; revisit Courier parity'
    print('PASS exact whole Courier inverse; builders preserve all non-history logic; maxLines remains unused')
if __name__=='__main__':verify()
