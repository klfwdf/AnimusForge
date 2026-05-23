using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009CD RID: 2509
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	internal sealed class DictionaryKeyCollection<TKey, TValue> : ICollection<!0>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x060063DF RID: 25567 RVA: 0x00154D4E File Offset: 0x00152F4E
		public DictionaryKeyCollection(IDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		// Token: 0x060063E0 RID: 25568 RVA: 0x00154D6C File Offset: 0x00152F6C
		public void CopyTo(TKey[] array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (array.Length <= index && this.Count > 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_IndexOutOfRangeException"));
			}
			if (array.Length - index < this.dictionary.Count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
			}
			int num = index;
			foreach (KeyValuePair<TKey, TValue> keyValuePair in this.dictionary)
			{
				array[num++] = keyValuePair.Key;
			}
		}

		// Token: 0x17001145 RID: 4421
		// (get) Token: 0x060063E1 RID: 25569 RVA: 0x00154E24 File Offset: 0x00153024
		public int Count
		{
			get
			{
				return this.dictionary.Count;
			}
		}

		// Token: 0x17001146 RID: 4422
		// (get) Token: 0x060063E2 RID: 25570 RVA: 0x00154E31 File Offset: 0x00153031
		bool ICollection<!0>.IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060063E3 RID: 25571 RVA: 0x00154E34 File Offset: 0x00153034
		void ICollection<!0>.Add(TKey item)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
		}

		// Token: 0x060063E4 RID: 25572 RVA: 0x00154E45 File Offset: 0x00153045
		void ICollection<!0>.Clear()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
		}

		// Token: 0x060063E5 RID: 25573 RVA: 0x00154E56 File Offset: 0x00153056
		public bool Contains(TKey item)
		{
			return this.dictionary.ContainsKey(item);
		}

		// Token: 0x060063E6 RID: 25574 RVA: 0x00154E64 File Offset: 0x00153064
		bool ICollection<!0>.Remove(TKey item)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
		}

		// Token: 0x060063E7 RID: 25575 RVA: 0x00154E75 File Offset: 0x00153075
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!0>)this).GetEnumerator();
		}

		// Token: 0x060063E8 RID: 25576 RVA: 0x00154E7D File Offset: 0x0015307D
		public IEnumerator<TKey> GetEnumerator()
		{
			return new DictionaryKeyEnumerator<TKey, TValue>(this.dictionary);
		}

		// Token: 0x04002CE6 RID: 11494
		private readonly IDictionary<TKey, TValue> dictionary;
	}
}
