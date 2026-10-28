using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Collections.Immutable;

internal static class ImmutableArray
{
	internal static readonly byte[] TwoElementArray = new byte[2];

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>()
	{
		return System.Collections.Immutable.ImmutableArray<T>.Empty;
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item)
	{
		return new System.Collections.Immutable.ImmutableArray<T>(new T[1] { item });
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2)
	{
		return new System.Collections.Immutable.ImmutableArray<T>(new T[2] { item1, item2 });
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3)
	{
		return new System.Collections.Immutable.ImmutableArray<T>(new T[3] { item1, item2, item3 });
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3, T item4)
	{
		return new System.Collections.Immutable.ImmutableArray<T>(new T[4] { item1, item2, item3, item4 });
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(params ReadOnlySpan<T> items)
	{
		if (items.IsEmpty)
		{
			return System.Collections.Immutable.ImmutableArray<T>.Empty;
		}
		return new System.Collections.Immutable.ImmutableArray<T>(items.ToArray());
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(Span<T> items)
	{
		return Create((ReadOnlySpan<T>)items);
	}

	public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this ReadOnlySpan<T> items)
	{
		return Create(items);
	}

	public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this Span<T> items)
	{
		return Create((ReadOnlySpan<T>)items);
	}

	public static System.Collections.Immutable.ImmutableArray<T> CreateRange<T>(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		if (items is IImmutableArray immutableArray)
		{
			return new System.Collections.Immutable.ImmutableArray<T>((T[])(immutableArray.Array ?? throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidOperationOnDefaultArray)));
		}
		if (items.TryGetCount(out var count))
		{
			return new System.Collections.Immutable.ImmutableArray<T>(items.ToArray(count));
		}
		return new System.Collections.Immutable.ImmutableArray<T>(items.ToArray());
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(params T[]? items)
	{
		if (items == null || items.Length == 0)
		{
			return System.Collections.Immutable.ImmutableArray<T>.Empty;
		}
		T[] array = new T[items.Length];
		Array.Copy(items, array, items.Length);
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T[] items, int start, int length)
	{
		Requires.NotNull(items, "items");
		Requires.Range(start >= 0 && start <= items.Length, "start");
		Requires.Range(length >= 0 && start + length <= items.Length, "length");
		if (length == 0)
		{
			return Create<T>();
		}
		T[] array = new T[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = items[start + i];
		}
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.Collections.Immutable.ImmutableArray<T> items, int start, int length)
	{
		Requires.Range(start >= 0 && start <= items.Length, "start");
		Requires.Range(length >= 0 && start + length <= items.Length, "length");
		if (length == 0)
		{
			return Create<T>();
		}
		if (start == 0 && length == items.Length)
		{
			return items;
		}
		T[] array = new T[length];
		Array.Copy(items.array, start, array, 0, length);
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, Func<TSource, TResult> selector)
	{
		Requires.NotNull(selector, "selector");
		int length = items.Length;
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i]);
		}
		return new System.Collections.Immutable.ImmutableArray<TResult>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, int start, int length, Func<TSource, TResult> selector)
	{
		int length2 = items.Length;
		Requires.Range(start >= 0 && start <= length2, "start");
		Requires.Range(length >= 0 && start + length <= length2, "length");
		Requires.NotNull(selector, "selector");
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i + start]);
		}
		return new System.Collections.Immutable.ImmutableArray<TResult>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, Func<TSource, TArg, TResult> selector, TArg arg)
	{
		Requires.NotNull(selector, "selector");
		int length = items.Length;
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i], arg);
		}
		return new System.Collections.Immutable.ImmutableArray<TResult>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, int start, int length, Func<TSource, TArg, TResult> selector, TArg arg)
	{
		int length2 = items.Length;
		Requires.Range(start >= 0 && start <= length2, "start");
		Requires.Range(length >= 0 && start + length <= length2, "length");
		Requires.NotNull(selector, "selector");
		if (length == 0)
		{
			return Create<TResult>();
		}
		TResult[] array = new TResult[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = selector(items[i + start], arg);
		}
		return new System.Collections.Immutable.ImmutableArray<TResult>(array);
	}

	public static System.Collections.Immutable.ImmutableArray<T>.Builder CreateBuilder<T>()
	{
		return Create<T>().ToBuilder();
	}

	public static System.Collections.Immutable.ImmutableArray<T>.Builder CreateBuilder<T>(int initialCapacity)
	{
		return new System.Collections.Immutable.ImmutableArray<T>.Builder(initialCapacity);
	}

	public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items)
	{
		if (items is System.Collections.Immutable.ImmutableArray<TSource>)
		{
			return (System.Collections.Immutable.ImmutableArray<TSource>)(object)items;
		}
		return CreateRange(items);
	}

	public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this System.Collections.Immutable.ImmutableArray<TSource>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		return builder.ToImmutable();
	}

	public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value)
	{
		return Array.BinarySearch<T>(array.array, value);
	}

	public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value, IComparer<T>? comparer)
	{
		return Array.BinarySearch(array.array, value, comparer);
	}

	public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, int index, int length, T value)
	{
		return Array.BinarySearch<T>(array.array, index, length, value);
	}

	public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, int index, int length, T value, IComparer<T>? comparer)
	{
		return Array.BinarySearch(array.array, index, length, value, comparer);
	}
}
[CollectionBuilder(typeof(System.Collections.Immutable.ImmutableArray), "Create")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
internal readonly struct ImmutableArray<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IList<T>, ICollection<T>, IEquatable<System.Collections.Immutable.ImmutableArray<T>>, IList, ICollection, IImmutableArray, IStructuralComparable, IStructuralEquatable, IImmutableList<T>
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(ImmutableArrayBuilderDebuggerProxy<>))]
	public sealed class Builder : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		private T[] _elements;

		private int _count;

		public int Capacity
		{
			get
			{
				return _elements.Length;
			}
			set
			{
				if (value < _count)
				{
					throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CapacityMustBeGreaterThanOrEqualToCount, "value");
				}
				if (value == _elements.Length)
				{
					return;
				}
				if (value > 0)
				{
					T[] array = new T[value];
					if (_count > 0)
					{
						Array.Copy(_elements, array, _count);
					}
					_elements = array;
				}
				else
				{
					_elements = System.Collections.Immutable.ImmutableArray<T>.Empty.array;
				}
			}
		}

		public int Count
		{
			get
			{
				return _count;
			}
			set
			{
				Requires.Range(value >= 0, "value");
				if (value < _count)
				{
					if (_count - value > 64)
					{
						Array.Clear(_elements, value, _count - value);
					}
					else
					{
						for (int i = value; i < Count; i++)
						{
							_elements[i] = default(T);
						}
					}
				}
				else if (value > _count)
				{
					EnsureCapacity(value);
				}
				_count = value;
			}
		}

		public T this[int index]
		{
			get
			{
				if (index >= Count)
				{
					ThrowIndexOutOfRangeException();
				}
				return _elements[index];
			}
			set
			{
				if (index >= Count)
				{
					ThrowIndexOutOfRangeException();
				}
				_elements[index] = value;
			}
		}

		bool ICollection<T>.IsReadOnly => false;

		internal Builder(int capacity)
		{
			Requires.Range(capacity >= 0, "capacity");
			_elements = new T[capacity];
			_count = 0;
		}

		internal Builder()
			: this(8)
		{
		}

		private static void ThrowIndexOutOfRangeException()
		{
			throw new IndexOutOfRangeException();
		}

		public ref readonly T ItemRef(int index)
		{
			if (index >= Count)
			{
				ThrowIndexOutOfRangeException();
			}
			return ref _elements[index];
		}

		public System.Collections.Immutable.ImmutableArray<T> ToImmutable()
		{
			return new System.Collections.Immutable.ImmutableArray<T>(ToArray());
		}

		public System.Collections.Immutable.ImmutableArray<T> MoveToImmutable()
		{
			if (Capacity != Count)
			{
				throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CapacityMustEqualCountOnMove);
			}
			T[] elements = _elements;
			_elements = System.Collections.Immutable.ImmutableArray<T>.Empty.array;
			_count = 0;
			return new System.Collections.Immutable.ImmutableArray<T>(elements);
		}

		public System.Collections.Immutable.ImmutableArray<T> DrainToImmutable()
		{
			T[] array = _elements;
			if (array.Length != _count)
			{
				array = ToArray();
			}
			_elements = System.Collections.Immutable.ImmutableArray<T>.Empty.array;
			_count = 0;
			return new System.Collections.Immutable.ImmutableArray<T>(array);
		}

		public void Clear()
		{
			Count = 0;
		}

		public void Insert(int index, T item)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			EnsureCapacity(Count + 1);
			if (index < Count)
			{
				Array.Copy(_elements, index, _elements, index + 1, Count - index);
			}
			_count++;
			_elements[index] = item;
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			Requires.NotNull(items, "items");
			int count = ImmutableExtensions.GetCount(ref items);
			EnsureCapacity(Count + count);
			if (index != Count)
			{
				Array.Copy(_elements, index, _elements, index + count, _count - index);
			}
			if (!items.TryCopyTo(_elements, index))
			{
				foreach (T item in items)
				{
					_elements[index++] = item;
				}
			}
			_count += count;
		}

		public void InsertRange(int index, System.Collections.Immutable.ImmutableArray<T> items)
		{
			Requires.Range(index >= 0 && index <= Count, "index");
			if (!items.IsEmpty)
			{
				EnsureCapacity(Count + items.Length);
				if (index != Count)
				{
					Array.Copy(_elements, index, _elements, index + items.Length, _count - index);
				}
				Array.Copy(items.array, 0, _elements, index, items.Length);
				_count += items.Length;
			}
		}

		public void Add(T item)
		{
			int num = _count + 1;
			EnsureCapacity(num);
			_elements[_count] = item;
			_count = num;
		}

		public void AddRange(IEnumerable<T> items)
		{
			Requires.NotNull(items, "items");
			if (items.TryGetCount(out var count))
			{
				EnsureCapacity(Count + count);
				if (items.TryCopyTo(_elements, _count))
				{
					_count += count;
					return;
				}
			}
			foreach (T item in items)
			{
				Add(item);
			}
		}

		public void AddRange(params T[] items)
		{
			Requires.NotNull(items, "items");
			int count = Count;
			Count += items.Length;
			Array.Copy(items, 0, _elements, count, items.Length);
		}

		public void AddRange<TDerived>(TDerived[] items) where TDerived : T
		{
			Requires.NotNull(items, "items");
			int count = Count;
			Count += items.Length;
			Array.Copy(items, 0, _elements, count, items.Length);
		}

		public void AddRange(T[] items, int length)
		{
			Requires.NotNull(items, "items");
			Requires.Range(length >= 0 && length <= items.Length, "length");
			int count = Count;
			Count += length;
			Array.Copy(items, 0, _elements, count, length);
		}

		public void AddRange(System.Collections.Immutable.ImmutableArray<T> items)
		{
			AddRange(items, items.Length);
		}

		public void AddRange(System.Collections.Immutable.ImmutableArray<T> items, int length)
		{
			Requires.Range(length >= 0, "length");
			if (items.array != null)
			{
				AddRange(items.array, length);
			}
		}

		public void AddRange(params ReadOnlySpan<T> items)
		{
			int count = Count;
			Count += items.Length;
			items.CopyTo(new Span<T>(_elements, count, items.Length));
		}

		public void AddRange<TDerived>(params ReadOnlySpan<TDerived> items) where TDerived : T
		{
			int count = Count;
			Count += items.Length;
			Span<T> span = new Span<T>(_elements, count, items.Length);
			for (int i = 0; i < items.Length; i++)
			{
				span[i] = (T)(object)items[i];
			}
		}

		public void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items) where TDerived : T
		{
			if (items.array != null)
			{
				this.AddRange<TDerived>(items.array);
			}
		}

		public void AddRange(Builder items)
		{
			Requires.NotNull(items, "items");
			AddRange(items._elements, items.Count);
		}

		public void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived>.Builder items) where TDerived : T
		{
			Requires.NotNull(items, "items");
			AddRange<TDerived>(items._elements, items.Count);
		}

		public bool Remove(T element)
		{
			int num = IndexOf(element);
			if (num >= 0)
			{
				RemoveAt(num);
				return true;
			}
			return false;
		}

		public bool Remove(T element, IEqualityComparer<T>? equalityComparer)
		{
			int num = IndexOf(element, 0, _count, equalityComparer);
			if (num >= 0)
			{
				RemoveAt(num);
				return true;
			}
			return false;
		}

		public void RemoveAll(Predicate<T> match)
		{
			List<int> list = null;
			for (int i = 0; i < _count; i++)
			{
				if (match(_elements[i]))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(i);
				}
			}
			if (list != null)
			{
				RemoveAtRange(list);
			}
		}

		public void RemoveAt(int index)
		{
			Requires.Range(index >= 0 && index < Count, "index");
			if (index < Count - 1)
			{
				Array.Copy(_elements, index + 1, _elements, index, Count - index - 1);
			}
			Count--;
		}

		public void RemoveRange(int index, int length)
		{
			Requires.Range(index >= 0 && index <= _count, "index");
			Requires.Range(length >= 0 && index <= _count - length, "length");
			if (length != 0)
			{
				if (index + length < _count)
				{
					Array.Copy(_elements, index + length, _elements, index, Count - index - length);
				}
				_count -= length;
			}
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			RemoveRange(items, EqualityComparer<T>.Default);
		}

		public void RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
		{
			Requires.NotNull(items, "items");
			SortedSet<int> sortedSet = new SortedSet<int>();
			foreach (T item in items)
			{
				int num = IndexOf(item, 0, _count, equalityComparer);
				while (num >= 0 && !sortedSet.Add(num) && num + 1 < _count)
				{
					num = IndexOf(item, num + 1, equalityComparer);
				}
			}
			RemoveAtRange(sortedSet);
		}

		public void Replace(T oldValue, T newValue)
		{
			Replace(oldValue, newValue, EqualityComparer<T>.Default);
		}

		public void Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
		{
			int num = IndexOf(oldValue, 0, _count, equalityComparer);
			if (num >= 0)
			{
				_elements[num] = newValue;
			}
		}

		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}

		public T[] ToArray()
		{
			if (Count == 0)
			{
				return System.Collections.Immutable.ImmutableArray<T>.Empty.array;
			}
			T[] array = new T[Count];
			Array.Copy(_elements, array, Count);
			return array;
		}

		public void CopyTo(T[] array, int index)
		{
			Requires.NotNull(array, "array");
			Requires.Range(index >= 0 && index + Count <= array.Length, "index");
			Array.Copy(_elements, 0, array, index, Count);
		}

		public void CopyTo(T[] destination)
		{
			Requires.NotNull(destination, "destination");
			Array.Copy(_elements, 0, destination, 0, Count);
		}

		public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
		{
			Requires.NotNull(destination, "destination");
			Requires.Range(length >= 0, "length");
			Requires.Range(sourceIndex >= 0 && sourceIndex + length <= Count, "sourceIndex");
			Requires.Range(destinationIndex >= 0 && destinationIndex + length <= destination.Length, "destinationIndex");
			Array.Copy(_elements, sourceIndex, destination, destinationIndex, length);
		}

		private void EnsureCapacity(int capacity)
		{
			if (_elements.Length < capacity)
			{
				int newSize = Math.Max(_elements.Length * 2, capacity);
				Array.Resize(ref _elements, newSize);
			}
		}

		public int IndexOf(T item)
		{
			return IndexOf(item, 0, _count, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex)
		{
			return IndexOf(item, startIndex, Count - startIndex, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex, int count)
		{
			return IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
		}

		public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
		{
			if (count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			Requires.Range(count >= 0 && startIndex + count <= Count, "count");
			if (equalityComparer == null)
			{
				equalityComparer = EqualityComparer<T>.Default;
			}
			if (equalityComparer == EqualityComparer<T>.Default)
			{
				return Array.IndexOf<T>(_elements, item, startIndex, count);
			}
			for (int i = startIndex; i < startIndex + count; i++)
			{
				if (equalityComparer.Equals(_elements[i], item))
				{
					return i;
				}
			}
			return -1;
		}

		public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer)
		{
			return IndexOf(item, startIndex, Count - startIndex, equalityComparer);
		}

		public int LastIndexOf(T item)
		{
			if (Count == 0)
			{
				return -1;
			}
			return LastIndexOf(item, Count - 1, Count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex)
		{
			if (Count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			return LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count)
		{
			return LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
		}

		public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
		{
			if (count == 0 && startIndex == 0)
			{
				return -1;
			}
			Requires.Range(startIndex >= 0 && startIndex < Count, "startIndex");
			Requires.Range(count >= 0 && startIndex - count + 1 >= 0, "count");
			if (equalityComparer == null)
			{
				equalityComparer = EqualityComparer<T>.Default;
			}
			if (equalityComparer == EqualityComparer<T>.Default)
			{
				return Array.LastIndexOf<T>(_elements, item, startIndex, count);
			}
			for (int num = startIndex; num >= startIndex - count + 1; num--)
			{
				if (equalityComparer.Equals(item, _elements[num]))
				{
					return num;
				}
			}
			return -1;
		}

		public void Reverse()
		{
			int num = 0;
			int num2 = _count - 1;
			T[] elements = _elements;
			while (num < num2)
			{
				T val = elements[num];
				elements[num] = elements[num2];
				elements[num2] = val;
				num++;
				num2--;
			}
		}

		public void Sort()
		{
			if (Count > 1)
			{
				Array.Sort(_elements, 0, Count, Comparer<T>.Default);
			}
		}

		public void Sort(Comparison<T> comparison)
		{
			Requires.NotNull(comparison, "comparison");
			if (Count > 1)
			{
				Array.Sort(_elements, 0, _count, Comparer<T>.Create(comparison));
			}
		}

		public void Sort(IComparer<T>? comparer)
		{
			if (Count > 1)
			{
				Array.Sort(_elements, 0, _count, comparer);
			}
		}

		public void Sort(int index, int count, IComparer<T>? comparer)
		{
			Requires.Range(index >= 0, "index");
			Requires.Range(count >= 0 && index + count <= Count, "count");
			if (count > 1)
			{
				Array.Sort(_elements, index, count, comparer);
			}
		}

		public void CopyTo(Span<T> destination)
		{
			Requires.Range(Count <= destination.Length, "destination");
			new ReadOnlySpan<T>(_elements, 0, Count).CopyTo(destination);
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private void AddRange<TDerived>(TDerived[] items, int length) where TDerived : notnull, T
		{
			EnsureCapacity(Count + length);
			int count = Count;
			Count += length;
			T[] elements = _elements;
			for (int i = 0; i < length; i++)
			{
				elements[count + i] = (T)(object)items[i];
			}
		}

		private void RemoveAtRange(ICollection<int> indicesToRemove)
		{
			Requires.NotNull(indicesToRemove, "indicesToRemove");
			if (indicesToRemove.Count == 0)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			int num3 = -1;
			foreach (int item in indicesToRemove)
			{
				int num4 = ((num3 == -1) ? item : (item - num3 - 1));
				Array.Copy(_elements, num + num2, _elements, num, num4);
				num2++;
				num += num4;
				num3 = item;
			}
			Array.Copy(_elements, num + num2, _elements, num, _elements.Length - (num + num2));
			_count -= indicesToRemove.Count;
		}
	}

	public struct Enumerator
	{
		private readonly T[] _array;

		private int _index;

		public T Current => _array[_index];

		internal Enumerator(T[] array)
		{
			_array = array;
			_index = -1;
		}

		public bool MoveNext()
		{
			return ++_index < _array.Length;
		}
	}

	private sealed class EnumeratorObject : IEnumerator<T>, IDisposable, IEnumerator
	{
		private static readonly IEnumerator<T> s_EmptyEnumerator = new EnumeratorObject(System.Collections.Immutable.ImmutableArray<T>.Empty.array);

		private readonly T[] _array;

		private int _index;

		public T Current
		{
			get
			{
				if ((uint)_index < (uint)_array.Length)
				{
					return _array[_index];
				}
				throw new InvalidOperationException();
			}
		}

		object IEnumerator.Current => Current;

		private EnumeratorObject(T[] array)
		{
			_index = -1;
			_array = array;
		}

		public bool MoveNext()
		{
			int num = _index + 1;
			int num2 = _array.Length;
			if ((uint)num <= (uint)num2)
			{
				_index = num;
				return (uint)num < (uint)num2;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}

		public void Dispose()
		{
		}

		internal static IEnumerator<T> Create(T[] array)
		{
			if (array.Length != 0)
			{
				return new EnumeratorObject(array);
			}
			return s_EmptyEnumerator;
		}
	}

	public static readonly System.Collections.Immutable.ImmutableArray<T> Empty = new System.Collections.Immutable.ImmutableArray<T>(new T[0]);

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	internal readonly T[]? array;

	T IList<T>.this[int index]
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection<T>.IsReadOnly => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int ICollection<T>.Count
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int IReadOnlyCollection<T>.Count
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool IList.IsFixedSize => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool IList.IsReadOnly => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	int ICollection.Count
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	bool ICollection.IsSynchronized => true;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	object? IList.this[int index]
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public T this[int index]
	{
		[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
		get
		{
			return array[index];
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsEmpty
	{
		[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
		get
		{
			return array.Length == 0;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public int Length
	{
		[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
		get
		{
			return array.Length;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsDefault => array == null;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public bool IsDefaultOrEmpty
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			if (immutableArray.array != null)
			{
				return immutableArray.array.Length == 0;
			}
			return true;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	Array? IImmutableArray.Array => array;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			if (!immutableArray.IsDefault)
			{
				return $"Length = {immutableArray.Length}";
			}
			return "Uninitialized";
		}
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return new ReadOnlySpan<T>(array);
	}

	public ReadOnlyMemory<T> AsMemory()
	{
		return new ReadOnlyMemory<T>(array);
	}

	public int IndexOf(T item)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, 0, immutableArray.Length, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, startIndex, immutableArray.Length - startIndex, equalityComparer);
	}

	public int IndexOf(T item, int startIndex)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.IndexOf(item, startIndex, immutableArray.Length - startIndex, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, int count)
	{
		return IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}

	public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		if (count == 0 && startIndex == 0)
		{
			return -1;
		}
		Requires.Range(startIndex >= 0 && startIndex < immutableArray.Length, "startIndex");
		Requires.Range(count >= 0 && startIndex + count <= immutableArray.Length, "count");
		if (equalityComparer == null)
		{
			equalityComparer = EqualityComparer<T>.Default;
		}
		if (equalityComparer == EqualityComparer<T>.Default)
		{
			return Array.IndexOf<T>(immutableArray.array, item, startIndex, count);
		}
		for (int i = startIndex; i < startIndex + count; i++)
		{
			if (equalityComparer.Equals(immutableArray.array[i], item))
			{
				return i;
			}
		}
		return -1;
	}

	public int LastIndexOf(T item)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		if (immutableArray.IsEmpty)
		{
			return -1;
		}
		return immutableArray.LastIndexOf(item, immutableArray.Length - 1, immutableArray.Length, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		if (immutableArray.IsEmpty && startIndex == 0)
		{
			return -1;
		}
		return immutableArray.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex, int count)
	{
		return LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
	}

	public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		if (startIndex == 0 && count == 0)
		{
			return -1;
		}
		Requires.Range(startIndex >= 0 && startIndex < immutableArray.Length, "startIndex");
		Requires.Range(count >= 0 && startIndex - count + 1 >= 0, "count");
		if (equalityComparer == null)
		{
			equalityComparer = EqualityComparer<T>.Default;
		}
		if (equalityComparer == EqualityComparer<T>.Default)
		{
			return Array.LastIndexOf<T>(immutableArray.array, item, startIndex, count);
		}
		for (int num = startIndex; num >= startIndex - count + 1; num--)
		{
			if (equalityComparer.Equals(item, immutableArray.array[num]))
			{
				return num;
			}
		}
		return -1;
	}

	public bool Contains(T item)
	{
		return IndexOf(item) >= 0;
	}

	public bool Contains(T item, IEqualityComparer<T>? equalityComparer)
	{
		return this.IndexOf(item, equalityComparer) >= 0;
	}

	public System.Collections.Immutable.ImmutableArray<T> Insert(int index, T item)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= immutableArray.Length, "index");
		if (immutableArray.IsEmpty)
		{
			return System.Collections.Immutable.ImmutableArray.Create(item);
		}
		T[] array = new T[immutableArray.Length + 1];
		array[index] = item;
		if (index != 0)
		{
			Array.Copy(immutableArray.array, array, index);
		}
		if (index != immutableArray.Length)
		{
			Array.Copy(immutableArray.array, index, array, index + 1, immutableArray.Length - index);
		}
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, IEnumerable<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		Requires.NotNull(items, "items");
		if (result.IsEmpty)
		{
			return System.Collections.Immutable.ImmutableArray.CreateRange(items);
		}
		int count = ImmutableExtensions.GetCount(ref items);
		if (count == 0)
		{
			return result;
		}
		T[] array = new T[result.Length + count];
		if (index != 0)
		{
			Array.Copy(result.array, array, index);
		}
		if (index != result.Length)
		{
			Array.Copy(result.array, index, array, index + count, result.Length - index);
		}
		if (!items.TryCopyTo(array, index))
		{
			int num = index;
			foreach (T item in items)
			{
				array[num++] = item;
			}
		}
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, System.Collections.Immutable.ImmutableArray<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		items.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		if (result.IsEmpty)
		{
			return items;
		}
		if (items.IsEmpty)
		{
			return result;
		}
		return result.InsertSpanRangeInternal(index, items.AsSpan());
	}

	public System.Collections.Immutable.ImmutableArray<T> Add(T item)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		if (immutableArray.IsEmpty)
		{
			return System.Collections.Immutable.ImmutableArray.Create(item);
		}
		return immutableArray.Insert(immutableArray.Length, item);
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(IEnumerable<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(T[] items, int length)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(items, "items");
		Requires.Range(length >= 0 && length <= items.Length, "length");
		if (items.Length == 0 || length == 0)
		{
			return result;
		}
		if (result.IsEmpty)
		{
			return System.Collections.Immutable.ImmutableArray.Create(items, 0, length);
		}
		T[] array = new T[result.Length + length];
		Array.Copy(result.array, array, result.Length);
		Array.Copy(items, 0, array, result.Length, length);
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(TDerived[] items) where TDerived : T
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(items, "items");
		if (items.Length == 0)
		{
			return result;
		}
		T[] array = new T[result.Length + items.Length];
		Array.Copy(result.array, array, result.Length);
		Array.Copy(items, 0, array, result.Length, items.Length);
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Immutable.ImmutableArray<T> items, int length)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		Requires.Range(length >= 0, "length");
		if (items.array != null)
		{
			return result.AddRange(items.array, length);
		}
		return result;
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items) where TDerived : T
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		if (items.array != null)
		{
			return result.AddRange<TDerived>(items.array);
		}
		return result;
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Immutable.ImmutableArray<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public System.Collections.Immutable.ImmutableArray<T> SetItem(int index, T item)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index < immutableArray.Length, "index");
		T[] array = new T[immutableArray.Length];
		Array.Copy(immutableArray.array, array, immutableArray.Length);
		array[index] = item;
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue)
	{
		return Replace(oldValue, newValue, EqualityComparer<T>.Default);
	}

	public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		int num = immutableArray.IndexOf(oldValue, 0, immutableArray.Length, equalityComparer);
		if (num < 0)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CannotFindOldValue, "oldValue");
		}
		return immutableArray.SetItem(num, newValue);
	}

	public System.Collections.Immutable.ImmutableArray<T> Remove(T item)
	{
		return Remove(item, EqualityComparer<T>.Default);
	}

	public System.Collections.Immutable.ImmutableArray<T> Remove(T item, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		int num = result.IndexOf(item, 0, result.Length, equalityComparer);
		if (num >= 0)
		{
			return result.RemoveAt(num);
		}
		return result;
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveAt(int index)
	{
		return RemoveRange(index, 1);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(int index, int length)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		Requires.Range(length >= 0 && index <= result.Length - length, "length");
		if (length == 0)
		{
			return result;
		}
		T[] array = new T[result.Length - length];
		Array.Copy(result.array, array, index);
		Array.Copy(result.array, index + length, array, index, result.Length - index - length);
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(IEnumerable<T> items)
	{
		return RemoveRange(items, EqualityComparer<T>.Default);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.NotNull(items, "items");
		SortedSet<int> sortedSet = new SortedSet<int>();
		foreach (T item in items)
		{
			int num = -1;
			do
			{
				num = immutableArray.IndexOf(item, num + 1, equalityComparer);
			}
			while (num >= 0 && !sortedSet.Add(num) && num < immutableArray.Length - 1);
		}
		return immutableArray.RemoveAtRange(sortedSet);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items)
	{
		return RemoveRange(items, EqualityComparer<T>.Default);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items, IEqualityComparer<T>? equalityComparer)
	{
		Requires.NotNull(items.array, "items");
		return RemoveRange(items.AsSpan(), equalityComparer);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveAll(Predicate<T> match)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(match, "match");
		if (result.IsEmpty)
		{
			return result;
		}
		List<int> list = null;
		for (int i = 0; i < result.array.Length; i++)
		{
			if (match(result.array[i]))
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(i);
			}
		}
		if (list == null)
		{
			return result;
		}
		return result.RemoveAtRange(list);
	}

	public System.Collections.Immutable.ImmutableArray<T> Clear()
	{
		return Empty;
	}

	public System.Collections.Immutable.ImmutableArray<T> Sort()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(0, immutableArray.Length, Comparer<T>.Default);
	}

	public System.Collections.Immutable.ImmutableArray<T> Sort(Comparison<T> comparison)
	{
		Requires.NotNull(comparison, "comparison");
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(Comparer<T>.Create(comparison));
	}

	public System.Collections.Immutable.ImmutableArray<T> Sort(IComparer<T>? comparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.Sort(0, immutableArray.Length, comparer);
	}

	public System.Collections.Immutable.ImmutableArray<T> Sort(int index, int count, IComparer<T>? comparer)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0, "index");
		Requires.Range(count >= 0 && index + count <= result.Length, "count");
		if (count > 1)
		{
			if (comparer == null)
			{
				comparer = Comparer<T>.Default;
			}
			bool flag = false;
			for (int i = index + 1; i < index + count; i++)
			{
				if (comparer.Compare(result.array[i - 1], result.array[i]) > 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				T[] array = new T[result.Length];
				Array.Copy(result.array, array, result.Length);
				Array.Sort(array, index, count, comparer);
				return new System.Collections.Immutable.ImmutableArray<T>(array);
			}
		}
		return result;
	}

	public IEnumerable<TResult> OfType<TResult>()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		if (immutableArray.array == null || immutableArray.array.Length == 0)
		{
			return Enumerable.Empty<TResult>();
		}
		return immutableArray.array.OfType<TResult>();
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(params ReadOnlySpan<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public System.Collections.Immutable.ImmutableArray<T> AddRange(params T[] items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return immutableArray.InsertRange(immutableArray.Length, items);
	}

	public ReadOnlySpan<T> AsSpan(int start, int length)
	{
		return new ReadOnlySpan<T>(array, start, length);
	}

	public void CopyTo(Span<T> destination)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.Range(immutableArray.Length <= destination.Length, "destination");
		immutableArray.AsSpan().CopyTo(destination);
	}

	public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, T[] items)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		Requires.NotNull(items, "items");
		if (items.Length == 0)
		{
			return result;
		}
		if (result.IsEmpty)
		{
			return new System.Collections.Immutable.ImmutableArray<T>(items);
		}
		return result.InsertSpanRangeInternal(index, items);
	}

	public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, params ReadOnlySpan<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.Range(index >= 0 && index <= result.Length, "index");
		if (items.IsEmpty)
		{
			return result;
		}
		if (result.IsEmpty)
		{
			return items.ToImmutableArray();
		}
		return result.InsertSpanRangeInternal(index, items);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(ReadOnlySpan<T> items, IEqualityComparer<T>? equalityComparer = null)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		if (items.IsEmpty || result.IsEmpty)
		{
			return result;
		}
		if (items.Length == 1)
		{
			return result.Remove(items[0], equalityComparer);
		}
		SortedSet<int> sortedSet = new SortedSet<int>();
		ReadOnlySpan<T> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T item = readOnlySpan[i];
			int num = -1;
			do
			{
				num = result.IndexOf(item, num + 1, equalityComparer);
			}
			while (num >= 0 && !sortedSet.Add(num) && num < result.Length - 1);
		}
		return result.RemoveAtRange(sortedSet);
	}

	public System.Collections.Immutable.ImmutableArray<T> RemoveRange(T[] items, IEqualityComparer<T>? equalityComparer = null)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Requires.NotNull(items, "items");
		return immutableArray.RemoveRange(new ReadOnlySpan<T>(items), equalityComparer);
	}

	public System.Collections.Immutable.ImmutableArray<T> Slice(int start, int length)
	{
		System.Collections.Immutable.ImmutableArray<T> items = this;
		items.ThrowNullRefIfNotInitialized();
		return System.Collections.Immutable.ImmutableArray.Create(items, start, length);
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

	IImmutableList<T> IImmutableList<T>.Clear()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Clear();
	}

	IImmutableList<T> IImmutableList<T>.Add(T value)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Add(value);
	}

	IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.AddRange(items);
	}

	IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Insert(index, element);
	}

	IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.InsertRange(index, items);
	}

	IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Remove(value, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveAll(match);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveRange(items, equalityComparer);
	}

	IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveRange(index, count);
	}

	IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.RemoveAt(index);
	}

	IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.SetItem(index, value);
	}

	IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return immutableArray.Replace(oldValue, newValue, equalityComparer);
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
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.Contains((T)value);
		}
		return false;
	}

	int IList.IndexOf(object value)
	{
		if (IsCompatibleObject(value))
		{
			System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
			immutableArray.ThrowInvalidOperationIfNotInitialized();
			return immutableArray.IndexOf((T)value);
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
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		Array.Copy(immutableArray.array, 0, array, index, immutableArray.Length);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		Array array = other as Array;
		if (array == null && other is IImmutableArray immutableArray2)
		{
			array = immutableArray2.Array;
			if (immutableArray.array == null && array == null)
			{
				return true;
			}
			if (immutableArray.array == null)
			{
				return false;
			}
		}
		return ((IStructuralEquatable)immutableArray.array).Equals((object)array, comparer);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		return ((IStructuralEquatable)immutableArray.array)?.GetHashCode(comparer) ?? immutableArray.GetHashCode();
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		Array array = other as Array;
		if (array == null && other is IImmutableArray immutableArray2)
		{
			array = immutableArray2.Array;
			if (immutableArray.array == null && array == null)
			{
				return 0;
			}
			if ((immutableArray.array == null) ^ (array == null))
			{
				throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.ArrayInitializedStateNotEqual, "other");
			}
		}
		if (array != null)
		{
			return ((IStructuralComparable)(immutableArray.array ?? throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.ArrayInitializedStateNotEqual, "other"))).CompareTo((object)array, comparer);
		}
		throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.ArrayLengthsNotEqual, "other");
	}

	private System.Collections.Immutable.ImmutableArray<T> RemoveAtRange(ICollection<int> indicesToRemove)
	{
		System.Collections.Immutable.ImmutableArray<T> result = this;
		result.ThrowNullRefIfNotInitialized();
		Requires.NotNull(indicesToRemove, "indicesToRemove");
		if (indicesToRemove.Count == 0)
		{
			return result;
		}
		T[] array = new T[result.Length - indicesToRemove.Count];
		int num = 0;
		int num2 = 0;
		int num3 = -1;
		foreach (int item in indicesToRemove)
		{
			int num4 = ((num3 == -1) ? item : (item - num3 - 1));
			Array.Copy(result.array, num + num2, array, num, num4);
			num2++;
			num += num4;
			num3 = item;
		}
		Array.Copy(result.array, num + num2, array, num, result.Length - (num + num2));
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	private System.Collections.Immutable.ImmutableArray<T> InsertSpanRangeInternal(int index, ReadOnlySpan<T> items)
	{
		T[] array = new T[Length + items.Length];
		if (index != 0)
		{
			Array.Copy(this.array, array, index);
		}
		items.CopyTo(new Span<T>(array, index, items.Length));
		if (index != Length)
		{
			Array.Copy(this.array, index, array, index + items.Length, Length - index);
		}
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	internal ImmutableArray(T[]? items)
	{
		array = items;
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
	public static bool operator ==(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right)
	{
		return left.Equals(right);
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
	public static bool operator !=(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right)
	{
		return !left.Equals(right);
	}

	public static bool operator ==(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right)
	{
		return left.GetValueOrDefault().Equals(right.GetValueOrDefault());
	}

	public static bool operator !=(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right)
	{
		return !left.GetValueOrDefault().Equals(right.GetValueOrDefault());
	}

	public ref readonly T ItemRef(int index)
	{
		return ref array[index];
	}

	public void CopyTo(T[] destination)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, destination, immutableArray.Length);
	}

	public void CopyTo(T[] destination, int destinationIndex)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, 0, destination, destinationIndex, immutableArray.Length);
	}

	public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length)
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		Array.Copy(immutableArray.array, sourceIndex, destination, destinationIndex, length);
	}

	public System.Collections.Immutable.ImmutableArray<T>.Builder ToBuilder()
	{
		System.Collections.Immutable.ImmutableArray<T> items = this;
		if (items.Length == 0)
		{
			return new Builder();
		}
		Builder builder = new Builder(items.Length);
		builder.AddRange(items);
		return builder;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Enumerator GetEnumerator()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowNullRefIfNotInitialized();
		return new Enumerator(immutableArray.array);
	}

	public override int GetHashCode()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		if (immutableArray.array != null)
		{
			return immutableArray.array.GetHashCode();
		}
		return 0;
	}

	public override bool Equals([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullWhen(true)] object? obj)
	{
		if (obj is IImmutableArray immutableArray)
		{
			return array == immutableArray.Array;
		}
		return false;
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENonVersionable]
	public bool Equals(System.Collections.Immutable.ImmutableArray<T> other)
	{
		return array == other.array;
	}

	public static System.Collections.Immutable.ImmutableArray<T> CastUp<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items) where TDerived : class?, T
	{
		T[] items2 = (T[])(object)items.array;
		return new System.Collections.Immutable.ImmutableArray<T>(items2);
	}

	public System.Collections.Immutable.ImmutableArray<TOther> CastArray<TOther>() where TOther : class?
	{
		return new System.Collections.Immutable.ImmutableArray<TOther>((TOther[])(object)array);
	}

	public System.Collections.Immutable.ImmutableArray<TOther> As<TOther>() where TOther : class?
	{
		return new System.Collections.Immutable.ImmutableArray<TOther>(array as TOther[]);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return EnumeratorObject.Create(immutableArray.array);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		System.Collections.Immutable.ImmutableArray<T> immutableArray = this;
		immutableArray.ThrowInvalidOperationIfNotInitialized();
		return EnumeratorObject.Create(immutableArray.array);
	}

	internal void ThrowNullRefIfNotInitialized()
	{
		_ = array.Length;
	}

	private void ThrowInvalidOperationIfNotInitialized()
	{
		if (IsDefault)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidOperationOnDefaultArray);
		}
	}
}
