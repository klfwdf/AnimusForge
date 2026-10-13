using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Adapters;
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

    private static PlayerDirectActionContext CapturePlayerDirectActionContext()
    {
        var mission = Mission.Current;
        if (MeetingBattleRuntime.IsCombatEscalated || IsActiveSceneConversationDuelCombat()
            || PlayerEncounterCompat.HasBattleOrEncounteredBattle() || PlayerEncounterCompat.HasCampaignBattleResult())
            return PlayerDirectActionContext.Restricted;
        if (mission == null) return PlayerDirectActionContext.WorldMap;
        if (TroopInspectionBehavior.ShouldSuppressReinforcementSystem(mission)
            || (MilitaryExerciseBehavior.IsCurrentExerciseRuntime() && mission.GetMissionBehavior<MilitaryExerciseMissionLogic>() != null)
            || mission.Mode == MissionMode.Deployment || mission.Mode == MissionMode.Duel
            || mission.Mode == MissionMode.Stealth || mission.Mode == MissionMode.Tournament)
            return PlayerDirectActionContext.Restricted;
        // Meetings can contain real Agents and use Battle mode without being combat.
        if (LordEncounterBehavior.IsEncounterMeetingMissionActive || MeetingBattleRuntime.IsMeetingActive
            || mission.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null)
            return PlayerDirectActionContext.WorldMap;
        return AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission()
            ? PlayerDirectActionContext.Restricted : PlayerDirectActionContext.Scene;
    }

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

    // The UI owns focus; this seam captures only its selected nameplate, never the framed crowd.
    public static bool OpenScenePresentationPlayerDirectActionsForExternal(Action onFinished)
    {
        var owner = CurrentInstance;
        if (owner == null || !IsBannerlordMainThreadForNativeActions() || owner.IsPresentationSubmitBlocked()) return false;
        var presentation = owner.Presentation;
        var member = presentation.IsPresentationSessionLive()
            ? presentation.FindPresentationMember(presentation._presentationAddresseeIndex) : null;
        if (member?.Character == null || !presentation.IsPresentationAudience(member)
            || !ReferenceEquals(member.Agent?.Character, member.Character)) return false;
        bool CurrentPresentation() => presentation.IsPresentationSessionLive()
            && presentation._presentationAddresseeIndex == member.AgentIndex
            && ReferenceEquals(presentation.FindPresentationMember(member.AgentIndex), member)
            && presentation.IsPresentationAudience(member) && !owner.IsPresentationSubmitBlocked();
        return owner.OpenPlayerDirectActions(member.Character.HeroObject, member.Character, member.AgentIndex,
            member.Name, false, onFinished, CurrentPresentation);
    }

    private bool OpenPlayerDirectActions(Hero hero, CharacterObject character, int index, string name, bool native, Action onFinished,
        Func<bool> isEntryCurrent = null)
    {
        var mission = Mission.Current;
        var campaign = Campaign.Current;
        var settlement = Settlement.CurrentSettlement;
        var manager = campaign?.ConversationManager;
        int token = manager?.ActiveToken ?? int.MinValue, epoch = _sceneConversationEpoch;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        var agent = index >= 0 ? mission?.Agents?.FirstOrDefault(a => a != null && a.Index == index) : null;
        if (campaign == null || (index >= 0 && agent == null) || isEntryCurrent?.Invoke() == false) return false;
        var context = CapturePlayerDirectActionContext();
        var mapActor = WorldMapPartyCommandBehavior.CapturePlayerDirectOrderActor(hero, character, index);
        bool mapRuleExcluded = PromptRuleCaptureBannerlordAdapter.ShouldExcludeWorldMapCommandRuleForTarget(hero, character, index);
        bool Current()
        {
            if (!IsBannerlordMainThreadForNativeActions() || !ReferenceEquals(CurrentInstance, this)
                || !ReferenceEquals(campaign, Campaign.Current) || !ReferenceEquals(mission, Mission.Current)
                || mission?.IsMissionEnding == true || !ReferenceEquals(settlement, Settlement.CurrentSettlement)
                || !SaveRuntimeGuard.IsCurrentGeneration(generation) || epoch != _sceneConversationEpoch
                || !ReferenceEquals(manager, Campaign.Current?.ConversationManager) || isEntryCurrent?.Invoke() == false
                || context != CapturePlayerDirectActionContext()) return false;
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
            if (PlayerCaptivityConversation.HasSession && kind != PlayerDirectActionKind.Gold && kind != PlayerDirectActionKind.Item
                && kind != PlayerDirectActionKind.Debt && kind != PlayerDirectActionKind.ResolveDebt && kind != PlayerDirectActionKind.FixedAsset)
                return new TaleWorlds.Localization.TextObject("{=AF_ENC_UI_CaptivityActionBlocked}This action is unavailable while you are in captivity.").ToString();
            if (PlayerDirectActionProtocol.IsWorldMap(kind, context))
            {
                if (context == PlayerDirectActionContext.Restricted)
                    return PlayerDirectActionProtocol.MapText("Restricted", "World-map commands are unavailable during combat, exercises or other restricted missions.");
                if (mapRuleExcluded)
                    return PlayerDirectActionProtocol.MapText("ActorIneligible", "The current character cannot receive world-map party commands under the original rules.");
                // The initial common qualification (including governor roster checks) is captured once.
                return kind == PlayerDirectActionKind.MergeToPlayer || kind == PlayerDirectActionKind.StopFollow
                    ? WorldMapPartyCommandBehavior.GetPlayerDirectOrderUnavailableReason(mapActor, PlayerDirectActionProtocol.WorldMapKind(kind))
                    : mapActor.InitialReason;
            }
            var reward = RewardSystemBehavior.Instance;
            bool merchant = character != null && settlement != null && reward?.TryGetSettlementMerchantKind(character, out _) == true;
            switch (kind)
            {
                case PlayerDirectActionKind.Gold: case PlayerDirectActionKind.Item:
                    return reward != null && (hero != null || merchant || TryResolveWildernessNonHeroRewardParty(hero, character, index, out _))
                        ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_048", "The existing mechanics only support heroes, settlement merchants or wilderness parties with a valid source.");
                case PlayerDirectActionKind.FixedAsset:
                    return reward != null && hero != null ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_049", "Fixed assets require an existing hero and assets transferable through the original mechanic.");
                case PlayerDirectActionKind.Troops: case PlayerDirectActionKind.Prisoners:
                    return MyBehavior.IsPartyTransferLordEligibleForExternal(hero, character)
                        || PartyAssetTransferBannerlordAdapter.IsPartyTransferNotableRecruitEligible(hero)
                        || MyBehavior.IsWildernessNonHeroPartyTransferEligibleForExternal(hero, character, index)
                        ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_050", "The existing mechanics only support lords, notables eligible to recruit volunteers or valid wilderness party sources.");
                case PlayerDirectActionKind.Debt: case PlayerDirectActionKind.ResolveDebt:
                    return reward != null && (hero != null || merchant) ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_051", "The current target has no personal or market debt ledger supported by the existing mechanics.");
                case PlayerDirectActionKind.Companion: case PlayerDirectActionKind.Family:
                    return reward != null && hero != null && hero != Hero.MainHero && Clan.PlayerClan != null && MobileParty.MainParty != null
                        ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_052", "An existing hero, player clan and player party are required. Promoting ordinary NPCs is outside this feature's scope.");
                case PlayerDirectActionKind.Follow: case PlayerDirectActionKind.StopFollow:
                    return context == PlayerDirectActionContext.Scene && agent != null && agent != Agent.Main ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_053", "A valid NPC Agent in the current scene is required. The original movement tag restrictions still apply.");
                case PlayerDirectActionKind.LordsHall:
                    return settlement?.IsTown == true && mission != null && CampaignMission.Current?.Location?.StringId == "center"
                        && LocationComplex.Current?.GetLocationWithId("lordshall") != null && campaign.GameMenuManager != null
                        ? null : ConversationUiLocalization.Text("AF_CONV_UI_Direct_054", "The existing mechanics only support town-center scenes with a lord's hall entrance.");
                case PlayerDirectActionKind.AcceptIssue: case PlayerDirectActionKind.TurnInQuest:
                    return VanillaIssueOfferBridge.GetPlayerDirectIssueUnavailableReason(hero, kind == PlayerDirectActionKind.TurnInQuest);
                default: return ConversationUiLocalization.Text("AF_CONV_UI_Direct_055", "Unsupported direct action.");
            }
        }
        List<PlayerDirectActionChoice> Candidates(PlayerDirectActionKind kind)
        {
            if (!PlayerDirectActionProtocol.IsWorldMap(kind, context)) return BuildPlayerDirectCandidates(kind, hero, character, index);
            return WorldMapPartyCommandBehavior.CapturePlayerDirectOrderTargets(mapActor, PlayerDirectActionProtocol.WorldMapKind(kind),
                playerOnly: kind == PlayerDirectActionKind.Follow).Select(x => new PlayerDirectActionChoice(x.Id,
                    x.Name + " — " + PlayerDirectMapTargetType(x) + " [" + x.Id + "]",
                    x.IsCurrent, targetType: x.Type, defaultDays: x.DefaultDays)).ToList();
        }
        void Finished()
        {
            // Never restore time/movement on a replacement campaign or mission.
            if (ReferenceEquals(campaign, Campaign.Current) && ReferenceEquals(mission, Mission.Current)) onFinished?.Invoke();
        }
        _playerDirectActions.Open(name, Current, Unavailable,
            Candidates,
            (kind, choice) => choice == null || choice.IsCurrent?.Invoke() == true,
            (kind, choice, raw) => {
                if (!Current() || (choice != null && choice.IsCurrent?.Invoke() != true))
                { InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_056", "The target or candidate has expired. No action was executed."))); return; }
                try
                {
                    if (PlayerDirectActionProtocol.IsWorldMap(kind, context))
                    {
                        string reason = WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(mapActor, raw);
                        if (!string.IsNullOrEmpty(reason)) { InformationManager.DisplayMessage(new InformationMessage(reason)); return; }
                        var result = _nativeGameEffects.ApplyNativeConversationActionTags(hero, character, ref raw, index,
                            expectedConversationManager: manager, expectedConversationToken: native ? token : int.MinValue,
                            actionChainName: "player_direct_action", npcReplyTextOverride: "");
                        InformationManager.DisplayMessage(new InformationMessage(result?.Handled == true
                            ? result.StopApplied && result.AddedCommandCount == 0
                                ? PlayerDirectActionProtocol.MapText("StopAccepted", "The original world-map owner handled STOP. Check its safe-exit receipt and state; this is not confirmation that the NPC has already stopped.")
                                : PlayerDirectActionProtocol.MapText("Accepted", "The original world-map owner accepted {COUNT} command(s). Check its messages and state; acceptance is not movement or completion.", ("COUNT", result.AddedCommandCount))
                            : result?.StopApplied == true
                                ? PlayerDirectActionProtocol.MapText("Partial", "STOP was processed but the new command was not fully accepted. Check the original owner's receipt and queue; no automatic retry will occur.")
                                : PlayerDirectActionProtocol.MapText("NotAccepted", "The original world-map owner did not accept the complete request. Check its receipt and state; no automatic retry will occur.")));
                        if (result?.NeedsChannelExit == true && Current())
                        {
                            if (native) CloseNativeConversationForSceneMechanism("worldmap_implicit_party_creation");
                            // No speech to await. Flush the existing exit immediately under captured identity,
                            // so no delayed callback can close a subsequently opened conversation.
                            if (mission != null && ReferenceEquals(mission, Mission.Current) && ReferenceEquals(CurrentInstance, this)
                                && ReferenceEquals(campaign, Campaign.Current) && SaveRuntimeGuard.IsCurrentGeneration(generation)
                                && epoch == _sceneConversationEpoch && ReferenceEquals(manager, campaign.ConversationManager)
                                && manager?.IsConversationInProgress != true)
                            {
                                int exitIndex = index >= 0 ? index : Agent.Main?.Index ?? -1;
                                ScheduleWorldMapMissionExitAfterSpeech(exitIndex, null);
                                FlushWorldMapMissionExitAfterSpeech(exitIndex);
                            }
                        }
                        return;
                    }
                    if (kind == PlayerDirectActionKind.Follow || kind == PlayerDirectActionKind.StopFollow)
                    {
                        var npc = BuildNativeConversationNpcData(hero, character);
                        npc.AgentIndex = index;
                        if (native)
                        {
                            if (!TryQueueNativeSceneMechanismActionAfterConversationExit(npc, null, null, null, ref raw))
                            { InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_057", "The current scene action could not be submitted."))); return; }
                            CloseNativeConversationForSceneMechanism("player_direct_action");
                        }
                        else
                        {
                            DeactivateMultiSceneMovementSuppression();
                            _sceneMovement.TryExecuteDeferredSceneFollowTagsDirectly(npc, raw);
                        }
                    }
                    else _nativeGameEffects.ApplyNativeConversationActionTags(hero, character, ref raw, index,
                        expectedConversationManager: manager, expectedConversationToken: native ? token : int.MinValue,
                        actionChainName: "player_direct_action", npcReplyTextOverride: "");
                    InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_058", "Direct action submitted. Its actual result is determined by the original mechanic's messages and state.")));
                }
                catch (Exception ex)
                {
                    Logger.Log("PlayerDirectActions", "dispatch failed: " + ex.Message);
                    InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Direct_059", "An error occurred while submitting the action. Check the original mechanic's state; there will be no automatic retry.")));
                }
            }, Finished, context, raw => WorldMapPartyCommandBehavior.ValidatePlayerDirectOrder(mapActor, raw));
        return true;
    }

    private static string PlayerDirectMapTargetType(WorldMapPartyCommandBehavior.PlayerDirectOrderTarget target) => target.DisplayType switch {
        "TownType" => PlayerDirectActionProtocol.MapText("TownType", "Town"),
        "CastleType" => PlayerDirectActionProtocol.MapText("CastleType", "Castle"),
        "VillageType" => PlayerDirectActionProtocol.MapText("VillageType", "Village"),
        "HideoutType" => PlayerDirectActionProtocol.MapText("HideoutType", "Hideout"),
        _ => target.Type == "hero" ? PlayerDirectActionProtocol.MapText("HeroType", "Hero")
            : target.Type == "party" ? PlayerDirectActionProtocol.MapText("PartyType", "Party")
            : PlayerDirectActionProtocol.MapText("LocationType", "Location")
    };

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
                    result.Add(new PlayerDirectActionChoice(key, item.Name + " ×" + item.Count + (item.IsPrivateEquipment ? ConversationUiLocalization.Text("AF_CONV_UI_Direct_060", " (private equipment)") : ""),
                        () => Items().Any(x => x.Item == item.Item && x.ModifierStringId == item.ModifierStringId
                            && (string.IsNullOrWhiteSpace(x.PromptStringId) ? x.StringId : x.PromptStringId) == key && x.Count > 0)));
                }
                string ItemKey(RewardSystemBehavior.RewardItemInfo x) => string.IsNullOrWhiteSpace(x.PromptStringId) ? x.StringId : x.PromptStringId;
                var ordinary = items.Where(x => x?.Item != null && x.Count > 0 && !x.IsPrivateEquipment && !string.IsNullOrWhiteSpace(ItemKey(x)))
                    .GroupBy(ItemKey, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
                if (ordinary.Count > 0)
                    result.Add(new PlayerDirectActionChoice("ALL", ConversationUiLocalization.Text("AF_CONV_UI_Direct_061", "All ordinary items (existing per-item :ALL; excludes fixed assets and private equipment)"), () => {
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
                    result.Add(new PlayerDirectActionChoice("ALL", prisoners ? ConversationUiLocalization.Text("AF_CONV_UI_Direct_062", "All prisoners (existing ATP:ALL:ALL)") : ConversationUiLocalization.Text("AF_CONV_UI_Direct_063", "All troops (existing ATT:ALL:ALL)"),
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
                    result.Add(new PlayerDirectActionChoice(debt.DebtId, debt.DebtId + "：" + debt.DebtNote + ConversationUiLocalization.Text("AF_CONV_UI_Direct_064", " (remaining ") + debt.RemainingAmount
                        + (debt.IsGold ? ConversationUiLocalization.Text("AF_CONV_UI_Direct_065", " denars") : " × " + debt.ItemId) + (debt.IsDueUnlimited ? ConversationUiLocalization.Text("AF_CONV_UI_Direct_066", ", no deadline)") : "）"),
                        () => reward.GetPlayerDirectDebtCandidates(hero, character).Any(x => x.DebtId == debt.DebtId && x.RemainingAmount > 0)));
                break;
        }
        return result;
    }
}
