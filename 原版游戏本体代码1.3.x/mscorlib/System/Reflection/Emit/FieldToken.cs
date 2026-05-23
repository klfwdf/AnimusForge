using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200063C RID: 1596
	[ComVisible(true)]
	[Serializable]
	public struct FieldToken
	{
		// Token: 0x06004A93 RID: 19091 RVA: 0x0010D9AD File Offset: 0x0010BBAD
		internal FieldToken(int field, Type fieldClass)
		{
			this.m_fieldTok = field;
			this.m_class = fieldClass;
		}

		// Token: 0x17000BA9 RID: 2985
		// (get) Token: 0x06004A94 RID: 19092 RVA: 0x0010D9BD File Offset: 0x0010BBBD
		public int Token
		{
			get
			{
				return this.m_fieldTok;
			}
		}

		// Token: 0x06004A95 RID: 19093 RVA: 0x0010D9C5 File Offset: 0x0010BBC5
		public override int GetHashCode()
		{
			return this.m_fieldTok;
		}

		// Token: 0x06004A96 RID: 19094 RVA: 0x0010D9CD File Offset: 0x0010BBCD
		public override bool Equals(object obj)
		{
			return obj is FieldToken && this.Equals((FieldToken)obj);
		}

		// Token: 0x06004A97 RID: 19095 RVA: 0x0010D9E5 File Offset: 0x0010BBE5
		public bool Equals(FieldToken obj)
		{
			return obj.m_fieldTok == this.m_fieldTok && obj.m_class == this.m_class;
		}

		// Token: 0x06004A98 RID: 19096 RVA: 0x0010DA05 File Offset: 0x0010BC05
		public static bool operator ==(FieldToken a, FieldToken b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004A99 RID: 19097 RVA: 0x0010DA0F File Offset: 0x0010BC0F
		public static bool operator !=(FieldToken a, FieldToken b)
		{
			return !(a == b);
		}

		// Token: 0x04001EBC RID: 7868
		public static readonly FieldToken Empty;

		// Token: 0x04001EBD RID: 7869
		internal int m_fieldTok;

		// Token: 0x04001EBE RID: 7870
		internal object m_class;
	}
}
