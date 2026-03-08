using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x020000AE RID: 174
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum AttributeTargets
	{
		// Token: 0x040003D6 RID: 982
		[__DynamicallyInvokable]
		Assembly = 1,
		// Token: 0x040003D7 RID: 983
		[__DynamicallyInvokable]
		Module = 2,
		// Token: 0x040003D8 RID: 984
		[__DynamicallyInvokable]
		Class = 4,
		// Token: 0x040003D9 RID: 985
		[__DynamicallyInvokable]
		Struct = 8,
		// Token: 0x040003DA RID: 986
		[__DynamicallyInvokable]
		Enum = 16,
		// Token: 0x040003DB RID: 987
		[__DynamicallyInvokable]
		Constructor = 32,
		// Token: 0x040003DC RID: 988
		[__DynamicallyInvokable]
		Method = 64,
		// Token: 0x040003DD RID: 989
		[__DynamicallyInvokable]
		Property = 128,
		// Token: 0x040003DE RID: 990
		[__DynamicallyInvokable]
		Field = 256,
		// Token: 0x040003DF RID: 991
		[__DynamicallyInvokable]
		Event = 512,
		// Token: 0x040003E0 RID: 992
		[__DynamicallyInvokable]
		Interface = 1024,
		// Token: 0x040003E1 RID: 993
		[__DynamicallyInvokable]
		Parameter = 2048,
		// Token: 0x040003E2 RID: 994
		[__DynamicallyInvokable]
		Delegate = 4096,
		// Token: 0x040003E3 RID: 995
		[__DynamicallyInvokable]
		ReturnValue = 8192,
		// Token: 0x040003E4 RID: 996
		[__DynamicallyInvokable]
		GenericParameter = 16384,
		// Token: 0x040003E5 RID: 997
		[__DynamicallyInvokable]
		All = 32767
	}
}
