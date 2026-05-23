using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200065E RID: 1630
	[ComVisible(true)]
	[Serializable]
	public struct PropertyToken
	{
		// Token: 0x06004CF3 RID: 19699 RVA: 0x001173BC File Offset: 0x001155BC
		internal PropertyToken(int str)
		{
			this.m_property = str;
		}

		// Token: 0x17000C14 RID: 3092
		// (get) Token: 0x06004CF4 RID: 19700 RVA: 0x001173C5 File Offset: 0x001155C5
		public int Token
		{
			get
			{
				return this.m_property;
			}
		}

		// Token: 0x06004CF5 RID: 19701 RVA: 0x001173CD File Offset: 0x001155CD
		public override int GetHashCode()
		{
			return this.m_property;
		}

		// Token: 0x06004CF6 RID: 19702 RVA: 0x001173D5 File Offset: 0x001155D5
		public override bool Equals(object obj)
		{
			return obj is PropertyToken && this.Equals((PropertyToken)obj);
		}

		// Token: 0x06004CF7 RID: 19703 RVA: 0x001173ED File Offset: 0x001155ED
		public bool Equals(PropertyToken obj)
		{
			return obj.m_property == this.m_property;
		}

		// Token: 0x06004CF8 RID: 19704 RVA: 0x001173FD File Offset: 0x001155FD
		public static bool operator ==(PropertyToken a, PropertyToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004CF9 RID: 19705 RVA: 0x00117407 File Offset: 0x00115607
		public static bool operator !=(PropertyToken a, PropertyToken b)
		{
			return !(a == b);
		}

		// Token: 0x0400219C RID: 8604
		public static readonly PropertyToken Empty;

		// Token: 0x0400219D RID: 8605
		internal int m_property;
	}
}
