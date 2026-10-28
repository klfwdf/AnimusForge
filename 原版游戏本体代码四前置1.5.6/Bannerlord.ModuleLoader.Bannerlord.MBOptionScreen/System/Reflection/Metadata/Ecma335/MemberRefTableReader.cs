using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal struct MemberRefTableReader
{
	internal int NumberOfRows;

	private readonly bool _IsMemberRefParentRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _ClassOffset;

	private readonly int _NameOffset;

	private readonly int _SignatureOffset;

	internal readonly int RowSize;

	internal System.Reflection.Internal.MemoryBlock Block;

	internal MemberRefTableReader(int numberOfRows, int memberRefParentRefSize, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsMemberRefParentRefSizeSmall = memberRefParentRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_ClassOffset = 0;
		_NameOffset = _ClassOffset + memberRefParentRefSize;
		_SignatureOffset = _NameOffset + stringHeapRefSize;
		RowSize = _SignatureOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal System.Reflection.Metadata.BlobHandle GetSignature(MemberReferenceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _SignatureOffset, _IsBlobHeapRefSizeSmall));
	}

	internal StringHandle GetName(MemberReferenceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal EntityHandle GetClass(MemberReferenceHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return MemberRefParentTag.ConvertToHandle(Block.PeekTaggedReference(num + _ClassOffset, _IsMemberRefParentRefSizeSmall));
	}
}
