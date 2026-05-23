using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A3F RID: 2623
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum IDLFLAG : short
	{
		// Token: 0x04002DA2 RID: 11682
		[__DynamicallyInvokable]
		IDLFLAG_NONE = 0,
		// Token: 0x04002DA3 RID: 11683
		[__DynamicallyInvokable]
		IDLFLAG_FIN = 1,
		// Token: 0x04002DA4 RID: 11684
		[__DynamicallyInvokable]
		IDLFLAG_FOUT = 2,
		// Token: 0x04002DA5 RID: 11685
		[__DynamicallyInvokable]
		IDLFLAG_FLCID = 4,
		// Token: 0x04002DA6 RID: 11686
		[__DynamicallyInvokable]
		IDLFLAG_FRETVAL = 8
	}
}
