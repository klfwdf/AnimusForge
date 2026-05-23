using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000923 RID: 2339
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TypeLibTypeFlags
	{
		// Token: 0x04002A7E RID: 10878
		FAppObject = 1,
		// Token: 0x04002A7F RID: 10879
		FCanCreate = 2,
		// Token: 0x04002A80 RID: 10880
		FLicensed = 4,
		// Token: 0x04002A81 RID: 10881
		FPreDeclId = 8,
		// Token: 0x04002A82 RID: 10882
		FHidden = 16,
		// Token: 0x04002A83 RID: 10883
		FControl = 32,
		// Token: 0x04002A84 RID: 10884
		FDual = 64,
		// Token: 0x04002A85 RID: 10885
		FNonExtensible = 128,
		// Token: 0x04002A86 RID: 10886
		FOleAutomation = 256,
		// Token: 0x04002A87 RID: 10887
		FRestricted = 512,
		// Token: 0x04002A88 RID: 10888
		FAggregatable = 1024,
		// Token: 0x04002A89 RID: 10889
		FReplaceable = 2048,
		// Token: 0x04002A8A RID: 10890
		FDispatchable = 4096,
		// Token: 0x04002A8B RID: 10891
		FReverseBind = 8192
	}
}
