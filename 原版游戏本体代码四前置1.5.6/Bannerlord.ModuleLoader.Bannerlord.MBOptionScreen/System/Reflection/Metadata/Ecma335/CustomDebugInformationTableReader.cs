using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomDebugInformationTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isHasCustomDebugInformationRefSizeSmall;

	private readonly bool _isGuidHeapRefSizeSmall;

	private readonly bool _isBlobHeapRefSizeSmall;

	private const int ParentOffset = 0;

	private readonly int _kindOffset;

	private readonly int _valueOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal CustomDebugInformationTableReader(int numberOfRows, bool declaredSorted, int hasCustomDebugInformationRefSize, int guidHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isHasCustomDebugInformationRefSizeSmall = hasCustomDebugInformationRefSize == 2;
		_isGuidHeapRefSizeSmall = guidHeapRefSize == 2;
		_isBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_kindOffset = hasCustomDebugInformationRefSize;
		_valueOffset = _kindOffset + guidHeapRefSize;
		RowSize = _valueOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (numberOfRows > 0 && !declaredSorted)
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.CustomDebugInformation);
		}
	}

	internal EntityHandle GetParent(CustomDebugInformationHandle handle)
	{
		int offset = (handle.RowId - 1) * RowSize;
		return HasCustomDebugInformationTag.ConvertToHandle(Block.PeekTaggedReference(offset, _isHasCustomDebugInformationRefSizeSmall));
	}

	internal GuidHandle GetKind(CustomDebugInformationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return GuidHandle.FromIndex(Block.PeekHeapReference(num + _kindOffset, _isGuidHeapRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetValue(CustomDebugInformationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _valueOffset, _isBlobHeapRefSizeSmall));
	}

	internal void GetRange(EntityHandle parentHandle, out int firstImplRowId, out int lastImplRowId)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, 0, HasCustomDebugInformationTag.ConvertToTag(parentHandle), _isHasCustomDebugInformationRefSizeSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			firstImplRowId = 1;
			lastImplRowId = 0;
		}
		else
		{
			firstImplRowId = startRowNumber + 1;
			lastImplRowId = endRowNumber + 1;
		}
	}
}
