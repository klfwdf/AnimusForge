using System;
using System.Collections;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000853 RID: 2131
	internal class AggregateDictionary : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x06005A55 RID: 23125 RVA: 0x0013DB07 File Offset: 0x0013BD07
		public AggregateDictionary(ICollection dictionaries)
		{
			this._dictionaries = dictionaries;
		}

		// Token: 0x17000F12 RID: 3858
		public virtual object this[object key]
		{
			get
			{
				foreach (object obj in this._dictionaries)
				{
					IDictionary dictionary = (IDictionary)obj;
					if (dictionary.Contains(key))
					{
						return dictionary[key];
					}
				}
				return null;
			}
			set
			{
				foreach (object obj in this._dictionaries)
				{
					IDictionary dictionary = (IDictionary)obj;
					if (dictionary.Contains(key))
					{
						dictionary[key] = value;
					}
				}
			}
		}

		// Token: 0x17000F13 RID: 3859
		// (get) Token: 0x06005A58 RID: 23128 RVA: 0x0013DBE4 File Offset: 0x0013BDE4
		public virtual ICollection Keys
		{
			get
			{
				ArrayList arrayList = new ArrayList();
				foreach (object obj in this._dictionaries)
				{
					IDictionary dictionary = (IDictionary)obj;
					ICollection keys = dictionary.Keys;
					if (keys != null)
					{
						foreach (object value in keys)
						{
							arrayList.Add(value);
						}
					}
				}
				return arrayList;
			}
		}

		// Token: 0x17000F14 RID: 3860
		// (get) Token: 0x06005A59 RID: 23129 RVA: 0x0013DC94 File Offset: 0x0013BE94
		public virtual ICollection Values
		{
			get
			{
				ArrayList arrayList = new ArrayList();
				foreach (object obj in this._dictionaries)
				{
					IDictionary dictionary = (IDictionary)obj;
					ICollection values = dictionary.Values;
					if (values != null)
					{
						foreach (object value in values)
						{
							arrayList.Add(value);
						}
					}
				}
				return arrayList;
			}
		}

		// Token: 0x06005A5A RID: 23130 RVA: 0x0013DD44 File Offset: 0x0013BF44
		public virtual bool Contains(object key)
		{
			foreach (object obj in this._dictionaries)
			{
				IDictionary dictionary = (IDictionary)obj;
				if (dictionary.Contains(key))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000F15 RID: 3861
		// (get) Token: 0x06005A5B RID: 23131 RVA: 0x0013DDA8 File Offset: 0x0013BFA8
		public virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000F16 RID: 3862
		// (get) Token: 0x06005A5C RID: 23132 RVA: 0x0013DDAB File Offset: 0x0013BFAB
		public virtual bool IsFixedSize
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06005A5D RID: 23133 RVA: 0x0013DDAE File Offset: 0x0013BFAE
		public virtual void Add(object key, object value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A5E RID: 23134 RVA: 0x0013DDB5 File Offset: 0x0013BFB5
		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A5F RID: 23135 RVA: 0x0013DDBC File Offset: 0x0013BFBC
		public virtual void Remove(object key)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A60 RID: 23136 RVA: 0x0013DDC3 File Offset: 0x0013BFC3
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return new DictionaryEnumeratorByKeys(this);
		}

		// Token: 0x06005A61 RID: 23137 RVA: 0x0013DDCB File Offset: 0x0013BFCB
		public virtual void CopyTo(Array array, int index)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000F17 RID: 3863
		// (get) Token: 0x06005A62 RID: 23138 RVA: 0x0013DDD4 File Offset: 0x0013BFD4
		public virtual int Count
		{
			get
			{
				int num = 0;
				foreach (object obj in this._dictionaries)
				{
					IDictionary dictionary = (IDictionary)obj;
					num += dictionary.Count;
				}
				return num;
			}
		}

		// Token: 0x17000F18 RID: 3864
		// (get) Token: 0x06005A63 RID: 23139 RVA: 0x0013DE34 File Offset: 0x0013C034
		public virtual object SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x17000F19 RID: 3865
		// (get) Token: 0x06005A64 RID: 23140 RVA: 0x0013DE37 File Offset: 0x0013C037
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06005A65 RID: 23141 RVA: 0x0013DE3A File Offset: 0x0013C03A
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new DictionaryEnumeratorByKeys(this);
		}

		// Token: 0x04002905 RID: 10501
		private ICollection _dictionaries;
	}
}
