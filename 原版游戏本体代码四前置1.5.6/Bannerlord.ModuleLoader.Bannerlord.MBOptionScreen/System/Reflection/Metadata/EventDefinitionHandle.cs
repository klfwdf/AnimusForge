namespace System.Reflection.Metadata;

internal readonly struct EventDefinitionHandle : IEquatable<EventDefinitionHandle>
{
	private const uint tokenType = 335544320u;

	private const byte tokenTypeSmall = 20;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private EventDefinitionHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static EventDefinitionHandle FromRowId(int rowId)
	{
		return new EventDefinitionHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(EventDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.Handle(20, handle._rowId);
	}

	public static implicit operator EntityHandle(EventDefinitionHandle handle)
	{
		return new EntityHandle((uint)(0x14000000uL | (ulong)handle._rowId));
	}

	public static explicit operator EventDefinitionHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 20)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new EventDefinitionHandle(handle.RowId);
	}

	public static explicit operator EventDefinitionHandle(EntityHandle handle)
	{
		if (handle.VType != 335544320)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new EventDefinitionHandle(handle.RowId);
	}

	public static bool operator ==(EventDefinitionHandle left, EventDefinitionHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals(object? obj)
	{
		if (obj is EventDefinitionHandle)
		{
			return ((EventDefinitionHandle)obj)._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(EventDefinitionHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(EventDefinitionHandle left, EventDefinitionHandle right)
	{
		return left._rowId != right._rowId;
	}
}
