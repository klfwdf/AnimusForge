using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerEndOfBattleUIHandler))]
public class MissionGauntletEndOfBattle : MissionView
{
	private MultiplayerEndOfBattleVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionLobbyComponent _lobbyComponent;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ViewOrderPriority = 30;
		_dataSource = new MultiplayerEndOfBattleVM();
		_gauntletLayer = new GauntletLayer("MultiplayerEndOfBattle", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerEndOfBattle", _dataSource);
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
	}

	private void OnPostMatchEnded()
	{
		_dataSource.OnBattleEnded();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.OnTick(dt);
	}
}
