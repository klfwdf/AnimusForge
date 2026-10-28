using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct UserStringHeap(System.Reflection.Internal.MemoryBlock block)
{
	internal readonly System.Reflection.Internal.MemoryBlock Block = block;

	internal string GetString(UserStringHandle handle)
	{
		if (!Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out var offset, out var size))
		{
			return string.Empty;
		}
		return Block.PeekUtf16(offset, size & -2);
	}

	internal UserStringHandle GetNextHandle(UserStringHandle handle)
	{
		if (!Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out var offset, out var size))
		{
			return default(UserStringHandle);
		}
		int num = offset + size;
		if (num >= Block.Length)
		{
			return default(UserStringHandle);
		}
		return UserStringHandle.FromOffset(num);
	}
}
