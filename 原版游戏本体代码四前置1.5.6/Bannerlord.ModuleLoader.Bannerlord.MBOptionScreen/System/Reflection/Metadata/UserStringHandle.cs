namespace System.Reflection.Metadata;

internal readonly struct UserStringHandle : IEquatable<UserStringHandle>
{
	private readonly int _offset;

	public bool IsNil => _offset == 0;

	private UserStringHandle(int offset)
	{
		_offset = offset;
	}

	internal static UserStringHandle FromOffset(int heapOffset)
	{
		return new UserStringHandle(heapOffset);
	}

	public static implicit operator System.Reflection.Metadata.Handle(UserStringHandle handle)
	{
		return new System.Reflection.Metadata.Handle(112, handle._offset);
	}

	public static explicit operator UserStringHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 112)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new UserStringHandle(handle.Offset);
	}

	internal int GetHeapOffset()
	{
		return _offset;
	}

	public static bool operator ==(UserStringHandle left, UserStringHandle right)
	{
		return left._offset == right._offset;
	}

	public override bool Equals(object? obj)
	{
		if (obj is UserStringHandle)
		{
			return ((UserStringHandle)obj)._offset == _offset;
		}
		return false;
	}

	public bool Equals(UserStringHandle other)
	{
		return _offset == other._offset;
	}

	public override int GetHashCode()
	{
		return _offset.GetHashCode();
	}

	public static bool operator !=(UserStringHandle left, UserStringHandle right)
	{
		return left._offset != right._offset;
	}
}
