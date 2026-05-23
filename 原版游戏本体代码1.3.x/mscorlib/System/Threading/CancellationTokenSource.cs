using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000545 RID: 1349
	[ComVisible(false)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class CancellationTokenSource : IDisposable
	{
		// Token: 0x06003F47 RID: 16199 RVA: 0x000EB930 File Offset: 0x000E9B30
		private static void LinkedTokenCancelDelegate(object source)
		{
			CancellationTokenSource cancellationTokenSource = source as CancellationTokenSource;
			cancellationTokenSource.Cancel();
		}

		// Token: 0x17000959 RID: 2393
		// (get) Token: 0x06003F48 RID: 16200 RVA: 0x000EB94A File Offset: 0x000E9B4A
		[__DynamicallyInvokable]
		public bool IsCancellationRequested
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_state >= 2;
			}
		}

		// Token: 0x1700095A RID: 2394
		// (get) Token: 0x06003F49 RID: 16201 RVA: 0x000EB95A File Offset: 0x000E9B5A
		internal bool IsCancellationCompleted
		{
			get
			{
				return this.m_state == 3;
			}
		}

		// Token: 0x1700095B RID: 2395
		// (get) Token: 0x06003F4A RID: 16202 RVA: 0x000EB967 File Offset: 0x000E9B67
		internal bool IsDisposed
		{
			get
			{
				return this.m_disposed;
			}
		}

		// Token: 0x1700095C RID: 2396
		// (get) Token: 0x06003F4C RID: 16204 RVA: 0x000EB97A File Offset: 0x000E9B7A
		// (set) Token: 0x06003F4B RID: 16203 RVA: 0x000EB96F File Offset: 0x000E9B6F
		internal int ThreadIDExecutingCallbacks
		{
			get
			{
				return this.m_threadIDExecutingCallbacks;
			}
			set
			{
				this.m_threadIDExecutingCallbacks = value;
			}
		}

		// Token: 0x1700095D RID: 2397
		// (get) Token: 0x06003F4D RID: 16205 RVA: 0x000EB984 File Offset: 0x000E9B84
		[__DynamicallyInvokable]
		public CancellationToken Token
		{
			[__DynamicallyInvokable]
			get
			{
				this.ThrowIfDisposed();
				return new CancellationToken(this);
			}
		}

		// Token: 0x1700095E RID: 2398
		// (get) Token: 0x06003F4E RID: 16206 RVA: 0x000EB992 File Offset: 0x000E9B92
		internal bool CanBeCanceled
		{
			get
			{
				return this.m_state != 0;
			}
		}

		// Token: 0x1700095F RID: 2399
		// (get) Token: 0x06003F4F RID: 16207 RVA: 0x000EB9A0 File Offset: 0x000E9BA0
		internal WaitHandle WaitHandle
		{
			get
			{
				this.ThrowIfDisposed();
				if (this.m_kernelEvent != null)
				{
					return this.m_kernelEvent;
				}
				ManualResetEvent manualResetEvent = new ManualResetEvent(false);
				if (Interlocked.CompareExchange<ManualResetEvent>(ref this.m_kernelEvent, manualResetEvent, null) != null)
				{
					((IDisposable)manualResetEvent).Dispose();
				}
				if (this.IsCancellationRequested)
				{
					this.m_kernelEvent.Set();
				}
				return this.m_kernelEvent;
			}
		}

		// Token: 0x17000960 RID: 2400
		// (get) Token: 0x06003F50 RID: 16208 RVA: 0x000EBA00 File Offset: 0x000E9C00
		internal CancellationCallbackInfo ExecutingCallback
		{
			get
			{
				return this.m_executingCallback;
			}
		}

		// Token: 0x06003F51 RID: 16209 RVA: 0x000EBA0A File Offset: 0x000E9C0A
		[__DynamicallyInvokable]
		public CancellationTokenSource()
		{
			this.m_state = 1;
		}

		// Token: 0x06003F52 RID: 16210 RVA: 0x000EBA24 File Offset: 0x000E9C24
		private CancellationTokenSource(bool set)
		{
			this.m_state = (set ? 3 : 0);
		}

		// Token: 0x06003F53 RID: 16211 RVA: 0x000EBA44 File Offset: 0x000E9C44
		[__DynamicallyInvokable]
		public CancellationTokenSource(TimeSpan delay)
		{
			long num = (long)delay.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("delay");
			}
			this.InitializeWithTimer((int)num);
		}

		// Token: 0x06003F54 RID: 16212 RVA: 0x000EBA8A File Offset: 0x000E9C8A
		[__DynamicallyInvokable]
		public CancellationTokenSource(int millisecondsDelay)
		{
			if (millisecondsDelay < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsDelay");
			}
			this.InitializeWithTimer(millisecondsDelay);
		}

		// Token: 0x06003F55 RID: 16213 RVA: 0x000EBAB1 File Offset: 0x000E9CB1
		private void InitializeWithTimer(int millisecondsDelay)
		{
			this.m_state = 1;
			this.m_timer = new Timer(CancellationTokenSource.s_timerCallback, this, millisecondsDelay, -1);
		}

		// Token: 0x06003F56 RID: 16214 RVA: 0x000EBAD1 File Offset: 0x000E9CD1
		[__DynamicallyInvokable]
		public void Cancel()
		{
			this.Cancel(false);
		}

		// Token: 0x06003F57 RID: 16215 RVA: 0x000EBADA File Offset: 0x000E9CDA
		[__DynamicallyInvokable]
		public void Cancel(bool throwOnFirstException)
		{
			this.ThrowIfDisposed();
			this.NotifyCancellation(throwOnFirstException);
		}

		// Token: 0x06003F58 RID: 16216 RVA: 0x000EBAEC File Offset: 0x000E9CEC
		[__DynamicallyInvokable]
		public void CancelAfter(TimeSpan delay)
		{
			long num = (long)delay.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("delay");
			}
			this.CancelAfter((int)num);
		}

		// Token: 0x06003F59 RID: 16217 RVA: 0x000EBB24 File Offset: 0x000E9D24
		[__DynamicallyInvokable]
		public void CancelAfter(int millisecondsDelay)
		{
			this.ThrowIfDisposed();
			if (millisecondsDelay < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsDelay");
			}
			if (this.IsCancellationRequested)
			{
				return;
			}
			if (this.m_timer == null)
			{
				Timer timer = new Timer(CancellationTokenSource.s_timerCallback, this, -1, -1);
				if (Interlocked.CompareExchange<Timer>(ref this.m_timer, timer, null) != null)
				{
					timer.Dispose();
				}
			}
			try
			{
				this.m_timer.Change(millisecondsDelay, -1);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		// Token: 0x06003F5A RID: 16218 RVA: 0x000EBBA4 File Offset: 0x000E9DA4
		private static void TimerCallbackLogic(object obj)
		{
			CancellationTokenSource cancellationTokenSource = (CancellationTokenSource)obj;
			if (!cancellationTokenSource.IsDisposed)
			{
				try
				{
					cancellationTokenSource.Cancel();
				}
				catch (ObjectDisposedException)
				{
					if (!cancellationTokenSource.IsDisposed)
					{
						throw;
					}
				}
			}
		}

		// Token: 0x06003F5B RID: 16219 RVA: 0x000EBBE8 File Offset: 0x000E9DE8
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06003F5C RID: 16220 RVA: 0x000EBBF8 File Offset: 0x000E9DF8
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.m_disposed)
				{
					return;
				}
				if (this.m_timer != null)
				{
					this.m_timer.Dispose();
				}
				CancellationTokenRegistration[] linkingRegistrations = this.m_linkingRegistrations;
				if (linkingRegistrations != null)
				{
					this.m_linkingRegistrations = null;
					for (int i = 0; i < linkingRegistrations.Length; i++)
					{
						linkingRegistrations[i].Dispose();
					}
				}
				this.m_registeredCallbacksLists = null;
				if (this.m_kernelEvent != null)
				{
					this.m_kernelEvent.Close();
					this.m_kernelEvent = null;
				}
				this.m_disposed = true;
			}
		}

		// Token: 0x06003F5D RID: 16221 RVA: 0x000EBC83 File Offset: 0x000E9E83
		internal void ThrowIfDisposed()
		{
			if (this.m_disposed)
			{
				CancellationTokenSource.ThrowObjectDisposedException();
			}
		}

		// Token: 0x06003F5E RID: 16222 RVA: 0x000EBC92 File Offset: 0x000E9E92
		private static void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationTokenSource_Disposed"));
		}

		// Token: 0x06003F5F RID: 16223 RVA: 0x000EBCA4 File Offset: 0x000E9EA4
		internal static CancellationTokenSource InternalGetStaticSource(bool set)
		{
			if (!set)
			{
				return CancellationTokenSource._staticSource_NotCancelable;
			}
			return CancellationTokenSource._staticSource_Set;
		}

		// Token: 0x06003F60 RID: 16224 RVA: 0x000EBCB4 File Offset: 0x000E9EB4
		internal CancellationTokenRegistration InternalRegister(Action<object> callback, object stateForCallback, SynchronizationContext targetSyncContext, ExecutionContext executionContext)
		{
			if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
			{
				this.ThrowIfDisposed();
			}
			if (!this.IsCancellationRequested)
			{
				if (this.m_disposed && !AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
				{
					return default(CancellationTokenRegistration);
				}
				int num = Thread.CurrentThread.ManagedThreadId % CancellationTokenSource.s_nLists;
				CancellationCallbackInfo cancellationCallbackInfo = new CancellationCallbackInfo(callback, stateForCallback, targetSyncContext, executionContext, this);
				SparselyPopulatedArray<CancellationCallbackInfo>[] array = this.m_registeredCallbacksLists;
				if (array == null)
				{
					SparselyPopulatedArray<CancellationCallbackInfo>[] array2 = new SparselyPopulatedArray<CancellationCallbackInfo>[CancellationTokenSource.s_nLists];
					array = Interlocked.CompareExchange<SparselyPopulatedArray<CancellationCallbackInfo>[]>(ref this.m_registeredCallbacksLists, array2, null);
					if (array == null)
					{
						array = array2;
					}
				}
				SparselyPopulatedArray<CancellationCallbackInfo> sparselyPopulatedArray = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref array[num]);
				if (sparselyPopulatedArray == null)
				{
					SparselyPopulatedArray<CancellationCallbackInfo> value = new SparselyPopulatedArray<CancellationCallbackInfo>(4);
					Interlocked.CompareExchange<SparselyPopulatedArray<CancellationCallbackInfo>>(ref array[num], value, null);
					sparselyPopulatedArray = array[num];
				}
				SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> registrationInfo = sparselyPopulatedArray.Add(cancellationCallbackInfo);
				CancellationTokenRegistration result = new CancellationTokenRegistration(cancellationCallbackInfo, registrationInfo);
				if (!this.IsCancellationRequested)
				{
					return result;
				}
				if (!result.TryDeregister())
				{
					return result;
				}
			}
			callback(stateForCallback);
			return default(CancellationTokenRegistration);
		}

		// Token: 0x06003F61 RID: 16225 RVA: 0x000EBDA8 File Offset: 0x000E9FA8
		private void NotifyCancellation(bool throwOnFirstException)
		{
			if (this.IsCancellationRequested)
			{
				return;
			}
			if (Interlocked.CompareExchange(ref this.m_state, 2, 1) == 1)
			{
				Timer timer = this.m_timer;
				if (timer != null)
				{
					timer.Dispose();
				}
				this.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
				if (this.m_kernelEvent != null)
				{
					this.m_kernelEvent.Set();
				}
				this.ExecuteCallbackHandlers(throwOnFirstException);
			}
		}

		// Token: 0x06003F62 RID: 16226 RVA: 0x000EBE10 File Offset: 0x000EA010
		private void ExecuteCallbackHandlers(bool throwOnFirstException)
		{
			List<Exception> list = null;
			SparselyPopulatedArray<CancellationCallbackInfo>[] registeredCallbacksLists = this.m_registeredCallbacksLists;
			if (registeredCallbacksLists == null)
			{
				Interlocked.Exchange(ref this.m_state, 3);
				return;
			}
			try
			{
				for (int i = 0; i < registeredCallbacksLists.Length; i++)
				{
					SparselyPopulatedArray<CancellationCallbackInfo> sparselyPopulatedArray = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref registeredCallbacksLists[i]);
					if (sparselyPopulatedArray != null)
					{
						for (SparselyPopulatedArrayFragment<CancellationCallbackInfo> sparselyPopulatedArrayFragment = sparselyPopulatedArray.Tail; sparselyPopulatedArrayFragment != null; sparselyPopulatedArrayFragment = sparselyPopulatedArrayFragment.Prev)
						{
							for (int j = sparselyPopulatedArrayFragment.Length - 1; j >= 0; j--)
							{
								this.m_executingCallback = sparselyPopulatedArrayFragment[j];
								if (this.m_executingCallback != null)
								{
									CancellationCallbackCoreWorkArguments cancellationCallbackCoreWorkArguments = new CancellationCallbackCoreWorkArguments(sparselyPopulatedArrayFragment, j);
									try
									{
										if (this.m_executingCallback.TargetSyncContext != null)
										{
											this.m_executingCallback.TargetSyncContext.Send(new SendOrPostCallback(this.CancellationCallbackCoreWork_OnSyncContext), cancellationCallbackCoreWorkArguments);
											this.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
										}
										else
										{
											this.CancellationCallbackCoreWork(cancellationCallbackCoreWorkArguments);
										}
									}
									catch (Exception item)
									{
										if (throwOnFirstException)
										{
											throw;
										}
										if (list == null)
										{
											list = new List<Exception>();
										}
										list.Add(item);
									}
								}
							}
						}
					}
				}
			}
			finally
			{
				this.m_state = 3;
				this.m_executingCallback = null;
				Thread.MemoryBarrier();
			}
			if (list != null)
			{
				throw new AggregateException(list);
			}
		}

		// Token: 0x06003F63 RID: 16227 RVA: 0x000EBF6C File Offset: 0x000EA16C
		private void CancellationCallbackCoreWork_OnSyncContext(object obj)
		{
			this.CancellationCallbackCoreWork((CancellationCallbackCoreWorkArguments)obj);
		}

		// Token: 0x06003F64 RID: 16228 RVA: 0x000EBF7C File Offset: 0x000EA17C
		private void CancellationCallbackCoreWork(CancellationCallbackCoreWorkArguments args)
		{
			CancellationCallbackInfo cancellationCallbackInfo = args.m_currArrayFragment.SafeAtomicRemove(args.m_currArrayIndex, this.m_executingCallback);
			if (cancellationCallbackInfo == this.m_executingCallback)
			{
				if (cancellationCallbackInfo.TargetExecutionContext != null)
				{
					cancellationCallbackInfo.CancellationTokenSource.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
				}
				cancellationCallbackInfo.ExecuteCallback();
			}
		}

		// Token: 0x06003F65 RID: 16229 RVA: 0x000EBFD4 File Offset: 0x000EA1D4
		[__DynamicallyInvokable]
		public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
		{
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			bool canBeCanceled = token2.CanBeCanceled;
			if (token1.CanBeCanceled)
			{
				cancellationTokenSource.m_linkingRegistrations = new CancellationTokenRegistration[canBeCanceled ? 2 : 1];
				cancellationTokenSource.m_linkingRegistrations[0] = token1.InternalRegisterWithoutEC(CancellationTokenSource.s_LinkedTokenCancelDelegate, cancellationTokenSource);
			}
			if (canBeCanceled)
			{
				int num = 1;
				if (cancellationTokenSource.m_linkingRegistrations == null)
				{
					cancellationTokenSource.m_linkingRegistrations = new CancellationTokenRegistration[1];
					num = 0;
				}
				cancellationTokenSource.m_linkingRegistrations[num] = token2.InternalRegisterWithoutEC(CancellationTokenSource.s_LinkedTokenCancelDelegate, cancellationTokenSource);
			}
			return cancellationTokenSource;
		}

		// Token: 0x06003F66 RID: 16230 RVA: 0x000EC058 File Offset: 0x000EA258
		[__DynamicallyInvokable]
		public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
		{
			if (tokens == null)
			{
				throw new ArgumentNullException("tokens");
			}
			if (tokens.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("CancellationToken_CreateLinkedToken_TokensIsEmpty"));
			}
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.m_linkingRegistrations = new CancellationTokenRegistration[tokens.Length];
			for (int i = 0; i < tokens.Length; i++)
			{
				if (tokens[i].CanBeCanceled)
				{
					cancellationTokenSource.m_linkingRegistrations[i] = tokens[i].InternalRegisterWithoutEC(CancellationTokenSource.s_LinkedTokenCancelDelegate, cancellationTokenSource);
				}
			}
			return cancellationTokenSource;
		}

		// Token: 0x06003F67 RID: 16231 RVA: 0x000EC0D8 File Offset: 0x000EA2D8
		internal void WaitForCallbackToComplete(CancellationCallbackInfo callbackInfo)
		{
			SpinWait spinWait = default(SpinWait);
			while (this.ExecutingCallback == callbackInfo)
			{
				spinWait.SpinOnce();
			}
		}

		// Token: 0x04001A9C RID: 6812
		private static readonly CancellationTokenSource _staticSource_Set = new CancellationTokenSource(true);

		// Token: 0x04001A9D RID: 6813
		private static readonly CancellationTokenSource _staticSource_NotCancelable = new CancellationTokenSource(false);

		// Token: 0x04001A9E RID: 6814
		private static readonly int s_nLists = ((PlatformHelper.ProcessorCount > 24) ? 24 : PlatformHelper.ProcessorCount);

		// Token: 0x04001A9F RID: 6815
		private volatile ManualResetEvent m_kernelEvent;

		// Token: 0x04001AA0 RID: 6816
		private volatile SparselyPopulatedArray<CancellationCallbackInfo>[] m_registeredCallbacksLists;

		// Token: 0x04001AA1 RID: 6817
		private const int CANNOT_BE_CANCELED = 0;

		// Token: 0x04001AA2 RID: 6818
		private const int NOT_CANCELED = 1;

		// Token: 0x04001AA3 RID: 6819
		private const int NOTIFYING = 2;

		// Token: 0x04001AA4 RID: 6820
		private const int NOTIFYINGCOMPLETE = 3;

		// Token: 0x04001AA5 RID: 6821
		private volatile int m_state;

		// Token: 0x04001AA6 RID: 6822
		private volatile int m_threadIDExecutingCallbacks = -1;

		// Token: 0x04001AA7 RID: 6823
		private bool m_disposed;

		// Token: 0x04001AA8 RID: 6824
		private CancellationTokenRegistration[] m_linkingRegistrations;

		// Token: 0x04001AA9 RID: 6825
		private static readonly Action<object> s_LinkedTokenCancelDelegate = new Action<object>(CancellationTokenSource.LinkedTokenCancelDelegate);

		// Token: 0x04001AAA RID: 6826
		private volatile CancellationCallbackInfo m_executingCallback;

		// Token: 0x04001AAB RID: 6827
		private volatile Timer m_timer;

		// Token: 0x04001AAC RID: 6828
		private static readonly TimerCallback s_timerCallback = new TimerCallback(CancellationTokenSource.TimerCallbackLogic);
	}
}
