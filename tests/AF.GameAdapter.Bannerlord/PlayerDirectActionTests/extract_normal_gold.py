"""Link the complete normal reward gold method, not the EconomyReplay subset."""
from pathlib import Path
import importlib.util
import sys

root = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("declaration", root / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (root / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "internal int TransferGold(Hero giver, Hero receiver, int amount, bool forceComplete)")
output = Path(sys.argv[1])
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("using System; using TaleWorlds.CampaignSystem; namespace AnimusForge { public partial class RewardSystemBehavior {\n" + method + "\n}}", encoding="utf-8")
world_root = root / "src/modules/AF.Module.WorldMap/Runtime"
world_source = (world_root / "WorldMapPartyCommandBehavior.cs").read_text(encoding="utf-8-sig")
methods = [extract.declaration(world_source, signature) for signature in [
    "private bool TryAppendQueue(", "private bool TryAppendQueueForParty(",
    "private void StopQueue(", "private void StopQueueForParty(",
    "private static List<PartyCommandEntry> ConvertGoToSettlementCommandsToAttacks(",
    "private static bool TryBuildGoToSettlementAttackCommand(",
    "private static MobileParty ResolveActorParty(Hero", "private static bool IsHeroActuallyInPlayerMainPartyRoster(",
    "private static bool IsValidNonHeroPartyFallbackParty(", "private static bool IsSupportedAttackSettlement(",
    "private static int GetDefaultAttackDaysForSettlement(", "private static bool IsExecutableCommand(",
    "private static bool IsExecutableNonHeroPartyCommand(", "private static bool IsTaskCommandForImplicitPartyCreation(",
    "private static PartyCommandEntry CloneCommand(", "private static string NormalizeTargetType(",
    "private static bool IsStopPending(", "private static bool HasFollowSiegeState(", "private static PartyCommandEntry GetCurrentCommand(",
    "private static bool TryResolveMergeToPlayerPostprocessVariant(", "private static bool TryValidatePlayerWildernessPartyForMerge(",
    "private bool CanMergeToPlayer(", "private static bool TryValidateGovernorExpeditionCandidate(",
    "private static int GetGovernorExpeditionPartyCapacity(", "private static bool TryBuildGovernorTroopTransferPlan("]]
delayed_source = (world_root / "WorldMapPartyCommandBehavior.DelayedRequests.cs").read_text(encoding="utf-8-sig")
hideout_source = (world_root / "WorldMapPartyCommandBehavior.HideoutClear.cs").read_text(encoding="utf-8-sig")
methods.append(extract.declaration(hideout_source, "private static bool MustWaitForNpcHideoutBattle("))
methods += [extract.declaration(hideout_source, signature) for signature in [
    "private static bool HideoutHasBandits(", "private static NpcHideoutClearTargetState CaptureHideoutClearTarget(",
    "private static bool CanOfferHideoutClearTarget("]]
methods += [extract.declaration(delayed_source, signature) for signature in [
    "private bool TryOpenCreateCompanionParty(", "private void QueuePendingCreateCompanionParty(",
    "private static List<PartyCommandEntry> SanitizeFollowUpCommands(",
    "private bool TryStartGovernorExpeditionRequest(", "private bool TryQueuePendingGovernorExpedition(",
    "private static bool CanCreateGovernorExpeditionNow("]]
classes = [extract.declaration((world_root / name).read_text(encoding="utf-8-sig"),
    "public sealed partial class WorldMapPartyCommandBehavior : CampaignBehaviorBase")
    for name in ["WorldMapPartyCommandBehavior.Protocol.cs", "WorldMapPartyCommandBehavior.Admission.cs"]]
(output.parent / "WorldMapOwner.cs").write_text(
    "using System; using System.Linq; using System.Collections.Generic; using System.Text.RegularExpressions; "
    "using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Party; using TaleWorlds.CampaignSystem.Settlements; "
    "using TaleWorlds.Core; using TaleWorlds.Library; using TaleWorlds.CampaignSystem.Roster; using TaleWorlds.MountAndBlade; namespace AnimusForge {\n" + "\n".join(classes)
    + "\npublic sealed partial class WorldMapPartyCommandBehavior {\n" + "\n".join(methods) + "\n}}", encoding="utf-8")
