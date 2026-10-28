using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct PropertyTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _SignatureOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal PropertyTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_FlagsOffset = 0;
		_NameOffset = _FlagsOffset + 2;
		_SignatureOffset = _NameOffset + stringHeapRefSize;
		RowSize = _SignatureOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal PropertyAttributes GetFlags(PropertyDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (PropertyAttributes)Block.PeekUInt16(num + _FlagsOffset);
	}

	internal StringHandle GetName(PropertyDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetSignature(PropertyDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _SignatureOffset, _IsBlobHeapRefSizeSmall));
	}
}
