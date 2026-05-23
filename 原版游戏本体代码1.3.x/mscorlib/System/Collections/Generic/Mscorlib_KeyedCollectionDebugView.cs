using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Collections.Generic
{
	// Token: 0x020004CF RID: 1231
	internal sealed class Mscorlib_KeyedCollectionDebugView<K, T>
	{
		// Token: 0x06003AC2 RID: 15042 RVA: 0x000DFA94 File Offset: 0x000DDC94
		public Mscorlib_KeyedCollectionDebugView(KeyedCollection<K, T> keyedCollection)
		{
			if (keyedCollection == null)
			{
				throw new ArgumentNullException("keyedCollection");
			}
			this.kc = keyedCollection;
		}

		// Token: 0x170008E5 RID: 2277
		// (get) Token: 0x06003AC3 RID: 15043 RVA: 0x000DFAB4 File Offset: 0x000DDCB4
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				T[] array = new T[this.kc.Count];
				this.kc.CopyTo(array, 0);
				return array;
			}
		}

		// Token: 0x04001958 RID: 6488
		private KeyedCollection<K, T> kc;
	}
}
