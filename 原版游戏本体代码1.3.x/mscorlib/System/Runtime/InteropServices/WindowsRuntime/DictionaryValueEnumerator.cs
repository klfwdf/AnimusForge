using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009D0 RID: 2512
	[Serializable]
	internal sealed class DictionaryValueEnumerator<TKey, TValue> : IEnumerator<TValue>, IDisposable, IEnumerator
	{
		// Token: 0x060063F9 RID: 25593 RVA: 0x00155096 File Offset: 0x00153296
		public DictionaryValueEnumerator(IDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
			this.enumeration = dictionary.GetEnumerator();
		}

		// Token: 0x060063FA RID: 25594 RVA: 0x001550BF File Offset: 0x001532BF
		void IDisposable.Dispose()
		{
			this.enumeration.Dispose();
		}

		// Token: 0x060063FB RID: 25595 RVA: 0x001550CC File Offset: 0x001532CC
		public bool MoveNext()
		{
			return this.enumeration.MoveNext();
		}

		// Token: 0x1700114B RID: 4427
		// (get) Token: 0x060063FC RID: 25596 RVA: 0x001550D9 File Offset: 0x001532D9
		object IEnumerator.Current
		{
			get
			{
				return ((IEnumerator<TValue>)this).Current;
			}
		}

		// Token: 0x1700114C RID: 4428
		// (get) Token: 0x060063FD RID: 25597 RVA: 0x001550E8 File Offset: 0x001532E8
		public TValue Current
		{
			get
			{
				KeyValuePair<TKey, TValue> keyValuePair = this.enumeration.Current;
				return keyValuePair.Value;
			}
		}

		// Token: 0x060063FE RID: 25598 RVA: 0x00155108 File Offset: 0x00153308
		public void Reset()
		{
			this.enumeration = this.dictionary.GetEnumerator();
		}

		// Token: 0x04002CEA RID: 11498
		private readonly IDictionary<TKey, TValue> dictionary;

		// Token: 0x04002CEB RID: 11499
		private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
	}
}
