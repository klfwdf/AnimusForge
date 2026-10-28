namespace System.Reflection.Metadata;

internal readonly struct DocumentHandle : IEquatable<System.Reflection.Metadata.DocumentHandle>
{
	private const uint tokenType = 805306368u;

	private const byte tokenTypeSmall = 48;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private DocumentHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static System.Reflection.Metadata.DocumentHandle FromRowId(int rowId)
	{
		return new System.Reflection.Metadata.DocumentHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(System.Reflection.Metadata.DocumentHandle handle)
	{
		return new System.Reflection.Metadata.Handle(48, handle._rowId);
	}

	public static implicit operator EntityHandle(System.Reflection.Metadata.DocumentHandle handle)
	{
		return new EntityHandle((uint)(0x30000000uL | (ulong)handle._rowId));
	}

	public static explicit operator System.Reflection.Metadata.DocumentHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 48)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.DocumentHandle(handle.RowId);
	}

	public static explicit operator System.Reflection.Metadata.DocumentHandle(EntityHandle handle)
	{
		if (handle.VType != 805306368)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.DocumentHandle(handle.RowId);
	}

	public static bool operator ==(System.Reflection.Metadata.DocumentHandle left, System.Reflection.Metadata.DocumentHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is System.Reflection.Metadata.DocumentHandle documentHandle)
		{
			return documentHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.DocumentHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(System.Reflection.Metadata.DocumentHandle left, System.Reflection.Metadata.DocumentHandle right)
	{
		return left._rowId != right._rowId;
	}
}
