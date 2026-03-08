using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL
{
	// Token: 0x02000037 RID: 55
	internal enum BlendingDestinationFactor : uint
	{
		// Token: 0x0400023A RID: 570
		Zero,
		// Token: 0x0400023B RID: 571
		One,
		// Token: 0x0400023C RID: 572
		SourceColor = 768U,
		// Token: 0x0400023D RID: 573
		OneMinusSourceColor,
		// Token: 0x0400023E RID: 574
		SourceAlpha,
		// Token: 0x0400023F RID: 575
		OneMinusSourceAlpha,
		// Token: 0x04000240 RID: 576
		DestinationAlpha,
		// Token: 0x04000241 RID: 577
		OneMinusDestinationAlpha,
		// Token: 0x04000242 RID: 578
		DestinationColor,
		// Token: 0x04000243 RID: 579
		OneMinusDestinationColor,
		// Token: 0x04000244 RID: 580
		SourceAlphaSaturate
	}
}
