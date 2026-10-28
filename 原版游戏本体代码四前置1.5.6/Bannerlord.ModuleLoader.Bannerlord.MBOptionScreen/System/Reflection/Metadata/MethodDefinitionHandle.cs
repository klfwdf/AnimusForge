namespace System.Reflection.Metadata;

internal readonly struct MethodDefinitionHandle : IEquatable<System.Reflection.Metadata.MethodDefinitionHandle>
{
	private const uint tokenType = 100663296u;

	private const byte tokenTypeSmall = 6;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private MethodDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static System.Reflection.Metadata.MethodDefinitionHandle FromRowId(int rowId)
	{
		return new System.Reflection.Metadata.MethodDefinitionHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.Handle(6, handle._rowId);
	}

	public static implicit operator EntityHandle(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x6000000uL | (ulong)handle._rowId));
	}

	public static explicit operator System.Reflection.Metadata.MethodDefinitionHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 6)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.MethodDefinitionHandle(handle.RowId);
	}

	public static explicit operator System.Reflection.Metadata.MethodDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 100663296)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.MethodDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(System.Reflection.Metadata.MethodDefinitionHandle left, System.Reflection.Metadata.MethodDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is System.Reflection.Metadata.MethodDefinitionHandle)
		{
			return ((System.Reflection.Metadata.MethodDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.MethodDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(System.Reflection.Metadata.MethodDefinitionHandle left, System.Reflection.Metadata.MethodDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}

	public System.Reflection.Metadata.MethodDebugInformationHandle ToDebugInformationHandle()
	{
		return System.Reflection.Metadata.MethodDebugInformationHandle.FromRowId(_rowId);
	}
}
