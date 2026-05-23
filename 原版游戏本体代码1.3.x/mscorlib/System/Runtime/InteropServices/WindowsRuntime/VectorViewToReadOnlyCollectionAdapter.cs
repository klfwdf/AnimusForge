using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D6 RID: 2518
	internal sealed class VectorViewToReadOnlyCollectionAdapter
	{
		// Token: 0x0600641B RID: 25627 RVA: 0x00155613 File Offset: 0x00153813
		private VectorViewToReadOnlyCollectionAdapter()
		{
		}

		// Token: 0x0600641C RID: 25628 RVA: 0x0015561C File Offset: 0x0015381C
		[SecurityCritical]
		internal int Count<T>()
		{
			IVectorView<T> vectorView = JitHelpers.UnsafeCast<IVectorView<T>>(this);
			uint size = vectorView.Size;
			if (2147483647U < size)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
			}
			return (int)size;
		}
	}
}
