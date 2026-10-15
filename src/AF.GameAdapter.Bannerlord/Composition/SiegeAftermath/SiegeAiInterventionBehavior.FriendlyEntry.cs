using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
    // A peaceful visit is NOT SETS victory and NOT a friendly-settlement massacre incident.
    private static string _peacefulEntrySettlementId = "";
    private static bool IsPeacefulGovernanceEntry(Settlement settlement) => settlement != null && settlement.StringId == _peacefulEntrySettlementId;
    private static string FriendlyEntryContext => GcczTownPromptResourceProvider.UseEnglishPrompt
        ? "[Entry provenance: peaceful governance] The player entered through the friendly settlement affairs menu, NOT after a siege, riot or massacre. No new conquest, defeated garrison, captive local lord or ownership transfer occurred. Castle prisoners are only real prisoners selected from the player's roster. Residents are not prisoners. Do not invent military victory or blame."
        : "【入口来源：和平治理】玩家通过友方领地事务菜单进入，并非攻城、暴动或屠杀后进入。没有新占领、击败守军、俘获当地领主或领地转移。城堡战俘仅来自玩家部队中实际选中的俘虏，居民不是战俘。不要编造战胜或追责背景。";

    private static string BuildPeacefulSpeakerContext(bool prisoner, bool lord, bool allied, bool civilian)
    {
        bool en = GcczTownPromptResourceProvider.UseEnglishPrompt;
        string role = prisoner
            ? (en ? (lord ? "You are an actual lord prisoner brought from the player's party, awaiting individual disposition." : "You are an actual regular prisoner brought from the player's party; local residents and guards are not your prisoner group.")
                : (lord ? "你是从玩家部队带来的真实领主俘虏，等待个人处置。" : "你是从玩家部队带来的真实普通俘虏；当地居民和守军不是这批俘虏。"))
            : allied ? (en ? "You accompany the player as an allied escort and obey valid orders; this does not imply a recent battle." : "你是玩家带入的己方随行人员，服从有效命令；这不代表刚刚参加过战斗。")
            : civilian ? (en ? "You are a local resident in friendly territory, not a captive or defeated defender." : "你是友方领地的当地居民，不是战俘或战败守军。")
            : (en ? "Use your actual role in this friendly settlement; do not invent imprisonment or a battle." : "按在友方领地的真实身份回应，不编造被俘或战败经历。");
        return FriendlyEntryContext + "\n" + role;
    }

    private static void RegisterFriendlyGcczEntries(CampaignGameStarter starter)
    {
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(Campaign.Current.GetCampaignBehavior<SiegeAiInterventionBehavior>(), AlignGcczMenuOrder);
        starter.AddGameMenuOption("town", "AnimusForge_gccz_friendly_town", "{=AF_GCCZ_TownAffairs}Personally attend to town affairs",
            FriendlyGcczCondition, FriendlyGcczConsequence, false, 1);
        starter.AddGameMenuOption("castle", "AnimusForge_gccz_friendly_castle", "{=AF_GCCZ_CastlePrisoners}Handle prisoners in the castle",
            FriendlyGcczCondition, FriendlyGcczConsequence, false, 1);
    }
    private static void AlignGcczMenuOrder(MenuCallbackArgs args)
    {
        var menu = args?.MenuContext?.GameMenu;
        if (menu?.MenuOptions is not IList<GameMenuOption> options) return;
        string ownId = menu.StringId == "town" ? "AnimusForge_gccz_friendly_town"
            : menu.StringId == "castle" ? "AnimusForge_gccz_friendly_castle"
            : menu.StringId == "village" ? "AnimusForge_village_disposition" : "";
        if (ownId.Length == 0) return;
        var own = options.FirstOrDefault(o => o.IdString == ownId);
        var anchor = menu.StringId == "village" ? options.FirstOrDefault(o => o.IdString == "hostile_action")
            : options.FirstOrDefault(o => o.IdString == ExpeditionParadeMenuPolicy.EntryId)
            ?? options.FirstOrDefault(o => o != own && ((o.IdString ?? "").IndexOf("inspection", StringComparison.OrdinalIgnoreCase) >= 0
                || (o.IdString ?? "").IndexOf("parade", StringComparison.OrdinalIgnoreCase) >= 0
                || o.Text?.ToString().StartsWith("阅兵", StringComparison.Ordinal) == true));
        if (own == null || anchor == null || options.IsReadOnly) return;
        int target = options.IndexOf(anchor) + (menu.StringId == "village" ? 1 : 0);
        int current = options.IndexOf(own);
        if (current < target) target--;
        if (current == target) return;
        options.RemoveAt(current); options.Insert(target, own);
    }

    private static bool FriendlyGcczCondition(MenuCallbackArgs args)
    {
        var s = ResolveCurrentSettlement(); var clan = Clan.PlayerClan;
        bool authority = s?.Town != null && clan != null && s.MapFaction == clan.MapFaction
            && (s.OwnerClan == clan || clan.Kingdom?.Leader == Hero.MainHero);
        if (!authority) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        args.IsEnabled = Mission.Current == null && !s.IsUnderSiege && s.Party?.MapEvent == null
            && MobileParty.MainParty?.Party?.MapEvent == null && PlayerEncounter.LocationEncounter?.Settlement == s
            && ResolveInterventionLocation(s) != null && !CastleAftermathMissionEntryBridge.IsPending;
        return true;
    }
    private static void FriendlyGcczConsequence(MenuCallbackArgs args)
    {
        if (!FriendlyGcczCondition(args) || !args.IsEnabled) return;
        var settlement = ResolveCurrentSettlement();
        ResetAftermathRuntimeGuards("friendly_governance_menu");
        _peacefulEntrySettlementId = settlement.StringId;
        ClearCastleLordDefeatProvenance("friendly_governance_menu");
        EnterIntervention(args);
    }
    internal static void EnterFriendlyVillageFromMenu(MenuCallbackArgs args)
    {
        var village = Settlement.CurrentSettlement;
        if (Mission.Current == null && VillageAftermathBehavior.CanEnterFriendlyVillage(village))
            OpenForeignVillageTroopSelection(args, village, false);
    }
}
