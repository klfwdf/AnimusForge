using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct GenericParamTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsTypeOrMethodDefRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _NumberOffset;

	private readonly int _FlagsOffset;

	private readonly int _OwnerOffset;

	private readonly int _NameOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal GenericParamTableReader(int numberOfRows, bool declaredSorted, int typeOrMethodDefRefSize, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsTypeOrMethodDefRefSizeSmall = typeOrMethodDefRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_NumberOffset = 0;
		_FlagsOffset = _NumberOffset + 2;
		_OwnerOffset = _FlagsOffset + 2;
		_NameOffset = _OwnerOffset + typeOrMethodDefRefSize;
		RowSize = _NameOffset + stringHeapRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (!declaredSorted && !CheckSorted())
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.GenericParam);
		}
	}

	internal ushort GetNumber(GenericParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return Block.PeekUInt16(num + _NumberOffset);
	}

	internal GenericParameterAttributes GetFlags(GenericParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (GenericParameterAttributes)Block.PeekUInt16(num + _FlagsOffset);
	}

	internal StringHandle GetName(GenericParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal EntityHandle GetOwner(GenericParameterHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return TypeOrMethodDefTag.ConvertToHandle(Block.PeekTaggedReference(num + _OwnerOffset, _IsTypeOrMethodDefRefSizeSmall));
	}

	internal GenericParameterHandleCollection FindGenericParametersForType(TypeDefinitionHandle typeDef)
	{
		ushort genericParamCount = 0;
		uint searchCodedTag = TypeOrMethodDefTag.ConvertTypeDefRowIdToTag(typeDef);
		return new GenericParameterHandleCollection(BinarySearchTag(searchCodedTag, ref genericParamCount), genericParamCount);
	}

	internal GenericParameterHandleCollection FindGenericParametersForMethod(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		ushort genericParamCount = 0;
		uint searchCodedTag = TypeOrMethodDefTag.ConvertMethodDefToTag(methodDef);
		return new GenericParameterHandleCollection(BinarySearchTag(searchCodedTag, ref genericParamCount), genericParamCount);
	}

	private int BinarySearchTag(uint searchCodedTag, ref ushort genericParamCount)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, _OwnerOffset, searchCodedTag, _IsTypeOrMethodDefRefSizeSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			genericParamCount = 0;
			return 0;
		}
		genericParamCount = (ushort)(endRowNumber - startRowNumber + 1);
		return startRowNumber + 1;
	}

	private bool CheckSorted()
	{
		return Block.IsOrderedByReferenceAscending(RowSize, _OwnerOffset, _IsTypeOrMethodDefRefSizeSmall);
	}
}
