"""Reviewed exact owner-phase lifetime fix; reject unrelated postprocess changes."""
from pathlib import Path
import subprocess,hashlib,json,importlib.util
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent;PATH='src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs'
spec=importlib.util.spec_from_file_location('owner_decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');e=importlib.util.module_from_spec(spec);spec.loader.exec_module(e)
def old():return subprocess.check_output(['git','show','4140bd04:CourierDeliveryBehavior.DetachedPostprocess.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def restore_j17_dequeue(method):
 # J17's production queue retirement callback; old outcome/cancellation tests
 # compare the receipt algorithm, while current run.py executes this callback.
 edits=[
  (', CancellationToken cancellationToken, Action onDequeued = null)', ', CancellationToken cancellationToken)'),
  ('        void Invoke()\n        {\n            try\n            {\n', '        void Invoke()\n        {\n'),
  ('            }\n            finally { onDequeued?.Invoke(); }\n        }\n', '        }\n'),
 ]
 for after,before in edits:
  assert method.count(after)==1, 'Unreviewed J17 Courier dequeue delta: '+after
  method=method.replace(after,before,1)
 return method

def verify():
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 # The executable runner moved three times; lock the unchanged behavioral harness,
 # not machine paths/output allocation. Compare the real phase rather than unrelated
 # postprocess members subsequently extracted by J17.
 p='tests/modules/AF.Module.Conversation/CourierOwnerPhaseTests/Harness.cs.txt'
 assert hashlib.sha256((ROOT/p).read_text(encoding='utf-8-sig').encode()).hexdigest()==review['dependencies'][p], 'Unreviewed owner phase dependency: '+p
 life_spec=importlib.util.spec_from_file_location('lifetime_inverse',ROOT/'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(life_spec);life_spec.loader.exec_module(life)
 sig='private async Task<T> RunCourierOwnerPhaseAsync<T>('
 actual=life.restore_method(PATH,sig,e.declaration((ROOT/PATH).read_text(encoding='utf-8-sig'),sig));prior=e.declaration(old(),sig)
 for before,after in review['exactEdits']:
  assert prior.count(before)==1;prior=prior.replace(before,after,1)
 assert actual==prior,'Unreviewed owner phase surrounding change'
 return actual
def restore_method(method):
 actual=verify();sig='private async Task<T> RunCourierOwnerPhaseAsync<T>('
 life_spec=importlib.util.spec_from_file_location('lifetime_method_inverse',ROOT/'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/source_parity.py');life=importlib.util.module_from_spec(life_spec);life_spec.loader.exec_module(life)
 method=life.restore_method(PATH,sig,method)
 assert method==actual,'Unreviewed owner phase declaration'
 return e.declaration(old(),sig)
if __name__=='__main__':verify();print('PASS exact owner phase cancellation/timeout delta; phase lifecycle and receipt deltas unchanged; unrelated postprocess owned separately')
