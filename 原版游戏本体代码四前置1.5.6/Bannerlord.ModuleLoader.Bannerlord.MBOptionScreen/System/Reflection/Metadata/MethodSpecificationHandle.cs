namespace System.Reflection.Metadata;

internal readonly struct MethodSpecificationHandle : IEquatable<MethodSpecificationHandle>
{
	private const uint tokenType = 721420288u;

	private const byte tokenTypeSmall = 43;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodSpecificationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static MethodSpecificationHandle FromRowId(int rowId)
	{
		return new MethodSpecificationHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(MethodSpecificationHandle handle)
	{
		return new System.Reflection.Metadata.Handle(43, handle._rowId);
	}

	public static implicit operator EntityHandle(MethodSpecificationHandle handle)
	{
		return new EntityHandle((uint)(0x2B000000uL | (ulong)handle._rowId));
	}

	public static explicit operator MethodSpecificationHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 43)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new MethodSpecificationHandle(handle.RowId);
	}

	public static explicit operator MethodSpecificationHandle(EntityHandle handle)
	{
		if (handle.VType != 721420288)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new MethodSpecificationHandle(handle.RowId);
	}

	public static bool operator ==(MethodSpecificationHandle left, MethodSpecificationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is MethodSpecificationHandle)
		{
			return ((MethodSpecificationHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(MethodSpecificationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(MethodSpecificationHandle left, MethodSpecificationHandle right)
	{
		return left._rowId != right._rowId;
	}
}
