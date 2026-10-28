using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyRefTableReader
{
	internal readonly int NumberOfNonVirtualRows;

	internal readonly int NumberOfVirtualRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _MajorVersionOffset;

	private readonly int _MinorVersionOffset;

	private readonly int _BuildNumberOffset;

	private readonly int _RevisionNumberOffset;

	private readonly int _FlagsOffset;

	private readonly int _PublicKeyOrTokenOffset;

	private readonly int _NameOffset;

	private readonly int _CultureOffset;

	private readonly int _HashValueOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal AssemblyRefTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset, System.Reflection.Metadata.MetadataKind metadataKind)
	{
		NumberOfNonVirtualRows = numberOfRows;
		NumberOfVirtualRows = ((metadataKind != System.Reflection.Metadata.MetadataKind.Ecma335) ? 6 : 0);
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_MajorVersionOffset = 0;
		_MinorVersionOffset = _MajorVersionOffset + 2;
		_BuildNumberOffset = _MinorVersionOffset + 2;
		_RevisionNumberOffset = _BuildNumberOffset + 2;
		_FlagsOffset = _RevisionNumberOffset + 2;
		_PublicKeyOrTokenOffset = _FlagsOffset + 4;
		_NameOffset = _PublicKeyOrTokenOffset + blobHeapRefSize;
		_CultureOffset = _NameOffset + stringHeapRefSize;
		_HashValueOffset = _CultureOffset + stringHeapRefSize;
		RowSize = _HashValueOffset + blobHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal Version GetVersion(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return new Version(Block.PeekUInt16(num + _MajorVersionOffset), Block.PeekUInt16(num + _MinorVersionOffset), Block.PeekUInt16(num + _BuildNumberOffset), Block.PeekUInt16(num + _RevisionNumberOffset));
	}

	internal AssemblyFlags GetFlags(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return (AssemblyFlags)Block.PeekUInt32(num + _FlagsOffset);
	}

	internal System.Reflection.Metadata.BlobHandle GetPublicKeyOrToken(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _PublicKeyOrTokenOffset, _IsBlobHeapRefSizeSmall));
	}

	internal StringHandle GetName(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal StringHandle GetCulture(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _CultureOffset, _IsStringHeapRefSizeSmall));
	}

	internal System.Reflection.Metadata.BlobHandle GetHashValue(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _HashValueOffset, _IsBlobHeapRefSizeSmall));
	}
}
