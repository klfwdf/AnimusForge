"""Rebuild the reviewed unfixed intermediate candidate from exact inverse hunks, without ignored files."""
from pathlib import Path
import json,hashlib,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent

def old_source(path):
 review=json.loads((HERE/'liveness-review.json').read_text(encoding='utf-8-sig'))['files'][path]
 # The fixed historical candidate is immutable; later J17 owner movement must
 # not be mistaken for a mutation of this intentionally-red intermediate.
 fixed={'CourierDeliveryBehavior.cs':'6e419f6d17859fc49fef538e8a2ea5acf922deb3','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs':'bdf58283c1d2276388da6a4ab82040876b7c33dc'}
 source=subprocess.check_output(['git','show',fixed[path]+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 assert hashlib.sha256(source.encode()).hexdigest()==review['afterSha256'],'Unreviewed liveness candidate: '+path
 lines=source.splitlines(keepends=True)
 for hunk in reversed(review['hunks']):
  start=hunk['afterStart'];expected=hunk['after'];assert lines[start:start+len(expected)]==expected,'Liveness hunk changed'
  lines[start:start+len(expected)]=hunk['before']
 before=''.join(lines)
 assert hashlib.sha256(before.encode()).hexdigest()==review['beforeSha256'],'Liveness old source did not reproduce'
 return before

if __name__=='__main__':
 for path in ['CourierDeliveryBehavior.cs','src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs']:
  old_source(path);print('PASS portable exact old intermediate reconstruction: '+path)

if __name__=='__main__':
 import importlib.util
 spec=importlib.util.spec_from_file_location('decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 live=ex.courier_source(None)
 for name in ['PrepareAndGenerateCourierReplyOffMainThreadAsync','PrepareAndGenerateInboundLetterOffMainThreadAsync']:
  body=ex.declaration(live,'private async Task '+name+'(')
  assert 'using IDisposable requestWorker = promptRun.Lifetime.Enter();' in body, 'Courier liveness worker lifetime missing'
  assert 'IsCourierPromptRunCurrent(promptRun)' in body, 'Courier liveness owner guard missing'
 prompt=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig')
 assert 'CompleteCourierPromptSourceChanged(promptRun, input);' in prompt, 'Courier liveness source-change completion missing'
 print('PASS current Courier worker lifetime/owner/source-change consumers; behavioral liveness covered by run_liveness.py')
