using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL
{
	// Token: 0x0200003A RID: 58
	internal enum AttribueMask : uint
	{
		// Token: 0x0400024D RID: 589
		CurrentBit = 1U,
		// Token: 0x0400024E RID: 590
		PointBit,
		// Token: 0x0400024F RID: 591
		LineBit = 4U,
		// Token: 0x04000250 RID: 592
		PolygonBit = 8U,
		// Token: 0x04000251 RID: 593
		PolygonStippleBit = 16U,
		// Token: 0x04000252 RID: 594
		PixelModeBit = 32U,
		// Token: 0x04000253 RID: 595
		LightingBit = 64U,
		// Token: 0x04000254 RID: 596
		FogBit = 128U,
		// Token: 0x04000255 RID: 597
		DepthBufferBit = 256U,
		// Token: 0x04000256 RID: 598
		AccumBufferBit = 512U,
		// Token: 0x04000257 RID: 599
		StencilBufferBit = 1024U,
		// Token: 0x04000258 RID: 600
		ViewportBit = 2048U,
		// Token: 0x04000259 RID: 601
		TransformBit = 4096U,
		// Token: 0x0400025A RID: 602
		EnableBit = 8192U,
		// Token: 0x0400025B RID: 603
		ColorBufferBit = 16384U,
		// Token: 0x0400025C RID: 604
		HintBit = 32768U,
		// Token: 0x0400025D RID: 605
		EvalBit = 65536U,
		// Token: 0x0400025E RID: 606
		ListBit = 131072U,
		// Token: 0x0400025F RID: 607
		TextureBit = 262144U,
		// Token: 0x04000260 RID: 608
		ScissorBit = 524288U,
		// Token: 0x04000261 RID: 609
		AllAttribBits = 1048575U
	}
}
