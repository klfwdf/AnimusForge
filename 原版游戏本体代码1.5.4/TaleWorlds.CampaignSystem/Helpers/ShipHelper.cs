using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Helpers;

public static class ShipHelper
{
	public const int NavalRaidMissionShipLimit = 3;

	public static bool TryGetShipBanner(IShipOrigin shipOrigin, out Banner banner, IAgent captain = null)
	{
		banner = Banner.CreateOneColoredEmptyBanner(92);
		if (captain?.Character is CharacterObject { IsHero: not false } characterObject)
		{
			banner = characterObject.HeroObject.ClanBanner;
			return true;
		}
		if (shipOrigin is Ship { Owner: not null } ship)
		{
			if (ship.Owner.IsMobile && ship.Owner.MobileParty.Army != null)
			{
				banner = ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Banner;
			}
			else
			{
				banner = ship.Owner.Banner;
			}
			return true;
		}
		return false;
	}

	public static bool TryGetSailColors(IShipOrigin shipOrigin, out (uint sailColor1, uint sailColor2) sailColors, IAgent captain = null)
	{
		sailColors = (sailColor1: 4291609515u, sailColor2: 4291609515u);
		if (captain?.Character is CharacterObject { IsHero: not false } characterObject)
		{
			sailColors.sailColor1 = characterObject.HeroObject.MapFaction.Color;
			sailColors.sailColor2 = characterObject.HeroObject.MapFaction.Color2;
			return true;
		}
		if (shipOrigin is Ship { Owner: not null } ship)
		{
			if (ship.Owner.IsMobile && ship.Owner.MobileParty.Army != null)
			{
				sailColors.sailColor1 = ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Color;
				sailColors.sailColor2 = ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Color2;
			}
			else
			{
				sailColors.sailColor1 = ship.Owner.MapFaction.Color;
				sailColors.sailColor2 = ship.Owner.MapFaction.Color2;
			}
			return true;
		}
		return false;
	}

	public static Banner GetShipBannerForParty(PartyBase party = null)
	{
		if (party != null)
		{
			if (party.IsMobile && party.MobileParty.Army != null)
			{
				return party.MobileParty.Army.LeaderParty.MapFaction.Banner;
			}
			return party.Banner;
		}
		return Banner.CreateOneColoredEmptyBanner(92);
	}

	public static (uint sailColor1, uint sailColor2) GetSailColorsForParty(PartyBase party = null)
	{
		(uint, uint) result = (4291609515u, 4291609515u);
		if (party != null)
		{
			if (party.IsMobile && party.MobileParty.Army != null)
			{
				result.Item1 = party.MobileParty.Army.LeaderParty.MapFaction.Color;
				result.Item2 = party.MobileParty.Army.LeaderParty.MapFaction.Color2;
			}
			else
			{
				result.Item1 = party.Owner.MapFaction.Color;
				result.Item2 = party.Owner.MapFaction.Color2;
			}
		}
		return result;
	}

	public static List<Ship> GetOrderedNavalRaidShipsOfPlayerParty()
	{
		List<Ship> list = new List<Ship>();
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.ShipHull.CanNavigateShallowWater)
			{
				list.Add(ship);
			}
		}
		return list.OrderByDescending((Ship x) => x.ShipHull.MainDeckCrewCapacity).Take(3).ToList();
	}

	public static MobileParty GetClanPartyToGetAvailableShip(Ship ship, Clan clan, out bool doesPartyNeedShips)
	{
		MobileParty mobileParty = null;
		float num = float.MinValue;
		MBList<Ship> mBList = new MBList<Ship>();
		doesPartyNeedShips = false;
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.Party != ship.Owner && Campaign.Current.Models.ShipDistributionModel.CanSendShipToParty(ship, warPartyComponent.MobileParty) && (mobileParty == null || warPartyComponent.Party.Ships.Count <= mobileParty.Ships.Count))
			{
				mBList.Clear();
				mBList.AddRange(warPartyComponent.Party.Ships);
				float scoreForPartyShipComposition = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(warPartyComponent.MobileParty, mBList);
				mBList.Add(ship);
				float num2 = Campaign.Current.Models.ShipDistributionModel.GetScoreForPartyShipComposition(warPartyComponent.MobileParty, mBList) - scoreForPartyShipComposition;
				if (num2 > num)
				{
					mobileParty = warPartyComponent.MobileParty;
					num = num2;
				}
			}
		}
		if (num > 0f)
		{
			doesPartyNeedShips = true;
		}
		return mobileParty;
	}

	public static int GetAmountToRecoverFromRemainingShipsAfterDistribution(MBReadOnlyList<Ship> shipsToRecover, MobileParty seller)
	{
		int num = (int)shipsToRecover.SumQ((Ship x) => Campaign.Current.Models.ShipCostModel.GetShipTradeValue(x, seller.Party, null));
		if (seller.ActualClan == Clan.PlayerClan)
		{
			float shipSellingPenalty = Campaign.Current.Models.ShipCostModel.GetShipSellingPenalty();
			num = (int)((float)num * shipSellingPenalty);
		}
		return num;
	}
}
