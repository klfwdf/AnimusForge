using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodSpecTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsMethodDefOrRefRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _MethodOffset;

	private readonly int _InstantiationOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal MethodSpecTableReader(int numberOfRows, int methodDefOrRefRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsMethodDefOrRefRefSizeSmall = methodDefOrRefRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_MethodOffset = 0;
		_InstantiationOffset = _MethodOffset + methodDefOrRefRefSize;
		RowSize = _InstantiationOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal EntityHandle GetMethod(MethodSpecificationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return MethodDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _MethodOffset, _IsMethodDefOrRefRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetInstantiation(MethodSpecificationHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _InstantiationOffset, _IsBlobHeapRefSizeSmall));
	}
}
