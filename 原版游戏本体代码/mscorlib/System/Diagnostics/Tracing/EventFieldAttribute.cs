using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000443 RID: 1091
	[AttributeUsage(AttributeTargets.Property)]
	[__DynamicallyInvokable]
	public class EventFieldAttribute : Attribute
	{
		// Token: 0x170007FB RID: 2043
		// (get) Token: 0x06003606 RID: 13830 RVA: 0x000D24B2 File Offset: 0x000D06B2
		// (set) Token: 0x06003607 RID: 13831 RVA: 0x000D24BA File Offset: 0x000D06BA
		[__DynamicallyInvokable]
		public EventFieldTags Tags
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007FC RID: 2044
		// (get) Token: 0x06003608 RID: 13832 RVA: 0x000D24C3 File Offset: 0x000D06C3
		// (set) Token: 0x06003609 RID: 13833 RVA: 0x000D24CB File Offset: 0x000D06CB
		internal string Name { get; set; }

		// Token: 0x170007FD RID: 2045
		// (get) Token: 0x0600360A RID: 13834 RVA: 0x000D24D4 File Offset: 0x000D06D4
		// (set) Token: 0x0600360B RID: 13835 RVA: 0x000D24DC File Offset: 0x000D06DC
		[__DynamicallyInvokable]
		public EventFieldFormat Format
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x0600360C RID: 13836 RVA: 0x000D24E5 File Offset: 0x000D06E5
		[__DynamicallyInvokable]
		public EventFieldAttribute()
		{
		}
	}
}
