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
        var logic = new PartyScreenLogic();
        var selected = TroopRoster.CreateDummyTroopRoster();
        var data = new PartyScreenLogicInitializationData
        {
            LeftOwnerParty = null,
            RightOwnerParty = null,
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
            IsTroopUpgradesDisabled = true,
            TransferHealthiesGetWoundedsFirst = true,
            ShowProgressBar = false,
            MemberTransferState = PartyScreenLogic.TransferState.Transferable,
            PrisonerTransferState = PartyScreenLogic.TransferState.NotTransferable,
            AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
            PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
        };
        logic.Initialize(data);
        var state = Game.Current.GameStateManager.CreateState<PartyState>();
        state.PartyScreenLogic = logic;
        state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
        Game.Current.GameStateManager.PushState(state);
    }
}
