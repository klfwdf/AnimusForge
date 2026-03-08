using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000924 RID: 2340
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TypeLibFuncFlags
	{
		// Token: 0x04002A8D RID: 10893
		FRestricted = 1,
		// Token: 0x04002A8E RID: 10894
		FSource = 2,
		// Token: 0x04002A8F RID: 10895
		FBindable = 4,
		// Token: 0x04002A90 RID: 10896
		FRequestEdit = 8,
		// Token: 0x04002A91 RID: 10897
		FDisplayBind = 16,
		// Token: 0x04002A92 RID: 10898
		FDefaultBind = 32,
		// Token: 0x04002A93 RID: 10899
		FHidden = 64,
		// Token: 0x04002A94 RID: 10900
		FUsesGetLastError = 128,
		// Token: 0x04002A95 RID: 10901
		FDefaultCollelem = 256,
		// Token: 0x04002A96 RID: 10902
		FUiDefault = 512,
		// Token: 0x04002A97 RID: 10903
		FNonBrowsable = 1024,
		// Token: 0x04002A98 RID: 10904
		FReplaceable = 2048,
		// Token: 0x04002A99 RID: 10905
		FImmediateBind = 4096
	}
}
