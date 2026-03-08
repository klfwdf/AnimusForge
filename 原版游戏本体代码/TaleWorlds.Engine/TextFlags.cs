using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200006C RID: 108
	[Flags]
	[EngineStruct("rglText_flags", false, null, FirstCharacterUppercase = false)]
	public enum TextFlags
	{
		// Token: 0x0400014A RID: 330
		RglTfNone = 0,
		// Token: 0x0400014B RID: 331
		RglTfHAlignLeft = 1,
		// Token: 0x0400014C RID: 332
		RglTfHAlignRight = 2,
		// Token: 0x0400014D RID: 333
		RglTfHAlignCenter = 3,
		// Token: 0x0400014E RID: 334
		RglTfVAlignTop = 4,
		// Token: 0x0400014F RID: 335
		RglTfVAlignDown = 8,
		// Token: 0x04000150 RID: 336
		RglTfVAlignCenter = 12,
		// Token: 0x04000151 RID: 337
		RglTfSingleLine = 16,
		// Token: 0x04000152 RID: 338
		RglTfMultiline = 32,
		// Token: 0x04000153 RID: 339
		RglTfItalic = 64,
		// Token: 0x04000154 RID: 340
		RglTfCutTextFromLeft = 128,
		// Token: 0x04000155 RID: 341
		RglTfDoubleSpace = 256,
		// Token: 0x04000156 RID: 342
		RglTfWithOutline = 512,
		// Token: 0x04000157 RID: 343
		RglTfHalfSpace = 1024
	}
}
