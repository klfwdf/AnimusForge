using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsHasCustomAttributeRefSizeSmall;

	private readonly bool _IsCustomAttributeTypeRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _ParentOffset;

	private readonly int _TypeOffset;

	private readonly int _ValueOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal readonly int[]? PtrTable;

	internal CustomAttributeTableReader(int numberOfRows, bool declaredSorted, int hasCustomAttributeRefSize, int customAttributeTypeRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsHasCustomAttributeRefSizeSmall = hasCustomAttributeRefSize == 2;
		_IsCustomAttributeTypeRefSizeSmall = customAttributeTypeRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_ParentOffset = 0;
		_TypeOffset = _ParentOffset + hasCustomAttributeRefSize;
		_ValueOffset = _TypeOffset + customAttributeTypeRefSize;
		RowSize = _ValueOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		PtrTable = null;
		if (!declaredSorted && !CheckSorted())
		{
			PtrTable = Block.BuildPtrTable(numberOfRows, RowSize, _ParentOffset, _IsHasCustomAttributeRefSizeSmall);
		}
	}

	internal EntityHandle GetParent(CustomAttributeHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return HasCustomAttributeTag.ConvertToHandle(Block.PeekTaggedReference(num + _ParentOffset, _IsHasCustomAttributeRefSizeSmall));
	}

	internal EntityHandle GetConstructor(CustomAttributeHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return CustomAttributeTypeTag.ConvertToHandle(Block.PeekTaggedReference(num + _TypeOffset, _IsCustomAttributeTypeRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetValue(CustomAttributeHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _ValueOffset, _IsBlobHeapRefSizeSmall));
	}

	internal void GetAttributeRange(EntityHandle parentHandle, out int firstImplRowId, out int lastImplRowId)
	{
		int startRowNumber;
		int endRowNumber;
		if (PtrTable != null)
		{
			Block.BinarySearchReferenceRange(PtrTable, RowSize, _ParentOffset, HasCustomAttributeTag.ConvertToTag(parentHandle), _IsHasCustomAttributeRefSizeSmall, out startRowNumber, out endRowNumber);
		}
		else
		{
			Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _ParentOffset, HasCustomAttributeTag.ConvertToTag(parentHandle), _IsHasCustomAttributeRefSizeSmall, out startRowNumber, out endRowNumber);
		}
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

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ParentOffset, _IsHasCustomAttributeRefSizeSmall);
	}
}
