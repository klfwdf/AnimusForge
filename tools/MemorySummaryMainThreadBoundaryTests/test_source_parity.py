"""Read-only guards for the scoped B1 inverse; never edit production to test rejection."""
from pathlib import Path
import importlib.util
import json
import subprocess
import unittest
from unittest.mock import patch

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
spec = importlib.util.spec_from_file_location("b1_inverse_under_test", HERE / "source_parity.py")
inverse = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inverse)
SOURCE = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
REVIEW = json.loads((HERE / "source-review-b1.json").read_text(encoding="utf-8"))
BASELINE = subprocess.check_output(
    ["git", "show", REVIEW["baseline"] + ":MyBehavior.cs"], cwd=ROOT
).decode("utf-8-sig").replace("\r\n", "\n")


class InverseGuards(unittest.TestCase):
    def reject(self, source, message):
        with self.assertRaisesRegex(AssertionError, message):
            inverse.restore_memory_summary_source("MyBehavior.cs", source)

    def test_exact_whole_baseline(self):
        self.assertEqual(BASELINE, inverse.restore_memory_summary_source("MyBehavior.cs", SOURCE))

    def test_changed_accepted_body(self):
        self.reject(SOURCE.replace("_eventSourceMaterialIndexBinding.Build(source);",
                                   "_eventSourceMaterialIndexBinding.Build(null);", 1),
                    "Unreviewed B1 declaration")

    def test_added_composition_span_drift(self):
        self.reject(SOURCE.replace("item => item.Day, item => item.StableKey",
                                   "item => 0, item => item.StableKey", 1),
                    "Unreviewed B1 added source span")

    def test_duplicate_composition_span(self):
        self.reject(SOURCE + REVIEW["addedSourceSpans"][0]["text"],
                    "Unreviewed B1 added source span")

    def test_unlisted_surrounding_change(self):
        self.reject(SOURCE + "\n// unreviewed extra source\n",
                    "Unreviewed B1 surrounding source changes")

    def test_deleted_original_cannot_return(self):
        extractor = importlib.util.spec_from_file_location(
            "guard_extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
        module = importlib.util.module_from_spec(extractor)
        extractor.loader.exec_module(module)
        body = module.declaration(BASELINE, "private bool HasPastDailyMemoryDrafts(")
        self.reject(SOURCE.replace("public override void SyncData(IDataStore dataStore)",
                                   body + "\npublic override void SyncData(IDataStore dataStore)", 1),
                    "Deleted B1 declaration unexpectedly restored")

    def test_added_campaign_scope_cannot_drift(self):
        self.reject(SOURCE.replace("_campaignMemoryMaintenanceCycleActive = true;",
                                   "_campaignMemoryMaintenanceCycleActive = false;", 1),
                    "Unreviewed B1 added source span")

    def test_whole_components_and_test_inputs_are_locked(self):
        original = Path.read_text
        targets = list(REVIEW["productionDependencies"]) + [
            REVIEW["evidence"]["materials"]["runner"],
            REVIEW["evidence"]["sealing"]["harness"],
            "tools/MemorySummaryMainThreadBoundaryTests/CapturedHarness.cs.txt",
        ]
        for target in targets:
            with self.subTest(path=target):
                path = ROOT / target
                def changed(file, *args, **kwargs):
                    text = original(file, *args, **kwargs)
                    return text + "\n// unreviewed dependency\n" if file == path else text
                with patch.object(Path, "read_text", changed):
                    self.reject(SOURCE, "Unreviewed B1 (production dependency|evidence)")

    def test_obsolete_partial_cannot_reenter(self):
        original = Path.exists
        removed = ROOT / "MyBehavior.EventSourceMaterialIndex.cs"
        with patch.object(Path, "exists", lambda p: True if p == removed else original(p)):
            self.reject(SOURCE, "Obsolete B1 production file restored")


if __name__ == "__main__":
    unittest.main(verbosity=2)
