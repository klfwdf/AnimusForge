using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenDictionary_Full<TValue> : OrdinalStringFrozenDictionary<TValue>
{
	private readonly ulong _lengthFilter;

	internal OrdinalStringFrozenDictionary_Full(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, ulong lengthFilter)
		: base(keys, values, comparer, minimumLength, maximumLengthDiff, -1, -1)
	{
		_lengthFilter = lengthFilter;
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
	{
		return ref base.GetValueRefOrNullRefCore(key);
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore<TAlternateKey>(TAlternateKey key)
	{
		return ref base.GetValueRefOrNullRefCore(key);
	}

	private protected override bool Equals(string? x, string? y)
	{
		return string.Equals(x, y);
	}

	private protected override bool Equals(ReadOnlySpan<char> x, string? y)
	{
		return x.SequenceEqual(y.AsSpan());
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinal(s.AsSpan());
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return Hashing.GetHashCodeOrdinal(s);
	}

	private protected override bool CheckLengthQuick(uint length)
	{
		return (_lengthFilter & (ulong)(1L << (int)(length % 64))) != 0;
	}
}
