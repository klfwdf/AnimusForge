using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComparerExtensions
{
	// Token: 0x0200000C RID: 12
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class TypedKeyComparer<[Nullable(2)] T, [Nullable(2)] TKey> : KeyComparer<T>
	{
		// Token: 0x06000026 RID: 38 RVA: 0x00002527 File Offset: 0x00000727
		public TypedKeyComparer(Func<T, TKey> keySelector, IComparer<TKey> keyComparer)
		{
			this._keySelector = keySelector;
			this._keyComparer = keyComparer;
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000027 RID: 39 RVA: 0x0000253D File Offset: 0x0000073D
		// (set) Token: 0x06000028 RID: 40 RVA: 0x00002545 File Offset: 0x00000745
		public bool Descending { get; set; }

		// Token: 0x06000029 RID: 41 RVA: 0x00002550 File Offset: 0x00000750
		public override int Compare(T x, T y)
		{
			TKey key = this._keySelector(x);
			TKey key2 = this._keySelector(y);
			if (!this.Descending)
			{
				return this._keyComparer.Compare(key, key2);
			}
			return this._keyComparer.Compare(key2, key);
		}

		// Token: 0x04000007 RID: 7
		private readonly Func<T, TKey> _keySelector;

		// Token: 0x04000008 RID: 8
		private readonly IComparer<TKey> _keyComparer;
	}
}
