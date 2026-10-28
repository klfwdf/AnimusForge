using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Linq;

internal static class ImmutableArrayExtensions
{
	public static IEnumerable<TResult> Select<T, TResult>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, TResult> selector)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		return immutableArray.array.Select(selector);
	}

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Immutable.ImmutableArray<TSource> immutableArray, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		if (collectionSelector == null || resultSelector == null)
		{
			return Enumerable.SelectMany(immutableArray, collectionSelector, resultSelector);
		}
		if (immutableArray.Length != 0)
		{
			return immutableArray.SelectManyIterator(collectionSelector, resultSelector);
		}
		return Enumerable.Empty<TResult>();
	}

	public static IEnumerable<T> Where<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		return immutableArray.array.Where(predicate);
	}

	public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		return immutableArray.Length > 0;
	}

	public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.NotNull(predicate, "predicate");
		T[] array = immutableArray.array;
		foreach (T arg in array)
		{
			if (predicate(arg))
			{
				return true;
			}
		}
		return false;
	}

	public static bool All<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.NotNull(predicate, "predicate");
		T[] array = immutableArray.array;
		foreach (T arg in array)
		{
			if (!predicate(arg))
			{
				return false;
			}
		}
		return true;
	}

	public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, System.Collections.Immutable.ImmutableArray<TDerived> items, IEqualityComparer<TBase>? comparer = null) where TDerived : TBase
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		items.ThrowNullRefIfNotInitialized();
		if ((object)immutableArray.array == items.array)
		{
			return true;
		}
		if (immutableArray.Length != items.Length)
		{
			return false;
		}
		if (comparer == null)
		{
			comparer = EqualityComparer<TBase>.Default;
		}
		for (int i = 0; i < immutableArray.Length; i++)
		{
			if (!comparer.Equals(immutableArray.array[i], (TBase)(object)items.array[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, IEnumerable<TDerived> items, IEqualityComparer<TBase>? comparer = null) where TDerived : TBase
	{
		Requires.NotNull(items, "items");
		if (comparer == null)
		{
			comparer = EqualityComparer<TBase>.Default;
		}
		int num = 0;
		int length = immutableArray.Length;
		foreach (TDerived item in items)
		{
			if (num == length)
			{
				return false;
			}
			if (!comparer.Equals(immutableArray[num], (TBase)(object)item))
			{
				return false;
			}
			num++;
		}
		return num == length;
	}

	public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, System.Collections.Immutable.ImmutableArray<TDerived> items, Func<TBase, TBase, bool> predicate) where TDerived : TBase
	{
		Requires.NotNull(predicate, "predicate");
		immutableArray.ThrowNullRefIfNotInitialized();
		items.ThrowNullRefIfNotInitialized();
		if ((object)immutableArray.array == items.array)
		{
			return true;
		}
		if (immutableArray.Length != items.Length)
		{
			return false;
		}
		int i = 0;
		for (int length = immutableArray.Length; i < length; i++)
		{
			if (!predicate(immutableArray[i], (TBase)(object)items[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static T? Aggregate<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, T, T> func)
	{
		Requires.NotNull(func, "func");
		if (immutableArray.Length == 0)
		{
			return default(T);
		}
		T val = immutableArray[0];
		int i = 1;
		for (int length = immutableArray.Length; i < length; i++)
		{
			val = func(val, immutableArray[i]);
		}
		return val;
	}

	public static TAccumulate Aggregate<TAccumulate, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
	{
		Requires.NotNull(func, "func");
		TAccumulate val = seed;
		T[] array = immutableArray.array;
		foreach (T arg in array)
		{
			val = func(val, arg);
		}
		return val;
	}

	public static TResult Aggregate<TAccumulate, TResult, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
	{
		Requires.NotNull(resultSelector, "resultSelector");
		return resultSelector(immutableArray.Aggregate(seed, func));
	}

	public static T ElementAt<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, int index)
	{
		return immutableArray[index];
	}

	public static T? ElementAtOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, int index)
	{
		if (index < 0 || index >= immutableArray.Length)
		{
			return default(T);
		}
		return immutableArray[index];
	}

	public static T First<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		T[] array = immutableArray.array;
		foreach (T val in array)
		{
			if (predicate(val))
			{
				return val;
			}
		}
		return Enumerable.Empty<T>().First();
	}

	public static T First<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		if (immutableArray.Length <= 0)
		{
			return immutableArray.array.First();
		}
		return immutableArray[0];
	}

	public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		if (immutableArray.array.Length == 0)
		{
			return default(T);
		}
		return immutableArray.array[0];
	}

	public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		T[] array = immutableArray.array;
		foreach (T val in array)
		{
			if (predicate(val))
			{
				return val;
			}
		}
		return default(T);
	}

	public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		if (immutableArray.Length <= 0)
		{
			return immutableArray.array.Last();
		}
		return immutableArray[immutableArray.Length - 1];
	}

	public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		for (int num = immutableArray.Length - 1; num >= 0; num--)
		{
			if (predicate(immutableArray[num]))
			{
				return immutableArray[num];
			}
		}
		return Enumerable.Empty<T>().Last();
	}

	public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		return immutableArray.array.LastOrDefault();
	}

	public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		for (int num = immutableArray.Length - 1; num >= 0; num--)
		{
			if (predicate(immutableArray[num]))
			{
				return immutableArray[num];
			}
		}
		return default(T);
	}

	public static T Single<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		return immutableArray.array.Single();
	}

	public static T Single<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		bool flag = true;
		T result = default(T);
		T[] array = immutableArray.array;
		foreach (T val in array)
		{
			if (predicate(val))
			{
				if (!flag)
				{
					System.Collections.Immutable.ImmutableArray.TwoElementArray.Single();
				}
				flag = false;
				result = val;
			}
		}
		if (flag)
		{
			Enumerable.Empty<T>().Single();
		}
		return result;
	}

	public static T? SingleOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		return immutableArray.array.SingleOrDefault();
	}

	public static T? SingleOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, bool> predicate)
	{
		Requires.NotNull(predicate, "predicate");
		bool flag = true;
		T result = default(T);
		T[] array = immutableArray.array;
		foreach (T val in array)
		{
			if (predicate(val))
			{
				if (!flag)
				{
					System.Collections.Immutable.ImmutableArray.TwoElementArray.Single();
				}
				flag = false;
				result = val;
			}
		}
		return result;
	}

	public static Dictionary<TKey, T> ToDictionary<TKey, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, TKey> keySelector) where TKey : notnull
	{
		return immutableArray.ToDictionary(keySelector, EqualityComparer<TKey>.Default);
	}

	public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, Func<T, TElement> elementSelector) where TKey : notnull
	{
		return immutableArray.ToDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);
	}

	public static Dictionary<TKey, T> ToDictionary<TKey, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		Requires.NotNull(keySelector, "keySelector");
		Dictionary<TKey, T> dictionary = new Dictionary<TKey, T>(immutableArray.Length, comparer);
		foreach (T item in immutableArray)
		{
			dictionary.Add(keySelector(item), item);
		}
		return dictionary;
	}

	public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		Requires.NotNull(keySelector, "keySelector");
		Requires.NotNull(elementSelector, "elementSelector");
		Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(immutableArray.Length, comparer);
		T[] array = immutableArray.array;
		foreach (T arg in array)
		{
			dictionary.Add(keySelector(arg), elementSelector(arg));
		}
		return dictionary;
	}

	public static T[] ToArray<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray)
	{
		immutableArray.ThrowNullRefIfNotInitialized();
		if (immutableArray.array.Length == 0)
		{
			return System.Collections.Immutable.ImmutableArray<T>.Empty.array;
		}
		return (T[])immutableArray.array.Clone();
	}

	public static T First<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		if (!builder.Any())
		{
			throw new InvalidOperationException();
		}
		return builder[0];
	}

	public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		if (!builder.Any())
		{
			return default(T);
		}
		return builder[0];
	}

	public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		if (!builder.Any())
		{
			throw new InvalidOperationException();
		}
		return builder[builder.Count - 1];
	}

	public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		if (!builder.Any())
		{
			return default(T);
		}
		return builder[builder.Count - 1];
	}

	public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		return builder.Count > 0;
	}

	private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(this System.Collections.Immutable.ImmutableArray<TSource> immutableArray, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector) where TSource : notnull where TCollection : notnull where TResult : notnull
	{
		TSource[] array = immutableArray.array;
		foreach (TSource item in array)
		{
			foreach (TCollection item2 in collectionSelector(item))
			{
				yield return resultSelector(item, item2);
			}
		}
	}
}
