using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
	// Token: 0x0200003F RID: 63
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class CWTEnumerable<TKey, [Nullable(2)] TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : class where TValue : class
	{
		// Token: 0x06000273 RID: 627 RVA: 0x0000C07D File Offset: 0x0000A27D
		public CWTEnumerable(ConditionalWeakTable<TKey, TValue> table)
		{
			this.cwt = table;
		}

		// Token: 0x06000274 RID: 628 RVA: 0x0000C08C File Offset: 0x0000A28C
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return this.cwt.GetEnumerator<TKey, TValue>();
		}

		// Token: 0x06000275 RID: 629 RVA: 0x0000C099 File Offset: 0x0000A299
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0400007B RID: 123
		private readonly ConditionalWeakTable<TKey, TValue> cwt;
	}
}
