using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E6 RID: 2534
	[Serializable]
	internal sealed class ReadOnlyDictionaryKeyEnumerator<TKey, TValue> : IEnumerator<!0>, IDisposable, IEnumerator
	{
		// Token: 0x0600648E RID: 25742 RVA: 0x00156D22 File Offset: 0x00154F22
		public ReadOnlyDictionaryKeyEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
			this.enumeration = dictionary.GetEnumerator();
		}

		// Token: 0x0600648F RID: 25743 RVA: 0x00156D4B File Offset: 0x00154F4B
		void IDisposable.Dispose()
		{
			this.enumeration.Dispose();
		}

		// Token: 0x06006490 RID: 25744 RVA: 0x00156D58 File Offset: 0x00154F58
		public bool MoveNext()
		{
			return this.enumeration.MoveNext();
		}

		// Token: 0x17001153 RID: 4435
		// (get) Token: 0x06006491 RID: 25745 RVA: 0x00156D65 File Offset: 0x00154F65
		object IEnumerator.Current
		{
			get
			{
				return ((IEnumerator<!0>)this).Current;
			}
		}

		// Token: 0x17001154 RID: 4436
		// (get) Token: 0x06006492 RID: 25746 RVA: 0x00156D74 File Offset: 0x00154F74
		public TKey Current
		{
			get
			{
				KeyValuePair<TKey, TValue> keyValuePair = this.enumeration.Current;
				return keyValuePair.Key;
			}
		}

		// Token: 0x06006493 RID: 25747 RVA: 0x00156D94 File Offset: 0x00154F94
		public void Reset()
		{
			this.enumeration = this.dictionary.GetEnumerator();
		}

		// Token: 0x04002CF4 RID: 11508
		private readonly IReadOnlyDictionary<TKey, TValue> dictionary;

		// Token: 0x04002CF5 RID: 11509
		private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
	}
}
