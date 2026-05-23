using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x020004A3 RID: 1187
	internal static class Utilities
	{
		// Token: 0x06001A89 RID: 6793 RVA: 0x00056CB1 File Offset: 0x00054EB1
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SelectBucketIndex(int bufferSize)
		{
			return BitOperations.Log2((uint)((bufferSize - 1) | 15)) - 3;
		}

		// Token: 0x06001A8A RID: 6794 RVA: 0x00056CC0 File Offset: 0x00054EC0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetMaxSizeForBucket(int binIndex)
		{
			return 16 << binIndex;
		}

		// Token: 0x06001A8B RID: 6795 RVA: 0x0001B69F File Offset: 0x0001989F
		internal static Utilities.MemoryPressure GetMemoryPressure()
		{
			return Utilities.MemoryPressure.Low;
		}

		// Token: 0x020004A4 RID: 1188
		internal enum MemoryPressure
		{
			// Token: 0x0400110E RID: 4366
			Low,
			// Token: 0x0400110F RID: 4367
			Medium,
			// Token: 0x04001110 RID: 4368
			High
		}
	}
}
