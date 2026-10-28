using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal readonly struct BlobHandle : IEquatable<System.Reflection.Metadata.BlobHandle>
{
	internal enum VirtualIndex : byte
	{
		Nil,
		ContractPublicKeyToken,
		ContractPublicKey,
		AttributeUsage_AllowSingle,
		AttributeUsage_AllowMultiple,
		Count
	}

	private readonly uint _value;

	internal const int TemplateParameterOffset_AttributeUsageTarget = 2;

	internal uint RawValue => _value;

	public bool IsNil => _value == 0;

	internal bool IsVirtual => (_value & 0x80000000u) != 0;

	private ushort VirtualValue => (ushort)(_value >> 8);

	private BlobHandle(uint value)
	{
		_value = value;
	}

	internal static System.Reflection.Metadata.BlobHandle FromOffset(int heapOffset)
	{
		return new System.Reflection.Metadata.BlobHandle((uint)heapOffset);
	}

	internal static System.Reflection.Metadata.BlobHandle FromVirtualIndex(VirtualIndex virtualIndex, ushort virtualValue)
	{
		return new System.Reflection.Metadata.BlobHandle((uint)(int.MinValue | (virtualValue << 8)) | (uint)virtualIndex);
	}

	internal unsafe void SubstituteTemplateParameters(byte[] blob)
	{
		fixed (byte* ptr = &blob[2])
		{
			*(int*)ptr = VirtualValue;
		}
	}

	public static implicit operator System.Reflection.Metadata.Handle(System.Reflection.Metadata.BlobHandle handle)
	{
		return new System.Reflection.Metadata.Handle((byte)(((handle._value & 0x80000000u) >> 24) | 0x71), (int)(handle._value & 0x1FFFFFFF));
	}

	public static explicit operator System.Reflection.Metadata.BlobHandle(System.Reflection.Metadata.Handle handle)
	{
		if ((handle.VType & 0x7F) != 113)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new System.Reflection.Metadata.BlobHandle((uint)(((handle.VType & 0x80) << 24) | handle.Offset));
	}

	internal int GetHeapOffset()
	{
		return (int)_value;
	}

	internal VirtualIndex GetVirtualIndex()
	{
		return (VirtualIndex)(_value & 0xFF);
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is System.Reflection.Metadata.BlobHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.BlobHandle other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return (int)_value;
	}

	public static bool operator ==(System.Reflection.Metadata.BlobHandle left, System.Reflection.Metadata.BlobHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(System.Reflection.Metadata.BlobHandle left, System.Reflection.Metadata.BlobHandle right)
	{
		return !left.Equals(right);
	}
}
