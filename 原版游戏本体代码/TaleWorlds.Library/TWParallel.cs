using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x0200009B RID: 155
	public static class TWParallel
	{
		// Token: 0x0600057B RID: 1403 RVA: 0x0001360E File Offset: 0x0001180E
		public static void InitializeAndSetImplementation(IParallelDriver parallelDriver)
		{
			TWParallel._parallelDriver = parallelDriver;
			TWParallel._mainThreadId = TWParallel.GetMainThreadId();
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x00013620 File Offset: 0x00011820
		public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
		{
			return Parallel.ForEach<TSource>(Partitioner.Create<TSource>(source), Common.ParallelOptions, body);
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x00013633 File Offset: 0x00011833
		[Obsolete("Please use For() not ForEach() for better Parallel Performance.", true)]
		public static void ForEach<TSource>(IList<TSource> source, Action<TSource> body)
		{
			Parallel.ForEach<TSource>(Partitioner.Create<TSource>(source), Common.ParallelOptions, body);
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x00013647 File Offset: 0x00011847
		public static void For(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize = 16)
		{
			if (toExclusive - fromInclusive < grainSize)
			{
				body(fromInclusive, toExclusive);
				return;
			}
			TWParallel._parallelDriver.For(fromInclusive, toExclusive, body, grainSize);
		}

		// Token: 0x0600057F RID: 1407 RVA: 0x00013666 File Offset: 0x00011866
		public static void ForWithoutRenderThread(int fromInclusive, int toExclusive, TWParallel.ParallelForAuxPredicate body, int grainSize = 16)
		{
			if (toExclusive - fromInclusive < grainSize)
			{
				body(fromInclusive, toExclusive);
				return;
			}
			TWParallel._parallelDriver.ForWithoutRenderThread(fromInclusive, toExclusive, body, grainSize);
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x00013685 File Offset: 0x00011885
		public static void For(int fromInclusive, int toExclusive, float deltaTime, TWParallel.ParallelForWithDtAuxPredicate body, int grainSize = 16)
		{
			if (toExclusive - fromInclusive < grainSize)
			{
				body(fromInclusive, toExclusive, deltaTime);
				return;
			}
			TWParallel._parallelDriver.For(fromInclusive, toExclusive, deltaTime, body, grainSize);
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x000136A8 File Offset: 0x000118A8
		[Conditional("_RGL_KEEP_ASSERTS")]
		public static void AssertIsMainThread()
		{
			TWParallel.GetCurrentThreadId();
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x000136B0 File Offset: 0x000118B0
		public static bool IsMainThread()
		{
			return TWParallel._mainThreadId == TWParallel.GetCurrentThreadId();
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x000136BE File Offset: 0x000118BE
		private static ulong GetMainThreadId()
		{
			return TWParallel._parallelDriver.GetMainThreadId();
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x000136CA File Offset: 0x000118CA
		internal static ulong GetCurrentThreadId()
		{
			return TWParallel._parallelDriver.GetCurrentThreadId();
		}

		// Token: 0x040001AD RID: 429
		private static IParallelDriver _parallelDriver = new DefaultParallelDriver();

		// Token: 0x040001AE RID: 430
		private static ulong _mainThreadId;

		// Token: 0x020000EA RID: 234
		// (Invoke) Token: 0x060007B5 RID: 1973
		public delegate void ParallelForAuxPredicate(int localStartIndex, int localEndIndex);

		// Token: 0x020000EB RID: 235
		// (Invoke) Token: 0x060007B9 RID: 1977
		public delegate void ParallelForWithDtAuxPredicate(int localStartIndex, int localEndIndex, float dt);
	}
}
