using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen;

internal static class FrozenSet
{
	public static FrozenSet<T> Create<T>(params ReadOnlySpan<T> source)
	{
		return Create((IEqualityComparer<T>?)null, source);
	}

	public static FrozenSet<T> Create<T>(IEqualityComparer<T>? equalityComparer, params ReadOnlySpan<T> source)
	{
		if (source.Length == 0)
		{
			if (equalityComparer != null && equalityComparer != FrozenSet<T>.Empty.Comparer)
			{
				return new EmptyFrozenSet<T>(equalityComparer);
			}
			return FrozenSet<T>.Empty;
		}
		HashSet<T> hashSet = new HashSet<T>(equalityComparer);
		ReadOnlySpan<T> readOnlySpan = source;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T item = readOnlySpan[i];
			hashSet.Add(item);
		}
		return hashSet.ToFrozenSet(equalityComparer);
	}

	public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
	{
		HashSet<T> newSet;
		return GetExistingFrozenOrNewSet(source, comparer, out newSet) ?? CreateFromSet(newSet);
	}

	private static FrozenSet<T> GetExistingFrozenOrNewSet<T>(IEnumerable<T> source, IEqualityComparer<T> comparer, out HashSet<T> newSet) where T : notnull
	{
		ThrowHelper.ThrowIfNull(source, "source");
		if (comparer == null)
		{
			comparer = EqualityComparer<T>.Default;
		}
		if (source is FrozenSet<T> frozenSet && frozenSet.Comparer.Equals(comparer))
		{
			newSet = null;
			return frozenSet;
		}
		newSet = source as HashSet<T>;
		if (newSet == null || (newSet.Count != 0 && !newSet.Comparer.Equals(comparer)))
		{
			newSet = new HashSet<T>(source, comparer);
		}
		if (newSet.Count == 0)
		{
			if (comparer != FrozenSet<T>.Empty.Comparer)
			{
				return new EmptyFrozenSet<T>(comparer);
			}
			return FrozenSet<T>.Empty;
		}
		return null;
	}

	private static FrozenSet<T> CreateFromSet<T>(HashSet<T> source) where T : notnull
	{
		IEqualityComparer<T> comparer = source.Comparer;
		if (typeof(T).IsValueType && comparer == EqualityComparer<T>.Default)
		{
			if (source.Count <= 10)
			{
				if (Constants.IsKnownComparable<T>())
				{
					return new SmallValueTypeComparableFrozenSet<T>(source);
				}
				return new SmallValueTypeDefaultComparerFrozenSet<T>(source);
			}
			if (typeof(T) == typeof(int))
			{
				return (FrozenSet<T>)(object)new Int32FrozenSet((HashSet<int>)(object)source);
			}
			return new ValueTypeDefaultComparerFrozenSet<T>(source);
		}
		if (typeof(T) == typeof(string) && !source.Contains(default(T)) && (comparer == EqualityComparer<T>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase))
		{
			IEqualityComparer<string> equalityComparer = (IEqualityComparer<string>)comparer;
			HashSet<string> obj = (HashSet<string>)(object)source;
			string[] array = new string[obj.Count];
			obj.CopyTo(array);
			int num = int.MaxValue;
			int num2 = 0;
			ulong num3 = 0uL;
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.Length < num)
				{
					num = text.Length;
				}
				if (text.Length > num2)
				{
					num2 = text.Length;
				}
				num3 |= (ulong)(1L << text.Length % 64);
			}
			FrozenSet<string> frozenSet = LengthBucketsFrozenSet.CreateLengthBucketsFrozenSetIfAppropriate(array, equalityComparer, num, num2);
			if (frozenSet != null)
			{
				return (FrozenSet<T>)(object)frozenSet;
			}
			KeyAnalyzer.AnalysisResults analysisResults = KeyAnalyzer.Analyze(array, equalityComparer == StringComparer.OrdinalIgnoreCase, num, num2);
			frozenSet = (analysisResults.SubstringHashing ? (analysisResults.RightJustifiedSubstring ? ((!analysisResults.IgnoreCase) ? ((analysisResults.HashCount == 1) ? ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_RightJustifiedSingleChar(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex)) : ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_RightJustifiedSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveAsciiSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)) : ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)))) : ((!analysisResults.IgnoreCase) ? ((analysisResults.HashCount == 1) ? ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_LeftJustifiedSingleChar(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex)) : ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_LeftJustifiedSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveAsciiSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)) : ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveSubstring(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))))) : ((!analysisResults.IgnoreCase) ? new OrdinalStringFrozenSet_Full(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_FullCaseInsensitiveAscii(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3)) : ((OrdinalStringFrozenSet)new OrdinalStringFrozenSet_FullCaseInsensitive(array, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3)))));
			return (FrozenSet<T>)(object)frozenSet;
		}
		if (source.Count <= 4)
		{
			return new SmallFrozenSet<T>(source);
		}
		return new DefaultFrozenSet<T>(source);
	}
}
[CollectionBuilder(typeof(FrozenSet), "Create")]
[DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
[DebuggerDisplay("Count = {Count}")]
internal abstract class FrozenSet<T> : ISet<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
{
	public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly T[] _entries;

		private int _index;

		public readonly T Current
		{
			get
			{
				if ((uint)_index >= (uint)_entries.Length)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				return _entries[_index];
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(T[] entries)
		{
			_entries = entries;
			_index = -1;
		}

		public bool MoveNext()
		{
			_index++;
			if ((uint)_index < (uint)_entries.Length)
			{
				return true;
			}
			_index = _entries.Length;
			return false;
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}

		void IDisposable.Dispose()
		{
		}
	}

	public static FrozenSet<T> Empty { get; } = new EmptyFrozenSet<T>(EqualityComparer<T>.Default);

	public IEqualityComparer<T> Comparer { get; }

	public System.Collections.Immutable.ImmutableArray<T> Items => ImmutableCollectionsMarshal.AsImmutableArray(ItemsCore);

	private protected abstract T[] ItemsCore { get; }

	public int Count => CountCore;

	private protected abstract int CountCore { get; }

	bool ICollection<T>.IsReadOnly => true;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	private protected FrozenSet(IEqualityComparer<T> comparer)
	{
		Comparer = comparer;
	}

	public void CopyTo(T[] destination, int destinationIndex)
	{
		ThrowHelper.ThrowIfNull(destination, "destination");
		CopyTo(destination.AsSpan(destinationIndex));
	}

	public void CopyTo(Span<T> destination)
	{
		Items.AsSpan().CopyTo(destination);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array != null && array.Rank != 1)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Arg_RankMultiDimNotSupported, "array");
		}
		T[] itemsCore = ItemsCore;
		Array.Copy(itemsCore, 0, array, index, itemsCore.Length);
	}

	public bool Contains(T item)
	{
		return FindItemIndex(item) >= 0;
	}

	public bool TryGetValue(T equalValue, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out T actualValue)
	{
		int num = FindItemIndex(equalValue);
		if (num >= 0)
		{
			actualValue = Items[num];
			return true;
		}
		actualValue = default(T);
		return false;
	}

	private protected abstract int FindItemIndex(T item);

	private protected virtual int FindItemIndex<TAlternate>(TAlternate item)
	{
		return -1;
	}

	public Enumerator GetEnumerator()
	{
		return GetEnumeratorCore();
	}

	private protected abstract Enumerator GetEnumeratorCore();

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		if (Count != 0)
		{
			return GetEnumerator();
		}
		return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		if (Count != 0)
		{
			return GetEnumerator();
		}
		return Array.Empty<T>().GetEnumerator();
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

	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return IsProperSubsetOfCore(other);
	}

	private protected abstract bool IsProperSubsetOfCore(IEnumerable<T> other);

	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return IsProperSupersetOfCore(other);
	}

	private protected abstract bool IsProperSupersetOfCore(IEnumerable<T> other);

	public bool IsSubsetOf(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return IsSubsetOfCore(other);
	}

	private protected abstract bool IsSubsetOfCore(IEnumerable<T> other);

	public bool IsSupersetOf(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return IsSupersetOfCore(other);
	}

	private protected abstract bool IsSupersetOfCore(IEnumerable<T> other);

	public bool Overlaps(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return OverlapsCore(other);
	}

	private protected abstract bool OverlapsCore(IEnumerable<T> other);

	public bool SetEquals(IEnumerable<T> other)
	{
		ThrowHelper.ThrowIfNull(other, "other");
		return SetEqualsCore(other);
	}

	private protected abstract bool SetEqualsCore(IEnumerable<T> other);
}
