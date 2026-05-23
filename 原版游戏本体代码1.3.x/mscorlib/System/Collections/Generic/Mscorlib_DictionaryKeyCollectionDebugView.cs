using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
	// Token: 0x020004CC RID: 1228
	internal sealed class Mscorlib_DictionaryKeyCollectionDebugView<TKey, TValue>
	{
		// Token: 0x06003ABC RID: 15036 RVA: 0x000DF9C8 File Offset: 0x000DDBC8
		public Mscorlib_DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
			}
			this.collection = collection;
		}

		// Token: 0x170008E2 RID: 2274
		// (get) Token: 0x06003ABD RID: 15037 RVA: 0x000DF9E0 File Offset: 0x000DDBE0
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TKey[] Items
		{
			get
			{
				TKey[] array = new TKey[this.collection.Count];
				this.collection.CopyTo(array, 0);
				return array;
			}
		}

		// Token: 0x04001955 RID: 6485
		private ICollection<TKey> collection;
	}
}
