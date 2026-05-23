using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public sealed class NativeParallelDriver : IParallelDriver
{
	private struct LoopBodyHolder
	{
		public static long UniqueLoopBodyKeySeed;

		public TWParallel.ParallelForAuxPredicate LoopBody;
	}

	private struct LoopBodyWithDtHolder
	{
		public static long UniqueLoopBodyKeySeed;

		public TWParallel.ParallelForWithDtAuxPredicate LoopBody;

		public float DeltaTime;
	}

	private const int K = 256;

	private static readonly LoopBodyHolder[] _loopBodyCache = new LoopBodyHolder[256];

	private static readonly LoopBodyWithDtHolder[] _loopBodyWithDtCache = new LoopBodyWithDtHolder[256];

	public void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate loopBody, int grainSize)
	{
		long num = Interlocked.Increment(ref LoopBodyHolder.UniqueLoopBodyKeySeed) % 256;
		_loopBodyCache[num].LoopBody = loopBody;
		Utilities.ParallelFor(fromInclusive, toExclusive, num, grainSize);
		_loopBodyCache[num].LoopBody = null;
	}

	public void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate loopBody, int grainSize)
	{
		long num = Interlocked.Increment(ref LoopBodyHolder.UniqueLoopBodyKeySeed) % 256;
		_loopBodyCache[num].LoopBody = loopBody;
		Utilities.ParallelForWithoutRenderThread(fromInclusive, toExclusive, num, grainSize);
		_loopBodyCache[num].LoopBody = null;
	}

	public void ForWithoutRenderThreadDt(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate loopBody, int grainSize)
	{
		long num = Interlocked.Increment(ref LoopBodyWithDtHolder.UniqueLoopBodyKeySeed) % 256;
		_loopBodyWithDtCache[num].LoopBody = loopBody;
		_loopBodyWithDtCache[num].DeltaTime = deltaTime;
		Utilities.ParallelForWithoutRenderThreadDt(fromInclusive, toExclusive, num, grainSize);
		_loopBodyWithDtCache[num].LoopBody = null;
	}

	public void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate loopBody, int grainSize)
	{
		long num = Interlocked.Increment(ref LoopBodyWithDtHolder.UniqueLoopBodyKeySeed) % 256;
		_loopBodyWithDtCache[num].LoopBody = loopBody;
		_loopBodyWithDtCache[num].DeltaTime = deltaTime;
		Utilities.ParallelForWithDt(fromInclusive, toExclusive, num, grainSize);
		_loopBodyWithDtCache[num].LoopBody = null;
	}

	public ulong GetMainThreadId()
	{
		return Utilities.GetMainThreadId();
	}

	public ulong GetCurrentThreadId()
	{
		return Utilities.GetCurrentThreadId();
	}

	[EngineCallback(null, false)]
	internal static void ParalelForLoopBodyCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
	{
		_loopBodyCache[loopBodyKey].LoopBody(localStartIndex, localEndIndex);
	}

	[EngineCallback(null, false)]
	internal static void ParalelForLoopBodyWithDtCaller(long loopBodyKey, int localStartIndex, int localEndIndex)
	{
		_loopBodyWithDtCache[loopBodyKey].LoopBody(localStartIndex, localEndIndex, _loopBodyWithDtCache[loopBodyKey].DeltaTime);
	}
}
