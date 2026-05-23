using System;

namespace TaleWorlds.TwoDimension.Standalone.Native.OpenGL
{
	// Token: 0x02000036 RID: 54
	internal enum BlendingSourceFactor : uint
	{
		// Token: 0x0400022E RID: 558
		Zero,
		// Token: 0x0400022F RID: 559
		One,
		// Token: 0x04000230 RID: 560
		SourceColor = 768U,
		// Token: 0x04000231 RID: 561
		OneMinusSourceColor,
		// Token: 0x04000232 RID: 562
		SourceAlpha,
		// Token: 0x04000233 RID: 563
		OneMinusSourceAlpha,
		// Token: 0x04000234 RID: 564
		DestinationAlpha,
		// Token: 0x04000235 RID: 565
		OneMinusDestinationAlpha,
		// Token: 0x04000236 RID: 566
		DestinationColor,
		// Token: 0x04000237 RID: 567
		OneMinusDestinationColor,
		// Token: 0x04000238 RID: 568
		SourceAlphaSaturate
	}
}
