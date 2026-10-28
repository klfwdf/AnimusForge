using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodDebugInformationTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isDocumentRefSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private const int DocumentOffset = 0;

	private readonly int _sequencePointsOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal MethodDebugInformationTableReader(int numberOfRows, int documentRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isDocumentRefSmall = documentRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_sequencePointsOffset = documentRefSize;
		RowSize = _sequencePointsOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal System.Reflection.Metadata.DocumentHandle GetDocument(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.DocumentHandle.FromRowId(Block.PeekReference(offset, _isDocumentRefSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetSequencePoints(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _sequencePointsOffset, _isBlobHeapRefSizeSmall));
	}
}
