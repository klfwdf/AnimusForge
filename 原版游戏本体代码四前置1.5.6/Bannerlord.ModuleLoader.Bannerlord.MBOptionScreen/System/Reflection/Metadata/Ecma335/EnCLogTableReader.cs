using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct EnCLogTableReader
{
	internal readonly int NumberOfRows;

	private readonly int _TokenOffset;

	private readonly int _FuncCodeOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal EnCLogTableReader(int numberOfRows, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset, System.Reflection.Metadata.Ecma335.MetadataStreamKind metadataStreamKind)
	{
		NumberOfRows = ((metadataStreamKind != System.Reflection.Metadata.Ecma335.MetadataStreamKind.Compressed) ? numberOfRows : 0);
		_TokenOffset = 0;
		_FuncCodeOffset = _TokenOffset + 4;
		RowSize = _FuncCodeOffset + 4;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal uint GetToken(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekUInt32(num + _TokenOffset);
	}

	internal EditAndContinueOperation GetFuncCode(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return (EditAndContinueOperation)Block.PeekUInt32(num + _FuncCodeOffset);
	}
}
