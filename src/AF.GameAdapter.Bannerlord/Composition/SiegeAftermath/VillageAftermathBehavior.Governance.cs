using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class VillageAftermathBehavior
{
    private static readonly VillageDepartureDecision Departure = new VillageDepartureDecision();
    private static bool _giftInquiryActive;
    private static long _giftInquirySequence;
    private static int _villageReplySequence;
    private static readonly HashSet<VillageDispositionActionKind> DestructiveActions = new HashSet<VillageDispositionActionKind>();
    private Dictionary<string, int> _aidLastDay = new Dictionary<string, int>(); // Legacy save identity; no longer a delivery cooldown.
    private Dictionary<string, int> _aidDeliveryRemainder = new Dictionary<string, int>();
    private Dictionary<string, int> _aidRoundDay = new Dictionary<string, int>();
    private Dictionary<string, int> _aidRoundMask = new Dictionary<string, int>();
    private Dictionary<string, int> _aidBonusExpires = new Dictionary<string, int>();
    private Dictionary<string, int> _aidBonuses = new Dictionary<string, int>();
    private readonly Dictionary<string, float> _aidMultiplier = new Dictionary<string, float>();
    private static string VUi(string cn, string en) => GcczLocalizedResourceLoader.UseChineseInterface ? cn : en;
    private static string VP(string cn, string en) => GcczTownPromptResourceProvider.UseEnglishPrompt ? en : cn;
    private static VillageDispositionActionSpec Spec(VillageDispositionActionKind kind) => VillageDispositionActionCatalog.All.First(a => a.Kind == kind);
    private static bool SameFaction(Settlement village) => Clan.PlayerClan != null && village?.MapFaction == Clan.PlayerClan.MapFaction;
    private static bool CanGovern(Settlement village) => VillageGovernanceRules.CanGovern(SameFaction(village),
        village?.Village?.Bound?.OwnerClan == Clan.PlayerClan,
        Clan.PlayerClan?.Kingdom != null && Clan.PlayerClan.Kingdom.Leader == Hero.MainHero);

    internal static bool CanEnterFriendlyVillage(Settlement village)
        => village?.IsVillage == true && SameFaction(village) && village.Village.Bound != null
            && village.Village.VillageState == Village.VillageStates.Normal && !village.IsUnderSiege
            && village.Party?.MapEvent == null && MobileParty.MainParty?.Party?.MapEvent == null;

    internal static void OnPlayerVillageLeaveRequest(Mission mission)
    {
        if (!Departure.RequestDefaultPlunder(IsActiveForMission(mission), mission?.MainAgent?.IsActive() == true)) return;
        ApplyForeignEconomicAction(Spec(VillageDispositionActionKind.Plunder), -1);
    }

    private static bool FriendlyActionEnabled(FriendlyVillageAction action, int index)
    {
        var role = ResolveHostileSpeakerRole(index, out _);
        if (role == VillageDispositionSpeakerRole.Unknown || role == VillageDispositionSpeakerRole.Other) return false;
        if (action.Name == "核对文化方案") return _cultureTarget.Length > 0 && _cultureQuestionTurn == 0 && index == _cultureSpeaker
            && CanGovern(ResolveActiveVillage()) && role == VillageDispositionSpeakerRole.PlayerCompanion;
        if (action.Name == "文化改造") return CanGovern(ResolveActiveVillage()) && role == VillageDispositionSpeakerRole.PlayerCompanion
            && (_cultureTarget.Length == 0 || (_cultureQuestionTurn > 0 && index == _cultureSpeaker));
        if (action.Economic.HasValue)
        {
            var spec = Spec(action.Economic.Value);
            return spec.HasDefinedEffect && VillageDispositionSpeakerPolicy.CanAuthorize(spec, role, true)
                && (spec.OutcomeLevel <= 0 || EconomyOwner?.Assistance.CanSpendForSceneTurn(_activeVillageId, InitialVillageAssistance, ForeignPersuasionTurn) == true);
        }
        return true;
    }

    private static List<PostprocessRuleEntry> BuildFriendlyRules(int index)
        => FriendlyVillageActionCatalog.All.Where(a => FriendlyActionEnabled(a, index)).Select(a => Rule(a.Tag,
            a.Economic.HasValue
                ? VillageDispositionPromptProfile.BuildActionDescription(Spec(a.Economic.Value), GcczTownPromptResourceProvider.UseEnglishPrompt)
                : (GcczTownPromptResourceProvider.UseEnglishPrompt ? a.English : a.Name)
                    + (a.Gift >= 0 ? TownPromptTextCatalog.FormatTemplate(GcczTownPromptResourceProvider.GetCatalog().GetRuntimeText("VillageAid_Confirm"),
                        ("minimum", VillageAidRules.Minimum(a.Gift).ToString())) : ""))).ToList();

    private static string BuildFriendlyPrompt(Settlement village, Hero hero, CharacterObject character, int index)
    {
        string prompt = BuildHostileDialoguePrompt(village, hero, character, index);
        bool canDiscussCulture = CanGovern(village) && ResolveHostileSpeakerRole(index, out _) == VillageDispositionSpeakerRole.PlayerCompanion;
        return prompt
            + "\n" + GcczTownPromptResourceProvider.GetCatalog().GetRuntimeText("VillageAid_Dialogue")
            + "\n" + VP("可讨论的事务：", "Available affairs: ")
            + string.Join(" / ", FriendlyVillageActionCatalog.All.Where(a => FriendlyActionEnabled(a, index)
                && a.Name != "认可游说" && a.Name != "核对文化方案")
                .Select(a => GcczTownPromptResourceProvider.UseEnglishPrompt ? a.English : a.Name))
            + (canDiscussCulture ? "\n" + CultureRuleContext(false) : string.Empty);
    }

    private static bool TryProcessFriendlyDisposition(int index, ref string text, out bool handled, bool direct)
    {
        handled = false;
        if (direct) _villageReplySequence++;
        bool has = text?.IndexOf("[VILLAGE_ACTION:", StringComparison.OrdinalIgnoreCase) >= 0;
        var invocations = FriendlyVillageActionCatalog.ExtractInvocations(text);
        var actions = invocations.Select(a => a.Action).ToList();
        text = FriendlyVillageActionCatalog.RemoveTags(VillageDispositionActionCatalog.RemoveTags(text));
        if (!IsActive() || !direct || !CanEnterFriendlyVillage(ResolveActiveVillage())) return has;
        if (_cultureTarget.Length > 0 && index == _cultureSpeaker && _villageReplySequence > _culturePreparedTurn
            && !actions.Any(a => a.Name == (_cultureQuestionTurn > 0 ? "文化改造" : "核对文化方案"))) ClearCultureProposal();
        bool voluntary = actions.Any(a => a.Economic.HasValue && Spec(a.Economic.Value).OutcomeLevel > 0);
        foreach (var invocation in invocations)
        {
            var action = invocation.Action;
            if (!FriendlyActionEnabled(action, index)) continue;
            if (action.Economic == VillageDispositionActionKind.VillagerApproval && voluntary) continue;
            if (action.Name == "核对文化方案") { _cultureQuestionTurn = _villageReplySequence; handled = true; continue; }
            if (action.Gift >= 0) { handled |= OpenVillageGift(invocation); continue; }
            if (action.Name == "文化改造") { handled |= HandleCultureConversation(index); continue; }
            if (action.Name == "约束军纪" || action.Economic == VillageDispositionActionKind.StopPlunder)
            { ClearForeignSceneOrders(); StopVillageMassacre(); handled = true; continue; }
            if (action.Economic == VillageDispositionActionKind.Mercy)
            {
                if (!Departure.HasDisposition || !_hostileVillageMercyApplied)
                {
                    StopVillageMassacre(); Departure.MarkDisposition(); _hostileVillageMercyApplied = true; handled = true;
                    RecordForeignFact(ResolveActiveVillage(), VP("玩家宽恕村民；既有损失和关系后果保持。", "The player pardoned the villagers; prior losses and relationship consequences remain."), "mercy", false);
                }
                continue;
            }
            if (action.Economic.HasValue) handled |= ApplyForeignEconomicAction(Spec(action.Economic.Value), index);
        }
        return has;
    }

    private void SyncGovernance(IDataStore store)
    {
        store.SyncData("_gcczVillagePurgeUntil_v1", ref _purgeUntil);
        _purgeUntil ??= new Dictionary<string,int>();
        store.SyncData("_gcczVillageAidLastDay_v1", ref _aidLastDay);
        store.SyncData("_gcczVillageAidDeliveryRemainder_v1", ref _aidDeliveryRemainder);
        _aidDeliveryRemainder ??= new Dictionary<string,int>();
        store.SyncData("_gcczVillageAidRoundDay_v1", ref _aidRoundDay);
        store.SyncData("_gcczVillageAidRoundMask_v1", ref _aidRoundMask);
        store.SyncData("_gcczVillageAidBonuses_v1", ref _aidBonuses);
        store.SyncData("_gcczVillageAidBonusExpires_v1", ref _aidBonusExpires);
        _aidBonusExpires ??= new Dictionary<string,int>();
        _aidLastDay ??= new Dictionary<string,int>(); _aidRoundDay ??= new Dictionary<string,int>();
        _aidRoundMask ??= new Dictionary<string,int>(); _aidBonuses ??= new Dictionary<string,int>();
        if (store.IsLoading) RefreshAidMultipliers();
    }
}
