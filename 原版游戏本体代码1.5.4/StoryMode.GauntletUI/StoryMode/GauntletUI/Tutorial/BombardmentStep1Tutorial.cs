using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.MapSiege;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("BombardmentStep1")]
public class BombardmentStep1Tutorial : TutorialItemBase
{
	private bool _playerSelectedSiegeEngine;

	private bool _isGameMenuChangedAfterActivation;

	private bool _isActivated;

	public BombardmentStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (!_playerSelectedSiegeEngine)
		{
			return _isGameMenuChangedAfterActivation;
		}
		return true;
	}

	public override void OnPlayerStartEngineConstruction(PlayerStartEngineConstructionEvent obj)
	{
		_playerSelectedSiegeEngine = true;
	}

	public override void OnGameMenuOptionSelected(GameMenuOption obj)
	{
		base.OnGameMenuOptionSelected(obj);
		if (_isActivated)
		{
			_isGameMenuChangedAfterActivation = true;
		}
	}

	public override void OnGameMenuOpened(MenuCallbackArgs obj)
	{
		base.OnGameMenuOpened(obj);
		if (_isActivated)
		{
			_isGameMenuChangedAfterActivation = true;
		}
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		int isActivated;
		if (Campaign.Current.CurrentMenuContext?.GameMenu.StringId == "menu_siege_strategies")
		{
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			if (playerSiegeEvent != null && playerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide).SiegeEngines?.SiegePreparations?.IsActive == true)
			{
				isActivated = ((TutorialHelper.CurrentContext == TutorialContexts.MapWindow) ? 1 : 0);
				goto IL_0092;
			}
		}
		isActivated = 0;
		goto IL_0092;
		IL_0092:
		_isActivated = (byte)isActivated != 0;
		return _isActivated;
	}
}
