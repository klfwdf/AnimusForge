using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal sealed class ImmutableHashTree<TKey, TValue>
{
	public static readonly ImmutableHashTree<TKey, TValue> Empty = new ImmutableHashTree<TKey, TValue>();

	public readonly TKey Key;

	public readonly TValue Value;

	public readonly ImmutableList<KeyValue<TKey, TValue>> Duplicates;

	public readonly int HashCode;

	public readonly ImmutableHashTree<TKey, TValue> Left;

	public readonly ImmutableHashTree<TKey, TValue> Right;

	public readonly int Height;

	public readonly bool IsEmpty;

	public ImmutableHashTree(TKey key, TValue value, ImmutableHashTree<TKey, TValue> hashTree)
	{
		Duplicates = hashTree.Duplicates.Add(new KeyValue<TKey, TValue>(key, value));
		Key = hashTree.Key;
		Value = hashTree.Value;
		Height = hashTree.Height;
		HashCode = hashTree.HashCode;
		Left = hashTree.Left;
		Right = hashTree.Right;
	}

	public ImmutableHashTree(TKey key, TValue value, ImmutableHashTree<TKey, TValue> left, ImmutableHashTree<TKey, TValue> right)
	{
		switch (left.Height - right.Height)
		{
		case -2:
			if (right.IsLeftHeavy())
			{
				right = RotateRight(right);
			}
			Key = right.Key;
			Value = right.Value;
			Left = new ImmutableHashTree<TKey, TValue>(key, value, left, right.Left);
			Right = right.Right;
			break;
		case 2:
			if (left.IsRightHeavy())
			{
				left = RotateLeft(left);
			}
			Key = left.Key;
			Value = left.Value;
			Right = new ImmutableHashTree<TKey, TValue>(key, value, left.Right, right);
			Left = left.Left;
			break;
		default:
			Key = key;
			Value = value;
			Left = left;
			Right = right;
			break;
		}
		Height = 1 + Math.Max(Left.Height, Right.Height);
		Duplicates = ImmutableList<KeyValue<TKey, TValue>>.Empty;
		HashCode = Key.GetHashCode();
	}

	private ImmutableHashTree()
	{
		IsEmpty = true;
		Duplicates = ImmutableList<KeyValue<TKey, TValue>>.Empty;
	}

	private static ImmutableHashTree<TKey, TValue> RotateLeft(ImmutableHashTree<TKey, TValue> node)
	{
		return new ImmutableHashTree<TKey, TValue>(node.Right.Key, node.Right.Value, new ImmutableHashTree<TKey, TValue>(node.Key, node.Value, node.Left, node.Right.Left), node.Right.Right);
	}

	private static ImmutableHashTree<TKey, TValue> RotateRight(ImmutableHashTree<TKey, TValue> node)
	{
		return new ImmutableHashTree<TKey, TValue>(node.Left.Key, node.Left.Value, node.Left.Left, new ImmutableHashTree<TKey, TValue>(node.Key, node.Value, node.Left.Right, node.Right));
	}

	private bool IsLeftHeavy()
	{
		return Left.Height > Right.Height;
	}

	private bool IsRightHeavy()
	{
		return Right.Height > Left.Height;
	}
}
