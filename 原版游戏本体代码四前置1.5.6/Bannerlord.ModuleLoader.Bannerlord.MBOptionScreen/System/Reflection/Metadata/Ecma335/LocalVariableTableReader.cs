using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct LocalVariableTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isStringHeapRefSizeSmall;

	private readonly int _attributesOffset;

	private readonly int _indexOffset;

	private readonly int _nameOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal LocalVariableTableReader(int numberOfRows, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_attributesOffset = 0;
		_indexOffset = _attributesOffset + 2;
		_nameOffset = _indexOffset + 2;
		RowSize = _nameOffset + stringHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal LocalVariableAttributes GetAttributes(LocalVariableHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (LocalVariableAttributes)Block.PeekUInt16(num + _attributesOffset);
	}

	internal ushort GetIndex(LocalVariableHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekUInt16(num + _indexOffset);
	}

	internal StringHandle GetName(LocalVariableHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _nameOffset, _isStringHeapRefSizeSmall));
	}
}
