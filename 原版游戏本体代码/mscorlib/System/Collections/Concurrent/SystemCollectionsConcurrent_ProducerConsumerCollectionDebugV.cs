using System;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
	// Token: 0x020004AC RID: 1196
	internal sealed class SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<T>
	{
		// Token: 0x06003927 RID: 14631 RVA: 0x000DAC1C File Offset: 0x000D8E1C
		public SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView(IProducerConsumerCollection<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this.m_collection = collection;
		}

		// Token: 0x1700088D RID: 2189
		// (get) Token: 0x06003928 RID: 14632 RVA: 0x000DAC39 File Offset: 0x000D8E39
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this.m_collection.ToArray();
			}
		}

		// Token: 0x04001914 RID: 6420
		private IProducerConsumerCollection<T> m_collection;
	}
}
