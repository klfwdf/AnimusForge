using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000743 RID: 1859
	[ComVisible(true)]
	public sealed class SerializationInfoEnumerator : IEnumerator
	{
		// Token: 0x06005227 RID: 21031 RVA: 0x00120B35 File Offset: 0x0011ED35
		internal SerializationInfoEnumerator(string[] members, object[] info, Type[] types, int numItems)
		{
			this.m_members = members;
			this.m_data = info;
			this.m_types = types;
			this.m_numItems = numItems - 1;
			this.m_currItem = -1;
			this.m_current = false;
		}

		// Token: 0x06005228 RID: 21032 RVA: 0x00120B6A File Offset: 0x0011ED6A
		public bool MoveNext()
		{
			if (this.m_currItem < this.m_numItems)
			{
				this.m_currItem++;
				this.m_current = true;
			}
			else
			{
				this.m_current = false;
			}
			return this.m_current;
		}

		// Token: 0x17000D8A RID: 3466
		// (get) Token: 0x06005229 RID: 21033 RVA: 0x00120BA0 File Offset: 0x0011EDA0
		object IEnumerator.Current
		{
			get
			{
				if (!this.m_current)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
				return new SerializationEntry(this.m_members[this.m_currItem], this.m_data[this.m_currItem], this.m_types[this.m_currItem]);
			}
		}

		// Token: 0x17000D8B RID: 3467
		// (get) Token: 0x0600522A RID: 21034 RVA: 0x00120BF8 File Offset: 0x0011EDF8
		public SerializationEntry Current
		{
			get
			{
				if (!this.m_current)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
				return new SerializationEntry(this.m_members[this.m_currItem], this.m_data[this.m_currItem], this.m_types[this.m_currItem]);
			}
		}

		// Token: 0x0600522B RID: 21035 RVA: 0x00120C49 File Offset: 0x0011EE49
		public void Reset()
		{
			this.m_currItem = -1;
			this.m_current = false;
		}

		// Token: 0x17000D8C RID: 3468
		// (get) Token: 0x0600522C RID: 21036 RVA: 0x00120C59 File Offset: 0x0011EE59
		public string Name
		{
			get
			{
				if (!this.m_current)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
				return this.m_members[this.m_currItem];
			}
		}

		// Token: 0x17000D8D RID: 3469
		// (get) Token: 0x0600522D RID: 21037 RVA: 0x00120C80 File Offset: 0x0011EE80
		public object Value
		{
			get
			{
				if (!this.m_current)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
				return this.m_data[this.m_currItem];
			}
		}

		// Token: 0x17000D8E RID: 3470
		// (get) Token: 0x0600522E RID: 21038 RVA: 0x00120CA7 File Offset: 0x0011EEA7
		public Type ObjectType
		{
			get
			{
				if (!this.m_current)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
				}
				return this.m_types[this.m_currItem];
			}
		}

		// Token: 0x0400245C RID: 9308
		private string[] m_members;

		// Token: 0x0400245D RID: 9309
		private object[] m_data;

		// Token: 0x0400245E RID: 9310
		private Type[] m_types;

		// Token: 0x0400245F RID: 9311
		private int m_numItems;

		// Token: 0x04002460 RID: 9312
		private int m_currItem;

		// Token: 0x04002461 RID: 9313
		private bool m_current;
	}
}
