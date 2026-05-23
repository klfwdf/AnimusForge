using System;
using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionMultiplayerGameModeBase : MissionNetwork
{
	public const int GoldCap = 2000;

	public const float PerkTickPeriod = 1f;

	public const float GameModeSystemTickPeriod = 0.25f;

	private float _lastPerkTickTime;

	private MultiplayerMissionAgentVisualSpawnComponent _agentVisualSpawnComponent;

	public MultiplayerTeamSelectComponent MultiplayerTeamSelectComponent;

	protected MissionLobbyComponent MissionLobbyComponent;

	protected MultiplayerGameNotificationsComponent NotificationsComponent;

	public MultiplayerRoundController RoundController;

	public MultiplayerWarmupComponent WarmupComponent;

	public MultiplayerTimerComponent TimerComponent;

	protected MissionMultiplayerGameModeBaseClient GameModeBaseClient;

	private float _gameModeSystemTickTimer;

	public abstract bool IsGameModeHidingAllAgentVisuals { get; }

	public abstract bool IsGameModeUsingOpposingTeams { get; }

	public virtual bool IsGameModeAllowChargeDamageOnFriendly => false;

	public SpawnComponent SpawnComponent { get; private set; }

	protected bool CanGameModeSystemsTickThisFrame { get; private set; }

	public abstract MultiplayerGameType GetMissionType();

	public virtual bool CheckIfOvertime()
	{
		return false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MultiplayerTeamSelectComponent = base.Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
		MissionLobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		GameModeBaseClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		NotificationsComponent = base.Mission.GetMissionBehavior<MultiplayerGameNotificationsComponent>();
		RoundController = base.Mission.GetMissionBehavior<MultiplayerRoundController>();
		WarmupComponent = base.Mission.GetMissionBehavior<MultiplayerWarmupComponent>();
		TimerComponent = base.Mission.GetMissionBehavior<MultiplayerTimerComponent>();
		SpawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
		_agentVisualSpawnComponent = base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
		_lastPerkTickTime = Mission.Current.CurrentTime;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (Mission.Current.CurrentTime - _lastPerkTickTime >= 1f)
		{
			_lastPerkTickTime = Mission.Current.CurrentTime;
			MPPerkObject.TickAllPeerPerks((int)(_lastPerkTickTime / 1f));
		}
	}

	public virtual bool CheckForWarmupEnd()
	{
		return false;
	}

	public virtual bool CheckForRoundEnd()
	{
		return false;
	}

	public virtual bool CheckForMatchEnd()
	{
		return false;
	}

	public virtual bool UseCultureSelection()
	{
		return false;
	}

	public virtual bool UseRoundController()
	{
		return false;
	}

	public virtual Team GetWinnerTeam()
	{
		return null;
	}

	public virtual void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
	{
	}

	public override void OnClearScene()
	{
		base.OnClearScene();
		if (RoundController == null)
		{
			ClearPeerCounts();
		}
		_lastPerkTickTime = Mission.Current.CurrentTime;
	}

	public void ClearPeerCounts()
	{
		List<MissionPeer> list = VirtualPlayer.Peers<MissionPeer>();
		for (int i = 0; i < list.Count; i++)
		{
			MissionPeer missionPeer = list[i];
			missionPeer.AssistCount = 0;
			missionPeer.DeathCount = 0;
			missionPeer.KillCount = 0;
			missionPeer.Score = 0;
			missionPeer.ResetRequestedKickPollCount();
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new KillDeathCountChange(missionPeer.GetNetworkPeer(), null, missionPeer.KillCount, missionPeer.AssistCount, missionPeer.DeathCount, missionPeer.Score));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}

	public bool ShouldSpawnVisualsForServer(NetworkCommunicator spawningNetworkPeer)
	{
		if (GameNetwork.IsDedicatedServer)
		{
			return false;
		}
		MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
		if (missionPeer != null)
		{
			MissionPeer component = spawningNetworkPeer.GetComponent<MissionPeer>();
			if (!IsGameModeHidingAllAgentVisuals && component.Team == missionPeer.Team)
			{
				return true;
			}
			if (spawningNetworkPeer.IsServerPeer)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public void HandleAgentVisualSpawning(NetworkCommunicator spawningNetworkPeer, AgentBuildData spawningAgentBuildData, int troopCountInFormation = 0, bool useCosmetics = true)
	{
		MissionPeer component = spawningNetworkPeer.GetComponent<MissionPeer>();
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(spawningNetworkPeer, component.Perks[component.SelectedTroopIndex]));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, spawningNetworkPeer);
		component.HasSpawnedAgentVisuals = true;
		component.EquipmentUpdatingExpired = false;
		if (useCosmetics)
		{
			AddCosmeticItemsToEquipment(spawningAgentBuildData.AgentOverridenSpawnEquipment, GetUsedCosmeticsFromPeer(component, spawningAgentBuildData.AgentCharacter));
		}
		if (!IsGameModeHidingAllAgentVisuals)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CreateAgentVisuals(spawningNetworkPeer, spawningAgentBuildData, component.SelectedTroopIndex, troopCountInFormation));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, spawningNetworkPeer);
		}
		else if (!spawningNetworkPeer.IsServerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(spawningNetworkPeer);
			GameNetwork.WriteMessage(new CreateAgentVisuals(spawningNetworkPeer, spawningAgentBuildData, component.SelectedTroopIndex, troopCountInFormation));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	public virtual bool AllowCustomPlayerBanners()
	{
		return true;
	}

	public virtual int GetScoreForKill(Agent killedAgent)
	{
		return 20;
	}

	public virtual float GetTroopNumberMultiplierForMissingPlayer(MissionPeer spawningPeer)
	{
		return 1f;
	}

	public int GetCurrentGoldForPeer(MissionPeer peer)
	{
		return peer.Representative.Gold;
	}

	public void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
	{
		if (newAmount >= 0)
		{
			newAmount = MBMath.ClampInt(newAmount, 0, 2000);
		}
		if (peer.Peer.Communicator.IsConnectionActive)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		if (GameModeBaseClient != null)
		{
			GameModeBaseClient.OnGoldAmountChangedForRepresentative(peer.Representative, newAmount);
		}
	}

	protected override void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		if (!GameModeBaseClient.IsGameModeUsingGold)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
		{
			if (networkPeer2 != networkPeer)
			{
				MissionRepresentativeBase component = networkPeer2.GetComponent<MissionRepresentativeBase>();
				if (component != null)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new SyncGoldsForSkirmish(component.Peer, component.Gold));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
	}

	public virtual bool CheckIfPlayerCanDespawn(MissionPeer missionPeer)
	{
		return false;
	}

	public override void OnPreMissionTick(float dt)
	{
		CanGameModeSystemsTickThisFrame = false;
		_gameModeSystemTickTimer += dt;
		if (_gameModeSystemTickTimer >= 0.25f)
		{
			_gameModeSystemTickTimer -= 0.25f;
			CanGameModeSystemsTickThisFrame = true;
		}
	}

	public Dictionary<string, string> GetUsedCosmeticsFromPeer(MissionPeer missionPeer, BasicCharacterObject selectedTroopCharacter)
	{
		if (missionPeer.Peer.UsedCosmetics != null)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			MBReadOnlyList<MultiplayerClassDivisions.MPHeroClass> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<MultiplayerClassDivisions.MPHeroClass>();
			int num = -1;
			for (int i = 0; i < objectTypeList.Count; i++)
			{
				if (objectTypeList[i].HeroCharacter == selectedTroopCharacter || objectTypeList[i].TroopCharacter == selectedTroopCharacter)
				{
					num = i;
					break;
				}
			}
			missionPeer.Peer.UsedCosmetics.TryGetValue(num, out var value);
			if (value != null)
			{
				foreach (int item in value)
				{
					if (!(CosmeticsManager.CosmeticElementsList[item] is ClothingCosmeticElement clothingCosmeticElement))
					{
						continue;
					}
					foreach (string item2 in clothingCosmeticElement.ReplaceItemsId)
					{
						dictionary.Add(item2, CosmeticsManager.CosmeticElementsList[item].Id);
					}
					foreach (Tuple<string, string> item3 in clothingCosmeticElement.ReplaceItemless)
					{
						if (item3.Item1 == objectTypeList[num].StringId)
						{
							dictionary.Add(item3.Item2, CosmeticsManager.CosmeticElementsList[item].Id);
							break;
						}
					}
				}
			}
			return dictionary;
		}
		return null;
	}

	public void AddCosmeticItemsToEquipment(Equipment equipment, Dictionary<string, string> choosenCosmetics)
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
		{
			if (equipment[equipmentIndex].Item == null)
			{
				string key = equipmentIndex.ToString();
				switch (equipmentIndex)
				{
				case EquipmentIndex.NumAllWeaponSlots:
					key = "Head";
					break;
				case EquipmentIndex.Body:
					key = "Body";
					break;
				case EquipmentIndex.Leg:
					key = "Leg";
					break;
				case EquipmentIndex.Gloves:
					key = "Gloves";
					break;
				case EquipmentIndex.Cape:
					key = "Cape";
					break;
				}
				string value = null;
				choosenCosmetics?.TryGetValue(key, out value);
				if (value != null)
				{
					ItemObject cosmeticItem = MBObjectManager.Instance.GetObject<ItemObject>(value);
					EquipmentElement value2 = equipment[equipmentIndex];
					value2.CosmeticItem = cosmeticItem;
					equipment[equipmentIndex] = value2;
				}
			}
			else
			{
				string stringId = equipment[equipmentIndex].Item.StringId;
				string value3 = null;
				choosenCosmetics?.TryGetValue(stringId, out value3);
				if (value3 != null)
				{
					ItemObject cosmeticItem2 = MBObjectManager.Instance.GetObject<ItemObject>(value3);
					EquipmentElement value4 = equipment[equipmentIndex];
					value4.CosmeticItem = cosmeticItem2;
					equipment[equipmentIndex] = value4;
				}
			}
		}
	}

	public bool IsClassAvailable(MultiplayerClassDivisions.MPHeroClass heroClass)
	{
		if (Enum.TryParse<FormationClass>(heroClass.ClassGroup.StringId, out var result))
		{
			return MissionLobbyComponent.IsClassAvailable(result);
		}
		Debug.FailedAssert("\"" + heroClass.ClassGroup.StringId + "\" does not match with any FormationClass.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameModeLogics\\ServerGameModeLogics\\MissionMultiplayerGameModeBase.cs", "IsClassAvailable", 389);
		return false;
	}
}
