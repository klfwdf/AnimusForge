using System;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200002C RID: 44
	public readonly struct ImageFitResult
	{
		// Token: 0x06000349 RID: 841 RVA: 0x0000EBA3 File Offset: 0x0000CDA3
		public ImageFitResult(float offsetX, float offsetY, float width, float height)
		{
			this.OffsetX = offsetX;
			this.OffsetY = offsetY;
			this.Width = width;
			this.Height = height;
		}

		// Token: 0x04000196 RID: 406
		public readonly float OffsetX;

		// Token: 0x04000197 RID: 407
		public readonly float OffsetY;

		// Token: 0x04000198 RID: 408
		public readonly float Width;

		// Token: 0x04000199 RID: 409
		public readonly float Height;
	}
}
