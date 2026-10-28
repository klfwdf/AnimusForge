using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public sealed class DefaultParallelDriver : IParallelDriver
{
	public void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize)
	{
		Parallel.ForEach(Partitioner.Create(fromInclusive, toExclusive, grainSize), Common.ParallelOptions, delegate(Tuple<int, int> range, ParallelLoopState loopState)
		{
			body(range.Item1, range.Item2);
		});
	}

	public void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize)
	{
		For(fromInclusive, toExclusive, body, grainSize);
	}

	public void ForWithoutRenderThreadDt(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize)
	{
		For(fromInclusive, toExclusive, deltaTime, body, grainSize);
	}

	public void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize)
	{
		Parallel.ForEach(Partitioner.Create(fromInclusive, toExclusive, grainSize), Common.ParallelOptions, delegate(Tuple<int, int> range, ParallelLoopState loopState)
		{
			body(range.Item1, range.Item2, deltaTime);
		});
	}

	public ulong GetMainThreadId()
	{
		return 0uL;
	}

	public ulong GetCurrentThreadId()
	{
		return 0uL;
	}
}
