using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ImmutableHashTableExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[ExcludeFromCodeCoverage]
	public static TValue Search<TKey, TValue>(this ImmutableHashTable<TKey, TValue> hashTable, TKey key)
	{
		int hashCode = key.GetHashCode();
		int num = hashCode & (hashTable.Divisor - 1);
		ImmutableHashTree<TKey, TValue> immutableHashTree = hashTable.Buckets[num];
		while (immutableHashTree.Height != 0 && immutableHashTree.HashCode != hashCode)
		{
			immutableHashTree = ((hashCode < immutableHashTree.HashCode) ? immutableHashTree.Left : immutableHashTree.Right);
		}
		if (immutableHashTree.Height != 0 && ((object)immutableHashTree.Key == (object)key || object.Equals(immutableHashTree.Key, key)))
		{
			return immutableHashTree.Value;
		}
		if (immutableHashTree.Duplicates.Items.Length != 0)
		{
			KeyValue<TKey, TValue>[] items = immutableHashTree.Duplicates.Items;
			foreach (KeyValue<TKey, TValue> keyValue in items)
			{
				if ((object)keyValue.Key == (object)key || object.Equals(keyValue.Key, key))
				{
					return keyValue.Value;
				}
			}
		}
		return default(TValue);
	}

	public static ImmutableHashTable<TKey, TValue> Add<TKey, TValue>(this ImmutableHashTable<TKey, TValue> hashTable, TKey key, TValue value)
	{
		return new ImmutableHashTable<TKey, TValue>(hashTable, key, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[ExcludeFromCodeCoverage]
	internal static GetInstanceDelegate Search(this ImmutableHashTable<Type, GetInstanceDelegate> hashTable, Type key)
	{
		int hashCode = key.GetHashCode();
		int num = hashCode & (hashTable.Divisor - 1);
		ImmutableHashTree<Type, GetInstanceDelegate> immutableHashTree = hashTable.Buckets[num];
		while (immutableHashTree.Height != 0 && immutableHashTree.HashCode != hashCode)
		{
			immutableHashTree = ((hashCode < immutableHashTree.HashCode) ? immutableHashTree.Left : immutableHashTree.Right);
		}
		if (immutableHashTree.Height != 0 && (object)immutableHashTree.Key == key)
		{
			return immutableHashTree.Value;
		}
		if (immutableHashTree.Duplicates.Items.Length != 0)
		{
			KeyValue<Type, GetInstanceDelegate>[] items = immutableHashTree.Duplicates.Items;
			foreach (KeyValue<Type, GetInstanceDelegate> keyValue in items)
			{
				if ((object)keyValue.Key == key)
				{
					return keyValue.Value;
				}
			}
		}
		return null;
	}
}
