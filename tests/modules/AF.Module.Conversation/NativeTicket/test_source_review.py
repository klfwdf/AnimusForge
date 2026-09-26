"""Full-file extraction proof plus live owner wiring; never substitutes for behavior tests."""
from pathlib import Path
import importlib.util, subprocess, unittest
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse', ROOT/'tools/NativeConversationAdmissionTests/owner_extraction.py')
inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
class SourceReviewTests(unittest.TestCase):
    def test_four_live_files_restore_exact_before_source(self):
        for path in inverse.REVIEW['files']:
            raw=(ROOT/path).read_text(encoding='utf-8-sig')
            original=subprocess.check_output(['git','show',inverse.REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
            self.assertEqual(inverse.restore(path,raw),original)
    def test_unreviewed_or_lost_owner_call_rejected(self):
        path='ShoutBehavior.NativeAdmission.cs';s=(ROOT/path).read_text(encoding='utf-8-sig')
        for changed in [s+'\n// drift\n',s.replace('_nativeAdmissionOwner.Release(admission);',';',1),s.replace('_nativeAdmissionOwner.Owns(admission)','admission != null',1)]:
            with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift'):
                inverse.restore(path,changed)
    def test_active_host_has_one_owner_and_no_duplicate_slot(self):
        s=(ROOT/'ShoutBehavior.NativeAdmission.cs').read_text(encoding='utf-8-sig')
        self.assertEqual(s.count('new NativeConversationAdmissionOwner<NativeConversationAdmission>()'),1)
        self.assertNotIn('private NativeConversationAdmission _nativeConversationAdmission;',s)
        self.assertNotIn('_nativeConversationAdmissionEpoch',s)
        self.assertEqual(s.count('_nativeAdmissionOwner.EndConversation();'),1)
        self.assertEqual(s.count('_nativeAdmissionOwner.Release(admission);'),3)
        self.assertLess(s.index('throw new NativeConversationAdmissionException("native.busy"'),s.index('NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening'))
    def test_monolith_only_changes_one_reviewed_line_preserving_bytes(self):
        p=ROOT/'ShoutBehavior.cs';actual=p.read_bytes()
        raw=inverse.restore_claim('ShoutBehavior.cs',p.read_text(encoding='utf-8-sig')).replace('\n','\r\n').encode()
        self.assertEqual(actual.count(b'\r\n'),actual.count(b'\n'))
        self.assertFalse(actual.startswith(b'\xef\xbb\xbf'))
        old=subprocess.check_output(['git','show',inverse.REVIEW['baseline']+':ShoutBehavior.cs'],cwd=ROOT)
        # Git blob is LF; this worktree's pre-edit format is CRLF (no BOM).
        old=old.replace(b'\r\n',b'\n').replace(b'\n',b'\r\n')
        edit=inverse.REVIEW['files']['ShoutBehavior.cs']['edits'][0]
        self.assertEqual(raw,old.replace(edit['before'].encode(),edit['after'].encode(),1))
        self.assertEqual(raw.count(b'\r\n'),raw.count(b'\n'))
        self.assertEqual(raw.startswith(b'\xef\xbb\xbf'),old.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
