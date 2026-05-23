using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000045 RID: 69
	[NullableContext(2)]
	internal interface ITuple
	{
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x060002A1 RID: 673
		int Length { get; }

		// Token: 0x17000039 RID: 57
		object this[int index] { get; }
	}
}
