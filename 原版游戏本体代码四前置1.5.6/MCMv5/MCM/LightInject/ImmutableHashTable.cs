using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal sealed class ImmutableHashTable<TKey, TValue>
{
	public static readonly ImmutableHashTable<TKey, TValue> Empty = new ImmutableHashTable<TKey, TValue>();

	public readonly int Count;

	internal readonly ImmutableHashTree<TKey, TValue>[] Buckets;

	internal readonly int Divisor;

	internal ImmutableHashTable(ImmutableHashTable<TKey, TValue> previous, TKey key, TValue value)
	{
		Count = previous.Count + 1;
		if (previous.Count >= previous.Divisor)
		{
			Divisor = previous.Divisor * 2;
			Buckets = new ImmutableHashTree<TKey, TValue>[Divisor];
			InitializeBuckets(0, Divisor);
			AddExistingValues(previous);
		}
		else
		{
			Divisor = previous.Divisor;
			Buckets = new ImmutableHashTree<TKey, TValue>[Divisor];
			Array.Copy(previous.Buckets, Buckets, previous.Divisor);
		}
		int hashCode = key.GetHashCode();
		int num = hashCode & (Divisor - 1);
		Buckets[num] = Buckets[num].Add(key, value);
	}

	private ImmutableHashTable()
	{
		Buckets = new ImmutableHashTree<TKey, TValue>[2];
		Divisor = 2;
		InitializeBuckets(0, 2);
	}

	private void AddExistingValues(ImmutableHashTable<TKey, TValue> previous)
	{
		ImmutableHashTree<TKey, TValue>[] buckets = previous.Buckets;
		foreach (ImmutableHashTree<TKey, TValue> hashTree in buckets)
		{
			foreach (KeyValue<TKey, TValue> item in hashTree.InOrder())
			{
				int hashCode = item.Key.GetHashCode();
				int num = hashCode & (Divisor - 1);
				Buckets[num] = Buckets[num].Add(item.Key, item.Value);
			}
		}
	}

	private void InitializeBuckets(int startIndex, int count)
	{
		for (int i = startIndex; i < count; i++)
		{
			Buckets[i] = ImmutableHashTree<TKey, TValue>.Empty;
		}
	}
}
