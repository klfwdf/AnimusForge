using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodImplTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeDefTableRowRefSizeSmall;

	private readonly bool _IsMethodDefOrRefRefSizeSmall;

	private readonly int _ClassOffset;

	private readonly int _MethodBodyOffset;

	private readonly int _MethodDeclarationOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal MethodImplTableReader(int numberOfRows, bool declaredSorted, int typeDefTableRowRefSize, int methodDefOrRefRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeDefTableRowRefSizeSmall = typeDefTableRowRefSize == 2;
		_IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
		_ClassOffset = 0;
		_MethodBodyOffset = _ClassOffset + typeDefTableRowRefSize;
		_MethodDeclarationOffset = _MethodBodyOffset + methodDefOrRefRefSize;
		RowSize = _MethodDeclarationOffset + methodDefOrRefRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.MethodImpl);
		}
	}

	internal TypeDefinitionHandle GetClass(MethodImplementationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return TypeDefinitionHandle.FromRowId(Block.PeekReference(num + _ClassOffset, _IsTypeDefTableRowRefSizeSmall));
	}

	internal EntityHandle GetMethodBody(MethodImplementationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return MethodDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _MethodBodyOffset, _IsMethodDefOrRefRefSizeSmall));
	}

	internal EntityHandle GetMethodDeclaration(MethodImplementationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return MethodDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _MethodDeclarationOffset, _IsMethodDefOrRefRefSizeSmall));
	}

	internal void GetMethodImplRange(TypeDefinitionHandle typeDef, out int firstImplRowId, out int lastImplRowId)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _ClassOffset, (uint)typeDef.RowId, _IsTypeDefTableRowRefSizeSmall, out var startRowNumber, out var endRowNumber);
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
		return Block.IsOrderedByReferenceAscending(RowSize, _ClassOffset, _IsTypeDefTableRowRefSizeSmall);
	}
}
