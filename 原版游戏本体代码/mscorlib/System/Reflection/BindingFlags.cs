using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005D0 RID: 1488
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum BindingFlags
	{
		// Token: 0x04001C31 RID: 7217
		Default = 0,
		// Token: 0x04001C32 RID: 7218
		[__DynamicallyInvokable]
		IgnoreCase = 1,
		// Token: 0x04001C33 RID: 7219
		[__DynamicallyInvokable]
		DeclaredOnly = 2,
		// Token: 0x04001C34 RID: 7220
		[__DynamicallyInvokable]
		Instance = 4,
		// Token: 0x04001C35 RID: 7221
		[__DynamicallyInvokable]
		Static = 8,
		// Token: 0x04001C36 RID: 7222
		[__DynamicallyInvokable]
		Public = 16,
		// Token: 0x04001C37 RID: 7223
		[__DynamicallyInvokable]
		NonPublic = 32,
		// Token: 0x04001C38 RID: 7224
		[__DynamicallyInvokable]
		FlattenHierarchy = 64,
		// Token: 0x04001C39 RID: 7225
		InvokeMethod = 256,
		// Token: 0x04001C3A RID: 7226
		CreateInstance = 512,
		// Token: 0x04001C3B RID: 7227
		GetField = 1024,
		// Token: 0x04001C3C RID: 7228
		SetField = 2048,
		// Token: 0x04001C3D RID: 7229
		GetProperty = 4096,
		// Token: 0x04001C3E RID: 7230
		SetProperty = 8192,
		// Token: 0x04001C3F RID: 7231
		PutDispProperty = 16384,
		// Token: 0x04001C40 RID: 7232
		PutRefDispProperty = 32768,
		// Token: 0x04001C41 RID: 7233
		[__DynamicallyInvokable]
		ExactBinding = 65536,
		// Token: 0x04001C42 RID: 7234
		SuppressChangeType = 131072,
		// Token: 0x04001C43 RID: 7235
		[__DynamicallyInvokable]
		OptionalParamBinding = 262144,
		// Token: 0x04001C44 RID: 7236
		IgnoreReturn = 16777216
	}
}
