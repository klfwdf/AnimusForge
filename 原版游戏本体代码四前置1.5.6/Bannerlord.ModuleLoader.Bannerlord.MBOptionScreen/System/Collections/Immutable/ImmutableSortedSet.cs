using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Immutable;

internal static class ImmutableSortedSet
{
	public static ImmutableSortedSet<T> Create<T>()
	{
		return ImmutableSortedSet<T>.Empty;
	}

	public static ImmutableSortedSet<T> Create<T>(IComparer<T>? comparer)
	{
		return ImmutableSortedSet<T>.Empty.WithComparer(comparer);
	}

	public static ImmutableSortedSet<T> Create<T>(T item)
	{
		return ImmutableSortedSet<T>.Empty.Add(item);
	}

	public static ImmutableSortedSet<T> Create<T>(IComparer<T>? comparer, T item)
	{
		return ImmutableSortedSet<T>.Empty.WithComparer(comparer).Add(item);
	}

	public static ImmutableSortedSet<T> CreateRange<T>(IEnumerable<T> items)
	{
		return ImmutableSortedSet<T>.Empty.Union(items);
	}

	public static ImmutableSortedSet<T> CreateRange<T>(IComparer<T>? comparer, IEnumerable<T> items)
	{
		return ImmutableSortedSet<T>.Empty.WithComparer(comparer).Union(items);
	}

	public static ImmutableSortedSet<T> Create<T>(params T[] items)
	{
		Requires.NotNull(items, "items");
		return Create((ReadOnlySpan<T>)items);
	}

	public static ImmutableSortedSet<T> Create<T>(params ReadOnlySpan<T> items)
	{
		return ImmutableSortedSet<T>.Empty.Union(items);
	}

	public static ImmutableSortedSet<T> Create<T>(IComparer<T>? comparer, params T[] items)
	{
		Requires.NotNull(items, "items");
		return Create(comparer, (ReadOnlySpan<T>)items);
	}

	public static ImmutableSortedSet<T> Create<T>(IComparer<T>? comparer, params ReadOnlySpan<T> items)
	{
		return ImmutableSortedSet<T>.Empty.WithComparer(comparer).Union(items);
	}

	public static ImmutableSortedSet<T>.Builder CreateBuilder<T>()
	{
		return Create<T>().ToBuilder();
	}

	public static ImmutableSortedSet<T>.Builder CreateBuilder<T>(IComparer<T>? comparer)
	{
		return Create(comparer).ToBuilder();
	}

	public static ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
	{
		if (source is ImmutableSortedSet<TSource> immutableSortedSet)
		{
			return immutableSortedSet.WithComparer(comparer);
		}
		return ImmutableSortedSet<TSource>.Empty.WithComparer(comparer).Union(source);
	}

	public static ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this IEnumerable<TSource> source)
	{
		return source.ToImmutableSortedSet(null);
	}

	public static ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this ImmutableSortedSet<TSource>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		return builder.ToImmutable();
	}
}
[CollectionBuilder(typeof(ImmutableSortedSet), "Create")]
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
internal sealed class ImmutableSortedSet<T> : IImmutableSet<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, ISortKeyCollection<T>, IReadOnlyList<T>, IList<T>, ICollection<T>, ISet<T>, IList, ICollection, IStrongEnumerable<T, ImmutableSortedSet<T>.Enumerator>
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(ImmutableSortedSetBuilderDebuggerProxy<>))]
	public sealed class Builder : ISortKeyCollection<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, ISet<T>, ICollection<T>, ICollection
	{
		private ImmutableSortedSet<T>.Node _root = Node.EmptyNode;

		private IComparer<T> _comparer = Comparer<T>.Default;

		private ImmutableSortedSet<T> _immutable;

		private int _version;

		private object _syncRoot;

		public int Count => Root.Count;

		bool ICollection<T>.IsReadOnly => false;

		public T this[int index] => _root.ItemRef(index);

		public T? Max => _root.Max;

		public T? Min => _root.Min;

		public IComparer<T> KeyComparer
		{
			get
			{
				return _comparer;
			}
			set
			{
				Requires.NotNull(value, "value");
				if (value == _comparer)
				{
					return;
				}
				Node node = Node.EmptyNode;
				using (Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						T current = enumerator.Current;
						node = node.Add(current, value, out var _);
					}
				}
				_immutable = null;
				_comparer = value;
				Root = node;
			}
		}

		internal int Version => _version;

		private Node Root
		{
			get
			{
				return _root;
			}
			set
			{
				_version++;
				if (_root != value)
				{
					_root = value;
					_immutable = null;
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized => false;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object ICollection.SyncRoot
		{
			get
			{
				if (_syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref _syncRoot, new object(), (object)null);
				}
				return _syncRoot;
			}
		}

		internal Builder(ImmutableSortedSet<T> set)
		{
			Requires.NotNull(set, "set");
			_root = set._root;
			_comparer = set.KeyComparer;
			_immutable = set;
		}

		public ref readonly T ItemRef(int index)
		{
			return ref _root.ItemRef(index);
		}

		public bool Add(T item)
		{
			Root = Root.Add(item, _comparer, out var mutated);
			return mutated;
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			Requires.NotNull(other, "other");
			foreach (T item in other)
			{
				Root = Root.Remove(item, _comparer, out var _);
			}
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			Requires.NotNull(other, "other");
			Node node = Node.EmptyNode;
			foreach (T item in other)
			{
				if (Contains(item))
				{
					node = node.Add(item, _comparer, out var _);
				}
			}
			Root = node;
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			return ToImmutable().IsProperSubsetOf(other);
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			return ToImmutable().IsProperSupersetOf(other);
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			return ToImmutable().IsSubsetOf(other);
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			return ToImmutable().IsSupersetOf(other);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			return ToImmutable().Overlaps(other);
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			return ToImmutable().SetEquals(other);
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			Root = ToImmutable().SymmetricExcept(other)._root;
		}

		public void UnionWith(IEnumerable<T> other)
		{
			Requires.NotNull(other, "other");
			foreach (T item in other)
			{
				Root = Root.Add(item, _comparer, out var _);
			}
		}

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		public void Clear()
		{
			Root = Node.EmptyNode;
		}

		public bool Contains(T item)
		{
			return Root.Contains(item, _comparer);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			_root.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			Root = Root.Remove(item, _comparer, out var mutated);
			return mutated;
		}

		public ImmutableSortedSet<T>.Enumerator GetEnumerator()
		{
			return Root.GetEnumerator(this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return Root.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return Root.IndexOf(item, _comparer);
		}

		public IEnumerable<T> Reverse()
		{
			return new ReverseEnumerable(_root);
		}

		public ImmutableSortedSet<T> ToImmutable()
		{
			return _immutable ?? (_immutable = ImmutableSortedSet<T>.Wrap(Root, _comparer));
		}

		public bool TryGetValue(T equalValue, out T actualValue)
		{
			Node node = _root.Search(equalValue, _comparer);
			if (!node.IsEmpty)
			{
				actualValue = node.Key;
				return true;
			}
			actualValue = equalValue;
			return false;
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			Root.CopyTo(array, arrayIndex);
		}
	}

	private sealed class ReverseEnumerable : IEnumerable<T>, IEnumerable
	{
		private readonly ImmutableSortedSet<T>.Node _root;

		internal ReverseEnumerable(ImmutableSortedSet<T>.Node root)
		{
			Requires.NotNull(root, "root");
			_root = root;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _root.Reverse();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator, ISecurePooledObjectUser, IStrongEnumerator<T>
	{
		private readonly Builder _builder;

		private readonly int _poolUserId;

		private readonly bool _reverse;

		private Node _root;

		private SecurePooledObject<Stack<RefAsValueType<Node>>> _stack;

		private Node _current;

		private int _enumeratingBuilderVersion;

		int ISecurePooledObjectUser.PoolUserId => _poolUserId;

		public T Current
		{
			get
			{
				ThrowIfDisposed();
				if (_current != null)
				{
					return _current.Value;
				}
				throw new InvalidOperationException();
			}
		}

		object? IEnumerator.Current => Current;

		internal Enumerator(Node root, Builder? builder = null, bool reverse = false)
		{
			Requires.NotNull(root, "root");
			_root = root;
			_builder = builder;
			_current = null;
			_reverse = reverse;
			_enumeratingBuilderVersion = builder?.Version ?? (-1);
			_poolUserId = SecureObjectPool.NewId();
			_stack = null;
			if (!SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryTake(this, out _stack))
			{
				_stack = SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
			}
			PushNext(_root);
		}

		public void Dispose()
		{
			_root = null;
			_current = null;
			if (_stack != null && _stack.TryUse(ref this, out var value))
			{
				value.ClearFastWhenEmpty();
				SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryAdd(this, _stack);
				_stack = null;
			}
		}

		public bool MoveNext()
		{
			ThrowIfDisposed();
			ThrowIfChanged();
			Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
			if (stack.Count > 0)
			{
				Node node = (_current = stack.Pop().Value);
				PushNext(_reverse ? node.Left : node.Right);
				return true;
			}
			_current = null;
			return false;
		}

		public void Reset()
		{
			ThrowIfDisposed();
			_enumeratingBuilderVersion = ((_builder != null) ? _builder.Version : (-1));
			_current = null;
			_stack.Use(ref this).ClearFastWhenEmpty();
			PushNext(_root);
		}

		private void ThrowIfDisposed()
		{
			if (_root == null || (_stack != null && !_stack.IsOwned(ref this)))
			{
				Requires.FailObjectDisposed(this);
			}
		}

		private void ThrowIfChanged()
		{
			if (_builder != null && _builder.Version != _enumeratingBuilderVersion)
			{
				throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CollectionModifiedDuringEnumeration);
			}
		}

		private void PushNext(Node node)
		{
			Requires.NotNull(node, "node");
			Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
			while (!node.IsEmpty)
			{
				stack.Push(new RefAsValueType<Node>(node));
				node = (_reverse ? node.Right : node.Left);
			}
		}
	}

	[DebuggerDisplay("{_key}")]
	internal sealed class Node : IBinaryTree<T>, IBinaryTree, IEnumerable<T>, IEnumerable
	{
		internal static readonly Node EmptyNode = new Node();

		private readonly T _key;

		private bool _frozen;

		private byte _height;

		private int _count;

		private ImmutableSortedSet<T>.Node _left;

		private ImmutableSortedSet<T>.Node _right;

		public bool IsEmpty => _left == null;

		public int Height => _height;

		public Node? Left => _left;

		IBinaryTree? IBinaryTree.Left => _left;

		public Node? Right => _right;

		IBinaryTree? IBinaryTree.Right => _right;

		IBinaryTree<T>? IBinaryTree<T>.Left => _left;

		IBinaryTree<T>? IBinaryTree<T>.Right => _right;

		public T Value => _key;

		public int Count => _count;

		internal T Key => _key;

		internal T? Max
		{
			get
			{
				if (IsEmpty)
				{
					return default(T);
				}
				Node node = this;
				while (!node._right.IsEmpty)
				{
					node = node._right;
				}
				return node._key;
			}
		}

		internal T? Min
		{
			get
			{
				if (IsEmpty)
				{
					return default(T);
				}
				Node node = this;
				while (!node._left.IsEmpty)
				{
					node = node._left;
				}
				return node._key;
			}
		}

		internal T this[int index]
		{
			get
			{
				Requires.Range(index >= 0 && index < Count, "index");
				if (index < _left._count)
				{
					return _left[index];
				}
				if (index > _left._count)
				{
					return _right[index - _left._count - 1];
				}
				return _key;
			}
		}

		private Node()
		{
			_frozen = true;
		}

		private Node(T key, ImmutableSortedSet<T>.Node left, ImmutableSortedSet<T>.Node right, bool frozen = false)
		{
			Requires.NotNull(left, "left");
			Requires.NotNull(right, "right");
			_key = key;
			_left = left;
			_right = right;
			_height = checked((byte)(1 + Math.Max(left._height, right._height)));
			_count = 1 + left._count + right._count;
			_frozen = frozen;
		}

		internal ref readonly T ItemRef(int index)
		{
			Requires.Range(index >= 0 && index < Count, "index");
			return ref ItemRefUnchecked(index);
		}

		private ref readonly T ItemRefUnchecked(int index)
		{
			if (index < _left._count)
			{
				return ref _left.ItemRefUnchecked(index);
			}
			if (index > _left._count)
			{
				return ref _right.ItemRefUnchecked(index - _left._count - 1);
			}
			return ref _key;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		[ExcludeFromCodeCoverage]
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		[ExcludeFromCodeCoverage]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal Enumerator GetEnumerator(Builder builder)
		{
			return new Enumerator(this, builder);
		}

		internal void CopyTo(T[] array, int arrayIndex)
		{
			Requires.NotNull(array, "array");
			Requires.Range(arrayIndex >= 0, "arrayIndex");
			Requires.Range(array.Length >= arrayIndex + Count, "arrayIndex");
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				array[arrayIndex++] = current;
			}
		}

		internal void CopyTo(Array array, int arrayIndex)
		{
			Requires.NotNull(array, "array");
			Requires.Range(arrayIndex >= 0, "arrayIndex");
			Requires.Range(array.Length >= arrayIndex + Count, "arrayIndex");
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				array.SetValue(current, arrayIndex++);
			}
		}

		internal Node Add(T key, IComparer<T> comparer, out bool mutated)
		{
			Requires.NotNull(comparer, "comparer");
			if (IsEmpty)
			{
				mutated = true;
				return new Node(key, this, this);
			}
			Node node = this;
			int num = comparer.Compare(key, _key);
			if (num > 0)
			{
				Node right = _right.Add(key, comparer, out mutated);
				if (mutated)
				{
					node = Mutate(null, right);
				}
			}
			else
			{
				if (num >= 0)
				{
					mutated = false;
					return this;
				}
				Node left = _left.Add(key, comparer, out mutated);
				if (mutated)
				{
					node = Mutate(left);
				}
			}
			if (!mutated)
			{
				return node;
			}
			return MakeBalanced(node);
		}

		internal Node Remove(T key, IComparer<T> comparer, out bool mutated)
		{
			Requires.NotNull(comparer, "comparer");
			if (IsEmpty)
			{
				mutated = false;
				return this;
			}
			Node node = this;
			int num = comparer.Compare(key, _key);
			if (num == 0)
			{
				mutated = true;
				if (_right.IsEmpty && _left.IsEmpty)
				{
					node = EmptyNode;
				}
				else if (_right.IsEmpty && !_left.IsEmpty)
				{
					node = _left;
				}
				else if (!_right.IsEmpty && _left.IsEmpty)
				{
					node = _right;
				}
				else
				{
					Node node2 = _right;
					while (!node2._left.IsEmpty)
					{
						node2 = node2._left;
					}
					Node right = _right.Remove(node2._key, comparer, out var _);
					node = node2.Mutate(_left, right);
				}
			}
			else if (num < 0)
			{
				Node left = _left.Remove(key, comparer, out mutated);
				if (mutated)
				{
					node = Mutate(left);
				}
			}
			else
			{
				Node right2 = _right.Remove(key, comparer, out mutated);
				if (mutated)
				{
					node = Mutate(null, right2);
				}
			}
			if (!node.IsEmpty)
			{
				return MakeBalanced(node);
			}
			return node;
		}

		internal bool Contains(T key, IComparer<T> comparer)
		{
			Requires.NotNull(comparer, "comparer");
			return !Search(key, comparer).IsEmpty;
		}

		internal void Freeze()
		{
			if (!_frozen)
			{
				_left.Freeze();
				_right.Freeze();
				_frozen = true;
			}
		}

		internal Node Search(T key, IComparer<T> comparer)
		{
			Requires.NotNull(comparer, "comparer");
			if (IsEmpty)
			{
				return this;
			}
			int num = comparer.Compare(key, _key);
			if (num == 0)
			{
				return this;
			}
			if (num > 0)
			{
				return _right.Search(key, comparer);
			}
			return _left.Search(key, comparer);
		}

		internal int IndexOf(T key, IComparer<T> comparer)
		{
			Requires.NotNull(comparer, "comparer");
			if (IsEmpty)
			{
				return -1;
			}
			int num = comparer.Compare(key, _key);
			if (num == 0)
			{
				return _left.Count;
			}
			if (num > 0)
			{
				int num2 = _right.IndexOf(key, comparer);
				bool num3 = num2 < 0;
				if (num3)
				{
					num2 = ~num2;
				}
				num2 = _left.Count + 1 + num2;
				if (num3)
				{
					num2 = ~num2;
				}
				return num2;
			}
			return _left.IndexOf(key, comparer);
		}

		internal IEnumerator<T> Reverse()
		{
			return new Enumerator(this, null, reverse: true);
		}

		private static ImmutableSortedSet<T>.Node RotateLeft(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._right.IsEmpty)
			{
				return tree;
			}
			Node right = tree._right;
			return right.Mutate(tree.Mutate(null, right._left));
		}

		private static ImmutableSortedSet<T>.Node RotateRight(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._left.IsEmpty)
			{
				return tree;
			}
			Node left = tree._left;
			return left.Mutate(null, tree.Mutate(left._right));
		}

		private static ImmutableSortedSet<T>.Node DoubleLeft(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._right.IsEmpty)
			{
				return tree;
			}
			return RotateLeft(tree.Mutate(null, RotateRight(tree._right)));
		}

		private static ImmutableSortedSet<T>.Node DoubleRight(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._left.IsEmpty)
			{
				return tree;
			}
			return RotateRight(tree.Mutate(RotateLeft(tree._left)));
		}

		private static int Balance(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return tree._right._height - tree._left._height;
		}

		private static bool IsRightHeavy(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return Balance(tree) >= 2;
		}

		private static bool IsLeftHeavy(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return Balance(tree) <= -2;
		}

		private static ImmutableSortedSet<T>.Node MakeBalanced(ImmutableSortedSet<T>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (IsRightHeavy(tree))
			{
				if (Balance(tree._right) >= 0)
				{
					return RotateLeft(tree);
				}
				return DoubleLeft(tree);
			}
			if (IsLeftHeavy(tree))
			{
				if (Balance(tree._left) <= 0)
				{
					return RotateRight(tree);
				}
				return DoubleRight(tree);
			}
			return tree;
		}

		internal static Node NodeTreeFromList(IOrderedCollection<T> items, int start, int length)
		{
			Requires.NotNull(items, "items");
			if (length == 0)
			{
				return EmptyNode;
			}
			int num = (length - 1) / 2;
			int num2 = length - 1 - num;
			Node left = NodeTreeFromList(items, start, num2);
			Node right = NodeTreeFromList(items, start + num2 + 1, num);
			return new Node(items[start + num2], left, right, frozen: true);
		}

		private ImmutableSortedSet<T>.Node Mutate(ImmutableSortedSet<T>.Node left = null, ImmutableSortedSet<T>.Node right = null)
		{
			if (_frozen)
			{
				return new Node(_key, left ?? _left, right ?? _right);
			}
			if (left != null)
			{
				_left = left;
			}
			if (right != null)
			{
				_right = right;
			}
			_height = checked((byte)(1 + Math.Max(_left._height, _right._height)));
			_count = 1 + _left._count + _right._count;
			return this;
		}
	}

	private const float RefillOverIncrementalThreshold = 0.15f;

	public static readonly ImmutableSortedSet<T> Empty = new ImmutableSortedSet<T>();

	private readonly ImmutableSortedSet<T>.Node _root;

	private readonly IComparer<T> _comparer;

	public T? Max => _root.Max;

	public T? Min => _root.Min;

	public bool IsEmpty => _root.IsEmpty;

	public int Count => _root.Count;

	public IComparer<T> KeyComparer => _comparer;

	internal IBinaryTree Root => _root;

	public T this[int index] => _root.ItemRef(index);

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	object ICollection.SyncRoot => this;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection.IsSynchronized => true;

	object? IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	internal ImmutableSortedSet(IComparer<T>? comparer = null)
	{
		_root = Node.EmptyNode;
		_comparer = comparer ?? Comparer<T>.Default;
	}

	private ImmutableSortedSet(ImmutableSortedSet<T>.Node root, IComparer<T> comparer)
	{
		Requires.NotNull(root, "root");
		Requires.NotNull(comparer, "comparer");
		root.Freeze();
		_root = root;
		_comparer = comparer;
	}

	public ImmutableSortedSet<T> Clear()
	{
		if (!_root.IsEmpty)
		{
			return Empty.WithComparer(_comparer);
		}
		return this;
	}

	public ref readonly T ItemRef(int index)
	{
		return ref _root.ItemRef(index);
	}

	public Builder ToBuilder()
	{
		return new Builder(this);
	}

	public ImmutableSortedSet<T> Add(T value)
	{
		bool mutated;
		return Wrap(_root.Add(value, _comparer, out mutated));
	}

	public ImmutableSortedSet<T> Remove(T value)
	{
		bool mutated;
		return Wrap(_root.Remove(value, _comparer, out mutated));
	}

	public bool TryGetValue(T equalValue, out T actualValue)
	{
		Node node = _root.Search(equalValue, _comparer);
		if (node.IsEmpty)
		{
			actualValue = equalValue;
			return false;
		}
		actualValue = node.Key;
		return true;
	}

	public ImmutableSortedSet<T> Intersect(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		ImmutableSortedSet<T> immutableSortedSet = Clear();
		foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
		{
			if (Contains(item))
			{
				immutableSortedSet = immutableSortedSet.Add(item);
			}
		}
		return immutableSortedSet;
	}

	public ImmutableSortedSet<T> Except(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		Node node = _root;
		foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
		{
			node = node.Remove(item, _comparer, out var _);
		}
		return Wrap(node);
	}

	public ImmutableSortedSet<T> SymmetricExcept(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		ImmutableSortedSet<T> immutableSortedSet = ImmutableSortedSet.CreateRange(_comparer, other);
		ImmutableSortedSet<T> immutableSortedSet2 = Clear();
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				if (!immutableSortedSet.Contains(current))
				{
					immutableSortedSet2 = immutableSortedSet2.Add(current);
				}
			}
		}
		foreach (T item in immutableSortedSet)
		{
			if (!Contains(item))
			{
				immutableSortedSet2 = immutableSortedSet2.Add(item);
			}
		}
		return immutableSortedSet2;
	}

	public ImmutableSortedSet<T> Union(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (TryCastToImmutableSortedSet(other, out ImmutableSortedSet<T> other2) && other2.KeyComparer == KeyComparer)
		{
			if (other2.IsEmpty)
			{
				return this;
			}
			if (IsEmpty)
			{
				return other2;
			}
			if (other2.Count > Count)
			{
				return other2.Union(this);
			}
		}
		if (IsEmpty || (other.TryGetCount(out var count) && (float)(Count + count) * 0.15f > (float)Count))
		{
			return LeafToRootRefill(other);
		}
		return UnionIncremental(other);
	}

	internal ImmutableSortedSet<T> Union(ReadOnlySpan<T> other)
	{
		if (IsEmpty || (float)(Count + other.Length) * 0.15f > (float)Count)
		{
			return LeafToRootRefill(other);
		}
		return UnionIncremental(other);
	}

	public ImmutableSortedSet<T> WithComparer(IComparer<T>? comparer)
	{
		if (comparer == null)
		{
			comparer = Comparer<T>.Default;
		}
		if (comparer == _comparer)
		{
			return this;
		}
		return new ImmutableSortedSet<T>(Node.EmptyNode, comparer).Union(this);
	}

	public bool SetEquals(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (this == other)
		{
			return true;
		}
		SortedSet<T> sortedSet = new SortedSet<T>(other, KeyComparer);
		if (Count != sortedSet.Count)
		{
			return false;
		}
		int num = 0;
		foreach (T item in sortedSet)
		{
			if (!Contains(item))
			{
				return false;
			}
			num++;
		}
		return num == Count;
	}

	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (IsEmpty)
		{
			return other.Any();
		}
		SortedSet<T> sortedSet = new SortedSet<T>(other, KeyComparer);
		if (Count >= sortedSet.Count)
		{
			return false;
		}
		int num = 0;
		bool flag = false;
		foreach (T item in sortedSet)
		{
			if (Contains(item))
			{
				num++;
			}
			else
			{
				flag = true;
			}
			if ((num == Count) & flag)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (IsEmpty)
		{
			return false;
		}
		int num = 0;
		foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
		{
			num++;
			if (!Contains(item))
			{
				return false;
			}
		}
		return Count > num;
	}

	public bool IsSubsetOf(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (IsEmpty)
		{
			return true;
		}
		SortedSet<T> sortedSet = new SortedSet<T>(other, KeyComparer);
		int num = 0;
		foreach (T item in sortedSet)
		{
			if (Contains(item))
			{
				num++;
			}
		}
		return num == Count;
	}

	public bool IsSupersetOf(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
		{
			if (!Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	public bool Overlaps(IEnumerable<T> other)
	{
		Requires.NotNull(other, "other");
		if (IsEmpty)
		{
			return false;
		}
		foreach (T item in other.GetEnumerableDisposable<T, Enumerator>())
		{
			if (Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<T> Reverse()
	{
		return new ReverseEnumerable(_root);
	}

	public int IndexOf(T item)
	{
		return _root.IndexOf(item, _comparer);
	}

	public bool Contains(T value)
	{
		return _root.Contains(value, _comparer);
	}

	IImmutableSet<T> IImmutableSet<T>.Clear()
	{
		return Clear();
	}

	IImmutableSet<T> IImmutableSet<T>.Add(T value)
	{
		return Add(value);
	}

	IImmutableSet<T> IImmutableSet<T>.Remove(T value)
	{
		return Remove(value);
	}

	IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
	{
		return Intersect(other);
	}

	IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other)
	{
		return Except(other);
	}

	IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other)
	{
		return SymmetricExcept(other);
	}

	IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other)
	{
		return Union(other);
	}

	bool ISet<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ISet<T>.ExceptWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	void ISet<T>.IntersectWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	void ISet<T>.UnionWith(IEnumerable<T> other)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		_root.CopyTo(array, arrayIndex);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	private static bool IsCompatibleObject(object value)
	{
		if (!(value is T))
		{
			if (default(T) == null)
			{
				return value == null;
			}
			return false;
		}
		return true;
	}

	bool IList.Contains(object value)
	{
		if (IsCompatibleObject(value))
		{
			return Contains((T)value);
		}
		return false;
	}

	int IList.IndexOf(object value)
	{
		if (IsCompatibleObject(value))
		{
			return IndexOf((T)value);
		}
		return -1;
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		_root.CopyTo(array, index);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		if (!IsEmpty)
		{
			return GetEnumerator();
		}
		return Enumerable.Empty<T>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public Enumerator GetEnumerator()
	{
		return _root.GetEnumerator();
	}

	private static bool TryCastToImmutableSortedSet(IEnumerable<T> sequence, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullWhen(true)] out ImmutableSortedSet<T> other)
	{
		other = sequence as ImmutableSortedSet<T>;
		if (other != null)
		{
			return true;
		}
		if (sequence is Builder builder)
		{
			other = builder.ToImmutable();
			return true;
		}
		return false;
	}

	private static ImmutableSortedSet<T> Wrap(ImmutableSortedSet<T>.Node root, IComparer<T> comparer)
	{
		if (!root.IsEmpty)
		{
			return new ImmutableSortedSet<T>(root, comparer);
		}
		return Empty.WithComparer(comparer);
	}

	private ImmutableSortedSet<T> UnionIncremental(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		Node node = _root;
		foreach (T item in items.GetEnumerableDisposable<T, Enumerator>())
		{
			node = node.Add(item, _comparer, out var _);
		}
		return Wrap(node);
	}

	private ImmutableSortedSet<T> UnionIncremental(ReadOnlySpan<T> items)
	{
		Node node = _root;
		ReadOnlySpan<T> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T key = readOnlySpan[i];
			node = node.Add(key, _comparer, out var _);
		}
		return Wrap(node);
	}

	private ImmutableSortedSet<T> Wrap(ImmutableSortedSet<T>.Node root)
	{
		if (root != _root)
		{
			if (!root.IsEmpty)
			{
				return new ImmutableSortedSet<T>(root, _comparer);
			}
			return Clear();
		}
		return this;
	}

	private ImmutableSortedSet<T> LeafToRootRefill(IEnumerable<T> addedItems)
	{
		Requires.NotNull(addedItems, "addedItems");
		List<T> list;
		if (IsEmpty)
		{
			if (addedItems.TryGetCount(out var count) && count == 0)
			{
				return this;
			}
			list = new List<T>(addedItems);
			if (list.Count == 0)
			{
				return this;
			}
		}
		else
		{
			list = new List<T>(this);
			list.AddRange(addedItems);
		}
		IComparer<T> keyComparer = KeyComparer;
		list.Sort(keyComparer);
		int num = 1;
		for (int i = 1; i < list.Count; i++)
		{
			if (keyComparer.Compare(list[i], list[i - 1]) != 0)
			{
				list[num++] = list[i];
			}
		}
		list.RemoveRange(num, list.Count - num);
		Node root = Node.NodeTreeFromList(list.AsOrderedCollection(), 0, list.Count);
		return Wrap(root);
	}

	private ImmutableSortedSet<T> LeafToRootRefill(ReadOnlySpan<T> addedItems)
	{
		List<T> list;
		if (IsEmpty && addedItems.IsEmpty)
		{
			if (addedItems.IsEmpty)
			{
				return this;
			}
			list = new List<T>(addedItems.Length);
		}
		else
		{
			list = new List<T>(Count + addedItems.Length);
			list.AddRange(this);
		}
		ReadOnlySpan<T> readOnlySpan = addedItems;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T item = readOnlySpan[i];
			list.Add(item);
		}
		IComparer<T> keyComparer = KeyComparer;
		list.Sort(keyComparer);
		int num = 1;
		for (int j = 1; j < list.Count; j++)
		{
			if (keyComparer.Compare(list[j], list[j - 1]) != 0)
			{
				list[num++] = list[j];
			}
		}
		list.RemoveRange(num, list.Count - num);
		Node root = Node.NodeTreeFromList(list.AsOrderedCollection(), 0, list.Count);
		return Wrap(root);
	}
}
