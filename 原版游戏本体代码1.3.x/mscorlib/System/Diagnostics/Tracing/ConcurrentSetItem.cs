using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200043C RID: 1084
	internal abstract class ConcurrentSetItem<KeyType, ItemType> where ItemType : ConcurrentSetItem<KeyType, ItemType>
	{
		// Token: 0x060035E0 RID: 13792
		public abstract int Compare(ItemType other);

		// Token: 0x060035E1 RID: 13793
		public abstract int Compare(KeyType key);
	}
}
