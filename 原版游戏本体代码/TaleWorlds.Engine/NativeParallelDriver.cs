using System;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200006F RID: 111
	public sealed class NativeParallelDriver : IParallelDriver
	{
		// Token: 0x06000A40 RID: 2624 RVA: 0x0000A5FC File Offset: 0x000087FC
		public void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate loopBody, int grainSize)
		{
			long num = Interlocked.Increment(ref NativeParallelDriver.LoopBodyHolder.UniqueLoopBodyKeySeed) % 256L;
			checked
			{
				NativeParallelDriver._loopBodyCache[(int)((IntPtr)num)].LoopBody = loopBody;
				Utilities.ParallelFor(fromInclusive, toExclusive, num, grainSize);
				NativeParallelDriver._loopBodyCache[(int)((IntPtr)num)].LoopBody = null;
			}
		}

		// Token: 0x06000A41 RID: 2625 RVA: 0x0000A64C File Offset: 0x0000884C
		public void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate loopBody, int grainSize)
		{
			long num = Interlocked.Increment(ref NativeParallelDriver.LoopBodyHolder.UniqueLoopBodyKeySeed) % 256L;
			checked
			{
				NativeParallelDriver._loopBodyCache[(int)((IntPtr)num)].LoopBody = loopBody;
				Utilities.ParallelForWithoutRenderThread(fromInclusive, toExclusive, num, grainSize);
				NativeParallelDriver._loopBodyCache[(int)((IntPtr)num)].LoopBody = null;
			}
		}

		// Token: 0x06000A42 RID: 2626 RVA: 0x0000A699 File Offset: 0x00008899
		[EngineCallback(null, false)]
		internal static void ParalelForLoopBodyCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
		{
			NativeParallelDriver._loopBodyCache[(int)(checked((IntPtr)loopBodyKey))].LoopBody(localStartIndex, localEndIndex);
		}

		// Token: 0x06000A43 RID: 2627 RVA: 0x0000A6B4 File Offset: 0x000088B4
		public void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate loopBody, int grainSize)
		{
			long num = Interlocked.Increment(ref NativeParallelDriver.LoopBodyWithDtHolder.UniqueLoopBodyKeySeed) % 256L;
			checked
			{
				NativeParallelDriver._loopBodyWithDtCache[(int)((IntPtr)num)].LoopBody = loopBody;
				NativeParallelDriver._loopBodyWithDtCache[(int)((IntPtr)num)].DeltaTime = deltaTime;
				Utilities.ParallelForWithDt(fromInclusive, toExclusive, num, grainSize);
				NativeParallelDriver._loopBodyWithDtCache[(int)((IntPtr)num)].LoopBody = null;
			}
		}

		// Token: 0x06000A44 RID: 2628 RVA: 0x0000A714 File Offset: 0x00008914
		[EngineCallback(null, false)]
		internal static void ParalelForLoopBodyWithDtCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
		{
			checked
			{
				NativeParallelDriver._loopBodyWithDtCache[(int)((IntPtr)loopBodyKey)].LoopBody(localStartIndex, localEndIndex, NativeParallelDriver._loopBodyWithDtCache[(int)((IntPtr)loopBodyKey)].DeltaTime);
			}
		}

		// Token: 0x06000A45 RID: 2629 RVA: 0x0000A73F File Offset: 0x0000893F
		public ulong GetMainThreadId()
		{
			return Utilities.GetMainThreadId();
		}

		// Token: 0x06000A46 RID: 2630 RVA: 0x0000A746 File Offset: 0x00008946
		public ulong GetCurrentThreadId()
		{
			return Utilities.GetCurrentThreadId();
		}

		// Token: 0x04000158 RID: 344
		private const int K = 256;

		// Token: 0x04000159 RID: 345
		private static readonly NativeParallelDriver.LoopBodyHolder[] _loopBodyCache = new NativeParallelDriver.LoopBodyHolder[256];

		// Token: 0x0400015A RID: 346
		private static readonly NativeParallelDriver.LoopBodyWithDtHolder[] _loopBodyWithDtCache = new NativeParallelDriver.LoopBodyWithDtHolder[256];

		// Token: 0x020000CB RID: 203
		private struct LoopBodyHolder
		{
			// Token: 0x0400042E RID: 1070
			public static long UniqueLoopBodyKeySeed;

			// Token: 0x0400042F RID: 1071
			public TWParallel.ParallelForAuxPredicate LoopBody;
		}

		// Token: 0x020000CC RID: 204
		private struct LoopBodyWithDtHolder
		{
			// Token: 0x04000430 RID: 1072
			public static long UniqueLoopBodyKeySeed;

			// Token: 0x04000431 RID: 1073
			public TWParallel.ParallelForWithDtAuxPredicate LoopBody;

			// Token: 0x04000432 RID: 1074
			public float DeltaTime;
		}
	}
}
