using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private readonly PlayerDirectActionController _playerDirectActions = new();

    internal static bool OpenNativePlayerDirectActions(Action onFinished)
    {
        var owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions() || !CanSubmitNativeConversationForExternal()
            || IsNativeConversationBackendBusyForUi() || !TryResolveNativeConversationTarget(out var hero, out var character, out var name)) return false;
        int index = TryResolveNativeConversationAgentIndex(hero, character);
        return owner.OpenPlayerDirectActions(hero, character, index, name, true, onFinished);
    }

    internal bool OpenScenePlayerDirectActions(NpcDataPacket npc, Agent expectedAgent, Action onFinished)
    {
        if (npc == null || !IsBannerlordMainThreadForNativeActions()) return false;
        var agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == npc.AgentIndex);
        var character = agent?.Character as CharacterObject;
        return agent != null && ReferenceEquals(agent, expectedAgent) && character != null
            && OpenPlayerDirectActions(character.HeroObject, character, agent.Index, npc.Name, false, onFinished);
    }

    private bool OpenPlayerDirectActions(Hero hero, CharacterObject character, int index, string name, bool native, Action onFinished)
    {
        var mission = Mission.Current;
        var campaign = Campaign.Current;
        var settlement = Settlement.CurrentSettlement;
        var manager = campaign?.ConversationManager;
        int token = manager?.ActiveToken ?? int.MinValue, epoch = _sceneConversationEpoch;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        var agent = index >= 0 ? mission?.Agents?.FirstOrDefault(a => a != null && a.Index == index) : null;
        if (campaign == null || (index >= 0 && agent == null)) return false;
        bool Current()
        {
            if (!IsBannerlordMainThreadForNativeActions() || !ReferenceEquals(CurrentInstance, this)
                || !ReferenceEquals(campaign, Campaign.Current) || !ReferenceEquals(mission, Mission.Current)
                || mission?.IsMissionEnding == true || !ReferenceEquals(settlement, Settlement.CurrentSettlement)
                || !SaveRuntimeGuard.IsCurrentGeneration(generation) || epoch != _sceneConversationEpoch
                || !ReferenceEquals(manager, Campaign.Current?.ConversationManager)) return false;
            if (native)
            {
                if (manager?.IsConversationInProgress != true || manager.ActiveToken != token
                    || !TryResolveNativeConversationTarget(out var liveHero, out var liveCharacter, out _)
                    || !ReferenceEquals(hero, liveHero) || !ReferenceEquals(character, liveCharacter)) return false;
            }
            else if (manager?.IsConversationInProgress == true || mission == null || mission.Mode == MissionMode.Conversation) return false;
            return (index < 0 || (ReferenceEquals(agent, mission?.Agents?.FirstOrDefault(a => a != null && a.Index == index)) && agent.IsActive()))
                && IsNativeConversationResponseTargetAvailableForActionDispatch(index, hero, character, out _);
        }
        string Unavailable(PlayerDirectActionKind kind)
        {
            var reward = RewardSystemBehavior.Instance;
            bool merchant = character != null && settlement != null && reward?.TryGetSettlementMerchantKind(character, out _) == true;
            switch (kind)
            {
                case PlayerDirectActionKind.Gold: case PlayerDirectActionKind.Item:
                    return reward != null && (hero != null || merchant || TryResolveWildernessNonHeroRewardParty(hero, character, index, out _))
                        ? null : "原机制只支持英雄、定居点商人或有实际来源的野外部队。";
                case PlayerDirectActionKind.FixedAsset:
                    return reward != null && hero != null ? null : "固定资产需要已有英雄及其原机制可转交资产。";
                case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners:
                    return MyBehavior.IsPartyTransferLordEligibleForExternal(hero, character)
                        || PartyAssetTransferBannerlordAdapter.IsPartyTransferNotableRecruitEligible(hero)
                        || MyBehavior.IsWildernessNonHeroPartyTransferEligibleForExternal(hero, character, index)
                        ? null : "原机制只支持领主、可招募志愿兵的显贵或有效野外部队来源。";
                case PlayerDirectActionKind.Debt: case PlayerDirectActionKind.ResolveDebt:
                    return reward != null && (hero != null || merchant) ? null : "当前目标没有原机制支持的个人／市场债务账本。";
                case PlayerDirectActionKind.Companion: case PlayerDirectActionKind.Family:
                    return reward != null && hero != null && hero != Hero.MainHero && Clan.PlayerClan != null && MobileParty.MainParty != null
                        ? null : "需要已有英雄、玩家家族及玩家队伍；普通 NPC 晋升不在本次范围。";
                case PlayerDirectActionKind.Follow: case PlayerDirectActionKind.StopFollow:
                    return agent != null && agent != Agent.Main ? null : "需要当前场景中的有效 NPC Agent；原移动标签限制仍适用。";
                case PlayerDirectActionKind.LordsHall:
                    return settlement?.IsTown == true && mission != null && CampaignMission.Current?.Location?.StringId == "center"
                        && LocationComplex.Current?.GetLocationWithId("lordshall") != null && campaign.GameMenuManager != null
                        ? null : "原机制仅支持有领主大厅入口的城镇中心场景。";
                case PlayerDirectActionKind.AcceptIssue: case PlayerDirectActionKind.TurnInQuest:
                    return VanillaIssueOfferBridge.GetPlayerDirectIssueUnavailableReason(hero, kind == PlayerDirectActionKind.TurnInQuest);
                default: return "不支持的直接动作。";
            }
        }
        void Finished()
        {
            // Never restore time/movement on a replacement campaign or mission.
            if (ReferenceEquals(campaign, Campaign.Current) && ReferenceEquals(mission, Mission.Current)) onFinished?.Invoke();
        }
        _playerDirectActions.Open(name, Current, Unavailable,
            kind => BuildPlayerDirectCandidates(kind, hero, character, index),
            (kind, choice) => choice == null || choice.IsCurrent?.Invoke() == true,
            (kind, choice, raw) => {
                if (!Current() || (choice != null && choice.IsCurrent?.Invoke() != true))
                { InformationManager.DisplayMessage(new InformationMessage("当前目标或候选已过期，未执行动作。")); return; }
                try
                {
                    if (kind == PlayerDirectActionKind.Follow || kind == PlayerDirectActionKind.StopFollow)
                    {
                        var npc = BuildNativeConversationNpcData(hero, character);
                        npc.AgentIndex = index;
                        if (native)
                        {
                            if (!TryQueueNativeSceneMechanismActionAfterConversationExit(npc, null, null, null, ref raw))
                            { InformationManager.DisplayMessage(new InformationMessage("当前场景动作无法提交。")); return; }
                            CloseNativeConversationForSceneMechanism("player_direct_action");
                        }
                        else _sceneMovement.TryExecuteDeferredSceneFollowTagsDirectly(npc, raw);
                    }
                    else _nativeGameEffects.ApplyNativeConversationActionTags(hero, character, ref raw, index,
                        expectedConversationManager: manager, expectedConversationToken: native ? token : int.MinValue,
                        actionChainName: "player_direct_action", npcReplyTextOverride: "");
                    InformationManager.DisplayMessage(new InformationMessage("直接动作已提交；实际结果以原机制提示和状态为准。"));
                }
                catch (Exception ex)
                {
                    Logger.Log("PlayerDirectActions", "dispatch failed: " + ex.Message);
                    InformationManager.DisplayMessage(new InformationMessage("动作提交出现异常，请检查原机制状态；不会自动重试。"));
                }
            }, Finished);
        return true;
    }

    private static List<PlayerDirectActionChoice> BuildPlayerDirectCandidates(PlayerDirectActionKind kind, Hero hero, CharacterObject character, int index)
    {
        var reward = RewardSystemBehavior.Instance;
        var result = new List<PlayerDirectActionChoice>();
        switch (kind)
        {
            case PlayerDirectActionKind.Item:
                List<RewardSystemBehavior.RewardItemInfo> Items()
                {
                    string scope;
                    List<RewardSystemBehavior.RewardItemInfo> items;
                    if (hero != null) { items = reward.BuildHeroRewardPostprocessItems(hero); scope = PromptListRetrievalService.NpcRewardItemsAllSnapshotScope; }
                    else if (TryResolveWildernessNonHeroRewardParty(hero, character, index, out var party))
                    { items = reward.BuildPartyRewardPostprocessItems(party); scope = PromptListRetrievalService.PartyRewardItemsAllSnapshotScope; }
                    else { items = reward.BuildSettlementMerchantPostprocessItems(character); scope = PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope; }
                    PromptListRetrievalService.PublishRewardItemSnapshot(scope, hero, character, -1, items);
                    return items;
                }
                var items = Items();
                var itemKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in items.Where(x => x?.Item != null && x.Count > 0))
                {
                    string key = string.IsNullOrWhiteSpace(item.PromptStringId) ? item.StringId : item.PromptStringId;
                    if (string.IsNullOrWhiteSpace(key) || !itemKeys.Add(key)) continue;
                    result.Add(new PlayerDirectActionChoice(key, item.Name + " ×" + item.Count + (item.IsPrivateEquipment ? "（私人装备）" : ""),
                        () => Items().Any(x => x.Item == item.Item && x.ModifierStringId == item.ModifierStringId
                            && (string.IsNullOrWhiteSpace(x.PromptStringId) ? x.StringId : x.PromptStringId) == key && x.Count > 0)));
                }
                string ItemKey(RewardSystemBehavior.RewardItemInfo x) => string.IsNullOrWhiteSpace(x.PromptStringId) ? x.StringId : x.PromptStringId;
                var ordinary = items.Where(x => x?.Item != null && x.Count > 0 && !x.IsPrivateEquipment && !string.IsNullOrWhiteSpace(ItemKey(x)))
                    .GroupBy(ItemKey, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
                if (ordinary.Count > 0)
                    result.Add(new PlayerDirectActionChoice("ALL", "全部普通物品（逐项执行原 :ALL，不含固定资产／私人装备）", () => {
                        var live = Items().Where(x => x?.Item != null && x.Count > 0 && !x.IsPrivateEquipment && !string.IsNullOrWhiteSpace(ItemKey(x)))
                            .GroupBy(ItemKey, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
                        return ordinary.All(x => live.TryGetValue(ItemKey(x), out var matches)
                            && matches.Any(y => ReferenceEquals(y.Item, x.Item) && y.ModifierStringId == x.ModifierStringId));
                    }, ordinary.Select(ItemKey)));
                break;
            case PlayerDirectActionKind.FixedAsset:
                List<MyBehavior.SettlementTransferPromptEntry> Assets()
                {
                    var assets = reward.GetAllowedNpcSettlementTransferEntriesForPlayer(hero, character)
                        .Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
                    PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, hero, character, -1, assets);
                    PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, hero, character, -1, assets);
                    return assets;
                }
                foreach (var asset in Assets())
                {
                    string key = MyBehavior.GetSettlementTransferAssetIdForExternal(asset);
                    result.Add(new PlayerDirectActionChoice(key, asset.TypeLabel + "：" + asset.DisplayName,
                        () => Assets().Any(x => MyBehavior.GetSettlementTransferAssetIdForExternal(x) == key
                            && ReferenceEquals(x.OwnerClan, asset.OwnerClan) && ReferenceEquals(x.OwnerHero, asset.OwnerHero)
                            && ReferenceEquals(x.Settlement, asset.Settlement) && ReferenceEquals(x.Workshop, asset.Workshop)
                            && ReferenceEquals(x.CaravanParty, asset.CaravanParty))));
                }
                break;
            case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners:
                bool prisoners = kind == PlayerDirectActionKind.Prisoners;
                PartyTransferAuthorizationSnapshot Capture() => PartyAssetTransferBannerlordAdapter.CapturePlayerDirectTransferCandidates(hero, character, index);
                List<MyBehavior.PartyTransferPromptEntry> Entries(PartyTransferAuthorizationSnapshot snapshot) => prisoners ? snapshot.AllPrisoners : snapshot.IndexedTroops;
                var snapshot = Capture();
                foreach (var entry in Entries(snapshot))
                {
                    result.Add(new PlayerDirectActionChoice(entry.PromptIndex.ToString(CultureInfo.InvariantCulture), entry.DisplayName + " ×" + entry.Count,
                        () => Entries(Capture()).Any(x => x.PromptIndex == entry.PromptIndex && ReferenceEquals(x.Character, entry.Character)
                            && ReferenceEquals(x.OwnerParty, entry.OwnerParty) && ReferenceEquals(x.VolunteerOwner, entry.VolunteerOwner)
                            && ReferenceEquals(x.SourceSettlement, entry.SourceSettlement) && x.Count > 0)));
                }
                var allEntries = prisoners ? snapshot.AllPrisoners : snapshot.AllTroops;
                var sources = allEntries.Select(x => (x.OwnerParty, x.VolunteerOwner, x.SourceSettlement)).Distinct().ToList();
                if (allEntries.Count > 0)
                    result.Add(new PlayerDirectActionChoice("ALL", prisoners ? "全部俘虏（原 ATP:ALL:ALL）" : "全部部队（原 ATT:ALL:ALL）",
                        () => {
                            var current = Capture();
                            var entries = prisoners ? current.AllPrisoners : current.AllTroops;
                            // ALL remains live quantity, but must not switch to another source party/dungeon.
                            return entries.Count > 0 && entries.All(x => sources.Any(s => ReferenceEquals(s.OwnerParty, x.OwnerParty)
                                && ReferenceEquals(s.VolunteerOwner, x.VolunteerOwner) && ReferenceEquals(s.SourceSettlement, x.SourceSettlement)))
                                && sources.All(s => entries.Any(x => ReferenceEquals(s.OwnerParty, x.OwnerParty)
                                    && ReferenceEquals(s.VolunteerOwner, x.VolunteerOwner) && ReferenceEquals(s.SourceSettlement, x.SourceSettlement)));
                        }));
                break;
            case PlayerDirectActionKind.ResolveDebt:
                foreach (var debt in reward.GetPlayerDirectDebtCandidates(hero, character))
                    result.Add(new PlayerDirectActionChoice(debt.DebtId, debt.DebtId + "：" + debt.DebtNote + "（剩余 " + debt.RemainingAmount
                        + (debt.IsGold ? " 第纳尔" : " × " + debt.ItemId) + (debt.IsDueUnlimited ? "，无期限）" : "）"),
                        () => reward.GetPlayerDirectDebtCandidates(hero, character).Any(x => x.DebtId == debt.DebtId && x.RemainingAmount > 0)));
                break;
        }
        return result;
    }
}
