using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct GenericParamConstraintTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsGenericParamTableRowRefSizeSmall;

	private readonly bool _IsTypeDefOrRefRefSizeSmall;

	private readonly int _OwnerOffset;

	private readonly int _ConstraintOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal GenericParamConstraintTableReader(int numberOfRows, bool declaredSorted, int genericParamTableRowRefSize, int typeDefOrRefRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsGenericParamTableRowRefSizeSmall = genericParamTableRowRefSize == 2;
		_IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
		_OwnerOffset = 0;
		_ConstraintOffset = _OwnerOffset + genericParamTableRowRefSize;
		RowSize = _ConstraintOffset + typeDefOrRefRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint);
		}
	}

	internal GenericParameterConstraintHandleCollection FindConstraintsForGenericParam(GenericParameterHandle genericParameter)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _OwnerOffset, (uint)genericParameter.RowId, _IsGenericParamTableRowRefSizeSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			return default(GenericParameterConstraintHandleCollection);
		}
		return new GenericParameterConstraintHandleCollection(startRowNumber + 1, (ushort)(endRowNumber - startRowNumber + 1));
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _OwnerOffset, _IsGenericParamTableRowRefSizeSmall);
	}

	internal EntityHandle GetConstraint(GenericParameterConstraintHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return TypeDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _ConstraintOffset, _IsTypeDefOrRefRefSizeSmall));
	}

	internal GenericParameterHandle GetOwner(GenericParameterConstraintHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return GenericParameterHandle.FromRowId(Block.PeekReference(num + _OwnerOffset, _IsGenericParamTableRowRefSizeSmall));
	}
}
