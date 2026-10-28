using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal sealed class ImmutableMapTree<TValue>
{
	public static readonly ImmutableMapTree<TValue> Empty = new ImmutableMapTree<TValue>();

	public readonly int Key;

	public readonly TValue Value;

	public readonly ImmutableMapTree<TValue> Left;

	public readonly ImmutableMapTree<TValue> Right;

	public readonly int Height;

	public readonly bool IsEmpty;

	public ImmutableMapTree(int key, TValue value, ImmutableMapTree<TValue> hashTree)
	{
		Key = key;
		Value = value;
		Height = hashTree.Height;
		Left = hashTree.Left;
		Right = hashTree.Right;
	}

	public ImmutableMapTree(int key, TValue value, ImmutableMapTree<TValue> left, ImmutableMapTree<TValue> right)
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
			Left = new ImmutableMapTree<TValue>(key, value, left, right.Left);
			Right = right.Right;
			break;
		case 2:
			if (left.IsRightHeavy())
			{
				left = RotateLeft(left);
			}
			Key = left.Key;
			Value = left.Value;
			Right = new ImmutableMapTree<TValue>(key, value, left.Right, right);
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
	}

	private ImmutableMapTree()
	{
		IsEmpty = true;
	}

	private static ImmutableMapTree<TValue> RotateLeft(ImmutableMapTree<TValue> node)
	{
		return new ImmutableMapTree<TValue>(node.Right.Key, node.Right.Value, new ImmutableMapTree<TValue>(node.Key, node.Value, node.Left, node.Right.Left), node.Right.Right);
	}

	private static ImmutableMapTree<TValue> RotateRight(ImmutableMapTree<TValue> node)
	{
		return new ImmutableMapTree<TValue>(node.Left.Key, node.Left.Value, node.Left.Left, new ImmutableMapTree<TValue>(node.Key, node.Value, node.Left.Right, node.Right));
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
