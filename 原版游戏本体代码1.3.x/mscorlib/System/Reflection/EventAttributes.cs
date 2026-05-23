using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005E1 RID: 1505
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum EventAttributes
	{
		// Token: 0x04001C96 RID: 7318
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001C97 RID: 7319
		[__DynamicallyInvokable]
		SpecialName = 512,
		// Token: 0x04001C98 RID: 7320
		ReservedMask = 1024,
		// Token: 0x04001C99 RID: 7321
		[__DynamicallyInvokable]
		RTSpecialName = 1024
	}
}
