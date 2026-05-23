using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E5 RID: 2533
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	internal sealed class ReadOnlyDictionaryKeyCollection<TKey, TValue> : IEnumerable<!0>, IEnumerable
	{
		// Token: 0x0600648B RID: 25739 RVA: 0x00156CF0 File Offset: 0x00154EF0
		public ReadOnlyDictionaryKeyCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		// Token: 0x0600648C RID: 25740 RVA: 0x00156D0D File Offset: 0x00154F0D
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!0>)this).GetEnumerator();
		}

		// Token: 0x0600648D RID: 25741 RVA: 0x00156D15 File Offset: 0x00154F15
		public IEnumerator<TKey> GetEnumerator()
		{
			return new ReadOnlyDictionaryKeyEnumerator<TKey, TValue>(this.dictionary);
		}

		// Token: 0x04002CF3 RID: 11507
		private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
	}
}
