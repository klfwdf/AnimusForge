using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000429 RID: 1065
	[AttributeUsage(AttributeTargets.Field)]
	internal class EventChannelAttribute : Attribute
	{
		// Token: 0x170007F0 RID: 2032
		// (get) Token: 0x06003547 RID: 13639 RVA: 0x000CE842 File Offset: 0x000CCA42
		// (set) Token: 0x06003548 RID: 13640 RVA: 0x000CE84A File Offset: 0x000CCA4A
		public bool Enabled { get; set; }

		// Token: 0x170007F1 RID: 2033
		// (get) Token: 0x06003549 RID: 13641 RVA: 0x000CE853 File Offset: 0x000CCA53
		// (set) Token: 0x0600354A RID: 13642 RVA: 0x000CE85B File Offset: 0x000CCA5B
		public EventChannelType EventChannelType { get; set; }
	}
}
