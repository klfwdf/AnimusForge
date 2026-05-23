using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009A0 RID: 2464
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.INVOKEKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	public enum INVOKEKIND
	{
		// Token: 0x04002C6C RID: 11372
		INVOKE_FUNC = 1,
		// Token: 0x04002C6D RID: 11373
		INVOKE_PROPERTYGET,
		// Token: 0x04002C6E RID: 11374
		INVOKE_PROPERTYPUT = 4,
		// Token: 0x04002C6F RID: 11375
		INVOKE_PROPERTYPUTREF = 8
	}
}
