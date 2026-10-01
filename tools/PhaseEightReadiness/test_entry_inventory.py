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
    # Remote vote-interest owner is inventoried, not accepted as reviewed policy gameplay.
    "policy-political": (
        "PolicySystem/Core/CustomPolicyBehavior.VoteInterest.cs",
    ),
    # J12 Economy moved real owners into the module tree and added a detached
    # projection; current owner review must be renewed before COMPLETE returns.
    "economy-reward-debt": (
        "src/modules/AF.Module.Economy/Authorization/RewardSystemBehavior.EconomyAssetAuthorization.cs",
        "src/modules/AF.Module.Economy/Execution/Hero/RewardSystemBehavior.EconomyReplay.cs",
        "src/modules/AF.Module.Economy/Execution/Merchant/RewardSystemBehavior.EconomyMerchantReplay.cs",
        "src/modules/AF.Module.Economy/Execution/Party/RewardSystemBehavior.EconomyPartyReplay.cs",
        "src/modules/AF.Module.Economy/Projection/EconomyPromptProjection.cs",
    ),
    # Newly inventoried existing Courier boundaries require renewed J10 coverage review.
    "courier-proactive-issue": (
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CommitDispatch.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.GenerationLifecycle.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.HistoryPreparation.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.LetterInventory.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PreparationAdmission.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptPreparation.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptMessages.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.ReplyWait.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.PromptSchedule.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.ProactiveLetters.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.RouteTransport.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.RuntimeTick.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionCreation.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionRegistry.cs",
        "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionTransport.cs",
    ),
    "world-simulation-worldmap": (
        "WarStats/AfWarStatsBehavior.cs",
        # Remote CivilWar v2 includes four real content/math/effect/lookup owners.
        # Catalog presence does not promote representative coverage to COMPLETE.
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarCatalog.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarEffects.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarRules.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarWorld.cs",
    ),
    "social-progression-reports": (
        "src/AF.GameAdapter.Bannerlord/UI/Weekly/AnimusForgeWeeklyReportMapNotification.cs",
        "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.WeeklyActionOutcomeReceipts.cs",
        "src/modules/AF.Module.Weekly/Generation/WeeklyFullReportCompletionOwner.cs",
        "src/modules/AF.Module.Weekly/Scheduling/WeeklyReportSchedulePolicy.cs",
        "src/modules/AF.Module.Weekly/Scheduling/WeeklyAutoScheduleOwner.cs",
        "src/modules/AF.Module.Weekly/Materials/WeeklyReportTextHelper.cs",
        "src/modules/AF.Module.Weekly/Materials/WeeklyMaterialBatchPlanner.cs",
        "src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs",
        # User-approved instant world bulletin owners; presence stays REPRESENTATIVE.
        "src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs",
        "src/modules/AF.Module.Weekly/Panel/WorldBulletinPanelVM.cs",
    ),
    "ui-tts-external-integration": (
        "src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalBehavior.cs",
        "src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs",
        "src/modules/AF.Module.UI/Settings/AnimusForgeTerminalSettings.cs",
        "src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs",
        "src/modules/AF.Module.Diplomacy/Vassalage/UI/TerminalVassalageTributeHistoryPopupVM.cs",
        "src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopup.cs",
        "WarStats/AfWarStatsMapNavigationEntry.cs",
        "WarStats/AfWarStatsPopupVM.cs",
        "WarStats/AfWarStatsEncyclopedia.cs",
        "content/modules/AF.Module.UI/GUI/Prefabs/AnimusForgeTerminalPopup.xml",
        "content/modules/AF.Module.WarStats/GUI/Brushes/AFTerminalMapBarBrushes.xml",
        "content/modules/AF.Module.Weekly/GUI/Prefabs/DevWeeklyReportPopup.xml",
    ),
}

# Existing production candidates previously masked by the unresolved root glob.
EXPANDED_ENTRIES['knowledge-persona-profile'] = EXPANDED_ENTRIES.get('knowledge-persona-profile', ()) + (
    'src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs',
)
EXPANDED_ENTRIES['economy-reward-debt'] = EXPANDED_ENTRIES.get('economy-reward-debt', ()) + (
    'src/modules/AF.Module.Economy/Host/DebtPromiseQuest.cs',
    'src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs',
    'src/modules/AF.Module.Economy/Host/PlayerRpCraftItemComponentService.cs',
    'src/modules/AF.Module.Economy/Host/PlayerRpCraftModels.cs',
    'src/modules/AF.Module.Economy/Host/PlayerRpCraftTemplateSelectorLog.cs',
    'src/modules/AF.Module.Economy/Host/TransferQuantitySpec.cs',
)
EXPANDED_ENTRIES['settlement-siege-gccz-sets'] = EXPANDED_ENTRIES.get('settlement-siege-gccz-sets', ()) + (
    'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.TownRuleMemoryEvents.cs',
)
EXPANDED_ENTRIES['social-progression-reports'] = EXPANDED_ENTRIES.get('social-progression-reports', ()) + (
    'src/modules/AF.Module.Social/Host/MarriageSceneNotificationSafety.cs',
    'src/modules/AF.Module.Social/Patience/PatienceOwner.cs',
    'src/modules/AF.Module.Social/Patience/PatienceRules.cs',
    'src/modules/AF.Module.Weekly/Generation/WeeklyGenerationAttemptOwner.cs',
    'src/modules/AF.Module.Weekly/Generation/WeeklyGenerationModels.cs',
    'src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs',
    'src/modules/AF.Module.Weekly/Generation/WeeklyNoticeStateOwner.cs',
    'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs',
    'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs',
    'src/modules/AF.Module.Weekly/Panel/WorldBulletinBodyWidget.cs',
)
EXPANDED_ENTRIES['world-simulation-worldmap'] = EXPANDED_ENTRIES.get('world-simulation-worldmap', ()) + (
    'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.Agenda.cs',
    'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.MapNotification.cs',
    'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.Propose.cs',
    'src/modules/AF.Module.Diplomacy/Agenda/VoteDealBehavior.cs',
    'src/modules/AF.Module.Diplomacy/Annexation/KingdomAnnexationDiagnosticLog.cs',
    'src/modules/AF.Module.Diplomacy/Direct/DiplomacyPeaceTermsService.cs',
    'src/modules/AF.Module.Diplomacy/Guards/DiplomacyRecentPeaceGuard.cs',
    'src/modules/AF.Module.Diplomacy/Guards/Patch_Diplomacy_GuardFactionManagerDeclareWar.cs',
    'src/modules/AF.Module.Diplomacy/Guards/Patch_Diplomacy_RegisterMakePeaceAction.cs',
    'src/modules/AF.Module.Diplomacy/Guards/PermanentAllianceGuard.cs',
    'src/modules/AF.Module.Diplomacy/Profiles/KingdomStrategicProfileBehavior.DevUi.cs',
    'src/modules/AF.Module.Diplomacy/Profiles/KingdomStrategicProfileBehavior.cs',
    'src/modules/AF.Module.Diplomacy/Vassalage/UI/TerminalVassalageTributeHistoryPopupVM.cs',
    'src/modules/AF.Module.Diplomacy/Vassalage/VassalageDiagnosticLog.cs',
    'src/modules/AF.Module.Diplomacy/World/WorldDiplomacyLlmClient.cs',
    'src/modules/AF.Module.Kingdom/CivilWar/CivilWarPoliticalRules.cs',
    'src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Actions.cs',
    'src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Coup.cs',
    'src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Events.cs',
    'src/modules/AF.Module.Kingdom/CivilWar/RestoreCoupDynastyEffect.cs',
    'src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingOwner.cs',
    'src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingRules.cs',
    'src/modules/AF.Module.Kingdom/Rebellion/RebellionRules.cs',
)


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
        self.assertIn("src/modules/AF.Module.Economy/Execution/Party/RewardSystemBehavior.EconomyPartyReplay.cs", result["economy-reward-debt"])
        self.assertIn("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.InboundCompletion.cs", result["courier-proactive-issue"])
        self.assertIn("src/modules/AF.Module.Conversation/Channels/Courier/CourierInboundCompletionCommitCoordinator.cs", result["courier-proactive-issue"])
        self.assertIn("src/modules/AF.Module.Conversation/Channels/Courier/CourierInboundCompletionReceipt.cs", result["courier-proactive-issue"])
        self.assertIn("src/modules/AF.Module.Social/Host/PlayerNotorietyBehavior.ConversationOutcomes.cs", result["social-progression-reports"])
        self.assertIn("src/AF.GameAdapter.Bannerlord/Compatibility/PlayerEncounterCompat.cs", result["game-adapter-compatibility"])
        self.assertIn("src/modules/AF.Module.Conversation/Internal/DetachedInteractionHost.cs", result["action-commit"])

    def test_report_includes_stable_source_reasons(self) -> None:
        result = entry_inventory.build_explained_inventory(ROOT)
        self.assertEqual(result, entry_inventory.build_explained_inventory(ROOT))
        economy = next(
            item for item in result["economy-reward-debt"]
            if item["path"] == "src/modules/AF.Module.Economy/Execution/Party/RewardSystemBehavior.EconomyPartyReplay.cs"
        )
        self.assertEqual(
            economy["sourceReasons"],
            ["reviewed-pattern:src/modules/AF.Module.Economy/**/*.cs"],
        )

    def test_exclusion_keeps_real_terminal_entries_and_rejects_generated_paths(self) -> None:
        for value in (
            Path("tools/Foo.cs"),
            Path("bin/Foo.cs"),
            Path("obj/Terminal.g.cs"),
            Path("Modules/AnimusForge/Foo.cs"),
            Path("terminal/scratch.cs"),
            Path("scratch/terminal/scratch.cs"),
            Path("Foo.g.cs"),
            Path("原版游戏本体代码1.4.5/Foo.cs"),
        ):
            self.assertTrue(entry_inventory._excluded(value), str(value))
        for value in EXPANDED_ENTRIES["ui-tts-external-integration"]:
            self.assertFalse(entry_inventory._excluded(Path(value)), value)

    def test_module_sources_do_not_unexclude_generated_or_deployed_files(self) -> None:
        self.assertFalse(entry_inventory._excluded(Path("src/modules/AF.Module.Conversation/Internal/DetachedInteractionHost.cs")))
        for value in (
            "Modules/AnimusForge/Foo.cs", "scratch/Modules/AnimusForge/Foo.cs",
            "src/modules/AF.Module.Conversation/bin/Foo.cs",
            "src/modules/AF.Module.Conversation/obj/Foo.cs",
            "src/modules/AF.Module.Conversation/Modules/AnimusForge/Foo.cs",
        ):
            self.assertTrue(entry_inventory._excluded(Path(value)), value)

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
                    entry["entryPaths"] = [entry_inventory.canonical_path(item) for item in entry["entryPaths"] if entry_inventory.canonical_path(item) != path]
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
            {"id": "social-progression-reports", "entryPaths": ["src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs"],
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
                "ui-tts-external-integration": ["Existing.cs", "src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs"],
                "social-progression-reports": ["src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs"],
            }):
                entry_inventory.update_catalog(project)
                result = json.loads(path.read_text(encoding="utf-8"))
                entry_inventory.update_catalog(project)
                self.assertEqual(result, json.loads(path.read_text(encoding="utf-8")))
            self.assertEqual(result["domains"][0]["entryCoverage"], "REPRESENTATIVE")
            self.assertEqual(result["domains"][0]["ownerAssignmentState"], "ASSIGNED")
            self.assertEqual(result["domains"][0]["entryPaths"], sorted(["src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs", "Existing.cs"]))
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

    def test_relocation_resolves_patterns_without_crossing_path_segments(self) -> None:
        self.assertIn("src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/SiegeAiInterventionBehavior.cs", entry_inventory._matches(ROOT, "SiegeAiInterventionBehavior*.cs"))
        self.assertIn("src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs", entry_inventory._matches(ROOT, "AnimusForgeTerminal*.cs"))
        self.assertEqual(entry_inventory._matches(ROOT, "AF.GameAdapter*.cs"), [])
        self.assertTrue(entry_inventory._excluded(Path("src/AF.GameAdapter.Bannerlord/UI/Terminal/obj/Foo.cs")))

    def test_check_rejects_drift(self) -> None:
        self.assertEqual(entry_inventory.check_catalog(ROOT), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
