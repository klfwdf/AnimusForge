using System;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A21 RID: 2593
	internal sealed class CLRIKeyValuePairImpl<K, V> : IKeyValuePair<K, V>
	{
		// Token: 0x06006600 RID: 26112 RVA: 0x00159CE8 File Offset: 0x00157EE8
		public CLRIKeyValuePairImpl([In] ref KeyValuePair<K, V> pair)
		{
			this._pair = pair;
		}

		// Token: 0x17001188 RID: 4488
		// (get) Token: 0x06006601 RID: 26113 RVA: 0x00159CFC File Offset: 0x00157EFC
		public K Key
		{
			get
			{
				return this._pair.Key;
			}
		}

		// Token: 0x17001189 RID: 4489
		// (get) Token: 0x06006602 RID: 26114 RVA: 0x00159D18 File Offset: 0x00157F18
		public V Value
		{
			get
			{
				return this._pair.Value;
			}
		}

		// Token: 0x06006603 RID: 26115 RVA: 0x00159D34 File Offset: 0x00157F34
		internal static object BoxHelper(object pair)
		{
			KeyValuePair<K, V> keyValuePair = (KeyValuePair<K, V>)pair;
			return new CLRIKeyValuePairImpl<K, V>(ref keyValuePair);
		}

		// Token: 0x06006604 RID: 26116 RVA: 0x00159D50 File Offset: 0x00157F50
		internal static object UnboxHelper(object wrapper)
		{
			CLRIKeyValuePairImpl<K, V> clrikeyValuePairImpl = (CLRIKeyValuePairImpl<K, V>)wrapper;
			return clrikeyValuePairImpl._pair;
		}

		// Token: 0x06006605 RID: 26117 RVA: 0x00159D70 File Offset: 0x00157F70
		public override string ToString()
		{
			return this._pair.ToString();
		}

		// Token: 0x04002D45 RID: 11589
		private readonly KeyValuePair<K, V> _pair;
	}
}
