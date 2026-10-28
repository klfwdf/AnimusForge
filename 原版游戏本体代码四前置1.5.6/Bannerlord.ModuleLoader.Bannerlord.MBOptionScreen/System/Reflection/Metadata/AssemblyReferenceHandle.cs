namespace System.Reflection.Metadata;

internal readonly struct AssemblyReferenceHandle : IEquatable<AssemblyReferenceHandle>
{
	internal enum VirtualIndex
	{
		System_Runtime,
		System_Runtime_InteropServices_WindowsRuntime,
		System_ObjectModel,
		System_Runtime_WindowsRuntime,
		System_Runtime_WindowsRuntime_UI_Xaml,
		System_Numerics_Vectors,
		Count
	}

	private const uint tokenType = 587202560u;

	private const byte tokenTypeSmall = 35;

	private readonly uint _value;

	internal uint Value => _value;

	private uint VToken => _value | 0x23000000;

	public bool IsNil => _value == 0;

	internal bool IsVirtual => (_value & 0x80000000u) != 0;

	internal int RowId => (int)(_value & 0xFFFFFF);

	private AssemblyReferenceHandle(uint value)
	{
		_value = value;
	}

	internal static AssemblyReferenceHandle FromRowId(int rowId)
	{
		return new AssemblyReferenceHandle((uint)rowId);
	}

	internal static AssemblyReferenceHandle FromVirtualIndex(VirtualIndex virtualIndex)
	{
		return new AssemblyReferenceHandle((uint)((VirtualIndex)int.MinValue | virtualIndex));
	}

	public static implicit operator System.Reflection.Metadata.Handle(AssemblyReferenceHandle handle)
	{
		return System.Reflection.Metadata.Handle.FromVToken(handle.VToken);
	}

	public static implicit operator EntityHandle(AssemblyReferenceHandle handle)
	{
		return new EntityHandle(handle.VToken);
	}

	public static explicit operator AssemblyReferenceHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.Type != 35)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new AssemblyReferenceHandle(handle.SpecificEntityHandleValue);
	}

	public static explicit operator AssemblyReferenceHandle(EntityHandle handle)
	{
		if (handle.Type != 587202560)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new AssemblyReferenceHandle(handle.SpecificHandleValue);
	}

	public static bool operator ==(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
	{
		return left._value == right._value;
	}

	public override bool Equals(object? obj)
	{
		if (obj is AssemblyReferenceHandle)
		{
			return ((AssemblyReferenceHandle)obj)._value == _value;
		}
		return false;
	}

	public bool Equals(AssemblyReferenceHandle other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	public static bool operator !=(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
	{
		return left._value != right._value;
	}
}
