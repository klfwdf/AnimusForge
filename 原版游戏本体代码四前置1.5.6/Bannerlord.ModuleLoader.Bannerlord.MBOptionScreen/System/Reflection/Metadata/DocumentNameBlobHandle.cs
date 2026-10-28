using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal readonly struct DocumentNameBlobHandle : IEquatable<System.Reflection.Metadata.DocumentNameBlobHandle>
{
	private readonly int _heapOffset;

	public bool IsNil => _heapOffset == 0;

	private DocumentNameBlobHandle(int heapOffset)
	{
		_heapOffset = heapOffset;
	}

	internal static System.Reflection.Metadata.DocumentNameBlobHandle FromOffset(int heapOffset)
	{
		return new System.Reflection.Metadata.DocumentNameBlobHandle(heapOffset);
	}

	public static implicit operator System.Reflection.Metadata.BlobHandle(System.Reflection.Metadata.DocumentNameBlobHandle handle)
	{
		return System.Reflection.Metadata.BlobHandle.FromOffset(handle._heapOffset);
	}

	public static explicit operator System.Reflection.Metadata.DocumentNameBlobHandle(System.Reflection.Metadata.BlobHandle handle)
	{
		if (handle.IsVirtual)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return FromOffset(handle.GetHeapOffset());
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is System.Reflection.Metadata.DocumentNameBlobHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.DocumentNameBlobHandle other)
	{
		return _heapOffset == other._heapOffset;
	}

	public override int GetHashCode()
	{
		return _heapOffset;
	}

	public static bool operator ==(System.Reflection.Metadata.DocumentNameBlobHandle left, System.Reflection.Metadata.DocumentNameBlobHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(System.Reflection.Metadata.DocumentNameBlobHandle left, System.Reflection.Metadata.DocumentNameBlobHandle right)
	{
		return !left.Equals(right);
	}
}
