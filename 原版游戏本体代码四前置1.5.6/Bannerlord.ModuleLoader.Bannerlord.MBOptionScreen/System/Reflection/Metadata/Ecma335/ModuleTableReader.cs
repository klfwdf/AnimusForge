using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ModuleTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsGUIDHeapRefSizeSmall;

	private readonly int _GenerationOffset;

	private readonly int _NameOffset;

	private readonly int _MVIdOffset;

	private readonly int _EnCIdOffset;

	private readonly int _EnCBaseIdOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ModuleTableReader(int numberOfRows, int stringHeapRefSize, int guidHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsGUIDHeapRefSizeSmall = guidHeapRefSize == 2;
		_GenerationOffset = 0;
		_NameOffset = _GenerationOffset + 2;
		_MVIdOffset = _NameOffset + stringHeapRefSize;
		_EnCIdOffset = _MVIdOffset + guidHeapRefSize;
		_EnCBaseIdOffset = _EnCIdOffset + guidHeapRefSize;
		RowSize = _EnCBaseIdOffset + guidHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal ushort GetGeneration()
	{
		return Block.PeekUInt16(_GenerationOffset);
	}

	internal StringHandle GetName()
	{
		return StringHandle.FromOffset(Block.PeekHeapReference(_NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal GuidHandle GetMvid()
	{
		return GuidHandle.FromIndex(Block.PeekHeapReference(_MVIdOffset, _IsGUIDHeapRefSizeSmall));
	}

	internal GuidHandle GetEncId()
	{
		return GuidHandle.FromIndex(Block.PeekHeapReference(_EnCIdOffset, _IsGUIDHeapRefSizeSmall));
	}

	internal GuidHandle GetEncBaseId()
	{
		return GuidHandle.FromIndex(Block.PeekHeapReference(_EnCBaseIdOffset, _IsGUIDHeapRefSizeSmall));
	}
}
