using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map
{
	// Token: 0x0200005F RID: 95
	public class SnowAndRainTextureDefiner : ScriptComponentBehavior
	{
		// Token: 0x060003C1 RID: 961 RVA: 0x0001E16C File Offset: 0x0001C36C
		protected override void OnInit()
		{
			this.SetDataToScene();
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x0001E174 File Offset: 0x0001C374
		protected override void OnTerrainReload(int step)
		{
			if (step == 1)
			{
				this.SetDataToScene();
			}
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0001E180 File Offset: 0x0001C380
		protected override void OnEditorInit()
		{
			if (base.GameEntity.Scene.ContainsTerrain)
			{
				base.GameEntity.Scene.SetDynamicSnowTexture(this.SnowAndRainTexture);
			}
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0001E1BC File Offset: 0x0001C3BC
		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == "SnowAndRainTexture" && base.GameEntity.Scene.ContainsTerrain)
			{
				base.GameEntity.Scene.SetDynamicSnowTexture(this.SnowAndRainTexture);
			}
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x0001E204 File Offset: 0x0001C404
		private void SetDataToScene()
		{
			if (this.SnowAndRainTexture != null)
			{
				((MapScene)Campaign.Current.MapSceneWrapper).SetSnowAndRainDataWithDimension(this.SnowAndRainTexture, this.WeatherNodeGridWidthAndHeight);
			}
		}

		// Token: 0x040001F2 RID: 498
		[EditorVisibleScriptComponentVariable(true)]
		public Texture SnowAndRainTexture;

		// Token: 0x040001F3 RID: 499
		[EditorVisibleScriptComponentVariable(true)]
		public int WeatherNodeGridWidthAndHeight;
	}
}
