"""Exact host changes and real claim wiring, separate from runtime mutation evidence."""
from pathlib import Path
import importlib.util,subprocess,unittest
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse',ROOT/'tools/NativeConversationAdmissionTests/owner_extraction.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
class ClaimSourceTests(unittest.TestCase):
    def test_all_changes_restore_previous_source_exactly(self):
        for path in inverse.CLAIM_REVIEW['files']:
            source=(ROOT/path).read_text(encoding='utf-8-sig')
            old=subprocess.check_output(['git','show',inverse.CLAIM_REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
            self.assertEqual(inverse.restore_claim(path,source),old)
    def test_lost_start_or_expiry_gate_rejected(self):
        for path in ['ShoutBehavior.cs','ShoutBehavior.NativeAdmission.cs']:
            s=(ROOT/path).read_text(encoding='utf-8-sig')
            for before,after in [('!dispatchClaim.TryStart()','false'),('dispatchClaim.TryExpireBeforeStart()','true')]:
                self.assertIn(before,s)
                with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift'):
                    inverse.restore_claim(path,s.replace(before,after,1))
    def test_no_parallel_int_claim_and_no_format_normalization(self):
        for path,expiries in [('ShoutBehavior.cs',3),('ShoutBehavior.NativeAdmission.cs',1)]:
            raw=(ROOT/path).read_bytes();s=inverse.restore_main_reply(path,raw.decode('utf-8-sig').replace('\r\n','\n'))
            self.assertNotIn('dispatchState',s)
            self.assertEqual(s.count('var dispatchClaim = new NativeConversationDispatchClaim();'),1)
            self.assertEqual(s.count('!dispatchClaim.TryStart()'),1)
            self.assertEqual(s.count('dispatchClaim.TryExpireBeforeStart()'),expiries)
            self.assertEqual(raw.count(b'\r\n'),raw.count(b'\n'))
            self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
