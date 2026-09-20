"""Focused guards for the reviewed J06 call-site and J07a path-only inverses."""
from pathlib import Path
import importlib.util
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("lifetime_inverse", Path(__file__).with_name("source_parity.py"))
INVERSE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INVERSE)


class SourceInverseTests(unittest.TestCase):
    def setUp(self):
        self.source = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")

    def test_current_source_restores_exact_baseline(self):
        self.assertEqual(INVERSE.restore("ShoutBehavior.cs", self.source), INVERSE.old("ShoutBehavior.cs"))

    def test_changed_eligibility_target_is_rejected(self):
        old = "MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding)"
        self.assertEqual(self.source.count(old), 1)
        changed = self.source.replace(old, "MyBehavior.CapturePromptRuleEligibility(null, targetCharacter, runtimeTargetBinding)", 1)
        with self.assertRaisesRegex(AssertionError, "Unreviewed J06 Shout eligibility capture"):
            INVERSE.restore("ShoutBehavior.cs", changed)

    def test_removing_eligibility_capture_is_rejected(self):
        old = "AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));"
        self.assertEqual(self.source.count(old), 1)
        changed = self.source.replace(old, "AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding);", 1)
        with self.assertRaisesRegex(AssertionError, "Unreviewed J06 Shout eligibility capture"):
            INVERSE.restore("ShoutBehavior.cs", changed)

    def test_unrelated_source_drift_is_rejected(self):
        with self.assertRaisesRegex(AssertionError, "Unreviewed game lifetime source change"):
            INVERSE.restore("ShoutBehavior.cs", self.source + "\n// unrelated drift\n")

    def test_runner_edits_beyond_path_relocation_are_rejected(self):
        original = Path.read_text
        target = ROOT / "tools/GameLifetimeTests/run_commit.py"
        def changed(path, *args, **kwargs):
            text = original(path, *args, **kwargs)
            return text + ("\n# unreviewed change\n" if path == target else "")
        with patch.object(Path, "read_text", changed):
            with self.assertRaisesRegex(AssertionError, "Unreviewed game lifetime dependency"):
                INVERSE.restore("ShoutBehavior.cs", self.source)


if __name__ == "__main__":
    unittest.main(verbosity=2)
