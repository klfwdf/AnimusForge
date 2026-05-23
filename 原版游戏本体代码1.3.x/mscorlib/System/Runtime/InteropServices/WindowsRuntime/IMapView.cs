using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1F RID: 2591
	[Guid("e480ce40-a338-4ada-adcf-272272e48cb9")]
	[ComImport]
	internal interface IMapView<K, V> : IIterable<IKeyValuePair<K, V>>, IEnumerable<IKeyValuePair<K, V>>, IEnumerable
	{
		// Token: 0x060065FA RID: 26106
		V Lookup(K key);

		// Token: 0x17001185 RID: 4485
		// (get) Token: 0x060065FB RID: 26107
		uint Size { get; }

		// Token: 0x060065FC RID: 26108
		bool HasKey(K key);

		// Token: 0x060065FD RID: 26109
		void Split(out IMapView<K, V> first, out IMapView<K, V> second);
	}
}
