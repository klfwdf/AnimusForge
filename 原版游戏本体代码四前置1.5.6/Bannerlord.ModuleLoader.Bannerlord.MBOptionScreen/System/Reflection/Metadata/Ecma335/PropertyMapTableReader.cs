using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct PropertyMapTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly bool _IsPropertyRefSizeSmall;

	private readonly int _ParentOffset;

	private readonly int _PropertyListOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal PropertyMapTableReader(int numberOfRows, int typeDefTableRowRefSize, int propertyRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_IsPropertyRefSizeSmall = propertyRefSize == 2;
		_ParentOffset = 0;
		_PropertyListOffset = _ParentOffset + typeDefTableRowRefSize;
		RowSize = _PropertyListOffset + propertyRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal int FindPropertyMapRowIdFor(TypeDefinitionHandle typeDef)
	{
		return Block.LinearSearchReference(RowSize, _ParentOffset, (uint)typeDef.RowId, _IsTypeDefTableRowRefSizeSmall) + 1;
	}

	internal TypeDefinitionHandle GetParentType(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal int GetPropertyListStartFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _PropertyListOffset, _IsPropertyRefSizeSmall);
	}
}
