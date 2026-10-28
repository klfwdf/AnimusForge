using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class LazyConcurrentDictionary<TKey, TValue>
{
	private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary;

	public LazyConcurrentDictionary()
	{
		concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		Lazy<TValue> orAdd = concurrentDictionary.GetOrAdd(key, (TKey k) => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));
		return orAdd.Value;
	}

	public bool ContainsUninitializedValue(TKey key)
	{
		if (concurrentDictionary.TryGetValue(key, out var value))
		{
			return !value.IsValueCreated;
		}
		return false;
	}

	public void Remove(TKey key)
	{
		concurrentDictionary.TryRemove(key, out var _);
	}
}
