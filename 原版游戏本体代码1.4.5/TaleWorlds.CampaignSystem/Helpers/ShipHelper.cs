using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Helpers;

public static class ShipHelper
{
	public const int NavalRaidMissionShipLimit = 3;

	public static Banner GetShipBanner(IShipOrigin shipOrigin, IAgent captain = null)
	{
		if (captain?.Character is CharacterObject { IsHero: not false } characterObject)
		{
			return characterObject.HeroObject.ClanBanner;
		}
		if (shipOrigin is Ship { Owner: not null } ship)
		{
			if (ship.Owner.IsMobile && ship.Owner.MobileParty.Army != null)
			{
				return ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Banner;
			}
			return ship.Owner.Banner;
		}
		return Banner.CreateOneColoredEmptyBanner(92);
	}

	public static (uint sailColor1, uint sailColor2) GetSailColors(IShipOrigin shipOrigin, IAgent captain = null)
	{
		(uint, uint) result = (4291609515u, 4291609515u);
		if (captain?.Character is CharacterObject { IsHero: not false } characterObject)
		{
			result.Item1 = characterObject.HeroObject.MapFaction.Color;
			result.Item2 = characterObject.HeroObject.MapFaction.Color2;
		}
		else if (shipOrigin is Ship { Owner: not null } ship)
		{
			if (ship.Owner.IsMobile && ship.Owner.MobileParty.Army != null)
			{
				result.Item1 = ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Color;
				result.Item2 = ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Color2;
			}
			else
			{
				result.Item1 = ship.Owner.MapFaction.Color;
				result.Item2 = ship.Owner.MapFaction.Color2;
			}
		}
		return result;
	}

	public static Banner GetShipBanner(PartyBase party = null)
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

	public static (uint sailColor1, uint sailColor2) GetSailColors(PartyBase party = null)
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
}
