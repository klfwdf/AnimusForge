using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ThreadSafeDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
{
	public ThreadSafeDictionary()
	{
	}

	public ThreadSafeDictionary(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
	}
}
