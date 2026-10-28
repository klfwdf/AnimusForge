using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ManifestResourceTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsImplementationRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _OffsetOffset;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _ImplementationOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ManifestResourceTableReader(int numberOfRows, int implementationRefSize, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsImplementationRefSizeSmall = implementationRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_OffsetOffset = 0;
		_FlagsOffset = _OffsetOffset + 4;
		_NameOffset = _FlagsOffset + 4;
		_ImplementationOffset = _NameOffset + stringHeapRefSize;
		RowSize = _ImplementationOffset + implementationRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal StringHandle GetName(ManifestResourceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal EntityHandle GetImplementation(ManifestResourceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return ImplementationTag.ConvertToHandle(Block.PeekTaggedReference(num + _ImplementationOffset, _IsImplementationRefSizeSmall));
	}

	internal uint GetOffset(ManifestResourceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekUInt32(num + _OffsetOffset);
	}

	internal ManifestResourceAttributes GetFlags(ManifestResourceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (ManifestResourceAttributes)Block.PeekUInt32(num + _FlagsOffset);
	}
}
