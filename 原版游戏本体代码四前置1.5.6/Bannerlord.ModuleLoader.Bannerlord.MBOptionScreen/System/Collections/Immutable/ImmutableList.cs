using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Immutable;

internal static class ImmutableList
{
	public static ImmutableList<T> Create<T>()
	{
		return ImmutableList<T>.Empty;
	}

	public static ImmutableList<T> Create<T>(T item)
	{
		return ImmutableList<T>.Empty.Add(item);
	}

	public static ImmutableList<T> CreateRange<T>(IEnumerable<T> items)
	{
		return ImmutableList<T>.Empty.AddRange(items);
	}

	public static ImmutableList<T> Create<T>(params T[] items)
	{
		Requires.NotNull(items, "items");
		return Create((ReadOnlySpan<T>)items);
	}

	public static ImmutableList<T> Create<T>(params ReadOnlySpan<T> items)
	{
		return ImmutableList<T>.Empty.AddRange(items);
	}

	public static ImmutableList<T>.Builder CreateBuilder<T>()
	{
		return Create<T>().ToBuilder();
	}

	public static ImmutableList<TSource> ToImmutableList<TSource>(this IEnumerable<TSource> source)
	{
		if (source is ImmutableList<TSource> result)
		{
			return result;
		}
		return ImmutableList<TSource>.Empty.AddRange(source);
	}

	public static ImmutableList<TSource> ToImmutableList<TSource>(this ImmutableList<TSource>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		return builder.ToImmutable();
	}

	public static IImmutableList<T> Replace<T>(this IImmutableList<T> list, T oldValue, T newValue)
	{
		Requires.NotNull(list, "list");
		return list.Replace(oldValue, newValue, EqualityComparer<T>.Default);
	}

	public static IImmutableList<T> Remove<T>(this IImmutableList<T> list, T value)
	{
		Requires.NotNull(list, "list");
		return list.Remove(value, EqualityComparer<T>.Default);
	}

	public static IImmutableList<T> RemoveRange<T>(this IImmutableList<T> list, IEnumerable<T> items)
	{
		Requires.NotNull(list, "list");
		return list.RemoveRange(items, EqualityComparer<T>.Default);
	}

	public static int IndexOf<T>(this IImmutableList<T> list, T item)
	{
		Requires.NotNull(list, "list");
		return list.IndexOf(item, 0, list.Count, EqualityComparer<T>.Default);
	}

	public static int IndexOf<T>(this IImmutableList<T> list, T item, IEqualityComparer<T>? equalityComparer)
	{
		Requires.NotNull(list, "list");
		return list.IndexOf(item, 0, list.Count, equalityComparer);
	}

	public static int IndexOf<T>(this IImmutableList<T> list, T item, int startIndex)
	{
		Requires.NotNull(list, "list");
		return list.IndexOf(item, startIndex, list.Count - startIndex, EqualityComparer<T>.Default);
	}

	public static int IndexOf<T>(this IImmutableList<T> list, T item, int startIndex, int count)
	{
		Requires.NotNull(list, "list");
		return list.IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}

	public static int LastIndexOf<T>(this IImmutableList<T> list, T item)
	{
		Requires.NotNull(list, "list");
		if (list.Count == 0)
		{
			return -1;
		}
		return list.LastIndexOf(item, list.Count - 1, list.Count, EqualityComparer<T>.Default);
	}

	public static int LastIndexOf<T>(this IImmutableList<T> list, T item, IEqualityComparer<T>? equalityComparer)
	{
		Requires.NotNull(list, "list");
		if (list.Count == 0)
		{
			return -1;
		}
		return list.LastIndexOf(item, list.Count - 1, list.Count, equalityComparer);
	}

	public static int LastIndexOf<T>(this IImmutableList<T> list, T item, int startIndex)
	{
		Requires.NotNull(list, "list");
		if (list.Count == 0 && startIndex == 0)
		{
			return -1;
		}
		return list.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
	}

	public static int LastIndexOf<T>(this IImmutableList<T> list, T item, int startIndex, int count)
	{
		Requires.NotNull(list, "list");
		return list.LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}
}
[CollectionBuilder(typeof(ImmutableList), "Create")]
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
internal sealed class ImmutableList<T> : IImmutableList<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IList<T>, ICollection<T>, IList, ICollection, IOrderedCollection<T>, IImmutableListQueries<T>, IStrongEnumerable<T, ImmutableList<T>.Enumerator>
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(ImmutableListBuilderDebuggerProxy<>))]
	public sealed class Builder : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IOrderedCollection<T>, IImmutableListQueries<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		private ImmutableList<T>.Node _root = Node.EmptyNode;

		private ImmutableList<T> _immutable;

		private int _version;

		private object _syncRoot;

		public int Count => Root.Count;

		bool ICollection<T>.IsReadOnly => false;

		internal int Version => _version;

		internal Node Root
		{
			get
			{
				return _root;
			}
			private set
			{
				_version++;
				if (_root != value)
				{
					_root = value;
					_immutable = null;
				}
			}
		}

		public T this[int index]
		{
			get
			{
				return Root.ItemRef(index);
			}
			set
			{
				Root = Root.ReplaceAt(index, value);
			}
		}

		T IOrderedCollection<T>.this[int index] => this[index];

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object? IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				this[index] = (T)value;
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

		internal Builder(ImmutableList<T> list)
		{
			Requires.NotNull(list, "list");
			_root = list._root;
			_immutable = list;
		}

		public ref readonly T ItemRef(int index)
		{
			return ref Root.ItemRef(index);
		}

		public int IndexOf(T item)
		{
			return Root.IndexOf(item, EqualityComparer<T>.Default);
		}

		public void Insert(int index, T item)
		{
			Root = Root.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			Root = Root.RemoveAt(index);
		}

		public void Add(T item)
		{
			Root = Root.Add(item);
		}

		public void Clear()
		{
			Root = Node.EmptyNode;
		}

		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}

		public bool Remove(T item)
		{
			int num = IndexOf(item);
			if (num < 0)
			{
				return false;
			}
			Root = Root.RemoveAt(num);
			return true;
		}

		public ImmutableList<T>.Enumerator GetEnumerator()
		{
			return Root.GetEnumerator(this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void ForEach(Action<T> action)
		{
			Requires.NotNull(action, "action");
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				action(current);
			}
		}

		public void CopyTo(T[] array)
		{
			_root.CopyTo(array);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_root.CopyTo(array, arrayIndex);
		}

		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			_root.CopyTo(index, array, arrayIndex, count);
		}

		public ImmutableList<T> GetRange(int index, int count)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(index + count <= Count, "count");
			return ImmutableList<T>.WrapNode(Node.NodeTreeFromList(this, index, count));
		}

		public ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
		{
			Requires.NotNull(converter, "converter");
			return ImmutableList<TOutput>.WrapNode(_root.ConvertAll(converter));
		}

		public bool Exists(Predicate<T> match)
		{
			return _root.Exists(match);
		}

		public T? Find(Predicate<T> match)
		{
			return _root.Find(match);
		}

		public ImmutableList<T> FindAll(Predicate<T> match)
		{
			return _root.FindAll(match);
		}

		public int FindIndex(Predicate<T> match)
		{
			return _root.FindIndex(match);
		}

		public int FindIndex(int startIndex, Predicate<T> match)
		{
			return _root.FindIndex(startIndex, match);
		}

		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			return _root.FindIndex(startIndex, count, match);
		}

		public T? FindLast(Predicate<T> match)
		{
			return _root.FindLast(match);
		}

		public int FindLastIndex(Predicate<T> match)
		{
			return _root.FindLastIndex(match);
		}

		public int FindLastIndex(int startIndex, Predicate<T> match)
		{
			return _root.FindLastIndex(startIndex, match);
		}

		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			return _root.FindLastIndex(startIndex, count, match);
		}

		public int IndexOf(T item, int index)
		{
			return _root.IndexOf(item, index, Count - index, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int index, int count)
		{
			return _root.IndexOf(item, index, count, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		{
			return _root.IndexOf(item, index, count, equalityComparer);
		}

		public int LastIndexOf(T item)
		{
			if (Count == 0)
			{
				return -1;
			}
			return _root.LastIndexOf(item, Count - 1, Count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex)
		{
			if (Count == 0 && startIndex == 0)
			{
				return -1;
			}
			return _root.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count)
		{
			return _root.LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
		{
			return _root.LastIndexOf(item, startIndex, count, equalityComparer);
		}

		public bool TrueForAll(Predicate<T> match)
		{
			return _root.TrueForAll(match);
		}

		public void AddRange(IEnumerable<T> items)
		{
			Requires.NotNull(items, "items");
			Root = Root.AddRange(items);
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			Requires.NotNull(items, "items");
			Root = Root.InsertRange(index, items);
		}

		public int RemoveAll(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			int count = Count;
			Root = Root.RemoveAll(match);
			return count - Count;
		}

		public bool Remove(T item, IEqualityComparer<T>? equalityComparer)
		{
			int num = IndexOf(item, 0, Count, equalityComparer);
			if (num >= 0)
			{
				RemoveAt(num);
				return true;
			}
			return false;
		}

		public void RemoveRange(int index, int count)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			Requires.Range(count >= 0 && index <= Count - count, "count");
			int num = count;
			while (num-- > 0)
			{
				RemoveAt(index);
			}
		}

		public void RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
		{
			Requires.NotNull(items, "items");
			foreach (T item in items.GetEnumerableDisposable<T, Enumerator>())
			{
				int num = Root.IndexOf(item, equalityComparer);
				if (num >= 0)
				{
					RemoveAt(num);
				}
			}
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			RemoveRange(items, EqualityComparer<T>.Default);
		}

		public void Replace(T oldValue, T newValue)
		{
			Replace(oldValue, newValue, EqualityComparer<T>.Default);
		}

		public void Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
		{
			int num = IndexOf(oldValue, 0, Count, equalityComparer);
			if (num < 0)
			{
				throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CannotFindOldValue, "oldValue");
			}
			Root = Root.ReplaceAt(num, newValue);
		}

		public void Reverse()
		{
			Reverse(0, Count);
		}

		public void Reverse(int index, int count)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(index + count <= Count, "count");
			Root = Root.Reverse(index, count);
		}

		public void Sort()
		{
			Root = Root.Sort();
		}

		public void Sort(Comparison<T> comparison)
		{
			Requires.NotNull(comparison, "comparison");
			Root = Root.Sort(comparison);
		}

		public void Sort(IComparer<T>? comparer)
		{
			Root = Root.Sort(comparer);
		}

		public void Sort(int index, int count, IComparer<T>? comparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(index + count <= Count, "count");
			Root = Root.Sort(index, count, comparer);
		}

		public int BinarySearch(T item)
		{
			return BinarySearch(item, null);
		}

		public int BinarySearch(T item, IComparer<T>? comparer)
		{
			return BinarySearch(0, Count, item, comparer);
		}

		public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
		{
			return Root.BinarySearch(index, count, item, comparer);
		}

		public ImmutableList<T> ToImmutable()
		{
			return _immutable ?? (_immutable = ImmutableList<T>.WrapNode(Root));
		}

		int IList.Add(object value)
		{
			Add((T)value);
			return Count - 1;
		}

		void IList.Clear()
		{
			Clear();
		}

		bool IList.Contains(object value)
		{
			if (ImmutableList<T>.IsCompatibleObject(value))
			{
				return Contains((T)value);
			}
			return false;
		}

		int IList.IndexOf(object value)
		{
			if (ImmutableList<T>.IsCompatibleObject(value))
			{
				return IndexOf((T)value);
			}
			return -1;
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (T)value);
		}

		void IList.Remove(object value)
		{
			if (ImmutableList<T>.IsCompatibleObject(value))
			{
				Remove((T)value);
			}
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			Root.CopyTo(array, arrayIndex);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator, ISecurePooledObjectUser, IStrongEnumerator<T>
	{
		private readonly Builder _builder;

		private readonly int _poolUserId;

		private readonly int _startIndex;

		private readonly int _count;

		private int _remainingCount;

		private readonly bool _reversed;

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

		internal Enumerator(Node root, Builder? builder = null, int startIndex = -1, int count = -1, bool reversed = false)
		{
			Requires.NotNull(root, "root");
			Requires.Range(startIndex >= -1, "startIndex");
			Requires.Range(count >= -1, "count");
			Requires.Argument(reversed || count == -1 || ((startIndex != -1) ? startIndex : 0) + count <= root.Count);
			Requires.Argument(!reversed || count == -1 || ((startIndex == -1) ? (root.Count - 1) : startIndex) - count + 1 >= 0);
			_root = root;
			_builder = builder;
			_current = null;
			_startIndex = ((startIndex >= 0) ? startIndex : (reversed ? (root.Count - 1) : 0));
			_count = ((count == -1) ? root.Count : count);
			_remainingCount = _count;
			_reversed = reversed;
			_enumeratingBuilderVersion = builder?.Version ?? (-1);
			_poolUserId = SecureObjectPool.NewId();
			_stack = null;
			if (_count > 0)
			{
				if (!SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryTake(this, out _stack))
				{
					_stack = SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
				}
				ResetStack();
			}
		}

		public void Dispose()
		{
			_root = null;
			_current = null;
			if (_stack != null && _stack.TryUse(ref this, out var value))
			{
				value.ClearFastWhenEmpty();
				SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryAdd(this, _stack);
			}
			_stack = null;
		}

		public bool MoveNext()
		{
			ThrowIfDisposed();
			ThrowIfChanged();
			if (_stack != null)
			{
				Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
				if (_remainingCount > 0 && stack.Count > 0)
				{
					PushNext(NextBranch(_current = stack.Pop().Value));
					_remainingCount--;
					return true;
				}
			}
			_current = null;
			return false;
		}

		public void Reset()
		{
			ThrowIfDisposed();
			_enumeratingBuilderVersion = ((_builder != null) ? _builder.Version : (-1));
			_remainingCount = _count;
			if (_stack != null)
			{
				ResetStack();
			}
		}

		private void ResetStack()
		{
			Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
			stack.ClearFastWhenEmpty();
			Node node = _root;
			int num = (_reversed ? (_root.Count - _startIndex - 1) : _startIndex);
			while (!node.IsEmpty && num != PreviousBranch(node).Count)
			{
				if (num < PreviousBranch(node).Count)
				{
					stack.Push(new RefAsValueType<Node>(node));
					node = PreviousBranch(node);
				}
				else
				{
					num -= PreviousBranch(node).Count + 1;
					node = NextBranch(node);
				}
			}
			if (!node.IsEmpty)
			{
				stack.Push(new RefAsValueType<Node>(node));
			}
		}

		private Node NextBranch(Node node)
		{
			if (!_reversed)
			{
				return node.Right;
			}
			return node.Left;
		}

		private Node PreviousBranch(Node node)
		{
			if (!_reversed)
			{
				return node.Left;
			}
			return node.Right;
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
			if (!node.IsEmpty)
			{
				Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
				while (!node.IsEmpty)
				{
					stack.Push(new RefAsValueType<Node>(node));
					node = PreviousBranch(node);
				}
			}
		}
	}

	[DebuggerDisplay("{_key}")]
	internal sealed class Node : IBinaryTree<T>, IBinaryTree, IEnumerable<T>, IEnumerable
	{
		internal static readonly Node EmptyNode = new Node();

		private T _key;

		private bool _frozen;

		private byte _height;

		private int _count;

		private ImmutableList<T>.Node _left;

		private ImmutableList<T>.Node _right;

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

		private int BalanceFactor => _right._height - _left._height;

		private bool IsRightHeavy => BalanceFactor >= 2;

		private bool IsLeftHeavy => BalanceFactor <= -2;

		private bool IsBalanced => (uint)(BalanceFactor + 1) <= 2u;

		private Node()
		{
			_frozen = true;
		}

		private Node(T key, ImmutableList<T>.Node left, ImmutableList<T>.Node right, bool frozen = false)
		{
			Requires.NotNull(left, "left");
			Requires.NotNull(right, "right");
			_key = key;
			_left = left;
			_right = right;
			_height = ParentHeight(left, right);
			_count = ParentCount(left, right);
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

		internal static Node NodeTreeFromList(IOrderedCollection<T> items, int start, int length)
		{
			Requires.NotNull(items, "items");
			Requires.Range(start >= 0, "start");
			Requires.Range(length >= 0, "length");
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

		internal static Node NodeTreeFromList(ReadOnlySpan<T> items)
		{
			if (items.IsEmpty)
			{
				return EmptyNode;
			}
			int num = (items.Length - 1) / 2;
			int num2 = items.Length - 1 - num;
			Node left = NodeTreeFromList(items.Slice(0, num2));
			Node right = NodeTreeFromList(items.Slice(num2 + 1));
			return new Node(items[num2], left, right, frozen: true);
		}

		internal Node Add(T key)
		{
			if (IsEmpty)
			{
				return CreateLeaf(key);
			}
			Node right = _right.Add(key);
			Node node = MutateRight(right);
			if (!node.IsBalanced)
			{
				return node.BalanceRight();
			}
			return node;
		}

		internal Node Insert(int index, T key)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			if (IsEmpty)
			{
				return CreateLeaf(key);
			}
			if (index <= _left._count)
			{
				Node left = _left.Insert(index, key);
				Node node = MutateLeft(left);
				if (!node.IsBalanced)
				{
					return node.BalanceLeft();
				}
				return node;
			}
			Node right = _right.Insert(index - _left._count - 1, key);
			Node node2 = MutateRight(right);
			if (!node2.IsBalanced)
			{
				return node2.BalanceRight();
			}
			return node2;
		}

		internal Node AddRange(IEnumerable<T> keys)
		{
			Requires.NotNull(keys, "keys");
			if (IsEmpty)
			{
				return CreateRange(keys);
			}
			Node right = _right.AddRange(keys);
			return MutateRight(right).BalanceMany();
		}

		internal Node AddRange(ReadOnlySpan<T> keys)
		{
			if (IsEmpty)
			{
				return NodeTreeFromList(keys);
			}
			Node right = _right.AddRange(keys);
			return MutateRight(right).BalanceMany();
		}

		internal Node InsertRange(int index, IEnumerable<T> keys)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			Requires.NotNull(keys, "keys");
			if (IsEmpty)
			{
				return CreateRange(keys);
			}
			Node node;
			if (index <= _left._count)
			{
				Node left = _left.InsertRange(index, keys);
				node = MutateLeft(left);
			}
			else
			{
				Node right = _right.InsertRange(index - _left._count - 1, keys);
				node = MutateRight(right);
			}
			return node.BalanceMany();
		}

		internal Node RemoveAt(int index)
		{
			Requires.Range(index >= 0 && index < Count, "index");
			Node node;
			if (index == _left._count)
			{
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
					Node right = _right.RemoveAt(0);
					node = node2.MutateBoth(_left, right);
				}
			}
			else if (index < _left._count)
			{
				Node left = _left.RemoveAt(index);
				node = MutateLeft(left);
			}
			else
			{
				Node right2 = _right.RemoveAt(index - _left._count - 1);
				node = MutateRight(right2);
			}
			if (!node.IsEmpty && !node.IsBalanced)
			{
				return node.Balance();
			}
			return node;
		}

		internal Node RemoveAll(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			Node node = this;
			Enumerator enumerator = new Enumerator(node);
			try
			{
				int num = 0;
				while (enumerator.MoveNext())
				{
					if (match(enumerator.Current))
					{
						node = node.RemoveAt(num);
						enumerator.Dispose();
						enumerator = new Enumerator(node, null, num);
					}
					else
					{
						num++;
					}
				}
				return node;
			}
			finally
			{
				enumerator.Dispose();
			}
		}

		internal Node ReplaceAt(int index, T value)
		{
			Requires.Range(index >= 0 && index < Count, "index");
			if (index == _left._count)
			{
				return MutateKey(value);
			}
			if (index < _left._count)
			{
				Node left = _left.ReplaceAt(index, value);
				return MutateLeft(left);
			}
			Node right = _right.ReplaceAt(index - _left._count - 1, value);
			return MutateRight(right);
		}

		internal Node Reverse()
		{
			return Reverse(0, Count);
		}

		internal Node Reverse(int index, int count)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(index + count <= Count, "index");
			Node node = this;
			int num = index;
			int num2 = index + count - 1;
			while (num < num2)
			{
				T value = node.ItemRef(num);
				T value2 = node.ItemRef(num2);
				node = node.ReplaceAt(num2, value).ReplaceAt(num, value2);
				num++;
				num2--;
			}
			return node;
		}

		internal Node Sort()
		{
			return Sort(Comparer<T>.Default);
		}

		internal Node Sort(Comparison<T> comparison)
		{
			Requires.NotNull(comparison, "comparison");
			T[] array = new T[Count];
			CopyTo(array);
			Array.Sort(array, comparison);
			return NodeTreeFromList(array.AsOrderedCollection(), 0, Count);
		}

		internal Node Sort(IComparer<T>? comparer)
		{
			return Sort(0, Count, comparer);
		}

		internal Node Sort(int index, int count, IComparer<T>? comparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Argument(index + count <= Count);
			T[] array = new T[Count];
			CopyTo(array);
			Array.Sort(array, index, count, comparer);
			return NodeTreeFromList(array.AsOrderedCollection(), 0, Count);
		}

		internal int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			if (IsEmpty || count <= 0)
			{
				return ~index;
			}
			int count2 = _left.Count;
			if (index + count <= count2)
			{
				return _left.BinarySearch(index, count, item, comparer);
			}
			if (index > count2)
			{
				int num = _right.BinarySearch(index - count2 - 1, count, item, comparer);
				int num2 = count2 + 1;
				if (num >= 0)
				{
					return num + num2;
				}
				return num - num2;
			}
			int num3 = comparer.Compare(item, _key);
			if (num3 == 0)
			{
				return count2;
			}
			if (num3 > 0)
			{
				int num4 = count - (count2 - index) - 1;
				int num5 = ((num4 < 0) ? (-1) : _right.BinarySearch(0, num4, item, comparer));
				int num6 = count2 + 1;
				if (num5 >= 0)
				{
					return num5 + num6;
				}
				return num5 - num6;
			}
			if (index == count2)
			{
				return ~index;
			}
			return _left.BinarySearch(index, count, item, comparer);
		}

		internal int IndexOf(T item, IEqualityComparer<T>? equalityComparer)
		{
			return IndexOf(item, 0, Count, equalityComparer);
		}

		internal bool Contains(T item, IEqualityComparer<T> equalityComparer)
		{
			return Contains(this, item, equalityComparer);
		}

		internal int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(count <= Count, "count");
			Requires.Range(index + count <= Count, "count");
			if (equalityComparer == null)
			{
				equalityComparer = EqualityComparer<T>.Default;
			}
			using (Enumerator enumerator = new Enumerator(this, null, index, count))
			{
				while (enumerator.MoveNext())
				{
					if (equalityComparer.Equals(item, enumerator.Current))
					{
						return index;
					}
					index++;
				}
			}
			return -1;
		}

		internal int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0 && count <= Count, "count");
			Requires.Argument(index - count + 1 >= 0);
			if (equalityComparer == null)
			{
				equalityComparer = EqualityComparer<T>.Default;
			}
			using (Enumerator enumerator = new Enumerator(this, null, index, count, reversed: true))
			{
				while (enumerator.MoveNext())
				{
					if (equalityComparer.Equals(item, enumerator.Current))
					{
						return index;
					}
					index--;
				}
			}
			return -1;
		}

		internal void CopyTo(T[] array)
		{
			Requires.NotNull(array, "array");
			Requires.Range(array.Length >= Count, "array");
			int num = 0;
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				array[num++] = current;
			}
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

		internal void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			Requires.NotNull(array, "array");
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0, "count");
			Requires.Range(index + count <= Count, "count");
			Requires.Range(arrayIndex >= 0, "arrayIndex");
			Requires.Range(arrayIndex + count <= array.Length, "arrayIndex");
			using Enumerator enumerator = new Enumerator(this, null, index, count);
			while (enumerator.MoveNext())
			{
				array[arrayIndex++] = enumerator.Current;
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

		internal ImmutableList<TOutput>.Node ConvertAll<TOutput>(Func<T, TOutput> converter)
		{
			ImmutableList<TOutput>.Node emptyNode = ImmutableList<TOutput>.Node.EmptyNode;
			if (IsEmpty)
			{
				return emptyNode;
			}
			return emptyNode.AddRange(this.Select<T, TOutput>(converter));
		}

		internal bool TrueForAll(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					if (!match(current))
					{
						return false;
					}
				}
			}
			return true;
		}

		internal bool Exists(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					if (match(current))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal T? Find(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					if (match(current))
					{
						return current;
					}
				}
			}
			return default(T);
		}

		internal ImmutableList<T> FindAll(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			if (IsEmpty)
			{
				return ImmutableList<T>.Empty;
			}
			List<T> list = null;
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T current = enumerator.Current;
					if (match(current))
					{
						if (list == null)
						{
							list = new List<T>();
						}
						list.Add(current);
					}
				}
			}
			if (list == null)
			{
				return ImmutableList<T>.Empty;
			}
			return ImmutableList.CreateRange(list);
		}

		internal int FindIndex(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			return FindIndex(0, _count, match);
		}

		internal int FindIndex(int startIndex, Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			Requires.Range(startIndex >= 0 && startIndex <= Count, "startIndex");
			return FindIndex(startIndex, Count - startIndex, match);
		}

		internal int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			Requires.Range(startIndex >= 0, "startIndex");
			Requires.Range(count >= 0, "count");
			Requires.Range(startIndex <= Count - count, "count");
			using (Enumerator enumerator = new Enumerator(this, null, startIndex, count))
			{
				int num = startIndex;
				while (enumerator.MoveNext())
				{
					if (match(enumerator.Current))
					{
						return num;
					}
					num++;
				}
			}
			return -1;
		}

		internal T? FindLast(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			using (Enumerator enumerator = new Enumerator(this, null, -1, -1, reversed: true))
			{
				while (enumerator.MoveNext())
				{
					if (match(enumerator.Current))
					{
						return enumerator.Current;
					}
				}
			}
			return default(T);
		}

		internal int FindLastIndex(Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			if (!IsEmpty)
			{
				return FindLastIndex(Count - 1, Count, match);
			}
			return -1;
		}

		internal int FindLastIndex(int startIndex, Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			Requires.Range(startIndex >= 0, "startIndex");
			Requires.Range(startIndex == 0 || startIndex < Count, "startIndex");
			if (!IsEmpty)
			{
				return FindLastIndex(startIndex, startIndex + 1, match);
			}
			return -1;
		}

		internal int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			Requires.NotNull(match, "match");
			Requires.Range(startIndex >= 0, "startIndex");
			Requires.Range(count <= Count, "count");
			Requires.Range(startIndex - count + 1 >= 0, "startIndex");
			using (Enumerator enumerator = new Enumerator(this, null, startIndex, count, reversed: true))
			{
				int num = startIndex;
				while (enumerator.MoveNext())
				{
					if (match(enumerator.Current))
					{
						return num;
					}
					num--;
				}
			}
			return -1;
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

		private ImmutableList<T>.Node RotateLeft()
		{
			return _right.MutateLeft(MutateRight(_right._left));
		}

		private ImmutableList<T>.Node RotateRight()
		{
			return _left.MutateRight(MutateLeft(_left._right));
		}

		private ImmutableList<T>.Node DoubleLeft()
		{
			Node right = _right;
			Node left = right._left;
			return left.MutateBoth(MutateRight(left._left), right.MutateLeft(left._right));
		}

		private ImmutableList<T>.Node DoubleRight()
		{
			Node left = _left;
			Node right = left._right;
			return right.MutateBoth(left.MutateRight(right._left), MutateLeft(right._right));
		}

		private ImmutableList<T>.Node Balance()
		{
			if (!IsLeftHeavy)
			{
				return BalanceRight();
			}
			return BalanceLeft();
		}

		private ImmutableList<T>.Node BalanceLeft()
		{
			if (_left.BalanceFactor <= 0)
			{
				return RotateRight();
			}
			return DoubleRight();
		}

		private ImmutableList<T>.Node BalanceRight()
		{
			if (_right.BalanceFactor >= 0)
			{
				return RotateLeft();
			}
			return DoubleLeft();
		}

		private ImmutableList<T>.Node BalanceMany()
		{
			Node node = this;
			while (!node.IsBalanced)
			{
				if (node.IsRightHeavy)
				{
					node = node.BalanceRight();
					node.MutateLeft(node._left.BalanceMany());
				}
				else
				{
					node = node.BalanceLeft();
					node.MutateRight(node._right.BalanceMany());
				}
			}
			return node;
		}

		private ImmutableList<T>.Node MutateBoth(ImmutableList<T>.Node left, ImmutableList<T>.Node right)
		{
			Requires.NotNull(left, "left");
			Requires.NotNull(right, "right");
			if (_frozen)
			{
				return new Node(_key, left, right);
			}
			_left = left;
			_right = right;
			_height = ParentHeight(left, right);
			_count = ParentCount(left, right);
			return this;
		}

		private ImmutableList<T>.Node MutateLeft(ImmutableList<T>.Node left)
		{
			Requires.NotNull(left, "left");
			if (_frozen)
			{
				return new Node(_key, left, _right);
			}
			_left = left;
			_height = ParentHeight(left, _right);
			_count = ParentCount(left, _right);
			return this;
		}

		private ImmutableList<T>.Node MutateRight(ImmutableList<T>.Node right)
		{
			Requires.NotNull(right, "right");
			if (_frozen)
			{
				return new Node(_key, _left, right);
			}
			_right = right;
			_height = ParentHeight(_left, right);
			_count = ParentCount(_left, right);
			return this;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte ParentHeight(ImmutableList<T>.Node left, ImmutableList<T>.Node right)
		{
			return checked((byte)(1 + Math.Max(left._height, right._height)));
		}

		private static int ParentCount(ImmutableList<T>.Node left, ImmutableList<T>.Node right)
		{
			return 1 + left._count + right._count;
		}

		private ImmutableList<T>.Node MutateKey(T key)
		{
			if (_frozen)
			{
				return new Node(key, _left, _right);
			}
			_key = key;
			return this;
		}

		private static ImmutableList<T>.Node CreateRange(IEnumerable<T> keys)
		{
			if (ImmutableList<T>.TryCastToImmutableList(keys, out ImmutableList<T> other))
			{
				return other._root;
			}
			IOrderedCollection<T> orderedCollection = keys.AsOrderedCollection();
			return NodeTreeFromList(orderedCollection, 0, orderedCollection.Count);
		}

		private static ImmutableList<T>.Node CreateLeaf(T key)
		{
			return new Node(key, EmptyNode, EmptyNode);
		}

		private static bool Contains(ImmutableList<T>.Node node, T value, IEqualityComparer<T> equalityComparer)
		{
			if (!node.IsEmpty)
			{
				if (!equalityComparer.Equals(value, node._key) && !Contains(node._left, value, equalityComparer))
				{
					return Contains(node._right, value, equalityComparer);
				}
				return true;
			}
			return false;
		}
	}

	public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

	private readonly ImmutableList<T>.Node _root;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsEmpty => _root.IsEmpty;

	public int Count => _root.Count;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	object ICollection.SyncRoot => this;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection.IsSynchronized => true;

	public T this[int index] => _root.ItemRef(index);

	T IOrderedCollection<T>.this[int index] => this[index];

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

	bool ICollection<T>.IsReadOnly => true;

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

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

	internal Node Root => _root;

	internal ImmutableList()
	{
		_root = Node.EmptyNode;
	}

	private ImmutableList(ImmutableList<T>.Node root)
	{
		Requires.NotNull(root, "root");
		root.Freeze();
		_root = root;
	}

	public ImmutableList<T> Clear()
	{
		return Empty;
	}

	public int BinarySearch(T item)
	{
		return BinarySearch(item, null);
	}

	public int BinarySearch(T item, IComparer<T>? comparer)
	{
		return BinarySearch(0, Count, item, comparer);
	}

	public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
	{
		return _root.BinarySearch(index, count, item, comparer);
	}

	IImmutableList<T> IImmutableList<T>.Clear()
	{
		return Clear();
	}

	public ref readonly T ItemRef(int index)
	{
		return ref _root.ItemRef(index);
	}

	public Builder ToBuilder()
	{
		return new Builder(this);
	}

	public ImmutableList<T> Add(T value)
	{
		Node root = _root.Add(value);
		return Wrap(root);
	}

	public ImmutableList<T> AddRange(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		if (IsEmpty)
		{
			return CreateRange(items);
		}
		Node root = _root.AddRange(items);
		return Wrap(root);
	}

	internal ImmutableList<T> AddRange(ReadOnlySpan<T> items)
	{
		if (IsEmpty)
		{
			if (items.IsEmpty)
			{
				return Empty;
			}
			return new ImmutableList<T>(Node.NodeTreeFromList(items));
		}
		if (items.IsEmpty)
		{
			return this;
		}
		return Wrap(_root.AddRange(items));
	}

	public ImmutableList<T> Insert(int index, T item)
	{
		Requires.Range(index >= 0 && index <= Count, "index");
		return Wrap(_root.Insert(index, item));
	}

	public ImmutableList<T> InsertRange(int index, IEnumerable<T> items)
	{
		Requires.Range(index >= 0 && index <= Count, "index");
		Requires.NotNull(items, "items");
		Node root = _root.InsertRange(index, items);
		return Wrap(root);
	}

	public ImmutableList<T> Remove(T value)
	{
		return Remove(value, EqualityComparer<T>.Default);
	}

	public ImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
	{
		int num = this.IndexOf(value, equalityComparer);
		if (num >= 0)
		{
			return RemoveAt(num);
		}
		return this;
	}

	public ImmutableList<T> RemoveRange(int index, int count)
	{
		Requires.Range(index >= 0 && index <= Count, "index");
		Requires.Range(count >= 0 && index <= Count - count, "count");
		Node node = _root;
		int num = count;
		while (num-- > 0)
		{
			node = node.RemoveAt(index);
		}
		return Wrap(node);
	}

	public ImmutableList<T> RemoveRange(IEnumerable<T> items)
	{
		return RemoveRange(items, EqualityComparer<T>.Default);
	}

	public ImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
	{
		Requires.NotNull(items, "items");
		if (IsEmpty)
		{
			return this;
		}
		Node node = _root;
		foreach (T item in items.GetEnumerableDisposable<T, Enumerator>())
		{
			int num = node.IndexOf(item, equalityComparer);
			if (num >= 0)
			{
				node = node.RemoveAt(num);
			}
		}
		return Wrap(node);
	}

	public ImmutableList<T> RemoveAt(int index)
	{
		Requires.Range(index >= 0 && index < Count, "index");
		Node root = _root.RemoveAt(index);
		return Wrap(root);
	}

	public ImmutableList<T> RemoveAll(Predicate<T> match)
	{
		Requires.NotNull(match, "match");
		return Wrap(_root.RemoveAll(match));
	}

	public ImmutableList<T> SetItem(int index, T value)
	{
		return Wrap(_root.ReplaceAt(index, value));
	}

	public ImmutableList<T> Replace(T oldValue, T newValue)
	{
		return Replace(oldValue, newValue, EqualityComparer<T>.Default);
	}

	public ImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
	{
		int num = this.IndexOf(oldValue, equalityComparer);
		if (num < 0)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CannotFindOldValue, "oldValue");
		}
		return SetItem(num, newValue);
	}

	public ImmutableList<T> Reverse()
	{
		return Wrap(_root.Reverse());
	}

	public ImmutableList<T> Reverse(int index, int count)
	{
		return Wrap(_root.Reverse(index, count));
	}

	public ImmutableList<T> Sort()
	{
		return Wrap(_root.Sort());
	}

	public ImmutableList<T> Sort(Comparison<T> comparison)
	{
		Requires.NotNull(comparison, "comparison");
		return Wrap(_root.Sort(comparison));
	}

	public ImmutableList<T> Sort(IComparer<T>? comparer)
	{
		return Wrap(_root.Sort(comparer));
	}

	public ImmutableList<T> Sort(int index, int count, IComparer<T>? comparer)
	{
		Requires.Range(index >= 0, "index");
		Requires.Range(count >= 0, "count");
		Requires.Range(index + count <= Count, "count");
		return Wrap(_root.Sort(index, count, comparer));
	}

	public void ForEach(Action<T> action)
	{
		Requires.NotNull(action, "action");
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			T current = enumerator.Current;
			action(current);
		}
	}

	public void CopyTo(T[] array)
	{
		_root.CopyTo(array);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_root.CopyTo(array, arrayIndex);
	}

	public void CopyTo(int index, T[] array, int arrayIndex, int count)
	{
		_root.CopyTo(index, array, arrayIndex, count);
	}

	public ImmutableList<T> GetRange(int index, int count)
	{
		Requires.Range(index >= 0, "index");
		Requires.Range(count >= 0, "count");
		Requires.Range(index + count <= Count, "count");
		return Wrap(Node.NodeTreeFromList(this, index, count));
	}

	public ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
	{
		Requires.NotNull(converter, "converter");
		return ImmutableList<TOutput>.WrapNode(_root.ConvertAll(converter));
	}

	public bool Exists(Predicate<T> match)
	{
		return _root.Exists(match);
	}

	public T? Find(Predicate<T> match)
	{
		return _root.Find(match);
	}

	public ImmutableList<T> FindAll(Predicate<T> match)
	{
		return _root.FindAll(match);
	}

	public int FindIndex(Predicate<T> match)
	{
		return _root.FindIndex(match);
	}

	public int FindIndex(int startIndex, Predicate<T> match)
	{
		return _root.FindIndex(startIndex, match);
	}

	public int FindIndex(int startIndex, int count, Predicate<T> match)
	{
		return _root.FindIndex(startIndex, count, match);
	}

	public T? FindLast(Predicate<T> match)
	{
		return _root.FindLast(match);
	}

	public int FindLastIndex(Predicate<T> match)
	{
		return _root.FindLastIndex(match);
	}

	public int FindLastIndex(int startIndex, Predicate<T> match)
	{
		return _root.FindLastIndex(startIndex, match);
	}

	public int FindLastIndex(int startIndex, int count, Predicate<T> match)
	{
		return _root.FindLastIndex(startIndex, count, match);
	}

	public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
	{
		return _root.IndexOf(item, index, count, equalityComparer);
	}

	public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
	{
		return _root.LastIndexOf(item, index, count, equalityComparer);
	}

	public bool TrueForAll(Predicate<T> match)
	{
		return _root.TrueForAll(match);
	}

	public bool Contains(T value)
	{
		return _root.Contains(value, EqualityComparer<T>.Default);
	}

	public int IndexOf(T value)
	{
		return this.IndexOf(value, EqualityComparer<T>.Default);
	}

	IImmutableList<T> IImmutableList<T>.Add(T value)
	{
		return Add(value);
	}

	IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
	{
		return AddRange(items);
	}

	IImmutableList<T> IImmutableList<T>.Insert(int index, T item)
	{
		return Insert(index, item);
	}

	IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
	{
		return InsertRange(index, items);
	}

	IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
	{
		return Remove(value, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
	{
		return RemoveAll(match);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
	{
		return RemoveRange(items, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
	{
		return RemoveRange(index, count);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
	{
		return RemoveAt(index);
	}

	IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
	{
		return SetItem(index, value);
	}

	IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
	{
		return Replace(oldValue, newValue, equalityComparer);
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

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
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

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		_root.CopyTo(array, arrayIndex);
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
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
		if (!IsCompatibleObject(value))
		{
			return -1;
		}
		return IndexOf((T)value);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_root);
	}

	private static ImmutableList<T> WrapNode(ImmutableList<T>.Node root)
	{
		if (!root.IsEmpty)
		{
			return new ImmutableList<T>(root);
		}
		return Empty;
	}

	private static bool TryCastToImmutableList(IEnumerable<T> sequence, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullWhen(true)] out ImmutableList<T> other)
	{
		other = sequence as ImmutableList<T>;
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

	private ImmutableList<T> Wrap(ImmutableList<T>.Node root)
	{
		if (root != _root)
		{
			if (!root.IsEmpty)
			{
				return new ImmutableList<T>(root);
			}
			return Clear();
		}
		return this;
	}

	private static ImmutableList<T> CreateRange(IEnumerable<T> items)
	{
		if (TryCastToImmutableList(items, out ImmutableList<T> other))
		{
			return other;
		}
		IOrderedCollection<T> orderedCollection = items.AsOrderedCollection();
		if (orderedCollection.Count == 0)
		{
			return Empty;
		}
		return new ImmutableList<T>(Node.NodeTreeFromList(orderedCollection, 0, orderedCollection.Count));
	}
}
