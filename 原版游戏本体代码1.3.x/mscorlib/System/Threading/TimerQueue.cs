using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.NetCore;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
	// Token: 0x0200052E RID: 1326
	internal class TimerQueue
	{
		// Token: 0x17000939 RID: 2361
		// (get) Token: 0x06003E35 RID: 15925 RVA: 0x000E7D32 File Offset: 0x000E5F32
		public static TimerQueue Instance
		{
			get
			{
				return TimerQueue.s_queue;
			}
		}

		// Token: 0x06003E36 RID: 15926 RVA: 0x000E7D39 File Offset: 0x000E5F39
		private TimerQueue()
		{
		}

		// Token: 0x1700093A RID: 2362
		// (get) Token: 0x06003E37 RID: 15927 RVA: 0x000E7D44 File Offset: 0x000E5F44
		private static int TickCount
		{
			[SecuritySafeCritical]
			get
			{
				if (!Environment.IsWindows8OrAbove)
				{
					return Environment.TickCount;
				}
				ulong num;
				if (!Win32Native.QueryUnbiasedInterruptTime(out num))
				{
					throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
				}
				return (int)((uint)(num / 10000UL));
			}
		}

		// Token: 0x06003E38 RID: 15928 RVA: 0x000E7D80 File Offset: 0x000E5F80
		[SecuritySafeCritical]
		private bool EnsureAppDomainTimerFiresBy(uint requestedDuration)
		{
			uint num = Math.Min(requestedDuration, 268435455U);
			if (this.m_isAppDomainTimerScheduled)
			{
				uint num2 = (uint)(TimerQueue.TickCount - this.m_currentAppDomainTimerStartTicks);
				if (num2 >= this.m_currentAppDomainTimerDuration)
				{
					return true;
				}
				uint num3 = this.m_currentAppDomainTimerDuration - num2;
				if (num >= num3)
				{
					return true;
				}
			}
			if (this.m_pauseTicks != 0)
			{
				return true;
			}
			if (this.m_appDomainTimer == null || this.m_appDomainTimer.IsInvalid)
			{
				this.m_appDomainTimer = TimerQueue.CreateAppDomainTimer(num, 0);
				if (!this.m_appDomainTimer.IsInvalid)
				{
					this.m_isAppDomainTimerScheduled = true;
					this.m_currentAppDomainTimerStartTicks = TimerQueue.TickCount;
					this.m_currentAppDomainTimerDuration = num;
					return true;
				}
				return false;
			}
			else
			{
				if (TimerQueue.ChangeAppDomainTimer(this.m_appDomainTimer, num))
				{
					this.m_isAppDomainTimerScheduled = true;
					this.m_currentAppDomainTimerStartTicks = TimerQueue.TickCount;
					this.m_currentAppDomainTimerDuration = num;
					return true;
				}
				return false;
			}
		}

		// Token: 0x06003E39 RID: 15929 RVA: 0x000E7E4A File Offset: 0x000E604A
		[SecuritySafeCritical]
		internal static void AppDomainTimerCallback(int id)
		{
			if (Timer.UseNetCoreTimer)
			{
				TimerQueue.AppDomainTimerCallback(id);
				return;
			}
			TimerQueue.Instance.FireNextTimers();
		}

		// Token: 0x06003E3A RID: 15930
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern TimerQueue.AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime, int id);

		// Token: 0x06003E3B RID: 15931
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool ChangeAppDomainTimer(TimerQueue.AppDomainTimerSafeHandle handle, uint dueTime);

		// Token: 0x06003E3C RID: 15932
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool DeleteAppDomainTimer(IntPtr handle);

		// Token: 0x06003E3D RID: 15933 RVA: 0x000E7E64 File Offset: 0x000E6064
		[SecurityCritical]
		internal void Pause()
		{
			lock (this)
			{
				if (this.m_appDomainTimer != null && !this.m_appDomainTimer.IsInvalid)
				{
					this.m_appDomainTimer.Dispose();
					this.m_appDomainTimer = null;
					this.m_isAppDomainTimerScheduled = false;
					this.m_pauseTicks = TimerQueue.TickCount;
				}
			}
		}

		// Token: 0x06003E3E RID: 15934 RVA: 0x000E7ED4 File Offset: 0x000E60D4
		[SecurityCritical]
		internal void Resume()
		{
			lock (this)
			{
				try
				{
				}
				finally
				{
					int pauseTicks = this.m_pauseTicks;
					this.m_pauseTicks = 0;
					int tickCount = TimerQueue.TickCount;
					int num = tickCount - pauseTicks;
					bool flag2 = false;
					uint num2 = uint.MaxValue;
					for (TimerQueueTimer timerQueueTimer = this.m_timers; timerQueueTimer != null; timerQueueTimer = timerQueueTimer.m_next)
					{
						uint num3;
						if (timerQueueTimer.m_startTicks <= pauseTicks)
						{
							num3 = (uint)(pauseTicks - timerQueueTimer.m_startTicks);
						}
						else
						{
							num3 = (uint)(tickCount - timerQueueTimer.m_startTicks);
						}
						timerQueueTimer.m_dueTime = ((timerQueueTimer.m_dueTime > num3) ? (timerQueueTimer.m_dueTime - num3) : 0U);
						timerQueueTimer.m_startTicks = tickCount;
						if (timerQueueTimer.m_dueTime < num2)
						{
							flag2 = true;
							num2 = timerQueueTimer.m_dueTime;
						}
					}
					if (flag2)
					{
						this.EnsureAppDomainTimerFiresBy(num2);
					}
				}
			}
		}

		// Token: 0x06003E3F RID: 15935 RVA: 0x000E7FC0 File Offset: 0x000E61C0
		private void FireNextTimers()
		{
			TimerQueueTimer timerQueueTimer = null;
			lock (this)
			{
				try
				{
				}
				finally
				{
					this.m_isAppDomainTimerScheduled = false;
					bool flag2 = false;
					uint num = uint.MaxValue;
					int tickCount = TimerQueue.TickCount;
					TimerQueueTimer timerQueueTimer2 = this.m_timers;
					while (timerQueueTimer2 != null)
					{
						uint num2 = (uint)(tickCount - timerQueueTimer2.m_startTicks);
						if (num2 >= timerQueueTimer2.m_dueTime)
						{
							TimerQueueTimer next = timerQueueTimer2.m_next;
							if (timerQueueTimer2.m_period != 4294967295U)
							{
								timerQueueTimer2.m_startTicks = tickCount;
								timerQueueTimer2.m_dueTime = timerQueueTimer2.m_period;
								if (timerQueueTimer2.m_dueTime < num)
								{
									flag2 = true;
									num = timerQueueTimer2.m_dueTime;
								}
							}
							else
							{
								this.DeleteTimer(timerQueueTimer2);
							}
							if (timerQueueTimer == null)
							{
								timerQueueTimer = timerQueueTimer2;
							}
							else
							{
								TimerQueue.QueueTimerCompletion(timerQueueTimer2);
							}
							timerQueueTimer2 = next;
						}
						else
						{
							uint num3 = timerQueueTimer2.m_dueTime - num2;
							if (num3 < num)
							{
								flag2 = true;
								num = num3;
							}
							timerQueueTimer2 = timerQueueTimer2.m_next;
						}
					}
					if (flag2)
					{
						this.EnsureAppDomainTimerFiresBy(num);
					}
				}
			}
			if (timerQueueTimer != null)
			{
				timerQueueTimer.Fire();
			}
		}

		// Token: 0x06003E40 RID: 15936 RVA: 0x000E80DC File Offset: 0x000E62DC
		[SecuritySafeCritical]
		private static void QueueTimerCompletion(TimerQueueTimer timer)
		{
			WaitCallback waitCallback = TimerQueue.s_fireQueuedTimerCompletion;
			if (waitCallback == null)
			{
				waitCallback = (TimerQueue.s_fireQueuedTimerCompletion = new WaitCallback(TimerQueue.FireQueuedTimerCompletion));
			}
			ThreadPool.UnsafeQueueUserWorkItem(waitCallback, timer);
		}

		// Token: 0x06003E41 RID: 15937 RVA: 0x000E810D File Offset: 0x000E630D
		private static void FireQueuedTimerCompletion(object state)
		{
			((TimerQueueTimer)state).Fire();
		}

		// Token: 0x06003E42 RID: 15938 RVA: 0x000E811C File Offset: 0x000E631C
		public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
		{
			if (timer.m_dueTime == 4294967295U)
			{
				timer.m_next = this.m_timers;
				timer.m_prev = null;
				if (timer.m_next != null)
				{
					timer.m_next.m_prev = timer;
				}
				this.m_timers = timer;
			}
			timer.m_dueTime = dueTime;
			timer.m_period = ((period == 0U) ? uint.MaxValue : period);
			timer.m_startTicks = TimerQueue.TickCount;
			return this.EnsureAppDomainTimerFiresBy(dueTime);
		}

		// Token: 0x06003E43 RID: 15939 RVA: 0x000E8188 File Offset: 0x000E6388
		public void DeleteTimer(TimerQueueTimer timer)
		{
			if (timer.m_dueTime != 4294967295U)
			{
				if (timer.m_next != null)
				{
					timer.m_next.m_prev = timer.m_prev;
				}
				if (timer.m_prev != null)
				{
					timer.m_prev.m_next = timer.m_next;
				}
				if (this.m_timers == timer)
				{
					this.m_timers = timer.m_next;
				}
				timer.m_dueTime = uint.MaxValue;
				timer.m_period = uint.MaxValue;
				timer.m_startTicks = 0;
				timer.m_prev = null;
				timer.m_next = null;
			}
		}

		// Token: 0x04001A32 RID: 6706
		private static TimerQueue s_queue = new TimerQueue();

		// Token: 0x04001A33 RID: 6707
		[SecurityCritical]
		private TimerQueue.AppDomainTimerSafeHandle m_appDomainTimer;

		// Token: 0x04001A34 RID: 6708
		private bool m_isAppDomainTimerScheduled;

		// Token: 0x04001A35 RID: 6709
		private int m_currentAppDomainTimerStartTicks;

		// Token: 0x04001A36 RID: 6710
		private uint m_currentAppDomainTimerDuration;

		// Token: 0x04001A37 RID: 6711
		private TimerQueueTimer m_timers;

		// Token: 0x04001A38 RID: 6712
		private volatile int m_pauseTicks;

		// Token: 0x04001A39 RID: 6713
		private static WaitCallback s_fireQueuedTimerCompletion;

		// Token: 0x02000BF9 RID: 3065
		[SecurityCritical]
		internal class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			// Token: 0x06006F84 RID: 28548 RVA: 0x001807B7 File Offset: 0x0017E9B7
			public AppDomainTimerSafeHandle()
				: base(true)
			{
			}

			// Token: 0x06006F85 RID: 28549 RVA: 0x001807C0 File Offset: 0x0017E9C0
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			protected override bool ReleaseHandle()
			{
				return TimerQueue.DeleteAppDomainTimer(this.handle);
			}
		}
	}
}
