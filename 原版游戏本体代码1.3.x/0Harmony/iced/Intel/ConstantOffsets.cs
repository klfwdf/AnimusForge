using System;

namespace Iced.Intel
{
	// Token: 0x02000637 RID: 1591
	internal struct ConstantOffsets
	{
		// Token: 0x17000767 RID: 1895
		// (get) Token: 0x06002113 RID: 8467 RVA: 0x00068D48 File Offset: 0x00066F48
		public readonly bool HasDisplacement
		{
			get
			{
				return this.DisplacementSize > 0;
			}
		}

		// Token: 0x17000768 RID: 1896
		// (get) Token: 0x06002114 RID: 8468 RVA: 0x00068D53 File Offset: 0x00066F53
		public readonly bool HasImmediate
		{
			get
			{
				return this.ImmediateSize > 0;
			}
		}

		// Token: 0x17000769 RID: 1897
		// (get) Token: 0x06002115 RID: 8469 RVA: 0x00068D5E File Offset: 0x00066F5E
		public readonly bool HasImmediate2
		{
			get
			{
				return this.ImmediateSize2 > 0;
			}
		}

		// Token: 0x040029E7 RID: 10727
		public byte DisplacementOffset;

		// Token: 0x040029E8 RID: 10728
		public byte DisplacementSize;

		// Token: 0x040029E9 RID: 10729
		public byte ImmediateOffset;

		// Token: 0x040029EA RID: 10730
		public byte ImmediateSize;

		// Token: 0x040029EB RID: 10731
		public byte ImmediateOffset2;

		// Token: 0x040029EC RID: 10732
		public byte ImmediateSize2;

		// Token: 0x040029ED RID: 10733
		private byte pad1;

		// Token: 0x040029EE RID: 10734
		private byte pad2;
	}
}
