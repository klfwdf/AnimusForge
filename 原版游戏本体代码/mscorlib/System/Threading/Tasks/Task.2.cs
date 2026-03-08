using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading.Tasks
{
	// Token: 0x0200055E RID: 1374
	[DebuggerTypeProxy(typeof(SystemThreadingTasks_TaskDebugView))]
	[DebuggerDisplay("Id = {Id}, Status = {Status}, Method = {DebuggerDisplayMethodDescription}")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class Task : IThreadPoolWorkItem, IAsyncResult, IDisposable
	{
		// Token: 0x06004072 RID: 16498 RVA: 0x000F0698 File Offset: 0x000EE898
		[FriendAccessAllowed]
		internal static bool AddToActiveTasks(Task task)
		{
			object obj = Task.s_activeTasksLock;
			lock (obj)
			{
				Task.s_currentActiveTasks[task.Id] = task;
			}
			return true;
		}

		// Token: 0x06004073 RID: 16499 RVA: 0x000F06E4 File Offset: 0x000EE8E4
		[FriendAccessAllowed]
		internal static void RemoveFromActiveTasks(int taskId)
		{
			object obj = Task.s_activeTasksLock;
			lock (obj)
			{
				Task.s_currentActiveTasks.Remove(taskId);
			}
		}

		// Token: 0x06004074 RID: 16500 RVA: 0x000F072C File Offset: 0x000EE92C
		internal Task(bool canceled, TaskCreationOptions creationOptions, CancellationToken ct)
		{
			if (canceled)
			{
				this.m_stateFlags = (int)((TaskCreationOptions)5242880 | creationOptions);
				Task.ContingentProperties contingentProperties = (this.m_contingentProperties = new Task.ContingentProperties());
				contingentProperties.m_cancellationToken = ct;
				contingentProperties.m_internalCancellationRequested = 1;
				return;
			}
			this.m_stateFlags = (int)((TaskCreationOptions)16777216 | creationOptions);
		}

		// Token: 0x06004075 RID: 16501 RVA: 0x000F0782 File Offset: 0x000EE982
		internal Task()
		{
			this.m_stateFlags = 33555456;
		}

		// Token: 0x06004076 RID: 16502 RVA: 0x000F0798 File Offset: 0x000EE998
		internal Task(object state, TaskCreationOptions creationOptions, bool promiseStyle)
		{
			if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.RunContinuationsAsynchronously)) != TaskCreationOptions.None)
			{
				throw new ArgumentOutOfRangeException("creationOptions");
			}
			if ((creationOptions & TaskCreationOptions.AttachedToParent) != TaskCreationOptions.None)
			{
				this.m_parent = Task.InternalCurrent;
			}
			this.TaskConstructorCore(null, state, default(CancellationToken), creationOptions, InternalTaskOptions.PromiseTask, null);
		}

		// Token: 0x06004077 RID: 16503 RVA: 0x000F07E4 File Offset: 0x000EE9E4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action action)
			: this(action, null, null, default(CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x06004078 RID: 16504 RVA: 0x000F0810 File Offset: 0x000EEA10
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action action, CancellationToken cancellationToken)
			: this(action, null, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x06004079 RID: 16505 RVA: 0x000F0834 File Offset: 0x000EEA34
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action action, TaskCreationOptions creationOptions)
			: this(action, null, Task.InternalCurrentIfAttached(creationOptions), default(CancellationToken), creationOptions, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407A RID: 16506 RVA: 0x000F0868 File Offset: 0x000EEA68
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(action, null, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407B RID: 16507 RVA: 0x000F0894 File Offset: 0x000EEA94
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action<object> action, object state)
			: this(action, state, null, default(CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407C RID: 16508 RVA: 0x000F08C0 File Offset: 0x000EEAC0
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action<object> action, object state, CancellationToken cancellationToken)
			: this(action, state, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407D RID: 16509 RVA: 0x000F08E4 File Offset: 0x000EEAE4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action<object> action, object state, TaskCreationOptions creationOptions)
			: this(action, state, Task.InternalCurrentIfAttached(creationOptions), default(CancellationToken), creationOptions, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407E RID: 16510 RVA: 0x000F0918 File Offset: 0x000EEB18
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
			: this(action, state, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PossiblyCaptureContext(ref stackCrawlMark);
		}

		// Token: 0x0600407F RID: 16511 RVA: 0x000F0943 File Offset: 0x000EEB43
		internal Task(Action<object> action, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark)
			: this(action, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
		{
			this.PossiblyCaptureContext(ref stackMark);
		}

		// Token: 0x06004080 RID: 16512 RVA: 0x000F095E File Offset: 0x000EEB5E
		internal Task(Delegate action, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			if ((creationOptions & TaskCreationOptions.AttachedToParent) != TaskCreationOptions.None || (internalOptions & InternalTaskOptions.SelfReplicating) != InternalTaskOptions.None)
			{
				this.m_parent = parent;
			}
			this.TaskConstructorCore(action, state, cancellationToken, creationOptions, internalOptions, scheduler);
		}

		// Token: 0x06004081 RID: 16513 RVA: 0x000F099C File Offset: 0x000EEB9C
		internal void TaskConstructorCore(object action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
		{
			this.m_action = action;
			this.m_stateObject = state;
			this.m_taskScheduler = scheduler;
			if ((creationOptions & ~(TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler | TaskCreationOptions.RunContinuationsAsynchronously)) != TaskCreationOptions.None)
			{
				throw new ArgumentOutOfRangeException("creationOptions");
			}
			if ((creationOptions & TaskCreationOptions.LongRunning) != TaskCreationOptions.None && (internalOptions & InternalTaskOptions.SelfReplicating) != InternalTaskOptions.None)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_ctor_LRandSR"));
			}
			int num = (int)(creationOptions | (TaskCreationOptions)internalOptions);
			if (this.m_action == null || (internalOptions & InternalTaskOptions.ContinuationTask) != InternalTaskOptions.None)
			{
				num |= 33554432;
			}
			this.m_stateFlags = num;
			if (this.m_parent != null && (creationOptions & TaskCreationOptions.AttachedToParent) != TaskCreationOptions.None && (this.m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.None)
			{
				this.m_parent.AddNewChild();
			}
			if (cancellationToken.CanBeCanceled)
			{
				this.AssignCancellationToken(cancellationToken, null, null);
			}
		}

		// Token: 0x06004082 RID: 16514 RVA: 0x000F0A54 File Offset: 0x000EEC54
		private void AssignCancellationToken(CancellationToken cancellationToken, Task antecedent, TaskContinuation continuation)
		{
			Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(false);
			contingentProperties.m_cancellationToken = cancellationToken;
			try
			{
				if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
				{
					cancellationToken.ThrowIfSourceDisposed();
				}
				if ((this.Options & (TaskCreationOptions)13312) == TaskCreationOptions.None)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						this.InternalCancel(false);
					}
					else
					{
						CancellationTokenRegistration value;
						if (antecedent == null)
						{
							value = cancellationToken.InternalRegisterWithoutEC(Task.s_taskCancelCallback, this);
						}
						else
						{
							value = cancellationToken.InternalRegisterWithoutEC(Task.s_taskCancelCallback, new Tuple<Task, Task, TaskContinuation>(this, antecedent, continuation));
						}
						contingentProperties.m_cancellationRegistration = new Shared<CancellationTokenRegistration>(value);
					}
				}
			}
			catch
			{
				if (this.m_parent != null && (this.Options & TaskCreationOptions.AttachedToParent) != TaskCreationOptions.None && (this.m_parent.Options & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.None)
				{
					this.m_parent.DisregardChild();
				}
				throw;
			}
		}

		// Token: 0x06004083 RID: 16515 RVA: 0x000F0B14 File Offset: 0x000EED14
		private static void TaskCancelCallback(object o)
		{
			Task task = o as Task;
			if (task == null)
			{
				Tuple<Task, Task, TaskContinuation> tuple = o as Tuple<Task, Task, TaskContinuation>;
				if (tuple != null)
				{
					task = tuple.Item1;
					Task item = tuple.Item2;
					TaskContinuation item2 = tuple.Item3;
					item.RemoveContinuation(item2);
				}
			}
			task.InternalCancel(false);
		}

		// Token: 0x17000994 RID: 2452
		// (get) Token: 0x06004084 RID: 16516 RVA: 0x000F0B5C File Offset: 0x000EED5C
		private string DebuggerDisplayMethodDescription
		{
			get
			{
				Delegate @delegate = (Delegate)this.m_action;
				if (@delegate == null)
				{
					return "{null}";
				}
				return @delegate.Method.ToString();
			}
		}

		// Token: 0x06004085 RID: 16517 RVA: 0x000F0B89 File Offset: 0x000EED89
		[SecuritySafeCritical]
		internal void PossiblyCaptureContext(ref StackCrawlMark stackMark)
		{
			this.CapturedContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
		}

		// Token: 0x17000995 RID: 2453
		// (get) Token: 0x06004086 RID: 16518 RVA: 0x000F0B98 File Offset: 0x000EED98
		internal TaskCreationOptions Options
		{
			get
			{
				int stateFlags = this.m_stateFlags;
				return Task.OptionsMethod(stateFlags);
			}
		}

		// Token: 0x06004087 RID: 16519 RVA: 0x000F0BB4 File Offset: 0x000EEDB4
		internal static TaskCreationOptions OptionsMethod(int flags)
		{
			return (TaskCreationOptions)(flags & 65535);
		}

		// Token: 0x06004088 RID: 16520 RVA: 0x000F0BC0 File Offset: 0x000EEDC0
		internal bool AtomicStateUpdate(int newBits, int illegalBits)
		{
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				int stateFlags = this.m_stateFlags;
				if ((stateFlags & illegalBits) != 0)
				{
					break;
				}
				if (Interlocked.CompareExchange(ref this.m_stateFlags, stateFlags | newBits, stateFlags) == stateFlags)
				{
					return true;
				}
				spinWait.SpinOnce();
			}
			return false;
		}

		// Token: 0x06004089 RID: 16521 RVA: 0x000F0C04 File Offset: 0x000EEE04
		internal bool AtomicStateUpdate(int newBits, int illegalBits, ref int oldFlags)
		{
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				oldFlags = this.m_stateFlags;
				if ((oldFlags & illegalBits) != 0)
				{
					break;
				}
				if (Interlocked.CompareExchange(ref this.m_stateFlags, oldFlags | newBits, oldFlags) == oldFlags)
				{
					return true;
				}
				spinWait.SpinOnce();
			}
			return false;
		}

		// Token: 0x0600408A RID: 16522 RVA: 0x000F0C4C File Offset: 0x000EEE4C
		internal void SetNotificationForWaitCompletion(bool enabled)
		{
			if (enabled)
			{
				bool flag = this.AtomicStateUpdate(268435456, 90177536);
				return;
			}
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				int stateFlags = this.m_stateFlags;
				int value = stateFlags & -268435457;
				if (Interlocked.CompareExchange(ref this.m_stateFlags, value, stateFlags) == stateFlags)
				{
					break;
				}
				spinWait.SpinOnce();
			}
		}

		// Token: 0x0600408B RID: 16523 RVA: 0x000F0CA0 File Offset: 0x000EEEA0
		internal bool NotifyDebuggerOfWaitCompletionIfNecessary()
		{
			if (this.IsWaitNotificationEnabled && this.ShouldNotifyDebuggerOfWaitCompletion)
			{
				this.NotifyDebuggerOfWaitCompletion();
				return true;
			}
			return false;
		}

		// Token: 0x0600408C RID: 16524 RVA: 0x000F0CBC File Offset: 0x000EEEBC
		internal static bool AnyTaskRequiresNotifyDebuggerOfWaitCompletion(Task[] tasks)
		{
			foreach (Task task in tasks)
			{
				if (task != null && task.IsWaitNotificationEnabled && task.ShouldNotifyDebuggerOfWaitCompletion)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000996 RID: 2454
		// (get) Token: 0x0600408D RID: 16525 RVA: 0x000F0CF3 File Offset: 0x000EEEF3
		internal bool IsWaitNotificationEnabledOrNotRanToCompletion
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.m_stateFlags & 285212672) != 16777216;
			}
		}

		// Token: 0x17000997 RID: 2455
		// (get) Token: 0x0600408E RID: 16526 RVA: 0x000F0D10 File Offset: 0x000EEF10
		internal virtual bool ShouldNotifyDebuggerOfWaitCompletion
		{
			get
			{
				return this.IsWaitNotificationEnabled;
			}
		}

		// Token: 0x17000998 RID: 2456
		// (get) Token: 0x0600408F RID: 16527 RVA: 0x000F0D25 File Offset: 0x000EEF25
		internal bool IsWaitNotificationEnabled
		{
			get
			{
				return (this.m_stateFlags & 268435456) != 0;
			}
		}

		// Token: 0x06004090 RID: 16528 RVA: 0x000F0D38 File Offset: 0x000EEF38
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private void NotifyDebuggerOfWaitCompletion()
		{
			this.SetNotificationForWaitCompletion(false);
		}

		// Token: 0x06004091 RID: 16529 RVA: 0x000F0D41 File Offset: 0x000EEF41
		internal bool MarkStarted()
		{
			return this.AtomicStateUpdate(65536, 4259840);
		}

		// Token: 0x06004092 RID: 16530 RVA: 0x000F0D54 File Offset: 0x000EEF54
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool FireTaskScheduledIfNeeded(TaskScheduler ts)
		{
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.IsEnabled() && (this.m_stateFlags & 1073741824) == 0)
			{
				this.m_stateFlags |= 1073741824;
				Task internalCurrent = Task.InternalCurrent;
				Task parent = this.m_parent;
				log.TaskScheduled(ts.Id, (internalCurrent == null) ? 0 : internalCurrent.Id, this.Id, (parent == null) ? 0 : parent.Id, (int)this.Options, Thread.GetDomainID());
				return true;
			}
			return false;
		}

		// Token: 0x06004093 RID: 16531 RVA: 0x000F0DDC File Offset: 0x000EEFDC
		internal void AddNewChild()
		{
			Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(true);
			if (contingentProperties.m_completionCountdown == 1 && !this.IsSelfReplicatingRoot)
			{
				contingentProperties.m_completionCountdown++;
				return;
			}
			Interlocked.Increment(ref contingentProperties.m_completionCountdown);
		}

		// Token: 0x06004094 RID: 16532 RVA: 0x000F0E24 File Offset: 0x000EF024
		internal void DisregardChild()
		{
			Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(true);
			Interlocked.Decrement(ref contingentProperties.m_completionCountdown);
		}

		// Token: 0x06004095 RID: 16533 RVA: 0x000F0E45 File Offset: 0x000EF045
		[__DynamicallyInvokable]
		public void Start()
		{
			this.Start(TaskScheduler.Current);
		}

		// Token: 0x06004096 RID: 16534 RVA: 0x000F0E54 File Offset: 0x000EF054
		[__DynamicallyInvokable]
		public void Start(TaskScheduler scheduler)
		{
			int stateFlags = this.m_stateFlags;
			if (Task.IsCompletedMethod(stateFlags))
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_Start_TaskCompleted"));
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			TaskCreationOptions taskCreationOptions = Task.OptionsMethod(stateFlags);
			if ((taskCreationOptions & (TaskCreationOptions)1024) != TaskCreationOptions.None)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_Start_Promise"));
			}
			if ((taskCreationOptions & (TaskCreationOptions)512) != TaskCreationOptions.None)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_Start_ContinuationTask"));
			}
			if (Interlocked.CompareExchange<TaskScheduler>(ref this.m_taskScheduler, scheduler, null) != null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_Start_AlreadyStarted"));
			}
			this.ScheduleAndStart(true);
		}

		// Token: 0x06004097 RID: 16535 RVA: 0x000F0EEF File Offset: 0x000EF0EF
		[__DynamicallyInvokable]
		public void RunSynchronously()
		{
			this.InternalRunSynchronously(TaskScheduler.Current, true);
		}

		// Token: 0x06004098 RID: 16536 RVA: 0x000F0EFD File Offset: 0x000EF0FD
		[__DynamicallyInvokable]
		public void RunSynchronously(TaskScheduler scheduler)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			this.InternalRunSynchronously(scheduler, true);
		}

		// Token: 0x06004099 RID: 16537 RVA: 0x000F0F18 File Offset: 0x000EF118
		[SecuritySafeCritical]
		internal void InternalRunSynchronously(TaskScheduler scheduler, bool waitForCompletion)
		{
			int stateFlags = this.m_stateFlags;
			TaskCreationOptions taskCreationOptions = Task.OptionsMethod(stateFlags);
			if ((taskCreationOptions & (TaskCreationOptions)512) != TaskCreationOptions.None)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_Continuation"));
			}
			if ((taskCreationOptions & (TaskCreationOptions)1024) != TaskCreationOptions.None)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_Promise"));
			}
			if (Task.IsCompletedMethod(stateFlags))
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_TaskCompleted"));
			}
			if (Interlocked.CompareExchange<TaskScheduler>(ref this.m_taskScheduler, scheduler, null) != null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_AlreadyStarted"));
			}
			if (this.MarkStarted())
			{
				bool flag = false;
				try
				{
					if (!scheduler.TryRunInline(this, false))
					{
						scheduler.InternalQueueTask(this);
						flag = true;
					}
					if (waitForCompletion && !this.IsCompleted)
					{
						this.SpinThenBlockingWait(-1, default(CancellationToken));
					}
					return;
				}
				catch (Exception ex)
				{
					if (!flag && !(ex is ThreadAbortException))
					{
						TaskSchedulerException ex2 = new TaskSchedulerException(ex);
						this.AddException(ex2);
						this.Finish(false);
						this.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
						throw ex2;
					}
					throw;
				}
			}
			throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_TaskCompleted"));
		}

		// Token: 0x0600409A RID: 16538 RVA: 0x000F103C File Offset: 0x000EF23C
		internal static Task InternalStartNew(Task creatingTask, Delegate action, object state, CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark)
		{
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			Task task = new Task(action, state, creatingTask, cancellationToken, options, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler);
			task.PossiblyCaptureContext(ref stackMark);
			task.ScheduleAndStart(false);
			return task;
		}

		// Token: 0x0600409B RID: 16539 RVA: 0x000F1080 File Offset: 0x000EF280
		internal static int NewId()
		{
			int num;
			do
			{
				num = Interlocked.Increment(ref Task.s_taskIdCounter);
			}
			while (num == 0);
			TplEtwProvider.Log.NewID(num);
			return num;
		}

		// Token: 0x17000999 RID: 2457
		// (get) Token: 0x0600409C RID: 16540 RVA: 0x000F10AC File Offset: 0x000EF2AC
		[__DynamicallyInvokable]
		public int Id
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_taskId == 0)
				{
					int value = Task.NewId();
					Interlocked.CompareExchange(ref this.m_taskId, value, 0);
				}
				return this.m_taskId;
			}
		}

		// Token: 0x1700099A RID: 2458
		// (get) Token: 0x0600409D RID: 16541 RVA: 0x000F10E0 File Offset: 0x000EF2E0
		[__DynamicallyInvokable]
		public static int? CurrentId
		{
			[__DynamicallyInvokable]
			get
			{
				Task internalCurrent = Task.InternalCurrent;
				if (internalCurrent != null)
				{
					return new int?(internalCurrent.Id);
				}
				return null;
			}
		}

		// Token: 0x1700099B RID: 2459
		// (get) Token: 0x0600409E RID: 16542 RVA: 0x000F110B File Offset: 0x000EF30B
		internal static Task InternalCurrent
		{
			get
			{
				return Task.t_currentTask;
			}
		}

		// Token: 0x0600409F RID: 16543 RVA: 0x000F1112 File Offset: 0x000EF312
		internal static Task InternalCurrentIfAttached(TaskCreationOptions creationOptions)
		{
			if ((creationOptions & TaskCreationOptions.AttachedToParent) == TaskCreationOptions.None)
			{
				return null;
			}
			return Task.InternalCurrent;
		}

		// Token: 0x1700099C RID: 2460
		// (get) Token: 0x060040A0 RID: 16544 RVA: 0x000F1120 File Offset: 0x000EF320
		internal static StackGuard CurrentStackGuard
		{
			get
			{
				StackGuard stackGuard = Task.t_stackGuard;
				if (stackGuard == null)
				{
					stackGuard = (Task.t_stackGuard = new StackGuard());
				}
				return stackGuard;
			}
		}

		// Token: 0x1700099D RID: 2461
		// (get) Token: 0x060040A1 RID: 16545 RVA: 0x000F1144 File Offset: 0x000EF344
		[__DynamicallyInvokable]
		public AggregateException Exception
		{
			[__DynamicallyInvokable]
			get
			{
				AggregateException result = null;
				if (this.IsFaulted)
				{
					result = this.GetExceptions(false);
				}
				return result;
			}
		}

		// Token: 0x1700099E RID: 2462
		// (get) Token: 0x060040A2 RID: 16546 RVA: 0x000F1164 File Offset: 0x000EF364
		[__DynamicallyInvokable]
		public TaskStatus Status
		{
			[__DynamicallyInvokable]
			get
			{
				int stateFlags = this.m_stateFlags;
				TaskStatus result;
				if ((stateFlags & 2097152) != 0)
				{
					result = TaskStatus.Faulted;
				}
				else if ((stateFlags & 4194304) != 0)
				{
					result = TaskStatus.Canceled;
				}
				else if ((stateFlags & 16777216) != 0)
				{
					result = TaskStatus.RanToCompletion;
				}
				else if ((stateFlags & 8388608) != 0)
				{
					result = TaskStatus.WaitingForChildrenToComplete;
				}
				else if ((stateFlags & 131072) != 0)
				{
					result = TaskStatus.Running;
				}
				else if ((stateFlags & 65536) != 0)
				{
					result = TaskStatus.WaitingToRun;
				}
				else if ((stateFlags & 33554432) != 0)
				{
					result = TaskStatus.WaitingForActivation;
				}
				else
				{
					result = TaskStatus.Created;
				}
				return result;
			}
		}

		// Token: 0x1700099F RID: 2463
		// (get) Token: 0x060040A3 RID: 16547 RVA: 0x000F11D8 File Offset: 0x000EF3D8
		[__DynamicallyInvokable]
		public bool IsCanceled
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.m_stateFlags & 6291456) == 4194304;
			}
		}

		// Token: 0x170009A0 RID: 2464
		// (get) Token: 0x060040A4 RID: 16548 RVA: 0x000F11F0 File Offset: 0x000EF3F0
		internal bool IsCancellationRequested
		{
			get
			{
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				return contingentProperties != null && (contingentProperties.m_internalCancellationRequested == 1 || contingentProperties.m_cancellationToken.IsCancellationRequested);
			}
		}

		// Token: 0x060040A5 RID: 16549 RVA: 0x000F1224 File Offset: 0x000EF424
		internal Task.ContingentProperties EnsureContingentPropertiesInitialized(bool needsProtection)
		{
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (contingentProperties == null)
			{
				return this.EnsureContingentPropertiesInitializedCore(needsProtection);
			}
			return contingentProperties;
		}

		// Token: 0x060040A6 RID: 16550 RVA: 0x000F1248 File Offset: 0x000EF448
		private Task.ContingentProperties EnsureContingentPropertiesInitializedCore(bool needsProtection)
		{
			if (needsProtection)
			{
				return LazyInitializer.EnsureInitialized<Task.ContingentProperties>(ref this.m_contingentProperties, Task.s_createContingentProperties);
			}
			return this.m_contingentProperties = new Task.ContingentProperties();
		}

		// Token: 0x170009A1 RID: 2465
		// (get) Token: 0x060040A7 RID: 16551 RVA: 0x000F127C File Offset: 0x000EF47C
		internal CancellationToken CancellationToken
		{
			get
			{
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				if (contingentProperties != null)
				{
					return contingentProperties.m_cancellationToken;
				}
				return default(CancellationToken);
			}
		}

		// Token: 0x170009A2 RID: 2466
		// (get) Token: 0x060040A8 RID: 16552 RVA: 0x000F12A5 File Offset: 0x000EF4A5
		internal bool IsCancellationAcknowledged
		{
			get
			{
				return (this.m_stateFlags & 1048576) != 0;
			}
		}

		// Token: 0x170009A3 RID: 2467
		// (get) Token: 0x060040A9 RID: 16553 RVA: 0x000F12B8 File Offset: 0x000EF4B8
		[__DynamicallyInvokable]
		public bool IsCompleted
		{
			[__DynamicallyInvokable]
			get
			{
				int stateFlags = this.m_stateFlags;
				return Task.IsCompletedMethod(stateFlags);
			}
		}

		// Token: 0x060040AA RID: 16554 RVA: 0x000F12D4 File Offset: 0x000EF4D4
		private static bool IsCompletedMethod(int flags)
		{
			return (flags & 23068672) != 0;
		}

		// Token: 0x170009A4 RID: 2468
		// (get) Token: 0x060040AB RID: 16555 RVA: 0x000F12E0 File Offset: 0x000EF4E0
		internal bool IsRanToCompletion
		{
			get
			{
				return (this.m_stateFlags & 23068672) == 16777216;
			}
		}

		// Token: 0x170009A5 RID: 2469
		// (get) Token: 0x060040AC RID: 16556 RVA: 0x000F12F7 File Offset: 0x000EF4F7
		[__DynamicallyInvokable]
		public TaskCreationOptions CreationOptions
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Options & (TaskCreationOptions)(-65281);
			}
		}

		// Token: 0x170009A6 RID: 2470
		// (get) Token: 0x060040AD RID: 16557 RVA: 0x000F1308 File Offset: 0x000EF508
		[__DynamicallyInvokable]
		WaitHandle IAsyncResult.AsyncWaitHandle
		{
			[__DynamicallyInvokable]
			get
			{
				bool flag = (this.m_stateFlags & 262144) != 0;
				if (flag)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("Task_ThrowIfDisposed"));
				}
				return this.CompletedEvent.WaitHandle;
			}
		}

		// Token: 0x170009A7 RID: 2471
		// (get) Token: 0x060040AE RID: 16558 RVA: 0x000F1346 File Offset: 0x000EF546
		[__DynamicallyInvokable]
		public object AsyncState
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_stateObject;
			}
		}

		// Token: 0x170009A8 RID: 2472
		// (get) Token: 0x060040AF RID: 16559 RVA: 0x000F134E File Offset: 0x000EF54E
		[__DynamicallyInvokable]
		bool IAsyncResult.CompletedSynchronously
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x170009A9 RID: 2473
		// (get) Token: 0x060040B0 RID: 16560 RVA: 0x000F1351 File Offset: 0x000EF551
		internal TaskScheduler ExecutingTaskScheduler
		{
			get
			{
				return this.m_taskScheduler;
			}
		}

		// Token: 0x170009AA RID: 2474
		// (get) Token: 0x060040B1 RID: 16561 RVA: 0x000F1359 File Offset: 0x000EF559
		[__DynamicallyInvokable]
		public static TaskFactory Factory
		{
			[__DynamicallyInvokable]
			get
			{
				return Task.s_factory;
			}
		}

		// Token: 0x170009AB RID: 2475
		// (get) Token: 0x060040B2 RID: 16562 RVA: 0x000F1360 File Offset: 0x000EF560
		[__DynamicallyInvokable]
		public static Task CompletedTask
		{
			[__DynamicallyInvokable]
			get
			{
				Task task = Task.s_completedTask;
				if (task == null)
				{
					task = (Task.s_completedTask = new Task(false, (TaskCreationOptions)16384, default(CancellationToken)));
				}
				return task;
			}
		}

		// Token: 0x170009AC RID: 2476
		// (get) Token: 0x060040B3 RID: 16563 RVA: 0x000F1394 File Offset: 0x000EF594
		internal ManualResetEventSlim CompletedEvent
		{
			get
			{
				Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(true);
				if (contingentProperties.m_completionEvent == null)
				{
					bool isCompleted = this.IsCompleted;
					ManualResetEventSlim manualResetEventSlim = new ManualResetEventSlim(isCompleted);
					if (Interlocked.CompareExchange<ManualResetEventSlim>(ref contingentProperties.m_completionEvent, manualResetEventSlim, null) != null)
					{
						manualResetEventSlim.Dispose();
					}
					else if (!isCompleted && this.IsCompleted)
					{
						manualResetEventSlim.Set();
					}
				}
				return contingentProperties.m_completionEvent;
			}
		}

		// Token: 0x170009AD RID: 2477
		// (get) Token: 0x060040B4 RID: 16564 RVA: 0x000F13F1 File Offset: 0x000EF5F1
		internal bool IsSelfReplicatingRoot
		{
			get
			{
				return (this.Options & (TaskCreationOptions)2304) == (TaskCreationOptions)2048;
			}
		}

		// Token: 0x170009AE RID: 2478
		// (get) Token: 0x060040B5 RID: 16565 RVA: 0x000F1406 File Offset: 0x000EF606
		internal bool IsChildReplica
		{
			get
			{
				return (this.Options & (TaskCreationOptions)256) > TaskCreationOptions.None;
			}
		}

		// Token: 0x170009AF RID: 2479
		// (get) Token: 0x060040B6 RID: 16566 RVA: 0x000F1418 File Offset: 0x000EF618
		internal int ActiveChildCount
		{
			get
			{
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				if (contingentProperties == null)
				{
					return 0;
				}
				return contingentProperties.m_completionCountdown - 1;
			}
		}

		// Token: 0x170009B0 RID: 2480
		// (get) Token: 0x060040B7 RID: 16567 RVA: 0x000F1440 File Offset: 0x000EF640
		internal bool ExceptionRecorded
		{
			get
			{
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				return contingentProperties != null && contingentProperties.m_exceptionsHolder != null && contingentProperties.m_exceptionsHolder.ContainsFaultList;
			}
		}

		// Token: 0x170009B1 RID: 2481
		// (get) Token: 0x060040B8 RID: 16568 RVA: 0x000F1472 File Offset: 0x000EF672
		[__DynamicallyInvokable]
		public bool IsFaulted
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.m_stateFlags & 2097152) != 0;
			}
		}

		// Token: 0x170009B2 RID: 2482
		// (get) Token: 0x060040B9 RID: 16569 RVA: 0x000F1488 File Offset: 0x000EF688
		// (set) Token: 0x060040BA RID: 16570 RVA: 0x000F14CC File Offset: 0x000EF6CC
		internal ExecutionContext CapturedContext
		{
			get
			{
				if ((this.m_stateFlags & 536870912) == 536870912)
				{
					return null;
				}
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				if (contingentProperties != null && contingentProperties.m_capturedContext != null)
				{
					return contingentProperties.m_capturedContext;
				}
				return ExecutionContext.PreAllocatedDefault;
			}
			set
			{
				if (value == null)
				{
					this.m_stateFlags |= 536870912;
					return;
				}
				if (!value.IsPreAllocatedDefault)
				{
					this.EnsureContingentPropertiesInitialized(false).m_capturedContext = value;
				}
			}
		}

		// Token: 0x060040BB RID: 16571 RVA: 0x000F14FD File Offset: 0x000EF6FD
		private static ExecutionContext CopyExecutionContext(ExecutionContext capturedContext)
		{
			if (capturedContext == null)
			{
				return null;
			}
			if (capturedContext.IsPreAllocatedDefault)
			{
				return ExecutionContext.PreAllocatedDefault;
			}
			return capturedContext.CreateCopy();
		}

		// Token: 0x060040BC RID: 16572 RVA: 0x000F1518 File Offset: 0x000EF718
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060040BD RID: 16573 RVA: 0x000F1528 File Offset: 0x000EF728
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if ((this.Options & (TaskCreationOptions)16384) != TaskCreationOptions.None)
				{
					return;
				}
				if (!this.IsCompleted)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Task_Dispose_NotCompleted"));
				}
				Task.ContingentProperties contingentProperties = this.m_contingentProperties;
				if (contingentProperties != null)
				{
					ManualResetEventSlim completionEvent = contingentProperties.m_completionEvent;
					if (completionEvent != null)
					{
						contingentProperties.m_completionEvent = null;
						if (!completionEvent.IsSet)
						{
							completionEvent.Set();
						}
						completionEvent.Dispose();
					}
				}
			}
			this.m_stateFlags |= 262144;
		}

		// Token: 0x060040BE RID: 16574 RVA: 0x000F15AC File Offset: 0x000EF7AC
		[SecuritySafeCritical]
		internal void ScheduleAndStart(bool needsProtection)
		{
			if (needsProtection)
			{
				if (!this.MarkStarted())
				{
					return;
				}
			}
			else
			{
				this.m_stateFlags |= 65536;
			}
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.AddToActiveTasks(this);
			}
			if (AsyncCausalityTracer.LoggingOn && (this.Options & (TaskCreationOptions)512) == TaskCreationOptions.None)
			{
				AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task: " + ((Delegate)this.m_action).Method.Name, 0UL);
			}
			try
			{
				this.m_taskScheduler.InternalQueueTask(this);
			}
			catch (ThreadAbortException exceptionObject)
			{
				this.AddException(exceptionObject);
				this.FinishThreadAbortedTask(true, false);
			}
			catch (Exception innerException)
			{
				TaskSchedulerException ex = new TaskSchedulerException(innerException);
				this.AddException(ex);
				this.Finish(false);
				if ((this.Options & (TaskCreationOptions)512) == TaskCreationOptions.None)
				{
					this.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
				}
				throw ex;
			}
		}

		// Token: 0x060040BF RID: 16575 RVA: 0x000F16A4 File Offset: 0x000EF8A4
		internal void AddException(object exceptionObject)
		{
			this.AddException(exceptionObject, false);
		}

		// Token: 0x060040C0 RID: 16576 RVA: 0x000F16B0 File Offset: 0x000EF8B0
		internal void AddException(object exceptionObject, bool representsCancellation)
		{
			Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(true);
			if (contingentProperties.m_exceptionsHolder == null)
			{
				TaskExceptionHolder taskExceptionHolder = new TaskExceptionHolder(this);
				if (Interlocked.CompareExchange<TaskExceptionHolder>(ref contingentProperties.m_exceptionsHolder, taskExceptionHolder, null) != null)
				{
					taskExceptionHolder.MarkAsHandled(false);
				}
			}
			Task.ContingentProperties obj = contingentProperties;
			lock (obj)
			{
				contingentProperties.m_exceptionsHolder.Add(exceptionObject, representsCancellation);
			}
		}

		// Token: 0x060040C1 RID: 16577 RVA: 0x000F1724 File Offset: 0x000EF924
		private AggregateException GetExceptions(bool includeTaskCanceledExceptions)
		{
			Exception ex = null;
			if (includeTaskCanceledExceptions && this.IsCanceled)
			{
				ex = new TaskCanceledException(this);
			}
			if (this.ExceptionRecorded)
			{
				return this.m_contingentProperties.m_exceptionsHolder.CreateExceptionObject(false, ex);
			}
			if (ex != null)
			{
				return new AggregateException(new Exception[] { ex });
			}
			return null;
		}

		// Token: 0x060040C2 RID: 16578 RVA: 0x000F1778 File Offset: 0x000EF978
		internal ReadOnlyCollection<ExceptionDispatchInfo> GetExceptionDispatchInfos()
		{
			if (!this.IsFaulted || !this.ExceptionRecorded)
			{
				return new ReadOnlyCollection<ExceptionDispatchInfo>(new ExceptionDispatchInfo[0]);
			}
			return this.m_contingentProperties.m_exceptionsHolder.GetExceptionDispatchInfos();
		}

		// Token: 0x060040C3 RID: 16579 RVA: 0x000F17BC File Offset: 0x000EF9BC
		internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
		{
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (contingentProperties == null)
			{
				return null;
			}
			TaskExceptionHolder exceptionsHolder = contingentProperties.m_exceptionsHolder;
			if (exceptionsHolder == null)
			{
				return null;
			}
			return exceptionsHolder.GetCancellationExceptionDispatchInfo();
		}

		// Token: 0x060040C4 RID: 16580 RVA: 0x000F17EC File Offset: 0x000EF9EC
		internal void ThrowIfExceptional(bool includeTaskCanceledExceptions)
		{
			Exception exceptions = this.GetExceptions(includeTaskCanceledExceptions);
			if (exceptions != null)
			{
				this.UpdateExceptionObservedStatus();
				throw exceptions;
			}
		}

		// Token: 0x060040C5 RID: 16581 RVA: 0x000F180C File Offset: 0x000EFA0C
		internal void UpdateExceptionObservedStatus()
		{
			if (this.m_parent != null && (this.Options & TaskCreationOptions.AttachedToParent) != TaskCreationOptions.None && (this.m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.None && Task.InternalCurrent == this.m_parent)
			{
				this.m_stateFlags |= 524288;
			}
		}

		// Token: 0x170009B3 RID: 2483
		// (get) Token: 0x060040C6 RID: 16582 RVA: 0x000F185D File Offset: 0x000EFA5D
		internal bool IsExceptionObservedByParent
		{
			get
			{
				return (this.m_stateFlags & 524288) != 0;
			}
		}

		// Token: 0x170009B4 RID: 2484
		// (get) Token: 0x060040C7 RID: 16583 RVA: 0x000F1870 File Offset: 0x000EFA70
		internal bool IsDelegateInvoked
		{
			get
			{
				return (this.m_stateFlags & 131072) != 0;
			}
		}

		// Token: 0x060040C8 RID: 16584 RVA: 0x000F1884 File Offset: 0x000EFA84
		internal void Finish(bool bUserDelegateExecuted)
		{
			if (!bUserDelegateExecuted)
			{
				this.FinishStageTwo();
				return;
			}
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (contingentProperties == null || (contingentProperties.m_completionCountdown == 1 && !this.IsSelfReplicatingRoot) || Interlocked.Decrement(ref contingentProperties.m_completionCountdown) == 0)
			{
				this.FinishStageTwo();
			}
			else
			{
				this.AtomicStateUpdate(8388608, 23068672);
			}
			List<Task> list = ((contingentProperties != null) ? contingentProperties.m_exceptionalChildren : null);
			if (list != null)
			{
				List<Task> obj = list;
				lock (obj)
				{
					list.RemoveAll(Task.s_IsExceptionObservedByParentPredicate);
				}
			}
		}

		// Token: 0x060040C9 RID: 16585 RVA: 0x000F1928 File Offset: 0x000EFB28
		internal void FinishStageTwo()
		{
			this.AddExceptionsFromChildren();
			int num;
			if (this.ExceptionRecorded)
			{
				num = 2097152;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Error);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.RemoveFromActiveTasks(this.Id);
				}
			}
			else if (this.IsCancellationRequested && this.IsCancellationAcknowledged)
			{
				num = 4194304;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Canceled);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.RemoveFromActiveTasks(this.Id);
				}
			}
			else
			{
				num = 16777216;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.RemoveFromActiveTasks(this.Id);
				}
			}
			Interlocked.Exchange(ref this.m_stateFlags, this.m_stateFlags | num);
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (contingentProperties != null)
			{
				contingentProperties.SetCompleted();
				contingentProperties.DeregisterCancellationCallback();
			}
			this.FinishStageThree();
		}

		// Token: 0x060040CA RID: 16586 RVA: 0x000F1A10 File Offset: 0x000EFC10
		internal void FinishStageThree()
		{
			this.m_action = null;
			if (this.m_parent != null && (this.m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == TaskCreationOptions.None && (this.m_stateFlags & 65535 & 4) != 0)
			{
				this.m_parent.ProcessChildCompletion(this);
			}
			this.FinishContinuations();
		}

		// Token: 0x060040CB RID: 16587 RVA: 0x000F1A60 File Offset: 0x000EFC60
		internal void ProcessChildCompletion(Task childTask)
		{
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (childTask.IsFaulted && !childTask.IsExceptionObservedByParent)
			{
				if (contingentProperties.m_exceptionalChildren == null)
				{
					Interlocked.CompareExchange<List<Task>>(ref contingentProperties.m_exceptionalChildren, new List<Task>(), null);
				}
				List<Task> exceptionalChildren = contingentProperties.m_exceptionalChildren;
				if (exceptionalChildren != null)
				{
					List<Task> obj = exceptionalChildren;
					lock (obj)
					{
						exceptionalChildren.Add(childTask);
					}
				}
			}
			if (Interlocked.Decrement(ref contingentProperties.m_completionCountdown) == 0)
			{
				this.FinishStageTwo();
			}
		}

		// Token: 0x060040CC RID: 16588 RVA: 0x000F1AF0 File Offset: 0x000EFCF0
		internal void AddExceptionsFromChildren()
		{
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			List<Task> list = ((contingentProperties != null) ? contingentProperties.m_exceptionalChildren : null);
			if (list != null)
			{
				List<Task> obj = list;
				lock (obj)
				{
					foreach (Task task in list)
					{
						if (task.IsFaulted && !task.IsExceptionObservedByParent)
						{
							TaskExceptionHolder exceptionsHolder = task.m_contingentProperties.m_exceptionsHolder;
							this.AddException(exceptionsHolder.CreateExceptionObject(false, null));
						}
					}
				}
				contingentProperties.m_exceptionalChildren = null;
			}
		}

		// Token: 0x060040CD RID: 16589 RVA: 0x000F1BB4 File Offset: 0x000EFDB4
		internal void FinishThreadAbortedTask(bool bTAEAddedToExceptionHolder, bool delegateRan)
		{
			if (bTAEAddedToExceptionHolder)
			{
				this.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
			}
			if (!this.AtomicStateUpdate(134217728, 157286400))
			{
				return;
			}
			this.Finish(delegateRan);
		}

		// Token: 0x060040CE RID: 16590 RVA: 0x000F1BE8 File Offset: 0x000EFDE8
		private void Execute()
		{
			if (this.IsSelfReplicatingRoot)
			{
				Task.ExecuteSelfReplicating(this);
				return;
			}
			try
			{
				this.InnerInvoke();
			}
			catch (ThreadAbortException unhandledException)
			{
				if (!this.IsChildReplica)
				{
					this.HandleException(unhandledException);
					this.FinishThreadAbortedTask(true, true);
				}
			}
			catch (Exception unhandledException2)
			{
				this.HandleException(unhandledException2);
			}
		}

		// Token: 0x060040CF RID: 16591 RVA: 0x000F1C50 File Offset: 0x000EFE50
		internal virtual bool ShouldReplicate()
		{
			return true;
		}

		// Token: 0x060040D0 RID: 16592 RVA: 0x000F1C54 File Offset: 0x000EFE54
		internal virtual Task CreateReplicaTask(Action<object> taskReplicaDelegate, object stateObject, Task parentTask, TaskScheduler taskScheduler, TaskCreationOptions creationOptionsForReplica, InternalTaskOptions internalOptionsForReplica)
		{
			return new Task(taskReplicaDelegate, stateObject, parentTask, default(CancellationToken), creationOptionsForReplica, internalOptionsForReplica, parentTask.ExecutingTaskScheduler);
		}

		// Token: 0x170009B5 RID: 2485
		// (get) Token: 0x060040D1 RID: 16593 RVA: 0x000F1C7C File Offset: 0x000EFE7C
		// (set) Token: 0x060040D2 RID: 16594 RVA: 0x000F1C7F File Offset: 0x000EFE7F
		internal virtual object SavedStateForNextReplica
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x170009B6 RID: 2486
		// (get) Token: 0x060040D3 RID: 16595 RVA: 0x000F1C81 File Offset: 0x000EFE81
		// (set) Token: 0x060040D4 RID: 16596 RVA: 0x000F1C84 File Offset: 0x000EFE84
		internal virtual object SavedStateFromPreviousReplica
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x170009B7 RID: 2487
		// (get) Token: 0x060040D5 RID: 16597 RVA: 0x000F1C86 File Offset: 0x000EFE86
		// (set) Token: 0x060040D6 RID: 16598 RVA: 0x000F1C89 File Offset: 0x000EFE89
		internal virtual Task HandedOverChildReplica
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		// Token: 0x060040D7 RID: 16599 RVA: 0x000F1C8C File Offset: 0x000EFE8C
		private static void ExecuteSelfReplicating(Task root)
		{
			TaskCreationOptions creationOptionsForReplicas = root.CreationOptions | TaskCreationOptions.AttachedToParent;
			InternalTaskOptions internalOptionsForReplicas = InternalTaskOptions.ChildReplica | InternalTaskOptions.SelfReplicating | InternalTaskOptions.QueuedByRuntime;
			bool replicasAreQuitting = false;
			Action<object> taskReplicaDelegate = null;
			taskReplicaDelegate = delegate(object <p0>)
			{
				Task internalCurrent = Task.InternalCurrent;
				Task task = internalCurrent.HandedOverChildReplica;
				if (task == null)
				{
					if (!root.ShouldReplicate())
					{
						return;
					}
					if (Volatile.Read(ref replicasAreQuitting))
					{
						return;
					}
					ExecutionContext capturedContext = root.CapturedContext;
					task = root.CreateReplicaTask(taskReplicaDelegate, root.m_stateObject, root, root.ExecutingTaskScheduler, creationOptionsForReplicas, internalOptionsForReplicas);
					task.CapturedContext = Task.CopyExecutionContext(capturedContext);
					task.ScheduleAndStart(false);
				}
				try
				{
					root.InnerInvokeWithArg(internalCurrent);
				}
				catch (Exception ex)
				{
					root.HandleException(ex);
					if (ex is ThreadAbortException)
					{
						internalCurrent.FinishThreadAbortedTask(false, true);
					}
				}
				object savedStateForNextReplica = internalCurrent.SavedStateForNextReplica;
				if (savedStateForNextReplica != null)
				{
					Task task2 = root.CreateReplicaTask(taskReplicaDelegate, root.m_stateObject, root, root.ExecutingTaskScheduler, creationOptionsForReplicas, internalOptionsForReplicas);
					ExecutionContext capturedContext2 = root.CapturedContext;
					task2.CapturedContext = Task.CopyExecutionContext(capturedContext2);
					task2.HandedOverChildReplica = task;
					task2.SavedStateFromPreviousReplica = savedStateForNextReplica;
					task2.ScheduleAndStart(false);
					return;
				}
				replicasAreQuitting = true;
				try
				{
					task.InternalCancel(true);
				}
				catch (Exception unhandledException)
				{
					root.HandleException(unhandledException);
				}
			};
			taskReplicaDelegate(null);
		}

		// Token: 0x060040D8 RID: 16600 RVA: 0x000F1CF0 File Offset: 0x000EFEF0
		[SecurityCritical]
		void IThreadPoolWorkItem.ExecuteWorkItem()
		{
			this.ExecuteEntry(false);
		}

		// Token: 0x060040D9 RID: 16601 RVA: 0x000F1CFA File Offset: 0x000EFEFA
		[SecurityCritical]
		void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
		{
			if (!this.IsCompleted)
			{
				this.HandleException(tae);
				this.FinishThreadAbortedTask(true, false);
			}
		}

		// Token: 0x060040DA RID: 16602 RVA: 0x000F1D14 File Offset: 0x000EFF14
		[SecuritySafeCritical]
		internal bool ExecuteEntry(bool bPreventDoubleExecution)
		{
			if (bPreventDoubleExecution || (this.Options & (TaskCreationOptions)2048) != TaskCreationOptions.None)
			{
				int num = 0;
				if (!this.AtomicStateUpdate(131072, 23199744, ref num) && (num & 4194304) == 0)
				{
					return false;
				}
			}
			else
			{
				this.m_stateFlags |= 131072;
			}
			if (!this.IsCancellationRequested && !this.IsCanceled)
			{
				this.ExecuteWithThreadLocal(ref Task.t_currentTask);
			}
			else if (!this.IsCanceled)
			{
				int num2 = Interlocked.Exchange(ref this.m_stateFlags, this.m_stateFlags | 4194304);
				if ((num2 & 4194304) == 0)
				{
					this.CancellationCleanupLogic();
				}
			}
			return true;
		}

		// Token: 0x060040DB RID: 16603 RVA: 0x000F1DB8 File Offset: 0x000EFFB8
		[SecurityCritical]
		private void ExecuteWithThreadLocal(ref Task currentTaskSlot)
		{
			Task task = currentTaskSlot;
			TplEtwProvider log = TplEtwProvider.Log;
			Guid currentThreadActivityId = default(Guid);
			bool flag = log.IsEnabled();
			if (flag)
			{
				if (log.TasksSetActivityIds)
				{
					EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(this.Id), out currentThreadActivityId);
				}
				if (task != null)
				{
					log.TaskStarted(task.m_taskScheduler.Id, task.Id, this.Id);
				}
				else
				{
					log.TaskStarted(TaskScheduler.Current.Id, 0, this.Id);
				}
			}
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, this.Id, CausalitySynchronousWork.Execution);
			}
			try
			{
				currentTaskSlot = this;
				ExecutionContext capturedContext = this.CapturedContext;
				if (capturedContext == null)
				{
					this.Execute();
				}
				else
				{
					if (this.IsSelfReplicatingRoot || this.IsChildReplica)
					{
						this.CapturedContext = Task.CopyExecutionContext(capturedContext);
					}
					ContextCallback contextCallback = Task.s_ecCallback;
					if (contextCallback == null)
					{
						contextCallback = (Task.s_ecCallback = new ContextCallback(Task.ExecutionContextCallback));
					}
					ExecutionContext.Run(capturedContext, contextCallback, this, true);
				}
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.Execution);
				}
				this.Finish(true);
			}
			finally
			{
				currentTaskSlot = task;
				if (flag)
				{
					if (task != null)
					{
						log.TaskCompleted(task.m_taskScheduler.Id, task.Id, this.Id, this.IsFaulted);
					}
					else
					{
						log.TaskCompleted(TaskScheduler.Current.Id, 0, this.Id, this.IsFaulted);
					}
					if (log.TasksSetActivityIds)
					{
						EventSource.SetCurrentThreadActivityId(currentThreadActivityId);
					}
				}
			}
		}

		// Token: 0x060040DC RID: 16604 RVA: 0x000F1F28 File Offset: 0x000F0128
		[SecurityCritical]
		private static void ExecutionContextCallback(object obj)
		{
			Task task = obj as Task;
			task.Execute();
		}

		// Token: 0x060040DD RID: 16605 RVA: 0x000F1F44 File Offset: 0x000F0144
		internal virtual void InnerInvoke()
		{
			Action action = this.m_action as Action;
			if (action != null)
			{
				action();
				return;
			}
			Action<object> action2 = this.m_action as Action<object>;
			if (action2 != null)
			{
				action2(this.m_stateObject);
				return;
			}
		}

		// Token: 0x060040DE RID: 16606 RVA: 0x000F1F83 File Offset: 0x000F0183
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		internal void InnerInvokeWithArg(Task childTask)
		{
			this.InnerInvoke();
		}

		// Token: 0x060040DF RID: 16607 RVA: 0x000F1F8C File Offset: 0x000F018C
		private void HandleException(Exception unhandledException)
		{
			OperationCanceledException ex = unhandledException as OperationCanceledException;
			if (ex != null && this.IsCancellationRequested && this.m_contingentProperties.m_cancellationToken == ex.CancellationToken)
			{
				this.SetCancellationAcknowledged();
				this.AddException(ex, true);
				return;
			}
			this.AddException(unhandledException);
		}

		// Token: 0x060040E0 RID: 16608 RVA: 0x000F1FDB File Offset: 0x000F01DB
		[__DynamicallyInvokable]
		public TaskAwaiter GetAwaiter()
		{
			return new TaskAwaiter(this);
		}

		// Token: 0x060040E1 RID: 16609 RVA: 0x000F1FE3 File Offset: 0x000F01E3
		[__DynamicallyInvokable]
		public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
		{
			return new ConfiguredTaskAwaitable(this, continueOnCapturedContext);
		}

		// Token: 0x060040E2 RID: 16610 RVA: 0x000F1FEC File Offset: 0x000F01EC
		[SecurityCritical]
		internal void SetContinuationForAwait(Action continuationAction, bool continueOnCapturedContext, bool flowExecutionContext, ref StackCrawlMark stackMark)
		{
			TaskContinuation taskContinuation = null;
			if (continueOnCapturedContext)
			{
				SynchronizationContext currentNoFlow = SynchronizationContext.CurrentNoFlow;
				if (currentNoFlow != null && currentNoFlow.GetType() != typeof(SynchronizationContext))
				{
					taskContinuation = new SynchronizationContextAwaitTaskContinuation(currentNoFlow, continuationAction, flowExecutionContext, ref stackMark);
				}
				else
				{
					TaskScheduler internalCurrent = TaskScheduler.InternalCurrent;
					if (internalCurrent != null && internalCurrent != TaskScheduler.Default)
					{
						taskContinuation = new TaskSchedulerAwaitTaskContinuation(internalCurrent, continuationAction, flowExecutionContext, ref stackMark);
					}
				}
			}
			if (taskContinuation == null && flowExecutionContext)
			{
				taskContinuation = new AwaitTaskContinuation(continuationAction, true, ref stackMark);
			}
			if (taskContinuation != null)
			{
				if (!this.AddTaskContinuation(taskContinuation, false))
				{
					taskContinuation.Run(this, false);
					return;
				}
			}
			else if (!this.AddTaskContinuation(continuationAction, false))
			{
				AwaitTaskContinuation.UnsafeScheduleAction(continuationAction, this);
			}
		}

		// Token: 0x060040E3 RID: 16611 RVA: 0x000F2080 File Offset: 0x000F0280
		[__DynamicallyInvokable]
		public static YieldAwaitable Yield()
		{
			return default(YieldAwaitable);
		}

		// Token: 0x060040E4 RID: 16612 RVA: 0x000F2098 File Offset: 0x000F0298
		[__DynamicallyInvokable]
		public void Wait()
		{
			this.Wait(-1, default(CancellationToken));
		}

		// Token: 0x060040E5 RID: 16613 RVA: 0x000F20B8 File Offset: 0x000F02B8
		[__DynamicallyInvokable]
		public bool Wait(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
			return this.Wait((int)num, default(CancellationToken));
		}

		// Token: 0x060040E6 RID: 16614 RVA: 0x000F20F8 File Offset: 0x000F02F8
		[__DynamicallyInvokable]
		public void Wait(CancellationToken cancellationToken)
		{
			this.Wait(-1, cancellationToken);
		}

		// Token: 0x060040E7 RID: 16615 RVA: 0x000F2104 File Offset: 0x000F0304
		[__DynamicallyInvokable]
		public bool Wait(int millisecondsTimeout)
		{
			return this.Wait(millisecondsTimeout, default(CancellationToken));
		}

		// Token: 0x060040E8 RID: 16616 RVA: 0x000F2124 File Offset: 0x000F0324
		[__DynamicallyInvokable]
		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			if (!this.IsWaitNotificationEnabledOrNotRanToCompletion)
			{
				return true;
			}
			if (!this.InternalWait(millisecondsTimeout, cancellationToken))
			{
				return false;
			}
			if (this.IsWaitNotificationEnabledOrNotRanToCompletion)
			{
				this.NotifyDebuggerOfWaitCompletionIfNecessary();
				if (this.IsCanceled)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				this.ThrowIfExceptional(true);
			}
			return true;
		}

		// Token: 0x060040E9 RID: 16617 RVA: 0x000F217C File Offset: 0x000F037C
		private bool WrappedTryRunInline()
		{
			if (this.m_taskScheduler == null)
			{
				return false;
			}
			bool result;
			try
			{
				result = this.m_taskScheduler.TryRunInline(this, true);
			}
			catch (Exception ex)
			{
				if (!(ex is ThreadAbortException))
				{
					TaskSchedulerException ex2 = new TaskSchedulerException(ex);
					throw ex2;
				}
				throw;
			}
			return result;
		}

		// Token: 0x060040EA RID: 16618 RVA: 0x000F21CC File Offset: 0x000F03CC
		[MethodImpl(MethodImplOptions.NoOptimization)]
		internal bool InternalWait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			TplEtwProvider log = TplEtwProvider.Log;
			bool flag = log.IsEnabled();
			if (flag)
			{
				Task internalCurrent = Task.InternalCurrent;
				log.TaskWaitBegin((internalCurrent != null) ? internalCurrent.m_taskScheduler.Id : TaskScheduler.Default.Id, (internalCurrent != null) ? internalCurrent.Id : 0, this.Id, TplEtwProvider.TaskWaitBehavior.Synchronous, 0, Thread.GetDomainID());
			}
			bool flag2 = this.IsCompleted;
			if (!flag2)
			{
				Debugger.NotifyOfCrossThreadDependency();
				flag2 = (millisecondsTimeout == -1 && !cancellationToken.CanBeCanceled && this.WrappedTryRunInline() && this.IsCompleted) || this.SpinThenBlockingWait(millisecondsTimeout, cancellationToken);
			}
			if (flag)
			{
				Task internalCurrent2 = Task.InternalCurrent;
				if (internalCurrent2 != null)
				{
					log.TaskWaitEnd(internalCurrent2.m_taskScheduler.Id, internalCurrent2.Id, this.Id);
				}
				else
				{
					log.TaskWaitEnd(TaskScheduler.Default.Id, 0, this.Id);
				}
				log.TaskWaitContinuationComplete(this.Id);
			}
			return flag2;
		}

		// Token: 0x060040EB RID: 16619 RVA: 0x000F22B4 File Offset: 0x000F04B4
		private bool SpinThenBlockingWait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
			bool flag = millisecondsTimeout == -1;
			uint num = (uint)(flag ? 0 : Environment.TickCount);
			bool flag2 = this.SpinWait(millisecondsTimeout);
			if (!flag2)
			{
				Task.SetOnInvokeMres setOnInvokeMres = new Task.SetOnInvokeMres();
				try
				{
					this.AddCompletionAction(setOnInvokeMres, true);
					if (flag)
					{
						flag2 = setOnInvokeMres.Wait(-1, cancellationToken);
					}
					else
					{
						uint num2 = (uint)(Environment.TickCount - (int)num);
						if ((ulong)num2 < (ulong)((long)millisecondsTimeout))
						{
							flag2 = setOnInvokeMres.Wait((int)((long)millisecondsTimeout - (long)((ulong)num2)), cancellationToken);
						}
					}
				}
				finally
				{
					if (!this.IsCompleted)
					{
						this.RemoveContinuation(setOnInvokeMres);
					}
				}
			}
			return flag2;
		}

		// Token: 0x060040EC RID: 16620 RVA: 0x000F233C File Offset: 0x000F053C
		private bool SpinWait(int millisecondsTimeout)
		{
			if (this.IsCompleted)
			{
				return true;
			}
			if (millisecondsTimeout == 0)
			{
				return false;
			}
			int num = (PlatformHelper.IsSingleProcessor ? 1 : 10);
			for (int i = 0; i < num; i++)
			{
				if (this.IsCompleted)
				{
					return true;
				}
				if (i == num / 2)
				{
					Thread.Yield();
				}
				else
				{
					Thread.SpinWait(4 << i);
				}
			}
			return this.IsCompleted;
		}

		// Token: 0x060040ED RID: 16621 RVA: 0x000F239C File Offset: 0x000F059C
		[SecuritySafeCritical]
		internal bool InternalCancel(bool bCancelNonExecutingOnly)
		{
			bool flag = false;
			bool flag2 = false;
			TaskSchedulerException ex = null;
			if ((this.m_stateFlags & 65536) != 0)
			{
				TaskScheduler taskScheduler = this.m_taskScheduler;
				try
				{
					flag = taskScheduler != null && taskScheduler.TryDequeue(this);
				}
				catch (Exception ex2)
				{
					if (!(ex2 is ThreadAbortException))
					{
						ex = new TaskSchedulerException(ex2);
					}
				}
				bool flag3 = (taskScheduler != null && taskScheduler.RequiresAtomicStartTransition) || (this.Options & (TaskCreationOptions)2048) > TaskCreationOptions.None;
				if (!flag && bCancelNonExecutingOnly && flag3)
				{
					flag2 = this.AtomicStateUpdate(4194304, 4325376);
				}
			}
			if (!bCancelNonExecutingOnly || flag || flag2)
			{
				this.RecordInternalCancellationRequest();
				if (flag)
				{
					flag2 = this.AtomicStateUpdate(4194304, 4325376);
				}
				else if (!flag2 && (this.m_stateFlags & 65536) == 0)
				{
					flag2 = this.AtomicStateUpdate(4194304, 23265280);
				}
				if (flag2)
				{
					this.CancellationCleanupLogic();
				}
			}
			if (ex != null)
			{
				throw ex;
			}
			return flag2;
		}

		// Token: 0x060040EE RID: 16622 RVA: 0x000F2490 File Offset: 0x000F0690
		internal void RecordInternalCancellationRequest()
		{
			Task.ContingentProperties contingentProperties = this.EnsureContingentPropertiesInitialized(true);
			contingentProperties.m_internalCancellationRequested = 1;
		}

		// Token: 0x060040EF RID: 16623 RVA: 0x000F24B0 File Offset: 0x000F06B0
		internal void RecordInternalCancellationRequest(CancellationToken tokenToRecord)
		{
			this.RecordInternalCancellationRequest();
			if (tokenToRecord != default(CancellationToken))
			{
				this.m_contingentProperties.m_cancellationToken = tokenToRecord;
			}
		}

		// Token: 0x060040F0 RID: 16624 RVA: 0x000F24E2 File Offset: 0x000F06E2
		internal void RecordInternalCancellationRequest(CancellationToken tokenToRecord, object cancellationException)
		{
			this.RecordInternalCancellationRequest(tokenToRecord);
			if (cancellationException != null)
			{
				this.AddException(cancellationException, true);
			}
		}

		// Token: 0x060040F1 RID: 16625 RVA: 0x000F24F8 File Offset: 0x000F06F8
		internal void CancellationCleanupLogic()
		{
			Interlocked.Exchange(ref this.m_stateFlags, this.m_stateFlags | 4194304);
			Task.ContingentProperties contingentProperties = this.m_contingentProperties;
			if (contingentProperties != null)
			{
				contingentProperties.SetCompleted();
				contingentProperties.DeregisterCancellationCallback();
			}
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Canceled);
			}
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.RemoveFromActiveTasks(this.Id);
			}
			this.FinishStageThree();
		}

		// Token: 0x060040F2 RID: 16626 RVA: 0x000F2563 File Offset: 0x000F0763
		private void SetCancellationAcknowledged()
		{
			this.m_stateFlags |= 1048576;
		}

		// Token: 0x060040F3 RID: 16627 RVA: 0x000F257C File Offset: 0x000F077C
		[SecuritySafeCritical]
		internal void FinishContinuations()
		{
			object obj = Interlocked.Exchange(ref this.m_continuationObject, Task.s_taskCompletionSentinel);
			TplEtwProvider.Log.RunningContinuation(this.Id, obj);
			if (obj != null)
			{
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, this.Id, CausalitySynchronousWork.CompletionNotification);
				}
				bool flag = (this.m_stateFlags & 134217728) == 0 && Thread.CurrentThread.ThreadState != ThreadState.AbortRequested && (this.m_stateFlags & 64) == 0;
				Action action = obj as Action;
				if (action != null)
				{
					AwaitTaskContinuation.RunOrScheduleAction(action, flag, ref Task.t_currentTask);
					this.LogFinishCompletionNotification();
					return;
				}
				ITaskCompletionAction taskCompletionAction = obj as ITaskCompletionAction;
				if (taskCompletionAction != null)
				{
					if (flag)
					{
						taskCompletionAction.Invoke(this);
					}
					else
					{
						ThreadPool.UnsafeQueueCustomWorkItem(new CompletionActionInvoker(taskCompletionAction, this), false);
					}
					this.LogFinishCompletionNotification();
					return;
				}
				TaskContinuation taskContinuation = obj as TaskContinuation;
				if (taskContinuation != null)
				{
					taskContinuation.Run(this, flag);
					this.LogFinishCompletionNotification();
					return;
				}
				List<object> list = obj as List<object>;
				if (list == null)
				{
					this.LogFinishCompletionNotification();
					return;
				}
				List<object> obj2 = list;
				lock (obj2)
				{
				}
				int count = list.Count;
				for (int i = 0; i < count; i++)
				{
					StandardTaskContinuation standardTaskContinuation = list[i] as StandardTaskContinuation;
					if (standardTaskContinuation != null && (standardTaskContinuation.m_options & TaskContinuationOptions.ExecuteSynchronously) == TaskContinuationOptions.None)
					{
						TplEtwProvider.Log.RunningContinuationList(this.Id, i, standardTaskContinuation);
						list[i] = null;
						standardTaskContinuation.Run(this, flag);
					}
				}
				for (int j = 0; j < count; j++)
				{
					object obj3 = list[j];
					if (obj3 != null)
					{
						list[j] = null;
						TplEtwProvider.Log.RunningContinuationList(this.Id, j, obj3);
						Action action2 = obj3 as Action;
						if (action2 != null)
						{
							AwaitTaskContinuation.RunOrScheduleAction(action2, flag, ref Task.t_currentTask);
						}
						else
						{
							TaskContinuation taskContinuation2 = obj3 as TaskContinuation;
							if (taskContinuation2 != null)
							{
								taskContinuation2.Run(this, flag);
							}
							else
							{
								ITaskCompletionAction taskCompletionAction2 = (ITaskCompletionAction)obj3;
								if (flag)
								{
									taskCompletionAction2.Invoke(this);
								}
								else
								{
									ThreadPool.UnsafeQueueCustomWorkItem(new CompletionActionInvoker(taskCompletionAction2, this), false);
								}
							}
						}
					}
				}
				this.LogFinishCompletionNotification();
			}
		}

		// Token: 0x060040F4 RID: 16628 RVA: 0x000F27A4 File Offset: 0x000F09A4
		private void LogFinishCompletionNotification()
		{
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.CompletionNotification);
			}
		}

		// Token: 0x060040F5 RID: 16629 RVA: 0x000F27B4 File Offset: 0x000F09B4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task> continuationAction)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, TaskScheduler.Current, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040F6 RID: 16630 RVA: 0x000F27DC File Offset: 0x000F09DC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040F7 RID: 16631 RVA: 0x000F27FC File Offset: 0x000F09FC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, scheduler, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040F8 RID: 16632 RVA: 0x000F2820 File Offset: 0x000F0A20
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, TaskScheduler.Current, default(CancellationToken), continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x060040F9 RID: 16633 RVA: 0x000F2848 File Offset: 0x000F0A48
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, scheduler, cancellationToken, continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x060040FA RID: 16634 RVA: 0x000F2864 File Offset: 0x000F0A64
		private Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			TaskCreationOptions creationOptions;
			InternalTaskOptions internalOptions;
			Task.CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
			Task task = new ContinuationTaskFromTask(this, continuationAction, null, creationOptions, internalOptions, ref stackMark);
			this.ContinueWithCore(task, scheduler, cancellationToken, continuationOptions);
			return task;
		}

		// Token: 0x060040FB RID: 16635 RVA: 0x000F28B4 File Offset: 0x000F0AB4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task, object> continuationAction, object state)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, state, TaskScheduler.Current, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040FC RID: 16636 RVA: 0x000F28DC File Offset: 0x000F0ADC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040FD RID: 16637 RVA: 0x000F28FC File Offset: 0x000F0AFC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, state, scheduler, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x060040FE RID: 16638 RVA: 0x000F2920 File Offset: 0x000F0B20
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task, object> continuationAction, object state, TaskContinuationOptions continuationOptions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, state, TaskScheduler.Current, default(CancellationToken), continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x060040FF RID: 16639 RVA: 0x000F2948 File Offset: 0x000F0B48
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task ContinueWith(Action<Task, object> continuationAction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith(continuationAction, state, scheduler, cancellationToken, continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x06004100 RID: 16640 RVA: 0x000F2968 File Offset: 0x000F0B68
		private Task ContinueWith(Action<Task, object> continuationAction, object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
		{
			if (continuationAction == null)
			{
				throw new ArgumentNullException("continuationAction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			TaskCreationOptions creationOptions;
			InternalTaskOptions internalOptions;
			Task.CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
			Task task = new ContinuationTaskFromTask(this, continuationAction, state, creationOptions, internalOptions, ref stackMark);
			this.ContinueWithCore(task, scheduler, cancellationToken, continuationOptions);
			return task;
		}

		// Token: 0x06004101 RID: 16641 RVA: 0x000F29B8 File Offset: 0x000F0BB8
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x06004102 RID: 16642 RVA: 0x000F29E0 File Offset: 0x000F0BE0
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x06004103 RID: 16643 RVA: 0x000F2A00 File Offset: 0x000F0C00
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, scheduler, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x06004104 RID: 16644 RVA: 0x000F2A24 File Offset: 0x000F0C24
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, default(CancellationToken), continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x06004105 RID: 16645 RVA: 0x000F2A4C File Offset: 0x000F0C4C
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x06004106 RID: 16646 RVA: 0x000F2A68 File Offset: 0x000F0C68
		private Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			TaskCreationOptions creationOptions;
			InternalTaskOptions internalOptions;
			Task.CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
			Task<TResult> task = new ContinuationResultTaskFromTask<TResult>(this, continuationFunction, null, creationOptions, internalOptions, ref stackMark);
			this.ContinueWithCore(task, scheduler, cancellationToken, continuationOptions);
			return task;
		}

		// Token: 0x06004107 RID: 16647 RVA: 0x000F2AB8 File Offset: 0x000F0CB8
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x06004108 RID: 16648 RVA: 0x000F2AE0 File Offset: 0x000F0CE0
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x06004109 RID: 16649 RVA: 0x000F2B00 File Offset: 0x000F0D00
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, state, scheduler, default(CancellationToken), TaskContinuationOptions.None, ref stackCrawlMark);
		}

		// Token: 0x0600410A RID: 16650 RVA: 0x000F2B24 File Offset: 0x000F0D24
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskContinuationOptions continuationOptions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, default(CancellationToken), continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x0600410B RID: 16651 RVA: 0x000F2B4C File Offset: 0x000F0D4C
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.ContinueWith<TResult>(continuationFunction, state, scheduler, cancellationToken, continuationOptions, ref stackCrawlMark);
		}

		// Token: 0x0600410C RID: 16652 RVA: 0x000F2B6C File Offset: 0x000F0D6C
		private Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
		{
			if (continuationFunction == null)
			{
				throw new ArgumentNullException("continuationFunction");
			}
			if (scheduler == null)
			{
				throw new ArgumentNullException("scheduler");
			}
			TaskCreationOptions creationOptions;
			InternalTaskOptions internalOptions;
			Task.CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
			Task<TResult> task = new ContinuationResultTaskFromTask<TResult>(this, continuationFunction, state, creationOptions, internalOptions, ref stackMark);
			this.ContinueWithCore(task, scheduler, cancellationToken, continuationOptions);
			return task;
		}

		// Token: 0x0600410D RID: 16653 RVA: 0x000F2BBC File Offset: 0x000F0DBC
		internal static void CreationOptionsFromContinuationOptions(TaskContinuationOptions continuationOptions, out TaskCreationOptions creationOptions, out InternalTaskOptions internalOptions)
		{
			TaskContinuationOptions taskContinuationOptions = TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled;
			TaskContinuationOptions taskContinuationOptions2 = TaskContinuationOptions.PreferFairness | TaskContinuationOptions.LongRunning | TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler | TaskContinuationOptions.RunContinuationsAsynchronously;
			TaskContinuationOptions taskContinuationOptions3 = TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously;
			if ((continuationOptions & taskContinuationOptions3) == taskContinuationOptions3)
			{
				throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_ContinueWith_ESandLR"));
			}
			if ((continuationOptions & ~((taskContinuationOptions2 | taskContinuationOptions | TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously) != TaskContinuationOptions.None)) != TaskContinuationOptions.None)
			{
				throw new ArgumentOutOfRangeException("continuationOptions");
			}
			if ((continuationOptions & taskContinuationOptions) == taskContinuationOptions)
			{
				throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_ContinueWith_NotOnAnything"));
			}
			creationOptions = (TaskCreationOptions)(continuationOptions & taskContinuationOptions2);
			internalOptions = InternalTaskOptions.ContinuationTask;
			if ((continuationOptions & TaskContinuationOptions.LazyCancellation) != TaskContinuationOptions.None)
			{
				internalOptions |= InternalTaskOptions.LazyCancellation;
			}
		}

		// Token: 0x0600410E RID: 16654 RVA: 0x000F2C48 File Offset: 0x000F0E48
		internal void ContinueWithCore(Task continuationTask, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions options)
		{
			TaskContinuation taskContinuation = new StandardTaskContinuation(continuationTask, options, scheduler);
			if (cancellationToken.CanBeCanceled)
			{
				if (this.IsCompleted || cancellationToken.IsCancellationRequested)
				{
					continuationTask.AssignCancellationToken(cancellationToken, null, null);
				}
				else
				{
					continuationTask.AssignCancellationToken(cancellationToken, this, taskContinuation);
				}
			}
			if (!continuationTask.IsCompleted)
			{
				if ((this.Options & (TaskCreationOptions)1024) != TaskCreationOptions.None && !(this is ITaskCompletionAction))
				{
					TplEtwProvider log = TplEtwProvider.Log;
					if (log.IsEnabled())
					{
						log.AwaitTaskContinuationScheduled(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), continuationTask.Id);
					}
				}
				if (!this.AddTaskContinuation(taskContinuation, false))
				{
					taskContinuation.Run(this, true);
				}
			}
		}

		// Token: 0x0600410F RID: 16655 RVA: 0x000F2CF0 File Offset: 0x000F0EF0
		internal void AddCompletionAction(ITaskCompletionAction action)
		{
			this.AddCompletionAction(action, false);
		}

		// Token: 0x06004110 RID: 16656 RVA: 0x000F2CFA File Offset: 0x000F0EFA
		private void AddCompletionAction(ITaskCompletionAction action, bool addBeforeOthers)
		{
			if (!this.AddTaskContinuation(action, addBeforeOthers))
			{
				action.Invoke(this);
			}
		}

		// Token: 0x06004111 RID: 16657 RVA: 0x000F2D10 File Offset: 0x000F0F10
		private bool AddTaskContinuationComplex(object tc, bool addBeforeOthers)
		{
			object continuationObject = this.m_continuationObject;
			if (continuationObject != Task.s_taskCompletionSentinel && !(continuationObject is List<object>))
			{
				Interlocked.CompareExchange(ref this.m_continuationObject, new List<object> { continuationObject }, continuationObject);
			}
			List<object> list = this.m_continuationObject as List<object>;
			if (list != null)
			{
				List<object> obj = list;
				lock (obj)
				{
					if (this.m_continuationObject != Task.s_taskCompletionSentinel)
					{
						if (list.Count == list.Capacity)
						{
							list.RemoveAll(Task.s_IsTaskContinuationNullPredicate);
						}
						if (addBeforeOthers)
						{
							list.Insert(0, tc);
						}
						else
						{
							list.Add(tc);
						}
						return true;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06004112 RID: 16658 RVA: 0x000F2DD4 File Offset: 0x000F0FD4
		private bool AddTaskContinuation(object tc, bool addBeforeOthers)
		{
			return !this.IsCompleted && ((this.m_continuationObject == null && Interlocked.CompareExchange(ref this.m_continuationObject, tc, null) == null) || this.AddTaskContinuationComplex(tc, addBeforeOthers));
		}

		// Token: 0x06004113 RID: 16659 RVA: 0x000F2E04 File Offset: 0x000F1004
		internal void RemoveContinuation(object continuationObject)
		{
			object continuationObject2 = this.m_continuationObject;
			if (continuationObject2 == Task.s_taskCompletionSentinel)
			{
				return;
			}
			List<object> list = continuationObject2 as List<object>;
			if (list == null)
			{
				if (Interlocked.CompareExchange(ref this.m_continuationObject, new List<object>(), continuationObject) == continuationObject)
				{
					return;
				}
				list = this.m_continuationObject as List<object>;
			}
			if (list != null)
			{
				List<object> obj = list;
				lock (obj)
				{
					if (this.m_continuationObject != Task.s_taskCompletionSentinel)
					{
						int num = list.IndexOf(continuationObject);
						if (num != -1)
						{
							list[num] = null;
						}
					}
				}
			}
		}

		// Token: 0x06004114 RID: 16660 RVA: 0x000F2EA8 File Offset: 0x000F10A8
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static void WaitAll(params Task[] tasks)
		{
			Task.WaitAll(tasks, -1);
		}

		// Token: 0x06004115 RID: 16661 RVA: 0x000F2EB4 File Offset: 0x000F10B4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static bool WaitAll(Task[] tasks, TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
			return Task.WaitAll(tasks, (int)num);
		}

		// Token: 0x06004116 RID: 16662 RVA: 0x000F2EEC File Offset: 0x000F10EC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static bool WaitAll(Task[] tasks, int millisecondsTimeout)
		{
			return Task.WaitAll(tasks, millisecondsTimeout, default(CancellationToken));
		}

		// Token: 0x06004117 RID: 16663 RVA: 0x000F2F09 File Offset: 0x000F1109
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static void WaitAll(Task[] tasks, CancellationToken cancellationToken)
		{
			Task.WaitAll(tasks, -1, cancellationToken);
		}

		// Token: 0x06004118 RID: 16664 RVA: 0x000F2F14 File Offset: 0x000F1114
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			cancellationToken.ThrowIfCancellationRequested();
			List<Exception> innerExceptions = null;
			List<Task> list = null;
			List<Task> list2 = null;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = true;
			for (int i = tasks.Length - 1; i >= 0; i--)
			{
				Task task = tasks[i];
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_WaitMulti_NullTask"), "tasks");
				}
				bool flag4 = task.IsCompleted;
				if (!flag4)
				{
					if (millisecondsTimeout != -1 || cancellationToken.CanBeCanceled)
					{
						Task.AddToList<Task>(task, ref list, tasks.Length);
					}
					else
					{
						flag4 = task.WrappedTryRunInline() && task.IsCompleted;
						if (!flag4)
						{
							Task.AddToList<Task>(task, ref list, tasks.Length);
						}
					}
				}
				if (flag4)
				{
					if (task.IsFaulted)
					{
						flag = true;
					}
					else if (task.IsCanceled)
					{
						flag2 = true;
					}
					if (task.IsWaitNotificationEnabled)
					{
						Task.AddToList<Task>(task, ref list2, 1);
					}
				}
			}
			if (list != null)
			{
				flag3 = Task.WaitAllBlockingCore(list, millisecondsTimeout, cancellationToken);
				if (flag3)
				{
					foreach (Task task2 in list)
					{
						if (task2.IsFaulted)
						{
							flag = true;
						}
						else if (task2.IsCanceled)
						{
							flag2 = true;
						}
						if (task2.IsWaitNotificationEnabled)
						{
							Task.AddToList<Task>(task2, ref list2, 1);
						}
					}
				}
				GC.KeepAlive(tasks);
			}
			if (flag3 && list2 != null)
			{
				foreach (Task task3 in list2)
				{
					if (task3.NotifyDebuggerOfWaitCompletionIfNecessary())
					{
						break;
					}
				}
			}
			if (flag3 && (flag || flag2))
			{
				if (!flag)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				foreach (Task t in tasks)
				{
					Task.AddExceptionsForCompletedTask(ref innerExceptions, t);
				}
				throw new AggregateException(innerExceptions);
			}
			return flag3;
		}

		// Token: 0x06004119 RID: 16665 RVA: 0x000F3118 File Offset: 0x000F1318
		private static void AddToList<T>(T item, ref List<T> list, int initSize)
		{
			if (list == null)
			{
				list = new List<T>(initSize);
			}
			list.Add(item);
		}

		// Token: 0x0600411A RID: 16666 RVA: 0x000F3130 File Offset: 0x000F1330
		private static bool WaitAllBlockingCore(List<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			bool flag = false;
			Task.SetOnCountdownMres setOnCountdownMres = new Task.SetOnCountdownMres(tasks.Count);
			try
			{
				foreach (Task task in tasks)
				{
					task.AddCompletionAction(setOnCountdownMres, true);
				}
				flag = setOnCountdownMres.Wait(millisecondsTimeout, cancellationToken);
			}
			finally
			{
				if (!flag)
				{
					foreach (Task task2 in tasks)
					{
						if (!task2.IsCompleted)
						{
							task2.RemoveContinuation(setOnCountdownMres);
						}
					}
				}
			}
			return flag;
		}

		// Token: 0x0600411B RID: 16667 RVA: 0x000F31F4 File Offset: 0x000F13F4
		internal static void FastWaitAll(Task[] tasks)
		{
			List<Exception> list = null;
			for (int i = tasks.Length - 1; i >= 0; i--)
			{
				if (!tasks[i].IsCompleted)
				{
					tasks[i].WrappedTryRunInline();
				}
			}
			for (int j = tasks.Length - 1; j >= 0; j--)
			{
				Task task = tasks[j];
				task.SpinThenBlockingWait(-1, default(CancellationToken));
				Task.AddExceptionsForCompletedTask(ref list, task);
			}
			if (list != null)
			{
				throw new AggregateException(list);
			}
		}

		// Token: 0x0600411C RID: 16668 RVA: 0x000F3260 File Offset: 0x000F1460
		internal static void AddExceptionsForCompletedTask(ref List<Exception> exceptions, Task t)
		{
			AggregateException exceptions2 = t.GetExceptions(true);
			if (exceptions2 != null)
			{
				t.UpdateExceptionObservedStatus();
				if (exceptions == null)
				{
					exceptions = new List<Exception>(exceptions2.InnerExceptions.Count);
				}
				exceptions.AddRange(exceptions2.InnerExceptions);
			}
		}

		// Token: 0x0600411D RID: 16669 RVA: 0x000F32A4 File Offset: 0x000F14A4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static int WaitAny(params Task[] tasks)
		{
			return Task.WaitAny(tasks, -1);
		}

		// Token: 0x0600411E RID: 16670 RVA: 0x000F32BC File Offset: 0x000F14BC
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static int WaitAny(Task[] tasks, TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout");
			}
			return Task.WaitAny(tasks, (int)num);
		}

		// Token: 0x0600411F RID: 16671 RVA: 0x000F32F3 File Offset: 0x000F14F3
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static int WaitAny(Task[] tasks, CancellationToken cancellationToken)
		{
			return Task.WaitAny(tasks, -1, cancellationToken);
		}

		// Token: 0x06004120 RID: 16672 RVA: 0x000F3300 File Offset: 0x000F1500
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static int WaitAny(Task[] tasks, int millisecondsTimeout)
		{
			return Task.WaitAny(tasks, millisecondsTimeout, default(CancellationToken));
		}

		// Token: 0x06004121 RID: 16673 RVA: 0x000F3320 File Offset: 0x000F1520
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout");
			}
			cancellationToken.ThrowIfCancellationRequested();
			int num = -1;
			for (int i = 0; i < tasks.Length; i++)
			{
				Task task = tasks[i];
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_WaitMulti_NullTask"), "tasks");
				}
				if (num == -1 && task.IsCompleted)
				{
					num = i;
				}
			}
			if (num == -1 && tasks.Length != 0)
			{
				Task<Task> task2 = TaskFactory.CommonCWAnyLogic(tasks);
				bool flag = task2.Wait(millisecondsTimeout, cancellationToken);
				if (flag)
				{
					num = Array.IndexOf<Task>(tasks, task2.Result);
				}
			}
			GC.KeepAlive(tasks);
			return num;
		}

		// Token: 0x06004122 RID: 16674 RVA: 0x000F33BC File Offset: 0x000F15BC
		[__DynamicallyInvokable]
		public static Task<TResult> FromResult<TResult>(TResult result)
		{
			return new Task<TResult>(result);
		}

		// Token: 0x06004123 RID: 16675 RVA: 0x000F33C4 File Offset: 0x000F15C4
		[__DynamicallyInvokable]
		public static Task FromException(Exception exception)
		{
			return Task.FromException<VoidTaskResult>(exception);
		}

		// Token: 0x06004124 RID: 16676 RVA: 0x000F33CC File Offset: 0x000F15CC
		[__DynamicallyInvokable]
		public static Task<TResult> FromException<TResult>(Exception exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}
			Task<TResult> task = new Task<TResult>();
			bool flag = task.TrySetException(exception);
			return task;
		}

		// Token: 0x06004125 RID: 16677 RVA: 0x000F33F6 File Offset: 0x000F15F6
		[FriendAccessAllowed]
		internal static Task FromCancellation(CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				throw new ArgumentOutOfRangeException("cancellationToken");
			}
			return new Task(true, TaskCreationOptions.None, cancellationToken);
		}

		// Token: 0x06004126 RID: 16678 RVA: 0x000F3414 File Offset: 0x000F1614
		[__DynamicallyInvokable]
		public static Task FromCanceled(CancellationToken cancellationToken)
		{
			return Task.FromCancellation(cancellationToken);
		}

		// Token: 0x06004127 RID: 16679 RVA: 0x000F341C File Offset: 0x000F161C
		[FriendAccessAllowed]
		internal static Task<TResult> FromCancellation<TResult>(CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				throw new ArgumentOutOfRangeException("cancellationToken");
			}
			return new Task<TResult>(true, default(TResult), TaskCreationOptions.None, cancellationToken);
		}

		// Token: 0x06004128 RID: 16680 RVA: 0x000F344E File Offset: 0x000F164E
		[__DynamicallyInvokable]
		public static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
		{
			return Task.FromCancellation<TResult>(cancellationToken);
		}

		// Token: 0x06004129 RID: 16681 RVA: 0x000F3458 File Offset: 0x000F1658
		internal static Task<TResult> FromCancellation<TResult>(OperationCanceledException exception)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}
			Task<TResult> task = new Task<TResult>();
			bool flag = task.TrySetCanceled(exception.CancellationToken, exception);
			return task;
		}

		// Token: 0x0600412A RID: 16682 RVA: 0x000F3488 File Offset: 0x000F1688
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Task Run(Action action)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return Task.InternalStartNew(null, action, null, default(CancellationToken), TaskScheduler.Default, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, ref stackCrawlMark);
		}

		// Token: 0x0600412B RID: 16683 RVA: 0x000F34B4 File Offset: 0x000F16B4
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Task Run(Action action, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return Task.InternalStartNew(null, action, null, cancellationToken, TaskScheduler.Default, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, ref stackCrawlMark);
		}

		// Token: 0x0600412C RID: 16684 RVA: 0x000F34D8 File Offset: 0x000F16D8
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Task<TResult> Run<TResult>(Func<TResult> function)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return Task<TResult>.StartNew(null, function, default(CancellationToken), TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, TaskScheduler.Default, ref stackCrawlMark);
		}

		// Token: 0x0600412D RID: 16685 RVA: 0x000F3500 File Offset: 0x000F1700
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return Task<TResult>.StartNew(null, function, cancellationToken, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, TaskScheduler.Default, ref stackCrawlMark);
		}

		// Token: 0x0600412E RID: 16686 RVA: 0x000F3520 File Offset: 0x000F1720
		[__DynamicallyInvokable]
		public static Task Run(Func<Task> function)
		{
			return Task.Run(function, default(CancellationToken));
		}

		// Token: 0x0600412F RID: 16687 RVA: 0x000F353C File Offset: 0x000F173C
		[__DynamicallyInvokable]
		public static Task Run(Func<Task> function, CancellationToken cancellationToken)
		{
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
			{
				cancellationToken.ThrowIfSourceDisposed();
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			Task<Task> outerTask = Task<Task>.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			return new UnwrapPromise<VoidTaskResult>(outerTask, true);
		}

		// Token: 0x06004130 RID: 16688 RVA: 0x000F3594 File Offset: 0x000F1794
		[__DynamicallyInvokable]
		public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
		{
			return Task.Run<TResult>(function, default(CancellationToken));
		}

		// Token: 0x06004131 RID: 16689 RVA: 0x000F35B0 File Offset: 0x000F17B0
		[__DynamicallyInvokable]
		public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
		{
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
			{
				cancellationToken.ThrowIfSourceDisposed();
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation<TResult>(cancellationToken);
			}
			Task<Task<TResult>> outerTask = Task<Task<TResult>>.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			return new UnwrapPromise<TResult>(outerTask, true);
		}

		// Token: 0x06004132 RID: 16690 RVA: 0x000F3608 File Offset: 0x000F1808
		[__DynamicallyInvokable]
		public static Task Delay(TimeSpan delay)
		{
			return Task.Delay(delay, default(CancellationToken));
		}

		// Token: 0x06004133 RID: 16691 RVA: 0x000F3624 File Offset: 0x000F1824
		[__DynamicallyInvokable]
		public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
		{
			long num = (long)delay.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("delay", Environment.GetResourceString("Task_Delay_InvalidDelay"));
			}
			return Task.Delay((int)num, cancellationToken);
		}

		// Token: 0x06004134 RID: 16692 RVA: 0x000F3668 File Offset: 0x000F1868
		[__DynamicallyInvokable]
		public static Task Delay(int millisecondsDelay)
		{
			return Task.Delay(millisecondsDelay, default(CancellationToken));
		}

		// Token: 0x06004135 RID: 16693 RVA: 0x000F3684 File Offset: 0x000F1884
		[__DynamicallyInvokable]
		public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
		{
			if (millisecondsDelay < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsDelay", Environment.GetResourceString("Task_Delay_InvalidMillisecondsDelay"));
			}
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCancellation(cancellationToken);
			}
			if (millisecondsDelay == 0)
			{
				return Task.CompletedTask;
			}
			Task.DelayPromise delayPromise = new Task.DelayPromise(cancellationToken);
			if (cancellationToken.CanBeCanceled)
			{
				delayPromise.Registration = cancellationToken.InternalRegisterWithoutEC(delegate(object state)
				{
					((Task.DelayPromise)state).Complete();
				}, delayPromise);
			}
			if (millisecondsDelay != -1)
			{
				delayPromise.Timer = new Timer(delegate(object state)
				{
					((Task.DelayPromise)state).Complete();
				}, delayPromise, millisecondsDelay, -1);
				delayPromise.Timer.KeepRootedWhileScheduled();
			}
			return delayPromise;
		}

		// Token: 0x06004136 RID: 16694 RVA: 0x000F3740 File Offset: 0x000F1940
		[__DynamicallyInvokable]
		public static Task WhenAll(IEnumerable<Task> tasks)
		{
			Task[] array = tasks as Task[];
			if (array != null)
			{
				return Task.WhenAll(array);
			}
			ICollection<Task> collection = tasks as ICollection<Task>;
			if (collection != null)
			{
				int num = 0;
				array = new Task[collection.Count];
				foreach (Task task in tasks)
				{
					if (task == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
					}
					array[num++] = task;
				}
				return Task.InternalWhenAll(array);
			}
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			List<Task> list = new List<Task>();
			foreach (Task task2 in tasks)
			{
				if (task2 == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				list.Add(task2);
			}
			return Task.InternalWhenAll(list.ToArray());
		}

		// Token: 0x06004137 RID: 16695 RVA: 0x000F3850 File Offset: 0x000F1A50
		[__DynamicallyInvokable]
		public static Task WhenAll(params Task[] tasks)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			int num = tasks.Length;
			if (num == 0)
			{
				return Task.InternalWhenAll(tasks);
			}
			Task[] array = new Task[num];
			for (int i = 0; i < num; i++)
			{
				Task task = tasks[i];
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				array[i] = task;
			}
			return Task.InternalWhenAll(array);
		}

		// Token: 0x06004138 RID: 16696 RVA: 0x000F38B2 File Offset: 0x000F1AB2
		private static Task InternalWhenAll(Task[] tasks)
		{
			if (tasks.Length != 0)
			{
				return new Task.WhenAllPromise(tasks);
			}
			return Task.CompletedTask;
		}

		// Token: 0x06004139 RID: 16697 RVA: 0x000F38C4 File Offset: 0x000F1AC4
		[__DynamicallyInvokable]
		public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
		{
			Task<TResult>[] array = tasks as Task<TResult>[];
			if (array != null)
			{
				return Task.WhenAll<TResult>(array);
			}
			ICollection<Task<TResult>> collection = tasks as ICollection<Task<TResult>>;
			if (collection != null)
			{
				int num = 0;
				array = new Task<TResult>[collection.Count];
				foreach (Task<TResult> task in tasks)
				{
					if (task == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
					}
					array[num++] = task;
				}
				return Task.InternalWhenAll<TResult>(array);
			}
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			List<Task<TResult>> list = new List<Task<TResult>>();
			foreach (Task<TResult> task2 in tasks)
			{
				if (task2 == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				list.Add(task2);
			}
			return Task.InternalWhenAll<TResult>(list.ToArray());
		}

		// Token: 0x0600413A RID: 16698 RVA: 0x000F39D4 File Offset: 0x000F1BD4
		[__DynamicallyInvokable]
		public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			int num = tasks.Length;
			if (num == 0)
			{
				return Task.InternalWhenAll<TResult>(tasks);
			}
			Task<TResult>[] array = new Task<TResult>[num];
			for (int i = 0; i < num; i++)
			{
				Task<TResult> task = tasks[i];
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				array[i] = task;
			}
			return Task.InternalWhenAll<TResult>(array);
		}

		// Token: 0x0600413B RID: 16699 RVA: 0x000F3A38 File Offset: 0x000F1C38
		private static Task<TResult[]> InternalWhenAll<TResult>(Task<TResult>[] tasks)
		{
			if (tasks.Length != 0)
			{
				return new Task.WhenAllPromise<TResult>(tasks);
			}
			return new Task<TResult[]>(false, new TResult[0], TaskCreationOptions.None, default(CancellationToken));
		}

		// Token: 0x0600413C RID: 16700 RVA: 0x000F3A68 File Offset: 0x000F1C68
		[__DynamicallyInvokable]
		public static Task<Task> WhenAny(params Task[] tasks)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			if (tasks.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
			}
			int num = tasks.Length;
			Task[] array = new Task[num];
			for (int i = 0; i < num; i++)
			{
				Task task = tasks[i];
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				array[i] = task;
			}
			return TaskFactory.CommonCWAnyLogic(array);
		}

		// Token: 0x0600413D RID: 16701 RVA: 0x000F3ADC File Offset: 0x000F1CDC
		[__DynamicallyInvokable]
		public static Task<Task> WhenAny(IEnumerable<Task> tasks)
		{
			if (tasks == null)
			{
				throw new ArgumentNullException("tasks");
			}
			List<Task> list = new List<Task>();
			foreach (Task task in tasks)
			{
				if (task == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
				}
				list.Add(task);
			}
			if (list.Count == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
			}
			return TaskFactory.CommonCWAnyLogic(list);
		}

		// Token: 0x0600413E RID: 16702 RVA: 0x000F3B74 File Offset: 0x000F1D74
		[__DynamicallyInvokable]
		public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
		{
			Task<Task> task = Task.WhenAny(tasks);
			return task.ContinueWith<Task<TResult>>(Task<TResult>.TaskWhenAnyCast, default(CancellationToken), TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		// Token: 0x0600413F RID: 16703 RVA: 0x000F3BA8 File Offset: 0x000F1DA8
		[__DynamicallyInvokable]
		public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
		{
			Task<Task> task = Task.WhenAny(tasks);
			return task.ContinueWith<Task<TResult>>(Task<TResult>.TaskWhenAnyCast, default(CancellationToken), TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		// Token: 0x06004140 RID: 16704 RVA: 0x000F3BDA File Offset: 0x000F1DDA
		[FriendAccessAllowed]
		internal static Task<TResult> CreateUnwrapPromise<TResult>(Task outerTask, bool lookForOce)
		{
			return new UnwrapPromise<TResult>(outerTask, lookForOce);
		}

		// Token: 0x06004141 RID: 16705 RVA: 0x000F3BE3 File Offset: 0x000F1DE3
		internal virtual Delegate[] GetDelegateContinuationsForDebugger()
		{
			if (this.m_continuationObject != this)
			{
				return Task.GetDelegatesFromContinuationObject(this.m_continuationObject);
			}
			return null;
		}

		// Token: 0x06004142 RID: 16706 RVA: 0x000F3C00 File Offset: 0x000F1E00
		internal static Delegate[] GetDelegatesFromContinuationObject(object continuationObject)
		{
			if (continuationObject != null)
			{
				Action action = continuationObject as Action;
				if (action != null)
				{
					return new Delegate[] { AsyncMethodBuilderCore.TryGetStateMachineForDebugger(action) };
				}
				TaskContinuation taskContinuation = continuationObject as TaskContinuation;
				if (taskContinuation != null)
				{
					return taskContinuation.GetDelegateContinuationsForDebugger();
				}
				Task task = continuationObject as Task;
				if (task != null)
				{
					Delegate[] delegateContinuationsForDebugger = task.GetDelegateContinuationsForDebugger();
					if (delegateContinuationsForDebugger != null)
					{
						return delegateContinuationsForDebugger;
					}
				}
				ITaskCompletionAction taskCompletionAction = continuationObject as ITaskCompletionAction;
				if (taskCompletionAction != null)
				{
					return new Delegate[]
					{
						new Action<Task>(taskCompletionAction.Invoke)
					};
				}
				List<object> list = continuationObject as List<object>;
				if (list != null)
				{
					List<Delegate> list2 = new List<Delegate>();
					foreach (object continuationObject2 in list)
					{
						Delegate[] delegatesFromContinuationObject = Task.GetDelegatesFromContinuationObject(continuationObject2);
						if (delegatesFromContinuationObject != null)
						{
							foreach (Delegate @delegate in delegatesFromContinuationObject)
							{
								if (@delegate != null)
								{
									list2.Add(@delegate);
								}
							}
						}
					}
					return list2.ToArray();
				}
			}
			return null;
		}

		// Token: 0x06004143 RID: 16707 RVA: 0x000F3D0C File Offset: 0x000F1F0C
		private static Task GetActiveTaskFromId(int taskId)
		{
			Task result = null;
			Task.s_currentActiveTasks.TryGetValue(taskId, out result);
			return result;
		}

		// Token: 0x06004144 RID: 16708 RVA: 0x000F3D2A File Offset: 0x000F1F2A
		private static Task[] GetActiveTasks()
		{
			return new List<Task>(Task.s_currentActiveTasks.Values).ToArray();
		}

		// Token: 0x04001AF8 RID: 6904
		[ThreadStatic]
		internal static Task t_currentTask;

		// Token: 0x04001AF9 RID: 6905
		[ThreadStatic]
		private static StackGuard t_stackGuard;

		// Token: 0x04001AFA RID: 6906
		internal static int s_taskIdCounter;

		// Token: 0x04001AFB RID: 6907
		private static readonly TaskFactory s_factory = new TaskFactory();

		// Token: 0x04001AFC RID: 6908
		private volatile int m_taskId;

		// Token: 0x04001AFD RID: 6909
		internal object m_action;

		// Token: 0x04001AFE RID: 6910
		internal object m_stateObject;

		// Token: 0x04001AFF RID: 6911
		internal TaskScheduler m_taskScheduler;

		// Token: 0x04001B00 RID: 6912
		internal readonly Task m_parent;

		// Token: 0x04001B01 RID: 6913
		internal volatile int m_stateFlags;

		// Token: 0x04001B02 RID: 6914
		private const int OptionsMask = 65535;

		// Token: 0x04001B03 RID: 6915
		internal const int TASK_STATE_STARTED = 65536;

		// Token: 0x04001B04 RID: 6916
		internal const int TASK_STATE_DELEGATE_INVOKED = 131072;

		// Token: 0x04001B05 RID: 6917
		internal const int TASK_STATE_DISPOSED = 262144;

		// Token: 0x04001B06 RID: 6918
		internal const int TASK_STATE_EXCEPTIONOBSERVEDBYPARENT = 524288;

		// Token: 0x04001B07 RID: 6919
		internal const int TASK_STATE_CANCELLATIONACKNOWLEDGED = 1048576;

		// Token: 0x04001B08 RID: 6920
		internal const int TASK_STATE_FAULTED = 2097152;

		// Token: 0x04001B09 RID: 6921
		internal const int TASK_STATE_CANCELED = 4194304;

		// Token: 0x04001B0A RID: 6922
		internal const int TASK_STATE_WAITING_ON_CHILDREN = 8388608;

		// Token: 0x04001B0B RID: 6923
		internal const int TASK_STATE_RAN_TO_COMPLETION = 16777216;

		// Token: 0x04001B0C RID: 6924
		internal const int TASK_STATE_WAITINGFORACTIVATION = 33554432;

		// Token: 0x04001B0D RID: 6925
		internal const int TASK_STATE_COMPLETION_RESERVED = 67108864;

		// Token: 0x04001B0E RID: 6926
		internal const int TASK_STATE_THREAD_WAS_ABORTED = 134217728;

		// Token: 0x04001B0F RID: 6927
		internal const int TASK_STATE_WAIT_COMPLETION_NOTIFICATION = 268435456;

		// Token: 0x04001B10 RID: 6928
		internal const int TASK_STATE_EXECUTIONCONTEXT_IS_NULL = 536870912;

		// Token: 0x04001B11 RID: 6929
		internal const int TASK_STATE_TASKSCHEDULED_WAS_FIRED = 1073741824;

		// Token: 0x04001B12 RID: 6930
		private const int TASK_STATE_COMPLETED_MASK = 23068672;

		// Token: 0x04001B13 RID: 6931
		private const int CANCELLATION_REQUESTED = 1;

		// Token: 0x04001B14 RID: 6932
		private volatile object m_continuationObject;

		// Token: 0x04001B15 RID: 6933
		private static readonly object s_taskCompletionSentinel = new object();

		// Token: 0x04001B16 RID: 6934
		[FriendAccessAllowed]
		internal static bool s_asyncDebuggingEnabled;

		// Token: 0x04001B17 RID: 6935
		private static readonly Dictionary<int, Task> s_currentActiveTasks = new Dictionary<int, Task>();

		// Token: 0x04001B18 RID: 6936
		private static readonly object s_activeTasksLock = new object();

		// Token: 0x04001B19 RID: 6937
		internal volatile Task.ContingentProperties m_contingentProperties;

		// Token: 0x04001B1A RID: 6938
		private static readonly Action<object> s_taskCancelCallback = new Action<object>(Task.TaskCancelCallback);

		// Token: 0x04001B1B RID: 6939
		private static readonly Func<Task.ContingentProperties> s_createContingentProperties = () => new Task.ContingentProperties();

		// Token: 0x04001B1C RID: 6940
		private static Task s_completedTask;

		// Token: 0x04001B1D RID: 6941
		private static readonly Predicate<Task> s_IsExceptionObservedByParentPredicate = (Task t) => t.IsExceptionObservedByParent;

		// Token: 0x04001B1E RID: 6942
		[SecurityCritical]
		private static ContextCallback s_ecCallback;

		// Token: 0x04001B1F RID: 6943
		private static readonly Predicate<object> s_IsTaskContinuationNullPredicate = (object tc) => tc == null;

		// Token: 0x02000C13 RID: 3091
		internal class ContingentProperties
		{
			// Token: 0x06006FD4 RID: 28628 RVA: 0x00181D2C File Offset: 0x0017FF2C
			internal void SetCompleted()
			{
				ManualResetEventSlim completionEvent = this.m_completionEvent;
				if (completionEvent != null)
				{
					completionEvent.Set();
				}
			}

			// Token: 0x06006FD5 RID: 28629 RVA: 0x00181D4C File Offset: 0x0017FF4C
			internal void DeregisterCancellationCallback()
			{
				if (this.m_cancellationRegistration != null)
				{
					try
					{
						this.m_cancellationRegistration.Value.Dispose();
					}
					catch (ObjectDisposedException)
					{
					}
					this.m_cancellationRegistration = null;
				}
			}

			// Token: 0x040036B1 RID: 14001
			internal ExecutionContext m_capturedContext;

			// Token: 0x040036B2 RID: 14002
			internal volatile ManualResetEventSlim m_completionEvent;

			// Token: 0x040036B3 RID: 14003
			internal volatile TaskExceptionHolder m_exceptionsHolder;

			// Token: 0x040036B4 RID: 14004
			internal CancellationToken m_cancellationToken;

			// Token: 0x040036B5 RID: 14005
			internal Shared<CancellationTokenRegistration> m_cancellationRegistration;

			// Token: 0x040036B6 RID: 14006
			internal volatile int m_internalCancellationRequested;

			// Token: 0x040036B7 RID: 14007
			internal volatile int m_completionCountdown = 1;

			// Token: 0x040036B8 RID: 14008
			internal volatile List<Task> m_exceptionalChildren;
		}

		// Token: 0x02000C14 RID: 3092
		private sealed class SetOnInvokeMres : ManualResetEventSlim, ITaskCompletionAction
		{
			// Token: 0x06006FD7 RID: 28631 RVA: 0x00181DA1 File Offset: 0x0017FFA1
			internal SetOnInvokeMres()
				: base(false, 0)
			{
			}

			// Token: 0x06006FD8 RID: 28632 RVA: 0x00181DAB File Offset: 0x0017FFAB
			public void Invoke(Task completingTask)
			{
				base.Set();
			}
		}

		// Token: 0x02000C15 RID: 3093
		private sealed class SetOnCountdownMres : ManualResetEventSlim, ITaskCompletionAction
		{
			// Token: 0x06006FD9 RID: 28633 RVA: 0x00181DB3 File Offset: 0x0017FFB3
			internal SetOnCountdownMres(int count)
			{
				this._count = count;
			}

			// Token: 0x06006FDA RID: 28634 RVA: 0x00181DC2 File Offset: 0x0017FFC2
			public void Invoke(Task completingTask)
			{
				if (Interlocked.Decrement(ref this._count) == 0)
				{
					base.Set();
				}
			}

			// Token: 0x040036B9 RID: 14009
			private int _count;
		}

		// Token: 0x02000C16 RID: 3094
		private sealed class DelayPromise : Task<VoidTaskResult>
		{
			// Token: 0x06006FDB RID: 28635 RVA: 0x00181DD7 File Offset: 0x0017FFD7
			internal DelayPromise(CancellationToken token)
			{
				this.Token = token;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, base.Id, "Task.Delay", 0UL);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.AddToActiveTasks(this);
				}
			}

			// Token: 0x06006FDC RID: 28636 RVA: 0x00181E10 File Offset: 0x00180010
			internal void Complete()
			{
				bool flag;
				if (this.Token.IsCancellationRequested)
				{
					flag = base.TrySetCanceled(this.Token);
				}
				else
				{
					if (AsyncCausalityTracer.LoggingOn)
					{
						AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, base.Id, AsyncCausalityStatus.Completed);
					}
					if (Task.s_asyncDebuggingEnabled)
					{
						Task.RemoveFromActiveTasks(base.Id);
					}
					flag = base.TrySetResult(default(VoidTaskResult));
				}
				if (flag)
				{
					if (this.Timer != null)
					{
						this.Timer.Dispose();
					}
					this.Registration.Dispose();
				}
			}

			// Token: 0x040036BA RID: 14010
			internal readonly CancellationToken Token;

			// Token: 0x040036BB RID: 14011
			internal CancellationTokenRegistration Registration;

			// Token: 0x040036BC RID: 14012
			internal Timer Timer;
		}

		// Token: 0x02000C17 RID: 3095
		private sealed class WhenAllPromise : Task<VoidTaskResult>, ITaskCompletionAction
		{
			// Token: 0x06006FDD RID: 28637 RVA: 0x00181E94 File Offset: 0x00180094
			internal WhenAllPromise(Task[] tasks)
			{
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, base.Id, "Task.WhenAll", 0UL);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.AddToActiveTasks(this);
				}
				this.m_tasks = tasks;
				this.m_count = tasks.Length;
				foreach (Task task in tasks)
				{
					if (task.IsCompleted)
					{
						this.Invoke(task);
					}
					else
					{
						task.AddCompletionAction(this);
					}
				}
			}

			// Token: 0x06006FDE RID: 28638 RVA: 0x00181F0C File Offset: 0x0018010C
			public void Invoke(Task completedTask)
			{
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, base.Id, CausalityRelation.Join);
				}
				if (Interlocked.Decrement(ref this.m_count) == 0)
				{
					List<ExceptionDispatchInfo> list = null;
					Task task = null;
					for (int i = 0; i < this.m_tasks.Length; i++)
					{
						Task task2 = this.m_tasks[i];
						if (task2.IsFaulted)
						{
							if (list == null)
							{
								list = new List<ExceptionDispatchInfo>();
							}
							list.AddRange(task2.GetExceptionDispatchInfos());
						}
						else if (task2.IsCanceled && task == null)
						{
							task = task2;
						}
						if (task2.IsWaitNotificationEnabled)
						{
							base.SetNotificationForWaitCompletion(true);
						}
						else
						{
							this.m_tasks[i] = null;
						}
					}
					if (list != null)
					{
						base.TrySetException(list);
						return;
					}
					if (task != null)
					{
						base.TrySetCanceled(task.CancellationToken, task.GetCancellationExceptionDispatchInfo());
						return;
					}
					if (AsyncCausalityTracer.LoggingOn)
					{
						AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, base.Id, AsyncCausalityStatus.Completed);
					}
					if (Task.s_asyncDebuggingEnabled)
					{
						Task.RemoveFromActiveTasks(base.Id);
					}
					base.TrySetResult(default(VoidTaskResult));
				}
			}

			// Token: 0x17001328 RID: 4904
			// (get) Token: 0x06006FDF RID: 28639 RVA: 0x00181FFD File Offset: 0x001801FD
			internal override bool ShouldNotifyDebuggerOfWaitCompletion
			{
				get
				{
					return base.ShouldNotifyDebuggerOfWaitCompletion && Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(this.m_tasks);
				}
			}

			// Token: 0x040036BD RID: 14013
			private readonly Task[] m_tasks;

			// Token: 0x040036BE RID: 14014
			private int m_count;
		}

		// Token: 0x02000C18 RID: 3096
		private sealed class WhenAllPromise<T> : Task<T[]>, ITaskCompletionAction
		{
			// Token: 0x06006FE0 RID: 28640 RVA: 0x00182014 File Offset: 0x00180214
			internal WhenAllPromise(Task<T>[] tasks)
			{
				this.m_tasks = tasks;
				this.m_count = tasks.Length;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, base.Id, "Task.WhenAll", 0UL);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.AddToActiveTasks(this);
				}
				foreach (Task<T> task in tasks)
				{
					if (task.IsCompleted)
					{
						this.Invoke(task);
					}
					else
					{
						task.AddCompletionAction(this);
					}
				}
			}

			// Token: 0x06006FE1 RID: 28641 RVA: 0x0018208C File Offset: 0x0018028C
			public void Invoke(Task ignored)
			{
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, base.Id, CausalityRelation.Join);
				}
				if (Interlocked.Decrement(ref this.m_count) == 0)
				{
					T[] array = new T[this.m_tasks.Length];
					List<ExceptionDispatchInfo> list = null;
					Task task = null;
					for (int i = 0; i < this.m_tasks.Length; i++)
					{
						Task<T> task2 = this.m_tasks[i];
						if (task2.IsFaulted)
						{
							if (list == null)
							{
								list = new List<ExceptionDispatchInfo>();
							}
							list.AddRange(task2.GetExceptionDispatchInfos());
						}
						else if (task2.IsCanceled)
						{
							if (task == null)
							{
								task = task2;
							}
						}
						else
						{
							array[i] = task2.GetResultCore(false);
						}
						if (task2.IsWaitNotificationEnabled)
						{
							base.SetNotificationForWaitCompletion(true);
						}
						else
						{
							this.m_tasks[i] = null;
						}
					}
					if (list != null)
					{
						base.TrySetException(list);
						return;
					}
					if (task != null)
					{
						base.TrySetCanceled(task.CancellationToken, task.GetCancellationExceptionDispatchInfo());
						return;
					}
					if (AsyncCausalityTracer.LoggingOn)
					{
						AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, base.Id, AsyncCausalityStatus.Completed);
					}
					if (Task.s_asyncDebuggingEnabled)
					{
						Task.RemoveFromActiveTasks(base.Id);
					}
					base.TrySetResult(array);
				}
			}

			// Token: 0x17001329 RID: 4905
			// (get) Token: 0x06006FE2 RID: 28642 RVA: 0x0018219C File Offset: 0x0018039C
			internal override bool ShouldNotifyDebuggerOfWaitCompletion
			{
				get
				{
					if (base.ShouldNotifyDebuggerOfWaitCompletion)
					{
						Task[] tasks = this.m_tasks;
						return Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(tasks);
					}
					return false;
				}
			}

			// Token: 0x040036BF RID: 14015
			private readonly Task<T>[] m_tasks;

			// Token: 0x040036C0 RID: 14016
			private int m_count;
		}
	}
}
