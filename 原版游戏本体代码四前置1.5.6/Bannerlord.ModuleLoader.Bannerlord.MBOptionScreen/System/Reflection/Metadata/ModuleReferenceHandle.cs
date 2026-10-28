namespace System.Reflection.Metadata;

internal readonly struct ModuleReferenceHandle : IEquatable<ModuleReferenceHandle>
{
	private const uint tokenType = 436207616u;

	private const byte tokenTypeSmall = 26;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ModuleReferenceHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ModuleReferenceHandle FromRowId(int rowId)
	{
		return new ModuleReferenceHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(ModuleReferenceHandle handle)
	{
		return new System.Reflection.Metadata.Handle(26, handle._rowId);
	}

	public static implicit operator EntityHandle(ModuleReferenceHandle handle)
	{
		return new EntityHandle((uint)(0x1A000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ModuleReferenceHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 26)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ModuleReferenceHandle(handle.RowId);
	}

	public static explicit operator ModuleReferenceHandle(EntityHandle handle)
	{
		if (handle.VType != 436207616)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ModuleReferenceHandle(handle.RowId);
	}

	public static bool operator ==(ModuleReferenceHandle left, ModuleReferenceHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ModuleReferenceHandle)
		{
			return ((ModuleReferenceHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ModuleReferenceHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ModuleReferenceHandle left, ModuleReferenceHandle right)
	{
		return left._rowId != right._rowId;
	}
}
