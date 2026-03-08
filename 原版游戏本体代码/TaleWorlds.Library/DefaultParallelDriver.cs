using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x0200009A RID: 154
	public sealed class DefaultParallelDriver : IParallelDriver
	{
		// Token: 0x06000575 RID: 1397 RVA: 0x00013574 File Offset: 0x00011774
		public void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize)
		{
			Parallel.ForEach<Tuple<int, int>>(Partitioner.Create(fromInclusive, toExclusive, grainSize), Common.ParallelOptions, delegate(Tuple<int, int> range, ParallelLoopState loopState)
			{
				body(range.Item1, range.Item2);
			});
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x000135AE File Offset: 0x000117AE
		public void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize)
		{
			this.For(fromInclusive, toExclusive, body, grainSize);
		}

		// Token: 0x06000577 RID: 1399 RVA: 0x000135BC File Offset: 0x000117BC
		public void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize)
		{
			Parallel.ForEach<Tuple<int, int>>(Partitioner.Create(fromInclusive, toExclusive, grainSize), Common.ParallelOptions, delegate(Tuple<int, int> range, ParallelLoopState loopState)
			{
				body(range.Item1, range.Item2, deltaTime);
			});
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x000135FE File Offset: 0x000117FE
		public ulong GetMainThreadId()
		{
			return 0UL;
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x00013602 File Offset: 0x00011802
		public ulong GetCurrentThreadId()
		{
			return 0UL;
		}
	}
}
