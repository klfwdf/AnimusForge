using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ImmutableMapTreeExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TValue Search<TValue>(this ImmutableMapTree<TValue> tree, int key)
	{
		while (tree.Height != 0 && tree.Key != key)
		{
			tree = ((key < tree.Key) ? tree.Left : tree.Right);
		}
		if (!tree.IsEmpty)
		{
			return tree.Value;
		}
		return default(TValue);
	}

	public static ImmutableMapTree<TValue> Add<TValue>(this ImmutableMapTree<TValue> tree, int key, TValue value)
	{
		if (tree.IsEmpty)
		{
			return new ImmutableMapTree<TValue>(key, value, tree, tree);
		}
		if (key > tree.Key)
		{
			return AddToRightBranch(tree, key, value);
		}
		if (key < tree.Key)
		{
			return AddToLeftBranch(tree, key, value);
		}
		return new ImmutableMapTree<TValue>(key, value, tree);
	}

	private static ImmutableMapTree<TValue> AddToLeftBranch<TValue>(ImmutableMapTree<TValue> tree, int key, TValue value)
	{
		return new ImmutableMapTree<TValue>(tree.Key, tree.Value, tree.Left.Add(key, value), tree.Right);
	}

	private static ImmutableMapTree<TValue> AddToRightBranch<TValue>(ImmutableMapTree<TValue> tree, int key, TValue value)
	{
		return new ImmutableMapTree<TValue>(tree.Key, tree.Value, tree.Left, tree.Right.Add(key, value));
	}
}
