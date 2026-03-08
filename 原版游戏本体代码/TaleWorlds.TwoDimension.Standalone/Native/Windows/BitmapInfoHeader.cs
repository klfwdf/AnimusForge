using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000015 RID: 21
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BitmapInfoHeader
	{
		// Token: 0x04000062 RID: 98
		public uint biSize;

		// Token: 0x04000063 RID: 99
		public int biWidth;

		// Token: 0x04000064 RID: 100
		public int biHeight;

		// Token: 0x04000065 RID: 101
		public ushort biPlanes;

		// Token: 0x04000066 RID: 102
		public ushort biBitCount;

		// Token: 0x04000067 RID: 103
		public uint biCompression;

		// Token: 0x04000068 RID: 104
		public uint biSizeImage;

		// Token: 0x04000069 RID: 105
		public int biXPelsPerMeter;

		// Token: 0x0400006A RID: 106
		public int biYPelsPerMeter;

		// Token: 0x0400006B RID: 107
		public uint biClrUsed;

		// Token: 0x0400006C RID: 108
		public uint biClrImportant;
	}
}
