using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200096C RID: 2412
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum TypeLibExporterFlags
	{
		// Token: 0x04002BAD RID: 11181
		None = 0,
		// Token: 0x04002BAE RID: 11182
		OnlyReferenceRegistered = 1,
		// Token: 0x04002BAF RID: 11183
		CallerResolvedReferences = 2,
		// Token: 0x04002BB0 RID: 11184
		OldNames = 4,
		// Token: 0x04002BB1 RID: 11185
		ExportAs32Bit = 16,
		// Token: 0x04002BB2 RID: 11186
		ExportAs64Bit = 32
	}
}
