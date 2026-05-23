using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200064F RID: 1615
	[ComVisible(true)]
	[Serializable]
	public struct MethodToken
	{
		// Token: 0x06004C01 RID: 19457 RVA: 0x00113210 File Offset: 0x00111410
		internal MethodToken(int str)
		{
			this.m_method = str;
		}

		// Token: 0x17000BEE RID: 3054
		// (get) Token: 0x06004C02 RID: 19458 RVA: 0x00113219 File Offset: 0x00111419
		public int Token
		{
			get
			{
				return this.m_method;
			}
		}

		// Token: 0x06004C03 RID: 19459 RVA: 0x00113221 File Offset: 0x00111421
		public override int GetHashCode()
		{
			return this.m_method;
		}

		// Token: 0x06004C04 RID: 19460 RVA: 0x00113229 File Offset: 0x00111429
		public override bool Equals(object obj)
		{
			return obj is MethodToken && this.Equals((MethodToken)obj);
		}

		// Token: 0x06004C05 RID: 19461 RVA: 0x00113241 File Offset: 0x00111441
		public bool Equals(MethodToken obj)
		{
			return obj.m_method == this.m_method;
		}

		// Token: 0x06004C06 RID: 19462 RVA: 0x00113251 File Offset: 0x00111451
		public static bool operator ==(MethodToken a, MethodToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004C07 RID: 19463 RVA: 0x0011325B File Offset: 0x0011145B
		public static bool operator !=(MethodToken a, MethodToken b)
		{
			return !(a == b);
		}

		// Token: 0x04001F54 RID: 8020
		public static readonly MethodToken Empty;

		// Token: 0x04001F55 RID: 8021
		internal int m_method;
	}
}
