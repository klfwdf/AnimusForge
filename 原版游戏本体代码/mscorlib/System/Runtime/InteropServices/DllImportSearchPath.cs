using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000932 RID: 2354
	[Flags]
	[__DynamicallyInvokable]
	public enum DllImportSearchPath
	{
		// Token: 0x04002B0B RID: 11019
		[__DynamicallyInvokable]
		UseDllDirectoryForDependencies = 256,
		// Token: 0x04002B0C RID: 11020
		[__DynamicallyInvokable]
		ApplicationDirectory = 512,
		// Token: 0x04002B0D RID: 11021
		[__DynamicallyInvokable]
		UserDirectories = 1024,
		// Token: 0x04002B0E RID: 11022
		[__DynamicallyInvokable]
		System32 = 2048,
		// Token: 0x04002B0F RID: 11023
		[__DynamicallyInvokable]
		SafeDirectories = 4096,
		// Token: 0x04002B10 RID: 11024
		[__DynamicallyInvokable]
		AssemblyDirectory = 2,
		// Token: 0x04002B11 RID: 11025
		[__DynamicallyInvokable]
		LegacyBehavior = 0
	}
}
