from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import entry_inventory  # noqa: E402


class EntryInventoryTests(unittest.TestCase):
    def test_inventory_is_stable_and_paths_exist(self) -> None:
        result = entry_inventory.build_inventory(ROOT)
        self.assertEqual(result, entry_inventory.build_inventory(ROOT))
        for domain_id, paths in result.items():
            self.assertEqual(paths, sorted(paths), domain_id)
            self.assertTrue(paths, domain_id)
            for relative in paths:
                self.assertTrue((ROOT / relative).is_file(), relative)
                self.assertNotIn("原版游戏", relative)
                self.assertNotIn("tools/", relative)
                self.assertNotIn("/bin/", relative)
                self.assertNotIn("/obj/", relative)

    def test_required_candidates_are_present(self) -> None:
        result = entry_inventory.build_inventory(ROOT)
        self.assertIn("RewardSystemBehavior.EconomyPartyReplay.cs", result["economy-reward-debt"])
        self.assertIn("CourierDeliveryBehavior.InboundCompletion.cs", result["courier-proactive-issue"])
        self.assertIn("Refactor/Runtime/CourierInboundCompletionCommitCoordinator.cs", result["courier-proactive-issue"])
        self.assertIn("Refactor/Runtime/CourierInboundCompletionReceipt.cs", result["courier-proactive-issue"])
        self.assertIn("PlayerNotorietyBehavior.ConversationOutcomes.cs", result["social-progression-reports"])
        self.assertIn("PlayerEncounterCompat.cs", result["game-adapter-compatibility"])
        self.assertIn("Refactor/Runtime/DetachedInteractionHost.cs", result["action-commit"])

    def test_report_includes_stable_source_reasons(self) -> None:
        result = entry_inventory.build_explained_inventory(ROOT)
        self.assertEqual(result, entry_inventory.build_explained_inventory(ROOT))
        economy = next(
            item for item in result["economy-reward-debt"]
            if item["path"] == "RewardSystemBehavior.EconomyPartyReplay.cs"
        )
        self.assertEqual(
            economy["sourceReasons"],
            ["reviewed-pattern:RewardSystemBehavior*.cs"],
        )

    def test_exclusion_covers_generated_and_terminal_paths(self) -> None:
        for value in (
            Path("tools/Foo.cs"),
            Path("bin/Foo.cs"),
            Path("Modules/AnimusForge/Foo.cs"),
            Path("AnimusForgeTerminalBehavior.cs"),
            Path("Foo.g.cs"),
            Path("原版游戏本体代码1.4.5/Foo.cs"),
        ):
            self.assertTrue(entry_inventory._excluded(value), str(value))

    def test_catalog_state_matches_owner_review(self) -> None:
        document = json.loads((ROOT / "docs/phase8/full-domain-readiness-catalog.json").read_text(encoding="utf-8"))
        for domain in document["domains"]:
            self.assertEqual(domain["ownerAssignmentState"], "ASSIGNED")
            self.assertEqual(domain["entryCoverage"], "COMPLETE")

    def test_preparation_state_guard_detects_promotion(self) -> None:
        errors = entry_inventory.check_catalog(ROOT, require_preparation_state=True)
        self.assertTrue(errors)
        self.assertIn("ownerAssignmentState changed", " ".join(errors))

    def test_check_rejects_drift(self) -> None:
        self.assertEqual(entry_inventory.check_catalog(ROOT), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
