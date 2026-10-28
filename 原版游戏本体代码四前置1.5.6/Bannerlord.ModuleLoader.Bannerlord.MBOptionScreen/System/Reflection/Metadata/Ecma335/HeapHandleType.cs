namespace System.Reflection.Metadata.Ecma335;

internal static class HeapHandleType
{
	internal const int OffsetBitCount = 29;

	internal const uint OffsetMask = 536870911u;

	internal const uint VirtualBit = 2147483648u;

	internal static bool IsValidHeapOffset(uint offset)
	{
		return (offset & 0xE0000000u) == 0;
	}
}
