using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000644 RID: 1604
	[ComVisible(true)]
	[Serializable]
	public struct Label
	{
		// Token: 0x06004B06 RID: 19206 RVA: 0x0010FE8C File Offset: 0x0010E08C
		internal Label(int label)
		{
			this.m_label = label;
		}

		// Token: 0x06004B07 RID: 19207 RVA: 0x0010FE95 File Offset: 0x0010E095
		internal int GetLabelValue()
		{
			return this.m_label;
		}

		// Token: 0x06004B08 RID: 19208 RVA: 0x0010FE9D File Offset: 0x0010E09D
		public override int GetHashCode()
		{
			return this.m_label;
		}

		// Token: 0x06004B09 RID: 19209 RVA: 0x0010FEA5 File Offset: 0x0010E0A5
		public override bool Equals(object obj)
		{
			return obj is Label && this.Equals((Label)obj);
		}

		// Token: 0x06004B0A RID: 19210 RVA: 0x0010FEBD File Offset: 0x0010E0BD
		public bool Equals(Label obj)
		{
			return obj.m_label == this.m_label;
		}

		// Token: 0x06004B0B RID: 19211 RVA: 0x0010FECD File Offset: 0x0010E0CD
		public static bool operator ==(Label a, Label b)
		{
			return a.Equals(b);
		}

		// Token: 0x06004B0C RID: 19212 RVA: 0x0010FED7 File Offset: 0x0010E0D7
		public static bool operator !=(Label a, Label b)
		{
			return !(a == b);
		}

		// Token: 0x04001F06 RID: 7942
		internal int m_label;
	}
}
