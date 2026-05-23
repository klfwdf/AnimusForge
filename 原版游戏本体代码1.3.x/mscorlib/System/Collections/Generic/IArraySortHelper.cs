using System;

namespace System.Collections.Generic
{
	// Token: 0x020004DD RID: 1245
	internal interface IArraySortHelper<TKey>
	{
		// Token: 0x06003B35 RID: 15157
		void Sort(TKey[] keys, int index, int length, IComparer<TKey> comparer);

		// Token: 0x06003B36 RID: 15158
		int BinarySearch(TKey[] keys, int index, int length, TKey value, IComparer<TKey> comparer);
	}
}
