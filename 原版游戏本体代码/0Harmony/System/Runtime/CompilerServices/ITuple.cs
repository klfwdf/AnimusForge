using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004BA RID: 1210
	[NullableContext(2)]
	internal interface ITuple
	{
		// Token: 0x170005E3 RID: 1507
		// (get) Token: 0x06001B2C RID: 6956
		int Length { get; }

		// Token: 0x170005E4 RID: 1508
		object this[int index] { get; }
	}
}
