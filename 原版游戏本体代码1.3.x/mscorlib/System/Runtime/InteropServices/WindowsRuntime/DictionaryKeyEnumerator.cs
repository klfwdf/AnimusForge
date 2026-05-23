using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009CE RID: 2510
	[Serializable]
	internal sealed class DictionaryKeyEnumerator<TKey, TValue> : IEnumerator<!0>, IDisposable, IEnumerator
	{
		// Token: 0x060063E9 RID: 25577 RVA: 0x00154E8A File Offset: 0x0015308A
		public DictionaryKeyEnumerator(IDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
			this.enumeration = dictionary.GetEnumerator();
		}

		// Token: 0x060063EA RID: 25578 RVA: 0x00154EB3 File Offset: 0x001530B3
		void IDisposable.Dispose()
		{
			this.enumeration.Dispose();
		}

		// Token: 0x060063EB RID: 25579 RVA: 0x00154EC0 File Offset: 0x001530C0
		public bool MoveNext()
		{
			return this.enumeration.MoveNext();
		}

		// Token: 0x17001147 RID: 4423
		// (get) Token: 0x060063EC RID: 25580 RVA: 0x00154ECD File Offset: 0x001530CD
		object IEnumerator.Current
		{
			get
			{
				return ((IEnumerator<!0>)this).Current;
			}
		}

		// Token: 0x17001148 RID: 4424
		// (get) Token: 0x060063ED RID: 25581 RVA: 0x00154EDC File Offset: 0x001530DC
		public TKey Current
		{
			get
			{
				KeyValuePair<TKey, TValue> keyValuePair = this.enumeration.Current;
				return keyValuePair.Key;
			}
		}

		// Token: 0x060063EE RID: 25582 RVA: 0x00154EFC File Offset: 0x001530FC
		public void Reset()
		{
			this.enumeration = this.dictionary.GetEnumerator();
		}

		// Token: 0x04002CE7 RID: 11495
		private readonly IDictionary<TKey, TValue> dictionary;

		// Token: 0x04002CE8 RID: 11496
		private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
	}
}
