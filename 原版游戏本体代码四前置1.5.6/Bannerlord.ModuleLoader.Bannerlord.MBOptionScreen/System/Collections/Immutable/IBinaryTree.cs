namespace System.Collections.Immutable;

internal interface IBinaryTree
{
	int Height { get; }

	bool IsEmpty { get; }

	int Count { get; }

	IBinaryTree? Left { get; }

	IBinaryTree? Right { get; }
}
internal interface IBinaryTree<out T> : IBinaryTree
{
	T Value { get; }

	new IBinaryTree<T>? Left { get; }

	new IBinaryTree<T>? Right { get; }
}
