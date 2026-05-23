using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200063A RID: 1594
	[ComVisible(true)]
	[Serializable]
	public struct EventToken
	{
		// Token: 0x06004A71 RID: 19057 RVA: 0x0010D599 File Offset: 0x0010B799
		internal EventToken(int str)
		{
			this.m_event = str;
		}

		// Token: 0x17000BA0 RID: 2976
		// (get) Token: 0x06004A72 RID: 19058 RVA: 0x0010D5A2 File Offset: 0x0010B7A2
		public int Token
		{
			get
			{
				return this.m_event;
			}
		}

		// Token: 0x06004A73 RID: 19059 RVA: 0x0010D5AA File Offset: 0x0010B7AA
		public override int GetHashCode()
		{
			return this.m_event;
		}

		// Token: 0x06004A74 RID: 19060 RVA: 0x0010D5B2 File Offset: 0x0010B7B2
		public override bool Equals(object obj)
		{
			return obj is EventToken && this.Equals((EventToken)obj);
		}

		// Token: 0x06004A75 RID: 19061 RVA: 0x0010D5CA File Offset: 0x0010B7CA
		public bool Equals(EventToken obj)
		{
			return obj.m_event == this.m_event;
		}

		// Token: 0x06004A76 RID: 19062 RVA: 0x0010D5DA File Offset: 0x0010B7DA
		public static bool operator ==(EventToken a, EventToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004A77 RID: 19063 RVA: 0x0010D5E4 File Offset: 0x0010B7E4
		public static bool operator !=(EventToken a, EventToken b)
		{
			return !(a == b);
		}

		// Token: 0x04001EB4 RID: 7860
		public static readonly EventToken Empty;

		// Token: 0x04001EB5 RID: 7861
		internal int m_event;
	}
}
