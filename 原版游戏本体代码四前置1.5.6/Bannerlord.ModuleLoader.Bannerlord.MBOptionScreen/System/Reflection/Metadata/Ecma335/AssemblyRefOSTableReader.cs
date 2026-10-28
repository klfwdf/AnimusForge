using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyRefOSTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsAssemblyRefTableRowRefSizeSmall;

	private readonly int _OSPlatformIdOffset;

	private readonly int _OSMajorVersionIdOffset;

	private readonly int _OSMinorVersionIdOffset;

	private readonly int _AssemblyRefOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal AssemblyRefOSTableReader(int numberOfRows, int assemblyRefTableRowRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsAssemblyRefTableRowRefSizeSmall = assemblyRefTableRowRefSize == 2;
		_OSPlatformIdOffset = 0;
		_OSMajorVersionIdOffset = _OSPlatformIdOffset + 4;
		_OSMinorVersionIdOffset = _OSMajorVersionIdOffset + 4;
		_AssemblyRefOffset = _OSMinorVersionIdOffset + 4;
		RowSize = _AssemblyRefOffset + assemblyRefTableRowRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}
}
