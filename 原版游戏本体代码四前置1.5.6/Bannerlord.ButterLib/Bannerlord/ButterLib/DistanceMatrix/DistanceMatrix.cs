using System;
using System.Collections.Generic;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.DistanceMatrix;

public abstract class DistanceMatrix
{
	private static IDistanceMatrixStatic? _staticInstance;

	internal static IDistanceMatrixStatic? StaticInstance => _staticInstance ?? (_staticInstance = ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IDistanceMatrixStatic>());

	public static DistanceMatrix<T>? Create<T>() where T : MBObjectBase
	{
		return StaticInstance?.Create<T>();
	}

	public static DistanceMatrix<T>? Create<T>(Func<IEnumerable<T>> customListGetter, Func<T, T, object[]?, float> customDistanceCalculator, object[]? distanceCalculatorArgs) where T : MBObjectBase
	{
		return StaticInstance?.Create(customListGetter, customDistanceCalculator, distanceCalculatorArgs);
	}

	public static float? CalculateDistanceBetweenHeroes(Hero hero1, Hero hero2)
	{
		return StaticInstance?.CalculateDistanceBetweenHeroes(hero1, hero2);
	}

	public static float? CalculateDistanceBetweenClans(Clan clan1, Clan clan2, Dictionary<ulong, WeightedDistance> settlementOwnersPairedList)
	{
		return StaticInstance?.CalculateDistanceBetweenClans(clan1, clan2, settlementOwnersPairedList);
	}

	public static float? CalculateDistanceBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, DistanceMatrix<Clan> clanDistanceMatrix)
	{
		return StaticInstance?.CalculateDistanceBetweenKingdoms(kingdom1, kingdom2, clanDistanceMatrix);
	}

	public static Dictionary<ulong, WeightedDistance>? GetSettlementOwnersPairedList(DistanceMatrix<Settlement> settlementDistanceMatrix)
	{
		return StaticInstance?.GetSettlementOwnersPairedList(settlementDistanceMatrix);
	}
}
public abstract class DistanceMatrix<T> : DistanceMatrix where T : MBObjectBase
{
	public abstract Dictionary<ulong, float> AsDictionary { get; }

	public abstract Dictionary<(T Object1, T Object2), float> AsTypedDictionary { get; }

	public abstract Dictionary<T, SortedSet<(T OtherObject, float Distance)>> AsFlatenedDictionary { get; }

	public static DistanceMatrix<T>? Create()
	{
		return DistanceMatrix.StaticInstance?.Create<T>();
	}

	public static DistanceMatrix<T>? Create(Func<IEnumerable<T>> customListGetter, Func<T, T, object[]?, float> customDistanceCalculator, object[]? distanceCalculatorArgs)
	{
		return DistanceMatrix.StaticInstance?.Create(customListGetter, customDistanceCalculator, distanceCalculatorArgs);
	}

	protected DistanceMatrix()
	{
	}

	protected DistanceMatrix(Func<IEnumerable<T>> customListGetter, Func<T, T, float> customDistanceCalculator)
	{
	}

	public abstract float GetDistance(T object1, T object2);

	public abstract void SetDistance(T object1, T object2, float distance);

	public abstract IEnumerable<(T OtherObject, float Distance)> GetNearestNeighbours(T inquiredObject, int count);

	public abstract IEnumerable<(T OtherObject, float Distance)> GetNearestNeighbours(T inquiredObject, int count, Func<(T OtherObject, float Distance), bool> searchPredicate);

	public abstract IEnumerable<(T OtherObject, float Distance)> GetNearestNeighboursNormalized(T inquiredObject, int count, float scaleMin = 0f, float scaleMax = 100f);

	public abstract IEnumerable<(T OtherObject, float Distance)> GetNearestNeighboursNormalized(T inquiredObject, int count, Func<(T OtherObject, float Distance), bool> searchPredicate, float scaleMin = 0f, float scaleMax = 100f);
}
