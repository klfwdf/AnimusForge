using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D1 RID: 2513
	internal sealed class EnumerableToIterableAdapter
	{
		// Token: 0x060063FF RID: 25599 RVA: 0x0015511B File Offset: 0x0015331B
		private EnumerableToIterableAdapter()
		{
		}

		// Token: 0x06006400 RID: 25600 RVA: 0x00155124 File Offset: 0x00153324
		[SecurityCritical]
		internal IIterator<T> First_Stub<T>()
		{
			IEnumerable<T> enumerable = JitHelpers.UnsafeCast<IEnumerable<T>>(this);
			return new EnumeratorToIteratorAdapter<T>(enumerable.GetEnumerator());
		}
	}
}
