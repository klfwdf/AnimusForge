using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x02000510 RID: 1296
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public sealed class ReaderWriterLock : CriticalFinalizerObject
	{
		// Token: 0x06003CF2 RID: 15602 RVA: 0x000E5FE6 File Offset: 0x000E41E6
		[SecuritySafeCritical]
		public ReaderWriterLock()
		{
			this.PrivateInitialize();
		}

		// Token: 0x06003CF3 RID: 15603 RVA: 0x000E5FF4 File Offset: 0x000E41F4
		[SecuritySafeCritical]
		~ReaderWriterLock()
		{
			this.PrivateDestruct();
		}

		// Token: 0x17000923 RID: 2339
		// (get) Token: 0x06003CF4 RID: 15604 RVA: 0x000E6020 File Offset: 0x000E4220
		public bool IsReaderLockHeld
		{
			[SecuritySafeCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.PrivateGetIsReaderLockHeld();
			}
		}

		// Token: 0x17000924 RID: 2340
		// (get) Token: 0x06003CF5 RID: 15605 RVA: 0x000E6028 File Offset: 0x000E4228
		public bool IsWriterLockHeld
		{
			[SecuritySafeCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.PrivateGetIsWriterLockHeld();
			}
		}

		// Token: 0x17000925 RID: 2341
		// (get) Token: 0x06003CF6 RID: 15606 RVA: 0x000E6030 File Offset: 0x000E4230
		public int WriterSeqNum
		{
			[SecuritySafeCritical]
			get
			{
				return this.PrivateGetWriterSeqNum();
			}
		}

		// Token: 0x06003CF7 RID: 15607
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void AcquireReaderLockInternal(int millisecondsTimeout);

		// Token: 0x06003CF8 RID: 15608 RVA: 0x000E6038 File Offset: 0x000E4238
		[SecuritySafeCritical]
		public void AcquireReaderLock(int millisecondsTimeout)
		{
			this.AcquireReaderLockInternal(millisecondsTimeout);
		}

		// Token: 0x06003CF9 RID: 15609 RVA: 0x000E6044 File Offset: 0x000E4244
		[SecuritySafeCritical]
		public void AcquireReaderLock(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			this.AcquireReaderLockInternal((int)num);
		}

		// Token: 0x06003CFA RID: 15610
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void AcquireWriterLockInternal(int millisecondsTimeout);

		// Token: 0x06003CFB RID: 15611 RVA: 0x000E6085 File Offset: 0x000E4285
		[SecuritySafeCritical]
		public void AcquireWriterLock(int millisecondsTimeout)
		{
			this.AcquireWriterLockInternal(millisecondsTimeout);
		}

		// Token: 0x06003CFC RID: 15612 RVA: 0x000E6090 File Offset: 0x000E4290
		[SecuritySafeCritical]
		public void AcquireWriterLock(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			this.AcquireWriterLockInternal((int)num);
		}

		// Token: 0x06003CFD RID: 15613
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void ReleaseReaderLockInternal();

		// Token: 0x06003CFE RID: 15614 RVA: 0x000E60D1 File Offset: 0x000E42D1
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void ReleaseReaderLock()
		{
			this.ReleaseReaderLockInternal();
		}

		// Token: 0x06003CFF RID: 15615
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void ReleaseWriterLockInternal();

		// Token: 0x06003D00 RID: 15616 RVA: 0x000E60D9 File Offset: 0x000E42D9
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void ReleaseWriterLock()
		{
			this.ReleaseWriterLockInternal();
		}

		// Token: 0x06003D01 RID: 15617 RVA: 0x000E60E4 File Offset: 0x000E42E4
		[SecuritySafeCritical]
		public LockCookie UpgradeToWriterLock(int millisecondsTimeout)
		{
			LockCookie result = default(LockCookie);
			this.FCallUpgradeToWriterLock(ref result, millisecondsTimeout);
			return result;
		}

		// Token: 0x06003D02 RID: 15618
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void FCallUpgradeToWriterLock(ref LockCookie result, int millisecondsTimeout);

		// Token: 0x06003D03 RID: 15619 RVA: 0x000E6104 File Offset: 0x000E4304
		public LockCookie UpgradeToWriterLock(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return this.UpgradeToWriterLock((int)num);
		}

		// Token: 0x06003D04 RID: 15620
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void DowngradeFromWriterLockInternal(ref LockCookie lockCookie);

		// Token: 0x06003D05 RID: 15621 RVA: 0x000E6145 File Offset: 0x000E4345
		[SecuritySafeCritical]
		public void DowngradeFromWriterLock(ref LockCookie lockCookie)
		{
			this.DowngradeFromWriterLockInternal(ref lockCookie);
		}

		// Token: 0x06003D06 RID: 15622 RVA: 0x000E6150 File Offset: 0x000E4350
		[SecuritySafeCritical]
		public LockCookie ReleaseLock()
		{
			LockCookie result = default(LockCookie);
			this.FCallReleaseLock(ref result);
			return result;
		}

		// Token: 0x06003D07 RID: 15623
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void FCallReleaseLock(ref LockCookie result);

		// Token: 0x06003D08 RID: 15624
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void RestoreLockInternal(ref LockCookie lockCookie);

		// Token: 0x06003D09 RID: 15625 RVA: 0x000E616E File Offset: 0x000E436E
		[SecuritySafeCritical]
		public void RestoreLock(ref LockCookie lockCookie)
		{
			this.RestoreLockInternal(ref lockCookie);
		}

		// Token: 0x06003D0A RID: 15626
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool PrivateGetIsReaderLockHeld();

		// Token: 0x06003D0B RID: 15627
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool PrivateGetIsWriterLockHeld();

		// Token: 0x06003D0C RID: 15628
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int PrivateGetWriterSeqNum();

		// Token: 0x06003D0D RID: 15629
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool AnyWritersSince(int seqNum);

		// Token: 0x06003D0E RID: 15630
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void PrivateInitialize();

		// Token: 0x06003D0F RID: 15631
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void PrivateDestruct();

		// Token: 0x040019DB RID: 6619
		private IntPtr _hWriterEvent;

		// Token: 0x040019DC RID: 6620
		private IntPtr _hReaderEvent;

		// Token: 0x040019DD RID: 6621
		private IntPtr _hObjectHandle;

		// Token: 0x040019DE RID: 6622
		private int _dwState;

		// Token: 0x040019DF RID: 6623
		private int _dwULockID;

		// Token: 0x040019E0 RID: 6624
		private int _dwLLockID;

		// Token: 0x040019E1 RID: 6625
		private int _dwWriterID;

		// Token: 0x040019E2 RID: 6626
		private int _dwWriterSeqNum;

		// Token: 0x040019E3 RID: 6627
		private short _wWriterLevel;
	}
}
