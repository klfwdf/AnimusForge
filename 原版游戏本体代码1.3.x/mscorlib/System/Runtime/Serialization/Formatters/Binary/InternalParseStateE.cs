using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000773 RID: 1907
	[Serializable]
	internal enum InternalParseStateE
	{
		// Token: 0x0400253D RID: 9533
		Initial,
		// Token: 0x0400253E RID: 9534
		Object,
		// Token: 0x0400253F RID: 9535
		Member,
		// Token: 0x04002540 RID: 9536
		MemberChild
	}
}
