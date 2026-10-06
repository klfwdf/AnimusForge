using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(QuestsState))]
public class GauntletQuestsScreen : ScreenBase, IGameStateListener
{
	protected QuestsVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _questCategory;

	private readonly QuestsState _questsState;

	public GauntletQuestsScreen(QuestsState questsState)
	{
		_questsState = questsState;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		InformationManager.HideAllMessages();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit") || _gauntletLayer.Input.IsHotKeyReleased("Confirm") || _gauntletLayer.Input.IsGameKeyReleased(42))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteClose();
		}
	}

	void IGameStateListener.OnActivate()
	{
		base.OnActivate();
		_questCategory = UIResourceManager.LoadSpriteCategory("ui_quest");
		_dataSource = new QuestsVM(CloseQuestsScreen);
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_gauntletLayer = new GauntletLayer("QuestScreen", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.LoadMovie("QuestsScreen", _dataSource);
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		ScreenManager.TrySetFocus(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.QuestsScreen));
		if (_questsState.InitialSelectedIssue != null)
		{
			_dataSource.SetSelectedIssue(_questsState.InitialSelectedIssue);
		}
		else if (_questsState.InitialSelectedQuest != null)
		{
			_dataSource.SetSelectedQuest(_questsState.InitialSelectedQuest);
		}
		else if (_questsState.InitialSelectedLog != null)
		{
			_dataSource.SetSelectedLog(_questsState.InitialSelectedLog);
		}
		UISoundsHelper.PlayUISound("event:/ui/panels/panel_quest_open");
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	void IGameStateListener.OnDeactivate()
	{
		base.OnDeactivate();
		_questCategory.Unload();
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		RemoveLayer(_gauntletLayer);
		Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.None));
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
		_dataSource?.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
	}

	private void CloseQuestsScreen()
	{
		Game.Current.GameStateManager.PopState();
	}
}
