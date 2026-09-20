"""Rebuild the reviewed unfixed intermediate candidate from exact inverse hunks, without ignored files."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def old_source(path):
 review=json.loads((HERE/'liveness-review.json').read_text(encoding='utf-8-sig'))['files'][path]
 source=(ROOT/path).read_text(encoding='utf-8-sig')
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
