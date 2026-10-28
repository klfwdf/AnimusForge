namespace System.Reflection.Metadata;

internal readonly struct ParameterHandle : IEquatable<ParameterHandle>
{
	private const uint tokenType = 134217728u;

	private const byte tokenTypeSmall = 8;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ParameterHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ParameterHandle FromRowId(int rowId)
	{
		return new ParameterHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(ParameterHandle handle)
	{
		return new System.Reflection.Metadata.Handle(8, handle._rowId);
	}

	public static implicit operator EntityHandle(ParameterHandle handle)
	{
		return new EntityHandle((uint)(0x8000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ParameterHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 8)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ParameterHandle(handle.RowId);
	}

	public static explicit operator ParameterHandle(EntityHandle handle)
	{
		if (handle.VType != 134217728)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ParameterHandle(handle.RowId);
	}

	public static bool operator ==(ParameterHandle left, ParameterHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ParameterHandle)
		{
			return ((ParameterHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ParameterHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ParameterHandle left, ParameterHandle right)
	{
		return left._rowId != right._rowId;
	}
}
