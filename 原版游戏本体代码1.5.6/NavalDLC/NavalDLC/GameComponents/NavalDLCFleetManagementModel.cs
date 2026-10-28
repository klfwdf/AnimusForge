using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCFleetManagementModel : FleetManagementModel
{
	public override int MinimumTroopCountRequiredToSendShips => 8;

	public override bool CanTroopsReturn()
	{
		if (!Hero.MainHero.IsPrisoner && (!MobileParty.MainParty.IsCurrentlyAtSea || Settlement.CurrentSettlement != null))
		{
			return MobileParty.MainParty.MapEvent == null;
		}
		return false;
	}

	public override CampaignTime GetReturnTimeForTroops(Ship ship)
	{
		return CampaignTime.DaysFromNow(Hero.MainHero.RandomFloatWithSeed((uint)CampaignTime.Now.ToMinutes, 3f, 6f));
	}

	public override bool CanSendShipToPlayerClan(Ship ship, int playerShipsCount, int troopsCountToSend, out TextObject hint)
	{
		hint = TextObject.GetEmpty();
		bool result = true;
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			hint = new TextObject("{=lwbwTg5b}You can't perform this action during this time.");
			result = false;
		}
		else if (!ship.IsTradeable || ship.IsUsedByQuest)
		{
			hint = GameTexts.FindText("str_port_cant_take_action_quest_ship");
			result = false;
		}
		else if (playerShipsCount == 1 && MobileParty.MainParty.IsCurrentlyAtSea)
		{
			hint = GameTexts.FindText("str_cannot_give_all_ships");
			result = false;
		}
		else if (Clan.PlayerClan.WarPartyComponents.AllQ((WarPartyComponent x) => !Campaign.Current.Models.ShipDistributionModel.CanSendShipToParty(ship, x.MobileParty)))
		{
			hint = new TextObject("{=SwV5iZbN}There are no suitable parties in your clan to send ships to.");
			result = false;
		}
		else if (MobileParty.MainParty.MemberRoster.TotalRegulars - troopsCountToSend < Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips)
		{
			hint = new TextObject("{=U4avdcnH}You need at least {NUMBER} troops to send with the ship.");
			hint.SetTextVariable("NUMBER", Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips);
			result = false;
		}
		else if (MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent.PlayerSide != MobileParty.MainParty.MapEvent.WinningSide)
		{
			hint = GameTexts.FindText("str_action_disabled_reason_encounter");
			result = false;
		}
		else
		{
			hint = new TextObject("{=iRfrlsB8}{NUMBER} troops will spend {DAYS} {?DAYS > 1}days{?}day{\\?} to deliver the ship and return to your party.");
			hint.SetTextVariable("NUMBER", Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips);
			int variable = MathF.Round(Campaign.Current.Models.FleetManagementModel.GetReturnTimeForTroops(ship).RemainingDaysFromNow);
			hint.SetTextVariable("DAYS", variable);
		}
		return result;
	}
}
