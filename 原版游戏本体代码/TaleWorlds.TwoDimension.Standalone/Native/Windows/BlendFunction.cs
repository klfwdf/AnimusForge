using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000017 RID: 23
	public struct BlendFunction
	{
		// Token: 0x060000FB RID: 251 RVA: 0x00005585 File Offset: 0x00003785
		public BlendFunction(AlphaFormatFlags op, byte flags, byte alpha, AlphaFormatFlags format)
		{
			this.BlendOp = (byte)op;
			this.BlendFlags = flags;
			this.SourceConstantAlpha = alpha;
			this.AlphaFormat = (byte)format;
		}

		// Token: 0x04000070 RID: 112
		public byte BlendOp;

		// Token: 0x04000071 RID: 113
		public byte BlendFlags;

		// Token: 0x04000072 RID: 114
		public byte SourceConstantAlpha;

		// Token: 0x04000073 RID: 115
		public byte AlphaFormat;

		// Token: 0x04000074 RID: 116
		public static readonly BlendFunction Default = new BlendFunction(AlphaFormatFlags.Over, 0, byte.MaxValue, AlphaFormatFlags.Alpha);
	}
}
