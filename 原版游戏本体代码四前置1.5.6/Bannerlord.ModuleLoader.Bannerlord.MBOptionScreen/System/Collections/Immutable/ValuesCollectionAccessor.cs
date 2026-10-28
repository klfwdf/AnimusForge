namespace System.Collections.Immutable;

internal sealed class ValuesCollectionAccessor<TKey, TValue> : KeysOrValuesCollectionAccessor<TKey, TValue, TValue> where TKey : notnull
{
	internal ValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
		: base(dictionary, dictionary.Values)
	{
	}

	public override bool Contains(TValue item)
	{
		if (base.Dictionary is ImmutableSortedDictionary<TKey, TValue> immutableSortedDictionary)
		{
			return immutableSortedDictionary.ContainsValue(item);
		}
		if (base.Dictionary is IImmutableDictionaryInternal<TKey, TValue> immutableDictionaryInternal)
		{
			return immutableDictionaryInternal.ContainsValue(item);
		}
		throw new NotSupportedException();
	}
}
