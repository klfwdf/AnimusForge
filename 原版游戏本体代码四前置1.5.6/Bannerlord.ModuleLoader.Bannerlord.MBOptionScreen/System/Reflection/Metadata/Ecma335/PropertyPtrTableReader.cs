using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct PropertyPtrTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsPropertyTableRowRefSizeSmall;

	private readonly int _PropertyOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal PropertyPtrTableReader(int numberOfRows, int propertyTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsPropertyTableRowRefSizeSmall = propertyTableRowRefSize == 2;
		_PropertyOffset = 0;
		RowSize = _PropertyOffset + propertyTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal PropertyDefinitionHandle GetPropertyFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return PropertyDefinitionHandle.FromRowId(Block.PeekReference(num + _PropertyOffset, _IsPropertyTableRowRefSizeSmall));
	}
}
