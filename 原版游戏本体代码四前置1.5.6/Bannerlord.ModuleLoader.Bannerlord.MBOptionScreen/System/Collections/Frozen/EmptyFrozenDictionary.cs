using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

internal sealed class EmptyFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue> where TKey : notnull
{
	private protected override TKey[] KeysCore => Array.Empty<TKey>();

	private protected override TValue[] ValuesCore => Array.Empty<TValue>();

	private protected override int CountCore => 0;

	internal EmptyFrozenDictionary(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
	}

	private protected override Enumerator GetEnumeratorCore()
	{
		return new FrozenDictionary<TKey, TValue>.Enumerator(Array.Empty<TKey>(), Array.Empty<TValue>());
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
	{
		return ref Unsafe.NullRef<TValue>();
	}
}
