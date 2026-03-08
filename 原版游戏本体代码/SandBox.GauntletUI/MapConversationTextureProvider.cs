using System;
using SandBox.View.Map;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x02000010 RID: 16
	public class MapConversationTextureProvider : TextureProvider
	{
		// Token: 0x17000009 RID: 9
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00007608 File Offset: 0x00005808
		public object Data
		{
			set
			{
				this._mapConversationTableau.SetData(value);
			}
		}

		// Token: 0x1700000A RID: 10
		// (set) Token: 0x060000C0 RID: 192 RVA: 0x00007616 File Offset: 0x00005816
		public bool IsEnabled
		{
			set
			{
				this._mapConversationTableau.SetEnabled(value);
			}
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00007624 File Offset: 0x00005824
		public MapConversationTextureProvider()
		{
			this._mapConversationTableau = new MapConversationTableau();
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00007637 File Offset: 0x00005837
		public override void Clear(bool clearNextFrame)
		{
			this._mapConversationTableau.OnFinalize(clearNextFrame);
			base.Clear(clearNextFrame);
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x0000764C File Offset: 0x0000584C
		private void CheckTexture()
		{
			if (this._texture != this._mapConversationTableau.Texture)
			{
				this._texture = this._mapConversationTableau.Texture;
				if (this._texture != null)
				{
					EngineTexture platformTexture = new EngineTexture(this._texture);
					this._providedTexture = new TaleWorlds.TwoDimension.Texture(platformTexture);
					return;
				}
				this._providedTexture = null;
			}
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000076B0 File Offset: 0x000058B0
		protected override TaleWorlds.TwoDimension.Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
		{
			this.CheckTexture();
			return this._providedTexture;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x000076BE File Offset: 0x000058BE
		public override void SetTargetSize(int width, int height)
		{
			base.SetTargetSize(width, height);
			this._mapConversationTableau.SetTargetSize(width, height);
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x000076D5 File Offset: 0x000058D5
		public override void Tick(float dt)
		{
			base.Tick(dt);
			this.CheckTexture();
			this._mapConversationTableau.OnTick(dt);
		}

		// Token: 0x04000054 RID: 84
		private MapConversationTableau _mapConversationTableau;

		// Token: 0x04000055 RID: 85
		private TaleWorlds.Engine.Texture _texture;

		// Token: 0x04000056 RID: 86
		private TaleWorlds.TwoDimension.Texture _providedTexture;
	}
}
