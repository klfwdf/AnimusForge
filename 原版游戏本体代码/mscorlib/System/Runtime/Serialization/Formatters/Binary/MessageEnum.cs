using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000775 RID: 1909
	[Flags]
	[Serializable]
	internal enum MessageEnum
	{
		// Token: 0x04002556 RID: 9558
		NoArgs = 1,
		// Token: 0x04002557 RID: 9559
		ArgsInline = 2,
		// Token: 0x04002558 RID: 9560
		ArgsIsArray = 4,
		// Token: 0x04002559 RID: 9561
		ArgsInArray = 8,
		// Token: 0x0400255A RID: 9562
		NoContext = 16,
		// Token: 0x0400255B RID: 9563
		ContextInline = 32,
		// Token: 0x0400255C RID: 9564
		ContextInArray = 64,
		// Token: 0x0400255D RID: 9565
		MethodSignatureInArray = 128,
		// Token: 0x0400255E RID: 9566
		PropertyInArray = 256,
		// Token: 0x0400255F RID: 9567
		NoReturnValue = 512,
		// Token: 0x04002560 RID: 9568
		ReturnValueVoid = 1024,
		// Token: 0x04002561 RID: 9569
		ReturnValueInline = 2048,
		// Token: 0x04002562 RID: 9570
		ReturnValueInArray = 4096,
		// Token: 0x04002563 RID: 9571
		ExceptionInArray = 8192,
		// Token: 0x04002564 RID: 9572
		GenericMethod = 32768
	}
}
