using System;
using System.Diagnostics;

namespace System.Collections
{
	// Token: 0x020004A3 RID: 1187
	[DebuggerDisplay("{value}", Name = "[{key}]", Type = "")]
	internal class KeyValuePairs
	{
		// Token: 0x060038CD RID: 14541 RVA: 0x000D9D25 File Offset: 0x000D7F25
		public KeyValuePairs(object key, object value)
		{
			this.value = value;
			this.key = key;
		}

		// Token: 0x1700087C RID: 2172
		// (get) Token: 0x060038CE RID: 14542 RVA: 0x000D9D3B File Offset: 0x000D7F3B
		public object Key
		{
			get
			{
				return this.key;
			}
		}

		// Token: 0x1700087D RID: 2173
		// (get) Token: 0x060038CF RID: 14543 RVA: 0x000D9D43 File Offset: 0x000D7F43
		public object Value
		{
			get
			{
				return this.value;
			}
		}

		// Token: 0x04001903 RID: 6403
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private object key;

		// Token: 0x04001904 RID: 6404
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private object value;
	}
}
