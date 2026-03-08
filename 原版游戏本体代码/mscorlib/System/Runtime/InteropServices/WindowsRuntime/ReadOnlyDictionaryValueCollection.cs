using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E7 RID: 2535
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	internal sealed class ReadOnlyDictionaryValueCollection<TKey, TValue> : IEnumerable<!1>, IEnumerable
	{
		// Token: 0x06006494 RID: 25748 RVA: 0x00156DA7 File Offset: 0x00154FA7
		public ReadOnlyDictionaryValueCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		// Token: 0x06006495 RID: 25749 RVA: 0x00156DC4 File Offset: 0x00154FC4
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!1>)this).GetEnumerator();
		}

		// Token: 0x06006496 RID: 25750 RVA: 0x00156DCC File Offset: 0x00154FCC
		public IEnumerator<TValue> GetEnumerator()
		{
			return new ReadOnlyDictionaryValueEnumerator<TKey, TValue>(this.dictionary);
		}

		// Token: 0x04002CF6 RID: 11510
		private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
	}
}
