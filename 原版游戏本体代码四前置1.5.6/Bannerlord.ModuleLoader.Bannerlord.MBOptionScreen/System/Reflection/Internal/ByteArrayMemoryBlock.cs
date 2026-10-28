using System.Collections.Immutable;

namespace System.Reflection.Internal;

internal sealed class ByteArrayMemoryBlock : System.Reflection.Internal.AbstractMemoryBlock
{
	private System.Reflection.Internal.ByteArrayMemoryProvider _provider;

	private readonly int _start;

	private readonly int _size;

	public unsafe override byte* Pointer => _provider.Pointer + _start;

	public override int Size => _size;

	internal ByteArrayMemoryBlock(System.Reflection.Internal.ByteArrayMemoryProvider provider, int start, int size)
	{
		_provider = provider;
		_size = size;
		_start = start;
	}

	public override void Dispose()
	{
		_provider = null;
	}

	public override System.Collections.Immutable.ImmutableArray<byte> GetContentUnchecked(int start, int length)
	{
		return System.Collections.Immutable.ImmutableArray.Create(_provider.Array, _start + start, length);
	}
}
