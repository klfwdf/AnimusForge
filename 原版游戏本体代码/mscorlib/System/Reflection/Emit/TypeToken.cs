using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000667 RID: 1639
	[ComVisible(true)]
	[Serializable]
	public struct TypeToken
	{
		// Token: 0x06004EB6 RID: 20150 RVA: 0x0011BCBD File Offset: 0x00119EBD
		internal TypeToken(int str)
		{
			this.m_class = str;
		}

		// Token: 0x17000C6C RID: 3180
		// (get) Token: 0x06004EB7 RID: 20151 RVA: 0x0011BCC6 File Offset: 0x00119EC6
		public int Token
		{
			get
			{
				return this.m_class;
			}
		}

		// Token: 0x06004EB8 RID: 20152 RVA: 0x0011BCCE File Offset: 0x00119ECE
		public override int GetHashCode()
		{
			return this.m_class;
		}

		// Token: 0x06004EB9 RID: 20153 RVA: 0x0011BCD6 File Offset: 0x00119ED6
		public override bool Equals(object obj)
		{
			return obj is TypeToken && this.Equals((TypeToken)obj);
		}

		// Token: 0x06004EBA RID: 20154 RVA: 0x0011BCEE File Offset: 0x00119EEE
		public bool Equals(TypeToken obj)
		{
			return obj.m_class == this.m_class;
		}

		// Token: 0x06004EBB RID: 20155 RVA: 0x0011BCFE File Offset: 0x00119EFE
		public static bool operator ==(TypeToken a, TypeToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004EBC RID: 20156 RVA: 0x0011BD08 File Offset: 0x00119F08
		public static bool operator !=(TypeToken a, TypeToken b)
		{
			return !(a == b);
		}

		// Token: 0x040021D4 RID: 8660
		public static readonly TypeToken Empty;

		// Token: 0x040021D5 RID: 8661
		internal int m_class;
	}
}
