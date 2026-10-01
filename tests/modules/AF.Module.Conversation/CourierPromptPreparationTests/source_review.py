"""Strict inverse for this Courier Prompt package; called before prior Courier parity layers."""
from pathlib import Path
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path

def restore(source):
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8-sig'))
 before=subprocess.check_output(['git','show',review['baseline']+':'+review['path']],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 assert hashlib.sha256(before.encode()).hexdigest()==review['beforeSha256'],'Courier prompt baseline changed'
 expected=before.splitlines(keepends=True)
 for hunk in reversed(review['hunks']):
  start=hunk['start'];old=hunk['before'];assert expected[start:start+len(old)]==old,'Courier prompt inverse hunk moved'
  expected[start:start+len(old)]=hunk['after']
 expected=''.join(expected)
 assert hashlib.sha256(expected.encode()).hexdigest()==review['afterSha256'],'Courier prompt review corrupt'
 # The reviewed whole root is historical, not today's mixed partial owner.
 # Existing inverse consumers need its immutable identity; current behavioral
 # acceptance is run_j17_reviewed_delta.py + run.py/run_liveness.py.
 fixed=subprocess.check_output(['git','show','6e419f6d17859fc49fef538e8a2ea5acf922deb3:'+review['path']],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 assert fixed==expected,'Reviewed Courier prompt historical candidate changed'
 current=(current_source_path(ROOT, review['path'])).read_text(encoding='utf-8-sig')
 assert source in (current,expected,before),'Unexpected Courier prompt inverse input'
 exspec=importlib.util.spec_from_file_location('prompt_decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(exspec);exspec.loader.exec_module(ex)
 live=ex.courier_source(None)
 for name,inbound in [('PrepareAndGenerateCourierReplyOffMainThreadAsync',False),('PrepareAndGenerateInboundLetterOffMainThreadAsync',True)]:
  body=ex.declaration(live,'private async Task '+name+'(')
  assert body.count('PrepareCourierPromptRequestAsync(')==1,'Courier prompt preparation consumer missing'
  assert 'runtimeGeneration, preparedHistory, promptRun, '+('BuildInboundRequestFromPreparedPrompt' if inbound else 'BuildReplyRequestFromPreparedPrompt') in body,'Courier prepared owner forwarding changed'
  assert 'IsCourierPromptRunCurrent(promptRun)' in body,'Courier prompt owner guard missing'
 return before

def verify():
 current=(ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig');restore(current)
 import main_assembly_projection
 main_assembly_projection.projected_messages()
 from unittest.mock import patch
 target=ROOT/'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs'
 original=Path.read_text
 for before,after in [('PrepareCourierPromptRequestAsync(', 'MissingCourierPromptRequest('),('runtimeGeneration, preparedHistory, promptRun, BuildReplyRequestFromPreparedPrompt', 'runtimeGeneration, null, promptRun, BuildReplyRequestFromPreparedPrompt')]:
  assert before in target.read_text(encoding='utf-8-sig'), 'Courier source mutation anchor missing'
  def changed(path,*args,**kwargs):
   text=original(path,*args,**kwargs)
   return text.replace(before,after,1) if path==target else text
  with patch.object(Path,'read_text',changed):
   try:restore(current)
   except AssertionError:pass
   else:raise AssertionError('Courier actual prompt consumer mutation accepted')
 try:restore(current+'\n// unreviewed\n')
 except AssertionError:pass
 else:raise AssertionError('Unexpected inverse source accepted')
 print('PASS immutable historical Prompt inverse identity; current prepared consumer/message owner proof; 3 source mutation guards; behavioral acceptance via run_j17_reviewed_delta.py')
if __name__=='__main__':verify()
