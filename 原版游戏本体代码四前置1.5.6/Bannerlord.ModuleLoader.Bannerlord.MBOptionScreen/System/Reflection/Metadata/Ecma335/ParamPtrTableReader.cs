using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ParamPtrTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsParamTableRowRefSizeSmall;

	private readonly int _ParamOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ParamPtrTableReader(int numberOfRows, int paramTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsParamTableRowRefSizeSmall = paramTableRowRefSize == 2;
		_ParamOffset = 0;
		RowSize = _ParamOffset + paramTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal ParameterHandle GetParamFor(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return ParameterHandle.FromRowId(Block.PeekReference(num + _ParamOffset, _IsParamTableRowRefSizeSmall));
	}
}
