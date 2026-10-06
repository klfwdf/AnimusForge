using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.TeamSelection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerCultureSelectUIHandler))]
public class MissionGauntletCultureSelection : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MultiplayerCultureSelectVM _dataSource;

	private MissionLobbyComponent _missionLobbyComponent;

	private bool _toOpen;

	public MissionGauntletCultureSelection()
	{
		ViewOrderPriority = 22;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_missionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_missionLobbyComponent.OnCultureSelectionRequested += OnCultureSelectionRequested;
	}

	public override void OnMissionScreenFinalize()
	{
		_missionLobbyComponent.OnCultureSelectionRequested -= OnCultureSelectionRequested;
		base.OnMissionScreenFinalize();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_toOpen && base.MissionScreen.SetDisplayDialog(value: true))
		{
			_toOpen = false;
			OnOpen();
		}
	}

	private void OnOpen()
	{
		_dataSource = new MultiplayerCultureSelectVM(OnCultureSelected, OnClose);
		_gauntletLayer = new GauntletLayer("MultiplayerCultureSelection", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerCultureSelection", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	private void OnClose()
	{
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		base.MissionScreen.SetDisplayDialog(value: false);
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	private void OnCultureSelectionRequested()
	{
		_toOpen = true;
	}

	private void OnCultureSelected(BasicCultureObject culture)
	{
		_missionLobbyComponent.OnCultureSelected(culture);
		OnClose();
	}
}
