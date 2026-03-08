using System;
using System.Collections;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000852 RID: 2130
	internal class DictionaryEnumeratorByKeys : IDictionaryEnumerator, IEnumerator
	{
		// Token: 0x06005A4E RID: 23118 RVA: 0x0013DA8D File Offset: 0x0013BC8D
		public DictionaryEnumeratorByKeys(IDictionary properties)
		{
			this._properties = properties;
			this._keyEnum = properties.Keys.GetEnumerator();
		}

		// Token: 0x06005A4F RID: 23119 RVA: 0x0013DAAD File Offset: 0x0013BCAD
		public bool MoveNext()
		{
			return this._keyEnum.MoveNext();
		}

		// Token: 0x06005A50 RID: 23120 RVA: 0x0013DABA File Offset: 0x0013BCBA
		public void Reset()
		{
			this._keyEnum.Reset();
		}

		// Token: 0x17000F0E RID: 3854
		// (get) Token: 0x06005A51 RID: 23121 RVA: 0x0013DAC7 File Offset: 0x0013BCC7
		public object Current
		{
			get
			{
				return this.Entry;
			}
		}

		// Token: 0x17000F0F RID: 3855
		// (get) Token: 0x06005A52 RID: 23122 RVA: 0x0013DAD4 File Offset: 0x0013BCD4
		public DictionaryEntry Entry
		{
			get
			{
				return new DictionaryEntry(this.Key, this.Value);
			}
		}

		// Token: 0x17000F10 RID: 3856
		// (get) Token: 0x06005A53 RID: 23123 RVA: 0x0013DAE7 File Offset: 0x0013BCE7
		public object Key
		{
			get
			{
				return this._keyEnum.Current;
			}
		}

		// Token: 0x17000F11 RID: 3857
		// (get) Token: 0x06005A54 RID: 23124 RVA: 0x0013DAF4 File Offset: 0x0013BCF4
		public object Value
		{
			get
			{
				return this._properties[this.Key];
			}
		}

		// Token: 0x04002903 RID: 10499
		private IDictionary _properties;

		// Token: 0x04002904 RID: 10500
		private IEnumerator _keyEnum;
	}
}
