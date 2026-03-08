using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.StubHelpers
{
	// Token: 0x02000599 RID: 1433
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class DateMarshaler
	{
		// Token: 0x060042E5 RID: 17125
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern double ConvertToNative(DateTime managedDate);

		// Token: 0x060042E6 RID: 17126
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern long ConvertToManaged(double nativeDate);
	}
}
