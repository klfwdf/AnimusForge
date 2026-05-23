using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
	// Token: 0x020004CD RID: 1229
	internal sealed class Mscorlib_DictionaryValueCollectionDebugView<TKey, TValue>
	{
		// Token: 0x06003ABE RID: 15038 RVA: 0x000DFA0C File Offset: 0x000DDC0C
		public Mscorlib_DictionaryValueCollectionDebugView(ICollection<TValue> collection)
		{
			if (collection == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
			}
			this.collection = collection;
		}

		// Token: 0x170008E3 RID: 2275
		// (get) Token: 0x06003ABF RID: 15039 RVA: 0x000DFA24 File Offset: 0x000DDC24
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TValue[] Items
		{
			get
			{
				TValue[] array = new TValue[this.collection.Count];
				this.collection.CopyTo(array, 0);
				return array;
			}
		}

		// Token: 0x04001956 RID: 6486
		private ICollection<TValue> collection;
	}
}
