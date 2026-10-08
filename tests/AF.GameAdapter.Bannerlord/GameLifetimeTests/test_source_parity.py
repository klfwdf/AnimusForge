"""Focused guards for the reviewed J06 call-site and J07a path-only inverses."""
from pathlib import Path
import importlib.util
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location("lifetime_inverse", Path(__file__).with_name("source_parity.py"))
INVERSE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INVERSE)


from af2_terminal_migration_review import historical_source, historical_test_case

@historical_test_case
class SourceInverseTests(unittest.TestCase):
    def setUp(self):
        self.source = historical_source("ShoutBehavior.cs")

    def test_current_source_restores_exact_baseline(self):
        self.assertEqual(INVERSE.restore("ShoutBehavior.cs", self.source), INVERSE.old("ShoutBehavior.cs"))

    def test_changed_eligibility_target_is_rejected(self):
        old = "MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding)"
        self.assertEqual(self.source.count(old), 1)
        changed = self.source.replace(old, "MyBehavior.CapturePromptRuleEligibility(null, targetCharacter, runtimeTargetBinding)", 1)
        with self.assertRaisesRegex(AssertionError, "Unreviewed J07b source drift"):
            INVERSE.restore("ShoutBehavior.cs", changed)

    def test_removing_eligibility_capture_is_rejected(self):
        old = "AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, MyBehavior.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));"
        self.assertEqual(self.source.count(old), 1)
        changed = self.source.replace(old, "AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding);", 1)
        with self.assertRaisesRegex(AssertionError, "Unreviewed J07b source drift"):
            INVERSE.restore("ShoutBehavior.cs", changed)

    def test_unrelated_source_drift_is_rejected(self):
        with self.assertRaisesRegex(AssertionError, "Unreviewed J07b source drift"):
            INVERSE.restore("ShoutBehavior.cs", self.source + "\n// unrelated drift\n")

    def test_runner_edits_beyond_path_relocation_are_rejected(self):
        original = Path.read_text
        target = ROOT / "tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/run_commit.py"
        def changed(path, *args, **kwargs):
            text = original(path, *args, **kwargs)
            return text + ("\n# unreviewed change\n" if path == target else "")
        with patch.object(Path, "read_text", changed):
            with self.assertRaisesRegex(AssertionError, "Unreviewed game lifetime dependency"):
                INVERSE.restore("ShoutBehavior.cs", self.source)


class FreshOutputRunnerInverseTests(unittest.TestCase):
    """Actual physical A4 runners still reach their original whole-file guards."""
    CASES = (
        ("run.py", "056687f47d0812d022c7bc4fffaf9c3cf7537f7c91e7b112f3e8c02395b21ab1"),
        ("run_memory.py", "bee94077ca75a97e112dcce95e7ce242086d4751ae29933d8bdd2605061c6abc"),
    )

    def test_actual_fresh_runners_reach_original_locator_targets(self):
        import hashlib
        for name, target in self.CASES:
            with self.subTest(runner=name):
                relative = "tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/" + name
                source = (ROOT / relative).read_text(encoding="utf-8-sig")
                restored = INVERSE._restore_round2_current_paths(relative, source)
                self.assertEqual(hashlib.sha256(restored.encode()).hexdigest(), target)

    def test_actual_fresh_runners_reach_original_dependency_targets(self):
        import hashlib
        import json
        review = json.loads(Path(__file__).with_name("source-review.json").read_text(encoding="utf-8"))
        for name, _ in self.CASES:
            with self.subTest(runner=name):
                relative = "tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/" + name
                source = (ROOT / relative).read_text(encoding="utf-8-sig")
                restored = INVERSE.restore_relocation_runner(relative, source)
                restored = restored.replace("parents[3]", "parents[2]").replace(
                    "tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/", "tools/GameLifetimeTests/")
                for historical, current in INVERSE.RUNNER_PATH_EDITS.items():
                    restored = restored.replace(current, historical)
                self.assertEqual(hashlib.sha256(restored.encode()).hexdigest(), review["dependencies"][relative])

    def test_extra_body_neighbor_and_duplicate_fresh_runner_drift_is_rejected(self):
        for name, _ in self.CASES:
            relative = "tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/" + name
            source = (ROOT / relative).read_text(encoding="utf-8-sig")
            body = (source.replace("raise SystemExit(status)", "raise SystemExit(0)", 1)
                    if name == "run_memory.py" else source.replace("assert status == 0", "assert status == 1", 1))
            variants = {
                "extra": source + "\n# unapproved extra\n",
                "neighbor": source.replace("from pathlib import Path", "from pathlib import Path # altered", 1),
                "body": body,
                "duplicate": source + "\nfrom output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment\n",
            }
            for mutation, changed in variants.items():
                with self.subTest(runner=name, mutation=mutation):
                    self.assertNotEqual(changed, source)
                    with self.assertRaisesRegex(AssertionError, "Unreviewed lifetime fresh-output source"):
                        INVERSE._restore_round2_current_paths(relative, changed)


if __name__ == "__main__":
    unittest.main(verbosity=2)
