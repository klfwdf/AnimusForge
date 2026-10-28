using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace System.Collections.Immutable;

internal static class ImmutableSortedDictionary
{
	public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty;
	}

	public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(IComparer<TKey>? keyComparer) where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer);
	}

	public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);
	}

	public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty.AddRange(items);
	}

	public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IComparer<TKey>? keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer).AddRange(items);
	}

	public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
	{
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(items);
	}

	public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>() where TKey : notnull
	{
		return Create<TKey, TValue>().ToBuilder();
	}

	public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IComparer<TKey>? keyComparer) where TKey : notnull
	{
		return Create<TKey, TValue>(keyComparer).ToBuilder();
	}

	public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
	{
		return Create(keyComparer, valueComparer).ToBuilder();
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
	{
		Requires.NotNull(source, "source");
		Requires.NotNull(keySelector, "keySelector");
		Requires.NotNull(elementSelector, "elementSelector");
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(source.Select<TSource, KeyValuePair<TKey, TValue>>((TSource element) => new KeyValuePair<TKey, TValue>(keySelector(element), elementSelector(element))));
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue>.Builder builder) where TKey : notnull
	{
		Requires.NotNull(builder, "builder");
		return builder.ToImmutable();
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IComparer<TKey>? keyComparer) where TKey : notnull
	{
		return source.ToImmutableSortedDictionary(keySelector, elementSelector, keyComparer, null);
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector) where TKey : notnull
	{
		return source.ToImmutableSortedDictionary(keySelector, elementSelector, null, null);
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
	{
		Requires.NotNull(source, "source");
		if (source is ImmutableSortedDictionary<TKey, TValue> immutableSortedDictionary)
		{
			return immutableSortedDictionary.WithComparers(keyComparer, valueComparer);
		}
		return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(source);
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IComparer<TKey>? keyComparer) where TKey : notnull
	{
		return source.ToImmutableSortedDictionary(keyComparer, null);
	}

	public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
	{
		return source.ToImmutableSortedDictionary(null, null);
	}
}
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ImmutableDictionaryDebuggerProxy<, >))]
internal sealed class ImmutableSortedDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, ISortKeyCollection<TKey>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection where TKey : notnull
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(IDictionaryDebugView<, >))]
	public sealed class Builder : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection
	{
		private ImmutableSortedDictionary<TKey, TValue>.Node _root = Node.EmptyNode;

		private IComparer<TKey> _keyComparer = Comparer<TKey>.Default;

		private IEqualityComparer<TValue> _valueComparer = EqualityComparer<TValue>.Default;

		private int _count;

		private ImmutableSortedDictionary<TKey, TValue> _immutable;

		private int _version;

		private object _syncRoot;

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => Root.Keys.ToArray(Count);

		public IEnumerable<TKey> Keys => Root.Keys;

		ICollection<TValue> IDictionary<TKey, TValue>.Values => Root.Values.ToArray(Count);

		public IEnumerable<TValue> Values => Root.Values;

		public int Count => _count;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

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

		public TValue this[TKey key]
		{
			get
			{
				if (!TryGetValue(key, out var value))
				{
					ThrowHelper.ThrowKeyNotFoundException(key);
				}
				return value;
			}
			set
			{
				Root = _root.SetItem(key, value, _keyComparer, _valueComparer, out var replacedExistingValue, out var mutated);
				if (mutated && !replacedExistingValue)
				{
					_count++;
				}
			}
		}

		bool IDictionary.IsFixedSize => false;

		bool IDictionary.IsReadOnly => false;

		ICollection IDictionary.Keys => Keys.ToArray(Count);

		ICollection IDictionary.Values => Values.ToArray(Count);

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

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized => false;

		public IComparer<TKey> KeyComparer
		{
			get
			{
				return _keyComparer;
			}
			set
			{
				Requires.NotNull(value, "value");
				if (value == _keyComparer)
				{
					return;
				}
				Node node = Node.EmptyNode;
				int num = 0;
				using (Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<TKey, TValue> current = enumerator.Current;
						node = node.Add(current.Key, current.Value, value, _valueComparer, out var mutated);
						if (mutated)
						{
							num++;
						}
					}
				}
				_keyComparer = value;
				Root = node;
				_count = num;
			}
		}

		public IEqualityComparer<TValue> ValueComparer
		{
			get
			{
				return _valueComparer;
			}
			set
			{
				Requires.NotNull(value, "value");
				if (value != _valueComparer)
				{
					_valueComparer = value;
					_immutable = null;
				}
			}
		}

		object? IDictionary.this[object key]
		{
			get
			{
				return this[(TKey)key];
			}
			set
			{
				this[(TKey)key] = (TValue)value;
			}
		}

		internal Builder(ImmutableSortedDictionary<TKey, TValue> map)
		{
			Requires.NotNull(map, "map");
			_root = map._root;
			_keyComparer = map.KeyComparer;
			_valueComparer = map.ValueComparer;
			_count = map.Count;
			_immutable = map;
		}

		public ref readonly TValue ValueRef(TKey key)
		{
			Requires.NotNullAllowStructs(key, "key");
			return ref _root.ValueRef(key, _keyComparer);
		}

		void IDictionary.Add(object key, object value)
		{
			Add((TKey)key, (TValue)value);
		}

		bool IDictionary.Contains(object key)
		{
			return ContainsKey((TKey)key);
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return new DictionaryEnumerator<TKey, TValue>(GetEnumerator());
		}

		void IDictionary.Remove(object key)
		{
			Remove((TKey)key);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			Root.CopyTo(array, index, Count);
		}

		public void Add(TKey key, TValue value)
		{
			Root = Root.Add(key, value, _keyComparer, _valueComparer, out var mutated);
			if (mutated)
			{
				_count++;
			}
		}

		public bool ContainsKey(TKey key)
		{
			return Root.ContainsKey(key, _keyComparer);
		}

		public bool Remove(TKey key)
		{
			Root = Root.Remove(key, _keyComparer, out var mutated);
			if (mutated)
			{
				_count--;
			}
			return mutated;
		}

		public bool TryGetValue(TKey key, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out TValue value)
		{
			return Root.TryGetValue(key, _keyComparer, out value);
		}

		public bool TryGetKey(TKey equalKey, out TKey actualKey)
		{
			Requires.NotNullAllowStructs(equalKey, "equalKey");
			return Root.TryGetKey(equalKey, _keyComparer, out actualKey);
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			Root = Node.EmptyNode;
			_count = 0;
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return Root.Contains(item, _keyComparer, _valueComparer);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			Root.CopyTo(array, arrayIndex, Count);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (Contains(item))
			{
				return Remove(item.Key);
			}
			return false;
		}

		public ImmutableSortedDictionary<TKey, TValue>.Enumerator GetEnumerator()
		{
			return Root.GetEnumerator(this);
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool ContainsValue(TValue value)
		{
			return _root.ContainsValue(value, _valueComparer);
		}

		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			Requires.NotNull(items, "items");
			foreach (KeyValuePair<TKey, TValue> item in items)
			{
				Add(item);
			}
		}

		public void RemoveRange(IEnumerable<TKey> keys)
		{
			Requires.NotNull(keys, "keys");
			foreach (TKey key in keys)
			{
				Remove(key);
			}
		}

		public TValue? GetValueOrDefault(TKey key)
		{
			return GetValueOrDefault(key, default(TValue));
		}

		public TValue GetValueOrDefault(TKey key, TValue defaultValue)
		{
			Requires.NotNullAllowStructs(key, "key");
			if (TryGetValue(key, out var value))
			{
				return value;
			}
			return defaultValue;
		}

		public ImmutableSortedDictionary<TKey, TValue> ToImmutable()
		{
			return _immutable ?? (_immutable = ImmutableSortedDictionary<TKey, TValue>.Wrap(Root, _count, _keyComparer, _valueComparer));
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, ISecurePooledObjectUser
	{
		private readonly Builder _builder;

		private readonly int _poolUserId;

		private Node _root;

		private SecurePooledObject<Stack<RefAsValueType<Node>>> _stack;

		private Node _current;

		private int _enumeratingBuilderVersion;

		public KeyValuePair<TKey, TValue> Current
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

		int ISecurePooledObjectUser.PoolUserId => _poolUserId;

		object IEnumerator.Current => Current;

		internal Enumerator(Node root, Builder? builder = null)
		{
			Requires.NotNull(root, "root");
			_root = root;
			_builder = builder;
			_current = null;
			_enumeratingBuilderVersion = builder?.Version ?? (-1);
			_poolUserId = SecureObjectPool.NewId();
			_stack = null;
			if (!_root.IsEmpty)
			{
				if (!SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.TryTake(this, out _stack))
				{
					_stack = SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
				}
				PushLeft(_root);
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
				if (stack.Count > 0)
				{
					PushLeft((_current = stack.Pop().Value).Right);
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
			_current = null;
			if (_stack != null)
			{
				_stack.Use(ref this).ClearFastWhenEmpty();
				PushLeft(_root);
			}
		}

		internal void ThrowIfDisposed()
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

		private void PushLeft(Node node)
		{
			Requires.NotNull(node, "node");
			Stack<RefAsValueType<Node>> stack = _stack.Use(ref this);
			while (!node.IsEmpty)
			{
				stack.Push(new RefAsValueType<Node>(node));
				node = node.Left;
			}
		}
	}

	[DebuggerDisplay("{_key} = {_value}")]
	internal sealed class Node : IBinaryTree<KeyValuePair<TKey, TValue>>, IBinaryTree, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
	{
		internal static readonly Node EmptyNode = new Node();

		private readonly TKey _key;

		private readonly TValue _value;

		private bool _frozen;

		private byte _height;

		private ImmutableSortedDictionary<TKey, TValue>.Node _left;

		private ImmutableSortedDictionary<TKey, TValue>.Node _right;

		public bool IsEmpty => _left == null;

		IBinaryTree<KeyValuePair<TKey, TValue>>? IBinaryTree<KeyValuePair<TKey, TValue>>.Left => _left;

		IBinaryTree<KeyValuePair<TKey, TValue>>? IBinaryTree<KeyValuePair<TKey, TValue>>.Right => _right;

		public int Height => _height;

		public Node? Left => _left;

		IBinaryTree? IBinaryTree.Left => _left;

		public Node? Right => _right;

		IBinaryTree? IBinaryTree.Right => _right;

		public KeyValuePair<TKey, TValue> Value => new KeyValuePair<TKey, TValue>(_key, _value);

		int IBinaryTree.Count
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		internal IEnumerable<TKey> Keys => this.Select((KeyValuePair<TKey, TValue> p) => p.Key);

		internal IEnumerable<TValue> Values => this.Select((KeyValuePair<TKey, TValue> p) => p.Value);

		private Node()
		{
			_frozen = true;
		}

		private Node(TKey key, TValue value, ImmutableSortedDictionary<TKey, TValue>.Node left, ImmutableSortedDictionary<TKey, TValue>.Node right, bool frozen = false)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(left, "left");
			Requires.NotNull(right, "right");
			_key = key;
			_value = value;
			_left = left;
			_right = right;
			_height = checked((byte)(1 + Math.Max(left._height, right._height)));
			_frozen = frozen;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal Enumerator GetEnumerator(Builder builder)
		{
			return new Enumerator(this, builder);
		}

		internal void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, int dictionarySize)
		{
			Requires.NotNull(array, "array");
			Requires.Range(arrayIndex >= 0, "arrayIndex");
			Requires.Range(array.Length >= arrayIndex + dictionarySize, "arrayIndex");
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current = enumerator.Current;
				array[arrayIndex++] = current;
			}
		}

		internal void CopyTo(Array array, int arrayIndex, int dictionarySize)
		{
			Requires.NotNull(array, "array");
			Requires.Range(arrayIndex >= 0, "arrayIndex");
			Requires.Range(array.Length >= arrayIndex + dictionarySize, "arrayIndex");
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current = enumerator.Current;
				array.SetValue(new DictionaryEntry(current.Key, current.Value), arrayIndex++);
			}
		}

		internal static Node NodeTreeFromSortedDictionary(SortedDictionary<TKey, TValue> dictionary)
		{
			Requires.NotNull(dictionary, "dictionary");
			IOrderedCollection<KeyValuePair<TKey, TValue>> orderedCollection = dictionary.AsOrderedCollection();
			return NodeTreeFromList(orderedCollection, 0, orderedCollection.Count);
		}

		internal Node Add(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool mutated)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			Requires.NotNull(valueComparer, "valueComparer");
			bool replacedExistingValue;
			return SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue: false, out replacedExistingValue, out mutated);
		}

		internal Node SetItem(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool replacedExistingValue, out bool mutated)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			Requires.NotNull(valueComparer, "valueComparer");
			return SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue: true, out replacedExistingValue, out mutated);
		}

		internal Node Remove(TKey key, IComparer<TKey> keyComparer, out bool mutated)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			return RemoveRecursive(key, keyComparer, out mutated);
		}

		internal ref readonly TValue ValueRef(TKey key, IComparer<TKey> keyComparer)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			Node node = Search(key, keyComparer);
			if (node.IsEmpty)
			{
				ThrowHelper.ThrowKeyNotFoundException(key);
			}
			return ref node._value;
		}

		internal bool TryGetValue(TKey key, IComparer<TKey> keyComparer, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out TValue value)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			Node node = Search(key, keyComparer);
			if (node.IsEmpty)
			{
				value = default(TValue);
				return false;
			}
			value = node._value;
			return true;
		}

		internal bool TryGetKey(TKey equalKey, IComparer<TKey> keyComparer, out TKey actualKey)
		{
			Requires.NotNullAllowStructs(equalKey, "equalKey");
			Requires.NotNull(keyComparer, "keyComparer");
			Node node = Search(equalKey, keyComparer);
			if (node.IsEmpty)
			{
				actualKey = equalKey;
				return false;
			}
			actualKey = node._key;
			return true;
		}

		internal bool ContainsKey(TKey key, IComparer<TKey> keyComparer)
		{
			Requires.NotNullAllowStructs(key, "key");
			Requires.NotNull(keyComparer, "keyComparer");
			return !Search(key, keyComparer).IsEmpty;
		}

		internal bool ContainsValue(TValue value, IEqualityComparer<TValue> valueComparer)
		{
			Requires.NotNull(valueComparer, "valueComparer");
			using (Enumerator enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (valueComparer.Equals(value, enumerator.Current.Value))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal bool Contains(KeyValuePair<TKey, TValue> pair, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		{
			Requires.NotNullAllowStructs(pair.Key, "Key");
			Requires.NotNull(keyComparer, "keyComparer");
			Requires.NotNull(valueComparer, "valueComparer");
			Node node = Search(pair.Key, keyComparer);
			if (node.IsEmpty)
			{
				return false;
			}
			return valueComparer.Equals(node._value, pair.Value);
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

		private static ImmutableSortedDictionary<TKey, TValue>.Node RotateLeft(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._right.IsEmpty)
			{
				return tree;
			}
			Node right = tree._right;
			return right.Mutate(tree.Mutate(null, right._left));
		}

		private static ImmutableSortedDictionary<TKey, TValue>.Node RotateRight(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._left.IsEmpty)
			{
				return tree;
			}
			Node left = tree._left;
			return left.Mutate(null, tree.Mutate(left._right));
		}

		private static ImmutableSortedDictionary<TKey, TValue>.Node DoubleLeft(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._right.IsEmpty)
			{
				return tree;
			}
			return RotateLeft(tree.Mutate(null, RotateRight(tree._right)));
		}

		private static ImmutableSortedDictionary<TKey, TValue>.Node DoubleRight(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			if (tree._left.IsEmpty)
			{
				return tree;
			}
			return RotateRight(tree.Mutate(RotateLeft(tree._left)));
		}

		private static int Balance(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return tree._right._height - tree._left._height;
		}

		private static bool IsRightHeavy(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return Balance(tree) >= 2;
		}

		private static bool IsLeftHeavy(ImmutableSortedDictionary<TKey, TValue>.Node tree)
		{
			Requires.NotNull(tree, "tree");
			return Balance(tree) <= -2;
		}

		private static ImmutableSortedDictionary<TKey, TValue>.Node MakeBalanced(ImmutableSortedDictionary<TKey, TValue>.Node tree)
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

		private static ImmutableSortedDictionary<TKey, TValue>.Node NodeTreeFromList(IOrderedCollection<KeyValuePair<TKey, TValue>> items, int start, int length)
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
			KeyValuePair<TKey, TValue> keyValuePair = items[start + num2];
			return new Node(keyValuePair.Key, keyValuePair.Value, left, right, frozen: true);
		}

		private ImmutableSortedDictionary<TKey, TValue>.Node SetOrAdd(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue, out bool replacedExistingValue, out bool mutated)
		{
			replacedExistingValue = false;
			if (IsEmpty)
			{
				mutated = true;
				return new Node(key, value, this, this);
			}
			Node node = this;
			int num = keyComparer.Compare(key, _key);
			if (num > 0)
			{
				Node right = _right.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
				if (mutated)
				{
					node = Mutate(null, right);
				}
			}
			else if (num < 0)
			{
				Node left = _left.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
				if (mutated)
				{
					node = Mutate(left);
				}
			}
			else
			{
				if (valueComparer.Equals(_value, value))
				{
					mutated = false;
					return this;
				}
				if (!overwriteExistingValue)
				{
					throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Format(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.DuplicateKey, key));
				}
				mutated = true;
				replacedExistingValue = true;
				node = new Node(key, value, _left, _right);
			}
			if (!mutated)
			{
				return node;
			}
			return MakeBalanced(node);
		}

		private ImmutableSortedDictionary<TKey, TValue>.Node RemoveRecursive(TKey key, IComparer<TKey> keyComparer, out bool mutated)
		{
			if (IsEmpty)
			{
				mutated = false;
				return this;
			}
			Node node = this;
			int num = keyComparer.Compare(key, _key);
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
					Node right = _right.Remove(node2._key, keyComparer, out var _);
					node = node2.Mutate(_left, right);
				}
			}
			else if (num < 0)
			{
				Node left = _left.Remove(key, keyComparer, out mutated);
				if (mutated)
				{
					node = Mutate(left);
				}
			}
			else
			{
				Node right2 = _right.Remove(key, keyComparer, out mutated);
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

		private ImmutableSortedDictionary<TKey, TValue>.Node Mutate(ImmutableSortedDictionary<TKey, TValue>.Node left = null, ImmutableSortedDictionary<TKey, TValue>.Node right = null)
		{
			if (_frozen)
			{
				return new Node(_key, _value, left ?? _left, right ?? _right);
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
			return this;
		}

		private ImmutableSortedDictionary<TKey, TValue>.Node Search(TKey key, IComparer<TKey> keyComparer)
		{
			if (IsEmpty)
			{
				return this;
			}
			int num = keyComparer.Compare(key, _key);
			if (num == 0)
			{
				return this;
			}
			if (num > 0)
			{
				return _right.Search(key, keyComparer);
			}
			return _left.Search(key, keyComparer);
		}
	}

	public static readonly ImmutableSortedDictionary<TKey, TValue> Empty = new ImmutableSortedDictionary<TKey, TValue>();

	private readonly ImmutableSortedDictionary<TKey, TValue>.Node _root;

	private readonly int _count;

	private readonly IComparer<TKey> _keyComparer;

	private readonly IEqualityComparer<TValue> _valueComparer;

	public IEqualityComparer<TValue> ValueComparer => _valueComparer;

	public bool IsEmpty => _root.IsEmpty;

	public int Count => _count;

	public IEnumerable<TKey> Keys => _root.Keys;

	public IEnumerable<TValue> Values => _root.Values;

	ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeysCollectionAccessor<TKey, TValue>(this);

	ICollection<TValue> IDictionary<TKey, TValue>.Values => new ValuesCollectionAccessor<TKey, TValue>(this);

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

	public IComparer<TKey> KeyComparer => _keyComparer;

	internal Node Root => _root;

	public TValue this[TKey key]
	{
		get
		{
			Requires.NotNullAllowStructs(key, "key");
			if (!TryGetValue(key, out var value))
			{
				ThrowHelper.ThrowKeyNotFoundException(key);
			}
			return value;
		}
	}

	TValue IDictionary<TKey, TValue>.this[TKey key]
	{
		get
		{
			return this[key];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IDictionary.IsFixedSize => true;

	bool IDictionary.IsReadOnly => true;

	ICollection IDictionary.Keys => new KeysCollectionAccessor<TKey, TValue>(this);

	ICollection IDictionary.Values => new ValuesCollectionAccessor<TKey, TValue>(this);

	object? IDictionary.this[object key]
	{
		get
		{
			return this[(TKey)key];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	object ICollection.SyncRoot => this;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection.IsSynchronized => true;

	internal ImmutableSortedDictionary(IComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null)
	{
		_keyComparer = keyComparer ?? Comparer<TKey>.Default;
		_valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
		_root = Node.EmptyNode;
	}

	private ImmutableSortedDictionary(ImmutableSortedDictionary<TKey, TValue>.Node root, int count, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
	{
		Requires.NotNull(root, "root");
		Requires.Range(count >= 0, "count");
		Requires.NotNull(keyComparer, "keyComparer");
		Requires.NotNull(valueComparer, "valueComparer");
		root.Freeze();
		_root = root;
		_count = count;
		_keyComparer = keyComparer;
		_valueComparer = valueComparer;
	}

	public ImmutableSortedDictionary<TKey, TValue> Clear()
	{
		if (!_root.IsEmpty)
		{
			return Empty.WithComparers(_keyComparer, _valueComparer);
		}
		return this;
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
	{
		return Clear();
	}

	public ref readonly TValue ValueRef(TKey key)
	{
		Requires.NotNullAllowStructs(key, "key");
		return ref _root.ValueRef(key, _keyComparer);
	}

	public Builder ToBuilder()
	{
		return new Builder(this);
	}

	public ImmutableSortedDictionary<TKey, TValue> Add(TKey key, TValue value)
	{
		Requires.NotNullAllowStructs(key, "key");
		Node root = _root.Add(key, value, _keyComparer, _valueComparer, out var _);
		return Wrap(root, _count + 1);
	}

	public ImmutableSortedDictionary<TKey, TValue> SetItem(TKey key, TValue value)
	{
		Requires.NotNullAllowStructs(key, "key");
		Node root = _root.SetItem(key, value, _keyComparer, _valueComparer, out var replacedExistingValue, out var _);
		return Wrap(root, replacedExistingValue ? _count : (_count + 1));
	}

	public ImmutableSortedDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
	{
		Requires.NotNull(items, "items");
		return AddRange(items, overwriteOnCollision: true, avoidToSortedMap: false);
	}

	public ImmutableSortedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
	{
		Requires.NotNull(items, "items");
		return AddRange(items, overwriteOnCollision: false, avoidToSortedMap: false);
	}

	public ImmutableSortedDictionary<TKey, TValue> Remove(TKey value)
	{
		Requires.NotNullAllowStructs(value, "value");
		Node root = _root.Remove(value, _keyComparer, out var _);
		return Wrap(root, _count - 1);
	}

	public ImmutableSortedDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
	{
		Requires.NotNull(keys, "keys");
		Node node = _root;
		int num = _count;
		foreach (TKey key in keys)
		{
			Node node2 = node.Remove(key, _keyComparer, out var mutated);
			if (mutated)
			{
				node = node2;
				num--;
			}
		}
		return Wrap(node, num);
	}

	public ImmutableSortedDictionary<TKey, TValue> WithComparers(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
	{
		if (keyComparer == null)
		{
			keyComparer = Comparer<TKey>.Default;
		}
		if (valueComparer == null)
		{
			valueComparer = EqualityComparer<TValue>.Default;
		}
		if (keyComparer == _keyComparer)
		{
			if (valueComparer == _valueComparer)
			{
				return this;
			}
			return new ImmutableSortedDictionary<TKey, TValue>(_root, _count, _keyComparer, valueComparer);
		}
		return new ImmutableSortedDictionary<TKey, TValue>(Node.EmptyNode, 0, keyComparer, valueComparer).AddRange(this, overwriteOnCollision: false, avoidToSortedMap: true);
	}

	public ImmutableSortedDictionary<TKey, TValue> WithComparers(IComparer<TKey>? keyComparer)
	{
		return WithComparers(keyComparer, _valueComparer);
	}

	public bool ContainsValue(TValue value)
	{
		return _root.ContainsValue(value, _valueComparer);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		return Add(key, value);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
	{
		return SetItem(key, value);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
	{
		return SetItems(items);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
	{
		return AddRange(pairs);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
	{
		return RemoveRange(keys);
	}

	IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key)
	{
		return Remove(key);
	}

	public bool ContainsKey(TKey key)
	{
		Requires.NotNullAllowStructs(key, "key");
		return _root.ContainsKey(key, _keyComparer);
	}

	public bool Contains(KeyValuePair<TKey, TValue> pair)
	{
		return _root.Contains(pair, _keyComparer, _valueComparer);
	}

	public bool TryGetValue(TKey key, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out TValue value)
	{
		Requires.NotNullAllowStructs(key, "key");
		return _root.TryGetValue(key, _keyComparer, out value);
	}

	public bool TryGetKey(TKey equalKey, out TKey actualKey)
	{
		Requires.NotNullAllowStructs(equalKey, "equalKey");
		return _root.TryGetKey(equalKey, _keyComparer, out actualKey);
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		throw new NotSupportedException();
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		throw new NotSupportedException();
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException();
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException();
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		Requires.NotNull(array, "array");
		Requires.Range(arrayIndex >= 0, "arrayIndex");
		Requires.Range(array.Length >= arrayIndex + Count, "arrayIndex");
		using Enumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			KeyValuePair<TKey, TValue> current = enumerator.Current;
			array[arrayIndex++] = current;
		}
	}

	void IDictionary.Add(object key, object value)
	{
		throw new NotSupportedException();
	}

	bool IDictionary.Contains(object key)
	{
		return ContainsKey((TKey)key);
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new DictionaryEnumerator<TKey, TValue>(GetEnumerator());
	}

	void IDictionary.Remove(object key)
	{
		throw new NotSupportedException();
	}

	void IDictionary.Clear()
	{
		throw new NotSupportedException();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		_root.CopyTo(array, index, Count);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		if (!IsEmpty)
		{
			return GetEnumerator();
		}
		return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public Enumerator GetEnumerator()
	{
		return _root.GetEnumerator();
	}

	private static ImmutableSortedDictionary<TKey, TValue> Wrap(ImmutableSortedDictionary<TKey, TValue>.Node root, int count, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
	{
		if (!root.IsEmpty)
		{
			return new ImmutableSortedDictionary<TKey, TValue>(root, count, keyComparer, valueComparer);
		}
		return Empty.WithComparers(keyComparer, valueComparer);
	}

	private static bool TryCastToImmutableMap(IEnumerable<KeyValuePair<TKey, TValue>> sequence, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullWhen(true)] out ImmutableSortedDictionary<TKey, TValue> other)
	{
		other = sequence as ImmutableSortedDictionary<TKey, TValue>;
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

	private ImmutableSortedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision, bool avoidToSortedMap)
	{
		Requires.NotNull(items, "items");
		if (IsEmpty && !avoidToSortedMap)
		{
			return FillFromEmpty(items, overwriteOnCollision);
		}
		Node node = _root;
		int num = _count;
		foreach (KeyValuePair<TKey, TValue> item in items)
		{
			bool replacedExistingValue = false;
			Node node2 = (overwriteOnCollision ? node.SetItem(item.Key, item.Value, _keyComparer, _valueComparer, out replacedExistingValue, out var mutated) : node.Add(item.Key, item.Value, _keyComparer, _valueComparer, out mutated));
			if (mutated)
			{
				node = node2;
				if (!replacedExistingValue)
				{
					num++;
				}
			}
		}
		return Wrap(node, num);
	}

	private ImmutableSortedDictionary<TKey, TValue> Wrap(ImmutableSortedDictionary<TKey, TValue>.Node root, int adjustedCountIfDifferentRoot)
	{
		if (_root != root)
		{
			if (!root.IsEmpty)
			{
				return new ImmutableSortedDictionary<TKey, TValue>(root, adjustedCountIfDifferentRoot, _keyComparer, _valueComparer);
			}
			return Clear();
		}
		return this;
	}

	private ImmutableSortedDictionary<TKey, TValue> FillFromEmpty(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision)
	{
		Requires.NotNull(items, "items");
		if (TryCastToImmutableMap(items, out ImmutableSortedDictionary<TKey, TValue> other))
		{
			return other.WithComparers(KeyComparer, ValueComparer);
		}
		SortedDictionary<TKey, TValue> sortedDictionary;
		if (items is IDictionary<TKey, TValue> dictionary)
		{
			sortedDictionary = new SortedDictionary<TKey, TValue>(dictionary, KeyComparer);
		}
		else
		{
			sortedDictionary = new SortedDictionary<TKey, TValue>(KeyComparer);
			foreach (KeyValuePair<TKey, TValue> item in items)
			{
				TValue value;
				if (overwriteOnCollision)
				{
					sortedDictionary[item.Key] = item.Value;
				}
				else if (sortedDictionary.TryGetValue(item.Key, out value))
				{
					if (!_valueComparer.Equals(value, item.Value))
					{
						throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Format(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.DuplicateKey, item.Key));
					}
				}
				else
				{
					sortedDictionary.Add(item.Key, item.Value);
				}
			}
		}
		if (sortedDictionary.Count == 0)
		{
			return this;
		}
		return new ImmutableSortedDictionary<TKey, TValue>(Node.NodeTreeFromSortedDictionary(sortedDictionary), sortedDictionary.Count, KeyComparer, ValueComparer);
	}
}
