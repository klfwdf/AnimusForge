from pathlib import Path
import importlib.util,unittest
from unittest.mock import patch
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
spec=importlib.util.spec_from_file_location('inverse',HERE/'source_parity.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
SOURCE=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig')
class Guards(unittest.TestCase):
 def test_exact_whole_inverse(self):self.assertEqual(inverse.restore(SOURCE),inverse.prior())
 def test_original_prompt(self):inverse.verify()
 def test_extra_source_rejected(self):
  with self.assertRaisesRegex(AssertionError,'surrounding'):inverse.restore(SOURCE+'\n// extra\n')
 def test_modified_status_rejected(self):
  with self.assertRaisesRegex(AssertionError,'declaration'):inverse.restore(SOURCE.replace('return active || coolingDown;','return active;',1))
 def test_removed_reset_rejected(self):
  with self.assertRaises(AssertionError):inverse.restore(SOURCE.replace('\t\t_npcPersonaGeneration.Reset();','',1))
 def test_unreviewed_helper_rejected(self):
  orig=Path.read_text;target=ROOT/'MyBehavior.PersonaGeneration.cs'
  def change(p,*args,**kwargs):return orig(p,*args,**kwargs)+ ('\n// drift' if p==target else '')
  with patch.object(Path,'read_text',change):
   with self.assertRaisesRegex(AssertionError,'dependency'):inverse.restore(SOURCE)
if __name__=='__main__':unittest.main(verbosity=2)
