using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009CF RID: 2511
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	internal sealed class DictionaryValueCollection<TKey, TValue> : ICollection<!1>, IEnumerable<!1>, IEnumerable
	{
		// Token: 0x060063EF RID: 25583 RVA: 0x00154F0F File Offset: 0x0015310F
		public DictionaryValueCollection(IDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			this.dictionary = dictionary;
		}

		// Token: 0x060063F0 RID: 25584 RVA: 0x00154F2C File Offset: 0x0015312C
		public void CopyTo(TValue[] array, int index)
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
				array[num++] = keyValuePair.Value;
			}
		}

		// Token: 0x17001149 RID: 4425
		// (get) Token: 0x060063F1 RID: 25585 RVA: 0x00154FE4 File Offset: 0x001531E4
		public int Count
		{
			get
			{
				return this.dictionary.Count;
			}
		}

		// Token: 0x1700114A RID: 4426
		// (get) Token: 0x060063F2 RID: 25586 RVA: 0x00154FF1 File Offset: 0x001531F1
		bool ICollection<!1>.IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060063F3 RID: 25587 RVA: 0x00154FF4 File Offset: 0x001531F4
		void ICollection<!1>.Add(TValue item)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_ValueCollectionSet"));
		}

		// Token: 0x060063F4 RID: 25588 RVA: 0x00155005 File Offset: 0x00153205
		void ICollection<!1>.Clear()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_ValueCollectionSet"));
		}

		// Token: 0x060063F5 RID: 25589 RVA: 0x00155018 File Offset: 0x00153218
		public bool Contains(TValue item)
		{
			EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
			foreach (TValue y in this)
			{
				if (@default.Equals(item, y))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060063F6 RID: 25590 RVA: 0x00155070 File Offset: 0x00153270
		bool ICollection<!1>.Remove(TValue item)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_ValueCollectionSet"));
		}

		// Token: 0x060063F7 RID: 25591 RVA: 0x00155081 File Offset: 0x00153281
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!1>)this).GetEnumerator();
		}

		// Token: 0x060063F8 RID: 25592 RVA: 0x00155089 File Offset: 0x00153289
		public IEnumerator<TValue> GetEnumerator()
		{
			return new DictionaryValueEnumerator<TKey, TValue>(this.dictionary);
		}

		// Token: 0x04002CE9 RID: 11497
		private readonly IDictionary<TKey, TValue> dictionary;
	}
}
