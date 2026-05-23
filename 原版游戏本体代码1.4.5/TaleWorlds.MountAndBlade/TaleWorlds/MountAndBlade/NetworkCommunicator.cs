using System;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public sealed class NetworkCommunicator : ICommunicator
{
	private double _averagePingInMilliseconds;

	private double _averageLossPercent;

	private Agent _controlledAgent;

	private bool _isServerPeer;

	private bool _isSynchronized;

	private ServerPerformanceState _serverPerformanceProblemState;

	public VirtualPlayer VirtualPlayer { get; }

	public PlayerConnectionInfo PlayerConnectionInfo { get; private set; }

	public bool QuitFromMission { get; set; }

	public int SessionKey { get; internal set; }

	public bool JustReconnecting { get; private set; }

	public double AveragePingInMilliseconds
	{
		get
		{
			return _averagePingInMilliseconds;
		}
		private set
		{
			if (value != _averagePingInMilliseconds)
			{
				_averagePingInMilliseconds = value;
				NetworkCommunicator.OnPeerAveragePingUpdated?.Invoke(this);
			}
		}
	}

	public double AverageLossPercent
	{
		get
		{
			return _averageLossPercent;
		}
		private set
		{
			if (value != _averageLossPercent)
			{
				_averageLossPercent = value;
			}
		}
	}

	public bool IsMine => GameNetwork.MyPeer == this;

	public bool IsAdmin { get; private set; }

	public int Index => VirtualPlayer.Index;

	public string UserName => VirtualPlayer.UserName;

	public Agent ControlledAgent
	{
		get
		{
			return _controlledAgent;
		}
		set
		{
			_controlledAgent = value;
			if (GameNetwork.IsServer)
			{
				UIntPtr missionPointer = (value?.Mission)?.Pointer ?? UIntPtr.Zero;
				int agentIndex = value?.Index ?? (-1);
				MBAPI.IMBPeer.SetControlledAgent(Index, missionPointer, agentIndex);
			}
		}
	}

	public bool IsMuted { get; set; }

	public int ForcedAvatarIndex { get; set; } = -1;

	public bool IsNetworkActive => true;

	public bool IsConnectionActive
	{
		get
		{
			if (GameNetwork.VirtualPlayers[Index] == VirtualPlayer)
			{
				return MBAPI.IMBPeer.IsActive(Index);
			}
			return false;
		}
	}

	public bool IsSynchronized
	{
		get
		{
			if (GameNetwork.IsServer)
			{
				if (GameNetwork.VirtualPlayers[Index] == VirtualPlayer)
				{
					return MBAPI.IMBPeer.GetIsSynchronized(Index);
				}
				return false;
			}
			return _isSynchronized;
		}
		set
		{
			if (value == _isSynchronized && !JustReconnecting)
			{
				return;
			}
			if (GameNetwork.IsServer)
			{
				MBAPI.IMBPeer.SetIsSynchronized(Index, value);
			}
			_isSynchronized = value;
			if (_isSynchronized)
			{
				JustReconnecting = false;
				NetworkCommunicator.OnPeerSynchronized?.Invoke(this);
			}
			if (GameNetwork.IsServer && !IsServerPeer)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SynchronizingDone(this, value));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeTargetPlayer, this);
				GameNetwork.BeginModuleEventAsServer(this);
				GameNetwork.WriteMessage(new SynchronizingDone(this, value));
				GameNetwork.EndModuleEventAsServer();
				if (value)
				{
					MBDebug.Print("Server: " + UserName + " is now synchronized.", 0, Debug.DebugColor.White, 17179869184uL);
				}
				else
				{
					MBDebug.Print("Server: " + UserName + " is not synchronized.", 0, Debug.DebugColor.White, 17179869184uL);
				}
			}
		}
	}

	public bool IsServerPeer => _isServerPeer;

	public ServerPerformanceState ServerPerformanceProblemState
	{
		get
		{
			return _serverPerformanceProblemState;
		}
		private set
		{
			if (value != _serverPerformanceProblemState)
			{
				_serverPerformanceProblemState = value;
			}
		}
	}

	public static event Action<PeerComponent> OnPeerComponentAdded;

	public static event Action<NetworkCommunicator> OnPeerSynchronized;

	public static event Action<NetworkCommunicator> OnPeerAveragePingUpdated;

	private NetworkCommunicator(int index, string name, PlayerId playerID)
	{
		VirtualPlayer = new VirtualPlayer(index, name, playerID, this);
	}

	internal static NetworkCommunicator CreateAsServer(PlayerConnectionInfo playerConnectionInfo, int index, bool isAdmin)
	{
		NetworkCommunicator obj = new NetworkCommunicator(index, playerConnectionInfo.Name, playerConnectionInfo.PlayerID)
		{
			PlayerConnectionInfo = playerConnectionInfo,
			IsAdmin = isAdmin
		};
		MBNetworkPeer data = new MBNetworkPeer(obj);
		MBAPI.IMBPeer.SetUserData(index, data);
		return obj;
	}

	internal static NetworkCommunicator CreateAsClient(string name, int index)
	{
		return new NetworkCommunicator(index, name, PlayerId.Empty);
	}

	void ICommunicator.OnAddComponent(PeerComponent component)
	{
		if (GameNetwork.IsServer)
		{
			if (!IsServerPeer)
			{
				GameNetwork.BeginModuleEventAsServer(this);
				GameNetwork.WriteMessage(new AddPeerComponent(this, component.TypeId));
				GameNetwork.EndModuleEventAsServer();
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new AddPeerComponent(this, component.TypeId));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeTargetPlayer | GameNetwork.EventBroadcastFlags.AddToMissionRecord, this);
		}
		NetworkCommunicator.OnPeerComponentAdded?.Invoke(component);
	}

	void ICommunicator.OnRemoveComponent(PeerComponent component)
	{
		if (GameNetwork.IsServer)
		{
			if (!IsServerPeer && (IsSynchronized || !JustReconnecting))
			{
				GameNetwork.BeginModuleEventAsServer(this);
				GameNetwork.WriteMessage(new RemovePeerComponent(this, component.TypeId));
				GameNetwork.EndModuleEventAsServer();
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new RemovePeerComponent(this, component.TypeId));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeTargetPlayer | GameNetwork.EventBroadcastFlags.AddToMissionRecord, this);
		}
	}

	void ICommunicator.OnSynchronizeComponentTo(VirtualPlayer peer, PeerComponent component)
	{
		GameNetwork.BeginModuleEventAsServer(peer);
		GameNetwork.WriteMessage(new AddPeerComponent(this, component.TypeId));
		GameNetwork.EndModuleEventAsServer();
	}

	internal void SetServerPeer(bool serverPeer)
	{
		_isServerPeer = serverPeer;
	}

	internal double RefreshAndGetAveragePingInMilliseconds()
	{
		AveragePingInMilliseconds = MBAPI.IMBPeer.GetAveragePingInMilliseconds(Index);
		return AveragePingInMilliseconds;
	}

	internal void SetAveragePingInMillisecondsAsClient(double pingValue)
	{
		AveragePingInMilliseconds = pingValue;
		ControlledAgent?.SetAveragePingInMilliseconds(AveragePingInMilliseconds);
	}

	internal double RefreshAndGetAverageLossPercent()
	{
		AverageLossPercent = MBAPI.IMBPeer.GetAverageLossPercent(Index);
		return AverageLossPercent;
	}

	internal void SetAverageLossPercentAsClient(double lossValue)
	{
		AverageLossPercent = lossValue;
	}

	internal void SetServerPerformanceProblemStateAsClient(ServerPerformanceState serverPerformanceProblemState)
	{
		ServerPerformanceProblemState = serverPerformanceProblemState;
	}

	public void SetRelevantGameOptions(bool sendMeBloodEvents, bool sendMeSoundEvents)
	{
		MBAPI.IMBPeer.SetRelevantGameOptions(Index, sendMeBloodEvents, sendMeSoundEvents);
	}

	public uint GetHost()
	{
		return MBAPI.IMBPeer.GetHost(Index);
	}

	public uint GetReversedHost()
	{
		return MBAPI.IMBPeer.GetReversedHost(Index);
	}

	public ushort GetPort()
	{
		return MBAPI.IMBPeer.GetPort(Index);
	}

	public void UpdateConnectionInfoForReconnect(PlayerConnectionInfo playerConnectionInfo, bool isAdmin)
	{
		PlayerConnectionInfo = playerConnectionInfo;
		IsAdmin = isAdmin;
	}

	public void UpdateIndexForReconnectingPlayer(int newIndex)
	{
		JustReconnecting = true;
		VirtualPlayer.UpdateIndexForReconnectingPlayer(newIndex);
	}

	public void UpdateForJoiningCustomGame(bool isAdmin)
	{
		IsAdmin = isAdmin;
	}
}
