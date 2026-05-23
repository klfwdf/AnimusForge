using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000661 RID: 1633
	[ComVisible(true)]
	[Serializable]
	public struct StringToken
	{
		// Token: 0x06004D3B RID: 19771 RVA: 0x00118650 File Offset: 0x00116850
		internal StringToken(int str)
		{
			this.m_string = str;
		}

		// Token: 0x17000C17 RID: 3095
		// (get) Token: 0x06004D3C RID: 19772 RVA: 0x00118659 File Offset: 0x00116859
		public int Token
		{
			get
			{
				return this.m_string;
			}
		}

		// Token: 0x06004D3D RID: 19773 RVA: 0x00118661 File Offset: 0x00116861
		public override int GetHashCode()
		{
			return this.m_string;
		}

		// Token: 0x06004D3E RID: 19774 RVA: 0x00118669 File Offset: 0x00116869
		public override bool Equals(object obj)
		{
			return obj is StringToken && this.Equals((StringToken)obj);
		}

		// Token: 0x06004D3F RID: 19775 RVA: 0x00118681 File Offset: 0x00116881
		public bool Equals(StringToken obj)
		{
			return obj.m_string == this.m_string;
		}

		// Token: 0x06004D40 RID: 19776 RVA: 0x00118691 File Offset: 0x00116891
		public static bool operator ==(StringToken a, StringToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004D41 RID: 19777 RVA: 0x0011869B File Offset: 0x0011689B
		public static bool operator !=(StringToken a, StringToken b)
		{
			return !(a == b);
		}

		// Token: 0x040021A8 RID: 8616
		internal int m_string;
	}
}
