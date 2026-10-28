namespace System.Reflection.Metadata;

internal readonly struct LocalVariableHandle : IEquatable<LocalVariableHandle>
{
	private const uint tokenType = 855638016u;

	private const byte tokenTypeSmall = 51;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private LocalVariableHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static LocalVariableHandle FromRowId(int rowId)
	{
		return new LocalVariableHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(LocalVariableHandle handle)
	{
		return new System.Reflection.Metadata.Handle(51, handle._rowId);
	}

	public static implicit operator EntityHandle(LocalVariableHandle handle)
	{
		return new EntityHandle((uint)(0x33000000uL | (ulong)handle._rowId));
	}

	public static explicit operator LocalVariableHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 51)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new LocalVariableHandle(handle.RowId);
	}

	public static explicit operator LocalVariableHandle(EntityHandle handle)
	{
		if (handle.VType != 855638016)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new LocalVariableHandle(handle.RowId);
	}

	public static bool operator ==(LocalVariableHandle left, LocalVariableHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is LocalVariableHandle localVariableHandle)
		{
			return localVariableHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(LocalVariableHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(LocalVariableHandle left, LocalVariableHandle right)
	{
		return left._rowId != right._rowId;
	}
}
