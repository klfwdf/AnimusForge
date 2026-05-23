using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000424 RID: 1060
	public class EventSourceCreatedEventArgs : EventArgs
	{
		// Token: 0x170007D2 RID: 2002
		// (get) Token: 0x0600350F RID: 13583 RVA: 0x000CE450 File Offset: 0x000CC650
		// (set) Token: 0x06003510 RID: 13584 RVA: 0x000CE458 File Offset: 0x000CC658
		public EventSource EventSource { get; internal set; }
	}
}
