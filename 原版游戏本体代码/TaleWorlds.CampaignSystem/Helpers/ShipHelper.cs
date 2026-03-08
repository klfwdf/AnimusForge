using System;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Helpers
{
	// Token: 0x02000027 RID: 39
	public static class ShipHelper
	{
		// Token: 0x06000177 RID: 375 RVA: 0x00010BD8 File Offset: 0x0000EDD8
		public static Banner GetShipBanner(IShipOrigin shipOrigin, IAgent captain = null)
		{
			CharacterObject characterObject;
			if ((characterObject = ((captain != null) ? captain.Character : null) as CharacterObject) != null && characterObject.IsHero)
			{
				return characterObject.HeroObject.ClanBanner;
			}
			Ship ship;
			if ((ship = shipOrigin as Ship) == null || ship.Owner == null)
			{
				return null;
			}
			if (ship.Owner.IsMobile && ship.Owner.MobileParty.Army != null)
			{
				return ship.Owner.MobileParty.Army.LeaderParty.MapFaction.Banner;
			}
			return ship.Owner.Banner;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00010C6C File Offset: 0x0000EE6C
		[return: TupleElementNames(new string[] { "sailColor1", "sailColor2" })]
		public static ValueTuple<uint, uint> GetSailColors(IShipOrigin shipOrigin, IAgent captain = null)
		{
			ValueTuple<uint, uint> result = new ValueTuple<uint, uint>(4291609515U, 4291609515U);
			CharacterObject characterObject;
			Ship ship;
			if ((characterObject = ((captain != null) ? captain.Character : null) as CharacterObject) != null && characterObject.IsHero)
			{
				result.Item1 = characterObject.HeroObject.MapFaction.Color;
				result.Item2 = characterObject.HeroObject.MapFaction.Color2;
			}
			else if ((ship = shipOrigin as Ship) != null && ship.Owner != null)
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

		// Token: 0x06000179 RID: 377 RVA: 0x00010D90 File Offset: 0x0000EF90
		public static Banner GetShipBanner(PartyBase party = null)
		{
			if (party == null)
			{
				return Banner.CreateOneColoredEmptyBanner(92);
			}
			if (party.IsMobile && party.MobileParty.Army != null)
			{
				return party.MobileParty.Army.LeaderParty.MapFaction.Banner;
			}
			return party.Banner;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00010DE0 File Offset: 0x0000EFE0
		[return: TupleElementNames(new string[] { "sailColor1", "sailColor2" })]
		public static ValueTuple<uint, uint> GetSailColors(PartyBase party = null)
		{
			ValueTuple<uint, uint> result = new ValueTuple<uint, uint>(4291609515U, 4291609515U);
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
	}
}
