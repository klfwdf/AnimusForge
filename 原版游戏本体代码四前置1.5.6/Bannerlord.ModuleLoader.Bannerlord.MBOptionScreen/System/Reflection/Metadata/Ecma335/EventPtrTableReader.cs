using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct EventPtrTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsEventTableRowRefSizeSmall;

	private readonly int _EventOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal EventPtrTableReader(int numberOfRows, int eventTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsEventTableRowRefSizeSmall = eventTableRowRefSize == 2;
		_EventOffset = 0;
		RowSize = _EventOffset + eventTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal EventDefinitionHandle GetEventFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return EventDefinitionHandle.FromRowId(Block.PeekReference(num + _EventOffset, _IsEventTableRowRefSizeSmall));
	}
}
