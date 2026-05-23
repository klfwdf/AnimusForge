using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000501 RID: 1281
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public static class Monitor
	{
		// Token: 0x06003C61 RID: 15457
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void Enter(object obj);

		// Token: 0x06003C62 RID: 15458 RVA: 0x000E4394 File Offset: 0x000E2594
		[__DynamicallyInvokable]
		public static void Enter(object obj, ref bool lockTaken)
		{
			if (lockTaken)
			{
				Monitor.ThrowLockTakenException();
			}
			Monitor.ReliableEnter(obj, ref lockTaken);
		}

		// Token: 0x06003C63 RID: 15459 RVA: 0x000E43A6 File Offset: 0x000E25A6
		private static void ThrowLockTakenException()
		{
			throw new ArgumentException(Environment.GetResourceString("Argument_MustBeFalse"), "lockTaken");
		}

		// Token: 0x06003C64 RID: 15460
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ReliableEnter(object obj, ref bool lockTaken);

		// Token: 0x06003C65 RID: 15461
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void Exit(object obj);

		// Token: 0x06003C66 RID: 15462 RVA: 0x000E43BC File Offset: 0x000E25BC
		[__DynamicallyInvokable]
		public static bool TryEnter(object obj)
		{
			bool result = false;
			Monitor.TryEnter(obj, 0, ref result);
			return result;
		}

		// Token: 0x06003C67 RID: 15463 RVA: 0x000E43D5 File Offset: 0x000E25D5
		[__DynamicallyInvokable]
		public static void TryEnter(object obj, ref bool lockTaken)
		{
			if (lockTaken)
			{
				Monitor.ThrowLockTakenException();
			}
			Monitor.ReliableEnterTimeout(obj, 0, ref lockTaken);
		}

		// Token: 0x06003C68 RID: 15464 RVA: 0x000E43E8 File Offset: 0x000E25E8
		[__DynamicallyInvokable]
		public static bool TryEnter(object obj, int millisecondsTimeout)
		{
			bool result = false;
			Monitor.TryEnter(obj, millisecondsTimeout, ref result);
			return result;
		}

		// Token: 0x06003C69 RID: 15465 RVA: 0x000E4404 File Offset: 0x000E2604
		private static int MillisecondsTimeoutFromTimeSpan(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return (int)num;
		}

		// Token: 0x06003C6A RID: 15466 RVA: 0x000E443F File Offset: 0x000E263F
		[__DynamicallyInvokable]
		public static bool TryEnter(object obj, TimeSpan timeout)
		{
			return Monitor.TryEnter(obj, Monitor.MillisecondsTimeoutFromTimeSpan(timeout));
		}

		// Token: 0x06003C6B RID: 15467 RVA: 0x000E444D File Offset: 0x000E264D
		[__DynamicallyInvokable]
		public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken)
		{
			if (lockTaken)
			{
				Monitor.ThrowLockTakenException();
			}
			Monitor.ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
		}

		// Token: 0x06003C6C RID: 15468 RVA: 0x000E4460 File Offset: 0x000E2660
		[__DynamicallyInvokable]
		public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken)
		{
			if (lockTaken)
			{
				Monitor.ThrowLockTakenException();
			}
			Monitor.ReliableEnterTimeout(obj, Monitor.MillisecondsTimeoutFromTimeSpan(timeout), ref lockTaken);
		}

		// Token: 0x06003C6D RID: 15469
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ReliableEnterTimeout(object obj, int timeout, ref bool lockTaken);

		// Token: 0x06003C6E RID: 15470 RVA: 0x000E4478 File Offset: 0x000E2678
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static bool IsEntered(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			return Monitor.IsEnteredNative(obj);
		}

		// Token: 0x06003C6F RID: 15471
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsEnteredNative(object obj);

		// Token: 0x06003C70 RID: 15472
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool ObjWait(bool exitContext, int millisecondsTimeout, object obj);

		// Token: 0x06003C71 RID: 15473 RVA: 0x000E448E File Offset: 0x000E268E
		[SecuritySafeCritical]
		public static bool Wait(object obj, int millisecondsTimeout, bool exitContext)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			return Monitor.ObjWait(exitContext, millisecondsTimeout, obj);
		}

		// Token: 0x06003C72 RID: 15474 RVA: 0x000E44A6 File Offset: 0x000E26A6
		public static bool Wait(object obj, TimeSpan timeout, bool exitContext)
		{
			return Monitor.Wait(obj, Monitor.MillisecondsTimeoutFromTimeSpan(timeout), exitContext);
		}

		// Token: 0x06003C73 RID: 15475 RVA: 0x000E44B5 File Offset: 0x000E26B5
		[__DynamicallyInvokable]
		public static bool Wait(object obj, int millisecondsTimeout)
		{
			return Monitor.Wait(obj, millisecondsTimeout, false);
		}

		// Token: 0x06003C74 RID: 15476 RVA: 0x000E44BF File Offset: 0x000E26BF
		[__DynamicallyInvokable]
		public static bool Wait(object obj, TimeSpan timeout)
		{
			return Monitor.Wait(obj, Monitor.MillisecondsTimeoutFromTimeSpan(timeout), false);
		}

		// Token: 0x06003C75 RID: 15477 RVA: 0x000E44CE File Offset: 0x000E26CE
		[__DynamicallyInvokable]
		public static bool Wait(object obj)
		{
			return Monitor.Wait(obj, -1, false);
		}

		// Token: 0x06003C76 RID: 15478
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ObjPulse(object obj);

		// Token: 0x06003C77 RID: 15479 RVA: 0x000E44D8 File Offset: 0x000E26D8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void Pulse(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			Monitor.ObjPulse(obj);
		}

		// Token: 0x06003C78 RID: 15480
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ObjPulseAll(object obj);

		// Token: 0x06003C79 RID: 15481 RVA: 0x000E44EE File Offset: 0x000E26EE
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void PulseAll(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			Monitor.ObjPulseAll(obj);
		}
	}
}
