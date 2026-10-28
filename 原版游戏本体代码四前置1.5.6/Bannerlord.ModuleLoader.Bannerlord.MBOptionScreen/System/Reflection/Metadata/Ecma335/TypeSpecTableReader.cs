using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct TypeSpecTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _SignatureOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal TypeSpecTableReader(int numberOfRows, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_SignatureOffset = 0;
		RowSize = _SignatureOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal System.Reflection.Metadata.BlobHandle GetSignature(TypeSpecificationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _SignatureOffset, _IsBlobHeapRefSizeSmall));
	}
}
