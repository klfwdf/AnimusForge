using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen;

internal static class FrozenDictionary
{
	public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
	{
		Dictionary<TKey, TValue> newDictionary;
		return GetExistingFrozenOrNewDictionary(source, comparer, out newDictionary) ?? CreateFromDictionary(newDictionary);
	}

	public static FrozenDictionary<TKey, TSource> ToFrozenDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
	{
		return source.ToDictionary(keySelector, comparer).ToFrozenDictionary(comparer);
	}

	public static FrozenDictionary<TKey, TElement> ToFrozenDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
	{
		return source.ToDictionary(keySelector, elementSelector, comparer).ToFrozenDictionary(comparer);
	}

	private static FrozenDictionary<TKey, TValue> GetExistingFrozenOrNewDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer, out Dictionary<TKey, TValue> newDictionary) where TKey : notnull where TValue : notnull
	{
		ThrowHelper.ThrowIfNull(source, "source");
		if (comparer == null)
		{
			comparer = EqualityComparer<TKey>.Default;
		}
		if (source is FrozenDictionary<TKey, TValue> frozenDictionary && frozenDictionary.Comparer.Equals(comparer))
		{
			newDictionary = null;
			return frozenDictionary;
		}
		newDictionary = source as Dictionary<TKey, TValue>;
		if (newDictionary == null || (newDictionary.Count != 0 && !newDictionary.Comparer.Equals(comparer)))
		{
			newDictionary = new Dictionary<TKey, TValue>(comparer);
			foreach (KeyValuePair<TKey, TValue> item in source)
			{
				newDictionary[item.Key] = item.Value;
			}
		}
		if (newDictionary.Count == 0)
		{
			if (comparer != FrozenDictionary<TKey, TValue>.Empty.Comparer)
			{
				return new EmptyFrozenDictionary<TKey, TValue>(comparer);
			}
			return FrozenDictionary<TKey, TValue>.Empty;
		}
		return null;
	}

	private static FrozenDictionary<TKey, TValue> CreateFromDictionary<TKey, TValue>(Dictionary<TKey, TValue> source) where TKey : notnull where TValue : notnull
	{
		IEqualityComparer<TKey> comparer = source.Comparer;
		if (typeof(TKey).IsValueType && comparer == EqualityComparer<TKey>.Default)
		{
			if (source.Count <= 10)
			{
				if (Constants.IsKnownComparable<TKey>())
				{
					return new SmallValueTypeComparableFrozenDictionary<TKey, TValue>(source);
				}
				return new SmallValueTypeDefaultComparerFrozenDictionary<TKey, TValue>(source);
			}
			if (typeof(TKey) == typeof(int))
			{
				return (FrozenDictionary<TKey, TValue>)(object)new Int32FrozenDictionary<TValue>((Dictionary<int, TValue>)(object)source);
			}
			return new ValueTypeDefaultComparerFrozenDictionary<TKey, TValue>(source);
		}
		if (typeof(TKey) == typeof(string) && (comparer == EqualityComparer<TKey>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase))
		{
			IEqualityComparer<string> equalityComparer = (IEqualityComparer<string>)comparer;
			string[] array = (string[])(object)source.Keys.ToArray();
			TValue[] values = source.Values.ToArray();
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
			FrozenDictionary<string, TValue> frozenDictionary = LengthBucketsFrozenDictionary<TValue>.CreateLengthBucketsFrozenDictionaryIfAppropriate(array, values, equalityComparer, num, num2);
			if (frozenDictionary != null)
			{
				return (FrozenDictionary<TKey, TValue>)(object)frozenDictionary;
			}
			KeyAnalyzer.AnalysisResults analysisResults = KeyAnalyzer.Analyze(array, equalityComparer == StringComparer.OrdinalIgnoreCase, num, num2);
			frozenDictionary = (analysisResults.SubstringHashing ? (analysisResults.RightJustifiedSubstring ? ((!analysisResults.IgnoreCase) ? ((analysisResults.HashCount == 1) ? ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_RightJustifiedSingleChar<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex)) : ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_RightJustifiedSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveAsciiSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)) : ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)))) : ((!analysisResults.IgnoreCase) ? ((analysisResults.HashCount == 1) ? ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_LeftJustifiedSingleChar<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex)) : ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_LeftJustifiedSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveAsciiSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount)) : ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveSubstring<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, analysisResults.HashIndex, analysisResults.HashCount))))) : ((!analysisResults.IgnoreCase) ? new OrdinalStringFrozenDictionary_Full<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3) : (analysisResults.AllAsciiIfIgnoreCase ? ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_FullCaseInsensitiveAscii<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3)) : ((OrdinalStringFrozenDictionary<TValue>)new OrdinalStringFrozenDictionary_FullCaseInsensitive<TValue>(array, values, equalityComparer, analysisResults.MinimumLength, analysisResults.MaximumLengthDiff, num3)))));
			return (FrozenDictionary<TKey, TValue>)(object)frozenDictionary;
		}
		if (source.Count <= 4)
		{
			return new SmallFrozenDictionary<TKey, TValue>(source);
		}
		return new DefaultFrozenDictionary<TKey, TValue>(source);
	}
}
[DebuggerTypeProxy(typeof(ImmutableDictionaryDebuggerProxy<, >))]
[DebuggerDisplay("Count = {Count}")]
internal abstract class FrozenDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection where TKey : notnull
{
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator
	{
		private readonly TKey[] _keys;

		private readonly TValue[] _values;

		private int _index;

		public readonly KeyValuePair<TKey, TValue> Current
		{
			get
			{
				if ((uint)_index >= (uint)_keys.Length)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				return new KeyValuePair<TKey, TValue>(_keys[_index], _values[_index]);
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(TKey[] keys, TValue[] values)
		{
			_keys = keys;
			_values = values;
			_index = -1;
		}

		public bool MoveNext()
		{
			_index++;
			if ((uint)_index < (uint)_keys.Length)
			{
				return true;
			}
			_index = _keys.Length;
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

	public static FrozenDictionary<TKey, TValue> Empty { get; } = new EmptyFrozenDictionary<TKey, TValue>(EqualityComparer<TKey>.Default);

	public IEqualityComparer<TKey> Comparer { get; }

	public System.Collections.Immutable.ImmutableArray<TKey> Keys => ImmutableCollectionsMarshal.AsImmutableArray(KeysCore);

	private protected abstract TKey[] KeysCore { get; }

	ICollection<TKey> IDictionary<TKey, TValue>.Keys
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<TKey> keys = Keys;
			if (keys.Length <= 0)
			{
				return Array.Empty<TKey>();
			}
			return keys;
		}
	}

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IDictionary<TKey, TValue>)this).Keys;

	ICollection IDictionary.Keys => Keys;

	public System.Collections.Immutable.ImmutableArray<TValue> Values => ImmutableCollectionsMarshal.AsImmutableArray(ValuesCore);

	private protected abstract TValue[] ValuesCore { get; }

	ICollection<TValue> IDictionary<TKey, TValue>.Values
	{
		get
		{
			System.Collections.Immutable.ImmutableArray<TValue> values = Values;
			if (values.Length <= 0)
			{
				return Array.Empty<TValue>();
			}
			return values;
		}
	}

	ICollection IDictionary.Values => Values;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IDictionary<TKey, TValue>)this).Values;

	public int Count => CountCore;

	private protected abstract int CountCore { get; }

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

	bool IDictionary.IsReadOnly => true;

	bool IDictionary.IsFixedSize => true;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IDictionary.this[object key]
	{
		get
		{
			ThrowHelper.ThrowIfNull(key, "key");
			if (!(key is TKey key2) || !TryGetValue(key2, out var value))
			{
				return null;
			}
			return value;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public ref readonly TValue this[TKey key]
	{
		get
		{
			ref readonly TValue valueRefOrNullRef = ref GetValueRefOrNullRef(key);
			if (Unsafe.IsNullRef(ref Unsafe.AsRef(in valueRefOrNullRef)))
			{
				ThrowHelper.ThrowKeyNotFoundException(key);
			}
			return ref valueRefOrNullRef;
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

	TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

	private protected FrozenDictionary(IEqualityComparer<TKey> comparer)
	{
		Comparer = comparer;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] destination, int destinationIndex)
	{
		ThrowHelper.ThrowIfNull(destination, "destination");
		CopyTo(destination.AsSpan(destinationIndex));
	}

	public void CopyTo(Span<KeyValuePair<TKey, TValue>> destination)
	{
		if (destination.Length < Count)
		{
			ThrowHelper.ThrowIfDestinationTooSmall();
		}
		TKey[] keysCore = KeysCore;
		TValue[] valuesCore = ValuesCore;
		for (int i = 0; i < keysCore.Length; i++)
		{
			destination[i] = new KeyValuePair<TKey, TValue>(keysCore[i], valuesCore[i]);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		ThrowHelper.ThrowIfNull(array, "array");
		if (array.Rank != 1)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array.GetLowerBound(0) != 0)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Arg_NonZeroLowerBound, "array");
		}
		if ((uint)index > (uint)array.Length)
		{
			throw new ArgumentOutOfRangeException("index", _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Length - index < Count)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Arg_ArrayPlusOffTooSmall, "array");
		}
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current = enumerator.Current;
				array2[index++] = new KeyValuePair<TKey, TValue>(current.Key, current.Value);
			}
			return;
		}
		if (array is DictionaryEntry[] array3)
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current2 = enumerator.Current;
				array3[index++] = new DictionaryEntry(current2.Key, current2.Value);
			}
			return;
		}
		if (!(array is object[] array4))
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Argument_IncompatibleArrayType, "array");
		}
		try
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<TKey, TValue> current3 = enumerator.Current;
				array4[index++] = new KeyValuePair<TKey, TValue>(current3.Key, current3.Value);
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Argument_IncompatibleArrayType, "array");
		}
	}

	public ref readonly TValue GetValueRefOrNullRef(TKey key)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException("key");
		}
		return ref GetValueRefOrNullRefCore(key);
	}

	private protected abstract ref readonly TValue GetValueRefOrNullRefCore(TKey key);

	private protected virtual ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key) where TAlternateKey : notnull
	{
		return ref Unsafe.NullRef<TValue>();
	}

	public bool ContainsKey(TKey key)
	{
		return !Unsafe.IsNullRef(ref Unsafe.AsRef(in GetValueRefOrNullRef(key)));
	}

	bool IDictionary.Contains(object key)
	{
		ThrowHelper.ThrowIfNull(key, "key");
		if (key is TKey key2)
		{
			return ContainsKey(key2);
		}
		return false;
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
	{
		if (TryGetValue(item.Key, out var value))
		{
			return EqualityComparer<TValue>.Default.Equals(value, item.Value);
		}
		return false;
	}

	public bool TryGetValue(TKey key, [_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out TValue value)
	{
		ref readonly TValue valueRefOrNullRef = ref GetValueRefOrNullRef(key);
		if (!Unsafe.IsNullRef(ref Unsafe.AsRef(in valueRefOrNullRef)))
		{
			value = valueRefOrNullRef;
			return true;
		}
		value = default(TValue);
		return false;
	}

	public Enumerator GetEnumerator()
	{
		return GetEnumeratorCore();
	}

	private protected abstract Enumerator GetEnumeratorCore();

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		if (Count != 0)
		{
			return GetEnumerator();
		}
		return ((IEnumerable<KeyValuePair<TKey, TValue>>)Array.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		if (Count != 0)
		{
			return GetEnumerator();
		}
		return Array.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new DictionaryEnumerator<TKey, TValue>(GetEnumerator());
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		throw new NotSupportedException();
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException();
	}

	void IDictionary.Add(object key, object value)
	{
		throw new NotSupportedException();
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		throw new NotSupportedException();
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException();
	}

	void IDictionary.Remove(object key)
	{
		throw new NotSupportedException();
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		throw new NotSupportedException();
	}

	void IDictionary.Clear()
	{
		throw new NotSupportedException();
	}
}
