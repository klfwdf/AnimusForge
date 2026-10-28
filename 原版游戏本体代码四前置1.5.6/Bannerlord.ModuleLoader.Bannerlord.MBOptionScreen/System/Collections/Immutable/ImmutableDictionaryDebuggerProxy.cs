using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Immutable;

internal sealed class ImmutableDictionaryDebuggerProxy<TKey, TValue> where TKey : notnull
{
	private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

	private DebugViewDictionaryItem<TKey, TValue>[] _cachedContents;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public DebugViewDictionaryItem<TKey, TValue>[] Contents => _cachedContents ?? (_cachedContents = _dictionary.Select((KeyValuePair<TKey, TValue> kv) => new DebugViewDictionaryItem<TKey, TValue>(kv)).ToArray(_dictionary.Count));

	public ImmutableDictionaryDebuggerProxy(IReadOnlyDictionary<TKey, TValue> dictionary)
	{
		Requires.NotNull(dictionary, "dictionary");
		_dictionary = dictionary;
	}
}
