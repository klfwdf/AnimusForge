using System.Collections.Generic;

namespace System.Collections.Immutable;

internal sealed class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator, IEnumerator where TKey : notnull
{
	private readonly IEnumerator<KeyValuePair<TKey, TValue>> _inner;

	public DictionaryEntry Entry => new DictionaryEntry(_inner.Current.Key, _inner.Current.Value);

	public object Key => _inner.Current.Key;

	public object? Value => _inner.Current.Value;

	public object Current => Entry;

	internal DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> inner)
	{
		Requires.NotNull(inner, "inner");
		_inner = inner;
	}

	public bool MoveNext()
	{
		return _inner.MoveNext();
	}

	public void Reset()
	{
		_inner.Reset();
	}
}
