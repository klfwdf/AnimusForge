using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000060 RID: 96
	internal class SpriteFromTexture : Sprite
	{
		// Token: 0x170001CE RID: 462
		// (get) Token: 0x0600066C RID: 1644 RVA: 0x0001B961 File Offset: 0x00019B61
		public override Texture Texture
		{
			get
			{
				return this._texture;
			}
		}

		// Token: 0x0600066D RID: 1645 RVA: 0x0001B969 File Offset: 0x00019B69
		public override Vec2 GetMinUvs()
		{
			return Vec2.Zero;
		}

		// Token: 0x0600066E RID: 1646 RVA: 0x0001B970 File Offset: 0x00019B70
		public override Vec2 GetMaxUvs()
		{
			return Vec2.One;
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x0001B977 File Offset: 0x00019B77
		public SpriteFromTexture(Texture texture, int width, int height)
			: base("Sprite", width, height, SpriteNinePatchParameters.Empty)
		{
			this._texture = texture;
		}

		// Token: 0x04000301 RID: 769
		private Texture _texture;
	}
}
