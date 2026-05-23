using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000499 RID: 1177
	internal static class ReadOnlySequence
	{
		// Token: 0x06001A53 RID: 6739 RVA: 0x000562D9 File Offset: 0x000544D9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SegmentToSequenceStart(int startIndex)
		{
			return startIndex | 0;
		}

		// Token: 0x06001A54 RID: 6740 RVA: 0x000562D9 File Offset: 0x000544D9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SegmentToSequenceEnd(int endIndex)
		{
			return endIndex | 0;
		}

		// Token: 0x06001A55 RID: 6741 RVA: 0x000562D9 File Offset: 0x000544D9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ArrayToSequenceStart(int startIndex)
		{
			return startIndex | 0;
		}

		// Token: 0x06001A56 RID: 6742 RVA: 0x000562DE File Offset: 0x000544DE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ArrayToSequenceEnd(int endIndex)
		{
			return endIndex | int.MinValue;
		}

		// Token: 0x06001A57 RID: 6743 RVA: 0x000562DE File Offset: 0x000544DE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MemoryManagerToSequenceStart(int startIndex)
		{
			return startIndex | int.MinValue;
		}

		// Token: 0x06001A58 RID: 6744 RVA: 0x000562D9 File Offset: 0x000544D9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int MemoryManagerToSequenceEnd(int endIndex)
		{
			return endIndex | 0;
		}

		// Token: 0x06001A59 RID: 6745 RVA: 0x000562DE File Offset: 0x000544DE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int StringToSequenceStart(int startIndex)
		{
			return startIndex | int.MinValue;
		}

		// Token: 0x06001A5A RID: 6746 RVA: 0x000562DE File Offset: 0x000544DE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int StringToSequenceEnd(int endIndex)
		{
			return endIndex | int.MinValue;
		}

		// Token: 0x040010E9 RID: 4329
		public const int FlagBitMask = -2147483648;

		// Token: 0x040010EA RID: 4330
		public const int IndexBitMask = 2147483647;

		// Token: 0x040010EB RID: 4331
		public const int SegmentStartMask = 0;

		// Token: 0x040010EC RID: 4332
		public const int SegmentEndMask = 0;

		// Token: 0x040010ED RID: 4333
		public const int ArrayStartMask = 0;

		// Token: 0x040010EE RID: 4334
		public const int ArrayEndMask = -2147483648;

		// Token: 0x040010EF RID: 4335
		public const int MemoryManagerStartMask = -2147483648;

		// Token: 0x040010F0 RID: 4336
		public const int MemoryManagerEndMask = 0;

		// Token: 0x040010F1 RID: 4337
		public const int StringStartMask = -2147483648;

		// Token: 0x040010F2 RID: 4338
		public const int StringEndMask = -2147483648;
	}
}
