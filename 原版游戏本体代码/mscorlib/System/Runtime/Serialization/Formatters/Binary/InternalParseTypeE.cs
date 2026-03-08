using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200076D RID: 1901
	[Serializable]
	internal enum InternalParseTypeE
	{
		// Token: 0x04002515 RID: 9493
		Empty,
		// Token: 0x04002516 RID: 9494
		SerializedStreamHeader,
		// Token: 0x04002517 RID: 9495
		Object,
		// Token: 0x04002518 RID: 9496
		Member,
		// Token: 0x04002519 RID: 9497
		ObjectEnd,
		// Token: 0x0400251A RID: 9498
		MemberEnd,
		// Token: 0x0400251B RID: 9499
		Headers,
		// Token: 0x0400251C RID: 9500
		HeadersEnd,
		// Token: 0x0400251D RID: 9501
		SerializedStreamHeaderEnd,
		// Token: 0x0400251E RID: 9502
		Envelope,
		// Token: 0x0400251F RID: 9503
		EnvelopeEnd,
		// Token: 0x04002520 RID: 9504
		Body,
		// Token: 0x04002521 RID: 9505
		BodyEnd
	}
}
