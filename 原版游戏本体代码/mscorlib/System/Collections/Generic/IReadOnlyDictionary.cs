using System;

namespace System.Collections.Generic
{
	// Token: 0x020004D9 RID: 1241
	[__DynamicallyInvokable]
	public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<!0, !1>>, IEnumerable
	{
		// Token: 0x06003ADF RID: 15071
		[__DynamicallyInvokable]
		bool ContainsKey(TKey key);

		// Token: 0x06003AE0 RID: 15072
		[__DynamicallyInvokable]
		bool TryGetValue(TKey key, out TValue value);

		// Token: 0x170008EF RID: 2287
		[__DynamicallyInvokable]
		TValue this[TKey key]
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170008F0 RID: 2288
		// (get) Token: 0x06003AE2 RID: 15074
		[__DynamicallyInvokable]
		IEnumerable<TKey> Keys
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170008F1 RID: 2289
		// (get) Token: 0x06003AE3 RID: 15075
		[__DynamicallyInvokable]
		IEnumerable<TValue> Values
		{
			[__DynamicallyInvokable]
			get;
		}
	}
}
