using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal sealed class HandleComparer : IEqualityComparer<System.Reflection.Metadata.Handle>, IComparer<System.Reflection.Metadata.Handle>, IEqualityComparer<EntityHandle>, IComparer<EntityHandle>
{
	private static readonly HandleComparer s_default = new HandleComparer();

	public static HandleComparer Default => s_default;

	private HandleComparer()
	{
	}

	public bool Equals(System.Reflection.Metadata.Handle x, System.Reflection.Metadata.Handle y)
	{
		return x.Equals(y);
	}

	public bool Equals(EntityHandle x, EntityHandle y)
	{
		return x.Equals(y);
	}

	public int GetHashCode(System.Reflection.Metadata.Handle obj)
	{
		return obj.GetHashCode();
	}

	public int GetHashCode(EntityHandle obj)
	{
		return obj.GetHashCode();
	}

	public int Compare(System.Reflection.Metadata.Handle x, System.Reflection.Metadata.Handle y)
	{
		return System.Reflection.Metadata.Handle.Compare(x, y);
	}

	public int Compare(EntityHandle x, EntityHandle y)
	{
		return EntityHandle.Compare(x, y);
	}
}
