using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading.Tasks
{
	// Token: 0x02000577 RID: 1399
	[DebuggerDisplay("Id={Id}")]
	[DebuggerTypeProxy(typeof(TaskScheduler.SystemThreadingTasks_TaskSchedulerDebugView))]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
	public abstract class TaskScheduler
	{
		// Token: 0x06004202 RID: 16898
		[SecurityCritical]
		[__DynamicallyInvokable]
		protected internal abstract void QueueTask(Task task);

		// Token: 0x06004203 RID: 16899
		[SecurityCritical]
		[__DynamicallyInvokable]
		protected abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);

		// Token: 0x06004204 RID: 16900
		[SecurityCritical]
		[__DynamicallyInvokable]
		protected abstract IEnumerable<Task> GetScheduledTasks();

		// Token: 0x170009C9 RID: 2505
		// (get) Token: 0x06004205 RID: 16901 RVA: 0x000F619E File Offset: 0x000F439E
		[__DynamicallyInvokable]
		public virtual int MaximumConcurrencyLevel
		{
			[__DynamicallyInvokable]
			get
			{
				return int.MaxValue;
			}
		}

		// Token: 0x06004206 RID: 16902 RVA: 0x000F61A8 File Offset: 0x000F43A8
		[SecuritySafeCritical]
		internal bool TryRunInline(Task task, bool taskWasPreviouslyQueued)
		{
			TaskScheduler executingTaskScheduler = task.ExecutingTaskScheduler;
			if (executingTaskScheduler != this && executingTaskScheduler != null)
			{
				return executingTaskScheduler.TryRunInline(task, taskWasPreviouslyQueued);
			}
			StackGuard currentStackGuard;
			if (executingTaskScheduler == null || task.m_action == null || task.IsDelegateInvoked || task.IsCanceled || !(currentStackGuard = Task.CurrentStackGuard).TryBeginInliningScope())
			{
				return false;
			}
			bool flag = false;
			try
			{
				task.FireTaskScheduledIfNeeded(this);
				flag = this.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
			}
			finally
			{
				currentStackGuard.EndInliningScope();
			}
			if (flag && !task.IsDelegateInvoked && !task.IsCanceled)
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_InconsistentStateAfterTryExecuteTaskInline"));
			}
			return flag;
		}

		// Token: 0x06004207 RID: 16903 RVA: 0x000F6248 File Offset: 0x000F4448
		[SecurityCritical]
		[__DynamicallyInvokable]
		protected internal virtual bool TryDequeue(Task task)
		{
			return false;
		}

		// Token: 0x06004208 RID: 16904 RVA: 0x000F624B File Offset: 0x000F444B
		internal virtual void NotifyWorkItemProgress()
		{
		}

		// Token: 0x170009CA RID: 2506
		// (get) Token: 0x06004209 RID: 16905 RVA: 0x000F624D File Offset: 0x000F444D
		internal virtual bool RequiresAtomicStartTransition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600420A RID: 16906 RVA: 0x000F6250 File Offset: 0x000F4450
		[SecurityCritical]
		internal void InternalQueueTask(Task task)
		{
			task.FireTaskScheduledIfNeeded(this);
			this.QueueTask(task);
		}

		// Token: 0x0600420B RID: 16907 RVA: 0x000F6261 File Offset: 0x000F4461
		[__DynamicallyInvokable]
		protected TaskScheduler()
		{
			if (Debugger.IsAttached)
			{
				this.AddToActiveTaskSchedulers();
			}
		}

		// Token: 0x0600420C RID: 16908 RVA: 0x000F6278 File Offset: 0x000F4478
		private void AddToActiveTaskSchedulers()
		{
			ConditionalWeakTable<TaskScheduler, object> conditionalWeakTable = TaskScheduler.s_activeTaskSchedulers;
			if (conditionalWeakTable == null)
			{
				Interlocked.CompareExchange<ConditionalWeakTable<TaskScheduler, object>>(ref TaskScheduler.s_activeTaskSchedulers, new ConditionalWeakTable<TaskScheduler, object>(), null);
				conditionalWeakTable = TaskScheduler.s_activeTaskSchedulers;
			}
			conditionalWeakTable.Add(this, null);
		}

		// Token: 0x170009CB RID: 2507
		// (get) Token: 0x0600420D RID: 16909 RVA: 0x000F62AD File Offset: 0x000F44AD
		[__DynamicallyInvokable]
		public static TaskScheduler Default
		{
			[__DynamicallyInvokable]
			get
			{
				return TaskScheduler.s_defaultTaskScheduler;
			}
		}

		// Token: 0x170009CC RID: 2508
		// (get) Token: 0x0600420E RID: 16910 RVA: 0x000F62B4 File Offset: 0x000F44B4
		[__DynamicallyInvokable]
		public static TaskScheduler Current
		{
			[__DynamicallyInvokable]
			get
			{
				TaskScheduler internalCurrent = TaskScheduler.InternalCurrent;
				return internalCurrent ?? TaskScheduler.Default;
			}
		}

		// Token: 0x170009CD RID: 2509
		// (get) Token: 0x0600420F RID: 16911 RVA: 0x000F62D4 File Offset: 0x000F44D4
		internal static TaskScheduler InternalCurrent
		{
			get
			{
				Task internalCurrent = Task.InternalCurrent;
				if (internalCurrent == null || (internalCurrent.CreationOptions & TaskCreationOptions.HideScheduler) != TaskCreationOptions.None)
				{
					return null;
				}
				return internalCurrent.ExecutingTaskScheduler;
			}
		}

		// Token: 0x06004210 RID: 16912 RVA: 0x000F62FD File Offset: 0x000F44FD
		[__DynamicallyInvokable]
		public static TaskScheduler FromCurrentSynchronizationContext()
		{
			return new SynchronizationContextTaskScheduler();
		}

		// Token: 0x170009CE RID: 2510
		// (get) Token: 0x06004211 RID: 16913 RVA: 0x000F6304 File Offset: 0x000F4504
		[__DynamicallyInvokable]
		public int Id
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_taskSchedulerId == 0)
				{
					int num;
					do
					{
						num = Interlocked.Increment(ref TaskScheduler.s_taskSchedulerIdCounter);
					}
					while (num == 0);
					Interlocked.CompareExchange(ref this.m_taskSchedulerId, num, 0);
				}
				return this.m_taskSchedulerId;
			}
		}

		// Token: 0x06004212 RID: 16914 RVA: 0x000F6341 File Offset: 0x000F4541
		[SecurityCritical]
		[__DynamicallyInvokable]
		protected bool TryExecuteTask(Task task)
		{
			if (task.ExecutingTaskScheduler != this)
			{
				throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_ExecuteTask_WrongTaskScheduler"));
			}
			return task.ExecuteEntry(true);
		}

		// Token: 0x14000019 RID: 25
		// (add) Token: 0x06004213 RID: 16915 RVA: 0x000F6364 File Offset: 0x000F4564
		// (remove) Token: 0x06004214 RID: 16916 RVA: 0x000F63BC File Offset: 0x000F45BC
		[__DynamicallyInvokable]
		public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException
		{
			[SecurityCritical]
			[__DynamicallyInvokable]
			add
			{
				if (value != null)
				{
					RuntimeHelpers.PrepareContractedDelegate(value);
					object unobservedTaskExceptionLockObject = TaskScheduler._unobservedTaskExceptionLockObject;
					lock (unobservedTaskExceptionLockObject)
					{
						TaskScheduler._unobservedTaskException = (EventHandler<UnobservedTaskExceptionEventArgs>)Delegate.Combine(TaskScheduler._unobservedTaskException, value);
					}
				}
			}
			[SecurityCritical]
			[__DynamicallyInvokable]
			remove
			{
				object unobservedTaskExceptionLockObject = TaskScheduler._unobservedTaskExceptionLockObject;
				lock (unobservedTaskExceptionLockObject)
				{
					TaskScheduler._unobservedTaskException = (EventHandler<UnobservedTaskExceptionEventArgs>)Delegate.Remove(TaskScheduler._unobservedTaskException, value);
				}
			}
		}

		// Token: 0x06004215 RID: 16917 RVA: 0x000F640C File Offset: 0x000F460C
		internal static void PublishUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs ueea)
		{
			object unobservedTaskExceptionLockObject = TaskScheduler._unobservedTaskExceptionLockObject;
			lock (unobservedTaskExceptionLockObject)
			{
				EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskException = TaskScheduler._unobservedTaskException;
				if (unobservedTaskException != null)
				{
					unobservedTaskException(sender, ueea);
				}
			}
		}

		// Token: 0x06004216 RID: 16918 RVA: 0x000F6458 File Offset: 0x000F4658
		[SecurityCritical]
		internal Task[] GetScheduledTasksForDebugger()
		{
			IEnumerable<Task> scheduledTasks = this.GetScheduledTasks();
			if (scheduledTasks == null)
			{
				return null;
			}
			Task[] array = scheduledTasks as Task[];
			if (array == null)
			{
				array = new List<Task>(scheduledTasks).ToArray();
			}
			foreach (Task task in array)
			{
				int id = task.Id;
			}
			return array;
		}

		// Token: 0x06004217 RID: 16919 RVA: 0x000F64A8 File Offset: 0x000F46A8
		[SecurityCritical]
		internal static TaskScheduler[] GetTaskSchedulersForDebugger()
		{
			if (TaskScheduler.s_activeTaskSchedulers == null)
			{
				return new TaskScheduler[] { TaskScheduler.s_defaultTaskScheduler };
			}
			ICollection<TaskScheduler> keys = TaskScheduler.s_activeTaskSchedulers.Keys;
			if (!keys.Contains(TaskScheduler.s_defaultTaskScheduler))
			{
				keys.Add(TaskScheduler.s_defaultTaskScheduler);
			}
			TaskScheduler[] array = new TaskScheduler[keys.Count];
			keys.CopyTo(array, 0);
			foreach (TaskScheduler taskScheduler in array)
			{
				int id = taskScheduler.Id;
			}
			return array;
		}

		// Token: 0x04001B6E RID: 7022
		private static ConditionalWeakTable<TaskScheduler, object> s_activeTaskSchedulers;

		// Token: 0x04001B6F RID: 7023
		private static readonly TaskScheduler s_defaultTaskScheduler = new ThreadPoolTaskScheduler();

		// Token: 0x04001B70 RID: 7024
		internal static int s_taskSchedulerIdCounter;

		// Token: 0x04001B71 RID: 7025
		private volatile int m_taskSchedulerId;

		// Token: 0x04001B72 RID: 7026
		private static EventHandler<UnobservedTaskExceptionEventArgs> _unobservedTaskException;

		// Token: 0x04001B73 RID: 7027
		private static readonly object _unobservedTaskExceptionLockObject = new object();

		// Token: 0x02000C23 RID: 3107
		internal sealed class SystemThreadingTasks_TaskSchedulerDebugView
		{
			// Token: 0x06007002 RID: 28674 RVA: 0x0018270E File Offset: 0x0018090E
			public SystemThreadingTasks_TaskSchedulerDebugView(TaskScheduler scheduler)
			{
				this.m_taskScheduler = scheduler;
			}

			// Token: 0x1700132C RID: 4908
			// (get) Token: 0x06007003 RID: 28675 RVA: 0x0018271D File Offset: 0x0018091D
			public int Id
			{
				get
				{
					return this.m_taskScheduler.Id;
				}
			}

			// Token: 0x1700132D RID: 4909
			// (get) Token: 0x06007004 RID: 28676 RVA: 0x0018272A File Offset: 0x0018092A
			public IEnumerable<Task> ScheduledTasks
			{
				[SecurityCritical]
				get
				{
					return this.m_taskScheduler.GetScheduledTasks();
				}
			}

			// Token: 0x040036D8 RID: 14040
			private readonly TaskScheduler m_taskScheduler;
		}
	}
}
