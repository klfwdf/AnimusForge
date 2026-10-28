using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenSet_FullCaseInsensitiveAscii : OrdinalStringFrozenSet
{
	private readonly ulong _lengthFilter;

	internal OrdinalStringFrozenSet_FullCaseInsensitiveAscii(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, ulong lengthFilter)
		: base(entries, comparer, minimumLength, maximumLengthDiff)
	{
		_lengthFilter = lengthFilter;
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
		return Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s.AsSpan());
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s);
	}

	private protected override bool CheckLengthQuick(uint length)
	{
		return (_lengthFilter & (ulong)(1L << (int)(length % 64))) != 0;
	}
}
