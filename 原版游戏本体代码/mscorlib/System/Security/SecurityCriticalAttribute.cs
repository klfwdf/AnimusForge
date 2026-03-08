using System;

namespace System.Security
{
	// Token: 0x020001C7 RID: 455
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class SecurityCriticalAttribute : Attribute
	{
		// Token: 0x06001C1F RID: 7199 RVA: 0x00060E92 File Offset: 0x0005F092
		[__DynamicallyInvokable]
		public SecurityCriticalAttribute()
		{
		}

		// Token: 0x06001C20 RID: 7200 RVA: 0x00060E9A File Offset: 0x0005F09A
		public SecurityCriticalAttribute(SecurityCriticalScope scope)
		{
			this._val = scope;
		}

		// Token: 0x1700032B RID: 811
		// (get) Token: 0x06001C21 RID: 7201 RVA: 0x00060EA9 File Offset: 0x0005F0A9
		[Obsolete("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
		public SecurityCriticalScope Scope
		{
			get
			{
				return this._val;
			}
		}

		// Token: 0x040009C6 RID: 2502
		private SecurityCriticalScope _val;
	}
}
