using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalDLCShipDistributionModel : ShipDistributionModel
{
	private const float CulturePenalty = 0.96f;

	public override bool CanPartyTakeShip(PartyBase party, Ship ship)
	{
		if (party.IsMobile && party.MobileParty.IsBandit && ship.ShipHull.Type == ShipHull.ShipType.Heavy)
		{
			return false;
		}
		return true;
	}

	public override bool CanSendShipToParty(Ship ship, MobileParty mobileParty)
	{
		if (mobileParty != MobileParty.MainParty && mobileParty.IsActive && (!mobileParty.IsCurrentlyAtSea || mobileParty.MapEvent == null) && !mobileParty.IsDisbanding && !mobileParty.IsCaravan && !mobileParty.IsCurrentlyUsedByAQuest && !mobileParty.IsMilitia && !mobileParty.IsPatrolParty && !mobileParty.IsVillager)
		{
			return mobileParty.LeaderHero?.CanHaveFleet ?? true;
		}
		return false;
	}

	public override float GetScoreForPartyShipComposition(MobileParty party, MBReadOnlyList<Ship> shipsToConsider)
	{
		if (shipsToConsider.Count == 0)
		{
			return 0f;
		}
		float num = 1f;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		CultureObject cultureObject = null;
		if (party.ActualClan != null)
		{
			cultureObject = party.ActualClan.Culture;
		}
		else if (party.MapFaction != null)
		{
			cultureObject = party.MapFaction.Culture;
		}
		foreach (Ship item in shipsToConsider)
		{
			if (!cultureObject.AvailableShipHulls.ContainsQ(item.ShipHull))
			{
				num *= 0.96f;
			}
			switch (item.ShipHull.Type)
			{
			case ShipHull.ShipType.Light:
				num2++;
				break;
			case ShipHull.ShipType.Medium:
				num3++;
				break;
			case ShipHull.ShipType.Heavy:
				num4++;
				break;
			}
		}
		if (num2 < 1)
		{
			num *= 0.85f;
		}
		if (num3 < 1)
		{
			num *= 0.9f;
		}
		if (num4 < 1)
		{
			num *= 0.95f;
		}
		int num5 = shipsToConsider.SumQ((Ship x) => x.SkeletalCrewCapacity);
		int num6 = (int)Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(party.Party).ResultNumber;
		float num7 = (float)num6 * 0.5f;
		int idealShipNumber = Campaign.Current.Models.PartyShipLimitModel.GetIdealShipNumber(party);
		if (num7 < (float)num5)
		{
			float num8 = 1f - ((float)num5 - num7) / (float)num5 * 0.5f;
			num *= num8;
		}
		else if (shipsToConsider.Count > idealShipNumber)
		{
			num *= 2f / (float)(shipsToConsider.Count - idealShipNumber + 1);
		}
		int num9 = shipsToConsider.SumQ((Ship x) => x.ShipHull.TotalCrewCapacity);
		if ((float)num9 < (float)num6 * 0.85f)
		{
			num *= (float)num9 / (float)num6 * 15f / 85f + 0.85f;
		}
		else if ((float)num9 > (float)num6 * 1.3f)
		{
			num *= 0.8f;
		}
		return num;
	}
}
