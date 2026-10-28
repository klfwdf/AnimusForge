using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.DistanceMatrix;

internal interface IDistanceMatrixStatic
{
	DistanceMatrix<T> Create<T>() where T : MBObjectBase;

	DistanceMatrix<T> Create<T>(Func<IEnumerable<T>> customListGetter, Func<T, T, object[]?, float> customDistanceCalculator, object[]? distanceCalculatorArgs) where T : MBObjectBase;

	float CalculateDistanceBetweenHeroes(Hero hero1, Hero hero2);

	float CalculateDistanceBetweenClans(Clan clan1, Clan clan2, Dictionary<ulong, WeightedDistance> settlementOwnersPairedList);

	float CalculateDistanceBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, DistanceMatrix<Clan> clanDistanceMatrix);

	Dictionary<ulong, WeightedDistance> GetSettlementOwnersPairedList(DistanceMatrix<Settlement> settlementDistanceMatrix);
}
