using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008F1 RID: 2289
	internal static class AsyncTaskCache
	{
		// Token: 0x06005E2D RID: 24109 RVA: 0x0014B010 File Offset: 0x00149210
		private static Task<int>[] CreateInt32Tasks()
		{
			Task<int>[] array = new Task<int>[10];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = AsyncTaskCache.CreateCacheableTask<int>(i + -1);
			}
			return array;
		}

		// Token: 0x06005E2E RID: 24110 RVA: 0x0014B040 File Offset: 0x00149240
		internal static Task<TResult> CreateCacheableTask<TResult>(TResult result)
		{
			return new Task<TResult>(false, result, (TaskCreationOptions)16384, default(CancellationToken));
		}

		// Token: 0x04002A4F RID: 10831
		internal static readonly Task<bool> TrueTask = AsyncTaskCache.CreateCacheableTask<bool>(true);

		// Token: 0x04002A50 RID: 10832
		internal static readonly Task<bool> FalseTask = AsyncTaskCache.CreateCacheableTask<bool>(false);

		// Token: 0x04002A51 RID: 10833
		internal static readonly Task<int>[] Int32Tasks = AsyncTaskCache.CreateInt32Tasks();

		// Token: 0x04002A52 RID: 10834
		internal const int INCLUSIVE_INT32_MIN = -1;

		// Token: 0x04002A53 RID: 10835
		internal const int EXCLUSIVE_INT32_MAX = 9;
	}
}
