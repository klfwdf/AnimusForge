namespace TaleWorlds.Library;

public interface IParallelDriver
{
	void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize);

	void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize);

	void ForWithoutRenderThreadDt(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize);

	void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize);

	ulong GetMainThreadId();

	ulong GetCurrentThreadId();
}
