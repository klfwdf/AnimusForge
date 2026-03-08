using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x02000499 RID: 1177
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct DictionaryEntry
	{
		// Token: 0x060038A3 RID: 14499 RVA: 0x000D9CF3 File Offset: 0x000D7EF3
		[__DynamicallyInvokable]
		public DictionaryEntry(object key, object value)
		{
			this._key = key;
			this._value = value;
		}

		// Token: 0x1700086B RID: 2155
		// (get) Token: 0x060038A4 RID: 14500 RVA: 0x000D9D03 File Offset: 0x000D7F03
		// (set) Token: 0x060038A5 RID: 14501 RVA: 0x000D9D0B File Offset: 0x000D7F0B
		[__DynamicallyInvokable]
		public object Key
		{
			[__DynamicallyInvokable]
			get
			{
				return this._key;
			}
			[__DynamicallyInvokable]
			set
			{
				this._key = value;
			}
		}

		// Token: 0x1700086C RID: 2156
		// (get) Token: 0x060038A6 RID: 14502 RVA: 0x000D9D14 File Offset: 0x000D7F14
		// (set) Token: 0x060038A7 RID: 14503 RVA: 0x000D9D1C File Offset: 0x000D7F1C
		[__DynamicallyInvokable]
		public object Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._value;
			}
			[__DynamicallyInvokable]
			set
			{
				this._value = value;
			}
		}

		// Token: 0x04001901 RID: 6401
		private object _key;

		// Token: 0x04001902 RID: 6402
		private object _value;
	}
}
