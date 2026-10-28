using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.ButterLib.DistanceMatrix;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.Implementation.DistanceMatrix;

internal sealed class DistanceMatrixStaticImplementation : IDistanceMatrixStatic
{
	public record OwnersDistancePaired(ulong Owners, float Distance, float Weight);

	private record OwnersDistanceUnpaired(MBGUID OwnerId1, MBGUID OwnerId2, float Distance, float Weight);

	public DistanceMatrix<T> Create<T>() where T : MBObjectBase
	{
		return new DistanceMatrixImplementation<T>();
	}

	public DistanceMatrix<T> Create<T>(Func<IEnumerable<T>> customListGetter, Func<T, T, object[]?, float> customDistanceCalculator, object[]? distanceCalculatorArgs = null) where T : MBObjectBase
	{
		return new DistanceMatrixImplementation<T>(customListGetter, customDistanceCalculator, distanceCalculatorArgs);
	}

	public float CalculateDistanceBetweenHeroes(Hero hero1, Hero hero2)
	{
		var (mobileParty, settlement) = GetMapPosition(hero1);
		var (mobileParty2, settlement2) = GetMapPosition(hero2);
		if (settlement == null || settlement2 == null)
		{
			float landRatio;
			if (mobileParty == null || mobileParty2 == null)
			{
				if (settlement == null || mobileParty2 == null)
				{
					if (mobileParty == null || settlement2 == null)
					{
						return float.NaN;
					}
					return Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, settlement2, isTargetingPort: false, MobileParty.NavigationType.Default, out landRatio);
				}
				return Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty2, settlement, isTargetingPort: false, MobileParty.NavigationType.Default, out landRatio);
			}
			if (!Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, mobileParty2, MobileParty.NavigationType.Default, float.MaxValue, out var distance, out landRatio))
			{
				return float.NaN;
			}
			return distance;
		}
		return Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, settlement2, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default);
	}

	public float CalculateDistanceBetweenClans(Clan clan1, Clan clan2, Dictionary<ulong, WeightedDistance> settlementOwnersPairedList)
	{
		ulong key = ((clan1.Id > clan2.Id) ? ElegantPairHelper.Pair(clan2.Id, clan1.Id) : ElegantPairHelper.Pair(clan1.Id, clan2.Id));
		if (settlementOwnersPairedList.TryGetValue(key, out WeightedDistance value))
		{
			return (1f + value.Distance) / (1f + value.Weight);
		}
		return float.NaN;
	}

	public float CalculateDistanceBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, DistanceMatrix<Clan> clanDistanceMatrix)
	{
		List<WeightedDistance> settlementDistances = clanDistanceMatrix.AsTypedDictionary.Where(Predicate).Select(WeightedSettlementSelector).ToList();
		return GetWeightedMeanDistance(settlementDistances);
		bool Predicate(KeyValuePair<(Clan object1, Clan object2), float> x)
		{
			if (!float.IsNaN(x.Value))
			{
				if (x.Key.object1.Kingdom != kingdom1 || x.Key.object2.Kingdom != kingdom2)
				{
					if (x.Key.object2.Kingdom == kingdom1)
					{
						return x.Key.object1.Kingdom == kingdom2;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		static WeightedDistance WeightedSettlementSelector(KeyValuePair<(Clan object1, Clan object2), float> x)
		{
			return new WeightedDistance(x.Value, x.Key.object1.Tier + x.Key.object2.Tier);
		}
	}

	public Dictionary<ulong, WeightedDistance> GetSettlementOwnersPairedList(DistanceMatrix<Settlement> settlementDistanceMatrix)
	{
		return (from g in settlementDistanceMatrix.AsTypedDictionary.Select(FirstSelector).Select(SecondSelector)
			group g by g.Owners into g
			select new OwnersDistancePaired(g.Key, g.Sum((OwnersDistancePaired x) => x.Distance * x.Weight), g.Sum((OwnersDistancePaired x) => x.Weight))).ToDictionary((OwnersDistancePaired key) => key.Owners, (OwnersDistancePaired value) => new WeightedDistance(value.Distance, value.Weight));
		static OwnersDistanceUnpaired FirstSelector(KeyValuePair<(Settlement Object1, Settlement Object2), float> kvp)
		{
			return new OwnersDistanceUnpaired(kvp.Key.Object1.OwnerClan.Id, kvp.Key.Object2.OwnerClan.Id, kvp.Value, GetSettlementWeight(kvp.Key.Object1) + GetSettlementWeight(kvp.Key.Object2));
		}
		static OwnersDistancePaired SecondSelector(OwnersDistanceUnpaired x)
		{
			return new OwnersDistancePaired((x.OwnerId1 > x.OwnerId2) ? ElegantPairHelper.Pair(x.OwnerId2, x.OwnerId1) : ElegantPairHelper.Pair(x.OwnerId1, x.OwnerId2), x.Distance, x.Weight);
		}
	}

	private static (MobileParty? mobileParty, Settlement? settlement) GetMapPosition(Hero hero)
	{
		if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null)
		{
			if (hero.CurrentSettlement == null || hero.IsFugitive)
			{
				if (hero.PartyBelongedTo == null)
				{
					return (mobileParty: null, settlement: null);
				}
				return (mobileParty: hero.PartyBelongedTo, settlement: null);
			}
			return (mobileParty: null, settlement: hero.CurrentSettlement);
		}
		if (!hero.PartyBelongedToAsPrisoner.IsSettlement)
		{
			return (mobileParty: hero.PartyBelongedToAsPrisoner.MobileParty, settlement: null);
		}
		return (mobileParty: null, settlement: hero.PartyBelongedToAsPrisoner.Settlement);
	}

	private static float GetWeightedMeanDistance(IReadOnlyCollection<WeightedDistance> settlementDistances)
	{
		if (!settlementDistances.Any((WeightedDistance x) => x.Weight > 0f))
		{
			return float.NaN;
		}
		return (float)(((double)settlementDistances.Sum((WeightedDistance x) => x.Distance * x.Weight) + 1.0) / ((double)settlementDistances.Sum((WeightedDistance x) => x.Weight) + 1.0));
	}

	private static float GetSettlementWeight(Settlement settlement)
	{
		if (!settlement.IsTown)
		{
			if (!settlement.IsCastle)
			{
				if (!settlement.IsVillage)
				{
					return 0f;
				}
				return 0.5f;
			}
			return 1f;
		}
		return 2f;
	}
}
