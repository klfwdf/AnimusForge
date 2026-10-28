namespace System.Reflection.Metadata;

internal readonly struct ExportedTypeHandle : IEquatable<ExportedTypeHandle>
{
	private const uint tokenType = 654311424u;

	private const byte tokenTypeSmall = 39;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ExportedTypeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ExportedTypeHandle FromRowId(int rowId)
	{
		return new ExportedTypeHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(ExportedTypeHandle handle)
	{
		return new System.Reflection.Metadata.Handle(39, handle._rowId);
	}

	public static implicit operator EntityHandle(ExportedTypeHandle handle)
	{
		return new EntityHandle((uint)(0x27000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ExportedTypeHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 39)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ExportedTypeHandle(handle.RowId);
	}

	public static explicit operator ExportedTypeHandle(EntityHandle handle)
	{
		if (handle.VType != 654311424)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ExportedTypeHandle(handle.RowId);
	}

	public static bool operator ==(ExportedTypeHandle left, ExportedTypeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ExportedTypeHandle)
		{
			return ((ExportedTypeHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ExportedTypeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ExportedTypeHandle left, ExportedTypeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
