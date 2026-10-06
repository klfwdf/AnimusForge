using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionSpectatorControlView))]
public class MissionGauntletSpectatorControl : MissionView
{
	private GauntletLayer _gauntletLayer;

	private MissionSpectatorControlVM _dataSource;

	public override void EarlyStart()
	{
		base.EarlyStart();
		ViewOrderPriority = 14;
		_dataSource = new MissionSpectatorControlVM(base.Mission);
		_dataSource.SetPrevCharacterInputKey(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(10));
		_dataSource.SetNextCharacterInputKey(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(9));
		_dataSource.SetTakeControlInputKey(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(16));
		_gauntletLayer = new GauntletLayer("MissionSpectatorControl", ViewOrderPriority);
		_gauntletLayer.LoadMovie("SpectatorControl", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.MissionScreen.OnSpectateAgentFocusIn += _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut += _dataSource.OnSpectatedAgentFocusOut;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_dataSource == null)
		{
			return;
		}
		TaleWorlds.MountAndBlade.Mission.SpectatorData spectatingData = base.MissionScreen.GetSpectatingData(base.MissionScreen.CombatCamera.Frame.origin);
		bool flag = spectatingData.CameraType == SpectatorCameraTypes.LockToMainPlayer || spectatingData.CameraType == SpectatorCameraTypes.LockToPosition;
		MissionSpectatorControlVM dataSource = _dataSource;
		int isEnabled;
		if ((!flag && base.Mission.Mode != MissionMode.Deployment) || (base.MissionScreen.IsCheatGhostMode && !base.Mission.IsOrderMenuOpen))
		{
			MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
			if ((missionBehavior == null || missionBehavior.IsRoundInProgress) && !base.MissionScreen.LockCameraMovement)
			{
				isEnabled = ((base.MissionScreen.CustomCamera == null) ? 1 : 0);
				goto IL_00b6;
			}
		}
		isEnabled = 0;
		goto IL_00b6;
		IL_00b6:
		dataSource.IsEnabled = (byte)isEnabled != 0;
		bool flag2 = base.Mission.PlayerTeam != null && base.Mission.MainAgent == null;
		_dataSource.SetMainAgentStatus(flag2);
		_dataSource.IsTakeControlRelevant = flag2 && base.Mission.CanPlayerTakeControlOfAnotherAgentWhenDead;
		_dataSource.IsTakeControlEnabled = base.MissionScreen.LastFollowedAgent != null && base.Mission.CanTakeControlOfAgent(base.MissionScreen.LastFollowedAgent);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.OnSpectateAgentFocusIn -= _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut -= _dataSource.OnSpectatedAgentFocusOut;
		_dataSource.OnFinalize();
	}
}
