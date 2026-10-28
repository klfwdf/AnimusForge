using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FileTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _HashValueOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal FileTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_FlagsOffset = 0;
		_NameOffset = _FlagsOffset + 4;
		_HashValueOffset = _NameOffset + stringHeapRefSize;
		RowSize = _HashValueOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal System.Reflection.Metadata.BlobHandle GetHashValue(AssemblyFileHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _HashValueOffset, _IsBlobHeapRefSizeSmall));
	}

	internal uint GetFlags(AssemblyFileHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekUInt32(num + _FlagsOffset);
	}

	internal StringHandle GetName(AssemblyFileHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}
}
