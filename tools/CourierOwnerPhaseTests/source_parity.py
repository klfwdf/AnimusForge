"""Reviewed exact owner-phase lifetime fix; reject unrelated postprocess changes."""
from pathlib import Path
import subprocess,hashlib,json,importlib.util
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent;PATH='CourierDeliveryBehavior.DetachedPostprocess.cs'
spec=importlib.util.spec_from_file_location('owner_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
def old():return subprocess.check_output(['git','show','4140bd04:'+PATH],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def verify():
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for p,h in review['dependencies'].items():assert hashlib.sha256((ROOT/p).read_text(encoding='utf-8-sig').encode()).hexdigest()==h,'Unreviewed owner phase dependency: '+p
 actual=(ROOT/PATH).read_text(encoding='utf-8-sig');prior=old()
 for before,after in review['exactEdits']:
  assert prior.count(before)==1;prior=prior.replace(before,after,1)
 assert actual==prior,'Unreviewed owner phase surrounding change'
 return actual
def restore_method(method):
 actual=verify();sig='private async Task<T> RunCourierOwnerPhaseAsync<T>('
 assert method==e.declaration(actual,sig),'Unreviewed owner phase declaration'
 return e.declaration(old(),sig)
if __name__=='__main__':verify();print('PASS exact owner phase cancellation/timeout delta; other postprocess code unchanged')
