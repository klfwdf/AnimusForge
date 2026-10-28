using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FieldPtrTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsFieldTableRowRefSizeSmall;

	private readonly int _FieldOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal FieldPtrTableReader(int numberOfRows, int fieldTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsFieldTableRowRefSizeSmall = fieldTableRowRefSize == 2;
		_FieldOffset = 0;
		RowSize = _FieldOffset + fieldTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal FieldDefinitionHandle GetFieldFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return FieldDefinitionHandle.FromRowId(Block.PeekReference(num + _FieldOffset, _IsFieldTableRowRefSizeSmall));
	}

	internal int GetRowIdForFieldDefRow(int fieldDefRowId)
	{
		return Block.LinearSearchReference(RowSize, _FieldOffset, (uint)fieldDefRowId, _IsFieldTableRowRefSizeSmall) + 1;
	}
}
