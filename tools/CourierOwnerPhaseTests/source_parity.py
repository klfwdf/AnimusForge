"""Reviewed exact owner-phase lifetime fix; reject unrelated postprocess changes."""
from pathlib import Path
import subprocess,hashlib,json,importlib.util
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent;PATH='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs'
spec=importlib.util.spec_from_file_location('owner_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
def old():return subprocess.check_output(['git','show','4140bd04:'+PATH],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def verify():
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in review['dependencies'].items():
  text=(ROOT/p).read_text(encoding='utf-8-sig')
  if p=='tools/CourierOwnerPhaseTests/run.py':
   new='src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs'
   assert text.count(new)==1,'Owner phase runner path drift'
   text=text.replace(new,'Refactor/Runtime/PendingOperationRegistry.cs',1)
  assert hashlib.sha256(text.encode()).hexdigest()==h,'Unreviewed owner phase dependency: '+p
 life_spec=importlib.util.spec_from_file_location('lifetime_inverse',ROOT/'tools/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(life_spec);life_spec.loader.exec_module(life)
 actual=life.restore(PATH,(ROOT/PATH).read_text(encoding='utf-8-sig'));prior=old()
 for before,after in review['exactEdits']:
  assert prior.count(before)==1;prior=prior.replace(before,after,1)
 assert actual==prior,'Unreviewed owner phase surrounding change'
 return actual
def restore_method(method):
 actual=verify();sig='private async Task<T> RunCourierOwnerPhaseAsync<T>('
 life_spec=importlib.util.spec_from_file_location('lifetime_method_inverse',ROOT/'tools/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(life_spec);life_spec.loader.exec_module(life)
 method=life.restore_method(PATH,sig,method)
 assert method==e.declaration(actual,sig),'Unreviewed owner phase declaration'
 return e.declaration(old(),sig)
if __name__=='__main__':verify();print('PASS exact owner phase cancellation/timeout delta; other postprocess code unchanged')
