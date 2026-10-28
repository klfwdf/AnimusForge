using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyRefProcessorTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsAssemblyRefTableRowSizeSmall;

	private readonly int _ProcessorOffset;

	private readonly int _AssemblyRefOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal AssemblyRefProcessorTableReader(int numberOfRows, int assemblyRefTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsAssemblyRefTableRowSizeSmall = assemblyRefTableRowRefSize == 2;
		_ProcessorOffset = 0;
		_AssemblyRefOffset = _ProcessorOffset + 4;
		RowSize = _AssemblyRefOffset + assemblyRefTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}
}
