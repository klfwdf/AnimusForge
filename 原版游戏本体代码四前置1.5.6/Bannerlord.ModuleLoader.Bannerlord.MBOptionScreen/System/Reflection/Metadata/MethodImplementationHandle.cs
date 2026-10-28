namespace System.Reflection.Metadata;

internal readonly struct MethodImplementationHandle : IEquatable<MethodImplementationHandle>
{
	private const uint tokenType = 419430400u;

	private const byte tokenTypeSmall = 25;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodImplementationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static MethodImplementationHandle FromRowId(int rowId)
	{
		return new MethodImplementationHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(MethodImplementationHandle handle)
	{
		return new System.Reflection.Metadata.Handle(25, handle._rowId);
	}

	public static implicit operator EntityHandle(MethodImplementationHandle handle)
	{
		return new EntityHandle((uint)(0x19000000uL | (ulong)handle._rowId));
	}

	public static explicit operator MethodImplementationHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 25)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new MethodImplementationHandle(handle.RowId);
	}

	public static explicit operator MethodImplementationHandle(EntityHandle handle)
	{
		if (handle.VType != 419430400)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new MethodImplementationHandle(handle.RowId);
	}

	public static bool operator ==(MethodImplementationHandle left, MethodImplementationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is MethodImplementationHandle)
		{
			return ((MethodImplementationHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(MethodImplementationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(MethodImplementationHandle left, MethodImplementationHandle right)
	{
		return left._rowId != right._rowId;
	}
}
