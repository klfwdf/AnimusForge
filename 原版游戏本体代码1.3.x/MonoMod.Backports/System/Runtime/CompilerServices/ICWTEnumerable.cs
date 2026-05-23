using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
	// Token: 0x0200003E RID: 62
	[NullableContext(1)]
	internal interface ICWTEnumerable<[Nullable(2)] T>
	{
		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000271 RID: 625
		IEnumerable<T> SelfEnumerable { get; }

		// Token: 0x06000272 RID: 626
		IEnumerator<T> GetEnumerator();
	}
}
