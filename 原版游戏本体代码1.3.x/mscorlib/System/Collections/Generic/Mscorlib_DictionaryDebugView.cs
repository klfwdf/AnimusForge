using System;
using System.Diagnostics;

namespace System.Collections.Generic
{
	// Token: 0x020004CE RID: 1230
	internal sealed class Mscorlib_DictionaryDebugView<K, V>
	{
		// Token: 0x06003AC0 RID: 15040 RVA: 0x000DFA50 File Offset: 0x000DDC50
		public Mscorlib_DictionaryDebugView(IDictionary<K, V> dictionary)
		{
			if (dictionary == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
			}
			this.dict = dictionary;
		}

		// Token: 0x170008E4 RID: 2276
		// (get) Token: 0x06003AC1 RID: 15041 RVA: 0x000DFA68 File Offset: 0x000DDC68
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<K, V>[] Items
		{
			get
			{
				KeyValuePair<K, V>[] array = new KeyValuePair<K, V>[this.dict.Count];
				this.dict.CopyTo(array, 0);
				return array;
			}
		}

		// Token: 0x04001957 RID: 6487
		private IDictionary<K, V> dict;
	}
}
