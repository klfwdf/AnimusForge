"""Guard source extraction and fixture boundary without running a game or the SDK."""
import hashlib
import unittest
import run


class ExtractionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source=run.extractor.source('ShoutBehavior.cs',run.BASELINE)
        cls.original=run.extractor.declaration(cls.source,run.SIGNATURE)
        cls.current,cls.has_phases=run.extract_candidate(None)

    def test_oracle_is_immutable_complete_original_method(self):
        self.assertEqual(hashlib.sha256(self.original.encode()).hexdigest(),'961a59f6d48c60bd93b60b67f8438a692c2a02f3abf4e297de9f2b3626d166a3')
        self.assertTrue(self.original.rstrip().endswith('return text22;\n\t}'))

    def test_candidate_includes_actual_phase_declarations(self):
        self.assertTrue(self.has_phases)
        for name in ['TryRunSceneUnifiedActionPostprocess','PrepareSceneUnifiedActionPostprocess','TryRequestSceneUnifiedActionPostprocess','CompleteSceneUnifiedActionPostprocess']:
            self.assertIn(name+'(',self.current)

    def test_candidate_excludes_queue_and_game_loop(self):
        self.assertNotIn('QueueSceneUnifiedActionPostprocess(',self.current)
        self.assertNotIn('OnMissionTick(',self.current)

    def test_template_does_not_embed_the_oracle(self):
        template=(run.HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
        for key in ['BASELINE','CANDIDATE','HELPERS']:
            self.assertEqual(template.count('@@'+key+'@@'),1)
        self.assertNotIn('kingdomVassalageRuleInjected =',template)

    def test_helpers_are_generated_from_baseline(self):
        helpers=run.stub_helpers(self.source,self.original)
        self.assertIn('protected static string NormalizeRewardPostprocessTagsForScene(',helpers)
        self.assertIn('availableGold',helpers)
        self.assertIn('MarkWeeklyMemoryMaterialTriggerForScene',helpers)

    def test_helpers_keep_production_rule_and_output_merges(self):
        helpers=run.stub_helpers(self.source,self.original)
        for signature in ['private static string BuildPostprocessRuleTextForScene(', 'private static List<PostprocessRuleEntry> MergePostprocessRulesForScene(', 'private static string MergeNormalizedPostprocessBlocksForScene(']:
            expected=run.extractor.declaration(self.source,signature).replace('private static','protected static',1)
            self.assertIn(expected,helpers)

    def test_missing_declaration_is_not_silently_accepted(self):
        with self.assertRaises(ValueError):run.extractor.declaration(self.current,'private static string MissingPostprocessor(')

    def test_literal_braces_do_not_truncate_extraction(self):
        declaration='private static string Sample() { var x = "}"; /* } */ return "{"; }'
        self.assertEqual(run.extractor.declaration(declaration,'private static string Sample('),declaration)

if __name__=='__main__':unittest.main()
