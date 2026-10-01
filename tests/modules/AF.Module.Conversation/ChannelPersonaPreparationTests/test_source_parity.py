from pathlib import Path
import importlib.util,unittest
from unittest.mock import patch
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
spec=importlib.util.spec_from_file_location('inverse',HERE/'source_parity.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
class Guards(unittest.TestCase):
 def test_current_scope_and_historical_snapshots(self):
  for p in ['ShoutBehavior.cs','CourierDeliveryBehavior.cs']:self.assertEqual(inverse.restore(p,(ROOT/p).read_text(encoding='utf-8-sig')),inverse.old(p))
 def test_extra_shout_rejected(self):
  with self.assertRaisesRegex(AssertionError,'Unreviewed Shout'):inverse.restore('ShoutBehavior.cs',(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig')+'\n// extra')
 def test_extra_courier_rejected(self):
  with self.assertRaisesRegex(AssertionError,'Unreviewed Courier'):inverse.restore('CourierDeliveryBehavior.cs',(ROOT/'CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig')+'\n// extra')
 def reject_consumer(self,path,old,new):
  target=ROOT/path;read=Path.read_text
  def changed(p,*args,**kwargs):
   text=read(p,*args,**kwargs)
   if p==target:
    self.assertGreaterEqual(text.count(old),1,'Mutation must hit the actual migrated consumer')
    return text.replace(old,new,1)
   return text
  with patch.object(Path,'read_text',changed):
   with self.assertRaisesRegex(AssertionError,'consumer'):
    inverse.restore('ShoutBehavior.cs',(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig'))
 def test_hero_native_argument_rejected(self):
  self.reject_consumer('ShoutBehavior.NativeTurn.cs','EnsureNativeConversationPersonaReadyAsync(admission, onStreamText)','EnsureNativeConversationPersonaReadyAsync(targetHero, onStreamText)')
 def test_removed_courier_guard_rejected(self):
  self.reject_consumer('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs','if (admission == null) { QueueCourierPreparationFailure(promptRun); return; }','if (false) { QueueCourierPreparationFailure(promptRun); return; }')
 def test_new_dependency_drift_rejected(self):
  old=Path.read_text;target=ROOT/'src/modules/AF.Module.Conversation/Internal/PersonaGenerationWaiter.cs'
  def changed(p,*args,**kwargs):return old(p,*args,**kwargs)+ ('\n// drift' if p==target else '')
  with patch.object(Path,'read_text',changed):
   with self.assertRaisesRegex(AssertionError,'dependency'):inverse.restore('ShoutBehavior.cs',(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig'))
if __name__=='__main__':unittest.main(verbosity=2)
