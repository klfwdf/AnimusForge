using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004F1 RID: 1265
	[Serializable]
	public sealed class CompressedStack : ISerializable
	{
		// Token: 0x17000909 RID: 2313
		// (get) Token: 0x06003BAE RID: 15278 RVA: 0x000E29C6 File Offset: 0x000E0BC6
		// (set) Token: 0x06003BAF RID: 15279 RVA: 0x000E29CE File Offset: 0x000E0BCE
		internal bool CanSkipEvaluation
		{
			get
			{
				return this.m_canSkipEvaluation;
			}
			private set
			{
				this.m_canSkipEvaluation = value;
			}
		}

		// Token: 0x1700090A RID: 2314
		// (get) Token: 0x06003BB0 RID: 15280 RVA: 0x000E29D7 File Offset: 0x000E0BD7
		internal PermissionListSet PLS
		{
			get
			{
				return this.m_pls;
			}
		}

		// Token: 0x06003BB1 RID: 15281 RVA: 0x000E29E1 File Offset: 0x000E0BE1
		[SecurityCritical]
		internal CompressedStack(SafeCompressedStackHandle csHandle)
		{
			this.m_csHandle = csHandle;
		}

		// Token: 0x06003BB2 RID: 15282 RVA: 0x000E29F2 File Offset: 0x000E0BF2
		[SecurityCritical]
		private CompressedStack(SafeCompressedStackHandle csHandle, PermissionListSet pls)
		{
			this.m_csHandle = csHandle;
			this.m_pls = pls;
		}

		// Token: 0x06003BB3 RID: 15283 RVA: 0x000E2A0C File Offset: 0x000E0C0C
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.CompleteConstruction(null);
			info.AddValue("PLS", this.m_pls);
		}

		// Token: 0x06003BB4 RID: 15284 RVA: 0x000E2A36 File Offset: 0x000E0C36
		private CompressedStack(SerializationInfo info, StreamingContext context)
		{
			this.m_pls = (PermissionListSet)info.GetValue("PLS", typeof(PermissionListSet));
		}

		// Token: 0x1700090B RID: 2315
		// (get) Token: 0x06003BB5 RID: 15285 RVA: 0x000E2A60 File Offset: 0x000E0C60
		// (set) Token: 0x06003BB6 RID: 15286 RVA: 0x000E2A6A File Offset: 0x000E0C6A
		internal SafeCompressedStackHandle CompressedStackHandle
		{
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.m_csHandle;
			}
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			private set
			{
				this.m_csHandle = value;
			}
		}

		// Token: 0x06003BB7 RID: 15287 RVA: 0x000E2A78 File Offset: 0x000E0C78
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static CompressedStack GetCompressedStack()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return CompressedStack.GetCompressedStack(ref stackCrawlMark);
		}

		// Token: 0x06003BB8 RID: 15288 RVA: 0x000E2A90 File Offset: 0x000E0C90
		[SecurityCritical]
		internal static CompressedStack GetCompressedStack(ref StackCrawlMark stackMark)
		{
			CompressedStack innerCS = null;
			CompressedStack compressedStack;
			if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
			{
				compressedStack = new CompressedStack(null);
				compressedStack.CanSkipEvaluation = true;
			}
			else if (CodeAccessSecurityEngine.AllDomainsHomogeneousWithNoStackModifiers())
			{
				compressedStack = new CompressedStack(CompressedStack.GetDelayedCompressedStack(ref stackMark, false));
				compressedStack.m_pls = PermissionListSet.CreateCompressedState_HG();
			}
			else
			{
				compressedStack = new CompressedStack(null);
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					compressedStack.CompressedStackHandle = CompressedStack.GetDelayedCompressedStack(ref stackMark, true);
					if (compressedStack.CompressedStackHandle != null && CompressedStack.IsImmediateCompletionCandidate(compressedStack.CompressedStackHandle, out innerCS))
					{
						try
						{
							compressedStack.CompleteConstruction(innerCS);
						}
						finally
						{
							CompressedStack.DestroyDCSList(compressedStack.CompressedStackHandle);
						}
					}
				}
			}
			return compressedStack;
		}

		// Token: 0x06003BB9 RID: 15289 RVA: 0x000E2B40 File Offset: 0x000E0D40
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static CompressedStack Capture()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return CompressedStack.GetCompressedStack(ref stackCrawlMark);
		}

		// Token: 0x06003BBA RID: 15290 RVA: 0x000E2B58 File Offset: 0x000E0D58
		[SecurityCritical]
		public static void Run(CompressedStack compressedStack, ContextCallback callback, object state)
		{
			if (compressedStack == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamNull"), "compressedStack");
			}
			if (CompressedStack.cleanupCode == null)
			{
				CompressedStack.tryCode = new RuntimeHelpers.TryCode(CompressedStack.runTryCode);
				CompressedStack.cleanupCode = new RuntimeHelpers.CleanupCode(CompressedStack.runFinallyCode);
			}
			CompressedStack.CompressedStackRunData userData = new CompressedStack.CompressedStackRunData(compressedStack, callback, state);
			RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(CompressedStack.tryCode, CompressedStack.cleanupCode, userData);
		}

		// Token: 0x06003BBB RID: 15291 RVA: 0x000E2BCC File Offset: 0x000E0DCC
		[SecurityCritical]
		internal static void runTryCode(object userData)
		{
			CompressedStack.CompressedStackRunData compressedStackRunData = (CompressedStack.CompressedStackRunData)userData;
			compressedStackRunData.cssw = CompressedStack.SetCompressedStack(compressedStackRunData.cs, CompressedStack.GetCompressedStackThread());
			compressedStackRunData.callBack(compressedStackRunData.state);
		}

		// Token: 0x06003BBC RID: 15292 RVA: 0x000E2C08 File Offset: 0x000E0E08
		[SecurityCritical]
		[PrePrepareMethod]
		internal static void runFinallyCode(object userData, bool exceptionThrown)
		{
			CompressedStack.CompressedStackRunData compressedStackRunData = (CompressedStack.CompressedStackRunData)userData;
			compressedStackRunData.cssw.Undo();
		}

		// Token: 0x06003BBD RID: 15293 RVA: 0x000E2C28 File Offset: 0x000E0E28
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static CompressedStackSwitcher SetCompressedStack(CompressedStack cs, CompressedStack prevCS)
		{
			CompressedStackSwitcher result = default(CompressedStackSwitcher);
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					CompressedStack.SetCompressedStackThread(cs);
					result.prev_CS = prevCS;
					result.curr_CS = cs;
					result.prev_ADStack = CompressedStack.SetAppDomainStack(cs);
				}
			}
			catch
			{
				result.UndoNoThrow();
				throw;
			}
			return result;
		}

		// Token: 0x06003BBE RID: 15294 RVA: 0x000E2C98 File Offset: 0x000E0E98
		[SecuritySafeCritical]
		[ComVisible(false)]
		public CompressedStack CreateCopy()
		{
			return new CompressedStack(this.m_csHandle, this.m_pls);
		}

		// Token: 0x06003BBF RID: 15295 RVA: 0x000E2CAF File Offset: 0x000E0EAF
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static IntPtr SetAppDomainStack(CompressedStack cs)
		{
			return Thread.CurrentThread.SetAppDomainStack((cs == null) ? null : cs.CompressedStackHandle);
		}

		// Token: 0x06003BC0 RID: 15296 RVA: 0x000E2CC7 File Offset: 0x000E0EC7
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static void RestoreAppDomainStack(IntPtr appDomainStack)
		{
			Thread.CurrentThread.RestoreAppDomainStack(appDomainStack);
		}

		// Token: 0x06003BC1 RID: 15297 RVA: 0x000E2CD4 File Offset: 0x000E0ED4
		[SecurityCritical]
		internal static CompressedStack GetCompressedStackThread()
		{
			return Thread.CurrentThread.GetExecutionContextReader().SecurityContext.CompressedStack;
		}

		// Token: 0x06003BC2 RID: 15298 RVA: 0x000E2CFC File Offset: 0x000E0EFC
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal static void SetCompressedStackThread(CompressedStack cs)
		{
			Thread currentThread = Thread.CurrentThread;
			if (currentThread.GetExecutionContextReader().SecurityContext.CompressedStack != cs)
			{
				ExecutionContext mutableExecutionContext = currentThread.GetMutableExecutionContext();
				if (mutableExecutionContext.SecurityContext != null)
				{
					mutableExecutionContext.SecurityContext.CompressedStack = cs;
					return;
				}
				if (cs != null)
				{
					mutableExecutionContext.SecurityContext = new SecurityContext
					{
						CompressedStack = cs
					};
				}
			}
		}

		// Token: 0x06003BC3 RID: 15299 RVA: 0x000E2D5E File Offset: 0x000E0F5E
		[SecurityCritical]
		internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
		{
			this.CompleteConstruction(null);
			if (this.PLS == null)
			{
				return false;
			}
			this.PLS.CheckDemand(demand, permToken, rmh);
			return false;
		}

		// Token: 0x06003BC4 RID: 15300 RVA: 0x000E2D81 File Offset: 0x000E0F81
		[SecurityCritical]
		internal bool CheckDemandNoHalt(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
		{
			this.CompleteConstruction(null);
			return this.PLS == null || this.PLS.CheckDemand(demand, permToken, rmh);
		}

		// Token: 0x06003BC5 RID: 15301 RVA: 0x000E2DA2 File Offset: 0x000E0FA2
		[SecurityCritical]
		internal bool CheckSetDemand(PermissionSet pset, RuntimeMethodHandleInternal rmh)
		{
			this.CompleteConstruction(null);
			return this.PLS != null && this.PLS.CheckSetDemand(pset, rmh);
		}

		// Token: 0x06003BC6 RID: 15302 RVA: 0x000E2DC2 File Offset: 0x000E0FC2
		[SecurityCritical]
		internal bool CheckSetDemandWithModificationNoHalt(PermissionSet pset, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
		{
			alteredDemandSet = null;
			this.CompleteConstruction(null);
			return this.PLS == null || this.PLS.CheckSetDemandWithModification(pset, out alteredDemandSet, rmh);
		}

		// Token: 0x06003BC7 RID: 15303 RVA: 0x000E2DE6 File Offset: 0x000E0FE6
		[SecurityCritical]
		internal void DemandFlagsOrGrantSet(int flags, PermissionSet grantSet)
		{
			this.CompleteConstruction(null);
			if (this.PLS == null)
			{
				return;
			}
			this.PLS.DemandFlagsOrGrantSet(flags, grantSet);
		}

		// Token: 0x06003BC8 RID: 15304 RVA: 0x000E2E05 File Offset: 0x000E1005
		[SecurityCritical]
		internal void GetZoneAndOrigin(ArrayList zoneList, ArrayList originList, PermissionToken zoneToken, PermissionToken originToken)
		{
			this.CompleteConstruction(null);
			if (this.PLS != null)
			{
				this.PLS.GetZoneAndOrigin(zoneList, originList, zoneToken, originToken);
			}
		}

		// Token: 0x06003BC9 RID: 15305 RVA: 0x000E2E28 File Offset: 0x000E1028
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal void CompleteConstruction(CompressedStack innerCS)
		{
			if (this.PLS != null)
			{
				return;
			}
			PermissionListSet pls = PermissionListSet.CreateCompressedState(this, innerCS);
			lock (this)
			{
				if (this.PLS == null)
				{
					this.m_pls = pls;
				}
			}
		}

		// Token: 0x06003BCA RID: 15306
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern SafeCompressedStackHandle GetDelayedCompressedStack(ref StackCrawlMark stackMark, bool walkStack);

		// Token: 0x06003BCB RID: 15307
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void DestroyDelayedCompressedStack(IntPtr unmanagedCompressedStack);

		// Token: 0x06003BCC RID: 15308
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void DestroyDCSList(SafeCompressedStackHandle compressedStack);

		// Token: 0x06003BCD RID: 15309
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetDCSCount(SafeCompressedStackHandle compressedStack);

		// Token: 0x06003BCE RID: 15310
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsImmediateCompletionCandidate(SafeCompressedStackHandle compressedStack, out CompressedStack innerCS);

		// Token: 0x06003BCF RID: 15311
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern DomainCompressedStack GetDomainCompressedStack(SafeCompressedStackHandle compressedStack, int index);

		// Token: 0x06003BD0 RID: 15312
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void GetHomogeneousPLS(PermissionListSet hgPLS);

		// Token: 0x04001979 RID: 6521
		private volatile PermissionListSet m_pls;

		// Token: 0x0400197A RID: 6522
		[SecurityCritical]
		private volatile SafeCompressedStackHandle m_csHandle;

		// Token: 0x0400197B RID: 6523
		private bool m_canSkipEvaluation;

		// Token: 0x0400197C RID: 6524
		internal static volatile RuntimeHelpers.TryCode tryCode;

		// Token: 0x0400197D RID: 6525
		internal static volatile RuntimeHelpers.CleanupCode cleanupCode;

		// Token: 0x02000BED RID: 3053
		internal class CompressedStackRunData
		{
			// Token: 0x06006F55 RID: 28501 RVA: 0x0017FAD0 File Offset: 0x0017DCD0
			internal CompressedStackRunData(CompressedStack cs, ContextCallback cb, object state)
			{
				this.cs = cs;
				this.callBack = cb;
				this.state = state;
				this.cssw = default(CompressedStackSwitcher);
			}

			// Token: 0x0400360E RID: 13838
			internal CompressedStack cs;

			// Token: 0x0400360F RID: 13839
			internal ContextCallback callBack;

			// Token: 0x04003610 RID: 13840
			internal object state;

			// Token: 0x04003611 RID: 13841
			internal CompressedStackSwitcher cssw;
		}
	}
}
