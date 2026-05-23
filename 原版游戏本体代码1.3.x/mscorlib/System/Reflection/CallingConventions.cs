using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005D1 RID: 1489
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum CallingConventions
	{
		// Token: 0x04001C46 RID: 7238
		[__DynamicallyInvokable]
		Standard = 1,
		// Token: 0x04001C47 RID: 7239
		[__DynamicallyInvokable]
		VarArgs = 2,
		// Token: 0x04001C48 RID: 7240
		[__DynamicallyInvokable]
		Any = 3,
		// Token: 0x04001C49 RID: 7241
		[__DynamicallyInvokable]
		HasThis = 32,
		// Token: 0x04001C4A RID: 7242
		[__DynamicallyInvokable]
		ExplicitThis = 64
	}
}
