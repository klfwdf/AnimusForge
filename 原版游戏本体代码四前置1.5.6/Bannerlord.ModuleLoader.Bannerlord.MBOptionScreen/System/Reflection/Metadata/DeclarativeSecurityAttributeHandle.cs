namespace System.Reflection.Metadata;

internal readonly struct DeclarativeSecurityAttributeHandle : IEquatable<DeclarativeSecurityAttributeHandle>
{
	private const uint tokenType = 234881024u;

	private const byte tokenTypeSmall = 14;

	private readonly int _rowId;

	public bool IsNil => _rowId == 0;

	internal int RowId => _rowId;

	private DeclarativeSecurityAttributeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static DeclarativeSecurityAttributeHandle FromRowId(int rowId)
	{
		return new DeclarativeSecurityAttributeHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(DeclarativeSecurityAttributeHandle handle)
	{
		return new System.Reflection.Metadata.Handle(14, handle._rowId);
	}

	public static implicit operator EntityHandle(DeclarativeSecurityAttributeHandle handle)
	{
		return new EntityHandle((uint)(0xE000000uL | (ulong)handle._rowId));
	}

	public static explicit operator DeclarativeSecurityAttributeHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 14)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new DeclarativeSecurityAttributeHandle(handle.RowId);
	}

	public static explicit operator DeclarativeSecurityAttributeHandle(EntityHandle handle)
	{
		if (handle.VType != 234881024)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new DeclarativeSecurityAttributeHandle(handle.RowId);
	}

	public static bool operator ==(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is DeclarativeSecurityAttributeHandle)
		{
			return ((DeclarativeSecurityAttributeHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(DeclarativeSecurityAttributeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
