from pathlib import Path
import hashlib, importlib.util, unittest
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[4]
spec=importlib.util.spec_from_file_location('turn',ROOT/'tools/NativeConversationAdmissionTests/turn_extraction.py')
turn=importlib.util.module_from_spec(spec);spec.loader.exec_module(turn)

class TurnSourceTests(unittest.TestCase):
    def test_complete_algorithm_and_surroundings(self):
        turn.projected_source((ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig'))

    def test_hash_refresh_cannot_hide_lost_functionality(self):
        target='ShoutBehavior.NativeTurnCommit.cs'
        original=Path.read_text
        content=original(ROOT/target,encoding='utf-8-sig')
        changed=content.replace('TtsAlreadyDispatched = nativeTtsDispatchedBeforePostprocess,','TtsAlreadyDispatched = false,',1)
        self.assertNotEqual(changed,content)
        def read(path,*args,**kwargs):
            return changed if path==ROOT/target else original(path,*args,**kwargs)
        with patch.object(Path,'read_text',read), patch.dict(turn.REVIEW['addedFiles'],{target:hashlib.sha256(changed.encode()).hexdigest()}):
            with self.assertRaisesRegex(AssertionError,'algorithm/argument/order drift'):
                turn.projected_source(original(ROOT/'ShoutBehavior.cs',encoding='utf-8-sig'))

    def test_entry_and_monolith_bytes(self):
        raw=(ROOT/'ShoutBehavior.cs').read_bytes()
        self.assertFalse(raw.startswith(b'\xef\xbb\xbf'))
        self.assertEqual(raw.count(b'\n'),raw.count(b'\r\n'))
        source=raw.decode()
        self.assertEqual(source.count('return NativeConversationTurnCoordinator.RunAsync('),1)
        self.assertNotIn(turn.OLD_SIGNATURE,source)

    def test_captured_name_and_shared_postprocess_are_wired(self):
        presentation=(ROOT/'ShoutBehavior.NativeTurnPresentation.cs').read_text(encoding='utf-8-sig')
        self.assertNotIn('GetSceneNpcHistoryNameForPrompt(',presentation)
        prompt=(ROOT/'ShoutBehavior.NativeTurnPrompt.cs').read_text(encoding='utf-8-sig')
        self.assertIn('nativeHistoryDisplayName = GetSceneNpcHistoryNameForPrompt(npc);',prompt)
        commit=(ROOT/'ShoutBehavior.NativeTurnCommit.cs').read_text(encoding='utf-8-sig')
        for name in ['PrepareSceneUnifiedActionPostprocess','TryRequestSceneUnifiedActionPostprocess','CompleteSceneUnifiedActionPostprocess']:
            self.assertEqual(commit.count(name+'('),1)
        self.assertNotIn('TryRunSceneUnifiedActionPostprocess(',commit)
        self.assertEqual(prompt.count('Task.Run(nativeHistoryWork)'),1)
        self.assertLess(prompt.index('Task.Run(nativeHistoryWork)'),prompt.index('BuildNativePromptContextScheduledAsync('))
        self.assertLess(prompt.index('BuildNativePromptContextScheduledAsync('),prompt.index('await persistedHeroHistoryTask'))
        self.assertIn('"persisted_history_accept"',prompt)

if __name__=='__main__':unittest.main(verbosity=2)
