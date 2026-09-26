from pathlib import Path
import importlib.util,unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('inverse',Path(__file__).with_name('source_parity.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
CURRENT=(ROOT/'CourierDeliveryBehavior.cs').read_text(encoding='utf-8-sig')
class SourceGuards(unittest.TestCase):
    def test_exact_restore(self):self.assertEqual(m.restore(CURRENT),m.old())
    def test_extra_owner_edit_rejected(self):
        with self.assertRaisesRegex(AssertionError,'Unreviewed Courier source'):m.restore(CURRENT+'\n// unreviewed\n')
    def test_missing_expiry_guard_rejected(self):
        with self.assertRaisesRegex(AssertionError,'Unreviewed Courier source'):m.restore(CURRENT.replace('if (preparedHistory == null) return;','',1))
    def test_new_helper_drift_rejected(self):
        original=Path.read_text
        def changed(path,*a,**kw):
            text=original(path,*a,**kw)
            return text+'\n// unreviewed\n' if path.name=='CourierDeliveryBehavior.HistoryPreparation.cs' else text
        with patch.object(Path,'read_text',changed):
            with self.assertRaisesRegex(AssertionError,'Unreviewed Courier history dependency'):m.restore(CURRENT)
if __name__=='__main__':unittest.main(verbosity=2)
