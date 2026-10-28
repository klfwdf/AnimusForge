using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerMarkerUIHandler))]
public class MissionGauntletMultiplayerMarkerUIHandler : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MultiplayerMissionMarkerVM _dataSource;

	private bool _spectatorMarkersToggled;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MultiplayerMissionMarkerVM(base.MissionScreen.CombatCamera);
		_gauntletLayer = new GauntletLayer("MPMissionMarkers", 1);
		_gauntletLayer.LoadMovie("MPMissionMarkers", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (GameNetwork.IsMultiplayer && MultiplayerSpectatorHelper.IsLocalPeerSpectator())
		{
			if (base.Input.IsGameKeyPressed(5))
			{
				_spectatorMarkersToggled = !_spectatorMarkersToggled;
			}
			_dataSource.IsEnabled = _spectatorMarkersToggled;
		}
		else
		{
			_spectatorMarkersToggled = false;
			if (base.Input.IsGameKeyDown(5))
			{
				_dataSource.IsEnabled = true;
			}
			else
			{
				_dataSource.IsEnabled = false;
			}
		}
		_dataSource.Tick(dt);
	}
}
