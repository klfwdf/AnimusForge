using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000925 RID: 2341
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TypeLibVarFlags
	{
		// Token: 0x04002A9B RID: 10907
		FReadOnly = 1,
		// Token: 0x04002A9C RID: 10908
		FSource = 2,
		// Token: 0x04002A9D RID: 10909
		FBindable = 4,
		// Token: 0x04002A9E RID: 10910
		FRequestEdit = 8,
		// Token: 0x04002A9F RID: 10911
		FDisplayBind = 16,
		// Token: 0x04002AA0 RID: 10912
		FDefaultBind = 32,
		// Token: 0x04002AA1 RID: 10913
		FHidden = 64,
		// Token: 0x04002AA2 RID: 10914
		FRestricted = 128,
		// Token: 0x04002AA3 RID: 10915
		FDefaultCollelem = 256,
		// Token: 0x04002AA4 RID: 10916
		FUiDefault = 512,
		// Token: 0x04002AA5 RID: 10917
		FNonBrowsable = 1024,
		// Token: 0x04002AA6 RID: 10918
		FReplaceable = 2048,
		// Token: 0x04002AA7 RID: 10919
		FImmediateBind = 4096
	}
}
