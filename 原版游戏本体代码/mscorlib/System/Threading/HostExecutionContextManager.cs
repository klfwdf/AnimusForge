using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004FD RID: 1277
	public class HostExecutionContextManager
	{
		// Token: 0x06003C49 RID: 15433
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool HostSecurityManagerPresent();

		// Token: 0x06003C4A RID: 15434
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int ReleaseHostSecurityContext(IntPtr context);

		// Token: 0x06003C4B RID: 15435
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int CloneHostSecurityContext(SafeHandle context, SafeHandle clonedContext);

		// Token: 0x06003C4C RID: 15436
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int CaptureHostSecurityContext(SafeHandle capturedContext);

		// Token: 0x06003C4D RID: 15437
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int SetHostSecurityContext(SafeHandle context, bool fReturnPrevious, SafeHandle prevContext);

		// Token: 0x06003C4E RID: 15438 RVA: 0x000E40CC File Offset: 0x000E22CC
		[SecurityCritical]
		internal static bool CheckIfHosted()
		{
			if (!HostExecutionContextManager._fIsHostedChecked)
			{
				HostExecutionContextManager._fIsHosted = HostExecutionContextManager.HostSecurityManagerPresent();
				HostExecutionContextManager._fIsHostedChecked = true;
			}
			return HostExecutionContextManager._fIsHosted;
		}

		// Token: 0x06003C4F RID: 15439 RVA: 0x000E40F4 File Offset: 0x000E22F4
		[SecuritySafeCritical]
		public virtual HostExecutionContext Capture()
		{
			HostExecutionContext result = null;
			if (HostExecutionContextManager.CheckIfHosted())
			{
				IUnknownSafeHandle unknownSafeHandle = new IUnknownSafeHandle();
				result = new HostExecutionContext(unknownSafeHandle);
				HostExecutionContextManager.CaptureHostSecurityContext(unknownSafeHandle);
			}
			return result;
		}

		// Token: 0x06003C50 RID: 15440 RVA: 0x000E4120 File Offset: 0x000E2320
		[SecurityCritical]
		public virtual object SetHostExecutionContext(HostExecutionContext hostExecutionContext)
		{
			if (hostExecutionContext == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
			}
			HostExecutionContextSwitcher hostExecutionContextSwitcher = new HostExecutionContextSwitcher();
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			hostExecutionContextSwitcher.executionContext = mutableExecutionContext;
			hostExecutionContextSwitcher.currentHostContext = hostExecutionContext;
			hostExecutionContextSwitcher.previousHostContext = null;
			if (HostExecutionContextManager.CheckIfHosted() && hostExecutionContext.State is IUnknownSafeHandle)
			{
				IUnknownSafeHandle unknownSafeHandle = new IUnknownSafeHandle();
				hostExecutionContextSwitcher.previousHostContext = new HostExecutionContext(unknownSafeHandle);
				IUnknownSafeHandle context = (IUnknownSafeHandle)hostExecutionContext.State;
				HostExecutionContextManager.SetHostSecurityContext(context, true, unknownSafeHandle);
			}
			mutableExecutionContext.HostExecutionContext = hostExecutionContext;
			return hostExecutionContextSwitcher;
		}

		// Token: 0x06003C51 RID: 15441 RVA: 0x000E41AC File Offset: 0x000E23AC
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public virtual void Revert(object previousState)
		{
			HostExecutionContextSwitcher hostExecutionContextSwitcher = previousState as HostExecutionContextSwitcher;
			if (hostExecutionContextSwitcher == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotOverrideSetWithoutRevert"));
			}
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			if (mutableExecutionContext != hostExecutionContextSwitcher.executionContext)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotUseSwitcherOtherThread"));
			}
			hostExecutionContextSwitcher.executionContext = null;
			HostExecutionContext hostExecutionContext = mutableExecutionContext.HostExecutionContext;
			if (hostExecutionContext != hostExecutionContextSwitcher.currentHostContext)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotUseSwitcherOtherThread"));
			}
			HostExecutionContext previousHostContext = hostExecutionContextSwitcher.previousHostContext;
			if (HostExecutionContextManager.CheckIfHosted() && previousHostContext != null && previousHostContext.State is IUnknownSafeHandle)
			{
				IUnknownSafeHandle context = (IUnknownSafeHandle)previousHostContext.State;
				HostExecutionContextManager.SetHostSecurityContext(context, false, null);
			}
			mutableExecutionContext.HostExecutionContext = previousHostContext;
		}

		// Token: 0x06003C52 RID: 15442 RVA: 0x000E425C File Offset: 0x000E245C
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static HostExecutionContext CaptureHostExecutionContext()
		{
			HostExecutionContext result = null;
			HostExecutionContextManager currentHostExecutionContextManager = HostExecutionContextManager.GetCurrentHostExecutionContextManager();
			if (currentHostExecutionContextManager != null)
			{
				result = currentHostExecutionContextManager.Capture();
			}
			return result;
		}

		// Token: 0x06003C53 RID: 15443 RVA: 0x000E427C File Offset: 0x000E247C
		[SecurityCritical]
		internal static object SetHostExecutionContextInternal(HostExecutionContext hostContext)
		{
			HostExecutionContextManager currentHostExecutionContextManager = HostExecutionContextManager.GetCurrentHostExecutionContextManager();
			object result = null;
			if (currentHostExecutionContextManager != null)
			{
				result = currentHostExecutionContextManager.SetHostExecutionContext(hostContext);
			}
			return result;
		}

		// Token: 0x06003C54 RID: 15444 RVA: 0x000E42A0 File Offset: 0x000E24A0
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static HostExecutionContextManager GetCurrentHostExecutionContextManager()
		{
			AppDomainManager currentAppDomainManager = AppDomainManager.CurrentAppDomainManager;
			if (currentAppDomainManager != null)
			{
				return currentAppDomainManager.HostExecutionContextManager;
			}
			return null;
		}

		// Token: 0x06003C55 RID: 15445 RVA: 0x000E42BE File Offset: 0x000E24BE
		internal static HostExecutionContextManager GetInternalHostExecutionContextManager()
		{
			if (HostExecutionContextManager._hostExecutionContextManager == null)
			{
				HostExecutionContextManager._hostExecutionContextManager = new HostExecutionContextManager();
			}
			return HostExecutionContextManager._hostExecutionContextManager;
		}

		// Token: 0x0400199D RID: 6557
		private static volatile bool _fIsHostedChecked;

		// Token: 0x0400199E RID: 6558
		private static volatile bool _fIsHosted;

		// Token: 0x0400199F RID: 6559
		private static HostExecutionContextManager _hostExecutionContextManager;
	}
}
