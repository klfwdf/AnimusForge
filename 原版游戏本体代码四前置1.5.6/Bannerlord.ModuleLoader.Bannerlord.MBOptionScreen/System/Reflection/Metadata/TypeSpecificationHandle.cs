namespace System.Reflection.Metadata;

internal readonly struct TypeSpecificationHandle : IEquatable<TypeSpecificationHandle>
{
	private const uint tokenType = 452984832u;

	private const byte tokenTypeSmall = 27;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private TypeSpecificationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static TypeSpecificationHandle FromRowId(int rowId)
	{
		return new TypeSpecificationHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(TypeSpecificationHandle handle)
	{
		return new System.Reflection.Metadata.Handle(27, handle._rowId);
	}

	public static implicit operator EntityHandle(TypeSpecificationHandle handle)
	{
		return new EntityHandle((uint)(0x1B000000uL | (ulong)handle._rowId));
	}

	public static explicit operator TypeSpecificationHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 27)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new TypeSpecificationHandle(handle.RowId);
	}

	public static explicit operator TypeSpecificationHandle(EntityHandle handle)
	{
		if (handle.VType != 452984832)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new TypeSpecificationHandle(handle.RowId);
	}

	public static bool operator ==(TypeSpecificationHandle left, TypeSpecificationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is TypeSpecificationHandle)
		{
			return ((TypeSpecificationHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(TypeSpecificationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(TypeSpecificationHandle left, TypeSpecificationHandle right)
	{
		return left._rowId != right._rowId;
	}
}
