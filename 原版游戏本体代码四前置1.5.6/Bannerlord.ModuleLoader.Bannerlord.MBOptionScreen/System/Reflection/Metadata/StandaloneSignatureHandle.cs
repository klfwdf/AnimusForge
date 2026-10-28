namespace System.Reflection.Metadata;

internal readonly struct StandaloneSignatureHandle : IEquatable<StandaloneSignatureHandle>
{
	private const uint tokenType = 285212672u;

	private const byte tokenTypeSmall = 17;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private StandaloneSignatureHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static StandaloneSignatureHandle FromRowId(int rowId)
	{
		return new StandaloneSignatureHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(StandaloneSignatureHandle handle)
	{
		return new System.Reflection.Metadata.Handle(17, handle._rowId);
	}

	public static implicit operator EntityHandle(StandaloneSignatureHandle handle)
	{
		return new EntityHandle((uint)(0x11000000uL | (ulong)handle._rowId));
	}

	public static explicit operator StandaloneSignatureHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 17)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new StandaloneSignatureHandle(handle.RowId);
	}

	public static explicit operator StandaloneSignatureHandle(EntityHandle handle)
	{
		if (handle.VType != 285212672)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new StandaloneSignatureHandle(handle.RowId);
	}

	public static bool operator ==(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is StandaloneSignatureHandle)
		{
			return ((StandaloneSignatureHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(StandaloneSignatureHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
	{
		return left._rowId != right._rowId;
	}
}
