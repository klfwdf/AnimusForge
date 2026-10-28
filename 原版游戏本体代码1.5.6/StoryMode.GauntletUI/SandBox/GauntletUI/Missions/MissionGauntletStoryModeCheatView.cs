using System.Collections.Generic;
using SandBox.ViewModelCollection.Map.Cheat;
using StoryMode.GameComponents.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionCheatView))]
public class MissionGauntletStoryModeCheatView : MissionCheatView
{
	private GauntletLayer _gauntletLayer;

	private GameplayCheatsVM _dataSource;

	private bool _isActive;

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		FinalizeScreen();
	}

	public override bool GetIsCheatsAvailable()
	{
		AchievementsCampaignBehavior obj = Campaign.Current?.GetCampaignBehavior<AchievementsCampaignBehavior>();
		if (obj == null)
		{
			return true;
		}
		TextObject reason;
		return !obj.CheckAchievementSystemActivity(out reason);
	}

	public override void InitializeScreen()
	{
		if (!_isActive)
		{
			_isActive = true;
			IEnumerable<GameplayCheatBase> missionCheatList = GameplayCheatsManager.GetMissionCheatList();
			_dataSource = new GameplayCheatsVM(FinalizeScreen, missionCheatList);
			InitializeKeyVisuals();
			_gauntletLayer = new GauntletLayer("MapCheats", 4500);
			_gauntletLayer.LoadMovie("MapCheats", _dataSource);
			_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
			base.MissionScreen.AddLayer(_gauntletLayer);
		}
	}

	public override void FinalizeScreen()
	{
		if (_isActive)
		{
			_isActive = false;
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_dataSource?.OnFinalize();
			_gauntletLayer = null;
			_dataSource = null;
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_isActive)
		{
			HandleInput();
		}
	}

	private void HandleInput()
	{
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource?.ExecuteClose();
		}
	}

	private void InitializeKeyVisuals()
	{
		_dataSource.SetCloseInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
	}
}
