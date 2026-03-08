using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x020004EE RID: 1262
	[__DynamicallyInvokable]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy)]
	public class SynchronizationContext
	{
		// Token: 0x06003B92 RID: 15250 RVA: 0x000E260B File Offset: 0x000E080B
		[__DynamicallyInvokable]
		public SynchronizationContext()
		{
		}

		// Token: 0x06003B93 RID: 15251 RVA: 0x000E2614 File Offset: 0x000E0814
		[SecuritySafeCritical]
		protected void SetWaitNotificationRequired()
		{
			Type type = base.GetType();
			if (SynchronizationContext.s_cachedPreparedType1 != type && SynchronizationContext.s_cachedPreparedType2 != type && SynchronizationContext.s_cachedPreparedType3 != type && SynchronizationContext.s_cachedPreparedType4 != type && SynchronizationContext.s_cachedPreparedType5 != type)
			{
				RuntimeHelpers.PrepareDelegate(new SynchronizationContext.WaitDelegate(this.Wait));
				if (SynchronizationContext.s_cachedPreparedType1 == null)
				{
					SynchronizationContext.s_cachedPreparedType1 = type;
				}
				else if (SynchronizationContext.s_cachedPreparedType2 == null)
				{
					SynchronizationContext.s_cachedPreparedType2 = type;
				}
				else if (SynchronizationContext.s_cachedPreparedType3 == null)
				{
					SynchronizationContext.s_cachedPreparedType3 = type;
				}
				else if (SynchronizationContext.s_cachedPreparedType4 == null)
				{
					SynchronizationContext.s_cachedPreparedType4 = type;
				}
				else if (SynchronizationContext.s_cachedPreparedType5 == null)
				{
					SynchronizationContext.s_cachedPreparedType5 = type;
				}
			}
			this._props |= SynchronizationContextProperties.RequireWaitNotification;
		}

		// Token: 0x06003B94 RID: 15252 RVA: 0x000E26FC File Offset: 0x000E08FC
		public bool IsWaitNotificationRequired()
		{
			return (this._props & SynchronizationContextProperties.RequireWaitNotification) > SynchronizationContextProperties.None;
		}

		// Token: 0x06003B95 RID: 15253 RVA: 0x000E2709 File Offset: 0x000E0909
		[__DynamicallyInvokable]
		public virtual void Send(SendOrPostCallback d, object state)
		{
			d(state);
		}

		// Token: 0x06003B96 RID: 15254 RVA: 0x000E2712 File Offset: 0x000E0912
		[__DynamicallyInvokable]
		public virtual void Post(SendOrPostCallback d, object state)
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(d.Invoke), state);
		}

		// Token: 0x06003B97 RID: 15255 RVA: 0x000E2727 File Offset: 0x000E0927
		[__DynamicallyInvokable]
		public virtual void OperationStarted()
		{
		}

		// Token: 0x06003B98 RID: 15256 RVA: 0x000E2729 File Offset: 0x000E0929
		[__DynamicallyInvokable]
		public virtual void OperationCompleted()
		{
		}

		// Token: 0x06003B99 RID: 15257 RVA: 0x000E272B File Offset: 0x000E092B
		[SecurityCritical]
		[CLSCompliant(false)]
		[PrePrepareMethod]
		public virtual int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
		{
			if (waitHandles == null)
			{
				throw new ArgumentNullException("waitHandles");
			}
			return SynchronizationContext.WaitHelper(waitHandles, waitAll, millisecondsTimeout);
		}

		// Token: 0x06003B9A RID: 15258
		[SecurityCritical]
		[CLSCompliant(false)]
		[PrePrepareMethod]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		protected static extern int WaitHelper(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);

		// Token: 0x06003B9B RID: 15259 RVA: 0x000E2744 File Offset: 0x000E0944
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void SetSynchronizationContext(SynchronizationContext syncContext)
		{
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			mutableExecutionContext.SynchronizationContext = syncContext;
			mutableExecutionContext.SynchronizationContextNoFlow = syncContext;
		}

		// Token: 0x17000906 RID: 2310
		// (get) Token: 0x06003B9C RID: 15260 RVA: 0x000E276C File Offset: 0x000E096C
		[__DynamicallyInvokable]
		public static SynchronizationContext Current
		{
			[__DynamicallyInvokable]
			get
			{
				return Thread.CurrentThread.GetExecutionContextReader().SynchronizationContext ?? SynchronizationContext.GetThreadLocalContext();
			}
		}

		// Token: 0x17000907 RID: 2311
		// (get) Token: 0x06003B9D RID: 15261 RVA: 0x000E2794 File Offset: 0x000E0994
		internal static SynchronizationContext CurrentNoFlow
		{
			[FriendAccessAllowed]
			get
			{
				return Thread.CurrentThread.GetExecutionContextReader().SynchronizationContextNoFlow ?? SynchronizationContext.GetThreadLocalContext();
			}
		}

		// Token: 0x06003B9E RID: 15262 RVA: 0x000E27BC File Offset: 0x000E09BC
		private static SynchronizationContext GetThreadLocalContext()
		{
			SynchronizationContext synchronizationContext = null;
			if (synchronizationContext == null && Environment.IsWinRTSupported)
			{
				synchronizationContext = SynchronizationContext.GetWinRTContext();
			}
			return synchronizationContext;
		}

		// Token: 0x06003B9F RID: 15263 RVA: 0x000E27DC File Offset: 0x000E09DC
		[SecuritySafeCritical]
		private static SynchronizationContext GetWinRTContext()
		{
			if (!AppDomain.IsAppXModel())
			{
				return null;
			}
			object winRTDispatcherForCurrentThread = SynchronizationContext.GetWinRTDispatcherForCurrentThread();
			if (winRTDispatcherForCurrentThread != null)
			{
				return SynchronizationContext.GetWinRTSynchronizationContextFactory().Create(winRTDispatcherForCurrentThread);
			}
			return null;
		}

		// Token: 0x06003BA0 RID: 15264 RVA: 0x000E2808 File Offset: 0x000E0A08
		[SecurityCritical]
		private static WinRTSynchronizationContextFactoryBase GetWinRTSynchronizationContextFactory()
		{
			WinRTSynchronizationContextFactoryBase winRTSynchronizationContextFactoryBase = SynchronizationContext.s_winRTContextFactory;
			if (winRTSynchronizationContextFactoryBase == null)
			{
				Type type = Type.GetType("System.Threading.WinRTSynchronizationContextFactory, System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true);
				winRTSynchronizationContextFactoryBase = (SynchronizationContext.s_winRTContextFactory = (WinRTSynchronizationContextFactoryBase)Activator.CreateInstance(type, true));
			}
			return winRTSynchronizationContextFactoryBase;
		}

		// Token: 0x06003BA1 RID: 15265
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Interface)]
		private static extern object GetWinRTDispatcherForCurrentThread();

		// Token: 0x06003BA2 RID: 15266 RVA: 0x000E283E File Offset: 0x000E0A3E
		[__DynamicallyInvokable]
		public virtual SynchronizationContext CreateCopy()
		{
			return new SynchronizationContext();
		}

		// Token: 0x06003BA3 RID: 15267 RVA: 0x000E2845 File Offset: 0x000E0A45
		[SecurityCritical]
		private static int InvokeWaitMethodHelper(SynchronizationContext syncContext, IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
		{
			return syncContext.Wait(waitHandles, waitAll, millisecondsTimeout);
		}

		// Token: 0x0400196F RID: 6511
		private SynchronizationContextProperties _props;

		// Token: 0x04001970 RID: 6512
		private static Type s_cachedPreparedType1;

		// Token: 0x04001971 RID: 6513
		private static Type s_cachedPreparedType2;

		// Token: 0x04001972 RID: 6514
		private static Type s_cachedPreparedType3;

		// Token: 0x04001973 RID: 6515
		private static Type s_cachedPreparedType4;

		// Token: 0x04001974 RID: 6516
		private static Type s_cachedPreparedType5;

		// Token: 0x04001975 RID: 6517
		[SecurityCritical]
		private static WinRTSynchronizationContextFactoryBase s_winRTContextFactory;

		// Token: 0x02000BEC RID: 3052
		// (Invoke) Token: 0x06006F52 RID: 28498
		private delegate int WaitDelegate(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);
	}
}
