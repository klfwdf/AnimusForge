using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
	// Token: 0x020004CB RID: 1227
	internal sealed class Mscorlib_CollectionDebugView<T>
	{
		// Token: 0x06003ABA RID: 15034 RVA: 0x000DF982 File Offset: 0x000DDB82
		public Mscorlib_CollectionDebugView(ICollection<T> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
			}
			this.collection = collection;
		}

		// Token: 0x170008E1 RID: 2273
		// (get) Token: 0x06003ABB RID: 15035 RVA: 0x000DF99C File Offset: 0x000DDB9C
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				T[] array = new T[this.collection.Count];
				this.collection.CopyTo(array, 0);
				return array;
			}
		}

		// Token: 0x04001954 RID: 6484
		private ICollection<T> collection;
	}
}
