using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct LocalScopeTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isMethodRefSmall;

	private readonly bool _isImportScopeRefSmall;

	private readonly bool _isLocalConstantRefSmall;

	private readonly bool _isLocalVariableRefSmall;

	private const int MethodOffset = 0;

	private readonly int _importScopeOffset;

	private readonly int _variableListOffset;

	private readonly int _constantListOffset;

	private readonly int _startOffsetOffset;

	private readonly int _lengthOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal LocalScopeTableReader(int numberOfRows, bool declaredSorted, int methodRefSize, int importScopeRefSize, int localVariableRefSize, int localConstantRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isMethodRefSmall = methodRefSize == 2;
		_isImportScopeRefSmall = importScopeRefSize == 2;
		_isLocalVariableRefSmall = localVariableRefSize == 2;
		_isLocalConstantRefSmall = localConstantRefSize == 2;
		_importScopeOffset = methodRefSize;
		_variableListOffset = _importScopeOffset + importScopeRefSize;
		_constantListOffset = _variableListOffset + localVariableRefSize;
		_startOffsetOffset = _constantListOffset + localConstantRefSize;
		_lengthOffset = _startOffsetOffset + 4;
		RowSize = _lengthOffset + 4;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (numberOfRows > 0 && !declaredSorted)
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.LocalScope);
		}
	}

	internal System.Reflection.Metadata.MethodDefinitionHandle GetMethod(int rowId)
	{
		int offset = (rowId - 1) * RowSize;
		return System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(Block.PeekReference(offset, _isMethodRefSmall));
	}

	internal ImportScopeHandle GetImportScope(LocalScopeHandle handle)
	{
		int num = (handle.RowId - 1) * RowSize;
		return ImportScopeHandle.FromRowId(Block.PeekReference(num + _importScopeOffset, _isImportScopeRefSmall));
	}

	internal int GetVariableStart(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _variableListOffset, _isLocalVariableRefSmall);
	}

	internal int GetConstantStart(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekReference(num + _constantListOffset, _isLocalConstantRefSmall);
	}

	internal int GetStartOffset(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekInt32(num + _startOffsetOffset);
	}

	internal int GetLength(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return Block.PeekInt32(num + _lengthOffset);
	}

	internal int GetEndOffset(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		long num2 = Block.PeekUInt32(num + _startOffsetOffset) + Block.PeekUInt32(num + _lengthOffset);
		if ((int)num2 != num2)
		{
			System.Reflection.Throw.ValueOverflow();
		}
		return (int)num2;
	}

	internal void GetLocalScopeRange(int methodDefRid, out int firstScopeRowId, out int lastScopeRowId)
	{
		Block.BinarySearchReferenceRange(NumberOfRows, RowSize, 0, (uint)methodDefRid, _isMethodRefSmall, out var startRowNumber, out var endRowNumber);
		if (startRowNumber == -1)
		{
			firstScopeRowId = 1;
			lastScopeRowId = 0;
		}
		else
		{
			firstScopeRowId = startRowNumber + 1;
			lastScopeRowId = endRowNumber + 1;
		}
	}
}
