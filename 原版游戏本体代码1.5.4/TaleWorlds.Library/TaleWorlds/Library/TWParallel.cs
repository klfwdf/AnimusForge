using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public static class TWParallel
{
	public delegate void ParallelForAuxPredicate(int localStartIndex, int localEndIndex);

	public delegate void ParallelForWithDtAuxPredicate(int localStartIndex, int localEndIndex, float dt);

	private static IParallelDriver _parallelDriver = new DefaultParallelDriver();

	private static ulong _mainThreadId;

	public static void InitializeAndSetImplementation(IParallelDriver parallelDriver)
	{
		_parallelDriver = parallelDriver;
		_mainThreadId = GetMainThreadId();
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
	{
		return Parallel.ForEach(Partitioner.Create(source), Common.ParallelOptions, body);
	}

	[Obsolete("Please use For() not ForEach() for better Parallel Performance.", true)]
	public static void ForEach<TSource>(IList<TSource> source, Action<TSource> body)
	{
		Parallel.ForEach(Partitioner.Create(source), Common.ParallelOptions, body);
	}

	public static void For(int fromInclusive, int toExclusive, ParallelForAuxPredicate body, int grainSize = 16)
	{
		if (toExclusive - fromInclusive < grainSize)
		{
			body(fromInclusive, toExclusive);
		}
		else
		{
			_parallelDriver.For(fromInclusive, toExclusive, body, grainSize);
		}
	}

	public static void ForWithoutRenderThread(int fromInclusive, int toExclusive, ParallelForAuxPredicate body, int grainSize = 16)
	{
		if (toExclusive - fromInclusive < grainSize)
		{
			body(fromInclusive, toExclusive);
		}
		else
		{
			_parallelDriver.ForWithoutRenderThread(fromInclusive, toExclusive, body, grainSize);
		}
	}

	public static void ForWithoutRenderThreadDt(int fromInclusive, int toExclusive, float deltaTime, ParallelForWithDtAuxPredicate body, int grainSize = 16)
	{
		if (toExclusive - fromInclusive < grainSize)
		{
			body(fromInclusive, toExclusive, deltaTime);
		}
		else
		{
			_parallelDriver.ForWithoutRenderThreadDt(fromInclusive, toExclusive, deltaTime, body, grainSize);
		}
	}

	public static void For(int fromInclusive, int toExclusive, float deltaTime, ParallelForWithDtAuxPredicate body, int grainSize = 16)
	{
		if (toExclusive - fromInclusive < grainSize)
		{
			body(fromInclusive, toExclusive, deltaTime);
		}
		else
		{
			_parallelDriver.For(fromInclusive, toExclusive, deltaTime, body, grainSize);
		}
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	public static void AssertIsMainThread()
	{
		GetCurrentThreadId();
	}

	public static bool IsMainThread()
	{
		return _mainThreadId == GetCurrentThreadId();
	}

	private static ulong GetMainThreadId()
	{
		return _parallelDriver.GetMainThreadId();
	}

	internal static ulong GetCurrentThreadId()
	{
		return _parallelDriver.GetCurrentThreadId();
	}
}
