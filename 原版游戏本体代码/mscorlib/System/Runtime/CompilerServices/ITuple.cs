using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008EC RID: 2284
	public interface ITuple
	{
		// Token: 0x17001026 RID: 4134
		// (get) Token: 0x06005E08 RID: 24072
		int Length { get; }

		// Token: 0x17001027 RID: 4135
		object this[int index] { get; }
	}
}
