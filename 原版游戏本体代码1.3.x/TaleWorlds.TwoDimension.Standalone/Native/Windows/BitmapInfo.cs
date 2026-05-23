using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000014 RID: 20
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BitmapInfo
	{
		// Token: 0x0400005D RID: 93
		public BitmapInfoHeader bmiHeader;

		// Token: 0x0400005E RID: 94
		public byte r;

		// Token: 0x0400005F RID: 95
		public byte g;

		// Token: 0x04000060 RID: 96
		public byte b;

		// Token: 0x04000061 RID: 97
		public byte a;
	}
}
