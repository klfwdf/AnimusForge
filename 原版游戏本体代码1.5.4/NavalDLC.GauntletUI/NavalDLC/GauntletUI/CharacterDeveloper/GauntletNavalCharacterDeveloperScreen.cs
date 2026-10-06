using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.CharacterDeveloper;

[GameStateScreen(typeof(CharacterDeveloperState))]
public class GauntletNavalCharacterDeveloperScreen : GauntletCharacterDeveloperScreen
{
	private SpriteCategory _navalSpriteCategory;

	public GauntletNavalCharacterDeveloperScreen(CharacterDeveloperState clanState)
		: base(clanState)
	{
		_navalSpriteCategory = UIResourceManager.GetSpriteCategory("ui_naval_character_developer");
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_navalSpriteCategory.Load();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_navalSpriteCategory.Unload();
	}
}
