using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1B RID: 2587
	[Guid("bbe1fa4c-b0e3-4583-baef-1f1b2e483e56")]
	[ComImport]
	internal interface IVectorView<T> : IIterable<T>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x060065E2 RID: 26082
		T GetAt(uint index);

		// Token: 0x17001181 RID: 4481
		// (get) Token: 0x060065E3 RID: 26083
		uint Size { get; }

		// Token: 0x060065E4 RID: 26084
		bool IndexOf(T value, out uint index);

		// Token: 0x060065E5 RID: 26085
		uint GetMany(uint startIndex, [Out] T[] items);
	}
}
