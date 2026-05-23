using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000426 RID: 1062
	[AttributeUsage(AttributeTargets.Class)]
	[__DynamicallyInvokable]
	public sealed class EventSourceAttribute : Attribute
	{
		// Token: 0x170007E2 RID: 2018
		// (get) Token: 0x06003529 RID: 13609 RVA: 0x000CE729 File Offset: 0x000CC929
		// (set) Token: 0x0600352A RID: 13610 RVA: 0x000CE731 File Offset: 0x000CC931
		[__DynamicallyInvokable]
		public string Name
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007E3 RID: 2019
		// (get) Token: 0x0600352B RID: 13611 RVA: 0x000CE73A File Offset: 0x000CC93A
		// (set) Token: 0x0600352C RID: 13612 RVA: 0x000CE742 File Offset: 0x000CC942
		[__DynamicallyInvokable]
		public string Guid
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170007E4 RID: 2020
		// (get) Token: 0x0600352D RID: 13613 RVA: 0x000CE74B File Offset: 0x000CC94B
		// (set) Token: 0x0600352E RID: 13614 RVA: 0x000CE753 File Offset: 0x000CC953
		[__DynamicallyInvokable]
		public string LocalizationResources
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x0600352F RID: 13615 RVA: 0x000CE75C File Offset: 0x000CC95C
		[__DynamicallyInvokable]
		public EventSourceAttribute()
		{
		}
	}
}
