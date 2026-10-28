using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace System.Reflection.Internal;

internal abstract class AbstractMemoryBlock : IDisposable
{
	public unsafe abstract byte* Pointer { get; }

	public abstract int Size { get; }

	public unsafe System.Reflection.Metadata.BlobReader GetReader()
	{
		return new System.Reflection.Metadata.BlobReader(Pointer, Size);
	}

	public unsafe virtual System.Collections.Immutable.ImmutableArray<byte> GetContentUnchecked(int start, int length)
	{
		System.Collections.Immutable.ImmutableArray<byte> result = new ReadOnlySpan<byte>(Pointer + start, length).ToImmutableArray();
		GC.KeepAlive(this);
		return result;
	}

	public abstract void Dispose();
}
