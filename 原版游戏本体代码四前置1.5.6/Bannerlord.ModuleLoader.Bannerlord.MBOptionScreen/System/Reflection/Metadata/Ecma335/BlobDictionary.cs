using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

[DebuggerDisplay("Count = {Count}")]
internal readonly struct BlobDictionary
{
	private readonly Dictionary<int, KeyValuePair<System.Collections.Immutable.ImmutableArray<byte>, System.Reflection.Metadata.BlobHandle>> _dictionary;

	public int Count => _dictionary.Count;

	private static int GetNextDictionaryKey(int dictionaryKey)
	{
		return dictionaryKey * 747796405 + -1403630843;
	}

	public System.Reflection.Metadata.BlobHandle GetOrAdd(ReadOnlySpan<byte> key, System.Collections.Immutable.ImmutableArray<byte> immutableKey, System.Reflection.Metadata.BlobHandle value, out bool exists)
	{
		int num = System.Reflection.Internal.Hash.GetFNVHashCode(key);
		KeyValuePair<System.Collections.Immutable.ImmutableArray<byte>, System.Reflection.Metadata.BlobHandle> value2;
		while ((exists = _dictionary.TryGetValue(num, out value2)) && !value2.Key.AsSpan().SequenceEqual(key))
		{
			num = GetNextDictionaryKey(num);
		}
		if (exists)
		{
			return value2.Value;
		}
		if (immutableKey.IsDefault)
		{
			immutableKey = key.ToImmutableArray();
		}
		_dictionary.Add(num, new KeyValuePair<System.Collections.Immutable.ImmutableArray<byte>, System.Reflection.Metadata.BlobHandle>(immutableKey, value));
		return value;
	}

	public BlobDictionary(int capacity = 0)
	{
		_dictionary = new Dictionary<int, KeyValuePair<System.Collections.Immutable.ImmutableArray<byte>, System.Reflection.Metadata.BlobHandle>>(capacity);
	}

	public Dictionary<int, KeyValuePair<System.Collections.Immutable.ImmutableArray<byte>, System.Reflection.Metadata.BlobHandle>>.Enumerator GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}
}
