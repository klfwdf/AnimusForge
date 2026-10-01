"""Exact host changes and real claim wiring, separate from runtime mutation evidence."""
from pathlib import Path
import importlib.util,subprocess,unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
class ClaimSourceTests(unittest.TestCase):
    def test_all_changes_restore_previous_source_exactly(self):
        for path in inverse.CLAIM_REVIEW['files']:
            source=(ROOT/path).read_text(encoding='utf-8-sig')
            old=subprocess.check_output(['git','show',inverse.CLAIM_REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
            self.assertEqual(inverse.restore_claim(path,inverse.restore_request_lifetime(path,source)),old)
    def test_lost_start_or_expiry_gate_rejected(self):
        spec=importlib.util.spec_from_file_location('decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
        decl=importlib.util.module_from_spec(spec);spec.loader.exec_module(decl)
        source=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig')
        method=decl.declaration(source,'private Task<NativeConversationGameActionResult> ApplyNativeConversationGameActionsOnMainThreadAsync(')
        def validate_claim(body):
            assert body.count('!dispatchClaim.TryStart()')==1, 'Native dispatch start gate missing'
            assert body.count('dispatchClaim.TryExpireBeforeStart()')==3, 'Native dispatch expiry gate missing'
        validate_claim(method)
        for before,after in [('!dispatchClaim.TryStart()','false'),('dispatchClaim.TryExpireBeforeStart()','true')]:
            self.assertIn(before,method)
            with self.assertRaisesRegex(AssertionError,'Native dispatch (start|expiry) gate missing'):
                validate_claim(method.replace(before,after,1))
        path='ShoutBehavior.NativeAdmission.cs'
        source=inverse.restore_request_lifetime(path,(ROOT/path).read_text(encoding='utf-8-sig'))
        for before,after in [('!dispatchClaim.TryStart()','false'),('dispatchClaim.TryExpireBeforeStart()','true')]:
            self.assertIn(before,source)
            with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift'):
                inverse.restore_claim(path,source.replace(before,after,1))
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
