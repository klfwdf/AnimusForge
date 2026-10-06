using NetworkMessages.FromServer;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerServerStatusUIHandler))]
public class MissionGauntletServerStatus : MissionView
{
	private MultiplayerMissionServerStatusVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private bool IsOptionEnabled => BannerlordConfig.EnableNetworkAlertIcons;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MultiplayerMissionServerStatusVM();
		_gauntletLayer = new GauntletLayer("MultiplayerServerStatus", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerServerStatus", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		NetworkCommunicator.OnPeerAveragePingUpdated += OnPeerPingUpdated;
	}

	private void OnPeerPingUpdated(NetworkCommunicator obj)
	{
		if (IsOptionEnabled && obj.IsMine)
		{
			_dataSource.UpdatePeerPing(obj.AveragePingInMilliseconds);
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (IsOptionEnabled && GameNetwork.IsClient && GameNetwork.IsMyPeerReady)
		{
			_dataSource.UpdatePacketLossRatio((GameNetwork.MyPeer != null) ? ((float)GameNetwork.MyPeer.AverageLossPercent) : 0f);
			_dataSource.UpdateServerPerformanceState(GameNetwork.MyPeer?.ServerPerformanceProblemState ?? ServerPerformanceState.High);
		}
		else
		{
			_dataSource.ResetStates();
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		NetworkCommunicator.OnPeerAveragePingUpdated -= OnPeerPingUpdated;
	}
}
