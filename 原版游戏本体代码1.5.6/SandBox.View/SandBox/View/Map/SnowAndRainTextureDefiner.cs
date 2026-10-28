using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;

namespace SandBox.View.Map;

public class SnowAndRainTextureDefiner : ScriptComponentBehavior
{
	[EditorVisibleScriptComponentVariable(true)]
	public Texture SnowAndRainTexture;

	[EditorVisibleScriptComponentVariable(true)]
	public int WeatherNodeGridWidthAndHeight;

	protected override void OnInit()
	{
		SetDataToScene();
	}

	protected override void OnTerrainReload(int step)
	{
		if (step == 1)
		{
			SetDataToScene();
		}
	}

	protected override void OnEditorInit()
	{
		if (base.GameEntity.Scene.ContainsTerrain)
		{
			base.GameEntity.Scene.SetDynamicSnowTexture(SnowAndRainTexture);
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "SnowAndRainTexture" && base.GameEntity.Scene.ContainsTerrain)
		{
			base.GameEntity.Scene.SetDynamicSnowTexture(SnowAndRainTexture);
		}
	}

	private void SetDataToScene()
	{
		if (SnowAndRainTexture != null)
		{
			((MapScene)Campaign.Current.MapSceneWrapper).SetSnowAndRainDataWithDimension(SnowAndRainTexture, WeatherNodeGridWidthAndHeight);
		}
	}
}
