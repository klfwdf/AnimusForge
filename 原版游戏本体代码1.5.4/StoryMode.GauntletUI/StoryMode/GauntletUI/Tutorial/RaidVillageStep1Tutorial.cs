using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("RaidVillageStep1")]
public class RaidVillageStep1Tutorial : TutorialItemBase
{
	private bool _gameMenuChanged;

	private bool _villageRaidMenuOpened;

	public RaidVillageStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _gameMenuChanged;
	}

	public override void OnGameMenuOpened(MenuCallbackArgs obj)
	{
		if (_villageRaidMenuOpened && obj.MenuContext.GameMenu.StringId != TutorialHelper.ActiveVillageRaidGameMenuID)
		{
			_gameMenuChanged = true;
		}
	}

	public override void OnGameMenuOptionSelected(GameMenuOption obj)
	{
		base.OnGameMenuOptionSelected(obj);
		if (_villageRaidMenuOpened && Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == TutorialHelper.ActiveVillageRaidGameMenuID)
		{
			_gameMenuChanged = true;
		}
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		_villageRaidMenuOpened = TutorialHelper.IsActiveVillageRaidGameMenuOpen;
		return _villageRaidMenuOpened;
	}
}
