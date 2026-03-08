using System;

namespace System.Collections.Generic
{
	// Token: 0x020004BE RID: 1214
	[Serializable]
	internal class ComparisonComparer<T> : Comparer<T>
	{
		// Token: 0x06003A38 RID: 14904 RVA: 0x000DDF76 File Offset: 0x000DC176
		public ComparisonComparer(Comparison<T> comparison)
		{
			this._comparison = comparison;
		}

		// Token: 0x06003A39 RID: 14905 RVA: 0x000DDF85 File Offset: 0x000DC185
		public override int Compare(T x, T y)
		{
			return this._comparison(x, y);
		}

		// Token: 0x04001942 RID: 6466
		private readonly Comparison<T> _comparison;
	}
}
