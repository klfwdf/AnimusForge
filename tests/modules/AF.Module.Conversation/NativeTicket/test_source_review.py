"""Full-file extraction proof plus live owner wiring; never substitutes for behavior tests."""
from pathlib import Path
import importlib.util, subprocess, unittest
ROOT=Path(__file__).resolve().parents[4]
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
spec=importlib.util.spec_from_file_location('inverse', ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py')
inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
from af2_terminal_migration_review import historical_test_case

@historical_test_case
class SourceReviewTests(unittest.TestCase):
    def test_four_live_files_restore_exact_before_source(self):
        for path in inverse.REVIEW['files']:
            raw=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
            original=subprocess.check_output(['git','show',inverse.REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
            self.assertEqual(inverse.restore(path,raw),original)
    def test_unreviewed_or_lost_owner_call_rejected(self):
        path='ShoutBehavior.NativeAdmission.cs';s=inverse.restore_admission_application((current_source_path(ROOT, path)).read_text(encoding='utf-8-sig'))
        for changed in [s+'\n// drift\n',s.replace('_nativeAdmissionOwner.Release(admission);',';',1),s.replace('_nativeAdmissionOwner.Owns(admission)','admission != null',1)]:
            with self.assertRaisesRegex(AssertionError,'Unreviewed (J07b source drift|J17 Native lifetime delta)'):
                inverse.restore(path,changed)
    def test_request_lifetime_inverse_rejects_lost_retirement_and_owner_token(self):
        path='ShoutBehavior.NativeAdmission.cs';source=inverse.restore_admission_application((current_source_path(ROOT, path)).read_text(encoding='utf-8-sig'))
        for before in ['admission.Lifetime.Enter()',
                       'LlmNonStreamingTransport.PushOwnerCancellation(admission.Lifetime.Token)',
                       'admission.Lifetime.Retire();',
                       'admission.Lifetime?.Token.IsCancellationRequested != true']:
            self.assertIn(before,source)
            with self.assertRaisesRegex(AssertionError,'Native lifetime delta'):
                inverse.restore_request_lifetime(path,source.replace(before,'/* lost */',1))
        # The original historical controls above still run against the exact
        # contextual inverse. Also mutate the actual migrated lifetime authority.
        from unittest.mock import patch
        target=ROOT/inverse.ADMISSION_APPLICATION_PATH
        # Physical current application reads are deliberately separate from projected legacy text.
        original_read=Path.read_bytes
        for before in ['admission.Lifetime.Enter()',
                       'LlmNonStreamingTransport.PushOwnerCancellation(admission.Lifetime.Token)',
                       'admission.Lifetime.Retire();',
                       'admission.Lifetime?.Token.IsCancellationRequested != true']:
            physical=original_read(target)
            normalized=physical.decode('utf-8-sig').replace('\r\n','\n')
            self.assertIn(before,normalized)
            mutated=physical.replace(before.encode(),b'/* lost */',1)
            self.assertNotEqual(mutated,physical)
            def changed(path,*args,**kwargs):
                return mutated if path==target else original_read(path,*args,**kwargs)
            with patch.object(Path,'read_bytes',changed):
                with self.assertRaisesRegex(AssertionError,'Native lifetime delta: actual admission application'):
                    facade=(ROOT/inverse.ADMISSION_FACADE_PATH).read_bytes().decode('utf-8-sig').replace('\r\n','\n')
                    inverse.restore_admission_application(facade)
        path='ShoutBehavior.NativeMainReply.cs';source=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
        with self.assertRaisesRegex(AssertionError,'main reply token'):
            inverse.restore_request_lifetime(path,source.replace(', _admission.Lifetime.Token','',1))

    def test_active_host_has_one_owner_and_no_duplicate_slot(self):
        s=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs').read_text(encoding='utf-8-sig')
        self.assertEqual(s.count('new NativeConversationAdmissionOwner<NativeConversationAdmission>()'),1)
        self.assertNotIn('private NativeConversationAdmission _nativeConversationAdmission;',s)
        self.assertNotIn('_nativeConversationAdmissionEpoch',s)
        self.assertEqual(s.count('CurrentInstance?.NativeAdmissions.EndConversation();'),1)
        self.assertEqual(s.count('return NativeAdmissions.CaptureNativeConversationAdmissionOnMainThread(generation,conversationEpoch,npcInitiatedOpening);'),1)
        s=(ROOT/inverse.ADMISSION_APPLICATION_PATH).read_text(encoding='utf-8-sig')
        self.assertEqual(s.count('_nativeAdmissionOwner.EndConversation();'),1)
        self.assertEqual(s.count('_nativeAdmissionOwner.Release(admission);'),3)
        self.assertLess(s.index('throw new NativeConversationAdmissionException("native.busy"'),s.index('NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening'))
    def test_monolith_only_changes_one_reviewed_line_preserving_bytes(self):
        p=ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs';actual=p.read_bytes().replace(b'\r\n',b'\n')
        raw=inverse.restore_claim('ShoutBehavior.cs',p.read_text(encoding='utf-8-sig')).encode()
        self.assertFalse(actual.startswith(b'\xef\xbb\xbf'))
        old=subprocess.check_output(['git','show',inverse.REVIEW['baseline']+':ShoutBehavior.cs'],cwd=ROOT)
        # Git normalizes LF/CRLF checkout bytes; compare the exact normalized inverse.
        old=old.replace(b'\r\n',b'\n')
        edit=inverse.REVIEW['files']['ShoutBehavior.cs']['edits'][0]
        self.assertEqual(raw,old.replace(edit['before'].encode(),edit['after'].encode(),1))
        self.assertEqual(raw.startswith(b'\xef\xbb\xbf'),old.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
