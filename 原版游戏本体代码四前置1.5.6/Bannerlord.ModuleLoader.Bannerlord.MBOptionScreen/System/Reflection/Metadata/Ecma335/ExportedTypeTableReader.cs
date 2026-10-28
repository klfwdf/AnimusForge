using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ExportedTypeTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsImplementationRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _TypeDefIdOffset;

	private readonly int _TypeNameOffset;

	private readonly int _TypeNamespaceOffset;

	private readonly int _ImplementationOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal ExportedTypeTableReader(int numberOfRows, int implementationRefSize, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsImplementationRefSizeSmall = implementationRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_FlagsOffset = 0;
		_TypeDefIdOffset = _FlagsOffset + 4;
		_TypeNameOffset = _TypeDefIdOffset + 4;
		_TypeNamespaceOffset = _TypeNameOffset + stringHeapRefSize;
		_ImplementationOffset = _TypeNamespaceOffset + stringHeapRefSize;
		RowSize = _ImplementationOffset + implementationRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal StringHandle GetTypeName(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _TypeNameOffset, _IsStringHeapRefSizeSmall));
	}

	internal StringHandle GetTypeNamespaceString(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall));
	}

	internal NamespaceDefinitionHandle GetTypeNamespace(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return NamespaceDefinitionHandle.FromFullNameOffset(Block.PeekHeapReference(num + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall));
	}

	internal EntityHandle GetImplementation(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return ImplementationTag.ConvertToHandle(Block.PeekTaggedReference(num + _ImplementationOffset, _IsImplementationRefSizeSmall));
	}

	internal TypeAttributes GetFlags(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return (TypeAttributes)Block.PeekUInt32(num + _FlagsOffset);
	}

	internal int GetTypeDefId(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekInt32(num + _TypeDefIdOffset);
	}

	internal int GetNamespace(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _TypeNamespaceOffset, _IsStringHeapRefSizeSmall);
	}
}
