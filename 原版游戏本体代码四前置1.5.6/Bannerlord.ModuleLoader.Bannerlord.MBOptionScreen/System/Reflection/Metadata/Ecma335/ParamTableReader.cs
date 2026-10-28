using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ParamTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _SequenceOffset;

	private readonly int _NameOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ParamTableReader(int numberOfRows, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_FlagsOffset = 0;
		_SequenceOffset = _FlagsOffset + 2;
		_NameOffset = _SequenceOffset + 2;
		RowSize = _NameOffset + stringHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal ParameterAttributes GetFlags(ParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (ParameterAttributes)Block.PeekUInt16(num + _FlagsOffset);
	}

	internal ushort GetSequence(ParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekUInt16(num + _SequenceOffset);
	}

	internal StringHandle GetName(ParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}
}
