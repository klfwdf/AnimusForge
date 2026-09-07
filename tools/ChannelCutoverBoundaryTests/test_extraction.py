"""Narrow parser checks; behavioral red/green evidence comes from run.py."""
from __future__ import annotations

import unittest

from run import declaration, extract, source


class ExtractionTests(unittest.TestCase):
    def test_strings_and_comments_do_not_end_declaration(self):
        method = '''private void M()
{
    var regular = "quoted brace } and escaped quote \\"{";
    var verbatim = @"brace } and "" quote";
    var character = '}';
    // }
    /* } { */
    if (true) { }
}'''
        self.assertEqual(method, declaration("prefix\n" + method + "\nnext", "private void M("))

    def test_optional_missing_declaration_is_empty(self):
        self.assertEqual("", declaration("class Other {}", "void Absent(", optional=True))

    def test_missing_required_declaration_fails(self):
        with self.assertRaises(ValueError):
            declaration("class Other {}", "void Absent(")

    def test_unterminated_declaration_fails(self):
        with self.assertRaises(ValueError):
            declaration("private void M() {", "private void M(")

    def test_blocks_are_contiguous_unmodified_production_substrings(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        courier = source("CourierDeliveryBehavior.cs", None)
        self.assertIn(blocks["SCENE_BLOCK"], scene)
        self.assertIn(blocks["COURIER_BLOCK"], courier)
        self.assertTrue(blocks["SCENE_BLOCK"].startswith('string output = "";'))
        self.assertTrue(blocks["COURIER_BLOCK"].startswith("if (IsCourierBridgeEnabled()"))
        self.assertEqual(2, blocks["COURIER_BLOCK"].count("await GenerateNpcReplyAsync(request).ConfigureAwait(false);"))
        self.assertIn(blocks["FAIL_METHOD"], courier)
        self.assertIn(blocks["FINALIZE_METHOD"], courier)
        self.assertIn(blocks["STATUS_ENUM"], source("Refactor/Contracts/InteractionContracts.cs", None))

    def test_prompt_factory_and_anonymous_message_adapter_are_production_declarations(self):
        blocks = extract(None)
        scene = source("ShoutBehavior.cs", None)
        for key in ("PUBLIC_SCENE_FACTORY", "PRIVATE_SCENE_FACTORY", "CREATE_MESSAGE"):
            self.assertTrue(blocks[key], key)
            self.assertIn(blocks[key], scene)
        self.assertIn(blocks["BUILD_PROMPT"], source("Refactor/Adapters/LegacyConfiguredChatGateway.cs", None))
        self.assertIn(blocks["PORTS_TYPE"], source("Refactor/Adapters/LegacyInteractionPipelineComposition.cs", None))
        self.assertIn(blocks["MAIN_COMPOSER"], source("Refactor/Adapters/LegacyDetachedPromptComposer.cs", None))
        self.assertIn(blocks["POSTPROCESS_COMPOSER"], source("Refactor/Adapters/LegacyDetachedPostprocessPromptComposer.cs", None))

    def test_known_prompt_baseline_keeps_real_public_factory_and_no_synthetic_fix(self):
        blocks = extract("92ad625a")
        self.assertEqual("", blocks["PRIVATE_SCENE_FACTORY"])
        self.assertIn(blocks["PUBLIC_SCENE_FACTORY"], source("ShoutBehavior.cs", "92ad625a"))
        self.assertNotIn("preparedMainPrompt", blocks["SCENE_BLOCK"])


if __name__ == "__main__":
    unittest.main()
