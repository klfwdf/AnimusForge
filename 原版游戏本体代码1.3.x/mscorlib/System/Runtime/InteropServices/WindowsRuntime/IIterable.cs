using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A15 RID: 2581
	[Guid("faa585ea-6214-4217-afda-7f46de5869b3")]
	[ComImport]
	internal interface IIterable<T> : IEnumerable<!0>, IEnumerable
	{
		// Token: 0x060065C1 RID: 26049
		IIterator<T> First();
	}
}
