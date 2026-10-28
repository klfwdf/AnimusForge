using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenSet_RightJustifiedSingleChar : OrdinalStringFrozenSet
{
	internal OrdinalStringFrozenSet_RightJustifiedSingleChar(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex)
		: base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, 1)
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

	private protected override int GetHashCode(string s)
	{
		return s[s.Length + base.HashIndex];
	}

	private protected override int GetHashCode(ReadOnlySpan<char> s)
	{
		return s[s.Length + base.HashIndex];
	}
}
