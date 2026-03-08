using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Security
{
	// Token: 0x020001EE RID: 494
	public sealed class SecurityContext : IDisposable
	{
		// Token: 0x06001DB0 RID: 7600 RVA: 0x00067AC0 File Offset: 0x00065CC0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal SecurityContext()
		{
		}

		// Token: 0x1700034B RID: 843
		// (get) Token: 0x06001DB1 RID: 7601 RVA: 0x00067AC8 File Offset: 0x00065CC8
		internal static SecurityContext FullTrustSecurityContext
		{
			[SecurityCritical]
			get
			{
				if (SecurityContext._fullTrustSC == null)
				{
					SecurityContext._fullTrustSC = SecurityContext.CreateFullTrustSecurityContext();
				}
				return SecurityContext._fullTrustSC;
			}
		}

		// Token: 0x1700034C RID: 844
		// (set) Token: 0x06001DB2 RID: 7602 RVA: 0x00067AE6 File Offset: 0x00065CE6
		internal ExecutionContext ExecutionContext
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			set
			{
				this._executionContext = value;
			}
		}

		// Token: 0x1700034D RID: 845
		// (get) Token: 0x06001DB3 RID: 7603 RVA: 0x00067AEF File Offset: 0x00065CEF
		// (set) Token: 0x06001DB4 RID: 7604 RVA: 0x00067AF9 File Offset: 0x00065CF9
		internal WindowsIdentity WindowsIdentity
		{
			get
			{
				return this._windowsIdentity;
			}
			set
			{
				this._windowsIdentity = value;
			}
		}

		// Token: 0x1700034E RID: 846
		// (get) Token: 0x06001DB5 RID: 7605 RVA: 0x00067B04 File Offset: 0x00065D04
		// (set) Token: 0x06001DB6 RID: 7606 RVA: 0x00067B0E File Offset: 0x00065D0E
		internal CompressedStack CompressedStack
		{
			get
			{
				return this._compressedStack;
			}
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			set
			{
				this._compressedStack = value;
			}
		}

		// Token: 0x06001DB7 RID: 7607 RVA: 0x00067B19 File Offset: 0x00065D19
		public void Dispose()
		{
			if (this._windowsIdentity != null)
			{
				this._windowsIdentity.Dispose();
			}
		}

		// Token: 0x06001DB8 RID: 7608 RVA: 0x00067B32 File Offset: 0x00065D32
		[SecurityCritical]
		public static AsyncFlowControl SuppressFlow()
		{
			return SecurityContext.SuppressFlow(SecurityContextDisableFlow.All);
		}

		// Token: 0x06001DB9 RID: 7609 RVA: 0x00067B3E File Offset: 0x00065D3E
		[SecurityCritical]
		public static AsyncFlowControl SuppressFlowWindowsIdentity()
		{
			return SecurityContext.SuppressFlow(SecurityContextDisableFlow.WI);
		}

		// Token: 0x06001DBA RID: 7610 RVA: 0x00067B48 File Offset: 0x00065D48
		[SecurityCritical]
		internal static AsyncFlowControl SuppressFlow(SecurityContextDisableFlow flags)
		{
			if (SecurityContext.IsFlowSuppressed(flags))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotSupressFlowMultipleTimes"));
			}
			ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
			if (mutableExecutionContext.SecurityContext == null)
			{
				mutableExecutionContext.SecurityContext = new SecurityContext();
			}
			AsyncFlowControl result = default(AsyncFlowControl);
			result.Setup(flags);
			return result;
		}

		// Token: 0x06001DBB RID: 7611 RVA: 0x00067B9C File Offset: 0x00065D9C
		[SecuritySafeCritical]
		public static void RestoreFlow()
		{
			SecurityContext securityContext = Thread.CurrentThread.GetMutableExecutionContext().SecurityContext;
			if (securityContext == null || securityContext._disableFlow == SecurityContextDisableFlow.Nothing)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRestoreUnsupressedFlow"));
			}
			securityContext._disableFlow = SecurityContextDisableFlow.Nothing;
		}

		// Token: 0x06001DBC RID: 7612 RVA: 0x00067BDF File Offset: 0x00065DDF
		public static bool IsFlowSuppressed()
		{
			return SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.All);
		}

		// Token: 0x06001DBD RID: 7613 RVA: 0x00067BEB File Offset: 0x00065DEB
		public static bool IsWindowsIdentityFlowSuppressed()
		{
			return SecurityContext._LegacyImpersonationPolicy || SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.WI);
		}

		// Token: 0x06001DBE RID: 7614 RVA: 0x00067BFC File Offset: 0x00065DFC
		[SecuritySafeCritical]
		internal static bool IsFlowSuppressed(SecurityContextDisableFlow flags)
		{
			return Thread.CurrentThread.GetExecutionContextReader().SecurityContext.IsFlowSuppressed(flags);
		}

		// Token: 0x06001DBF RID: 7615 RVA: 0x00067C24 File Offset: 0x00065E24
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Run(SecurityContext securityContext, ContextCallback callback, object state)
		{
			if (securityContext == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMe;
			if (!securityContext.isNewCapture)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
			}
			securityContext.isNewCapture = false;
			ExecutionContext.Reader executionContextReader = Thread.CurrentThread.GetExecutionContextReader();
			if (SecurityContext.CurrentlyInDefaultFTSecurityContext(executionContextReader) && securityContext.IsDefaultFTSecurityContext())
			{
				callback(state);
				if (SecurityContext.GetCurrentWI(Thread.CurrentThread.GetExecutionContextReader()) != null)
				{
					WindowsIdentity.SafeRevertToSelf(ref stackCrawlMark);
					return;
				}
			}
			else
			{
				SecurityContext.RunInternal(securityContext, callback, state);
			}
		}

		// Token: 0x06001DC0 RID: 7616 RVA: 0x00067CB0 File Offset: 0x00065EB0
		[SecurityCritical]
		internal static void RunInternal(SecurityContext securityContext, ContextCallback callBack, object state)
		{
			if (SecurityContext.cleanupCode == null)
			{
				SecurityContext.tryCode = new RuntimeHelpers.TryCode(SecurityContext.runTryCode);
				SecurityContext.cleanupCode = new RuntimeHelpers.CleanupCode(SecurityContext.runFinallyCode);
			}
			SecurityContext.SecurityContextRunData userData = new SecurityContext.SecurityContextRunData(securityContext, callBack, state);
			RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(SecurityContext.tryCode, SecurityContext.cleanupCode, userData);
		}

		// Token: 0x06001DC1 RID: 7617 RVA: 0x00067D0C File Offset: 0x00065F0C
		[SecurityCritical]
		internal static void runTryCode(object userData)
		{
			SecurityContext.SecurityContextRunData securityContextRunData = (SecurityContext.SecurityContextRunData)userData;
			securityContextRunData.scsw = SecurityContext.SetSecurityContext(securityContextRunData.sc, Thread.CurrentThread.GetExecutionContextReader().SecurityContext, true);
			securityContextRunData.callBack(securityContextRunData.state);
		}

		// Token: 0x06001DC2 RID: 7618 RVA: 0x00067D58 File Offset: 0x00065F58
		[SecurityCritical]
		[PrePrepareMethod]
		internal static void runFinallyCode(object userData, bool exceptionThrown)
		{
			SecurityContext.SecurityContextRunData securityContextRunData = (SecurityContext.SecurityContextRunData)userData;
			securityContextRunData.scsw.Undo();
		}

		// Token: 0x06001DC3 RID: 7619 RVA: 0x00067D78 File Offset: 0x00065F78
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static SecurityContextSwitcher SetSecurityContext(SecurityContext sc, SecurityContext.Reader prevSecurityContext, bool modifyCurrentExecutionContext)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return SecurityContext.SetSecurityContext(sc, prevSecurityContext, modifyCurrentExecutionContext, ref stackCrawlMark);
		}

		// Token: 0x06001DC4 RID: 7620 RVA: 0x00067D94 File Offset: 0x00065F94
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static SecurityContextSwitcher SetSecurityContext(SecurityContext sc, SecurityContext.Reader prevSecurityContext, bool modifyCurrentExecutionContext, ref StackCrawlMark stackMark)
		{
			SecurityContextDisableFlow disableFlow = sc._disableFlow;
			sc._disableFlow = SecurityContextDisableFlow.Nothing;
			SecurityContextSwitcher result = default(SecurityContextSwitcher);
			result.currSC = sc;
			result.prevSC = prevSecurityContext;
			if (modifyCurrentExecutionContext)
			{
				ExecutionContext mutableExecutionContext = Thread.CurrentThread.GetMutableExecutionContext();
				result.currEC = mutableExecutionContext;
				mutableExecutionContext.SecurityContext = sc;
			}
			if (sc != null)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					result.wic = null;
					if (!SecurityContext._LegacyImpersonationPolicy)
					{
						if (sc.WindowsIdentity != null)
						{
							result.wic = sc.WindowsIdentity.Impersonate(ref stackMark);
						}
						else if ((disableFlow & SecurityContextDisableFlow.WI) == SecurityContextDisableFlow.Nothing && prevSecurityContext.WindowsIdentity != null)
						{
							result.wic = WindowsIdentity.SafeRevertToSelf(ref stackMark);
						}
					}
					result.cssw = CompressedStack.SetCompressedStack(sc.CompressedStack, prevSecurityContext.CompressedStack);
				}
				catch
				{
					result.UndoNoThrow();
					throw;
				}
			}
			return result;
		}

		// Token: 0x06001DC5 RID: 7621 RVA: 0x00067E70 File Offset: 0x00066070
		[SecuritySafeCritical]
		public SecurityContext CreateCopy()
		{
			if (!this.isNewCapture)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotNewCaptureContext"));
			}
			SecurityContext securityContext = new SecurityContext();
			securityContext.isNewCapture = true;
			securityContext._disableFlow = this._disableFlow;
			if (this.WindowsIdentity != null)
			{
				securityContext._windowsIdentity = new WindowsIdentity(this.WindowsIdentity.AccessToken);
			}
			if (this._compressedStack != null)
			{
				securityContext._compressedStack = this._compressedStack.CreateCopy();
			}
			return securityContext;
		}

		// Token: 0x06001DC6 RID: 7622 RVA: 0x00067EF8 File Offset: 0x000660F8
		[SecuritySafeCritical]
		internal SecurityContext CreateMutableCopy()
		{
			SecurityContext securityContext = new SecurityContext();
			securityContext._disableFlow = this._disableFlow;
			if (this.WindowsIdentity != null)
			{
				securityContext._windowsIdentity = new WindowsIdentity(this.WindowsIdentity.AccessToken);
			}
			if (this._compressedStack != null)
			{
				securityContext._compressedStack = this._compressedStack.CreateCopy();
			}
			return securityContext;
		}

		// Token: 0x06001DC7 RID: 7623 RVA: 0x00067F5C File Offset: 0x0006615C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static SecurityContext Capture()
		{
			if (SecurityContext.IsFlowSuppressed())
			{
				return null;
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityContext securityContext = SecurityContext.Capture(Thread.CurrentThread.GetExecutionContextReader(), ref stackCrawlMark);
			if (securityContext == null)
			{
				securityContext = SecurityContext.CreateFullTrustSecurityContext();
			}
			return securityContext;
		}

		// Token: 0x06001DC8 RID: 7624 RVA: 0x00067F90 File Offset: 0x00066190
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static SecurityContext Capture(ExecutionContext.Reader currThreadEC, ref StackCrawlMark stackMark)
		{
			if (currThreadEC.SecurityContext.IsFlowSuppressed(SecurityContextDisableFlow.All))
			{
				return null;
			}
			if (SecurityContext.CurrentlyInDefaultFTSecurityContext(currThreadEC))
			{
				return null;
			}
			return SecurityContext.CaptureCore(currThreadEC, ref stackMark);
		}

		// Token: 0x06001DC9 RID: 7625 RVA: 0x00067FC8 File Offset: 0x000661C8
		[SecurityCritical]
		private static SecurityContext CaptureCore(ExecutionContext.Reader currThreadEC, ref StackCrawlMark stackMark)
		{
			SecurityContext securityContext = new SecurityContext();
			securityContext.isNewCapture = true;
			if (!SecurityContext.IsWindowsIdentityFlowSuppressed())
			{
				WindowsIdentity currentWI = SecurityContext.GetCurrentWI(currThreadEC);
				if (currentWI != null)
				{
					securityContext._windowsIdentity = new WindowsIdentity(currentWI.AccessToken);
				}
			}
			else
			{
				securityContext._disableFlow = SecurityContextDisableFlow.WI;
			}
			securityContext.CompressedStack = CompressedStack.GetCompressedStack(ref stackMark);
			return securityContext;
		}

		// Token: 0x06001DCA RID: 7626 RVA: 0x00068020 File Offset: 0x00066220
		[SecurityCritical]
		internal static SecurityContext CreateFullTrustSecurityContext()
		{
			SecurityContext securityContext = new SecurityContext();
			securityContext.isNewCapture = true;
			if (SecurityContext.IsWindowsIdentityFlowSuppressed())
			{
				securityContext._disableFlow = SecurityContextDisableFlow.WI;
			}
			securityContext.CompressedStack = new CompressedStack(null);
			return securityContext;
		}

		// Token: 0x1700034F RID: 847
		// (get) Token: 0x06001DCB RID: 7627 RVA: 0x00068059 File Offset: 0x00066259
		internal static bool AlwaysFlowImpersonationPolicy
		{
			get
			{
				return SecurityContext._alwaysFlowImpersonationPolicy;
			}
		}

		// Token: 0x06001DCC RID: 7628 RVA: 0x00068060 File Offset: 0x00066260
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static WindowsIdentity GetCurrentWI(ExecutionContext.Reader threadEC)
		{
			return SecurityContext.GetCurrentWI(threadEC, SecurityContext._alwaysFlowImpersonationPolicy);
		}

		// Token: 0x06001DCD RID: 7629 RVA: 0x00068070 File Offset: 0x00066270
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static WindowsIdentity GetCurrentWI(ExecutionContext.Reader threadEC, bool cachedAlwaysFlowImpersonationPolicy)
		{
			if (cachedAlwaysFlowImpersonationPolicy)
			{
				return WindowsIdentity.GetCurrentInternal(TokenAccessLevels.MaximumAllowed, true);
			}
			return threadEC.SecurityContext.WindowsIdentity;
		}

		// Token: 0x06001DCE RID: 7630 RVA: 0x0006809C File Offset: 0x0006629C
		[SecurityCritical]
		internal static void RestoreCurrentWI(ExecutionContext.Reader currentEC, ExecutionContext.Reader prevEC, WindowsIdentity targetWI, bool cachedAlwaysFlowImpersonationPolicy)
		{
			if (cachedAlwaysFlowImpersonationPolicy || prevEC.SecurityContext.WindowsIdentity != targetWI)
			{
				SecurityContext.RestoreCurrentWIInternal(targetWI);
			}
		}

		// Token: 0x06001DCF RID: 7631 RVA: 0x000680C4 File Offset: 0x000662C4
		[SecurityCritical]
		private static void RestoreCurrentWIInternal(WindowsIdentity targetWI)
		{
			int num = Win32.RevertToSelf();
			if (num < 0)
			{
				Environment.FailFast(Win32Native.GetMessage(num));
			}
			if (targetWI != null)
			{
				SafeAccessTokenHandle accessToken = targetWI.AccessToken;
				if (accessToken != null && !accessToken.IsInvalid)
				{
					num = Win32.ImpersonateLoggedOnUser(accessToken);
					if (num < 0)
					{
						Environment.FailFast(Win32Native.GetMessage(num));
					}
				}
			}
		}

		// Token: 0x06001DD0 RID: 7632 RVA: 0x00068111 File Offset: 0x00066311
		[SecurityCritical]
		internal bool IsDefaultFTSecurityContext()
		{
			return this.WindowsIdentity == null && (this.CompressedStack == null || this.CompressedStack.CompressedStackHandle == null);
		}

		// Token: 0x06001DD1 RID: 7633 RVA: 0x00068135 File Offset: 0x00066335
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool CurrentlyInDefaultFTSecurityContext(ExecutionContext.Reader threadEC)
		{
			return SecurityContext.IsDefaultThreadSecurityInfo() && SecurityContext.GetCurrentWI(threadEC) == null;
		}

		// Token: 0x06001DD2 RID: 7634
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern WindowsImpersonationFlowMode GetImpersonationFlowMode();

		// Token: 0x06001DD3 RID: 7635
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsDefaultThreadSecurityInfo();

		// Token: 0x04000A63 RID: 2659
		private static bool _LegacyImpersonationPolicy = SecurityContext.GetImpersonationFlowMode() == WindowsImpersonationFlowMode.IMP_NOFLOW;

		// Token: 0x04000A64 RID: 2660
		private static bool _alwaysFlowImpersonationPolicy = SecurityContext.GetImpersonationFlowMode() == WindowsImpersonationFlowMode.IMP_ALWAYSFLOW;

		// Token: 0x04000A65 RID: 2661
		private ExecutionContext _executionContext;

		// Token: 0x04000A66 RID: 2662
		private volatile WindowsIdentity _windowsIdentity;

		// Token: 0x04000A67 RID: 2663
		private volatile CompressedStack _compressedStack;

		// Token: 0x04000A68 RID: 2664
		private static volatile SecurityContext _fullTrustSC;

		// Token: 0x04000A69 RID: 2665
		internal volatile bool isNewCapture;

		// Token: 0x04000A6A RID: 2666
		internal volatile SecurityContextDisableFlow _disableFlow;

		// Token: 0x04000A6B RID: 2667
		internal static volatile RuntimeHelpers.TryCode tryCode;

		// Token: 0x04000A6C RID: 2668
		internal static volatile RuntimeHelpers.CleanupCode cleanupCode;

		// Token: 0x02000B2D RID: 2861
		internal struct Reader
		{
			// Token: 0x06006B61 RID: 27489 RVA: 0x001737B2 File Offset: 0x001719B2
			public Reader(SecurityContext sc)
			{
				this.m_sc = sc;
			}

			// Token: 0x06006B62 RID: 27490 RVA: 0x001737BB File Offset: 0x001719BB
			public SecurityContext DangerousGetRawSecurityContext()
			{
				return this.m_sc;
			}

			// Token: 0x1700121E RID: 4638
			// (get) Token: 0x06006B63 RID: 27491 RVA: 0x001737C3 File Offset: 0x001719C3
			public bool IsNull
			{
				get
				{
					return this.m_sc == null;
				}
			}

			// Token: 0x06006B64 RID: 27492 RVA: 0x001737CE File Offset: 0x001719CE
			public bool IsSame(SecurityContext sc)
			{
				return this.m_sc == sc;
			}

			// Token: 0x06006B65 RID: 27493 RVA: 0x001737D9 File Offset: 0x001719D9
			public bool IsSame(SecurityContext.Reader sc)
			{
				return this.m_sc == sc.m_sc;
			}

			// Token: 0x06006B66 RID: 27494 RVA: 0x001737E9 File Offset: 0x001719E9
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IsFlowSuppressed(SecurityContextDisableFlow flags)
			{
				return this.m_sc != null && (this.m_sc._disableFlow & flags) == flags;
			}

			// Token: 0x1700121F RID: 4639
			// (get) Token: 0x06006B67 RID: 27495 RVA: 0x00173807 File Offset: 0x00171A07
			public CompressedStack CompressedStack
			{
				get
				{
					if (!this.IsNull)
					{
						return this.m_sc.CompressedStack;
					}
					return null;
				}
			}

			// Token: 0x17001220 RID: 4640
			// (get) Token: 0x06006B68 RID: 27496 RVA: 0x0017381E File Offset: 0x00171A1E
			public WindowsIdentity WindowsIdentity
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					if (!this.IsNull)
					{
						return this.m_sc.WindowsIdentity;
					}
					return null;
				}
			}

			// Token: 0x04003340 RID: 13120
			private SecurityContext m_sc;
		}

		// Token: 0x02000B2E RID: 2862
		internal class SecurityContextRunData
		{
			// Token: 0x06006B69 RID: 27497 RVA: 0x00173835 File Offset: 0x00171A35
			internal SecurityContextRunData(SecurityContext securityContext, ContextCallback cb, object state)
			{
				this.sc = securityContext;
				this.callBack = cb;
				this.state = state;
				this.scsw = default(SecurityContextSwitcher);
			}

			// Token: 0x04003341 RID: 13121
			internal SecurityContext sc;

			// Token: 0x04003342 RID: 13122
			internal ContextCallback callBack;

			// Token: 0x04003343 RID: 13123
			internal object state;

			// Token: 0x04003344 RID: 13124
			internal SecurityContextSwitcher scsw;
		}
	}
}
