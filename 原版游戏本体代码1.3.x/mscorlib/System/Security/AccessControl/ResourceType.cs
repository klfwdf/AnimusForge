using System;

namespace System.Security.AccessControl
{
	// Token: 0x020001FB RID: 507
	public enum ResourceType
	{
		// Token: 0x04000AAF RID: 2735
		Unknown,
		// Token: 0x04000AB0 RID: 2736
		FileObject,
		// Token: 0x04000AB1 RID: 2737
		Service,
		// Token: 0x04000AB2 RID: 2738
		Printer,
		// Token: 0x04000AB3 RID: 2739
		RegistryKey,
		// Token: 0x04000AB4 RID: 2740
		LMShare,
		// Token: 0x04000AB5 RID: 2741
		KernelObject,
		// Token: 0x04000AB6 RID: 2742
		WindowObject,
		// Token: 0x04000AB7 RID: 2743
		DSObject,
		// Token: 0x04000AB8 RID: 2744
		DSObjectAll,
		// Token: 0x04000AB9 RID: 2745
		ProviderDefined,
		// Token: 0x04000ABA RID: 2746
		WmiGuidObject,
		// Token: 0x04000ABB RID: 2747
		RegistryWow6432Key
	}
}
