using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveSubstring<TValue> : OrdinalStringFrozenDictionary<TValue>
{
	internal OrdinalStringFrozenDictionary_LeftJustifiedCaseInsensitiveSubstring(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex, int hashCount)
		: base(keys, values, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
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
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}

	private protected override bool Equals(ReadOnlySpan<char> x, string? y)
	{
		return x.Equals(y.AsSpan(), StringComparison.OrdinalIgnoreCase);
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan(base.HashIndex, base.HashCount));
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCase(s.Slice(base.HashIndex, base.HashCount));
	}
}
