using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementValueModel : SettlementValueModel
{
	private const float BenefitRatioForFaction = 0.33f;

	private const float CastleMultiplier = 1.25f;

	private const float SameMapFactionMultiplier = 1.1f;

	private const float SameCultureMultiplier = 1.1f;

	private const float BeingOwnerMultiplier = 1.1f;

	private const float HavingNoCoastalSettlementMultiplier = 1.4f;

	private const float HavingPortMultiplier = 1.2f;

	private const int SettlementAtWarWithClan = 10240;

	private const int HomeSettlementToOtherClanScore = 10;

	private const int AlreadyOwnerClanScoreForHomeSettlement = 5120;

	private const int SameFactionWithClanScoreForHomeSettlement = 2560;

	private const int SettlementTypeScoreForHomeSettlementTown = 1280;

	private const int SettlementTypeScoreForHomeSettlementCastle = 640;

	private const int SettlementTypeScoreForHomeSettlementVillage = 320;

	private const int MidSettlementDistanceScoreForHomeSettlement = 20;

	private const int SameCultureWithClanCultureScoreForHomeSettlement = 17;

	private const float AlreadyHomeSettlementScoreForHomeSettlement = 4.5f;

	private const float InitialHomeSettlementScoreForHomeSettlement = 3.5f;

	private const int SettlementOwnerClanCultureSameForHomeSettlement = 12;

	private const int NeighborScoreForHomeSettlement = 10;

	private const int ProsperityScoreForHomeSettlement = 5;

	private static float GetSettlementScoreForBeingHomeSettlementOfClan(Settlement settlement, Clan clan, float maxDistanceOfSettlementsToHomeSettlement)
	{
		float num = 0f;
		if (clan.IsRebelClan || clan.IsBanditFaction || clan.MapFaction.Settlements.Count == 0)
		{
			num = ((settlement != clan.InitialHomeSettlement) ? float.MinValue : float.MaxValue);
		}
		else
		{
			if (settlement.SettlementComponent is Hideout || settlement.SettlementComponent is RetirementSettlementComponent)
			{
				return float.MinValue;
			}
			if (settlement.MapFaction.IsAtWarWith(clan.MapFaction))
			{
				num -= 10240f;
			}
			if (settlement.OwnerClan == clan)
			{
				num += 5120f;
			}
			if (settlement.MapFaction == clan.MapFaction)
			{
				num += 2560f;
			}
			if (settlement.IsVillage)
			{
				num += 320f;
			}
			else if (settlement.IsCastle)
			{
				num += 640f;
			}
			else if (settlement.IsTown)
			{
				num += 1280f;
			}
			if (settlement == clan.HomeSettlement)
			{
				num += 4.5f;
			}
			if (settlement == clan.InitialHomeSettlement)
			{
				num += 3.5f;
			}
			if (settlement.Culture == clan.Culture)
			{
				num += 17f;
			}
			if (settlement.OwnerClan?.Culture == clan.Culture)
			{
				num += 12f;
			}
			Settlement factionMidSettlement = clan.MapFaction.FactionMidSettlement;
			if (clan.MapFaction.Settlements.Count > 1 && settlement != factionMidSettlement)
			{
				float num2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
				if (settlement.HasPort)
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, isFromPort: true, isTargetingPort: false, MobileParty.NavigationType.All);
					if (distance < num2)
					{
						num2 = distance;
					}
				}
				if (factionMidSettlement.HasPort)
				{
					float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, isFromPort: false, isTargetingPort: true, MobileParty.NavigationType.All);
					if (distance2 < num2)
					{
						num2 = distance2;
					}
					if (settlement.HasPort)
					{
						distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, isFromPort: true, isTargetingPort: true, MobileParty.NavigationType.All);
						if (distance2 < num2)
						{
							num2 = distance2;
						}
					}
				}
				float num3 = 20f - MBMath.Map(num2, 0f, maxDistanceOfSettlementsToHomeSettlement, 0f, 20f);
				num += num3;
			}
			else
			{
				num += 20f;
			}
			int num4 = CalculateTotalProsperity(settlement);
			float num5 = MBMath.Map(MathF.Sqrt(2500f + (float)num4) / 100f, 0.5f, 1f, 0f, 5f);
			num += num5;
			float num6 = MBMath.Map(SettlementHelper.GetNeighborScoreForConsideringClan(settlement, clan), -2f, 1f, -10f, 10f);
			num += num6;
			for (int i = 0; i < clan.Kingdom?.Clans.Count; i++)
			{
				Clan clan2 = clan.Kingdom.Clans[i];
				if (clan2 != clan && settlement == clan2.HomeSettlement)
				{
					num -= 10f;
				}
			}
		}
		return num;
	}

	public override Settlement FindMostSuitableHomeSettlement(Clan clan)
	{
		Settlement homeSettlement = null;
		if (Settlement.All == null || Settlement.All.Count == 0 || clan.IsRebelClan || clan.IsBanditFaction || clan.MapFaction.Settlements.Count == 0)
		{
			if (clan == Clan.PlayerClan && clan.InitialHomeSettlement == null)
			{
				return Settlement.All[0];
			}
			return clan.InitialHomeSettlement;
		}
		float maxScore = float.MinValue;
		float maxDistance = FindFarthestDistanceBetweenSettlementsInClan(clan);
		TryToFindHomeSettlementForClan(clan, clan.Fiefs.SelectQ((Town x) => x.Settlement), maxDistance, out homeSettlement, ref maxScore);
		if (maxScore < 5120f && clan.Kingdom != null)
		{
			TryToFindHomeSettlementForClan(clan, clan.Kingdom.Fiefs.SelectQ((Town x) => x.Settlement), maxDistance, out homeSettlement, ref maxScore);
		}
		if (maxScore < 2560f)
		{
			TryToFindHomeSettlementForClan(clan, Settlement.All, maxDistance, out homeSettlement, ref maxScore);
		}
		return homeSettlement;
	}

	private static void TryToFindHomeSettlementForClan(Clan clanToConsider, IEnumerable<Settlement> settlementsToConsider, float maxDistance, out Settlement homeSettlement, ref float maxScore)
	{
		homeSettlement = null;
		foreach (Settlement item in settlementsToConsider)
		{
			if (item.IsFortification || item.IsVillage || item.IsHideout)
			{
				float settlementScoreForBeingHomeSettlementOfClan = GetSettlementScoreForBeingHomeSettlementOfClan(item, clanToConsider, maxDistance);
				if (settlementScoreForBeingHomeSettlementOfClan > maxScore)
				{
					homeSettlement = item;
					maxScore = settlementScoreForBeingHomeSettlementOfClan;
				}
			}
		}
	}

	private static float FindFarthestDistanceBetweenSettlementsInClan(Clan clan)
	{
		float num = float.MinValue;
		foreach (Settlement settlement in clan.MapFaction.Settlements)
		{
			if (settlement == clan.MapFaction.FactionMidSettlement)
			{
				continue;
			}
			float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
			if (distance > num)
			{
				num = distance;
			}
			if (settlement.HasPort)
			{
				distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, isFromPort: false, isTargetingPort: true, MobileParty.NavigationType.All);
				if (distance > num)
				{
					num = distance;
				}
			}
			if (!clan.MapFaction.FactionMidSettlement.HasPort)
			{
				continue;
			}
			distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, isFromPort: true, isTargetingPort: false, MobileParty.NavigationType.All);
			if (distance > num)
			{
				num = distance;
			}
			if (settlement.HasPort)
			{
				distance = Campaign.Current.Models.MapDistanceModel.GetDistance(clan.MapFaction.FactionMidSettlement, settlement, isFromPort: true, isTargetingPort: true, MobileParty.NavigationType.All);
				if (distance > num)
				{
					num = distance;
				}
			}
		}
		return num;
	}

	private static int CalculateTotalProsperity(Settlement settlement)
	{
		int num = 0;
		if (settlement.IsFortification)
		{
			num = (int)settlement.Town.Prosperity;
			foreach (Village boundVillage in settlement.BoundVillages)
			{
				num += (int)boundVillage.Hearth;
			}
		}
		else if (settlement.IsVillage)
		{
			num = (int)settlement.Village.Hearth;
		}
		return num;
	}

	public override float CalculateSettlementBaseValue(Settlement settlement)
	{
		float num = (settlement.IsCastle ? 1.25f : 1f);
		float value = settlement.GetValue();
		float baseGeographicalAdvantage = GetBaseGeographicalAdvantage(settlement.IsVillage ? settlement.Village.Bound : settlement);
		return num * value * baseGeographicalAdvantage * 0.33f;
	}

	public override float CalculateSettlementValueForFaction(Settlement settlement, IFaction faction)
	{
		float num = (settlement.IsCastle ? 1.25f : 1f);
		float num2 = ((settlement.MapFaction == faction.MapFaction) ? 1.1f : 1f);
		float num3 = ((settlement.Culture == faction?.Culture) ? 1.1f : 1f);
		float value = settlement.GetValue();
		float num4 = GeographicalAdvantageForFaction(settlement.IsVillage ? settlement.Village.Bound : settlement, faction);
		float num5 = 1f;
		if (settlement.HasPort && settlement.IsFortification)
		{
			num5 = 1.2f;
			if (!faction.Settlements.Any((Settlement x) => x.HasPort))
			{
				num5 *= 1.4f;
			}
		}
		return value * num * num2 * num3 * num4 * num5 * 0.33f;
	}

	public override float CalculateSettlementValueForEnemyHero(Settlement settlement, Hero hero)
	{
		float num = (settlement.IsCastle ? 1.25f : 1f);
		float num2 = ((settlement.OwnerClan == hero.Clan) ? 1.1f : 1f);
		float num3 = ((settlement.Culture == hero.Culture) ? 1.1f : 1f);
		float value = settlement.GetValue();
		float num4 = GeographicalAdvantageForFaction(settlement.IsVillage ? settlement.Village.Bound : settlement, hero.MapFaction);
		float num5 = 1f;
		if (settlement.HasPort && settlement.IsFortification)
		{
			num5 = 1.2f;
			if (!hero.Clan.Settlements.Any((Settlement x) => x.HasPort))
			{
				num5 *= 1.4f;
			}
		}
		return value * num * num3 * num2 * num4 * num5 * 0.33f;
	}

	private static float GetBaseGeographicalAdvantage(Settlement settlement)
	{
		float num = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement.MapFaction.FactionMidSettlement, settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All) / Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
		return 1f / (1f + num);
	}

	private static float GeographicalAdvantageForFaction(Settlement settlement, IFaction faction)
	{
		Settlement factionMidSettlement = faction.FactionMidSettlement;
		float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, factionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
		if (faction.FactionMidSettlement.MapFaction != faction)
		{
			return MathF.Clamp(Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) / (distance + 0.1f), 0f, 4f);
		}
		float distanceToClosestNonAllyFortification = faction.DistanceToClosestNonAllyFortification;
		if (settlement.MapFaction == faction && distance < distanceToClosestNonAllyFortification)
		{
			return MathF.Clamp(Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) / (distanceToClosestNonAllyFortification - distance), 1f, 4f);
		}
		float num = (distance - distanceToClosestNonAllyFortification) / Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
		return 1f / (1f + num);
	}
}
