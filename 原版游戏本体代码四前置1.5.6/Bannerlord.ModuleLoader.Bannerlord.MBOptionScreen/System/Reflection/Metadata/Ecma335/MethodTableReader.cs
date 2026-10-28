using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct MethodTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsParamRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly bool _IsBlobHeapRefSizeSmall;

	private readonly int _RvaOffset;

	private readonly int _ImplFlagsOffset;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _SignatureOffset;

	private readonly int _ParamListOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal MethodTableReader(int numberOfRows, int paramRefSize, int stringHeapRefSize, int blobHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsParamRefSizeSmall = paramRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_IsBlobHeapRefSizeSmall = blobHeapRefSize == 2;
		_RvaOffset = 0;
		_ImplFlagsOffset = _RvaOffset + 4;
		_FlagsOffset = _ImplFlagsOffset + 2;
		_NameOffset = _FlagsOffset + 2;
		_SignatureOffset = _NameOffset + stringHeapRefSize;
		_ParamListOffset = _SignatureOffset + blobHeapRefSize;
		RowSize = _ParamListOffset + paramRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal int GetParamStart(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _ParamListOffset, _IsParamRefSizeSmall);
	}

	internal System.Reflection.Metadata.BlobHandle GetSignature(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return System.Reflection.Metadata.BlobHandle.FromOffset(Block.PeekHeapReference(num + _SignatureOffset, _IsBlobHeapRefSizeSmall));
	}

	internal int GetRva(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekInt32(num + _RvaOffset);
	}

	internal StringHandle GetName(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal MethodAttributes GetFlags(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (MethodAttributes)Block.PeekUInt16(num + _FlagsOffset);
	}

	internal MethodImplAttributes GetImplFlags(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (MethodImplAttributes)Block.PeekUInt16(num + _ImplFlagsOffset);
	}
}
