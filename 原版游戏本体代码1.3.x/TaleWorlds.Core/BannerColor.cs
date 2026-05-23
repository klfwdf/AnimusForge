using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000019 RID: 25
	public struct BannerColor
	{
		// Token: 0x17000044 RID: 68
		// (get) Token: 0x0600010F RID: 271 RVA: 0x000053D4 File Offset: 0x000035D4
		// (set) Token: 0x06000110 RID: 272 RVA: 0x000053DC File Offset: 0x000035DC
		public uint Color { get; private set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000111 RID: 273 RVA: 0x000053E5 File Offset: 0x000035E5
		// (set) Token: 0x06000112 RID: 274 RVA: 0x000053ED File Offset: 0x000035ED
		public bool PlayerCanChooseForSigil { get; private set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000113 RID: 275 RVA: 0x000053F6 File Offset: 0x000035F6
		// (set) Token: 0x06000114 RID: 276 RVA: 0x000053FE File Offset: 0x000035FE
		public bool PlayerCanChooseForBackground { get; private set; }

		// Token: 0x06000115 RID: 277 RVA: 0x00005407 File Offset: 0x00003607
		public BannerColor(uint color, bool playerCanChooseForSigil, bool playerCanChooseForBackground)
		{
			this.Color = color;
			this.PlayerCanChooseForSigil = playerCanChooseForSigil;
			this.PlayerCanChooseForBackground = playerCanChooseForBackground;
		}
	}
}
