namespace System.Reflection.Metadata;

internal readonly struct PropertyDefinitionHandle : IEquatable<PropertyDefinitionHandle>
{
	private const uint tokenType = 385875968u;

	private const byte tokenTypeSmall = 23;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private PropertyDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static PropertyDefinitionHandle FromRowId(int rowId)
	{
		return new PropertyDefinitionHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(PropertyDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.Handle(23, handle._rowId);
	}

	public static implicit operator EntityHandle(PropertyDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x17000000uL | (ulong)handle._rowId));
	}

	public static explicit operator PropertyDefinitionHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 23)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new PropertyDefinitionHandle(handle.RowId);
	}

	public static explicit operator PropertyDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 385875968)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new PropertyDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is PropertyDefinitionHandle)
		{
			return ((PropertyDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(PropertyDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
