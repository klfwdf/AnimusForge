using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x020000E9 RID: 233
	[__DynamicallyInvokable]
	public static class GC
	{
		// Token: 0x06000E91 RID: 3729
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetGCLatencyMode();

		// Token: 0x06000E92 RID: 3730
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int SetGCLatencyMode(int newLatencyMode);

		// Token: 0x06000E93 RID: 3731
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern int _StartNoGCRegion(long totalSize, bool lohSizeKnown, long lohSize, bool disallowFullBlockingGC);

		// Token: 0x06000E94 RID: 3732
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern int _EndNoGCRegion();

		// Token: 0x06000E95 RID: 3733
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetLOHCompactionMode();

		// Token: 0x06000E96 RID: 3734
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void SetLOHCompactionMode(int newLOHCompactionyMode);

		// Token: 0x06000E97 RID: 3735
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int GetGenerationWR(IntPtr handle);

		// Token: 0x06000E98 RID: 3736
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern long GetTotalMemory();

		// Token: 0x06000E99 RID: 3737
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void _Collect(int generation, int mode);

		// Token: 0x06000E9A RID: 3738
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int GetMaxGeneration();

		// Token: 0x06000E9B RID: 3739
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int _CollectionCount(int generation, int getSpecialGCCount);

		// Token: 0x06000E9C RID: 3740
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsServerGC();

		// Token: 0x06000E9D RID: 3741
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void _AddMemoryPressure(ulong bytesAllocated);

		// Token: 0x06000E9E RID: 3742
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void _RemoveMemoryPressure(ulong bytesAllocated);

		// Token: 0x06000E9F RID: 3743 RVA: 0x0002CD48 File Offset: 0x0002AF48
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void AddMemoryPressure(long bytesAllocated)
		{
			if (bytesAllocated <= 0L)
			{
				throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (4 == IntPtr.Size && bytesAllocated > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("pressure", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegInt32"));
			}
			GC._AddMemoryPressure((ulong)bytesAllocated);
		}

		// Token: 0x06000EA0 RID: 3744 RVA: 0x0002CD9C File Offset: 0x0002AF9C
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void RemoveMemoryPressure(long bytesAllocated)
		{
			if (bytesAllocated <= 0L)
			{
				throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (4 == IntPtr.Size && bytesAllocated > 2147483647L)
			{
				throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegInt32"));
			}
			GC._RemoveMemoryPressure((ulong)bytesAllocated);
		}

		// Token: 0x06000EA1 RID: 3745
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern int GetGeneration(object obj);

		// Token: 0x06000EA2 RID: 3746 RVA: 0x0002CDEF File Offset: 0x0002AFEF
		[__DynamicallyInvokable]
		public static void Collect(int generation)
		{
			GC.Collect(generation, GCCollectionMode.Default);
		}

		// Token: 0x06000EA3 RID: 3747 RVA: 0x0002CDF8 File Offset: 0x0002AFF8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void Collect()
		{
			GC._Collect(-1, 2);
		}

		// Token: 0x06000EA4 RID: 3748 RVA: 0x0002CE01 File Offset: 0x0002B001
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void Collect(int generation, GCCollectionMode mode)
		{
			GC.Collect(generation, mode, true);
		}

		// Token: 0x06000EA5 RID: 3749 RVA: 0x0002CE0B File Offset: 0x0002B00B
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void Collect(int generation, GCCollectionMode mode, bool blocking)
		{
			GC.Collect(generation, mode, blocking, false);
		}

		// Token: 0x06000EA6 RID: 3750 RVA: 0x0002CE18 File Offset: 0x0002B018
		[SecuritySafeCritical]
		public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
		{
			if (generation < 0)
			{
				throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (mode < GCCollectionMode.Default || mode > GCCollectionMode.Optimized)
			{
				throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
			}
			int num = 0;
			if (mode == GCCollectionMode.Optimized)
			{
				num |= 4;
			}
			if (compacting)
			{
				num |= 8;
			}
			if (blocking)
			{
				num |= 2;
			}
			else if (!compacting)
			{
				num |= 1;
			}
			GC._Collect(generation, num);
		}

		// Token: 0x06000EA7 RID: 3751 RVA: 0x0002CE7E File Offset: 0x0002B07E
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static int CollectionCount(int generation)
		{
			if (generation < 0)
			{
				throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			return GC._CollectionCount(generation, 0);
		}

		// Token: 0x06000EA8 RID: 3752 RVA: 0x0002CEA0 File Offset: 0x0002B0A0
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static int CollectionCount(int generation, bool getSpecialGCCount)
		{
			if (generation < 0)
			{
				throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			return GC._CollectionCount(generation, getSpecialGCCount ? 1 : 0);
		}

		// Token: 0x06000EA9 RID: 3753 RVA: 0x0002CEC8 File Offset: 0x0002B0C8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void KeepAlive(object obj)
		{
		}

		// Token: 0x06000EAA RID: 3754 RVA: 0x0002CECC File Offset: 0x0002B0CC
		[SecuritySafeCritical]
		public static int GetGeneration(WeakReference wo)
		{
			int generationWR = GC.GetGenerationWR(wo.m_handle);
			GC.KeepAlive(wo);
			return generationWR;
		}

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x06000EAB RID: 3755 RVA: 0x0002CEEC File Offset: 0x0002B0EC
		[__DynamicallyInvokable]
		public static int MaxGeneration
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return GC.GetMaxGeneration();
			}
		}

		// Token: 0x06000EAC RID: 3756
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void _WaitForPendingFinalizers();

		// Token: 0x06000EAD RID: 3757 RVA: 0x0002CEF3 File Offset: 0x0002B0F3
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void WaitForPendingFinalizers()
		{
			GC._WaitForPendingFinalizers();
		}

		// Token: 0x06000EAE RID: 3758
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _SuppressFinalize(object o);

		// Token: 0x06000EAF RID: 3759 RVA: 0x0002CEFA File Offset: 0x0002B0FA
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void SuppressFinalize(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			GC._SuppressFinalize(obj);
		}

		// Token: 0x06000EB0 RID: 3760
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _ReRegisterForFinalize(object o);

		// Token: 0x06000EB1 RID: 3761 RVA: 0x0002CF10 File Offset: 0x0002B110
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void ReRegisterForFinalize(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			GC._ReRegisterForFinalize(obj);
		}

		// Token: 0x06000EB2 RID: 3762 RVA: 0x0002CF28 File Offset: 0x0002B128
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static long GetTotalMemory(bool forceFullCollection)
		{
			long num = GC.GetTotalMemory();
			if (!forceFullCollection)
			{
				return num;
			}
			int num2 = 20;
			long num3 = num;
			float num4;
			do
			{
				GC.WaitForPendingFinalizers();
				GC.Collect();
				num = num3;
				num3 = GC.GetTotalMemory();
				num4 = (float)(num3 - num) / (float)num;
			}
			while (num2-- > 0 && (-0.05 >= (double)num4 || (double)num4 >= 0.05));
			return num3;
		}

		// Token: 0x06000EB3 RID: 3763
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern long _GetAllocatedBytesForCurrentThread();

		// Token: 0x06000EB4 RID: 3764 RVA: 0x0002CF82 File Offset: 0x0002B182
		[SecuritySafeCritical]
		public static long GetAllocatedBytesForCurrentThread()
		{
			return GC._GetAllocatedBytesForCurrentThread();
		}

		// Token: 0x06000EB5 RID: 3765
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);

		// Token: 0x06000EB6 RID: 3766
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool _CancelFullGCNotification();

		// Token: 0x06000EB7 RID: 3767
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int _WaitForFullGCApproach(int millisecondsTimeout);

		// Token: 0x06000EB8 RID: 3768
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int _WaitForFullGCComplete(int millisecondsTimeout);

		// Token: 0x06000EB9 RID: 3769 RVA: 0x0002CF8C File Offset: 0x0002B18C
		[SecurityCritical]
		public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
		{
			if (maxGenerationThreshold <= 0 || maxGenerationThreshold >= 100)
			{
				throw new ArgumentOutOfRangeException("maxGenerationThreshold", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, 99));
			}
			if (largeObjectHeapThreshold <= 0 || largeObjectHeapThreshold >= 100)
			{
				throw new ArgumentOutOfRangeException("largeObjectHeapThreshold", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, 99));
			}
			if (!GC._RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotWithConcurrentGC"));
			}
		}

		// Token: 0x06000EBA RID: 3770 RVA: 0x0002D01C File Offset: 0x0002B21C
		[SecurityCritical]
		public static void CancelFullGCNotification()
		{
			if (!GC._CancelFullGCNotification())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotWithConcurrentGC"));
			}
		}

		// Token: 0x06000EBB RID: 3771 RVA: 0x0002D035 File Offset: 0x0002B235
		[SecurityCritical]
		public static GCNotificationStatus WaitForFullGCApproach()
		{
			return (GCNotificationStatus)GC._WaitForFullGCApproach(-1);
		}

		// Token: 0x06000EBC RID: 3772 RVA: 0x0002D03D File Offset: 0x0002B23D
		[SecurityCritical]
		public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return (GCNotificationStatus)GC._WaitForFullGCApproach(millisecondsTimeout);
		}

		// Token: 0x06000EBD RID: 3773 RVA: 0x0002D05E File Offset: 0x0002B25E
		[SecurityCritical]
		public static GCNotificationStatus WaitForFullGCComplete()
		{
			return (GCNotificationStatus)GC._WaitForFullGCComplete(-1);
		}

		// Token: 0x06000EBE RID: 3774 RVA: 0x0002D066 File Offset: 0x0002B266
		[SecurityCritical]
		public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
		{
			if (millisecondsTimeout < -1)
			{
				throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
			}
			return (GCNotificationStatus)GC._WaitForFullGCComplete(millisecondsTimeout);
		}

		// Token: 0x06000EBF RID: 3775 RVA: 0x0002D088 File Offset: 0x0002B288
		[SecurityCritical]
		private static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
		{
			GC.StartNoGCRegionStatus startNoGCRegionStatus = (GC.StartNoGCRegionStatus)GC._StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);
			if (startNoGCRegionStatus == GC.StartNoGCRegionStatus.AmountTooLarge)
			{
				throw new ArgumentOutOfRangeException("totalSize", "totalSize is too large. For more information about setting the maximum size, see \"Latency Modes\" in http://go.microsoft.com/fwlink/?LinkId=522706");
			}
			if (startNoGCRegionStatus == GC.StartNoGCRegionStatus.AlreadyInProgress)
			{
				throw new InvalidOperationException("The NoGCRegion mode was already in progress");
			}
			return startNoGCRegionStatus != GC.StartNoGCRegionStatus.NotEnoughMemory;
		}

		// Token: 0x06000EC0 RID: 3776 RVA: 0x0002D0C9 File Offset: 0x0002B2C9
		[SecurityCritical]
		public static bool TryStartNoGCRegion(long totalSize)
		{
			return GC.StartNoGCRegionWorker(totalSize, false, 0L, false);
		}

		// Token: 0x06000EC1 RID: 3777 RVA: 0x0002D0D5 File Offset: 0x0002B2D5
		[SecurityCritical]
		public static bool TryStartNoGCRegion(long totalSize, long lohSize)
		{
			return GC.StartNoGCRegionWorker(totalSize, true, lohSize, false);
		}

		// Token: 0x06000EC2 RID: 3778 RVA: 0x0002D0E0 File Offset: 0x0002B2E0
		[SecurityCritical]
		public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
		{
			return GC.StartNoGCRegionWorker(totalSize, false, 0L, disallowFullBlockingGC);
		}

		// Token: 0x06000EC3 RID: 3779 RVA: 0x0002D0EC File Offset: 0x0002B2EC
		[SecurityCritical]
		public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
		{
			return GC.StartNoGCRegionWorker(totalSize, true, lohSize, disallowFullBlockingGC);
		}

		// Token: 0x06000EC4 RID: 3780 RVA: 0x0002D0F8 File Offset: 0x0002B2F8
		[SecurityCritical]
		private static GC.EndNoGCRegionStatus EndNoGCRegionWorker()
		{
			GC.EndNoGCRegionStatus endNoGCRegionStatus = (GC.EndNoGCRegionStatus)GC._EndNoGCRegion();
			if (endNoGCRegionStatus == GC.EndNoGCRegionStatus.NotInProgress)
			{
				throw new InvalidOperationException("NoGCRegion mode must be set");
			}
			if (endNoGCRegionStatus == GC.EndNoGCRegionStatus.GCInduced)
			{
				throw new InvalidOperationException("Garbage collection was induced in NoGCRegion mode");
			}
			if (endNoGCRegionStatus == GC.EndNoGCRegionStatus.AllocationExceeded)
			{
				throw new InvalidOperationException("Allocated memory exceeds specified memory for NoGCRegion mode");
			}
			return GC.EndNoGCRegionStatus.Succeeded;
		}

		// Token: 0x06000EC5 RID: 3781 RVA: 0x0002D139 File Offset: 0x0002B339
		[SecurityCritical]
		public static void EndNoGCRegion()
		{
			GC.EndNoGCRegionWorker();
		}

		// Token: 0x02000AE8 RID: 2792
		private enum StartNoGCRegionStatus
		{
			// Token: 0x0400317E RID: 12670
			Succeeded,
			// Token: 0x0400317F RID: 12671
			NotEnoughMemory,
			// Token: 0x04003180 RID: 12672
			AmountTooLarge,
			// Token: 0x04003181 RID: 12673
			AlreadyInProgress
		}

		// Token: 0x02000AE9 RID: 2793
		private enum EndNoGCRegionStatus
		{
			// Token: 0x04003183 RID: 12675
			Succeeded,
			// Token: 0x04003184 RID: 12676
			NotInProgress,
			// Token: 0x04003185 RID: 12677
			GCInduced,
			// Token: 0x04003186 RID: 12678
			AllocationExceeded
		}
	}
}
