using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000446 RID: 1094
	internal class EventPayload : IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
	{
		// Token: 0x0600360E RID: 13838 RVA: 0x000D24F5 File Offset: 0x000D06F5
		internal EventPayload(List<string> payloadNames, List<object> payloadValues)
		{
			this.m_names = payloadNames;
			this.m_values = payloadValues;
		}

		// Token: 0x170007FE RID: 2046
		// (get) Token: 0x0600360F RID: 13839 RVA: 0x000D250B File Offset: 0x000D070B
		public ICollection<string> Keys
		{
			get
			{
				return this.m_names;
			}
		}

		// Token: 0x170007FF RID: 2047
		// (get) Token: 0x06003610 RID: 13840 RVA: 0x000D2513 File Offset: 0x000D0713
		public ICollection<object> Values
		{
			get
			{
				return this.m_values;
			}
		}

		// Token: 0x17000800 RID: 2048
		public object this[string key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}
				int num = 0;
				foreach (string a in this.m_names)
				{
					if (a == key)
					{
						return this.m_values[num];
					}
					num++;
				}
				throw new KeyNotFoundException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06003613 RID: 13843 RVA: 0x000D25A3 File Offset: 0x000D07A3
		public void Add(string key, object value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06003614 RID: 13844 RVA: 0x000D25AA File Offset: 0x000D07AA
		public void Add(KeyValuePair<string, object> payloadEntry)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06003615 RID: 13845 RVA: 0x000D25B1 File Offset: 0x000D07B1
		public void Clear()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06003616 RID: 13846 RVA: 0x000D25B8 File Offset: 0x000D07B8
		public bool Contains(KeyValuePair<string, object> entry)
		{
			return this.ContainsKey(entry.Key);
		}

		// Token: 0x06003617 RID: 13847 RVA: 0x000D25C8 File Offset: 0x000D07C8
		public bool ContainsKey(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			foreach (string a in this.m_names)
			{
				if (a == key)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000801 RID: 2049
		// (get) Token: 0x06003618 RID: 13848 RVA: 0x000D2634 File Offset: 0x000D0834
		public int Count
		{
			get
			{
				return this.m_names.Count;
			}
		}

		// Token: 0x17000802 RID: 2050
		// (get) Token: 0x06003619 RID: 13849 RVA: 0x000D2641 File Offset: 0x000D0841
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600361A RID: 13850 RVA: 0x000D2644 File Offset: 0x000D0844
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			int num;
			for (int i = 0; i < this.Keys.Count; i = num + 1)
			{
				yield return new KeyValuePair<string, object>(this.m_names[i], this.m_values[i]);
				num = i;
			}
			yield break;
		}

		// Token: 0x0600361B RID: 13851 RVA: 0x000D2654 File Offset: 0x000D0854
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
		}

		// Token: 0x0600361C RID: 13852 RVA: 0x000D2669 File Offset: 0x000D0869
		public void CopyTo(KeyValuePair<string, object>[] payloadEntries, int count)
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600361D RID: 13853 RVA: 0x000D2670 File Offset: 0x000D0870
		public bool Remove(string key)
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600361E RID: 13854 RVA: 0x000D2677 File Offset: 0x000D0877
		public bool Remove(KeyValuePair<string, object> entry)
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600361F RID: 13855 RVA: 0x000D2680 File Offset: 0x000D0880
		public bool TryGetValue(string key, out object value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			int num = 0;
			foreach (string a in this.m_names)
			{
				if (a == key)
				{
					value = this.m_values[num];
					return true;
				}
				num++;
			}
			value = null;
			return false;
		}

		// Token: 0x04001830 RID: 6192
		private List<string> m_names;

		// Token: 0x04001831 RID: 6193
		private List<object> m_values;
	}
}
