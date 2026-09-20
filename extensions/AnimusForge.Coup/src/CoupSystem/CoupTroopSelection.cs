using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge.CoupSystem;

internal static class CoupTroopSelection
{
    // Dummy rosters: selecting an assault group must never transfer real party members.
    internal static void Open(TroopRoster available, int limit, string title, Action<TroopRoster> completed, Action cancelled)
    {
        PartyBase mainParty = MobileParty.MainParty?.Party;
        var data = CreateInitializationData(mainParty, available, limit, title, completed, cancelled);
        var logic = new PartyScreenLogic();
        logic.Initialize(data);
        // Native cancel/reset restores this inventory. Selection must not clear and
        // repopulate the real main-party inventory, even if another mod changed it.
        logic.CurrentData.RightItemRoster = new ItemRoster(mainParty.ItemRoster);
        var state = Game.Current.GameStateManager.CreateState<PartyState>();
        state.PartyScreenLogic = logic;
        state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
        Game.Current.GameStateManager.PushState(state);
    }

    internal static PartyScreenLogicInitializationData CreateInitializationData(PartyBase mainParty, TroopRoster available,
        int limit, string title, Action<TroopRoster> completed, Action cancelled)
    {
        if (mainParty == null || mainParty.ItemRoster == null) throw new ArgumentException("选兵需要有效的玩家部队。", nameof(mainParty));
        if (available == null || ReferenceEquals(available, mainParty.MemberRoster) || ReferenceEquals(available, mainParty.PrisonRoster))
            throw new ArgumentException("选兵必须使用独立临时名册。", nameof(available));
        if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
        if (completed == null) throw new ArgumentNullException(nameof(completed));
        if (cancelled == null) throw new ArgumentNullException(nameof(cancelled));
        var selected = TroopRoster.CreateDummyTroopRoster();
        return new PartyScreenLogicInitializationData
        {
            LeftOwnerParty = null,
            // PartyCharacterVM reads RightOwnerParty.ItemRoster for any ordinary
            // right-side troop with upgrade targets BEFORE checking upgrades-disabled.
            // Supply native UI context, while keeping all editable rosters detached.
            RightOwnerParty = mainParty,
            RightLeaderHero = mainParty.LeaderHero,
            LeftMemberRoster = available,
            RightMemberRoster = selected,
            LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
            RightPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
            LeftPartyMembersSizeLimit = available.TotalManCount,
            RightPartyMembersSizeLimit = limit,
            LeftPartyPrisonersSizeLimit = 0,
            RightPartyPrisonersSizeLimit = 0,
            LeftPartyName = new TextObject("可选士兵"),
            RightPartyName = new TextObject(title + "（最多 " + limit + " 人）"),
            Header = new TextObject(title),
            TroopTransferableDelegate = (character, type, side, owner) => character != null && !character.IsHero,
            PartyPresentationDoneButtonDelegate = (left, lp, right, rp, taken, released, forced, leftParty, rightParty) => true,
            PartyPresentationDoneButtonConditionDelegate = (left, lp, right, rp, ll, rl) =>
                Tuple.Create(right.TotalManCount > 0 && right.TotalManCount <= limit,
                    new TextObject("请选择 1 至 " + limit + " 名士兵。")),
            PartyScreenClosedDelegate = (leftParty, left, lp, rightParty, right, rp, fromCancel) =>
            {
                if (fromCancel) cancelled();
                else completed(right);
            },
            IsDismissMode = true,
            DoNotApplyGoldTransactions = true,
            IsTroopUpgradesDisabled = true,
            TransferHealthiesGetWoundedsFirst = true,
            ShowProgressBar = false,
            MemberTransferState = PartyScreenLogic.TransferState.Transferable,
            PrisonerTransferState = PartyScreenLogic.TransferState.NotTransferable,
            AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
            PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
        };
    }
}
