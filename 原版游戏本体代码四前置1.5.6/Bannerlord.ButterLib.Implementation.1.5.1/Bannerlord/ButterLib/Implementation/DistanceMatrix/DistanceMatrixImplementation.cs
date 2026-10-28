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

internal sealed class DistanceMatrixImplementation<T> : DistanceMatrix<T> where T : MBObjectBase
{
	private class TupleComparer : IComparer<(T OtherObject, float Distance)>
	{
		public int Compare((T OtherObject, float Distance) x, (T OtherObject, float Distance) y)
		{
			int num = Comparer<float>.Default.Compare(x.Distance, y.Distance);
			if (num != 0)
			{
				return num;
			}
			return Comparer<uint>.Default.Compare(x.OtherObject.Id.InternalValue, y.OtherObject.Id.InternalValue);
		}
	}

	private readonly Dictionary<ulong, float> _distanceMatrix;

	private readonly Dictionary<(T Object1, T Object2), float> _typedDistanceMatrix;

	private readonly Dictionary<T, SortedSet<(T OtherObject, float Distance)>> _flatenedDictionary;

	private readonly Func<IEnumerable<T>>? _entityListGetter;

	private readonly Func<T, T, object[]?, float>? _distanceCalculator;

	private readonly object[]? _distanceCalculatorArgs;

	private Dictionary<MBGUID, MBObjectBase> _cachedMapping = new Dictionary<MBGUID, MBObjectBase>();

	public override Dictionary<ulong, float> AsDictionary => _distanceMatrix;

	public override Dictionary<(T Object1, T Object2), float> AsTypedDictionary => _typedDistanceMatrix;

	public override Dictionary<T, SortedSet<(T OtherObject, float Distance)>> AsFlatenedDictionary => _flatenedDictionary;

	public DistanceMatrixImplementation()
	{
		_entityListGetter = null;
		_distanceCalculator = null;
		_distanceCalculatorArgs = null;
		_distanceMatrix = CalculateDistanceMatrix();
		_typedDistanceMatrix = GetTypedDistanceMatrix();
		_flatenedDictionary = GetFlatenedDictionary();
	}

	public DistanceMatrixImplementation(Func<IEnumerable<T>> customListGetter, Func<T, T, object[]?, float> customDistanceCalculator, object[]? distanceCalculatorArgs = null)
	{
		_entityListGetter = customListGetter;
		_distanceCalculator = customDistanceCalculator;
		_distanceCalculatorArgs = distanceCalculatorArgs;
		_distanceMatrix = CalculateDistanceMatrix();
		_typedDistanceMatrix = GetTypedDistanceMatrix();
		_flatenedDictionary = GetFlatenedDictionary();
	}

	public override float GetDistance(T object1, T object2)
	{
		if (!_distanceMatrix.TryGetValue((object1.Id > object2.Id) ? ElegantPairHelper.Pair(object2.Id, object1.Id) : ElegantPairHelper.Pair(object1.Id, object2.Id), out var value))
		{
			return float.NaN;
		}
		return value;
	}

	public override void SetDistance(T object1, T object2, float distance)
	{
		_distanceMatrix[(object1.Id > object2.Id) ? ElegantPairHelper.Pair(object2.Id, object1.Id) : ElegantPairHelper.Pair(object1.Id, object2.Id)] = distance;
		_typedDistanceMatrix[(object1.Id > object2.Id) ? (object2, object1) : (object1, object2)] = distance;
		_flatenedDictionary[object1].RemoveWhere(((T OtherObject, float Distance) x) => x.OtherObject == object2);
		_flatenedDictionary[object1].Add((object2, distance));
		_flatenedDictionary[object2].RemoveWhere(((T OtherObject, float Distance) x) => x.OtherObject == object1);
		_flatenedDictionary[object2].Add((object1, distance));
	}

	public override IEnumerable<(T OtherObject, float Distance)> GetNearestNeighbours(T inquiredObject, int count)
	{
		return GetNearestNeighbours(inquiredObject, count, IsNotNaN());
	}

	public override IEnumerable<(T OtherObject, float Distance)> GetNearestNeighbours(T inquiredObject, int count, Func<(T OtherObject, float Distance), bool> searchPredicate)
	{
		if (!_flatenedDictionary.TryGetValue(inquiredObject, out SortedSet<(T, float)> value))
		{
			return Array.Empty<(T, float)>();
		}
		return value.Where<(T, float)>(searchPredicate).Take(count);
	}

	public override IEnumerable<(T OtherObject, float Distance)> GetNearestNeighboursNormalized(T inquiredObject, int count, float scaleMin = 0f, float scaleMax = 100f)
	{
		return GetNearestNeighboursNormalized(inquiredObject, count, IsNotNaN(), scaleMin, scaleMax);
	}

	public override IEnumerable<(T OtherObject, float Distance)> GetNearestNeighboursNormalized(T inquiredObject, int count, Func<(T OtherObject, float Distance), bool> searchPredicate, float scaleMin = 0f, float scaleMax = 100f)
	{
		if (_flatenedDictionary.TryGetValue(inquiredObject, out SortedSet<(T, float)> value))
		{
			List<(T, float)> list = value.Where<(T, float)>(searchPredicate).ToList();
			GetRanges(scaleMin, scaleMax, list, out var value2, out var scale);
			return list.Select<(T, float), (T, float)>(((T OtherObject, float Distance) i) => (OtherObject: i.OtherObject, Distance: float.IsNaN(i.Distance) ? scale.Min : ((value2.Range == 0f) ? scale.Max : (scale.Range * (i.Distance - value2.Min) / value2.Range + scale.Min)))).Take(count);
		}
		return Array.Empty<(T, float)>();
	}

	private static List<TObject> GetRegisteredDistinct<TObject>(IEnumerable<TObject> source) where TObject : MBObjectBase
	{
		return (from x in source
			where x.Id.InternalValue != 0
			group x by x.Id into g
			select g.First()).ToList();
	}

	private static Func<(T OtherObject, float Distance), bool> IsNotNaN()
	{
		return ((T OtherObject, float Distance) x) => !float.IsNaN(x.Distance);
	}

	private static void GetRanges(float scaleMin, float scaleMax, ICollection<(T OtherObject, float Distance)> nearestNeighbors, out (float Min, float Max, float Range) value, out (float Min, float Max, float Range) scale)
	{
		List<float> list = (from x in nearestNeighbors.Where<(T, float)>(IsNotNaN())
			select x.Distance).ToList();
		value = ((list.Count > 0) ? (from g in list
			group g by 1).Select(delegate(IGrouping<int, float> g)
		{
			float num = g.Min((float x) => x);
			float num2 = g.Max((float x) => x);
			return (Min: num, Max: num2, Range: num2 - num);
		}).FirstOrDefault() : (Min: float.NaN, Max: float.NaN, Range: float.NaN));
		scale = (Min: scaleMin, Max: scaleMax, Range: scaleMax - scaleMin);
	}

	private T GetObject(MBGUID id)
	{
		if (!_cachedMapping.TryGetValue(id, out MBObjectBase value) || !(value is T result))
		{
			throw new ArgumentException($"Id '{id}' was not found!", "id");
		}
		return result;
	}

	private Dictionary<ulong, float> CalculateDistanceMatrix()
	{
		if (_entityListGetter != null && _distanceCalculator != null)
		{
			List<T> entities = GetRegisteredDistinct(_entityListGetter());
			_cachedMapping = ((IEnumerable<T>)entities).ToDictionary((Func<T, MBGUID>)((T key) => key.Id), (Func<T, MBObjectBase>)((T value) => value));
			return (from tuple in entities.SelectMany((T _) => entities, (T X, T Y) => (X: X, Y: Y))
				where tuple.X.Id < tuple.Y.Id
				select tuple).ToDictionary(((T X, T Y) key) => ElegantPairHelper.Pair(key.X.Id, key.Y.Id), ((T X, T Y) value) => _distanceCalculator(value.X, value.Y, _distanceCalculatorArgs));
		}
		if (typeof(Hero).IsAssignableFrom(typeof(T)))
		{
			List<Hero> activeHeroes = GetRegisteredDistinct(Hero.AllAliveHeroes.Where((Hero h) => !h.IsNotSpawned && !h.IsDisabled && !h.IsDead && !h.IsChild && !h.IsNotable));
			_cachedMapping = ((IEnumerable<Hero>)activeHeroes).ToDictionary((Func<Hero, MBGUID>)((Hero key) => key.Id), (Func<Hero, MBObjectBase>)((Hero value) => value));
			return (from tuple in activeHeroes.SelectMany((Hero _) => activeHeroes, (Hero X, Hero Y) => (X: X, Y: Y))
				where tuple.X.Id < tuple.Y.Id
				select tuple).ToDictionary(((Hero X, Hero Y) key) => ElegantPairHelper.Pair(key.X.Id, key.Y.Id), ((Hero X, Hero Y) value) => Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix.CalculateDistanceBetweenHeroes(value.X, value.Y).GetValueOrDefault());
		}
		if (typeof(Settlement).IsAssignableFrom(typeof(T)))
		{
			bool considerVillages = DistanceMatrixSubSystem.Instance?.ConsiderVillages ?? true;
			List<Settlement> settlements = GetRegisteredDistinct(Settlement.All.Where((Settlement s) => s.IsFortification || (considerVillages && s.IsVillage)));
			_cachedMapping = ((IEnumerable<Settlement>)settlements).ToDictionary((Func<Settlement, MBGUID>)((Settlement key) => key.Id), (Func<Settlement, MBObjectBase>)((Settlement value) => value));
			return (from tuple in settlements.SelectMany((Settlement _) => settlements, (Settlement X, Settlement Y) => (X: X, Y: Y))
				where tuple.X.Id < tuple.Y.Id
				select tuple).ToDictionary(((Settlement X, Settlement Y) key) => ElegantPairHelper.Pair(key.X.Id, key.Y.Id), ((Settlement X, Settlement Y) value) => Campaign.Current.Models.MapDistanceModel.GetDistance(value.X, value.Y, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default));
		}
		if (typeof(Clan).IsAssignableFrom(typeof(T)))
		{
			List<Clan> clans = GetRegisteredDistinct(Clan.All.Where((Clan c) => !c.IsEliminated && !c.IsBanditFaction));
			_cachedMapping = ((IEnumerable<Clan>)clans).ToDictionary((Func<Clan, MBGUID>)((Clan key) => key.Id), (Func<Clan, MBObjectBase>)((Clan value) => value));
			DistanceMatrixImplementation<Settlement> settlementDistanceMatrix = Campaign.Current.GetCampaignBehavior<GeopoliticsBehavior>().SettlementDistanceMatrix ?? new DistanceMatrixImplementation<Settlement>();
			Dictionary<ulong, WeightedDistance> lst = Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix.GetSettlementOwnersPairedList(settlementDistanceMatrix);
			return (from tuple in clans.SelectMany((Clan _) => clans, (Clan X, Clan Y) => (X: X, Y: Y))
				where tuple.X.Id < tuple.Y.Id
				select tuple).ToDictionary(((Clan X, Clan Y) key) => ElegantPairHelper.Pair(key.X.Id, key.Y.Id), ((Clan X, Clan Y) value) => Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix.CalculateDistanceBetweenClans(value.X, value.Y, lst).GetValueOrDefault());
		}
		if (typeof(Kingdom).IsAssignableFrom(typeof(T)))
		{
			List<Kingdom> kingdoms = GetRegisteredDistinct(Kingdom.All.Where((Kingdom k) => !k.IsEliminated));
			_cachedMapping = ((IEnumerable<Kingdom>)kingdoms).ToDictionary((Func<Kingdom, MBGUID>)((Kingdom key) => key.Id), (Func<Kingdom, MBObjectBase>)((Kingdom value) => value));
			DistanceMatrixImplementation<Clan> claDistanceMatrix = Campaign.Current.GetCampaignBehavior<GeopoliticsBehavior>().ClanDistanceMatrix ?? new DistanceMatrixImplementation<Clan>();
			return (from tuple in kingdoms.SelectMany((Kingdom _) => kingdoms, (Kingdom X, Kingdom Y) => (X: X, Y: Y))
				where tuple.X.Id < tuple.Y.Id
				select tuple).ToDictionary(((Kingdom X, Kingdom Y) key) => ElegantPairHelper.Pair(key.X.Id, key.Y.Id), ((Kingdom X, Kingdom Y) value) => Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix.CalculateDistanceBetweenKingdoms(value.X, value.Y, claDistanceMatrix).GetValueOrDefault());
		}
		throw new ArgumentException(typeof(T).FullName + " is not a supported type");
	}

	private Dictionary<(T Object1, T Object2), float> GetTypedDistanceMatrix()
	{
		return _distanceMatrix.ToDictionary((KeyValuePair<ulong, float> key) => ElegantPairHelper.UnPairMBGUID(key.Key), (KeyValuePair<ulong, float> value) => value.Value).ToDictionary((KeyValuePair<(MBGUID A, MBGUID B), float> key) => (GetObject(key.Key.A), GetObject(key.Key.B)), (KeyValuePair<(MBGUID A, MBGUID B), float> value) => value.Value);
	}

	private Dictionary<T, SortedSet<(T OtherObject, float Distance)>> GetFlatenedDictionary()
	{
		List<KeyValuePair<(T Object1, T Object2), float>> list = _typedDistanceMatrix.ToList();
		List<T> list2 = list.SelectMany((KeyValuePair<(T Object1, T Object2), float> kvp) => new T[2]
		{
			kvp.Key.Object1,
			kvp.Key.Object2
		}).Distinct().ToList();
		Dictionary<T, SortedSet<(T OtherObject, float Distance)>> result = new Dictionary<T, SortedSet<(T, float)>>();
		list2.ForEach(delegate(T key)
		{
			List<(T, float)> collection = (from kvp in list
				where kvp.Key.Object1 == key || kvp.Key.Object2 == key
				select (OtherObject: (kvp.Key.Object1 == key) ? kvp.Key.Object2 : kvp.Key.Object1, Distance: kvp.Value)).Distinct().ToList();
			SortedSet<(T, float)> value = new SortedSet<(T, float)>(collection, new TupleComparer());
			result.Add(key, value);
		});
		return result;
	}
}
