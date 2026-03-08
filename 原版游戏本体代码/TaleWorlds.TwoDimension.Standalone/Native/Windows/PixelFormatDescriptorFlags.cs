using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000021 RID: 33
	[Flags]
	internal enum PixelFormatDescriptorFlags : uint
	{
		// Token: 0x040000A0 RID: 160
		DoubleBuffer = 1U,
		// Token: 0x040000A1 RID: 161
		Stereo = 2U,
		// Token: 0x040000A2 RID: 162
		DrawToWindow = 4U,
		// Token: 0x040000A3 RID: 163
		DrawToBitmap = 8U,
		// Token: 0x040000A4 RID: 164
		SupportGDI = 16U,
		// Token: 0x040000A5 RID: 165
		SupportOpengl = 32U,
		// Token: 0x040000A6 RID: 166
		GenericFormat = 64U,
		// Token: 0x040000A7 RID: 167
		NeedPalette = 128U,
		// Token: 0x040000A8 RID: 168
		NeedSystemPalette = 256U,
		// Token: 0x040000A9 RID: 169
		SwapExchange = 512U,
		// Token: 0x040000AA RID: 170
		SwapCopy = 1024U,
		// Token: 0x040000AB RID: 171
		SwapLayerBuffers = 2048U,
		// Token: 0x040000AC RID: 172
		GenericAccelerated = 4096U,
		// Token: 0x040000AD RID: 173
		SupportDirectDraw = 8192U,
		// Token: 0x040000AE RID: 174
		Direct3DAccelerated = 16384U,
		// Token: 0x040000AF RID: 175
		SupportComposition = 32768U
	}
}
