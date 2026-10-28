using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct StateMachineMethodTableReader
{
	internal readonly int NumberOfRows;

	private readonly bool _isMethodRefSizeSmall;

	private const int MoveNextMethodOffset = 0;

	private readonly int _kickoffMethodOffset;

	internal readonly int RowSize;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal StateMachineMethodTableReader(int numberOfRows, bool declaredSorted, int methodRefSize, System.Reflection.Internal.MemoryBlock containingBlock, int containingBlockOffset)
	{
		NumberOfRows = numberOfRows;
		_isMethodRefSizeSmall = methodRefSize == 2;
		_kickoffMethodOffset = methodRefSize;
		RowSize = _kickoffMethodOffset + methodRefSize;
		Block = containingBlock.GetMemoryBlockAt(containingBlockOffset, RowSize * numberOfRows);
		if (numberOfRows > 0 && !declaredSorted)
		{
			System.Reflection.Throw.TableNotSorted(System.Reflection.Metadata.Ecma335.TableIndex.StateMachineMethod);
		}
	}

	internal System.Reflection.Metadata.MethodDefinitionHandle FindKickoffMethod(int moveNextMethodRowId)
	{
		int num = Block.BinarySearchReference(NumberOfRows, RowSize, 0, (uint)moveNextMethodRowId, _isMethodRefSizeSmall);
		if (num < 0)
		{
			return default(System.Reflection.Metadata.MethodDefinitionHandle);
		}
		return GetKickoffMethod(num + 1);
	}

	private System.Reflection.Metadata.MethodDefinitionHandle GetKickoffMethod(int rowId)
	{
		int num = (rowId - 1) * RowSize;
		return System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(Block.PeekReference(num + _kickoffMethodOffset, _isMethodRefSizeSmall));
	}
}
