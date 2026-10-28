using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct EventMapTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly bool _IsEventRefSizeSmall;

	private readonly int _ParentOffset;

	private readonly int _EventListOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal EventMapTableReader(int numberOfRows, int typeDefTableRowRefSize, int eventRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_IsEventRefSizeSmall = eventRefSize == 2;
		_ParentOffset = 0;
		_EventListOffset = _ParentOffset + typeDefTableRowRefSize;
		RowSize = _EventListOffset + eventRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal int FindEventMapRowIdFor(TypeDefinitionHandle typeDef)
	{
		return Block.LinearSearchReference(RowSize, _ParentOffset, (uint)typeDef.RowId, _IsTypeDefTableRowRefSizeSmall) + 1;
	}

	internal TypeDefinitionHandle GetParentType(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal int GetEventListStartFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _EventListOffset, _IsEventRefSizeSmall);
	}
}
