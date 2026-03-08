using System;

namespace MonoMod.Cil
{
	// Token: 0x02000828 RID: 2088
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class GetFastDelegateInvokersArrayAttribute : Attribute
	{
		// Token: 0x17000816 RID: 2070
		// (get) Token: 0x06002830 RID: 10288 RVA: 0x0008B419 File Offset: 0x00089619
		public int MaxParams { get; }

		// Token: 0x06002831 RID: 10289 RVA: 0x0008B421 File Offset: 0x00089621
		public GetFastDelegateInvokersArrayAttribute(int maxParams)
		{
			this.MaxParams = maxParams;
		}
	}
}
