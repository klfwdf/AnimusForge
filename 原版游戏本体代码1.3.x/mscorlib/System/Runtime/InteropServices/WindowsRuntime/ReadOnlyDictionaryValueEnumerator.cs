using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E8 RID: 2536
	[Serializable]
	internal sealed class ReadOnlyDictionaryValueEnumerator<TKey, TValue> : IEnumerator<TValue>, IDisposable, IEnumerator
	{
		// Token: 0x06006497 RID: 25751 RVA: 0x00156DD9 File Offset: 0x00154FD9
		public ReadOnlyDictionaryValueEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
			this.enumeration = dictionary.GetEnumerator();
		}

		// Token: 0x06006498 RID: 25752 RVA: 0x00156E02 File Offset: 0x00155002
		void IDisposable.Dispose()
		{
			this.enumeration.Dispose();
		}

		// Token: 0x06006499 RID: 25753 RVA: 0x00156E0F File Offset: 0x0015500F
		public bool MoveNext()
		{
			return this.enumeration.MoveNext();
		}

		// Token: 0x17001155 RID: 4437
		// (get) Token: 0x0600649A RID: 25754 RVA: 0x00156E1C File Offset: 0x0015501C
		object IEnumerator.Current
		{
			get
			{
				return ((IEnumerator<TValue>)this).Current;
			}
		}

		// Token: 0x17001156 RID: 4438
		// (get) Token: 0x0600649B RID: 25755 RVA: 0x00156E2C File Offset: 0x0015502C
		public TValue Current
		{
			get
			{
				KeyValuePair<TKey, TValue> keyValuePair = this.enumeration.Current;
				return keyValuePair.Value;
			}
		}

		// Token: 0x0600649C RID: 25756 RVA: 0x00156E4C File Offset: 0x0015504C
		public void Reset()
		{
			this.enumeration = this.dictionary.GetEnumerator();
		}

		// Token: 0x04002CF7 RID: 11511
		private readonly IReadOnlyDictionary<TKey, TValue> dictionary;

		// Token: 0x04002CF8 RID: 11512
		private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
	}
}
