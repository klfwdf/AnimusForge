"""Check the actual My/Reward/Scene/Native/Policy consumer call boundaries.

This is a source-wiring contract, paired with the separately executable
PromptListRetrievalService contract; it does not simulate game inventory.
"""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["remove-private-filter", "remove-agent-key", "remove-policy-terms"])
args = parser.parse_args()

def method(path: str, marker: str) -> str:
    return extract.declaration((ROOT / path).read_text(encoding="utf-8-sig"), marker)

def ordered(body: str, *fragments: str) -> None:
    positions = [body.find(fragment) for fragment in fragments]
    assert all(position >= 0 for position in positions), (fragments, positions)
    assert positions == sorted(positions), (fragments, positions)

my_identity = method("MyBehavior.cs", "private string BuildNpcIdentityInfoForPrompt(")
ordered(my_identity, "GetLatestAuxiliaryMentionedEntitiesForExternal()", "GetMaxCandidateCount()", "BuildFilteredInventorySummaryForAI(npcHero, mentions, promptListMax, includePrivateBattleEquipment: includeTradePricing)")
my_settlement = method("MyBehavior.cs", "public static string BuildSettlementTransferRuntimeInstructionForExternal(")
ordered(my_settlement, "FilterSettlementTransferEntries(list2All, mentions, promptListMax)", "SettlementTransferNpcAssetsSnapshotScope", "SettlementTransferAllNpcAssetsSnapshotScope")
my_party = method("MyBehavior.cs", "public static string BuildPartyTransferRuntimeInstructionForExternal(")
ordered(my_party, "FilterPartyTransferEntries(list6All, mentions, promptListMax, isPrisoner: false)", "PartyTransferTroopsSnapshotScope", "PartyTransferAllTroopsSnapshotScope")
assert "targetAgentIndex, list6" in my_party and "targetAgentIndex, authorizedAllTroops" in my_party

reward_visible = method("RewardSystemBehavior.cs", "public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, MentionedWorldEntities mentions")
ordered(reward_visible, "PlayerVisibleEquipmentSnapshotScope", "FilterRewardItems(orderedItems, mentions, maxItems)")
reward_inventory = method("RewardSystemBehavior.cs", "public string BuildFilteredInventorySummaryForAI(")
if args.mutate == "remove-private-filter":
    reward_inventory = reward_inventory.replace("!x.IsPrivateEquipment", "true")
ordered(reward_inventory, "NpcRewardItemsAllSnapshotScope", "!x.IsPrivateEquipment", "FilterNpcRewardItemsForAssetTransfer(displayCandidates, mentions, maxItems)", "NpcRewardItemsSnapshotScope")
reward_merchant = method("RewardSystemBehavior.cs", "public string BuildFilteredSettlementMerchantInventorySummaryForAI(")
ordered(reward_merchant, "SettlementMerchantItemsAllSnapshotScope", "FilterRewardItems(allOptions, mentions, maxItems)", "SettlementMerchantItemsSnapshotScope")

scene_post = method("ShoutBehavior.cs", "internal static bool TryPrepareCourierActionPostprocessForExternal(")
ordered(scene_post, "BeginGuardrailRuntimeScope()", "GetLatestAuxiliaryMentionedEntitiesForExternal()", "NpcRewardItemsAllSnapshotScope", "FilterNpcRewardItemsForAssetTransfer(rewardAllOptions, promptListMentions, promptListMax)")
for fragment in ("PartyTransferAllTroopsSnapshotScope", "PartyTransferTroopsSnapshotScope", "SettlementTransferAllNpcAssetsSnapshotScope", "SettlementTransferNpcAssetsSnapshotScope"):
    assert fragment in scene_post, fragment
if args.mutate == "remove-agent-key":
    scene_post = scene_post.replace("targetAgentIndex, out partyTransferTroopOptions", "-1, out partyTransferTroopOptions")
assert "targetHero, targetCharacter, targetAgentIndex, out partyTransferTroopOptions" in scene_post
scene_group = method("ShoutBehavior.cs", "private async Task HandleGroupResponsePerHeroIndependent(")
assert "BeginGuardrailRuntimeScope()" in scene_group and "ctx?.MentionedEntities" in scene_group

native = method("ShoutBehavior.cs", "private async Task<string> SubmitNativeConversationTextInternalAsync(")
ordered(native, "BuildNativePromptContextScheduledAsync(admission, nativeTargetLog, nativeTargetAgentIndex", "BeginGuardrailRuntimeScope()", "TryRunSceneUnifiedActionPostprocess(")
assert "SetGuardrailRuntimeTargetAgentIndex(nativeTargetAgentIndex)" in native
scheduled = method("ShoutBehavior.NativePromptBuild.cs", "private async Task<MyBehavior.ShoutPromptContext> BuildNativePromptContextScheduledAsync(")
# J04f: step 1 and 3 run through the main-thread scheduler with admission re-validation; step 2 through the background slot.
ordered(scheduled, "RunNativeConversationMainThreadFuncAsync(\"prompt_build_begin\"", "IsNativeConversationAdmissionCurrent(admission, out _)", "owner.BeginSharedPromptBuild(",
        "RunNativeConversationBackgroundPreprocessAsync(", "owner.RunSharedPromptRouting(phases)", "AwaitNativeConversationBackgroundPreprocessAsync(",
        "RunNativeConversationMainThreadFuncAsync(\"prompt_build_complete\"", "owner.CompleteSharedPromptBuild(phases")
assert scheduled.count("IsNativeConversationAdmissionCurrent(admission, out _)") == 2, "admission must be re-validated on both game-thread steps"
assert scheduled.count("SaveRuntimeGuard.IsStale(runtimeGeneration") == 2, "generation checked after each hop"

courier_sched = method("CourierDeliveryBehavior.PromptSchedule.cs", "private async Task<CourierPreparedPrompt> BuildCourierPreparedPromptScheduledAsync(")
# J04f: Courier owner phases (game thread) bracket two thread-pool retrieval steps; each owner phase re-checks run + source.
ordered(courier_sched,
        'RunCourierOwnerPhaseAsync(generation, source + "_prompt_begin"', "owner.BeginCourierRulePreprocess(",
        "await Task.Run(", "owner.RunCourierRulePreprocessRetrieval(",
        'RunCourierOwnerPhaseAsync(generation, source + "_prompt_capture"', "owner.BeginSharedPromptBuild(",
        "owner.RunSharedPromptRouting(phases)",
        'RunCourierOwnerPhaseAsync(generation, source + "_prompt_complete"', "owner.CompleteSharedPromptBuild(phases")
assert courier_sched.count("IsCourierPromptRunCurrent(promptRun) || !IsCourierPromptInputCurrent(input)") == 3, "all three Courier owner phases re-validate run and source"
courier_prep = method("CourierDeliveryBehavior.PromptPreparation.cs", "private async Task<T> PrepareCourierPromptRequestAsync<T>(")
assert "BuildCourierPreparedPromptScheduledAsync(input, promptRun, generation, source)" in courier_prep and "Task.Run(() => BuildCourierPreparedPrompt(input))" not in courier_prep, "Courier no longer runs the whole builder in one Task.Run"

policy = method("PolicySystem/History/PolicyHistoryRetrievalService.cs", "internal static bool TryRetrieveDialogueByMentions(")
if args.mutate == "remove-policy-terms":
    policy = policy.replace("PromptListRetrievalService.BuildMentionTerms(mentionedEntities)", "new List<string>()")
ordered(policy, "PromptListRetrievalService.BuildMentionTerms(mentionedEntities)", "result.DialogueMentionTermCount = allTerms.Count", "NormalizeOrderedIds(ownerKingdomIds)")
assert "ownerSet.Contains((entry.OwnerKingdomId ?? string.Empty).Trim())" in policy

print("PASS production-consumer boundaries=My,Reward,Scene,Native,Policy actualSource=true gameDomain=NOT_RUN")
