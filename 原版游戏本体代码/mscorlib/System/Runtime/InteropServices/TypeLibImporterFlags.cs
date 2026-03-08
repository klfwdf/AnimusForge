using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200096B RID: 2411
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TypeLibImporterFlags
	{
		// Token: 0x04002B9E RID: 11166
		None = 0,
		// Token: 0x04002B9F RID: 11167
		PrimaryInteropAssembly = 1,
		// Token: 0x04002BA0 RID: 11168
		UnsafeInterfaces = 2,
		// Token: 0x04002BA1 RID: 11169
		SafeArrayAsSystemArray = 4,
		// Token: 0x04002BA2 RID: 11170
		TransformDispRetVals = 8,
		// Token: 0x04002BA3 RID: 11171
		PreventClassMembers = 16,
		// Token: 0x04002BA4 RID: 11172
		SerializableValueClasses = 32,
		// Token: 0x04002BA5 RID: 11173
		ImportAsX86 = 256,
		// Token: 0x04002BA6 RID: 11174
		ImportAsX64 = 512,
		// Token: 0x04002BA7 RID: 11175
		ImportAsItanium = 1024,
		// Token: 0x04002BA8 RID: 11176
		ImportAsAgnostic = 2048,
		// Token: 0x04002BA9 RID: 11177
		ReflectionOnlyLoading = 4096,
		// Token: 0x04002BAA RID: 11178
		NoDefineVersionResource = 8192,
		// Token: 0x04002BAB RID: 11179
		ImportAsArm = 16384
	}
}
