using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000660 RID: 1632
	[ComVisible(true)]
	public struct SignatureToken
	{
		// Token: 0x06004D33 RID: 19763 RVA: 0x001185F0 File Offset: 0x001167F0
		internal SignatureToken(int str, ModuleBuilder mod)
		{
			this.m_signature = str;
			this.m_moduleBuilder = mod;
		}

		// Token: 0x17000C16 RID: 3094
		// (get) Token: 0x06004D34 RID: 19764 RVA: 0x00118600 File Offset: 0x00116800
		public int Token
		{
			get
			{
				return this.m_signature;
			}
		}

		// Token: 0x06004D35 RID: 19765 RVA: 0x00118608 File Offset: 0x00116808
		public override int GetHashCode()
		{
			return this.m_signature;
		}

		// Token: 0x06004D36 RID: 19766 RVA: 0x00118610 File Offset: 0x00116810
		public override bool Equals(object obj)
		{
			return obj is SignatureToken && this.Equals((SignatureToken)obj);
		}

		// Token: 0x06004D37 RID: 19767 RVA: 0x00118628 File Offset: 0x00116828
		public bool Equals(SignatureToken obj)
		{
			return obj.m_signature == this.m_signature;
		}

		// Token: 0x06004D38 RID: 19768 RVA: 0x00118638 File Offset: 0x00116838
		public static bool operator ==(SignatureToken a, SignatureToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004D39 RID: 19769 RVA: 0x00118642 File Offset: 0x00116842
		public static bool operator !=(SignatureToken a, SignatureToken b)
		{
			return !(a == b);
		}

		// Token: 0x040021A5 RID: 8613
		public static readonly SignatureToken Empty;

		// Token: 0x040021A6 RID: 8614
		internal int m_signature;

		// Token: 0x040021A7 RID: 8615
		internal ModuleBuilder m_moduleBuilder;
	}
}
