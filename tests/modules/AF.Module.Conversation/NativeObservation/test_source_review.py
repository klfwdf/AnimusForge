from pathlib import Path
import importlib.util,subprocess,unittest
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse',ROOT/'tools/NativeConversationAdmissionTests/owner_extraction.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
class RawPresentationSourceTests(unittest.TestCase):
    def test_full_source_and_observer_body_preserved(self):
        s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');old=subprocess.check_output(['git','show','00574541:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        self.assertEqual(inverse.restore_observation('ShoutBehavior.cs',s),old)
        marker='private static void SubmitNativeConversationSceneActionObservation('
        self.assertEqual(ex.declaration(s,marker),ex.declaration(old,marker))
        marker='private void TrySpeakNativeConversationReplyWithTts('
        self.assertEqual(ex.declaration(s,marker),ex.declaration(old,marker))
    def test_side_effects_stay_inside_validated_callback_in_order(self):
        s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
        start=body.index('string nativeMainReplyTargetUnavailableReason = "";');end=body.index('if (!nativeMainReplyTargetAvailable)',start);raw=body[start:end]
        tokens=['if (!IsNativeConversationAdmissionCurrent(admission,','TryProcessNativeConversationRawMeetingTauntTags(','TryProcessNativeConversationSceneTauntTags(','SubmitNativeConversationSceneActionObservation(','cleaned = StripStageDirectionsForPassiveShout(','TrySpeakNativeConversationReplyWithTts(','return true;']
        positions=[raw.index(t) for t in tokens];self.assertEqual(positions,sorted(positions))
        self.assertEqual(body.count('SubmitNativeConversationSceneActionObservation('),1)
        self.assertEqual(body.count('TrySpeakNativeConversationReplyWithTts('),1)
    def test_unrelated_or_lost_observer_edits_rejected(self):
        s=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');call='SubmitNativeConversationSceneActionObservation(postprocessReply, nativeTargetAgentIndex);'
        for changed in [s.replace(call,';',1),s+'\n// drift\n']:
            with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift'):inverse.restore_observation('ShoutBehavior.cs',changed)
    def test_crlf_and_bom_unchanged(self):
        raw=(ROOT/'ShoutBehavior.cs').read_bytes();self.assertEqual(raw.count(b'\r\n'),raw.count(b'\n'));self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
