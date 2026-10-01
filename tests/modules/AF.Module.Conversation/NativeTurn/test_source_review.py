from pathlib import Path
import hashlib, importlib.util, unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[4]
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
spec=importlib.util.spec_from_file_location('turn',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/turn_extraction.py')
turn=importlib.util.module_from_spec(spec);spec.loader.exec_module(turn)

from af2_terminal_migration_review import historical_test_case

@historical_test_case
class TurnSourceTests(unittest.TestCase):
    def test_complete_algorithm_and_surroundings(self):
        turn.projected_source((ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig'))

    def test_hash_refresh_cannot_hide_lost_functionality(self):
        target='ShoutBehavior.NativeTurnCommit.cs'
        original=Path.read_text
        content=original(current_source_path(ROOT, target),encoding='utf-8-sig')
        changed=content.replace('TtsAlreadyDispatched = nativeTtsDispatchedBeforePostprocess,','TtsAlreadyDispatched = false,',1)
        self.assertNotEqual(changed,content)
        def read(path,*args,**kwargs):
            return changed if path==current_source_path(ROOT, target) else original(path,*args,**kwargs)
        with patch.object(Path,'read_text',read), patch.dict(turn.REVIEW['addedFiles'],{target:hashlib.sha256(turn.restore_remote_feature_delta(target,turn.restore_f5_network_request_delta(target,changed)).encode()).hexdigest()}):
            with self.assertRaisesRegex(AssertionError,'algorithm/argument/order drift'):
                turn.projected_source(original(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs',encoding='utf-8-sig'))

    def test_ceremony_delta_cannot_hide_lost_cleanup(self):
        target='ShoutBehavior.NativeTurnPresentation.cs'
        content=(current_source_path(ROOT, target)).read_text(encoding='utf-8-sig')
        # Historical owner proof remains intact after the reviewed remote retirement.
        content=turn.restore_remote_feature_delta(target,content)
        restored=turn.restore_ceremony_owner(target,content)
        self.assertEqual(hashlib.sha256(restored.encode()).hexdigest(),turn.REVIEW['addedFiles'][target])
        for before in ['            _ceremonyOrderOwner = null;\n',
                       '            if (!ReferenceEquals(_ceremonyOrderOwner, owner)) return;\n']:
            with self.assertRaisesRegex(AssertionError,'ceremony owner delta'):
                turn.restore_ceremony_owner(target,content.replace(before,'',1))
        # A change outside the exact delta survives inverse and fails the old digest.
        changed=content.replace('manager.ConversationEndOneShot += handler;', 'manager.ConversationEndOneShot -= handler;',1)
        self.assertNotEqual(hashlib.sha256(turn.restore_ceremony_owner(target,changed).encode()).hexdigest(),turn.REVIEW['addedFiles'][target])

    def test_entry_and_monolith_bytes(self):
        raw=(ROOT/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_bytes()
        self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
        self.assertNotIn(b'\r\r\n',raw)
        source=raw.decode()
        self.assertEqual(source.count('return NativeConversationTurnCoordinator.RunAsync('),1)
        self.assertNotIn(turn.OLD_SIGNATURE,source)

    def test_captured_name_and_shared_postprocess_are_wired(self):
        presentation=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs').read_text(encoding='utf-8-sig')
        self.assertNotIn('GetSceneNpcHistoryNameForPrompt(',presentation)
        prompt=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs').read_text(encoding='utf-8-sig')
        self.assertIn('nativeHistoryDisplayName = GetSceneNpcHistoryNameForPrompt(npc);',prompt)
        commit=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnCommit.cs').read_text(encoding='utf-8-sig')
        for name in ['PrepareSceneUnifiedActionPostprocess','TryRequestSceneUnifiedActionPostprocess','CompleteSceneUnifiedActionPostprocess']:
            self.assertEqual(commit.count(name+'('),1)
        self.assertNotIn('TryRunSceneUnifiedActionPostprocess(',commit)
        self.assertEqual(prompt.count('Task.Run(nativeHistoryWork)'),1)
        self.assertLess(prompt.index('Task.Run(nativeHistoryWork)'),prompt.index('BuildNativePromptContextScheduledAsync('))
        self.assertLess(prompt.index('BuildNativePromptContextScheduledAsync('),prompt.index('await persistedHeroHistoryTask'))
        self.assertIn('"persisted_history_accept"',prompt)

    def test_new_owner_mutation_cannot_hide_behind_legacy_inverse(self):
        from af2_f5_migration_review import OWNERS, verify_owners
        original = Path.read_text
        for name in OWNERS:
            target = current_source_path(ROOT, name)
            def changed(path, *args, **kwargs):
                value = original(path, *args, **kwargs)
                return value + "\n// unreviewed owner mutation\n" if path == target else value
            with self.subTest(owner=name), patch.object(Path, 'read_text', changed):
                with self.assertRaisesRegex(AssertionError, 'Unreviewed F5 dependency'):
                    verify_owners()

if __name__=='__main__':unittest.main(verbosity=2)
