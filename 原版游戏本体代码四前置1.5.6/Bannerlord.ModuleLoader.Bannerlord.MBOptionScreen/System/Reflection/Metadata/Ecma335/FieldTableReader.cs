using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FieldTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _SignatureOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal FieldTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
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

	internal StringHandle GetName(FieldDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal FieldAttributes GetFlags(FieldDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (FieldAttributes)Block.PeekUInt16(num + _FlagsOffset);
	}

	internal System.Reflection.Metadata.BlobHandle GetSignature(FieldDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _SignatureOffset, _IsBlobHeapRefSizeSmall));
	}
}
