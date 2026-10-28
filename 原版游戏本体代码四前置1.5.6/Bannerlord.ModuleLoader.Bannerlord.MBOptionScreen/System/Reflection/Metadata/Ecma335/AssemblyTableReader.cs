using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct AssemblyTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _HashAlgIdOffset;

	private readonly int _MajorVersionOffset;

	private readonly int _MinorVersionOffset;

	private readonly int _BuildNumberOffset;

	private readonly int _RevisionNumberOffset;

	private readonly int _FlagsOffset;

	private readonly int _PublicKeyOffset;

	private readonly int _NameOffset;

	private readonly int _CultureOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal AssemblyTableReader(int numberOfRows, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = ((numberOfRows > 1) ? 1 : numberOfRows);
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_HashAlgIdOffset = 0;
		_MajorVersionOffset = _HashAlgIdOffset + 4;
		_MinorVersionOffset = _MajorVersionOffset + 2;
		_BuildNumberOffset = _MinorVersionOffset + 2;
		_RevisionNumberOffset = _BuildNumberOffset + 2;
		_FlagsOffset = _RevisionNumberOffset + 2;
		_PublicKeyOffset = _FlagsOffset + 4;
		_NameOffset = _PublicKeyOffset + blobHeapRefSize;
		_CultureOffset = _NameOffset + stringHeapRefSize;
		RowSize = _CultureOffset + stringHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal AssemblyHashAlgorithm GetHashAlgorithm()
	{
		return (AssemblyHashAlgorithm)Block.PeekUInt32(_HashAlgIdOffset);
	}

	internal Version GetVersion()
	{
		return new Version(Block.PeekUInt16(_MajorVersionOffset), Block.PeekUInt16(_MinorVersionOffset), Block.PeekUInt16(_BuildNumberOffset), Block.PeekUInt16(_RevisionNumberOffset));
	}

	internal AssemblyFlags GetFlags()
	{
		return (AssemblyFlags)Block.PeekUInt32(_FlagsOffset);
	}

	internal System.Reflection.Metadata.BlobHandle GetPublicKey()
	{
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(_PublicKeyOffset, _IsBlobHeapRefSizeSmall));
	}

	internal StringHandle GetName()
	{
		return StringHandle.FromOffset(Block.PeekHeapReference(_NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal StringHandle GetCulture()
	{
		return StringHandle.FromOffset(Block.PeekHeapReference(_CultureOffset, _IsStringHeapRefSizeSmall));
	}
}
