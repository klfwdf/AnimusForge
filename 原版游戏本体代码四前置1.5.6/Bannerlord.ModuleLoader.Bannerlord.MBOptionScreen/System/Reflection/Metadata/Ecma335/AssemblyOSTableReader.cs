using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyOSTableReader
{
	internal readonly int NumberOfRows;

	private readonly int _OSPlatformIdOffset;

	private readonly int _OSMajorVersionIdOffset;

	private readonly int _OSMinorVersionIdOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal AssemblyOSTableReader(int numberOfRows, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_OSPlatformIdOffset = 0;
		_OSMajorVersionIdOffset = _OSPlatformIdOffset + 4;
		_OSMinorVersionIdOffset = _OSMajorVersionIdOffset + 4;
		RowSize = _OSMinorVersionIdOffset + 4;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}
}
