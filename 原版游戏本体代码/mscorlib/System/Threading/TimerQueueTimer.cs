using System;
using System.Diagnostics.Tracing;
using System.Security;
using Microsoft.Win32;

namespace System.Threading
{
	// Token: 0x0200052F RID: 1327
	internal sealed class TimerQueueTimer
	{
		// Token: 0x06003E45 RID: 15941 RVA: 0x000E8214 File Offset: 0x000E6414
		[SecurityCritical]
		internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, ref StackCrawlMark stackMark)
		{
			this.m_timerCallback = timerCallback;
			this.m_state = state;
			this.m_dueTime = uint.MaxValue;
			this.m_period = uint.MaxValue;
			if (!ExecutionContext.IsFlowSuppressed())
			{
				this.m_executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
			}
			if (dueTime != 4294967295U)
			{
				this.Change(dueTime, period);
			}
		}

		// Token: 0x06003E46 RID: 15942 RVA: 0x000E8268 File Offset: 0x000E6468
		internal bool Change(uint dueTime, uint period)
		{
			TimerQueue instance = TimerQueue.Instance;
			bool result;
			lock (instance)
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
						TimerQueue.Instance.DeleteTimer(this);
						result = true;
					}
					else
					{
						if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
						{
							FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true);
						}
						result = TimerQueue.Instance.UpdateTimer(this, dueTime, period);
					}
				}
			}
			return result;
		}

		// Token: 0x06003E47 RID: 15943 RVA: 0x000E831C File Offset: 0x000E651C
		public void Close()
		{
			TimerQueue instance = TimerQueue.Instance;
			lock (instance)
			{
				try
				{
				}
				finally
				{
					if (!this.m_canceled)
					{
						this.m_canceled = true;
						TimerQueue.Instance.DeleteTimer(this);
					}
				}
			}
		}

		// Token: 0x06003E48 RID: 15944 RVA: 0x000E8380 File Offset: 0x000E6580
		public bool Close(WaitHandle toSignal)
		{
			bool flag = false;
			TimerQueue instance = TimerQueue.Instance;
			bool result;
			lock (instance)
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
						TimerQueue.Instance.DeleteTimer(this);
						if (this.m_callbacksRunning == 0)
						{
							flag = true;
						}
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

		// Token: 0x06003E49 RID: 15945 RVA: 0x000E840C File Offset: 0x000E660C
		internal void Fire()
		{
			bool flag = false;
			TimerQueue instance = TimerQueue.Instance;
			lock (instance)
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
			TimerQueue instance2 = TimerQueue.Instance;
			lock (instance2)
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

		// Token: 0x06003E4A RID: 15946 RVA: 0x000E84EC File Offset: 0x000E66EC
		[SecuritySafeCritical]
		internal void SignalNoCallbacksRunning()
		{
			Win32Native.SetEvent(this.m_notifyWhenNoCallbacksRunning.SafeWaitHandle);
		}

		// Token: 0x06003E4B RID: 15947 RVA: 0x000E8504 File Offset: 0x000E6704
		[SecuritySafeCritical]
		internal void CallCallback()
		{
			if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, (EventKeywords)16L))
			{
				FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);
			}
			if (this.m_executionContext == null)
			{
				this.m_timerCallback(this.m_state);
				return;
			}
			using (ExecutionContext executionContext = (this.m_executionContext.IsPreAllocatedDefault ? this.m_executionContext : this.m_executionContext.CreateCopy()))
			{
				ContextCallback contextCallback = TimerQueueTimer.s_callCallbackInContext;
				if (contextCallback == null)
				{
					contextCallback = (TimerQueueTimer.s_callCallbackInContext = new ContextCallback(TimerQueueTimer.CallCallbackInContext));
				}
				ExecutionContext.Run(executionContext, contextCallback, this, true);
			}
		}

		// Token: 0x06003E4C RID: 15948 RVA: 0x000E85B8 File Offset: 0x000E67B8
		[SecurityCritical]
		private static void CallCallbackInContext(object state)
		{
			TimerQueueTimer timerQueueTimer = (TimerQueueTimer)state;
			timerQueueTimer.m_timerCallback(timerQueueTimer.m_state);
		}

		// Token: 0x04001A3A RID: 6714
		internal TimerQueueTimer m_next;

		// Token: 0x04001A3B RID: 6715
		internal TimerQueueTimer m_prev;

		// Token: 0x04001A3C RID: 6716
		internal int m_startTicks;

		// Token: 0x04001A3D RID: 6717
		internal uint m_dueTime;

		// Token: 0x04001A3E RID: 6718
		internal uint m_period;

		// Token: 0x04001A3F RID: 6719
		private readonly TimerCallback m_timerCallback;

		// Token: 0x04001A40 RID: 6720
		private readonly object m_state;

		// Token: 0x04001A41 RID: 6721
		private readonly ExecutionContext m_executionContext;

		// Token: 0x04001A42 RID: 6722
		private int m_callbacksRunning;

		// Token: 0x04001A43 RID: 6723
		private volatile bool m_canceled;

		// Token: 0x04001A44 RID: 6724
		private volatile WaitHandle m_notifyWhenNoCallbacksRunning;

		// Token: 0x04001A45 RID: 6725
		[SecurityCritical]
		private static ContextCallback s_callCallbackInContext;
	}
}
