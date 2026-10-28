using System;
using System.Collections.Generic;

namespace Bannerlord.ModuleManager;

internal static class CollectionsExtensions
{
	public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
	{
		int num = 0;
		foreach (T item in self)
		{
			if (object.Equals(item, elementToFind))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public static int IndexOf<T>(this IReadOnlyList<T> self, Func<T, bool> preficate)
	{
		int num = 0;
		foreach (T item in self)
		{
			if (preficate(item))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.DistinctBy(keySelector, null);
	}

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		return DistinctByIterator(source, keySelector, comparer);
	}

	private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			yield break;
		}
		HashSet<TKey> set = new HashSet<TKey>(comparer);
		do
		{
			TSource current = enumerator.Current;
			if (set.Add(keySelector(current)))
			{
				yield return current;
			}
		}
		while (enumerator.MoveNext());
	}
}
