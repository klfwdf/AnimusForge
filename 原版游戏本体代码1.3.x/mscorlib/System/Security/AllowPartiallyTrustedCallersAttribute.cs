using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001C4 RID: 452
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
	{
		// Token: 0x06001C1C RID: 7196 RVA: 0x00060E79 File Offset: 0x0005F079
		[__DynamicallyInvokable]
		public AllowPartiallyTrustedCallersAttribute()
		{
		}

		// Token: 0x1700032A RID: 810
		// (get) Token: 0x06001C1D RID: 7197 RVA: 0x00060E81 File Offset: 0x0005F081
		// (set) Token: 0x06001C1E RID: 7198 RVA: 0x00060E89 File Offset: 0x0005F089
		public PartialTrustVisibilityLevel PartialTrustVisibilityLevel
		{
			get
			{
				return this._visibilityLevel;
			}
			set
			{
				this._visibilityLevel = value;
			}
		}

		// Token: 0x040009BF RID: 2495
		private PartialTrustVisibilityLevel _visibilityLevel;
	}
}
