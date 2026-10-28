using System.Collections.Immutable;

namespace System.Runtime.InteropServices;

internal static class ImmutableCollectionsMarshal
{
	public static System.Collections.Immutable.ImmutableArray<T> AsImmutableArray<T>(T[]? array)
	{
		return new System.Collections.Immutable.ImmutableArray<T>(array);
	}

	public static T[]? AsArray<T>(System.Collections.Immutable.ImmutableArray<T> array)
	{
		return array.array;
	}
}
