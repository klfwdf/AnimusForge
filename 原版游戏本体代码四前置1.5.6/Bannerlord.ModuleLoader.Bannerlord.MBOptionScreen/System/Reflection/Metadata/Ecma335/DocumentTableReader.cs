using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct DocumentTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isGuidHeapRefSizeSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private const int NameOffset = 0;

	private readonly int _hashAlgorithmOffset;

	private readonly int _hashOffset;

	private readonly int _languageOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal DocumentTableReader(int numberOfRows, int guidHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isGuidHeapRefSizeSmall = guidHeapRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_hashAlgorithmOffset = blobHeapRefSize;
		_hashOffset = _hashAlgorithmOffset + guidHeapRefSize;
		_languageOffset = _hashOffset + blobHeapRefSize;
		RowSize = _languageOffset + guidHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal System.Reflection.Metadata.DocumentNameBlobHandle GetName(System.Reflection.Metadata.DocumentHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.DocumentNameBlobHandle.FromOffset(Block.PeekHeapReference(offset, _isBlobHeapRefSizeSmall));
	}

	internal GuidHandle GetHashAlgorithm(System.Reflection.Metadata.DocumentHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return GuidHandle.FromIndex(Block.PeekHeapReference(num + _hashAlgorithmOffset, _isGuidHeapRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetHash(System.Reflection.Metadata.DocumentHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _hashOffset, _isBlobHeapRefSizeSmall));
	}

	internal GuidHandle GetLanguage(System.Reflection.Metadata.DocumentHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return GuidHandle.FromIndex(Block.PeekHeapReference(num + _languageOffset, _isGuidHeapRefSizeSmall));
	}
}
