namespace System.Reflection.Metadata;

internal readonly struct AssemblyDefinitionHandle : IEquatable<AssemblyDefinitionHandle>
{
	private const uint tokenType = 536870912u;

	private const byte tokenTypeSmall = 32;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	internal AssemblyDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static AssemblyDefinitionHandle FromRowId(int rowId)
	{
		return new AssemblyDefinitionHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(AssemblyDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.Handle(32, handle._rowId);
	}

	public static implicit operator EntityHandle(AssemblyDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x20000000uL | (ulong)handle._rowId));
	}

	public static explicit operator AssemblyDefinitionHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 32)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new AssemblyDefinitionHandle(handle.RowId);
	}

	public static explicit operator AssemblyDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 536870912)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new AssemblyDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is AssemblyDefinitionHandle)
		{
			return ((AssemblyDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(AssemblyDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
