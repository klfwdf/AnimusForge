using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct NamespaceDefinitionHandle : IEquatable<NamespaceDefinitionHandle>
{
	private readonly uint _value;

	public bool IsNil => _value == 0;

	internal bool IsVirtual => (_value & 0x80000000u) != 0;

	internal bool HasFullName => !IsVirtual;

	private NamespaceDefinitionHandle(uint value)
	{
		_value = value;
	}

	internal static NamespaceDefinitionHandle FromFullNameOffset(int stringHeapOffset)
	{
		return new NamespaceDefinitionHandle((uint)stringHeapOffset);
	}

	internal static NamespaceDefinitionHandle FromVirtualIndex(uint virtualIndex)
	{
		if (!System.Reflection.Metadata.Ecma335.HeapHandleType.IsValidHeapOffset(virtualIndex))
		{
			System.Reflection.Throw.TooManySubnamespaces();
		}
		return new NamespaceDefinitionHandle(0x80000000u | virtualIndex);
	}

	public static implicit operator System.Reflection.Metadata.Handle(NamespaceDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.Handle((byte)(((handle._value & 0x80000000u) >> 24) | 0x7C), (int)(handle._value & 0x1FFFFFFF));
	}

	public static explicit operator NamespaceDefinitionHandle(System.Reflection.Metadata.Handle handle)
	{
		if ((handle.VType & 0x7F) != 124)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new NamespaceDefinitionHandle((uint)(((handle.VType & 0x80) << 24) | handle.Offset));
	}

	internal int GetHeapOffset()
	{
		return (int)(_value & 0x1FFFFFFF);
	}

	internal StringHandle GetFullName()
	{
		return StringHandle.FromOffset(GetHeapOffset());
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is NamespaceDefinitionHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(NamespaceDefinitionHandle other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return (int)_value;
	}

	public static bool operator ==(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
	{
		return !left.Equals(right);
	}
}
