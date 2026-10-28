using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

internal readonly struct PEMemoryBlock
{
	private readonly System.Reflection.Internal.AbstractMemoryBlock _block;

	private readonly int _offset;

	public unsafe byte* Pointer
	{
		get
		{
			if (_block == null)
			{
				return null;
			}
			return _block.Pointer + _offset;
		}
	}

	public int Length => (_block?.Size - _offset).GetValueOrDefault();

	internal PEMemoryBlock(System.Reflection.Internal.AbstractMemoryBlock block, int offset = 0)
	{
		_block = block;
		_offset = offset;
	}

	public unsafe System.Reflection.Metadata.BlobReader GetReader()
	{
		return new System.Reflection.Metadata.BlobReader(Pointer, Length);
	}

	public unsafe System.Reflection.Metadata.BlobReader GetReader(int start, int length)
	{
		System.Reflection.BlobUtilities.ValidateRange(Length, start, length, "length");
		return new System.Reflection.Metadata.BlobReader(Pointer + start, length);
	}

	public System.Collections.Immutable.ImmutableArray<byte> GetContent()
	{
		return _block?.GetContentUnchecked(_offset, Length) ?? System.Collections.Immutable.ImmutableArray<byte>.Empty;
	}

	public System.Collections.Immutable.ImmutableArray<byte> GetContent(int start, int length)
	{
		System.Reflection.BlobUtilities.ValidateRange(Length, start, length, "length");
		return _block?.GetContentUnchecked(_offset + start, length) ?? System.Collections.Immutable.ImmutableArray<byte>.Empty;
	}
}
