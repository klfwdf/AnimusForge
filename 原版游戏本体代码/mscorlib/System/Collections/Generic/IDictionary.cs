using System;

namespace System.Collections.Generic
{
	// Token: 0x020004D2 RID: 1234
	[__DynamicallyInvokable]
	public interface IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
	{
		// Token: 0x170008E8 RID: 2280
		[__DynamicallyInvokable]
		TValue this[TKey key]
		{
			[__DynamicallyInvokable]
			get;
			[__DynamicallyInvokable]
			set;
		}

		// Token: 0x170008E9 RID: 2281
		// (get) Token: 0x06003ACE RID: 15054
		[__DynamicallyInvokable]
		ICollection<TKey> Keys
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170008EA RID: 2282
		// (get) Token: 0x06003ACF RID: 15055
		[__DynamicallyInvokable]
		ICollection<TValue> Values
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06003AD0 RID: 15056
		[__DynamicallyInvokable]
		bool ContainsKey(TKey key);

		// Token: 0x06003AD1 RID: 15057
		[__DynamicallyInvokable]
		void Add(TKey key, TValue value);

		// Token: 0x06003AD2 RID: 15058
		[__DynamicallyInvokable]
		bool Remove(TKey key);

		// Token: 0x06003AD3 RID: 15059
		[__DynamicallyInvokable]
		bool TryGetValue(TKey key, out TValue value);
	}
}
