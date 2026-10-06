using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerDuelUI))]
public class MissionGauntletDuelUI : MissionView
{
	private MultiplayerDuelVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _mpMissionCategory;

	private MissionMultiplayerGameModeDuelClient _client;

	private MissionLobbyEquipmentNetworkComponent _equipmentController;

	private MissionLobbyComponent _lobbyComponent;

	private bool _isPeerEquipmentsDirty;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		ViewOrderPriority = 15;
		_client = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeDuelClient>();
		_dataSource = new MultiplayerDuelVM(base.MissionScreen.CombatCamera, _client);
		_gauntletLayer = new GauntletLayer("MultiplayerDuel", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerDuel", _dataSource);
		_mpMissionCategory = UIResourceManager.LoadSpriteCategory("ui_mpmission");
		base.MissionScreen.AddLayer(_gauntletLayer);
		_equipmentController = base.Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
		_equipmentController.OnEquipmentRefreshed += OnEquipmentRefreshed;
		MissionPeer.OnEquipmentIndexRefreshed += OnPeerEquipmentIndexRefreshed;
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Combine(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
		_dataSource.IsEnabled = true;
		_isPeerEquipmentsDirty = true;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_mpMissionCategory?.Unload();
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		_equipmentController.OnEquipmentRefreshed -= OnEquipmentRefreshed;
		MissionPeer.OnEquipmentIndexRefreshed -= OnPeerEquipmentIndexRefreshed;
		_lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
		NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Remove(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.Tick(dt);
		if (_client.MyRepresentative?.ControlledAgent != null && base.Input.IsGameKeyReleased(13))
		{
			_client.MyRepresentative.OnInteraction();
		}
		if (_isPeerEquipmentsDirty)
		{
			_dataSource.Markers.RefreshPeerEquipments();
			_isPeerEquipmentsDirty = false;
		}
	}

	private void OnNativeOptionChanged(NativeOptions.NativeOptionsType optionType)
	{
		if (optionType == NativeOptions.NativeOptionsType.ScreenResolution)
		{
			_dataSource.OnScreenResolutionChanged();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent == Agent.Main)
		{
			_dataSource.OnMainAgentRemoved();
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
		if (agent == Agent.Main)
		{
			_dataSource.OnMainAgentBuild();
		}
	}

	public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
		base.OnFocusGained(agent, focusableObject, isInteractable);
		if (!(focusableObject is DuelZoneLandmark) && !(focusableObject is Agent))
		{
			_dataSource.Markers.OnFocusGained();
		}
	}

	public override void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		base.OnFocusLost(agent, focusableObject);
		if (!(focusableObject is DuelZoneLandmark) && !(focusableObject is Agent))
		{
			_dataSource.Markers.OnFocusLost();
		}
	}

	public void OnPeerEquipmentIndexRefreshed(MissionPeer peer, int equipmentSetIndex)
	{
		_dataSource.Markers.OnPeerEquipmentRefreshed(peer);
	}

	private void OnEquipmentRefreshed(MissionPeer peer)
	{
		_dataSource.Markers.OnPeerEquipmentRefreshed(peer);
	}

	private void OnPostMatchEnded()
	{
		_dataSource.IsEnabled = false;
	}
}
