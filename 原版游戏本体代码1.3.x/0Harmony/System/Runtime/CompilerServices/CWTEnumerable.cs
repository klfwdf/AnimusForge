using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004B0 RID: 1200
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class CWTEnumerable<TKey, [Nullable(2)] TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : class where TValue : class
	{
		// Token: 0x06001AF0 RID: 6896 RVA: 0x000577A6 File Offset: 0x000559A6
		public CWTEnumerable(ConditionalWeakTable<TKey, TValue> table)
		{
			this.cwt = table;
		}

		// Token: 0x06001AF1 RID: 6897 RVA: 0x000577B5 File Offset: 0x000559B5
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return this.cwt.GetEnumerator<TKey, TValue>();
		}

		// Token: 0x06001AF2 RID: 6898 RVA: 0x000577C2 File Offset: 0x000559C2
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0400111F RID: 4383
		private readonly ConditionalWeakTable<TKey, TValue> cwt;
	}
}
