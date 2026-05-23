using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
	// Token: 0x02000533 RID: 1331
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public abstract class WaitHandle : MarshalByRefObject, IDisposable
	{
		// Token: 0x06003E80 RID: 16000 RVA: 0x000E8C67 File Offset: 0x000E6E67
		[SecuritySafeCritical]
		private static IntPtr GetInvalidHandle()
		{
			return Win32Native.INVALID_HANDLE_VALUE;
		}

		// Token: 0x06003E81 RID: 16001 RVA: 0x000E8C6E File Offset: 0x000E6E6E
		[__DynamicallyInvokable]
		protected WaitHandle()
		{
			this.Init();
		}

		// Token: 0x06003E82 RID: 16002 RVA: 0x000E8C7C File Offset: 0x000E6E7C
		[SecuritySafeCritical]
		private void Init()
		{
			this.safeWaitHandle = null;
			this.waitHandle = WaitHandle.InvalidHandle;
			this.hasThreadAffinity = false;
		}

		// Token: 0x1700093D RID: 2365
		// (get) Token: 0x06003E83 RID: 16003 RVA: 0x000E8C99 File Offset: 0x000E6E99
		// (set) Token: 0x06003E84 RID: 16004 RVA: 0x000E8CB8 File Offset: 0x000E6EB8
		[Obsolete("Use the SafeWaitHandle property instead.")]
		public virtual IntPtr Handle
		{
			[SecuritySafeCritical]
			get
			{
				if (this.safeWaitHandle != null)
				{
					return this.safeWaitHandle.DangerousGetHandle();
				}
				return WaitHandle.InvalidHandle;
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			set
			{
				if (value == WaitHandle.InvalidHandle)
				{
					if (this.safeWaitHandle != null)
					{
						this.safeWaitHandle.SetHandleAsInvalid();
						this.safeWaitHandle = null;
					}
				}
				else
				{
					this.safeWaitHandle = new SafeWaitHandle(value, true);
				}
				this.waitHandle = value;
			}
		}

		// Token: 0x1700093E RID: 2366
		// (get) Token: 0x06003E85 RID: 16005 RVA: 0x000E8D0A File Offset: 0x000E6F0A
		// (set) Token: 0x06003E86 RID: 16006 RVA: 0x000E8D34 File Offset: 0x000E6F34
		public SafeWaitHandle SafeWaitHandle
		{
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				if (this.safeWaitHandle == null)
				{
					this.safeWaitHandle = new SafeWaitHandle(WaitHandle.InvalidHandle, false);
				}
				return this.safeWaitHandle;
			}
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			set
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					if (value == null)
					{
						this.safeWaitHandle = null;
						this.waitHandle = WaitHandle.InvalidHandle;
					}
					else
					{
						this.safeWaitHandle = value;
						this.waitHandle = this.safeWaitHandle.DangerousGetHandle();
					}
				}
			}
		}

		// Token: 0x06003E87 RID: 16007 RVA: 0x000E8D90 File Offset: 0x000E6F90
		[SecurityCritical]
		internal void SetHandleInternal(SafeWaitHandle handle)
		{
			this.safeWaitHandle = handle;
			this.waitHandle = handle.DangerousGetHandle();
		}

		// Token: 0x06003E88 RID: 16008 RVA: 0x000E8DA7 File Offset: 0x000E6FA7
		public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return this.WaitOne((long)millisecondsTimeout, exitContext);
		}

		// Token: 0x06003E89 RID: 16009 RVA: 0x000E8DCC File Offset: 0x000E6FCC
		public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (-1L > num || 2147483647L < num)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return this.WaitOne(num, exitContext);
		}

		// Token: 0x06003E8A RID: 16010 RVA: 0x000E8E0D File Offset: 0x000E700D
		[__DynamicallyInvokable]
		public virtual bool WaitOne()
		{
			return this.WaitOne(-1, false);
		}

		// Token: 0x06003E8B RID: 16011 RVA: 0x000E8E17 File Offset: 0x000E7017
		[__DynamicallyInvokable]
		public virtual bool WaitOne(int millisecondsTimeout)
		{
			return this.WaitOne(millisecondsTimeout, false);
		}

		// Token: 0x06003E8C RID: 16012 RVA: 0x000E8E21 File Offset: 0x000E7021
		[__DynamicallyInvokable]
		public virtual bool WaitOne(TimeSpan timeout)
		{
			return this.WaitOne(timeout, false);
		}

		// Token: 0x06003E8D RID: 16013 RVA: 0x000E8E2B File Offset: 0x000E702B
		[SecuritySafeCritical]
		private bool WaitOne(long timeout, bool exitContext)
		{
			return WaitHandle.InternalWaitOne(this.safeWaitHandle, timeout, this.hasThreadAffinity, exitContext);
		}

		// Token: 0x06003E8E RID: 16014 RVA: 0x000E8E44 File Offset: 0x000E7044
		[SecurityCritical]
		internal static bool InternalWaitOne(SafeHandle waitableSafeHandle, long millisecondsTimeout, bool hasThreadAffinity, bool exitContext)
		{
			if (waitableSafeHandle == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
			}
			int num = WaitHandle.WaitOneNative(waitableSafeHandle, (uint)millisecondsTimeout, hasThreadAffinity, exitContext);
			if (AppDomainPauseManager.IsPaused)
			{
				AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
			}
			if (num == 128)
			{
				WaitHandle.ThrowAbandonedMutexException();
			}
			return num != 258;
		}

		// Token: 0x06003E8F RID: 16015 RVA: 0x000E8E9C File Offset: 0x000E709C
		[SecurityCritical]
		internal bool WaitOneWithoutFAS()
		{
			if (this.safeWaitHandle == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
			}
			long num = -1L;
			int num2 = WaitHandle.WaitOneNative(this.safeWaitHandle, (uint)num, this.hasThreadAffinity, false);
			if (num2 == 128)
			{
				WaitHandle.ThrowAbandonedMutexException();
			}
			return num2 != 258;
		}

		// Token: 0x06003E90 RID: 16016
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int WaitOneNative(SafeHandle waitableSafeHandle, uint millisecondsTimeout, bool hasThreadAffinity, bool exitContext);

		// Token: 0x06003E91 RID: 16017
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int WaitMultiple(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext, bool WaitAll);

		// Token: 0x06003E92 RID: 16018 RVA: 0x000E8EF8 File Offset: 0x000E70F8
		[SecuritySafeCritical]
		public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
		{
			if (waitHandles == null)
			{
				throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_Waithandles"));
			}
			if (waitHandles.Length == 0)
			{
				throw new ArgumentNullException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
			}
			if (waitHandles.Length > 64)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
			}
			if (-1 > millisecondsTimeout)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			WaitHandle[] array = new WaitHandle[waitHandles.Length];
			for (int i = 0; i < waitHandles.Length; i++)
			{
				WaitHandle waitHandle = waitHandles[i];
				if (waitHandle == null)
				{
					throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayElement"));
				}
				if (RemotingServices.IsTransparentProxy(waitHandle))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WaitOnTransparentProxy"));
				}
				array[i] = waitHandle;
			}
			int num = WaitHandle.WaitMultiple(array, millisecondsTimeout, exitContext, true);
			if (AppDomainPauseManager.IsPaused)
			{
				AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
			}
			if (128 <= num && 128 + array.Length > num)
			{
				WaitHandle.ThrowAbandonedMutexException();
			}
			GC.KeepAlive(array);
			return num != 258;
		}

		// Token: 0x06003E93 RID: 16019 RVA: 0x000E8FEC File Offset: 0x000E71EC
		public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (-1L > num || 2147483647L < num)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return WaitHandle.WaitAll(waitHandles, (int)num, exitContext);
		}

		// Token: 0x06003E94 RID: 16020 RVA: 0x000E902E File Offset: 0x000E722E
		[__DynamicallyInvokable]
		public static bool WaitAll(WaitHandle[] waitHandles)
		{
			return WaitHandle.WaitAll(waitHandles, -1, true);
		}

		// Token: 0x06003E95 RID: 16021 RVA: 0x000E9038 File Offset: 0x000E7238
		[__DynamicallyInvokable]
		public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout)
		{
			return WaitHandle.WaitAll(waitHandles, millisecondsTimeout, true);
		}

		// Token: 0x06003E96 RID: 16022 RVA: 0x000E9042 File Offset: 0x000E7242
		[__DynamicallyInvokable]
		public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
		{
			return WaitHandle.WaitAll(waitHandles, timeout, true);
		}

		// Token: 0x06003E97 RID: 16023 RVA: 0x000E904C File Offset: 0x000E724C
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
		{
			if (waitHandles == null)
			{
				throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_Waithandles"));
			}
			if (waitHandles.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
			}
			if (64 < waitHandles.Length)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
			}
			if (-1 > millisecondsTimeout)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			WaitHandle[] array = new WaitHandle[waitHandles.Length];
			for (int i = 0; i < waitHandles.Length; i++)
			{
				WaitHandle waitHandle = waitHandles[i];
				if (waitHandle == null)
				{
					throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayElement"));
				}
				if (RemotingServices.IsTransparentProxy(waitHandle))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WaitOnTransparentProxy"));
				}
				array[i] = waitHandle;
			}
			int num = WaitHandle.WaitMultiple(array, millisecondsTimeout, exitContext, false);
			if (AppDomainPauseManager.IsPaused)
			{
				AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
			}
			if (128 <= num && 128 + array.Length > num)
			{
				int num2 = num - 128;
				if (0 <= num2 && num2 < array.Length)
				{
					WaitHandle.ThrowAbandonedMutexException(num2, array[num2]);
				}
				else
				{
					WaitHandle.ThrowAbandonedMutexException();
				}
			}
			GC.KeepAlive(array);
			return num;
		}

		// Token: 0x06003E98 RID: 16024 RVA: 0x000E9158 File Offset: 0x000E7358
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (-1L > num || 2147483647L < num)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return WaitHandle.WaitAny(waitHandles, (int)num, exitContext);
		}

		// Token: 0x06003E99 RID: 16025 RVA: 0x000E919A File Offset: 0x000E739A
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
		{
			return WaitHandle.WaitAny(waitHandles, timeout, true);
		}

		// Token: 0x06003E9A RID: 16026 RVA: 0x000E91A4 File Offset: 0x000E73A4
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static int WaitAny(WaitHandle[] waitHandles)
		{
			return WaitHandle.WaitAny(waitHandles, -1, true);
		}

		// Token: 0x06003E9B RID: 16027 RVA: 0x000E91AE File Offset: 0x000E73AE
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout)
		{
			return WaitHandle.WaitAny(waitHandles, millisecondsTimeout, true);
		}

		// Token: 0x06003E9C RID: 16028
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int SignalAndWaitOne(SafeWaitHandle waitHandleToSignal, SafeWaitHandle waitHandleToWaitOn, int millisecondsTimeout, bool hasThreadAffinity, bool exitContext);

		// Token: 0x06003E9D RID: 16029 RVA: 0x000E91B8 File Offset: 0x000E73B8
		public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn)
		{
			return WaitHandle.SignalAndWait(toSignal, toWaitOn, -1, false);
		}

		// Token: 0x06003E9E RID: 16030 RVA: 0x000E91C4 File Offset: 0x000E73C4
		public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, TimeSpan timeout, bool exitContext)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (-1L > num || 2147483647L < num)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return WaitHandle.SignalAndWait(toSignal, toWaitOn, (int)num, exitContext);
		}

		// Token: 0x06003E9F RID: 16031 RVA: 0x000E9208 File Offset: 0x000E7408
		[SecuritySafeCritical]
		public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext)
		{
			if (toSignal == null)
			{
				throw new ArgumentNullException("toSignal");
			}
			if (toWaitOn == null)
			{
				throw new ArgumentNullException("toWaitOn");
			}
			if (-1 > millisecondsTimeout)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			int num = WaitHandle.SignalAndWaitOne(toSignal.safeWaitHandle, toWaitOn.safeWaitHandle, millisecondsTimeout, toWaitOn.hasThreadAffinity, exitContext);
			if (2147483647 != num && toSignal.hasThreadAffinity)
			{
				Thread.EndCriticalRegion();
				Thread.EndThreadAffinity();
			}
			if (128 == num)
			{
				WaitHandle.ThrowAbandonedMutexException();
			}
			if (298 == num)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Threading.WaitHandleTooManyPosts"));
			}
			return num == 0;
		}

		// Token: 0x06003EA0 RID: 16032 RVA: 0x000E92AD File Offset: 0x000E74AD
		private static void ThrowAbandonedMutexException()
		{
			throw new AbandonedMutexException();
		}

		// Token: 0x06003EA1 RID: 16033 RVA: 0x000E92B4 File Offset: 0x000E74B4
		private static void ThrowAbandonedMutexException(int location, WaitHandle handle)
		{
			throw new AbandonedMutexException(location, handle);
		}

		// Token: 0x06003EA2 RID: 16034 RVA: 0x000E92BD File Offset: 0x000E74BD
		public virtual void Close()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06003EA3 RID: 16035 RVA: 0x000E92CC File Offset: 0x000E74CC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		protected virtual void Dispose(bool explicitDisposing)
		{
			if (this.safeWaitHandle != null)
			{
				this.safeWaitHandle.Close();
			}
		}

		// Token: 0x06003EA4 RID: 16036 RVA: 0x000E92E5 File Offset: 0x000E74E5
		[__DynamicallyInvokable]
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x04001A4A RID: 6730
		[__DynamicallyInvokable]
		public const int WaitTimeout = 258;

		// Token: 0x04001A4B RID: 6731
		private const int MAX_WAITHANDLES = 64;

		// Token: 0x04001A4C RID: 6732
		private IntPtr waitHandle;

		// Token: 0x04001A4D RID: 6733
		[SecurityCritical]
		internal volatile SafeWaitHandle safeWaitHandle;

		// Token: 0x04001A4E RID: 6734
		internal bool hasThreadAffinity;

		// Token: 0x04001A4F RID: 6735
		protected static readonly IntPtr InvalidHandle = WaitHandle.GetInvalidHandle();

		// Token: 0x04001A50 RID: 6736
		private const int WAIT_OBJECT_0 = 0;

		// Token: 0x04001A51 RID: 6737
		private const int WAIT_ABANDONED = 128;

		// Token: 0x04001A52 RID: 6738
		private const int WAIT_FAILED = 2147483647;

		// Token: 0x04001A53 RID: 6739
		private const int ERROR_TOO_MANY_POSTS = 298;

		// Token: 0x02000BFA RID: 3066
		internal enum OpenExistingResult
		{
			// Token: 0x04003640 RID: 13888
			Success,
			// Token: 0x04003641 RID: 13889
			NameNotFound,
			// Token: 0x04003642 RID: 13890
			PathNotFound,
			// Token: 0x04003643 RID: 13891
			NameInvalid
		}
	}
}
