using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D9 RID: 2521
	internal sealed class MapViewToReadOnlyCollectionAdapter
	{
		// Token: 0x06006430 RID: 25648 RVA: 0x00155B00 File Offset: 0x00153D00
		private MapViewToReadOnlyCollectionAdapter()
		{
		}

		// Token: 0x06006431 RID: 25649 RVA: 0x00155B08 File Offset: 0x00153D08
		[SecurityCritical]
		internal int Count<K, V>()
		{
			object obj = JitHelpers.UnsafeCast<object>(this);
			IMapView<K, V> mapView = obj as IMapView<K, V>;
			if (mapView != null)
			{
				uint size = mapView.Size;
				if (2147483647U < size)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
				}
				return (int)size;
			}
			else
			{
				IVectorView<KeyValuePair<K, V>> vectorView = JitHelpers.UnsafeCast<IVectorView<KeyValuePair<K, V>>>(this);
				uint size2 = vectorView.Size;
				if (2147483647U < size2)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
				}
				return (int)size2;
			}
		}
	}
}
