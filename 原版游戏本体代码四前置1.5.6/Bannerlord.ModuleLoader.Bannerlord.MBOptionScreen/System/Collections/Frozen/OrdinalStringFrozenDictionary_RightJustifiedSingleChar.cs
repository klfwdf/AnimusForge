using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenDictionary_RightJustifiedSingleChar<TValue> : OrdinalStringFrozenDictionary<TValue>
{
	internal OrdinalStringFrozenDictionary_RightJustifiedSingleChar(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex)
		: base(keys, values, comparer, minimumLength, maximumLengthDiff, hashIndex, 1)
	{
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
		return s[s.Length + base.HashIndex];
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return s[s.Length + base.HashIndex];
	}
}
