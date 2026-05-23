using System;

namespace System.Collections.Generic
{
	// Token: 0x020004E1 RID: 1249
	internal interface IArraySortHelper<TKey, TValue>
	{
		// Token: 0x06003B55 RID: 15189
		void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer);
	}
}
