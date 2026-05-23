using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200099F RID: 2463
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.FUNCKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	public enum FUNCKIND
	{
		// Token: 0x04002C66 RID: 11366
		FUNC_VIRTUAL,
		// Token: 0x04002C67 RID: 11367
		FUNC_PUREVIRTUAL,
		// Token: 0x04002C68 RID: 11368
		FUNC_NONVIRTUAL,
		// Token: 0x04002C69 RID: 11369
		FUNC_STATIC,
		// Token: 0x04002C6A RID: 11370
		FUNC_DISPATCH
	}
}
