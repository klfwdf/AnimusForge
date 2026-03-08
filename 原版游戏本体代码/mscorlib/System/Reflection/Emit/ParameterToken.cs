using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200065C RID: 1628
	[ComVisible(true)]
	[Serializable]
	public struct ParameterToken
	{
		// Token: 0x06004CC9 RID: 19657 RVA: 0x00116FEC File Offset: 0x001151EC
		internal ParameterToken(int tkParam)
		{
			this.m_tkParameter = tkParam;
		}

		// Token: 0x17000C09 RID: 3081
		// (get) Token: 0x06004CCA RID: 19658 RVA: 0x00116FF5 File Offset: 0x001151F5
		public int Token
		{
			get
			{
				return this.m_tkParameter;
			}
		}

		// Token: 0x06004CCB RID: 19659 RVA: 0x00116FFD File Offset: 0x001151FD
		public override int GetHashCode()
		{
			return this.m_tkParameter;
		}

		// Token: 0x06004CCC RID: 19660 RVA: 0x00117005 File Offset: 0x00115205
		public override bool Equals(object obj)
		{
			return obj is ParameterToken && this.Equals((ParameterToken)obj);
		}

		// Token: 0x06004CCD RID: 19661 RVA: 0x0011701D File Offset: 0x0011521D
		public bool Equals(ParameterToken obj)
		{
			return obj.m_tkParameter == this.m_tkParameter;
		}

		// Token: 0x06004CCE RID: 19662 RVA: 0x0011702D File Offset: 0x0011522D
		public static bool operator ==(ParameterToken a, ParameterToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004CCF RID: 19663 RVA: 0x00117037 File Offset: 0x00115237
		public static bool operator !=(ParameterToken a, ParameterToken b)
		{
			return !(a == b);
		}

		// Token: 0x04002190 RID: 8592
		public static readonly ParameterToken Empty;

		// Token: 0x04002191 RID: 8593
		internal int m_tkParameter;
	}
}
