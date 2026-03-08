using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000537 RID: 1335
	[ComVisible(false)]
	[DebuggerTypeProxy(typeof(SpinLock.SystemThreading_SpinLockDebugView))]
	[DebuggerDisplay("IsHeld = {IsHeld}")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public struct SpinLock
	{
		// Token: 0x06003EAC RID: 16044 RVA: 0x000E937D File Offset: 0x000E757D
		[__DynamicallyInvokable]
		public SpinLock(bool enableThreadOwnerTracking)
		{
			this.m_owner = 0;
			if (!enableThreadOwnerTracking)
			{
				this.m_owner |= int.MinValue;
			}
		}

		// Token: 0x06003EAD RID: 16045 RVA: 0x000E93A4 File Offset: 0x000E75A4
		[__DynamicallyInvokable]
		public void Enter(ref bool lockTaken)
		{
			Thread.BeginCriticalRegion();
			int owner = this.m_owner;
			if (lockTaken || (owner & -2147483647) != -2147483648 || Interlocked.CompareExchange(ref this.m_owner, owner | 1, owner, ref lockTaken) != owner)
			{
				this.ContinueTryEnter(-1, ref lockTaken);
			}
		}

		// Token: 0x06003EAE RID: 16046 RVA: 0x000E93EC File Offset: 0x000E75EC
		[__DynamicallyInvokable]
		public void TryEnter(ref bool lockTaken)
		{
			this.TryEnter(0, ref lockTaken);
		}

		// Token: 0x06003EAF RID: 16047 RVA: 0x000E93F8 File Offset: 0x000E75F8
		[__DynamicallyInvokable]
		public void TryEnter(TimeSpan timeout, ref bool lockTaken)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", timeout, Environment.GetResourceString("SpinLock_TryEnter_ArgumentOutOfRange"));
			}
			this.TryEnter((int)timeout.TotalMilliseconds, ref lockTaken);
		}

		// Token: 0x06003EB0 RID: 16048 RVA: 0x000E9448 File Offset: 0x000E7648
		[__DynamicallyInvokable]
		public void TryEnter(int millisecondsTimeout, ref bool lockTaken)
		{
			Thread.BeginCriticalRegion();
			int owner = this.m_owner;
			if (((millisecondsTimeout < -1) | lockTaken) || (owner & -2147483647) != -2147483648 || Interlocked.CompareExchange(ref this.m_owner, owner | 1, owner, ref lockTaken) != owner)
			{
				this.ContinueTryEnter(millisecondsTimeout, ref lockTaken);
			}
		}

		// Token: 0x06003EB1 RID: 16049 RVA: 0x000E9498 File Offset: 0x000E7698
		private void ContinueTryEnter(int millisecondsTimeout, ref bool lockTaken)
		{
			Thread.EndCriticalRegion();
			if (lockTaken)
			{
				lockTaken = false;
				throw new ArgumentException(Environment.GetResourceString("SpinLock_TryReliableEnter_ArgumentException"));
			}
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, Environment.GetResourceString("SpinLock_TryEnter_ArgumentOutOfRange"));
			}
			uint startTime = 0U;
			if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
			{
				startTime = TimeoutHelper.GetTime();
			}
			if (CdsSyncEtwBCLProvider.Log.IsEnabled())
			{
				CdsSyncEtwBCLProvider.Log.SpinLock_FastPathFailed(this.m_owner);
			}
			if (this.IsThreadOwnerTrackingEnabled)
			{
				this.ContinueTryEnterWithThreadTracking(millisecondsTimeout, startTime, ref lockTaken);
				return;
			}
			int num = int.MaxValue;
			int owner = this.m_owner;
			if ((owner & 1) == 0)
			{
				Thread.BeginCriticalRegion();
				if (Interlocked.CompareExchange(ref this.m_owner, owner | 1, owner, ref lockTaken) == owner)
				{
					return;
				}
				Thread.EndCriticalRegion();
			}
			else if ((owner & 2147483646) != SpinLock.MAXIMUM_WAITERS)
			{
				num = (Interlocked.Add(ref this.m_owner, 2) & 2147483646) >> 1;
			}
			if (millisecondsTimeout == 0 || (millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
			{
				this.DecrementWaiters();
				return;
			}
			int processorCount = PlatformHelper.ProcessorCount;
			if (num < processorCount)
			{
				int num2 = 1;
				for (int i = 1; i <= num * 100; i++)
				{
					Thread.SpinWait((num + i) * 100 * num2);
					if (num2 < processorCount)
					{
						num2++;
					}
					owner = this.m_owner;
					if ((owner & 1) == 0)
					{
						Thread.BeginCriticalRegion();
						int value = (((owner & 2147483646) == 0) ? (owner | 1) : ((owner - 2) | 1));
						if (Interlocked.CompareExchange(ref this.m_owner, value, owner, ref lockTaken) == owner)
						{
							return;
						}
						Thread.EndCriticalRegion();
					}
				}
			}
			if (millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
			{
				this.DecrementWaiters();
				return;
			}
			int num3 = 0;
			for (;;)
			{
				owner = this.m_owner;
				if ((owner & 1) == 0)
				{
					Thread.BeginCriticalRegion();
					int value2 = (((owner & 2147483646) == 0) ? (owner | 1) : ((owner - 2) | 1));
					if (Interlocked.CompareExchange(ref this.m_owner, value2, owner, ref lockTaken) == owner)
					{
						break;
					}
					Thread.EndCriticalRegion();
				}
				if (num3 % 40 == 0)
				{
					Thread.Sleep(1);
				}
				else if (num3 % 10 == 0)
				{
					Thread.Sleep(0);
				}
				else
				{
					Thread.Yield();
				}
				if (num3 % 10 == 0 && millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
				{
					goto Block_26;
				}
				num3++;
			}
			return;
			Block_26:
			this.DecrementWaiters();
		}

		// Token: 0x06003EB2 RID: 16050 RVA: 0x000E96AC File Offset: 0x000E78AC
		private void DecrementWaiters()
		{
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				int owner = this.m_owner;
				if ((owner & 2147483646) == 0)
				{
					break;
				}
				if (Interlocked.CompareExchange(ref this.m_owner, owner - 2, owner) == owner)
				{
					return;
				}
				spinWait.SpinOnce();
			}
		}

		// Token: 0x06003EB3 RID: 16051 RVA: 0x000E96F0 File Offset: 0x000E78F0
		private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, uint startTime, ref bool lockTaken)
		{
			int num = 0;
			int managedThreadId = Thread.CurrentThread.ManagedThreadId;
			if (this.m_owner == managedThreadId)
			{
				throw new LockRecursionException(Environment.GetResourceString("SpinLock_TryEnter_LockRecursionException"));
			}
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				spinWait.SpinOnce();
				if (this.m_owner == num)
				{
					Thread.BeginCriticalRegion();
					if (Interlocked.CompareExchange(ref this.m_owner, managedThreadId, num, ref lockTaken) == num)
					{
						break;
					}
					Thread.EndCriticalRegion();
				}
				if (millisecondsTimeout == 0 || (millisecondsTimeout != -1 && spinWait.NextSpinWillYield && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
				{
					return;
				}
			}
		}

		// Token: 0x06003EB4 RID: 16052 RVA: 0x000E9775 File Offset: 0x000E7975
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public void Exit()
		{
			if ((this.m_owner & -2147483648) == 0)
			{
				this.ExitSlowPath(true);
			}
			else
			{
				Interlocked.Decrement(ref this.m_owner);
			}
			Thread.EndCriticalRegion();
		}

		// Token: 0x06003EB5 RID: 16053 RVA: 0x000E97A4 File Offset: 0x000E79A4
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public void Exit(bool useMemoryBarrier)
		{
			if ((this.m_owner & -2147483648) != 0 && !useMemoryBarrier)
			{
				int owner = this.m_owner;
				this.m_owner = owner & -2;
			}
			else
			{
				this.ExitSlowPath(useMemoryBarrier);
			}
			Thread.EndCriticalRegion();
		}

		// Token: 0x06003EB6 RID: 16054 RVA: 0x000E97E8 File Offset: 0x000E79E8
		private void ExitSlowPath(bool useMemoryBarrier)
		{
			bool flag = (this.m_owner & int.MinValue) == 0;
			if (flag && !this.IsHeldByCurrentThread)
			{
				throw new SynchronizationLockException(Environment.GetResourceString("SpinLock_Exit_SynchronizationLockException"));
			}
			if (useMemoryBarrier)
			{
				if (flag)
				{
					Interlocked.Exchange(ref this.m_owner, 0);
					return;
				}
				Interlocked.Decrement(ref this.m_owner);
				return;
			}
			else
			{
				if (flag)
				{
					this.m_owner = 0;
					return;
				}
				int owner = this.m_owner;
				this.m_owner = owner & -2;
				return;
			}
		}

		// Token: 0x1700093F RID: 2367
		// (get) Token: 0x06003EB7 RID: 16055 RVA: 0x000E9865 File Offset: 0x000E7A65
		[__DynamicallyInvokable]
		public bool IsHeld
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				if (this.IsThreadOwnerTrackingEnabled)
				{
					return this.m_owner != 0;
				}
				return (this.m_owner & 1) != 0;
			}
		}

		// Token: 0x17000940 RID: 2368
		// (get) Token: 0x06003EB8 RID: 16056 RVA: 0x000E9888 File Offset: 0x000E7A88
		[__DynamicallyInvokable]
		public bool IsHeldByCurrentThread
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				if (!this.IsThreadOwnerTrackingEnabled)
				{
					throw new InvalidOperationException(Environment.GetResourceString("SpinLock_IsHeldByCurrentThread"));
				}
				return (this.m_owner & int.MaxValue) == Thread.CurrentThread.ManagedThreadId;
			}
		}

		// Token: 0x17000941 RID: 2369
		// (get) Token: 0x06003EB9 RID: 16057 RVA: 0x000E98BC File Offset: 0x000E7ABC
		[__DynamicallyInvokable]
		public bool IsThreadOwnerTrackingEnabled
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				return (this.m_owner & int.MinValue) == 0;
			}
		}

		// Token: 0x04001A58 RID: 6744
		private volatile int m_owner;

		// Token: 0x04001A59 RID: 6745
		private const int SPINNING_FACTOR = 100;

		// Token: 0x04001A5A RID: 6746
		private const int SLEEP_ONE_FREQUENCY = 40;

		// Token: 0x04001A5B RID: 6747
		private const int SLEEP_ZERO_FREQUENCY = 10;

		// Token: 0x04001A5C RID: 6748
		private const int TIMEOUT_CHECK_FREQUENCY = 10;

		// Token: 0x04001A5D RID: 6749
		private const int LOCK_ID_DISABLE_MASK = -2147483648;

		// Token: 0x04001A5E RID: 6750
		private const int LOCK_ANONYMOUS_OWNED = 1;

		// Token: 0x04001A5F RID: 6751
		private const int WAITERS_MASK = 2147483646;

		// Token: 0x04001A60 RID: 6752
		private const int ID_DISABLED_AND_ANONYMOUS_OWNED = -2147483647;

		// Token: 0x04001A61 RID: 6753
		private const int LOCK_UNOWNED = 0;

		// Token: 0x04001A62 RID: 6754
		private static int MAXIMUM_WAITERS = 2147483646;

		// Token: 0x02000BFB RID: 3067
		internal class SystemThreading_SpinLockDebugView
		{
			// Token: 0x06006F86 RID: 28550 RVA: 0x001807CD File Offset: 0x0017E9CD
			public SystemThreading_SpinLockDebugView(SpinLock spinLock)
			{
				this.m_spinLock = spinLock;
			}

			// Token: 0x17001325 RID: 4901
			// (get) Token: 0x06006F87 RID: 28551 RVA: 0x001807DC File Offset: 0x0017E9DC
			public bool? IsHeldByCurrentThread
			{
				get
				{
					bool? result;
					try
					{
						result = new bool?(this.m_spinLock.IsHeldByCurrentThread);
					}
					catch (InvalidOperationException)
					{
						result = null;
					}
					return result;
				}
			}

			// Token: 0x17001326 RID: 4902
			// (get) Token: 0x06006F88 RID: 28552 RVA: 0x0018081C File Offset: 0x0017EA1C
			public int? OwnerThreadID
			{
				get
				{
					if (this.m_spinLock.IsThreadOwnerTrackingEnabled)
					{
						return new int?(this.m_spinLock.m_owner);
					}
					return null;
				}
			}

			// Token: 0x17001327 RID: 4903
			// (get) Token: 0x06006F89 RID: 28553 RVA: 0x00180852 File Offset: 0x0017EA52
			public bool IsHeld
			{
				get
				{
					return this.m_spinLock.IsHeld;
				}
			}

			// Token: 0x04003644 RID: 13892
			private SpinLock m_spinLock;
		}
	}
}
