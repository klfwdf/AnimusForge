using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct NestedClassTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly int _NestedClassOffset;

	private readonly int _EnclosingClassOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal NestedClassTableReader(int numberOfRows, bool declaredSorted, int typeDefTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_NestedClassOffset = 0;
		_EnclosingClassOffset = _NestedClassOffset + typeDefTableRowRefSize;
		RowSize = _EnclosingClassOffset + typeDefTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.NestedClass);
		}
	}

	internal TypeDefinitionHandle GetNestedClass(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _NestedClassOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal TypeDefinitionHandle GetEnclosingClass(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _EnclosingClassOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal TypeDefinitionHandle FindEnclosingType(TypeDefinitionHandle nestedTypeDef)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, _NestedClassOffset, (uint)nestedTypeDef.RowId, _IsTypeDefTableRowRefSizeSmall);
		if (num == -1)
		{
			return default(TypeDefinitionHandle);
		}
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num * RowSize + _EnclosingClassOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _NestedClassOffset, _IsTypeDefTableRowRefSizeSmall);
	}
}
