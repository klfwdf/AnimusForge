using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct ArrayShape
{
	public int Rank { get; }

	public System.Collections.Immutable.ImmutableArray<int> Sizes { get; }

	public System.Collections.Immutable.ImmutableArray<int> LowerBounds { get; }

	public ArrayShape(int rank, System.Collections.Immutable.ImmutableArray<int> sizes, System.Collections.Immutable.ImmutableArray<int> lowerBounds)
	{
		Rank = rank;
		Sizes = sizes;
		LowerBounds = lowerBounds;
	}
}
