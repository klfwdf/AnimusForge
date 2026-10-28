using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ImportScopeTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isImportScopeRefSizeSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private const int ParentOffset = 0;

	private readonly int _importsOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ImportScopeTableReader(int numberOfRows, int importScopeRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isImportScopeRefSizeSmall = importScopeRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_importsOffset = importScopeRefSize;
		RowSize = _importsOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal ImportScopeHandle GetParent(ImportScopeHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return ImportScopeHandle.FromRowId(Block.PeekReference(offset, _isImportScopeRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetImports(ImportScopeHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _importsOffset, _isBlobHeapRefSizeSmall));
	}
}
