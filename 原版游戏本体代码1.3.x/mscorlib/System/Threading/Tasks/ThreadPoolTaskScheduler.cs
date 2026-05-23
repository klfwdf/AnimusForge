using System;
using System.Collections.Generic;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x0200057A RID: 1402
	internal sealed class ThreadPoolTaskScheduler : TaskScheduler
	{
		// Token: 0x06004224 RID: 16932 RVA: 0x000F65F4 File Offset: 0x000F47F4
		internal ThreadPoolTaskScheduler()
		{
			int id = base.Id;
		}

		// Token: 0x06004225 RID: 16933 RVA: 0x000F6610 File Offset: 0x000F4810
		private static void LongRunningThreadWork(object obj)
		{
			Task task = obj as Task;
			task.ExecuteEntry(false);
		}

		// Token: 0x06004226 RID: 16934 RVA: 0x000F662C File Offset: 0x000F482C
		[SecurityCritical]
		protected internal override void QueueTask(Task task)
		{
			if ((task.Options & TaskCreationOptions.LongRunning) != TaskCreationOptions.None)
			{
				new Thread(ThreadPoolTaskScheduler.s_longRunningThreadWork)
				{
					IsBackground = true
				}.Start(task);
				return;
			}
			bool forceGlobal = (task.Options & TaskCreationOptions.PreferFairness) > TaskCreationOptions.None;
			ThreadPool.UnsafeQueueCustomWorkItem(task, forceGlobal);
		}

		// Token: 0x06004227 RID: 16935 RVA: 0x000F6670 File Offset: 0x000F4870
		[SecurityCritical]
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (taskWasPreviouslyQueued && !ThreadPool.TryPopCustomWorkItem(task))
			{
				return false;
			}
			bool result = false;
			try
			{
				result = task.ExecuteEntry(false);
			}
			finally
			{
				if (taskWasPreviouslyQueued)
				{
					this.NotifyWorkItemProgress();
				}
			}
			return result;
		}

		// Token: 0x06004228 RID: 16936 RVA: 0x000F66B4 File Offset: 0x000F48B4
		[SecurityCritical]
		protected internal override bool TryDequeue(Task task)
		{
			return ThreadPool.TryPopCustomWorkItem(task);
		}

		// Token: 0x06004229 RID: 16937 RVA: 0x000F66BC File Offset: 0x000F48BC
		[SecurityCritical]
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return this.FilterTasksFromWorkItems(ThreadPool.GetQueuedWorkItems());
		}

		// Token: 0x0600422A RID: 16938 RVA: 0x000F66C9 File Offset: 0x000F48C9
		private IEnumerable<Task> FilterTasksFromWorkItems(IEnumerable<IThreadPoolWorkItem> tpwItems)
		{
			foreach (IThreadPoolWorkItem threadPoolWorkItem in tpwItems)
			{
				if (threadPoolWorkItem is Task)
				{
					yield return (Task)threadPoolWorkItem;
				}
			}
			IEnumerator<IThreadPoolWorkItem> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600422B RID: 16939 RVA: 0x000F66D9 File Offset: 0x000F48D9
		internal override void NotifyWorkItemProgress()
		{
			ThreadPool.NotifyWorkItemProgress();
		}

		// Token: 0x170009D2 RID: 2514
		// (get) Token: 0x0600422C RID: 16940 RVA: 0x000F66E0 File Offset: 0x000F48E0
		internal override bool RequiresAtomicStartTransition
		{
			get
			{
				return false;
			}
		}

		// Token: 0x04001B78 RID: 7032
		private static readonly ParameterizedThreadStart s_longRunningThreadWork = new ParameterizedThreadStart(ThreadPoolTaskScheduler.LongRunningThreadWork);
	}
}
