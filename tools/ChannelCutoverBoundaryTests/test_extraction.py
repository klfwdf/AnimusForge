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


if __name__ == "__main__":
    unittest.main()
