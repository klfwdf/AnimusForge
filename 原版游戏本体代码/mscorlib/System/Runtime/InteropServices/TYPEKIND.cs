using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000991 RID: 2449
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.TYPEKIND instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Serializable]
	public enum TYPEKIND
	{
		// Token: 0x04002C00 RID: 11264
		TKIND_ENUM,
		// Token: 0x04002C01 RID: 11265
		TKIND_RECORD,
		// Token: 0x04002C02 RID: 11266
		TKIND_MODULE,
		// Token: 0x04002C03 RID: 11267
		TKIND_INTERFACE,
		// Token: 0x04002C04 RID: 11268
		TKIND_DISPATCH,
		// Token: 0x04002C05 RID: 11269
		TKIND_COCLASS,
		// Token: 0x04002C06 RID: 11270
		TKIND_ALIAS,
		// Token: 0x04002C07 RID: 11271
		TKIND_UNION,
		// Token: 0x04002C08 RID: 11272
		TKIND_MAX
	}
}
