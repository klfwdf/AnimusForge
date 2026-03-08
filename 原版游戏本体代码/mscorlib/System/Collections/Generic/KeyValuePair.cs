using System;
using System.Text;

namespace System.Collections.Generic
{
	// Token: 0x020004DB RID: 1243
	[__DynamicallyInvokable]
	[Serializable]
	public struct KeyValuePair<TKey, TValue>
	{
		// Token: 0x06003AE8 RID: 15080 RVA: 0x000DFB30 File Offset: 0x000DDD30
		[__DynamicallyInvokable]
		public KeyValuePair(TKey key, TValue value)
		{
			this.key = key;
			this.value = value;
		}

		// Token: 0x170008F2 RID: 2290
		// (get) Token: 0x06003AE9 RID: 15081 RVA: 0x000DFB40 File Offset: 0x000DDD40
		[__DynamicallyInvokable]
		public TKey Key
		{
			[__DynamicallyInvokable]
			get
			{
				return this.key;
			}
		}

		// Token: 0x170008F3 RID: 2291
		// (get) Token: 0x06003AEA RID: 15082 RVA: 0x000DFB48 File Offset: 0x000DDD48
		[__DynamicallyInvokable]
		public TValue Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this.value;
			}
		}

		// Token: 0x06003AEB RID: 15083 RVA: 0x000DFB50 File Offset: 0x000DDD50
		[__DynamicallyInvokable]
		public override string ToString()
		{
			StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
			stringBuilder.Append('[');
			if (this.Key != null)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				TKey tkey = this.Key;
				stringBuilder2.Append(tkey.ToString());
			}
			stringBuilder.Append(", ");
			if (this.Value != null)
			{
				StringBuilder stringBuilder3 = stringBuilder;
				TValue tvalue = this.Value;
				stringBuilder3.Append(tvalue.ToString());
			}
			stringBuilder.Append(']');
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

		// Token: 0x04001959 RID: 6489
		private TKey key;

		// Token: 0x0400195A RID: 6490
		private TValue value;
	}
}
