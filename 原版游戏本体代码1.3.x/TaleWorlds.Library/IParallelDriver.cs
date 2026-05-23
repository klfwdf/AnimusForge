using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000099 RID: 153
	public interface IParallelDriver
	{
		// Token: 0x06000570 RID: 1392
		void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize);

		// Token: 0x06000571 RID: 1393
		void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize);

		// Token: 0x06000572 RID: 1394
		void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize);

		// Token: 0x06000573 RID: 1395
		ulong GetMainThreadId();

		// Token: 0x06000574 RID: 1396
		ulong GetCurrentThreadId();
	}
}
