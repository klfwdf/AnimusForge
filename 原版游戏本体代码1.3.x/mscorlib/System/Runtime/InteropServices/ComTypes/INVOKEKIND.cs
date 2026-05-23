using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A4A RID: 2634
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum INVOKEKIND
	{
		// Token: 0x04002DD7 RID: 11735
		[__DynamicallyInvokable]
		INVOKE_FUNC = 1,
		// Token: 0x04002DD8 RID: 11736
		[__DynamicallyInvokable]
		INVOKE_PROPERTYGET = 2,
		// Token: 0x04002DD9 RID: 11737
		[__DynamicallyInvokable]
		INVOKE_PROPERTYPUT = 4,
		// Token: 0x04002DDA RID: 11738
		[__DynamicallyInvokable]
		INVOKE_PROPERTYPUTREF = 8
	}
}
