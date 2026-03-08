using System;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004AF RID: 1199
	[NullableContext(1)]
	internal interface ICWTEnumerable<[Nullable(2)] T>
	{
		// Token: 0x170005DE RID: 1502
		// (get) Token: 0x06001AEE RID: 6894
		IEnumerable<T> SelfEnumerable { get; }

		// Token: 0x06001AEF RID: 6895
		IEnumerator<T> GetEnumerator();
	}
}
