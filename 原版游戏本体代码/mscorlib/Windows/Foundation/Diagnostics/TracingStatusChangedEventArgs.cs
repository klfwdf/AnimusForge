using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Foundation.Diagnostics
{
	// Token: 0x02000033 RID: 51
	[Guid("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
	[ComImport]
	internal sealed class TracingStatusChangedEventArgs : ITracingStatusChangedEventArgs
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x060001EC RID: 492
		public extern bool Enabled
		{
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x060001ED RID: 493
		public extern CausalityTraceLevel TraceLevel
		{
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		// Token: 0x060001EE RID: 494
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern TracingStatusChangedEventArgs();
	}
}
