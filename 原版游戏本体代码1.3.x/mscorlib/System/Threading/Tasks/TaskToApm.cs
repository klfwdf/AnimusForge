using System;
using System.IO;

namespace System.Threading.Tasks
{
	// Token: 0x02000589 RID: 1417
	internal static class TaskToApm
	{
		// Token: 0x060042A3 RID: 17059 RVA: 0x000F8544 File Offset: 0x000F6744
		public static IAsyncResult Begin(Task task, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			if (task.IsCompleted)
			{
				asyncResult = new TaskToApm.TaskWrapperAsyncResult(task, state, true);
				if (callback != null)
				{
					callback(asyncResult);
				}
			}
			else
			{
				IAsyncResult asyncResult3;
				if (task.AsyncState != state)
				{
					IAsyncResult asyncResult2 = new TaskToApm.TaskWrapperAsyncResult(task, state, false);
					asyncResult3 = asyncResult2;
				}
				else
				{
					asyncResult3 = task;
				}
				asyncResult = asyncResult3;
				if (callback != null)
				{
					TaskToApm.InvokeCallbackWhenTaskCompletes(task, callback, asyncResult);
				}
			}
			return asyncResult;
		}

		// Token: 0x060042A4 RID: 17060 RVA: 0x000F8594 File Offset: 0x000F6794
		public static void End(IAsyncResult asyncResult)
		{
			TaskToApm.TaskWrapperAsyncResult taskWrapperAsyncResult = asyncResult as TaskToApm.TaskWrapperAsyncResult;
			Task task;
			if (taskWrapperAsyncResult != null)
			{
				task = taskWrapperAsyncResult.Task;
			}
			else
			{
				task = asyncResult as Task;
			}
			if (task == null)
			{
				__Error.WrongAsyncResult();
			}
			task.GetAwaiter().GetResult();
		}

		// Token: 0x060042A5 RID: 17061 RVA: 0x000F85D4 File Offset: 0x000F67D4
		public static TResult End<TResult>(IAsyncResult asyncResult)
		{
			TaskToApm.TaskWrapperAsyncResult taskWrapperAsyncResult = asyncResult as TaskToApm.TaskWrapperAsyncResult;
			Task<TResult> task;
			if (taskWrapperAsyncResult != null)
			{
				task = taskWrapperAsyncResult.Task as Task<TResult>;
			}
			else
			{
				task = asyncResult as Task<TResult>;
			}
			if (task == null)
			{
				__Error.WrongAsyncResult();
			}
			return task.GetAwaiter().GetResult();
		}

		// Token: 0x060042A6 RID: 17062 RVA: 0x000F8618 File Offset: 0x000F6818
		private static void InvokeCallbackWhenTaskCompletes(Task antecedent, AsyncCallback callback, IAsyncResult asyncResult)
		{
			antecedent.ConfigureAwait(false).GetAwaiter().OnCompleted(delegate
			{
				callback(asyncResult);
			});
		}

		// Token: 0x02000C34 RID: 3124
		private sealed class TaskWrapperAsyncResult : IAsyncResult
		{
			// Token: 0x06007034 RID: 28724 RVA: 0x00182D9E File Offset: 0x00180F9E
			internal TaskWrapperAsyncResult(Task task, object state, bool completedSynchronously)
			{
				this.Task = task;
				this.m_state = state;
				this.m_completedSynchronously = completedSynchronously;
			}

			// Token: 0x1700133A RID: 4922
			// (get) Token: 0x06007035 RID: 28725 RVA: 0x00182DBB File Offset: 0x00180FBB
			object IAsyncResult.AsyncState
			{
				get
				{
					return this.m_state;
				}
			}

			// Token: 0x1700133B RID: 4923
			// (get) Token: 0x06007036 RID: 28726 RVA: 0x00182DC3 File Offset: 0x00180FC3
			bool IAsyncResult.CompletedSynchronously
			{
				get
				{
					return this.m_completedSynchronously;
				}
			}

			// Token: 0x1700133C RID: 4924
			// (get) Token: 0x06007037 RID: 28727 RVA: 0x00182DCB File Offset: 0x00180FCB
			bool IAsyncResult.IsCompleted
			{
				get
				{
					return this.Task.IsCompleted;
				}
			}

			// Token: 0x1700133D RID: 4925
			// (get) Token: 0x06007038 RID: 28728 RVA: 0x00182DD8 File Offset: 0x00180FD8
			WaitHandle IAsyncResult.AsyncWaitHandle
			{
				get
				{
					return ((IAsyncResult)this.Task).AsyncWaitHandle;
				}
			}

			// Token: 0x04003722 RID: 14114
			internal readonly Task Task;

			// Token: 0x04003723 RID: 14115
			private readonly object m_state;

			// Token: 0x04003724 RID: 14116
			private readonly bool m_completedSynchronously;
		}
	}
}
