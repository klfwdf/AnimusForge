using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct InterfaceImplTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly bool _IsTypeDefOrRefRefSizeSmall;

	private readonly int _ClassOffset;

	private readonly int _InterfaceOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal InterfaceImplTableReader(int numberOfRows, bool declaredSorted, int typeDefTableRowRefSize, int typeDefOrRefRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
		_ClassOffset = 0;
		_InterfaceOffset = _ClassOffset + typeDefTableRowRefSize;
		RowSize = _InterfaceOffset + typeDefOrRefRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl);
		}
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ClassOffset, _IsTypeDefTableRowRefSizeSmall);
	}

	internal void GetInterfaceImplRange(TypeDefinitionHandle typeDef, out int firstImplRowId, out int lastImplRowId)
	{
		int rowId = typeDef.RowId;
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _ClassOffset, (uint)rowId, _IsTypeDefTableRowRefSizeSmall, out var startRowNumber, out var endRowNumber);
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

	internal EntityHandle GetInterface(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _InterfaceOffset, _IsTypeDefOrRefRefSizeSmall));
	}
}
