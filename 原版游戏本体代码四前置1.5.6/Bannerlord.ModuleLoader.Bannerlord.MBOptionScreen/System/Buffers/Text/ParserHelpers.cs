using System.Runtime.CompilerServices;

namespace System.Buffers.Text;

internal static class ParserHelpers
{
	public const int ByteOverflowLength = 3;

	public const int ByteOverflowLengthHex = 2;

	public const int UInt16OverflowLength = 5;

	public const int UInt16OverflowLengthHex = 4;

	public const int UInt32OverflowLength = 10;

	public const int UInt32OverflowLengthHex = 8;

	public const int UInt64OverflowLength = 20;

	public const int UInt64OverflowLengthHex = 16;

	public const int SByteOverflowLength = 3;

	public const int SByteOverflowLengthHex = 2;

	public const int Int16OverflowLength = 5;

	public const int Int16OverflowLengthHex = 4;

	public const int Int32OverflowLength = 10;

	public const int Int32OverflowLengthHex = 8;

	public const int Int64OverflowLength = 19;

	public const int Int64OverflowLengthHex = 16;

	public static readonly byte[] s_hexLookup = new byte[256]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 0, 1,
		2, 3, 4, 5, 6, 7, 8, 9, 255, 255,
		255, 255, 255, 255, 255, 10, 11, 12, 13, 14,
		15, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 10, 11, 12,
		13, 14, 15, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDigit(int i)
	{
		return (uint)(i - 48) <= 9u;
	}
}
