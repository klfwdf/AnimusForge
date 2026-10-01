"""Strict source/consumer checks alongside executable old/new stage parity."""
from pathlib import Path
import importlib.util,subprocess,unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('inverse',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py');inverse=importlib.util.module_from_spec(spec);spec.loader.exec_module(inverse)
import sys
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests'))
from turn_extraction import projected_source
from af2_terminal_migration_review import historical_test_case

@historical_test_case
class MainReplySourceTests(unittest.TestCase):
    def test_full_source_restores_to_previous_candidate(self):
        live=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
        old=subprocess.check_output(['git','show','dabee763:ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        self.assertEqual(inverse.restore_main_reply('ShoutBehavior.cs',live),old)
    def test_actual_consumer_calls_stage_once_before_raw_actions(self):
        live=projected_source((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig'));edit=inverse.MAIN_REPLY_REVIEW['files']['ShoutBehavior.cs']['edits'][0]
        self.assertEqual(live.count(edit['after']),1);self.assertNotIn(edit['before'],live)
        self.assertLess(live.index('if (!nativeMainReply.CanContinue)'),live.index('string nativeMainReplyTargetUnavailableReason = "";'))
        self.assertNotIn('Task.Run',edit['after'])
    def test_changed_consumer_or_unrelated_code_rejected(self):
        live=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
        original=Path.read_text;target=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs'
        self.assertIn('if (!nativeMainReply.CanContinue)',target.read_text(encoding='utf-8-sig'))
        def changed(path,*args,**kwargs):
            result=original(path,*args,**kwargs)
            return result.replace('if (!nativeMainReply.CanContinue)','if (false)',1) if path==target else result
        with patch.object(Path,'read_text',changed):
            with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift: turn dependency'):
                inverse.restore_main_reply('ShoutBehavior.cs',live)
        with self.assertRaisesRegex(AssertionError,'Unreviewed J07b source drift: (turn entry|relocated host input)'):
            inverse.restore_main_reply('ShoutBehavior.cs',live.replace('return NativeConversationTurnCoordinator.RunAsync(', 'return MissingTurnCoordinator.RunAsync(',1))
    def test_unreviewed_stage_change_rejected(self):
        original=Path.read_text;target=ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs'
        def changed(path,*args,**kwargs):
            result=original(path,*args,**kwargs)
            return result.replace('if (!validation.IsCurrent)','if (false)',1) if path==target else result
        with patch.object(Path,'read_text',changed):
            with self.assertRaisesRegex(AssertionError,'Unreviewed main-reply dependency'):
                inverse.restore_main_reply('ShoutBehavior.cs',(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig'))
    def test_exact_byte_edit_preserves_line_endings_and_bom(self):
        current=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_bytes();self.assertEqual(current.count(b'\r\n'),current.count(b'\n'))
        raw=inverse.restore_observation('ShoutBehavior.cs',current.decode('utf-8-sig').replace('\r\n','\n')).replace('\n','\r\n').encode();old=subprocess.check_output(['git','show','dabee763:ShoutBehavior.cs'],cwd=ROOT)
        old=old.replace(b'\r\n',b'\n').replace(b'\n',b'\r\n');edit=inverse.MAIN_REPLY_REVIEW['files']['ShoutBehavior.cs']['edits'][0]
        self.assertEqual(raw,old.replace(edit['before'].replace('\n','\r\n').encode(),edit['after'].replace('\n','\r\n').encode(),1))
        self.assertEqual(raw.count(b'\r\n'),raw.count(b'\n'));self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
if __name__=='__main__':unittest.main(verbosity=2)
