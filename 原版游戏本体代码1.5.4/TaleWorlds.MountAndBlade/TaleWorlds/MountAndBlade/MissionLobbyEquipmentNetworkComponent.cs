using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class MissionLobbyEquipmentNetworkComponent : MissionNetwork
{
	public delegate void OnToggleLoadoutDelegate(bool isActive);

	public delegate void OnRefreshEquipmentEventDelegate(MissionPeer lobbyPeer);

	private MultiplayerMissionAgentVisualSpawnComponent _agentVisualSpawnComponent;

	public event OnToggleLoadoutDelegate OnToggleLoadout;

	public event OnRefreshEquipmentEventDelegate OnEquipmentRefreshed;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		if (!GameNetwork.IsDedicatedServer)
		{
			_agentVisualSpawnComponent = Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
			_agentVisualSpawnComponent.OnMyAgentVisualSpawned += OpenLoadout;
			_agentVisualSpawnComponent.OnMyAgentSpawnedFromVisual += CloseLoadout;
			_agentVisualSpawnComponent.OnMyAgentVisualRemoved += CloseLoadout;
		}
	}

	protected override void OnEndMission()
	{
		if (!GameNetwork.IsDedicatedServer)
		{
			_agentVisualSpawnComponent.OnMyAgentVisualSpawned -= OpenLoadout;
			_agentVisualSpawnComponent.OnMyAgentSpawnedFromVisual -= CloseLoadout;
			_agentVisualSpawnComponent.OnMyAgentVisualRemoved -= CloseLoadout;
			_agentVisualSpawnComponent = null;
		}
		base.OnEndMission();
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<RequestTroopIndexChange>(HandleClientEventLobbyEquipmentUpdated);
			registerer.RegisterBaseHandler<TeamInitialPerkInfoMessage>(HandleClientEventTeamInitialPerkInfoMessage);
			registerer.RegisterBaseHandler<RequestPerkChange>(HandleClientEventRequestPerkChange);
		}
		else if (GameNetwork.IsClientOrReplay)
		{
			registerer.RegisterBaseHandler<UpdateSelectedTroopIndex>(HandleServerEventEquipmentIndexUpdated);
			registerer.RegisterBaseHandler<SyncPerksForCurrentlySelectedTroop>(SyncPerksForCurrentlySelectedTroop);
		}
	}

	private void HandleServerEventEquipmentIndexUpdated(GameNetworkMessage baseMessage)
	{
		UpdateSelectedTroopIndex updateSelectedTroopIndex = (UpdateSelectedTroopIndex)baseMessage;
		updateSelectedTroopIndex.Peer.GetComponent<MissionPeer>().SelectedTroopIndex = updateSelectedTroopIndex.SelectedTroopIndex;
	}

	private void SyncPerksForCurrentlySelectedTroop(GameNetworkMessage baseMessage)
	{
		SyncPerksForCurrentlySelectedTroop syncPerksForCurrentlySelectedTroop = (SyncPerksForCurrentlySelectedTroop)baseMessage;
		MissionPeer component = syncPerksForCurrentlySelectedTroop.Peer.GetComponent<MissionPeer>();
		for (int i = 0; i < 3; i++)
		{
			component.SelectPerk(i, syncPerksForCurrentlySelectedTroop.PerkIndices[i], component.SelectedTroopIndex);
		}
	}

	private bool HandleClientEventLobbyEquipmentUpdated(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		RequestTroopIndexChange requestTroopIndexChange = (RequestTroopIndexChange)baseMessage;
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return false;
		}
		SpawnComponent missionBehavior = base.Mission.GetMissionBehavior<SpawnComponent>();
		if (missionBehavior == null)
		{
			return false;
		}
		if (missionBehavior.AreAgentsSpawning() && component.SelectedTroopIndex != requestTroopIndexChange.SelectedTroopIndex)
		{
			if (component.Culture == null || requestTroopIndexChange.SelectedTroopIndex < 0 || MultiplayerClassDivisions.GetMPHeroClasses(component.Culture).Count() <= requestTroopIndexChange.SelectedTroopIndex)
			{
				component.SelectedTroopIndex = 0;
			}
			else
			{
				component.SelectedTroopIndex = requestTroopIndexChange.SelectedTroopIndex;
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(peer, component.SelectedTroopIndex));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, peer);
			if (this.OnEquipmentRefreshed != null)
			{
				this.OnEquipmentRefreshed(component);
			}
		}
		return true;
	}

	private bool HandleClientEventTeamInitialPerkInfoMessage(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		TeamInitialPerkInfoMessage teamInitialPerkInfoMessage = (TeamInitialPerkInfoMessage)baseMessage;
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return false;
		}
		if (base.Mission.GetMissionBehavior<SpawnComponent>() == null)
		{
			return false;
		}
		component.OnTeamInitialPerkInfoReceived(teamInitialPerkInfoMessage.Perks);
		return true;
	}

	private bool HandleClientEventRequestPerkChange(NetworkCommunicator peer, GameNetworkMessage baseMessage)
	{
		RequestPerkChange requestPerkChange = (RequestPerkChange)baseMessage;
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return false;
		}
		SpawnComponent missionBehavior = base.Mission.GetMissionBehavior<SpawnComponent>();
		if (missionBehavior == null)
		{
			return false;
		}
		if (component.SelectPerk(requestPerkChange.PerkListIndex, requestPerkChange.PerkIndex) && missionBehavior.AreAgentsSpawning() && this.OnEquipmentRefreshed != null)
		{
			this.OnEquipmentRefreshed(component);
		}
		return true;
	}

	public void PerkUpdated(int perkList, int perkIndex)
	{
		if (GameNetwork.IsServer)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (this.OnEquipmentRefreshed != null)
			{
				this.OnEquipmentRefreshed(component);
			}
		}
		else
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestPerkChange(perkList, perkIndex));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void EquipmentUpdated()
	{
		if (GameNetwork.IsServer)
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component.SelectedTroopIndex != component.NextSelectedTroopIndex)
			{
				component.SelectedTroopIndex = component.NextSelectedTroopIndex;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(GameNetwork.MyPeer, component.SelectedTroopIndex));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, GameNetwork.MyPeer);
				if (this.OnEquipmentRefreshed != null)
				{
					this.OnEquipmentRefreshed(component);
				}
			}
		}
		else
		{
			MissionPeer component2 = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestTroopIndexChange(component2.NextSelectedTroopIndex));
			GameNetwork.EndModuleEventAsClient();
		}
	}

	public void ToggleLoadout(bool isActive)
	{
		if (this.OnToggleLoadout != null)
		{
			this.OnToggleLoadout(isActive);
		}
	}

	private void OpenLoadout()
	{
		ToggleLoadout(isActive: true);
	}

	private void CloseLoadout()
	{
		ToggleLoadout(isActive: false);
	}
}
