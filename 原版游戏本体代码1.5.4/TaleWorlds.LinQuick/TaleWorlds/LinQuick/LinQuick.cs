using System;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.LinQuick;

public static class LinQuick
{
	public static bool AllQ<T>(this T[] source, Func<T, bool> predicate)
	{
		int num = source.Length;
		for (int i = 0; i < num; i++)
		{
			if (!predicate(source[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AllQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (!predicate(source[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AllQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> source2)
		{
			return source2.AllQ(predicate);
		}
		if (source is T[] source3)
		{
			return source3.AllQ(predicate);
		}
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (!predicate(source[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AllQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.AllQ(predicate);
		}
		foreach (T item in source)
		{
			if (!predicate(item))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AnyQ<T>(this T[] source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool AnyQ<T>(this List<T> source)
	{
		return source.Count > 0;
	}

	public static bool AnyQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool AnyQ<T>(this IReadOnlyList<T> source)
	{
		return source.Count > 0;
	}

	public static bool AnyQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool AnyQ<T>(this IEnumerable<T> source)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.AnyQ();
		}
		return source.GetEnumerator().MoveNext();
	}

	public static bool AnyQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.AnyQ(predicate);
		}
		foreach (T item in source)
		{
			if (predicate(item))
			{
				return true;
			}
		}
		return false;
	}

	public static float AverageQ(this float[] source)
	{
		if (source.Length == 0)
		{
			throw Error.NoElements();
		}
		float num = 0f;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			num += source[i];
		}
		return num / (float)source.Length;
	}

	public static float AverageQ(this IEnumerable<float> source)
	{
		float num = 0f;
		int num2 = 0;
		foreach (float item in source)
		{
			num += item;
			num2++;
		}
		if (num2 == 0)
		{
			throw Error.NoElements();
		}
		return num / (float)num2;
	}

	public static float AverageQ<T>(this T[] source, Func<T, float> selector)
	{
		if (source.Length == 0)
		{
			throw Error.NoElements();
		}
		float num = 0f;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			num += selector(source[i]);
		}
		return num / (float)source.Length;
	}

	public static float AverageQ<T>(this List<T> source, Func<T, float> selector)
	{
		int count = source.Count;
		if (count == 0)
		{
			throw Error.NoElements();
		}
		float num = 0f;
		for (int i = 0; i < count; i++)
		{
			num += selector(source[i]);
		}
		return num / (float)count;
	}

	public static float AverageQ<T>(this IReadOnlyList<T> source, Func<T, float> selector)
	{
		if (source is List<T> source2)
		{
			return source2.AverageQ(selector);
		}
		if (source is T[] source3)
		{
			return source3.AverageQ(selector);
		}
		int count = source.Count;
		if (count == 0)
		{
			throw Error.NoElements();
		}
		float num = 0f;
		for (int i = 0; i < count; i++)
		{
			num += selector(source[i]);
		}
		return num / (float)count;
	}

	public static float AverageQ<T>(this IEnumerable<T> source, Func<T, float> selector)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.AverageQ(selector);
		}
		float num = 0f;
		int num2 = 0;
		foreach (T item in source)
		{
			float num3 = selector(item);
			num += num3;
			num2++;
		}
		if (num2 == 0)
		{
			throw Error.NoElements();
		}
		return num / (float)num2;
	}

	public static bool ContainsQ<T>(this T[] source, T value)
	{
		return source.FindIndexQ(value) != -1;
	}

	public static bool ContainsQ<T>(this List<T> source, T value)
	{
		return source.FindIndexQ(value) != -1;
	}

	public static bool ContainsQ<T>(this IReadOnlyList<T> source, T value)
	{
		return source.FindIndexQ(value) != -1;
	}

	public static bool ContainsQ<T>(this IEnumerable<T> source, T value)
	{
		return source.FindIndexQ(value) != -1;
	}

	public static bool ContainsQ<T>(this Queue<T> source, T value)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int count = source.Count;
		bool result = false;
		for (int i = 0; i < count; i++)
		{
			T val = source.Dequeue();
			if (equalityComparer.Equals(val, value))
			{
				result = true;
			}
			source.Enqueue(val);
		}
		return result;
	}

	public static bool ContainsQ<T>(this T[] source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool ContainsQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool ContainsQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool ContainsQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		return source.FindIndexQ(predicate) != -1;
	}

	public static bool ContainsQ<T>(this Queue<T> source, Func<T, bool> predicate)
	{
		int count = source.Count;
		bool result = false;
		for (int i = 0; i < count; i++)
		{
			T val = source.Dequeue();
			if (predicate(val))
			{
				result = true;
			}
			source.Enqueue(val);
		}
		return result;
	}

	public static int CountQ<T>(this T[] source, T value)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int num = 0;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			T x = source[i];
			if (equalityComparer.Equals(x, value))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this List<T> source, T value)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (equalityComparer.Equals(source[i], value))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this IReadOnlyList<T> source, T value)
	{
		if (source is List<T> source2)
		{
			return source2.CountQ(value);
		}
		if (source is T[] source3)
		{
			return source3.CountQ(value);
		}
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (equalityComparer.Equals(source[i], value))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this T[] source, Func<T, bool> predicate)
	{
		int num = 0;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			if (predicate(source[i]))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (predicate(source[i]))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> source2)
		{
			return source2.CountQ(predicate);
		}
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (predicate(source[i]))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.CountQ(predicate);
		}
		int num = 0;
		foreach (T item in source)
		{
			if (predicate(item))
			{
				num++;
			}
		}
		return num;
	}

	public static int CountQ<T>(this IEnumerable<T> source)
	{
		if (source is IReadOnlyList<T> readOnlyList)
		{
			return readOnlyList.Count;
		}
		int num = 0;
		using IEnumerator<T> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			num++;
		}
		return num;
	}

	public static int FindIndexQ<T>(this T[] source, T value)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int result = -1;
		int num = source.Length;
		for (int i = 0; i < num; i++)
		{
			if (equalityComparer.Equals(source[i], value))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this List<T> source, T value)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int result = -1;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (equalityComparer.Equals(source[i], value))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this IReadOnlyList<T> source, T value)
	{
		if (source is List<T> source2)
		{
			return source2.FindIndexQ(value);
		}
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int result = -1;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (equalityComparer.Equals(source[i], value))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this IEnumerable<T> source, T value)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.FindIndexQ(value);
		}
		if (value != null && value is IComparable)
		{
			return source.FindIndexComparableQ(value);
		}
		return source.FindIndexNonComparableQ(value);
	}

	public static int FindIndexQ<T>(this T[] source, Func<T, bool> predicate)
	{
		int result = -1;
		int num = source.Length;
		for (int i = 0; i < num; i++)
		{
			if (predicate(source[i]))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		int result = -1;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (predicate(source[i]))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> source2)
		{
			return source2.FindIndexQ(predicate);
		}
		if (source is T[] source3)
		{
			return source3.FindIndexQ(predicate);
		}
		int result = -1;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (predicate(source[i]))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int FindIndexQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.FindIndexQ(predicate);
		}
		using (IEnumerator<T> enumerator = source.GetEnumerator())
		{
			int num = 0;
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (predicate(current))
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	private static int FindIndexComparableQ<T>(this IEnumerable<T> source, T value)
	{
		Comparer<T> comparer = Comparer<T>.Default;
		using (IEnumerator<T> enumerator = source.GetEnumerator())
		{
			int num = 0;
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (comparer.Compare(current, value) == 0)
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	private static int FindIndexNonComparableQ<T>(this IEnumerable<T> source, T value)
	{
		using (IEnumerator<T> enumerator = source.GetEnumerator())
		{
			int num = 0;
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Equals(value))
				{
					return num;
				}
				num++;
			}
		}
		return -1;
	}

	public static T FirstOrDefaultQ<T>(this T[] source, Func<T, bool> predicate)
	{
		int num = source.FindIndexQ(predicate);
		if (num == -1)
		{
			return default(T);
		}
		return source[num];
	}

	public static T FirstOrDefaultQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		int num = source.FindIndexQ(predicate);
		if (num == -1)
		{
			return default(T);
		}
		return source[num];
	}

	public static T FirstOrDefaultQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		int num = source.FindIndexQ(predicate);
		if (num == -1)
		{
			return default(T);
		}
		return source[num];
	}

	public static T FirstOrDefaultQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.FirstOrDefaultQ(predicate);
		}
		foreach (T item in source)
		{
			if (predicate(item))
			{
				return item;
			}
		}
		return default(T);
	}

	public static int MaxQ(this int[] source)
	{
		int num = source[0];
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i] > num)
			{
				num = source[i];
			}
		}
		return num;
	}

	public static int MaxQ(this List<int> source)
	{
		int num = source[0];
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (source[i] > num)
			{
				num = source[i];
			}
		}
		return num;
	}

	public static T MaxQ<T>(this T[] source) where T : IComparable<T>
	{
		if (source.Length == 0)
		{
			throw Error.NoElements();
		}
		T val = source[0];
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i].CompareTo(val) > 0)
			{
				val = source[i];
			}
		}
		return val;
	}

	public static T MaxQ<T>(this List<T> source) where T : IComparable<T>
	{
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		T val = source[0];
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (source[i].CompareTo(val) > 0)
			{
				val = source[i];
			}
		}
		return val;
	}

	public static int MaxQ(this IReadOnlyList<int> source)
	{
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		int num = source[0];
		if (source is List<int> source2)
		{
			return source2.MaxQ();
		}
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			if (source[i] > num)
			{
				num = source[i];
			}
		}
		return num;
	}

	public static T MaxQ<T>(this IReadOnlyList<T> source) where T : IComparable<T>
	{
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		if (source is List<T> source2)
		{
			return source2.MaxQ();
		}
		if (source is T[] source3)
		{
			return source3.MaxQ();
		}
		T val = source[0];
		for (int i = 0; i < source.Count; i++)
		{
			if (source[i].CompareTo(val) > 0)
			{
				val = source[i];
			}
		}
		return val;
	}

	public static float MaxQ<T>(this T[] source, Func<T, float> selector)
	{
		if (source.Length == 0)
		{
			throw Error.NoElements();
		}
		float num = selector(source[0]);
		for (int i = 0; i < source.Length; i++)
		{
			float num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static int MaxQ<T>(this T[] source, Func<T, int> selector)
	{
		if (source.Length == 0)
		{
			throw Error.NoElements();
		}
		int num = selector(source[0]);
		for (int i = 0; i < source.Length; i++)
		{
			int num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static float MaxQ<T>(this List<T> source, Func<T, float> selector)
	{
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		float num = selector(source[0]);
		for (int i = 0; i < source.Count; i++)
		{
			float num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static int MaxQ<T>(this List<T> source, Func<T, int> selector)
	{
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		int num = selector(source[0]);
		for (int i = 0; i < source.Count; i++)
		{
			int num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static float MaxQ<T>(this IReadOnlyList<T> source, Func<T, float> selector)
	{
		if (source is List<T> source2)
		{
			return source2.MaxQ(selector);
		}
		if (source is T[] source3)
		{
			return source3.MaxQ(selector);
		}
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		float num = selector(source[0]);
		for (int i = 0; i < source.Count; i++)
		{
			float num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static int MaxQ<T>(this IReadOnlyList<T> source, Func<T, int> selector)
	{
		if (source is List<T> source2)
		{
			return source2.MaxQ(selector);
		}
		if (source is T[] source3)
		{
			return source3.MaxQ(selector);
		}
		if (source.Count == 0)
		{
			throw Error.NoElements();
		}
		int num = selector(source[0]);
		for (int i = 0; i < source.Count; i++)
		{
			int num2 = selector(source[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static float MaxQ<T>(this IEnumerable<T> source, Func<T, float> selector)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.MaxQ(selector);
		}
		float num = 0f;
		bool flag = false;
		foreach (T item in source)
		{
			float num2 = selector(item);
			if (!flag)
			{
				num = num2;
				flag = true;
			}
			else if (num2 > num)
			{
				num = num2;
			}
		}
		if (!flag)
		{
			Error.NoElements();
		}
		return num;
	}

	public static int MaxQ<T>(this IEnumerable<T> source, Func<T, int> selector)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.MaxQ(selector);
		}
		int num = 0;
		bool flag = false;
		foreach (T item in source)
		{
			int num2 = selector(item);
			if (!flag)
			{
				num = num2;
				flag = true;
			}
			else if (num2 > num)
			{
				num = num2;
			}
		}
		if (!flag)
		{
			Error.NoElements();
		}
		return num;
	}

	public static (T, T, T) MaxElements3<T>(this IEnumerable<T> collection, Func<T, float> func)
	{
		float num = float.MinValue;
		float num2 = float.MinValue;
		float num3 = float.MinValue;
		T val = default(T);
		T val2 = default(T);
		T item = default(T);
		foreach (T item2 in collection)
		{
			float num4 = func(item2);
			if (!(num4 > num3))
			{
				continue;
			}
			if (num4 > num2)
			{
				num3 = num2;
				item = val2;
				if (num4 > num)
				{
					num2 = num;
					val2 = val;
					num = num4;
					val = item2;
				}
				else
				{
					num2 = num4;
					val2 = item2;
				}
			}
			else
			{
				num3 = num4;
				item = item2;
			}
		}
		return (val, val2, item);
	}

	public static IOrderedEnumerable<T> OrderByQ<T, S>(this IEnumerable<T> source, Func<T, S> selector)
	{
		return source.OrderBy(selector);
	}

	public static T[] OrderByQ<T, TKey>(this T[] source, Func<T, TKey> selector)
	{
		Comparer<TKey> comparer = Comparer<TKey>.Default;
		TKey[] array = new TKey[source.Length];
		for (int i = 0; i < source.Length; i++)
		{
			array[i] = selector(source[i]);
		}
		T[] array2 = new T[source.Length];
		for (int j = 0; j < source.Length; j++)
		{
			array2[j] = source[j];
		}
		Array.Sort(array, array2, comparer);
		return array2;
	}

	public static T[] OrderByQ<T, TKey>(this List<T> source, Func<T, TKey> selector)
	{
		Comparer<TKey> comparer = Comparer<TKey>.Default;
		int count = source.Count;
		TKey[] array = new TKey[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = selector(source[i]);
		}
		T[] array2 = new T[count];
		for (int j = 0; j < count; j++)
		{
			array2[j] = source[j];
		}
		Array.Sort(array, array2, comparer);
		return array2;
	}

	public static T[] OrderByQ<T, TKey>(this IReadOnlyList<T> source, Func<T, TKey> selector)
	{
		Comparer<TKey> comparer = Comparer<TKey>.Default;
		int count = source.Count;
		TKey[] array = new TKey[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = selector(source[i]);
		}
		T[] array2 = new T[count];
		for (int j = 0; j < count; j++)
		{
			array2[j] = source[j];
		}
		Array.Sort(array, array2, comparer);
		return array2;
	}

	public static IEnumerable<R> SelectQ<T, R>(this T[] source, Func<T, R> selector)
	{
		int len = source.Length;
		int i = 0;
		while (i < len)
		{
			yield return selector(source[i]);
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<R> SelectQ<T, R>(this List<T> source, Func<T, R> selector)
	{
		int len = source.Count;
		int i = 0;
		while (i < len)
		{
			yield return selector(source[i]);
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<R> SelectQ<T, R>(this IReadOnlyList<T> source, Func<T, R> selector)
	{
		int len = source.Count;
		int i = 0;
		while (i < len)
		{
			yield return selector(source[i]);
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<R> SelectQ<T, R>(this IEnumerable<T> source, Func<T, R> selector)
	{
		foreach (T item in source)
		{
			yield return selector(item);
		}
	}

	public static int SumQ<T>(this T[] source, Func<T, int> func)
	{
		int num = 0;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static float SumQ<T>(this T[] source, Func<T, float> func)
	{
		float num = 0f;
		int num2 = source.Length;
		for (int i = 0; i < num2; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static int SumQ<T>(this List<T> source, Func<T, int> func)
	{
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static float SumQ<T>(this List<T> source, Func<T, float> func)
	{
		float num = 0f;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static int SumQ<T>(this IReadOnlyList<T> source, Func<T, int> func)
	{
		if (source is List<T> source2)
		{
			return source2.SumQ(func);
		}
		int num = 0;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static float SumQ<T>(this IReadOnlyList<T> source, Func<T, float> func)
	{
		if (source is List<T> source2)
		{
			return source2.SumQ(func);
		}
		float num = 0f;
		int count = source.Count;
		for (int i = 0; i < count; i++)
		{
			num += func(source[i]);
		}
		return num;
	}

	public static float SumQ<T>(this IEnumerable<T> source, Func<T, float> func)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.SumQ(func);
		}
		float num = 0f;
		foreach (T item in source)
		{
			float num2 = func(item);
			num += num2;
		}
		return num;
	}

	public static int SumQ<T>(this IEnumerable<T> source, Func<T, int> func)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.SumQ(func);
		}
		int num = 0;
		foreach (T item in source)
		{
			int num2 = func(item);
			num += num2;
		}
		return num;
	}

	public static T[] ToArrayQ<T>(this T[] source)
	{
		int num = source.Length;
		T[] array = new T[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = source[i];
		}
		return array;
	}

	public static T[] ToArrayQ<T>(this List<T> source)
	{
		return source.ToArray();
	}

	public static T[] ToArrayQ<T>(this IReadOnlyList<T> source)
	{
		if (source is List<T> list)
		{
			return list.ToArray();
		}
		if (source is T[] source2)
		{
			return source2.ToArrayQ();
		}
		int count = source.Count;
		T[] array = new T[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = source[i];
		}
		return array;
	}

	public static T[] ToArrayQ<T>(this IEnumerable<T> source)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.ToArrayQ();
		}
		List<T> list = new List<T>();
		foreach (T item in source)
		{
			list.Add(item);
		}
		return list.ToArray();
	}

	public static List<T> ToListQ<T>(this T[] source)
	{
		List<T> list = new List<T>(source.Length);
		list.AddRange(source);
		return list;
	}

	public static List<T> ToListQ<T>(this List<T> source)
	{
		List<T> list = new List<T>(source.Count);
		list.AddRange(source);
		return list;
	}

	public static List<T> ToListQ<T>(this IReadOnlyList<T> source)
	{
		if (source is List<T> source2)
		{
			return source2.ToListQ();
		}
		if (source is T[] source3)
		{
			return source3.ToListQ();
		}
		List<T> list = new List<T>(source.Count);
		list.AddRange(source);
		return list;
	}

	public static List<T> ToListQ<T>(this IEnumerable<T> source)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.ToListQ();
		}
		List<T> list = new List<T>();
		list.AddRange(source);
		return list;
	}

	public static IEnumerable<T> WhereQ<T>(this T[] source, Func<T, bool> predicate)
	{
		int length = source.Length;
		int i = 0;
		while (i < length)
		{
			T val = source[i];
			if (predicate(val))
			{
				yield return val;
			}
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<T> WhereQ<T>(this List<T> source, Func<T, bool> predicate)
	{
		int length = source.Count;
		int i = 0;
		while (i < length)
		{
			T val = source[i];
			if (predicate(val))
			{
				yield return val;
			}
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<T> WhereQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> source2)
		{
			return source2.WhereQ(predicate);
		}
		return WhereQImp(source, predicate);
	}

	private static IEnumerable<T> WhereQImp<T>(IReadOnlyList<T> source, Func<T, bool> predicate)
	{
		int length = source.Count;
		int i = 0;
		while (i < length)
		{
			T val = source[i];
			if (predicate(val))
			{
				yield return val;
			}
			int num = i + 1;
			i = num;
		}
	}

	public static IEnumerable<T> WhereQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is IReadOnlyList<T> source2)
		{
			return source2.WhereQ(predicate);
		}
		return WhereQImp(source, predicate);
	}

	private static IEnumerable<T> WhereQImp<T>(IEnumerable<T> source, Func<T, bool> predicate)
	{
		foreach (T item in source)
		{
			if (predicate(item))
			{
				yield return item;
			}
		}
	}
}
