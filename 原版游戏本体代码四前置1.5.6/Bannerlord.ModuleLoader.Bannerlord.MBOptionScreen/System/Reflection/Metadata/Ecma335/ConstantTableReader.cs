using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ConstantTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsHasConstantRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _TypeOffset;

	private readonly int _ParentOffset;

	private readonly int _ValueOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ConstantTableReader(int numberOfRows, bool declaredSorted, int hasConstantRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsHasConstantRefSizeSmall = hasConstantRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_TypeOffset = 0;
		_ParentOffset = _TypeOffset + 1 + 1;
		_ValueOffset = _ParentOffset + hasConstantRefSize;
		RowSize = _ValueOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.Constant);
		}
	}

	internal ConstantTypeCode GetType(ConstantHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (ConstantTypeCode)Block.PeekByte(num + _TypeOffset);
	}

	internal System.Reflection.Metadata.BlobHandle GetValue(ConstantHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _ValueOffset, _IsBlobHeapRefSizeSmall));
	}

	internal EntityHandle GetParent(ConstantHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return HasConstantTag.ConvertToHandle(Block.PeekTaggedReference(num + _ParentOffset, _IsHasConstantRefSizeSmall));
	}

	internal ConstantHandle FindConstant(EntityHandle parentHandle)
	{
		return ConstantHandle.FromRowId(Block.BinarySearchReference(NumberOfRows, RowSize, _ParentOffset, HasConstantTag.ConvertToTag(parentHandle), _IsHasConstantRefSizeSmall) + 1);
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ParentOffset, _IsHasConstantRefSizeSmall);
	}
}
