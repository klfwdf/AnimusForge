namespace System.Reflection.Metadata;

internal readonly struct MethodDebugInformationHandle : IEquatable<System.Reflection.Metadata.MethodDebugInformationHandle>
{
	private const uint tokenType = 822083584u;

	private const byte tokenTypeSmall = 49;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodDebugInformationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static System.Reflection.Metadata.MethodDebugInformationHandle FromRowId(int rowId)
	{
		return new System.Reflection.Metadata.MethodDebugInformationHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		return new System.Reflection.Metadata.Handle(49, handle._rowId);
	}

	public static implicit operator EntityHandle(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		return new EntityHandle((uint)(0x31000000uL | (ulong)handle._rowId));
	}

	public static explicit operator System.Reflection.Metadata.MethodDebugInformationHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 49)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.MethodDebugInformationHandle(handle.RowId);
	}

	public static explicit operator System.Reflection.Metadata.MethodDebugInformationHandle(EntityHandle handle)
	{
		if (handle.VType != 822083584)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.MethodDebugInformationHandle(handle.RowId);
	}

	public static bool operator ==(System.Reflection.Metadata.MethodDebugInformationHandle left, System.Reflection.Metadata.MethodDebugInformationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is System.Reflection.Metadata.MethodDebugInformationHandle methodDebugInformationHandle)
		{
			return methodDebugInformationHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.MethodDebugInformationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(System.Reflection.Metadata.MethodDebugInformationHandle left, System.Reflection.Metadata.MethodDebugInformationHandle right)
	{
		return left._rowId != right._rowId;
	}

	public System.Reflection.Metadata.MethodDefinitionHandle ToDefinitionHandle()
	{
		return System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_rowId);
	}
}
