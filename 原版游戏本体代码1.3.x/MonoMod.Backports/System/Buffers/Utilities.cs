using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000036 RID: 54
	internal static class Utilities
	{
		// Token: 0x0600021A RID: 538 RVA: 0x0000B7B4 File Offset: 0x000099B4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SelectBucketIndex(int bufferSize)
		{
			return BitOperations.Log2((uint)((bufferSize - 1) | 15)) - 3;
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000B7C3 File Offset: 0x000099C3
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int GetMaxSizeForBucket(int binIndex)
		{
			return 16 << binIndex;
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000B7CC File Offset: 0x000099CC
		internal static Utilities.MemoryPressure GetMemoryPressure()
		{
			return Utilities.MemoryPressure.Low;
		}

		// Token: 0x0200006A RID: 106
		internal enum MemoryPressure
		{
			// Token: 0x040000C9 RID: 201
			Low,
			// Token: 0x040000CA RID: 202
			Medium,
			// Token: 0x040000CB RID: 203
			High
		}
	}
}
