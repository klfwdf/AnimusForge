from __future__ import annotations

import copy
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from contextlib import redirect_stderr
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import entry_inventory  # noqa: E402

EXPANDED_ENTRIES = {
    "world-simulation-worldmap": ("WarStats/AfWarStatsBehavior.cs",),
    "social-progression-reports": (
        "AnimusForgeWeeklyReportMapNotification.cs",
        "MyBehavior.WeeklyActionOutcomeReceipts.cs",
        "WeeklyReportSchedulePolicy.cs",
        "WeeklyReportTextHelper.cs",
        "TerminalWeeklyReportBrowserPopupVM.cs",
    ),
    "ui-tts-external-integration": (
        "AnimusForgeTerminalBehavior.cs",
        "AnimusForgeTerminalUiModels.cs",
        "AnimusForgeTerminalSettings.cs",
        "TerminalWeeklyReportBrowserPopupVM.cs",
        "TerminalVassalageTributeHistoryPopupVM.cs",
        "DevWeeklyReportPopup.cs",
        "WarStats/AfWarStatsMapButtonLayer.cs",
        "WarStats/AfWarStatsMapButtonVM.cs",
        "WarStats/AfWarStatsPopupVM.cs",
        "WarStats/AfWarStatsEncyclopedia.cs",
        "AnimusForge/GUI/Prefabs/AnimusForgeTerminalPopup.xml",
        "AnimusForge/GUI/Prefabs/AFWarStatsMapButton.xml",
        "AnimusForge/GUI/Prefabs/DevWeeklyReportPopup.xml",
    ),
}


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

    def test_exclusion_keeps_real_terminal_entries_and_rejects_generated_paths(self) -> None:
        for value in (
            Path("tools/Foo.cs"),
            Path("bin/Foo.cs"),
            Path("obj/Terminal.g.cs"),
            Path("Modules/AnimusForge/Foo.cs"),
            Path("terminal/scratch.cs"),
            Path("Foo.g.cs"),
            Path("原版游戏本体代码1.4.5/Foo.cs"),
        ):
            self.assertTrue(entry_inventory._excluded(value), str(value))
        for value in EXPANDED_ENTRIES["ui-tts-external-integration"]:
            self.assertFalse(entry_inventory._excluded(Path(value)), value)

    def test_terminal_weekly_and_war_candidates_are_present(self) -> None:
        inventory = entry_inventory.build_inventory(ROOT)
        for domain, paths in EXPANDED_ENTRIES.items():
            with self.subTest(domain=domain):
                self.assertTrue(set(paths) <= set(inventory.get(domain, [])))

    def test_missing_expanded_entry_rejects_catalog(self) -> None:
        original = json.loads((ROOT / entry_inventory.CATALOG_PATH).read_text(encoding="utf-8"))
        for domain, paths in EXPANDED_ENTRIES.items():
            for path in paths:
                with self.subTest(domain=domain, path=path):
                    document = copy.deepcopy(original)
                    entry = next(item for item in document["domains"] if item["id"] == domain)
                    entry["entryPaths"] = [item for item in entry["entryPaths"] if item != path]
                    with patch.object(Path, "read_text", return_value=json.dumps(document)):
                        errors = entry_inventory.check_catalog(ROOT)
                        diagnostics = io.StringIO()
                        with patch.object(sys, "argv", ["entry_inventory.py", "--project-root", str(ROOT), "--check"]), redirect_stderr(diagnostics):
                            self.assertEqual(entry_inventory.main(), 1)
                        self.assertIn(path, diagnostics.getvalue())
                    self.assertTrue(any(error.startswith(domain + " missing:") and path in error for error in errors), errors)

    def test_update_invalidates_only_expanded_entry_review_and_preserves_owners(self) -> None:
        document = {"domains": [
            {"id": "ui-tts-external-integration", "entryPaths": ["Existing.cs"],
             "ownerAssignmentState": "ASSIGNED", "entryCoverage": "COMPLETE"},
            {"id": "social-progression-reports", "entryPaths": ["MyBehavior.cs"],
             "ownerAssignmentState": "ASSIGNED", "entryCoverage": "COMPLETE"},
            {"id": "unassigned-domain", "entryPaths": [],
             "ownerAssignmentState": "ROLE_PLACEHOLDER", "entryCoverage": "REPRESENTATIVE"},
        ]}
        with tempfile.TemporaryDirectory(prefix=".fixture-entry-", dir=Path(__file__).resolve().parent) as temporary:
            project = Path(temporary)
            path = project / entry_inventory.CATALOG_PATH
            path.parent.mkdir(parents=True)
            path.write_text(json.dumps(document), encoding="utf-8")
            with patch.object(entry_inventory, "build_inventory", return_value={
                "ui-tts-external-integration": ["Existing.cs", "AnimusForgeTerminalUiModels.cs"],
                "social-progression-reports": ["MyBehavior.cs"],
            }):
                entry_inventory.update_catalog(project)
                result = json.loads(path.read_text(encoding="utf-8"))
                entry_inventory.update_catalog(project)
                self.assertEqual(result, json.loads(path.read_text(encoding="utf-8")))
            self.assertEqual(result["domains"][0]["entryCoverage"], "REPRESENTATIVE")
            self.assertEqual(result["domains"][0]["ownerAssignmentState"], "ASSIGNED")
            self.assertEqual(result["domains"][0]["entryPaths"], ["AnimusForgeTerminalUiModels.cs", "Existing.cs"])
            self.assertEqual(result["domains"][1:], document["domains"][1:])

    def test_catalog_state_matches_owner_review(self) -> None:
        document = json.loads((ROOT / "docs/phase8/full-domain-readiness-catalog.json").read_text(encoding="utf-8"))
        for domain in document["domains"]:
            self.assertEqual(domain["ownerAssignmentState"], "ASSIGNED")
            expected = "REPRESENTATIVE" if domain["id"] in EXPANDED_ENTRIES else "COMPLETE"
            self.assertEqual(domain["entryCoverage"], expected, domain["id"])

    def test_preparation_state_guard_detects_promotion(self) -> None:
        errors = entry_inventory.check_catalog(ROOT, require_preparation_state=True)
        self.assertTrue(errors)
        self.assertIn("ownerAssignmentState changed", " ".join(errors))

    def test_check_rejects_drift(self) -> None:
        self.assertEqual(entry_inventory.check_catalog(ROOT), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
