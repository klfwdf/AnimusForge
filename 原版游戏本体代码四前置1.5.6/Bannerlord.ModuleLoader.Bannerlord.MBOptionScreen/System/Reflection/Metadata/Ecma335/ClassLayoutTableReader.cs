using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal struct ClassLayoutTableReader
{
	internal int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly int _PackagingSizeOffset;

	private readonly int _ClassSizeOffset;

	private readonly int _ParentOffset;

	internal readonly int RowSize;

	internal System.Reflection.Internal.MemoryBlock Block;

	internal ClassLayoutTableReader(int numberOfRows, bool declaredSorted, int typeDefTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_PackagingSizeOffset = 0;
		_ClassSizeOffset = _PackagingSizeOffset + 2;
		_ParentOffset = _ClassSizeOffset + 4;
		RowSize = _ParentOffset + typeDefTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.ClassLayout);
		}
	}

	internal TypeDefinitionHandle GetParent(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _ParentOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal ushort GetPackingSize(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekUInt16(num + _PackagingSizeOffset);
	}

	internal uint GetClassSize(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekUInt32(num + _ClassSizeOffset);
	}

	internal int FindRow(TypeDefinitionHandle typeDef)
	{
		return 1 + Block.BinarySearchReference(NumberOfRows, RowSize, _ParentOffset, (uint)typeDef.RowId, _IsTypeDefTableRowRefSizeSmall);
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _ParentOffset, _IsTypeDefTableRowRefSizeSmall);
	}
}
