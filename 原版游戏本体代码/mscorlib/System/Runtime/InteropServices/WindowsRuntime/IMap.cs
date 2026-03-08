using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A1E RID: 2590
	[Guid("3c2925fe-8519-45c1-aa79-197b6718c1c1")]
	[ComImport]
	internal interface IMap<K, V> : IIterable<IKeyValuePair<K, V>>, IEnumerable<IKeyValuePair<K, V>>, IEnumerable
	{
		// Token: 0x060065F3 RID: 26099
		V Lookup(K key);

		// Token: 0x17001184 RID: 4484
		// (get) Token: 0x060065F4 RID: 26100
		uint Size { get; }

		// Token: 0x060065F5 RID: 26101
		bool HasKey(K key);

		// Token: 0x060065F6 RID: 26102
		IReadOnlyDictionary<K, V> GetView();

		// Token: 0x060065F7 RID: 26103
		bool Insert(K key, V value);

		// Token: 0x060065F8 RID: 26104
		void Remove(K key);

		// Token: 0x060065F9 RID: 26105
		void Clear();
	}
}
