using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveAsciiSubstring : OrdinalStringFrozenSet
{
	internal OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveAsciiSubstring(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex, int hashCount)
		: base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
	{
	}

	private protected override int FindItemIndex(string item)
	{
		return base.FindItemIndex(item);
	}

	private protected override int FindItemIndex<TAlternate>(TAlternate item)
	{
		return base.FindItemIndex(item);
	}

	private protected override bool Equals(string? x, string? y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}

	private protected override bool Equals(ReadOnlySpan<char> x, string? y)
	{
		return OrdinalStringFrozenSet.EqualsOrdinalIgnoreCase(x, y);
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s.AsSpan(s.Length + base.HashIndex, base.HashCount));
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s.Slice(s.Length + base.HashIndex, base.HashCount));
	}
}
