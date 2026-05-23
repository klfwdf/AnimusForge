using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000031 RID: 49
	internal static class ReadOnlySequence
	{
		// Token: 0x060001F3 RID: 499 RVA: 0x0000B0A3 File Offset: 0x000092A3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SegmentToSequenceStart(int startIndex)
		{
			return startIndex | 0;
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000B0A8 File Offset: 0x000092A8
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SegmentToSequenceEnd(int endIndex)
		{
			return endIndex | 0;
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0000B0AD File Offset: 0x000092AD
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ArrayToSequenceStart(int startIndex)
		{
			return startIndex | 0;
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0000B0B2 File Offset: 0x000092B2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ArrayToSequenceEnd(int endIndex)
		{
			return endIndex | int.MinValue;
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000B0BB File Offset: 0x000092BB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MemoryManagerToSequenceStart(int startIndex)
		{
			return startIndex | int.MinValue;
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0000B0C4 File Offset: 0x000092C4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MemoryManagerToSequenceEnd(int endIndex)
		{
			return endIndex | 0;
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0000B0C9 File Offset: 0x000092C9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int StringToSequenceStart(int startIndex)
		{
			return startIndex | int.MinValue;
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000B0D2 File Offset: 0x000092D2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int StringToSequenceEnd(int endIndex)
		{
			return endIndex | int.MinValue;
		}

		// Token: 0x0400005C RID: 92
		public const int FlagBitMask = -2147483648;

		// Token: 0x0400005D RID: 93
		public const int IndexBitMask = 2147483647;

		// Token: 0x0400005E RID: 94
		public const int SegmentStartMask = 0;

		// Token: 0x0400005F RID: 95
		public const int SegmentEndMask = 0;

		// Token: 0x04000060 RID: 96
		public const int ArrayStartMask = 0;

		// Token: 0x04000061 RID: 97
		public const int ArrayEndMask = -2147483648;

		// Token: 0x04000062 RID: 98
		public const int MemoryManagerStartMask = -2147483648;

		// Token: 0x04000063 RID: 99
		public const int MemoryManagerEndMask = 0;

		// Token: 0x04000064 RID: 100
		public const int StringStartMask = -2147483648;

		// Token: 0x04000065 RID: 101
		public const int StringEndMask = -2147483648;
	}
}
