namespace System.Reflection.Metadata;

internal readonly struct ManifestResourceHandle : IEquatable<ManifestResourceHandle>
{
	private const uint tokenType = 671088640u;

	private const byte tokenTypeSmall = 40;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ManifestResourceHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ManifestResourceHandle FromRowId(int rowId)
	{
		return new ManifestResourceHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(ManifestResourceHandle handle)
	{
		return new System.Reflection.Metadata.Handle(40, handle._rowId);
	}

	public static implicit operator EntityHandle(ManifestResourceHandle handle)
	{
		return new EntityHandle((uint)(0x28000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ManifestResourceHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 40)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ManifestResourceHandle(handle.RowId);
	}

	public static explicit operator ManifestResourceHandle(EntityHandle handle)
	{
		if (handle.VType != 671088640)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ManifestResourceHandle(handle.RowId);
	}

	public static bool operator ==(ManifestResourceHandle left, ManifestResourceHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is ManifestResourceHandle)
		{
			return ((ManifestResourceHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ManifestResourceHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ManifestResourceHandle left, ManifestResourceHandle right)
	{
		return left._rowId != right._rowId;
	}
}
