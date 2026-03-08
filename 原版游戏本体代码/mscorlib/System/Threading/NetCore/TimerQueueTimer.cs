using System;
using System.Diagnostics.Tracing;
using System.Security;
using Microsoft.Win32;

namespace System.Threading.NetCore
{
	// Token: 0x0200058B RID: 1419
	internal sealed class TimerQueueTimer : IThreadPoolWorkItem
	{
		// Token: 0x060042B8 RID: 17080 RVA: 0x000F8E14 File Offset: 0x000F7014
		[SecuritySafeCritical]
		internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, bool flowExecutionContext, ref StackCrawlMark stackMark)
		{
			this.m_timerCallback = timerCallback;
			this.m_state = state;
			this.m_dueTime = uint.MaxValue;
			this.m_period = uint.MaxValue;
			if (flowExecutionContext)
			{
				this.m_executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
			}
			this.m_associatedTimerQueue = TimerQueue.Instances[Thread.GetCurrentProcessorId() % TimerQueue.Instances.Length];
			if (dueTime != 4294967295U)
			{
				this.Change(dueTime, period);
			}
		}

		// Token: 0x060042B9 RID: 17081 RVA: 0x000F8E7C File Offset: 0x000F707C
		internal bool Change(uint dueTime, uint period)
		{
			TimerQueue associatedTimerQueue = this.m_associatedTimerQueue;
			bool result;
			lock (associatedTimerQueue)
			{
				if (this.m_canceled)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
				}
				try
				{
				}
				finally
				{
					this.m_period = period;
					if (dueTime == 4294967295U)
					{
						this.m_associatedTimerQueue.DeleteTimer(this);
						result = true;
					}
					else
					{
						if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
						{
							FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true);
						}
						result = this.m_associatedTimerQueue.UpdateTimer(this, dueTime, period);
					}
				}
			}
			return result;
		}

		// Token: 0x060042BA RID: 17082 RVA: 0x000F8F34 File Offset: 0x000F7134
		public void Close()
		{
			TimerQueue associatedTimerQueue = this.m_associatedTimerQueue;
			lock (associatedTimerQueue)
			{
				try
				{
				}
				finally
				{
					if (!this.m_canceled)
					{
						this.m_canceled = true;
						this.m_associatedTimerQueue.DeleteTimer(this);
					}
				}
			}
		}

		// Token: 0x060042BB RID: 17083 RVA: 0x000F8F9C File Offset: 0x000F719C
		public bool Close(WaitHandle toSignal)
		{
			bool flag = false;
			TimerQueue associatedTimerQueue = this.m_associatedTimerQueue;
			bool result;
			lock (associatedTimerQueue)
			{
				try
				{
				}
				finally
				{
					if (this.m_canceled)
					{
						result = false;
					}
					else
					{
						this.m_canceled = true;
						this.m_notifyWhenNoCallbacksRunning = toSignal;
						this.m_associatedTimerQueue.DeleteTimer(this);
						flag = this.m_callbacksRunning == 0;
						result = true;
					}
				}
			}
			if (flag)
			{
				this.SignalNoCallbacksRunning();
			}
			return result;
		}

		// Token: 0x060042BC RID: 17084 RVA: 0x000F9028 File Offset: 0x000F7228
		[SecurityCritical]
		void IThreadPoolWorkItem.ExecuteWorkItem()
		{
			this.Fire();
		}

		// Token: 0x060042BD RID: 17085 RVA: 0x000F9030 File Offset: 0x000F7230
		[SecurityCritical]
		void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
		{
		}

		// Token: 0x060042BE RID: 17086 RVA: 0x000F9034 File Offset: 0x000F7234
		internal void Fire()
		{
			bool flag = false;
			TimerQueue associatedTimerQueue = this.m_associatedTimerQueue;
			lock (associatedTimerQueue)
			{
				try
				{
				}
				finally
				{
					flag = this.m_canceled;
					if (!flag)
					{
						this.m_callbacksRunning++;
					}
				}
			}
			if (flag)
			{
				return;
			}
			this.CallCallback();
			bool flag3 = false;
			TimerQueue associatedTimerQueue2 = this.m_associatedTimerQueue;
			lock (associatedTimerQueue2)
			{
				try
				{
				}
				finally
				{
					this.m_callbacksRunning--;
					if (this.m_canceled && this.m_callbacksRunning == 0 && this.m_notifyWhenNoCallbacksRunning != null)
					{
						flag3 = true;
					}
				}
			}
			if (flag3)
			{
				this.SignalNoCallbacksRunning();
			}
		}

		// Token: 0x060042BF RID: 17087 RVA: 0x000F9114 File Offset: 0x000F7314
		[SecuritySafeCritical]
		internal void SignalNoCallbacksRunning()
		{
			Win32Native.SetEvent(this.m_notifyWhenNoCallbacksRunning.SafeWaitHandle);
		}

		// Token: 0x060042C0 RID: 17088 RVA: 0x000F912C File Offset: 0x000F732C
		[SecuritySafeCritical]
		internal void CallCallback()
		{
			if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
			{
				FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);
			}
			ExecutionContext executionContext = this.m_executionContext;
			if (executionContext == null)
			{
				this.m_timerCallback(this.m_state);
				return;
			}
			ExecutionContext executionContext2;
			executionContext = (executionContext2 = (executionContext.IsPreAllocatedDefault ? executionContext : executionContext.CreateCopy()));
			try
			{
				if (TimerQueueTimer.s_callCallbackInContext == null)
				{
					TimerQueueTimer.s_callCallbackInContext = new ContextCallback(TimerQueueTimer.CallCallbackInContext);
				}
				ExecutionContext.Run(executionContext, TimerQueueTimer.s_callCallbackInContext, this, true);
			}
			finally
			{
				if (executionContext2 != null)
				{
					((IDisposable)executionContext2).Dispose();
				}
			}
		}

		// Token: 0x060042C1 RID: 17089 RVA: 0x000F91D8 File Offset: 0x000F73D8
		[SecurityCritical]
		private static void CallCallbackInContext(object state)
		{
			TimerQueueTimer timerQueueTimer = (TimerQueueTimer)state;
			timerQueueTimer.m_timerCallback(timerQueueTimer.m_state);
		}

		// Token: 0x04001BC9 RID: 7113
		private readonly TimerQueue m_associatedTimerQueue;

		// Token: 0x04001BCA RID: 7114
		internal TimerQueueTimer m_next;

		// Token: 0x04001BCB RID: 7115
		internal TimerQueueTimer m_prev;

		// Token: 0x04001BCC RID: 7116
		internal bool m_short;

		// Token: 0x04001BCD RID: 7117
		internal long m_startTicks;

		// Token: 0x04001BCE RID: 7118
		internal uint m_dueTime;

		// Token: 0x04001BCF RID: 7119
		internal uint m_period;

		// Token: 0x04001BD0 RID: 7120
		private readonly TimerCallback m_timerCallback;

		// Token: 0x04001BD1 RID: 7121
		private readonly object m_state;

		// Token: 0x04001BD2 RID: 7122
		private readonly ExecutionContext m_executionContext;

		// Token: 0x04001BD3 RID: 7123
		private int m_callbacksRunning;

		// Token: 0x04001BD4 RID: 7124
		private volatile bool m_canceled;

		// Token: 0x04001BD5 RID: 7125
		private volatile WaitHandle m_notifyWhenNoCallbacksRunning;

		// Token: 0x04001BD6 RID: 7126
		[SecurityCritical]
		private static ContextCallback s_callCallbackInContext;
	}
}
