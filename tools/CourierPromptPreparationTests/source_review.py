"""Strict inverse for this Courier Prompt package; called before prior Courier parity layers."""
from pathlib import Path
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

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
 live=(ROOT/review['path']).read_text(encoding='utf-8-sig')
 assert live==expected,'Unreviewed Courier prompt source outside approved Start/Begin/Prepare/prompt boundaries'
 for path,digest in review['dependencies'].items():
  text=(ROOT/path).read_text(encoding='utf-8-sig')
  if path in ('tools/CourierPromptPreparationTests/run.py','tools/CourierPromptPreparationTests/run_liveness.py'):
   new='src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs'
   assert text.count(new)==1,'Courier prompt runner path drift: '+path
   text=text.replace(new,'Refactor/Runtime/PendingOperationRegistry.cs',1)
  assert hashlib.sha256(text.encode()).hexdigest()==digest,'Unreviewed Courier prompt dependency: '+path
 assert source in (expected,before),'Unexpected Courier prompt inverse input'
 return before

def verify():
 current=(ROOT/'CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig');before=restore(current)
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8-sig'))
 main=subprocess.check_output(['git','show',review['main']+':CourierDeliveryBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 partial=(ROOT/'CourierDeliveryBehavior.PromptPreparation.cs').read_text(encoding='utf-8-sig')
 marker='\t\tList<string> selectedRuleHits = '
 for kind,oldname,newname in [('CourierReplyGenerationRequest','BuildCourierReplyGenerationRequestOnMainThread','BuildReplyRequestFromPreparedPrompt'),('InboundLetterGenerationRequest','BuildInboundLetterGenerationRequestOnMainThread','BuildInboundRequestFromPreparedPrompt')]:
  old=ex.declaration(main,'private '+kind+' '+oldname+'(');new=ex.declaration(partial,'private '+kind+' '+newname+'(')
  assert old[old.index(marker):]==new[new.index(marker):],'Original main final request/message assembly changed: '+kind
 for name in ['BuildCourierReplyMessages','BuildInboundNpcLetterMessages']:
  assert ex.declaration(main,'private static List<object> '+name+'(')==ex.declaration(current,'private static List<object> '+name+'('),'Main message semantics changed: '+name
 # These guards fail for unreviewed unrelated edits as well as edits to either actual async consumer.
 for mutated in [current.replace('if (request == null) return;','if (false) return;',1),current+'\n// unreviewed\n',current.replace('"courier_reply_preflight"','"other"',1)]:
  try:restore(mutated)
  except AssertionError:pass
  else:raise AssertionError('Whole-file inverse accepted unreviewed mutation')
 print('PASS exact full-file Courier prompt inverse / 3 mutation guards; fixed main both final request tails and complete message builders unchanged')
if __name__=='__main__':verify()
