using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x02000409 RID: 1033
	[ComVisible(true)]
	public struct SymbolToken
	{
		// Token: 0x060033E7 RID: 13287 RVA: 0x000C6780 File Offset: 0x000C4980
		public SymbolToken(int val)
		{
			this.m_token = val;
		}

		// Token: 0x060033E8 RID: 13288 RVA: 0x000C6789 File Offset: 0x000C4989
		public int GetToken()
		{
			return this.m_token;
		}

		// Token: 0x060033E9 RID: 13289 RVA: 0x000C6791 File Offset: 0x000C4991
		public override int GetHashCode()
		{
			return this.m_token;
		}

		// Token: 0x060033EA RID: 13290 RVA: 0x000C6799 File Offset: 0x000C4999
		public override bool Equals(object obj)
		{
			return obj is SymbolToken && this.Equals((SymbolToken)obj);
		}

		// Token: 0x060033EB RID: 13291 RVA: 0x000C67B1 File Offset: 0x000C49B1
		public bool Equals(SymbolToken obj)
		{
			return obj.m_token == this.m_token;
		}

		// Token: 0x060033EC RID: 13292 RVA: 0x000C67C1 File Offset: 0x000C49C1
		public static bool operator ==(SymbolToken a, SymbolToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x060033ED RID: 13293 RVA: 0x000C67CB File Offset: 0x000C49CB
		public static bool operator !=(SymbolToken a, SymbolToken b)
		{
			return !(a == b);
		}

		// Token: 0x04001707 RID: 5895
		internal int m_token;
	}
}
