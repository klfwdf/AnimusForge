using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000017 RID: 23
	public struct BannerIconData
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00004B70 File Offset: 0x00002D70
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x00004B78 File Offset: 0x00002D78
		public string MaterialName { get; private set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00004B81 File Offset: 0x00002D81
		// (set) Token: 0x060000F6 RID: 246 RVA: 0x00004B89 File Offset: 0x00002D89
		public int TextureIndex { get; private set; }

		// Token: 0x060000F7 RID: 247 RVA: 0x00004B92 File Offset: 0x00002D92
		public BannerIconData(string materialName, int textureIndex)
		{
			this.MaterialName = materialName;
			this.TextureIndex = textureIndex;
		}
	}
}
