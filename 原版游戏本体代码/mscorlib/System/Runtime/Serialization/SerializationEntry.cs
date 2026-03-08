using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000742 RID: 1858
	[ComVisible(true)]
	public struct SerializationEntry
	{
		// Token: 0x17000D87 RID: 3463
		// (get) Token: 0x06005223 RID: 21027 RVA: 0x00120B06 File Offset: 0x0011ED06
		public object Value
		{
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x17000D88 RID: 3464
		// (get) Token: 0x06005224 RID: 21028 RVA: 0x00120B0E File Offset: 0x0011ED0E
		public string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x17000D89 RID: 3465
		// (get) Token: 0x06005225 RID: 21029 RVA: 0x00120B16 File Offset: 0x0011ED16
		public Type ObjectType
		{
			get
			{
				return this.m_type;
			}
		}

		// Token: 0x06005226 RID: 21030 RVA: 0x00120B1E File Offset: 0x0011ED1E
		internal SerializationEntry(string entryName, object entryValue, Type entryType)
		{
			this.m_value = entryValue;
			this.m_name = entryName;
			this.m_type = entryType;
		}

		// Token: 0x04002459 RID: 9305
		private Type m_type;

		// Token: 0x0400245A RID: 9306
		private object m_value;

		// Token: 0x0400245B RID: 9307
		private string m_name;
	}
}
