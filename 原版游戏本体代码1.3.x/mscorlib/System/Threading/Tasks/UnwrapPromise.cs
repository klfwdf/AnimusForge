using System;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Security;

namespace System.Threading.Tasks
{
	// Token: 0x02000569 RID: 1385
	internal sealed class UnwrapPromise<TResult> : Task<TResult>, ITaskCompletionAction
	{
		// Token: 0x0600415F RID: 16735 RVA: 0x000F4048 File Offset: 0x000F2248
		public UnwrapPromise(Task outerTask, bool lookForOce)
			: base(null, outerTask.CreationOptions & TaskCreationOptions.AttachedToParent)
		{
			this._lookForOce = lookForOce;
			this._state = 0;
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, base.Id, "Task.Unwrap", 0UL);
			}
			if (Task.s_asyncDebuggingEnabled)
			{
				Task.AddToActiveTasks(this);
			}
			if (outerTask.IsCompleted)
			{
				this.ProcessCompletedOuterTask(outerTask);
				return;
			}
			outerTask.AddCompletionAction(this);
		}

		// Token: 0x06004160 RID: 16736 RVA: 0x000F40B4 File Offset: 0x000F22B4
		public void Invoke(Task completingTask)
		{
			StackGuard currentStackGuard = Task.CurrentStackGuard;
			if (currentStackGuard.TryBeginInliningScope())
			{
				try
				{
					this.InvokeCore(completingTask);
					return;
				}
				finally
				{
					currentStackGuard.EndInliningScope();
				}
			}
			this.InvokeCoreAsync(completingTask);
		}

		// Token: 0x06004161 RID: 16737 RVA: 0x000F40F8 File Offset: 0x000F22F8
		private void InvokeCore(Task completingTask)
		{
			byte state = this._state;
			if (state == 0)
			{
				this.ProcessCompletedOuterTask(completingTask);
				return;
			}
			if (state != 1)
			{
				return;
			}
			bool flag = this.TrySetFromTask(completingTask, false);
			this._state = 2;
		}

		// Token: 0x06004162 RID: 16738 RVA: 0x000F412C File Offset: 0x000F232C
		[SecuritySafeCritical]
		private void InvokeCoreAsync(Task completingTask)
		{
			ThreadPool.UnsafeQueueUserWorkItem(delegate(object state)
			{
				Tuple<UnwrapPromise<TResult>, Task> tuple = (Tuple<UnwrapPromise<TResult>, Task>)state;
				tuple.Item1.InvokeCore(tuple.Item2);
			}, Tuple.Create<UnwrapPromise<TResult>, Task>(this, completingTask));
		}

		// Token: 0x06004163 RID: 16739 RVA: 0x000F415C File Offset: 0x000F235C
		private void ProcessCompletedOuterTask(Task task)
		{
			this._state = 1;
			TaskStatus status = task.Status;
			if (status != TaskStatus.RanToCompletion)
			{
				if (status - TaskStatus.Canceled <= 1)
				{
					bool flag = this.TrySetFromTask(task, this._lookForOce);
					return;
				}
			}
			else
			{
				Task<Task<TResult>> task2 = task as Task<Task<TResult>>;
				this.ProcessInnerTask((task2 != null) ? task2.Result : ((Task<Task>)task).Result);
			}
		}

		// Token: 0x06004164 RID: 16740 RVA: 0x000F41B4 File Offset: 0x000F23B4
		private bool TrySetFromTask(Task task, bool lookForOce)
		{
			if (AsyncCausalityTracer.LoggingOn)
			{
				AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, base.Id, CausalityRelation.Join);
			}
			bool result = false;
			switch (task.Status)
			{
			case TaskStatus.RanToCompletion:
			{
				Task<TResult> task2 = task as Task<TResult>;
				if (AsyncCausalityTracer.LoggingOn)
				{
					AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, base.Id, AsyncCausalityStatus.Completed);
				}
				if (Task.s_asyncDebuggingEnabled)
				{
					Task.RemoveFromActiveTasks(base.Id);
				}
				result = base.TrySetResult((task2 != null) ? task2.Result : default(TResult));
				break;
			}
			case TaskStatus.Canceled:
				result = base.TrySetCanceled(task.CancellationToken, task.GetCancellationExceptionDispatchInfo());
				break;
			case TaskStatus.Faulted:
			{
				ReadOnlyCollection<ExceptionDispatchInfo> exceptionDispatchInfos = task.GetExceptionDispatchInfos();
				ExceptionDispatchInfo exceptionDispatchInfo;
				OperationCanceledException ex;
				if (lookForOce && exceptionDispatchInfos.Count > 0 && (exceptionDispatchInfo = exceptionDispatchInfos[0]) != null && (ex = exceptionDispatchInfo.SourceException as OperationCanceledException) != null)
				{
					result = base.TrySetCanceled(ex.CancellationToken, exceptionDispatchInfo);
				}
				else
				{
					result = base.TrySetException(exceptionDispatchInfos);
				}
				break;
			}
			}
			return result;
		}

		// Token: 0x06004165 RID: 16741 RVA: 0x000F42A8 File Offset: 0x000F24A8
		private void ProcessInnerTask(Task task)
		{
			if (task == null)
			{
				base.TrySetCanceled(default(CancellationToken));
				this._state = 2;
				return;
			}
			if (task.IsCompleted)
			{
				this.TrySetFromTask(task, false);
				this._state = 2;
				return;
			}
			task.AddCompletionAction(this);
		}

		// Token: 0x04001B4E RID: 6990
		private const byte STATE_WAITING_ON_OUTER_TASK = 0;

		// Token: 0x04001B4F RID: 6991
		private const byte STATE_WAITING_ON_INNER_TASK = 1;

		// Token: 0x04001B50 RID: 6992
		private const byte STATE_DONE = 2;

		// Token: 0x04001B51 RID: 6993
		private byte _state;

		// Token: 0x04001B52 RID: 6994
		private readonly bool _lookForOce;
	}
}
