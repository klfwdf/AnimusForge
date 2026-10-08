"""Exact inverse of the six Courier history capture wiring changes; no blanket file/hash waiver."""
from pathlib import Path
import importlib.util,subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[4]
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
BASELINE='73774a94fc1d2fcbebc69ea221e9a906a4e70b8e'
spec=importlib.util.spec_from_file_location('decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
def old():return subprocess.check_output(['git','show',BASELINE+':CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def restore(source):
    # Root-only equality rejected legitimate Prompt/Persona/lifecycle extraction.
    # Keep the exact historical identity, then bind this proof to the real history
    # owner and both generation consumers (run.py executes capture/resolve/accept).
    assert source==(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig'), 'Unreviewed Courier source input'
    review=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
    for path in ['src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs', 'tests/modules/AF.Module.Conversation/CourierHistoryPreparationTests/Harness.cs.txt']:
        text=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
        assert hashlib.sha256(text.encode()).hexdigest()==review['files'][path], 'Unreviewed Courier history dependency: '+path
    live=m.courier_source(None)
    for inbound,subject in [(False,'recipient'),(True,'sender')]:
        name='PrepareAndGenerateInboundLetterOffMainThreadAsync' if inbound else 'PrepareAndGenerateCourierReplyOffMainThreadAsync'
        method=m.declaration(live,'private async Task '+name+'(')
        capture='CourierPreparedHistory preparedHistory = await PrepareCourierHistoryAsync(sessionId, session, '+subject+', '+str(inbound).lower()+', runtimeGeneration).ConfigureAwait(false);'
        assert method.count(capture)==1, 'Unreviewed Courier source history capture'
        guard='if (preparedHistory == null) return;' if inbound else 'if (preparedHistory == null) { QueueCourierPreparationFailure(promptRun); return; }'
        assert method.count(guard)==1, 'Unreviewed Courier source history expiry'
        builder='BuildInboundRequestFromPreparedPrompt' if inbound else 'BuildReplyRequestFromPreparedPrompt'
        preparation='PrepareCourierPromptRequestAsync(sessionId, session, '+subject+', '+str(inbound).lower()+', '+('fallbackLetter' if inbound else 'null')+', runtimeGeneration, preparedHistory, promptRun, '+builder+')'
        assert method.count(preparation)==1 and method.index(capture)<method.index(preparation), 'Unreviewed Courier source prepared history forwarding'
        request=m.declaration((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig'),'private '+('InboundLetterGenerationRequest' if inbound else 'CourierReplyGenerationRequest')+' '+builder+'(')
        assert 'string extraFact = input.History.ExtraFact;' in request and 'string historyText = input.History.Text;' in request, 'Unreviewed Courier source prepared history reuse'
        assert 'BuildHistoryContextForExternal(' not in request, 'Unreviewed Courier source history recapture'
    before=old()
    return before

def verify():
    restore((ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig'))
    # The existing renderer ignores maxLines; reusing the captured path's zero preserves that behavior.
    current=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
    facade=m.declaration(current,'private string BuildHistoryContextById(')
    expected='private string BuildHistoryContextById(string memoryId, string memoryName, int maxLines = 0, string currentInput = null, string secondaryInput = null, bool includeCurrentActiveSceneSession = false, HistoryPromptSnapshot snapshot = null) => _memoryBusinessState.BuildHistoryContextById(_memoryHistoryContext, memoryId, memoryName, maxLines, currentInput, secondaryInput, includeCurrentActiveSceneSession, snapshot);'
    assert facade==expected,'History facade arguments/order changed; revisit Courier parity'
    owner=(ROOT/'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs').read_text(encoding='utf-8-sig')
    body=m.declaration(owner,'internal string BuildHistoryContextById(')
    assert body.count('maxLines')==1,'History maxLines semantics changed; revisit Courier parity'
    print('PASS historical baseline identity; current history owner + two generation consumers; maxLines remains unused; other root responsibilities excluded')
if __name__=='__main__':verify()
