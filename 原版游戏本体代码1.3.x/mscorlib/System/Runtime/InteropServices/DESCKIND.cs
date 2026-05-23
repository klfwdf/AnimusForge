using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200098E RID: 2446
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.DESCKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	public enum DESCKIND
	{
		// Token: 0x04002BF6 RID: 11254
		DESCKIND_NONE,
		// Token: 0x04002BF7 RID: 11255
		DESCKIND_FUNCDESC,
		// Token: 0x04002BF8 RID: 11256
		DESCKIND_VARDESC,
		// Token: 0x04002BF9 RID: 11257
		DESCKIND_TYPECOMP,
		// Token: 0x04002BFA RID: 11258
		DESCKIND_IMPLICITAPPOBJ,
		// Token: 0x04002BFB RID: 11259
		DESCKIND_MAX
	}
}
