using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ImmutableHashTreeExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TValue Search<TKey, TValue>(this ImmutableHashTree<TKey, TValue> tree, TKey key)
	{
		int hashCode = key.GetHashCode();
		while (tree.Height != 0 && tree.HashCode != hashCode)
		{
			tree = ((hashCode < tree.HashCode) ? tree.Left : tree.Right);
		}
		if (!tree.IsEmpty && ((object)tree.Key == (object)key || object.Equals(tree.Key, key)))
		{
			return tree.Value;
		}
		if (tree.Duplicates.Items.Length != 0)
		{
			KeyValue<TKey, TValue>[] items = tree.Duplicates.Items;
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

	public static ImmutableHashTree<TKey, TValue> Add<TKey, TValue>(this ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
	{
		if (tree.IsEmpty)
		{
			return new ImmutableHashTree<TKey, TValue>(key, value, tree, tree);
		}
		int hashCode = key.GetHashCode();
		if (hashCode > tree.HashCode)
		{
			return AddToRightBranch(tree, key, value);
		}
		if (hashCode < tree.HashCode)
		{
			return AddToLeftBranch(tree, key, value);
		}
		return new ImmutableHashTree<TKey, TValue>(key, value, tree);
	}

	public static IEnumerable<KeyValue<TKey, TValue>> InOrder<TKey, TValue>(this ImmutableHashTree<TKey, TValue> hashTree)
	{
		if (hashTree.IsEmpty)
		{
			yield break;
		}
		foreach (KeyValue<TKey, TValue> left in hashTree.Left.InOrder())
		{
			yield return new KeyValue<TKey, TValue>(left.Key, left.Value);
		}
		yield return new KeyValue<TKey, TValue>(hashTree.Key, hashTree.Value);
		for (int i = 0; i < hashTree.Duplicates.Items.Length; i++)
		{
			yield return hashTree.Duplicates.Items[i];
		}
		foreach (KeyValue<TKey, TValue> right in hashTree.Right.InOrder())
		{
			yield return new KeyValue<TKey, TValue>(right.Key, right.Value);
		}
	}

	private static ImmutableHashTree<TKey, TValue> AddToLeftBranch<TKey, TValue>(ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
	{
		return new ImmutableHashTree<TKey, TValue>(tree.Key, tree.Value, tree.Left.Add(key, value), tree.Right);
	}

	private static ImmutableHashTree<TKey, TValue> AddToRightBranch<TKey, TValue>(ImmutableHashTree<TKey, TValue> tree, TKey key, TValue value)
	{
		return new ImmutableHashTree<TKey, TValue>(tree.Key, tree.Value, tree.Left, tree.Right.Add(key, value));
	}
}
