using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal struct TypeDefTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _IsFieldRefSizeSmall;

	private readonly bool _IsMethodRefSizeSmall;

	private readonly bool _IsTypeDefOrRefRefSizeSmall;

	private readonly bool _IsStringHeapRefSizeSmall;

	private readonly int _FlagsOffset;

	private readonly int _NameOffset;

	private readonly int _NamespaceOffset;

	private readonly int _ExtendsOffset;

	private readonly int _FieldListOffset;

	private readonly int _MethodListOffset;

	internal readonly int RowSize;

	internal System.Reflection.Internal.MemoryBlock Block;

	internal TypeDefTableReader(int numberOfRows, int fieldRefSize, int methodRefSize, int typeDefOrRefRefSize, int stringHeapRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_IsFieldRefSizeSmall = fieldRefSize == 2;
		_IsMethodRefSizeSmall = methodRefSize == 2;
		_IsTypeDefOrRefRefSizeSmall = typeDefOrRefRefSize == 2;
		_IsStringHeapRefSizeSmall = stringHeapRefSize == 2;
		_FlagsOffset = 0;
		_NameOffset = _FlagsOffset + 4;
		_NamespaceOffset = _NameOffset + stringHeapRefSize;
		_ExtendsOffset = _NamespaceOffset + stringHeapRefSize;
		_FieldListOffset = _ExtendsOffset + typeDefOrRefRefSize;
		_MethodListOffset = _FieldListOffset + fieldRefSize;
		RowSize = _MethodListOffset + methodRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
	}

	internal TypeAttributes GetFlags(TypeDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return (TypeAttributes)Block.PeekUInt32(num + _FlagsOffset);
	}

	internal NamespaceDefinitionHandle GetNamespaceDefinition(TypeDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return NamespaceDefinitionHandle.FromFullNameOffset(Block.PeekHeapReference(num + _NamespaceOffset, _IsStringHeapRefSizeSmall));
	}

	internal StringHandle GetNamespace(TypeDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NamespaceOffset, _IsStringHeapRefSizeSmall));
	}

	internal StringHandle GetName(TypeDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return StringHandle.FromOffset(Block.PeekHeapReference(num + _NameOffset, _IsStringHeapRefSizeSmall));
	}

	internal EntityHandle GetExtends(TypeDefinitionHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return TypeDefOrRefTag.ConvertToHandle(Block.PeekTaggedReference(num + _ExtendsOffset, _IsTypeDefOrRefRefSizeSmall));
	}

	internal int GetFieldStart(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _FieldListOffset, _IsFieldRefSizeSmall);
	}

	internal int GetMethodStart(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _MethodListOffset, _IsMethodRefSizeSmall);
	}

	internal TypeDefinitionHandle FindTypeContainingMethod(int methodDefOrPtrRowId, int numberOfMethods)
	{
		int numberOfRows = NumberOfRows;
		int num = Block.BinarySearchForSlot(numberOfRows, RowSize, _MethodListOffset, (uint)methodDefOrPtrRowId, _IsMethodRefSizeSmall) + 1;
		if (num == 0)
		{
			return default(TypeDefinitionHandle);
		}
		if (num > numberOfRows)
		{
			if (methodDefOrPtrRowId <= numberOfMethods)
			{
				return TypeDefinitionHandle.FromRowId(numberOfRows);
			}
			return default(TypeDefinitionHandle);
		}
		if (GetMethodStart(num) == methodDefOrPtrRowId)
		{
			while (num < numberOfRows)
			{
				int num2 = num + 1;
				if (GetMethodStart(num2) != methodDefOrPtrRowId)
				{
					break;
				}
				num = num2;
			}
		}
		return TypeDefinitionHandle.FromRowId(num);
	}

	internal TypeDefinitionHandle FindTypeContainingField(int fieldDefOrPtrRowId, int numberOfFields)
	{
		int numberOfRows = NumberOfRows;
		int num = Block.BinarySearchForSlot(numberOfRows, RowSize, _FieldListOffset, (uint)fieldDefOrPtrRowId, _IsFieldRefSizeSmall) + 1;
		if (num == 0)
		{
			return default(TypeDefinitionHandle);
		}
		if (num > numberOfRows)
		{
			if (fieldDefOrPtrRowId <= numberOfFields)
			{
				return TypeDefinitionHandle.FromRowId(numberOfRows);
			}
			return default(TypeDefinitionHandle);
		}
		if (GetFieldStart(num) == fieldDefOrPtrRowId)
		{
			while (num < numberOfRows)
			{
				int num2 = num + 1;
				if (GetFieldStart(num2) != fieldDefOrPtrRowId)
				{
					break;
				}
				num = num2;
			}
		}
		return TypeDefinitionHandle.FromRowId(num);
	}
}
