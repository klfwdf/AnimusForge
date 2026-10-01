from pathlib import Path
import importlib.util,subprocess,unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
import sys
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
from turn_extraction import projected_source
from af2_terminal_migration_review import historical_test_case

@historical_test_case
class RawPresentationSourceTests(unittest.TestCase):
    def test_full_source_and_observer_body_preserved(self):
        s=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig');old=subprocess.check_output(['git','show','00574541:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        self.assertEqual(inverse.restore_observation('ShoutBehavior.cs',s),old)
        marker='private static void SubmitNativeConversationSceneActionObservation('
        self.assertEqual(ex.declaration(s,marker),ex.declaration(old,marker))
        marker='private void TrySpeakNativeConversationReplyWithTts('
        self.assertEqual(ex.declaration(s,marker),ex.declaration(old,marker))
    def test_side_effects_stay_inside_validated_callback_in_order(self):
        s=projected_source((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig'));body=ex.declaration(s,'private async Task<string> SubmitNativeConversationTextInternalAsync(')
        start=body.index('string nativeMainReplyTargetUnavailableReason = "";');end=body.index('if (!nativeMainReplyTargetAvailable)',start);raw=body[start:end]
        tokens=['if (!IsNativeConversationAdmissionCurrent(admission,','TryProcessNativeConversationRawMeetingTauntTags(','TryProcessNativeConversationSceneTauntTags(','SubmitNativeConversationSceneActionObservation(','cleaned = StripStageDirectionsForPassiveShout(','TrySpeakNativeConversationReplyWithTts(','return true;']
        positions=[raw.index(t) for t in tokens];self.assertEqual(positions,sorted(positions))
        self.assertEqual(body.count('SubmitNativeConversationSceneActionObservation('),1)
        self.assertEqual(body.count('TrySpeakNativeConversationReplyWithTts('),1)
    def test_unrelated_or_lost_observer_edits_rejected(self):
        live=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
        original=Path.read_text;target=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs'
        call='SubmitNativeConversationSceneActionObservation(postprocessReply, nativeTargetAgentIndex);'
        self.assertIn(call,target.read_text(encoding='utf-8-sig'))
        def changed(path,*args,**kwargs):
            result=original(path,*args,**kwargs)
            return result.replace(call,';',1) if path==target else result
        with patch.object(Path,'read_text',changed):
            with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift: turn dependency'):
                inverse.restore_observation('ShoutBehavior.cs',live)
        with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift: (turn entry|relocated host input)'):
            inverse.restore_observation('ShoutBehavior.cs',live.replace('return NativeConversationTurnCoordinator.RunAsync(', 'return MissingTurnCoordinator.RunAsync(',1))
    def test_crlf_and_bom_unchanged(self):
        raw=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_bytes();self.assertEqual(raw.count(b'\r\n'),raw.count(b'\n'));self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
