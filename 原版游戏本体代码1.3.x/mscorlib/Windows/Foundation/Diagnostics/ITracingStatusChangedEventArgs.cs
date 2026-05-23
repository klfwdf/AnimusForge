using System;
using System.Runtime.InteropServices;

namespace Windows.Foundation.Diagnostics
{
	// Token: 0x02000032 RID: 50
	[Guid("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
	[ComImport]
	internal interface ITracingStatusChangedEventArgs
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x060001EA RID: 490
		bool Enabled { get; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060001EB RID: 491
		CausalityTraceLevel TraceLevel { get; }
	}
}
