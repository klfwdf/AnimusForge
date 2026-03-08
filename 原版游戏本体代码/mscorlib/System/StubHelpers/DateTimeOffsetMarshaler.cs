using System;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x02000595 RID: 1429
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class DateTimeOffsetMarshaler
	{
		// Token: 0x060042D9 RID: 17113 RVA: 0x000F9774 File Offset: 0x000F7974
		[SecurityCritical]
		internal static void ConvertToNative(ref DateTimeOffset managedDTO, out DateTimeNative dateTime)
		{
			long utcTicks = managedDTO.UtcTicks;
			dateTime.UniversalTime = utcTicks - 504911232000000000L;
		}

		// Token: 0x060042DA RID: 17114 RVA: 0x000F979C File Offset: 0x000F799C
		[SecurityCritical]
		internal static void ConvertToManaged(out DateTimeOffset managedLocalDTO, ref DateTimeNative nativeTicks)
		{
			long ticks = 504911232000000000L + nativeTicks.UniversalTime;
			DateTimeOffset dateTimeOffset = new DateTimeOffset(ticks, TimeSpan.Zero);
			managedLocalDTO = dateTimeOffset.ToLocalTime(true);
		}

		// Token: 0x04001BD9 RID: 7129
		private const long ManagedUtcTicksAtNativeZero = 504911232000000000L;
	}
}
