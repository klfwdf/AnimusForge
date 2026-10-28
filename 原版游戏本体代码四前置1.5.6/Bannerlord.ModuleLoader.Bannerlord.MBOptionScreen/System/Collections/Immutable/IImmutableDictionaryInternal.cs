namespace System.Collections.Immutable;

internal interface IImmutableDictionaryInternal<TKey, TValue>
{
	bool ContainsValue(TValue value);
}
