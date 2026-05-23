using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime
{
	// Token: 0x02000717 RID: 1815
	[__DynamicallyInvokable]
	public static class GCSettings
	{
		// Token: 0x17000D56 RID: 3414
		// (get) Token: 0x0600512B RID: 20779 RVA: 0x0011E66C File Offset: 0x0011C86C
		// (set) Token: 0x0600512C RID: 20780 RVA: 0x0011E673 File Offset: 0x0011C873
		[__DynamicallyInvokable]
		public static GCLatencyMode LatencyMode
		{
			[SecuritySafeCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				return (GCLatencyMode)GC.GetGCLatencyMode();
			}
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
			set
			{
				if (value < GCLatencyMode.Batch || value > GCLatencyMode.SustainedLowLatency)
				{
					throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
				}
				if (GC.SetGCLatencyMode((int)value) == 1)
				{
					throw new InvalidOperationException("The NoGCRegion mode is in progress. End it and then set a different mode.");
				}
			}
		}

		// Token: 0x17000D57 RID: 3415
		// (get) Token: 0x0600512D RID: 20781 RVA: 0x0011E6A1 File Offset: 0x0011C8A1
		// (set) Token: 0x0600512E RID: 20782 RVA: 0x0011E6A8 File Offset: 0x0011C8A8
		[__DynamicallyInvokable]
		public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
		{
			[SecuritySafeCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			get
			{
				return (GCLargeObjectHeapCompactionMode)GC.GetLOHCompactionMode();
			}
			[SecurityCritical]
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[__DynamicallyInvokable]
			[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
			set
			{
				if (value < GCLargeObjectHeapCompactionMode.Default || value > GCLargeObjectHeapCompactionMode.CompactOnce)
				{
					throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
				}
				GC.SetLOHCompactionMode((int)value);
			}
		}

		// Token: 0x17000D58 RID: 3416
		// (get) Token: 0x0600512F RID: 20783 RVA: 0x0011E6C8 File Offset: 0x0011C8C8
		[__DynamicallyInvokable]
		public static bool IsServerGC
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				return GC.IsServerGC();
			}
		}

		// Token: 0x02000C63 RID: 3171
		private enum SetLatencyModeStatus
		{
			// Token: 0x040037C0 RID: 14272
			Succeeded,
			// Token: 0x040037C1 RID: 14273
			NoGCInProgress
		}
	}
}
