using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public static class GameNetwork
{
	public class NetworkMessageHandlerRegisterer
	{
		public enum RegisterMode
		{
			Add,
			Remove
		}

		private readonly RegisterMode _registerMode;

		public NetworkMessageHandlerRegisterer(RegisterMode definitionMode)
		{
			_registerMode = definitionMode;
		}

		public void Register<T>(GameNetworkMessage.ServerMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
		{
			if (_registerMode == RegisterMode.Add)
			{
				AddServerMessageHandler(handler);
			}
			else
			{
				RemoveServerMessageHandler(handler);
			}
		}

		public void RegisterBaseHandler<T>(GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage> handler) where T : GameNetworkMessage
		{
			if (_registerMode == RegisterMode.Add)
			{
				AddServerBaseMessageHandler(handler, typeof(T));
			}
			else
			{
				RemoveServerBaseMessageHandler(handler, typeof(T));
			}
		}

		public void Register<T>(GameNetworkMessage.ClientMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
		{
			if (_registerMode == RegisterMode.Add)
			{
				AddClientMessageHandler(handler);
			}
			else
			{
				RemoveClientMessageHandler(handler);
			}
		}

		public void RegisterBaseHandler<T>(GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage> handler) where T : GameNetworkMessage
		{
			if (_registerMode == RegisterMode.Add)
			{
				AddClientBaseMessageHandler(handler, typeof(T));
			}
			else
			{
				RemoveClientBaseMessageHandler(handler, typeof(T));
			}
		}
	}

	public class NetworkMessageHandlerRegistererContainer
	{
		private List<Delegate> _fromClientHandlers;

		private List<Delegate> _fromServerHandlers;

		private List<Tuple<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>, Type>> _fromServerBaseHandlers;

		private List<Tuple<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>, Type>> _fromClientBaseHandlers;

		public NetworkMessageHandlerRegistererContainer()
		{
			_fromClientHandlers = new List<Delegate>();
			_fromServerHandlers = new List<Delegate>();
			_fromServerBaseHandlers = new List<Tuple<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>, Type>>();
			_fromClientBaseHandlers = new List<Tuple<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>, Type>>();
		}

		public void RegisterBaseHandler<T>(GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage> handler) where T : GameNetworkMessage
		{
			_fromServerBaseHandlers.Add(new Tuple<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>, Type>(handler, typeof(T)));
		}

		public void Register<T>(GameNetworkMessage.ServerMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
		{
			_fromServerHandlers.Add(handler);
		}

		public void RegisterBaseHandler<T>(GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage> handler)
		{
			_fromClientBaseHandlers.Add(new Tuple<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>, Type>(handler, typeof(T)));
		}

		public void Register<T>(GameNetworkMessage.ClientMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
		{
			_fromClientHandlers.Add(handler);
		}

		public void RegisterMessages()
		{
			if (_fromServerHandlers.Count > 0 || _fromServerBaseHandlers.Count > 0)
			{
				foreach (Delegate fromServerHandler in _fromServerHandlers)
				{
					Type key = fromServerHandler.GetType().GenericTypeArguments[0];
					int key2 = _gameNetworkMessageTypesFromServer[key];
					_fromServerMessageHandlers[key2].Add(fromServerHandler);
				}
				{
					foreach (Tuple<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>, Type> fromServerBaseHandler in _fromServerBaseHandlers)
					{
						int key3 = _gameNetworkMessageTypesFromServer[fromServerBaseHandler.Item2];
						_fromServerBaseMessageHandlers[key3].Add(fromServerBaseHandler.Item1);
					}
					return;
				}
			}
			foreach (Delegate fromClientHandler in _fromClientHandlers)
			{
				Type key4 = fromClientHandler.GetType().GenericTypeArguments[0];
				int key5 = _gameNetworkMessageTypesFromClient[key4];
				_fromClientMessageHandlers[key5].Add(fromClientHandler);
			}
			foreach (Tuple<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>, Type> fromClientBaseHandler in _fromClientBaseHandlers)
			{
				int key6 = _gameNetworkMessageTypesFromClient[fromClientBaseHandler.Item2];
				_fromClientBaseMessageHandlers[key6].Add(fromClientBaseHandler.Item1);
			}
		}

		public void UnregisterMessages()
		{
			if (_fromServerHandlers.Count > 0 || _fromServerBaseHandlers.Count > 0)
			{
				foreach (Delegate fromServerHandler in _fromServerHandlers)
				{
					Type key = fromServerHandler.GetType().GenericTypeArguments[0];
					int key2 = _gameNetworkMessageTypesFromServer[key];
					_fromServerMessageHandlers[key2].Remove(fromServerHandler);
				}
				{
					foreach (Tuple<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>, Type> fromServerBaseHandler in _fromServerBaseHandlers)
					{
						int key3 = _gameNetworkMessageTypesFromServer[fromServerBaseHandler.Item2];
						_fromServerBaseMessageHandlers[key3].Remove(fromServerBaseHandler.Item1);
					}
					return;
				}
			}
			foreach (Delegate fromClientHandler in _fromClientHandlers)
			{
				Type key4 = fromClientHandler.GetType().GenericTypeArguments[0];
				int key5 = _gameNetworkMessageTypesFromClient[key4];
				_fromClientMessageHandlers[key5].Remove(fromClientHandler);
			}
			foreach (Tuple<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>, Type> fromClientBaseHandler in _fromClientBaseHandlers)
			{
				int key6 = _gameNetworkMessageTypesFromClient[fromClientBaseHandler.Item2];
				_fromClientBaseMessageHandlers[key6].Remove(fromClientBaseHandler.Item1);
			}
		}
	}

	[Flags]
	public enum EventBroadcastFlags
	{
		None = 0,
		ExcludeTargetPlayer = 1,
		ExcludeNoBloodStainsOption = 2,
		ExcludeNoParticlesOption = 4,
		ExcludeNoSoundOption = 8,
		AddToMissionRecord = 0x10,
		IncludeUnsynchronizedClients = 0x20,
		ExcludeOtherTeamPlayers = 0x40,
		ExcludePeerTeamPlayers = 0x80,
		DontSendToPeers = 0x100
	}

	[EngineStruct("Debug_network_position_compression_statistics_struct", false, null)]
	public struct DebugNetworkPositionCompressionStatisticsStruct
	{
		public int totalPositionUpload;

		public int totalPositionPrecisionBitCount;

		public int totalPositionCoarseBitCountX;

		public int totalPositionCoarseBitCountY;

		public int totalPositionCoarseBitCountZ;
	}

	[EngineStruct("Debug_network_packet_statistics_struct", false, null)]
	public struct DebugNetworkPacketStatisticsStruct
	{
		public int TotalPackets;

		public int TotalUpload;

		public int TotalConstantsUpload;

		public int TotalReliableEventUpload;

		public int TotalReplicationUpload;

		public int TotalUnreliableEventUpload;

		public int TotalReplicationTableAdderCount;

		public int TotalReplicationTableAdderBitCount;

		public int TotalReplicationTableAdder;

		public double TotalCellPriority;

		public double TotalCellAgentPriority;

		public double TotalCellCellPriority;

		public int TotalCellPriorityChecks;

		public int TotalSentCellCount;

		public int TotalNotSentCellCount;

		public int TotalReplicationWriteCount;

		public int CurMaxPacketSizeInBytes;

		public double AveragePingTime;

		public double AverageDtToSendPacket;

		public double TimeOutPeriod;

		public double PacingRate;

		public double DeliveryRate;

		public double RoundTripTime;

		public int InflightBitCount;

		public int IsCongested;

		public int ProbeBwPhaseIndex;

		public double LostPercent;

		public int LostCount;

		public int TotalCountOnLostCheck;
	}

	public struct AddPlayersResult
	{
		public bool Success;

		public NetworkCommunicator[] NetworkPeers;
	}

	public const int MaxAutomatedBattleIndex = 10;

	public const int MaxPlayerCount = 1023;

	private static IGameNetworkHandler _handler;

	public static int ClientPeerIndex;

	private static MultiplayerMessageFilter MultiplayerLogging = MultiplayerMessageFilter.All;

	private static Dictionary<Type, int> _gameNetworkMessageTypesAll;

	private static Dictionary<Type, int> _gameNetworkMessageTypesFromClient;

	private static List<Type> _gameNetworkMessageIdsFromClient;

	private static Dictionary<Type, int> _gameNetworkMessageTypesFromServer;

	private static List<Type> _gameNetworkMessageIdsFromServer;

	private static Dictionary<int, List<object>> _fromClientMessageHandlers;

	private static Dictionary<int, List<object>> _fromServerMessageHandlers;

	private static Dictionary<int, List<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>>> _fromClientBaseMessageHandlers;

	private static Dictionary<int, List<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>>> _fromServerBaseMessageHandlers;

	private static List<Type> _synchedMissionObjectClassTypes;

	public static bool IsServer
	{
		get
		{
			if (MBCommon.CurrentGameType != MBCommon.GameType.MultiServer)
			{
				return MBCommon.CurrentGameType == MBCommon.GameType.MultiClientServer;
			}
			return true;
		}
	}

	public static bool IsServerOrRecorder
	{
		get
		{
			if (!IsServer)
			{
				return MBCommon.CurrentGameType == MBCommon.GameType.SingleRecord;
			}
			return true;
		}
	}

	public static bool IsClient => MBCommon.CurrentGameType == MBCommon.GameType.MultiClient;

	public static bool IsReplay => MBCommon.CurrentGameType == MBCommon.GameType.SingleReplay;

	public static bool IsClientOrReplay
	{
		get
		{
			if (!IsClient)
			{
				return IsReplay;
			}
			return true;
		}
	}

	public static bool IsDedicatedServer => false;

	public static bool MultiplayerDisabled => false;

	public static bool IsMultiplayer
	{
		get
		{
			if (!IsServer)
			{
				return IsClient;
			}
			return true;
		}
	}

	public static bool IsMultiplayerOrReplay
	{
		get
		{
			if (!IsMultiplayer)
			{
				return IsReplay;
			}
			return true;
		}
	}

	public static bool IsSessionActive
	{
		get
		{
			if (!IsServerOrRecorder)
			{
				return IsClientOrReplay;
			}
			return true;
		}
	}

	public static IEnumerable<NetworkCommunicator> NetworkPeersIncludingDisconnectedPeers
	{
		get
		{
			foreach (NetworkCommunicator networkPeer in NetworkPeers)
			{
				yield return networkPeer;
			}
			for (int i = 0; i < DisconnectedNetworkPeers.Count; i++)
			{
				yield return DisconnectedNetworkPeers[i];
			}
		}
	}

	public static VirtualPlayer[] VirtualPlayers { get; private set; }

	public static List<NetworkCommunicator> NetworkPeers { get; private set; }

	public static List<NetworkCommunicator> DisconnectedNetworkPeers { get; private set; }

	public static int NetworkPeerCount => NetworkPeers.Count;

	public static bool NetworkPeersValid => NetworkPeers != null;

	public static List<UdpNetworkComponent> NetworkComponents { get; private set; }

	public static List<IUdpNetworkHandler> NetworkHandlers { get; private set; }

	public static NetworkCommunicator MyPeer { get; private set; }

	public static bool IsMyPeerReady
	{
		get
		{
			if (MyPeer != null)
			{
				return MyPeer.IsSynchronized;
			}
			return false;
		}
	}

	private static void AddNetworkPeer(NetworkCommunicator networkPeer)
	{
		NetworkPeers.Add(networkPeer);
		Debug.Print("AddNetworkPeer: " + networkPeer.UserName, 0, Debug.DebugColor.White, 17179869184uL);
	}

	private static void RemoveNetworkPeer(NetworkCommunicator networkPeer)
	{
		Debug.Print("RemoveNetworkPeer: " + networkPeer.UserName, 0, Debug.DebugColor.White, 17179869184uL);
		NetworkPeers.Remove(networkPeer);
	}

	private static void AddToDisconnectedPeers(NetworkCommunicator networkPeer)
	{
		Debug.Print("AddToDisconnectedPeers: " + networkPeer.UserName, 0, Debug.DebugColor.White, 17179869184uL);
		DisconnectedNetworkPeers.Add(networkPeer);
	}

	public static void ClearAllPeers()
	{
		if (VirtualPlayers != null)
		{
			for (int i = 0; i < VirtualPlayers.Length; i++)
			{
				VirtualPlayers[i] = null;
			}
			NetworkPeers.Clear();
			DisconnectedNetworkPeers.Clear();
		}
	}

	public static NetworkCommunicator FindNetworkPeer(int index)
	{
		foreach (NetworkCommunicator networkPeer in NetworkPeers)
		{
			if (networkPeer.Index == index)
			{
				return networkPeer;
			}
		}
		return null;
	}

	public static void Initialize(IGameNetworkHandler handler)
	{
		_handler = handler;
		VirtualPlayers = new VirtualPlayer[1023];
		NetworkPeers = new List<NetworkCommunicator>();
		DisconnectedNetworkPeers = new List<NetworkCommunicator>();
		MBNetwork.Initialize(new NetworkCommunication());
		NetworkComponents = new List<UdpNetworkComponent>();
		NetworkHandlers = new List<IUdpNetworkHandler>();
		_handler.OnInitialize();
	}

	internal static void Tick(float dt)
	{
		int i = 0;
		try
		{
			for (i = 0; i < NetworkHandlers.Count; i++)
			{
				NetworkHandlers[i].OnUdpNetworkHandlerTick(dt);
			}
		}
		catch (Exception ex)
		{
			if (NetworkHandlers.Count > 0 && i < NetworkHandlers.Count && NetworkHandlers[i] != null)
			{
				string text = NetworkHandlers[i].ToString();
				Debug.Print("Exception On Network Component: " + text);
			}
			Debug.Print(ex.StackTrace);
			Debug.Print(ex.Message);
		}
	}

	private static void StartMultiplayer()
	{
		VirtualPlayer.Reset();
		_handler.OnStartMultiplayer();
	}

	public static void EndMultiplayer()
	{
		_handler.OnEndMultiplayer();
		for (int num = NetworkComponents.Count - 1; num >= 0; num--)
		{
			DestroyComponent(NetworkComponents[num]);
		}
		for (int num2 = NetworkHandlers.Count - 1; num2 >= 0; num2--)
		{
			RemoveNetworkHandler(NetworkHandlers[num2]);
		}
		if (IsServer)
		{
			TerminateServerSide();
		}
		if (IsClientOrReplay)
		{
			AddRemoveMessageHandlers(NetworkMessageHandlerRegisterer.RegisterMode.Remove);
		}
		if (IsClient)
		{
			TerminateClientSide();
		}
		Debug.Print("Clearing peers list with count " + NetworkPeerCount);
		ClearAllPeers();
		VirtualPlayer.Reset();
		MyPeer = null;
		Debug.Print("NetworkManager::HandleMultiplayerEnd");
	}

	[MBCallback(null, false)]
	internal static void HandleRemovePlayer(MBNetworkPeer peer, bool isTimedOut)
	{
		DisconnectInfo disconnectInfo = peer.NetworkPeer.PlayerConnectionInfo.GetParameter<DisconnectInfo>("DisconnectInfo") ?? new DisconnectInfo
		{
			Type = DisconnectType.QuitFromGame
		};
		disconnectInfo.Type = (isTimedOut ? DisconnectType.TimedOut : disconnectInfo.Type);
		peer.NetworkPeer.PlayerConnectionInfo.AddParameter("DisconnectInfo", disconnectInfo);
		HandleRemovePlayerInternal(peer.NetworkPeer, peer.NetworkPeer.IsSynchronized && MultiplayerIntermissionVotingManager.Instance.CurrentVoteState == MultiplayerIntermissionState.Idle);
	}

	internal static void HandleRemovePlayerInternal(NetworkCommunicator networkPeer, bool isDisconnected)
	{
		if (IsClient && networkPeer.IsMine)
		{
			HandleDisconnect();
			return;
		}
		_handler.OnPlayerDisconnectedFromServer(networkPeer);
		if (IsServer)
		{
			foreach (IUdpNetworkHandler networkHandler in NetworkHandlers)
			{
				networkHandler.HandleEarlyPlayerDisconnect(networkPeer);
			}
			foreach (IUdpNetworkHandler networkHandler2 in NetworkHandlers)
			{
				networkHandler2.HandlePlayerDisconnect(networkPeer);
			}
		}
		foreach (IUdpNetworkHandler networkHandler3 in NetworkHandlers)
		{
			networkHandler3.OnPlayerDisconnectedFromServer(networkPeer);
		}
		RemoveNetworkPeer(networkPeer);
		if (isDisconnected)
		{
			AddToDisconnectedPeers(networkPeer);
		}
		VirtualPlayers[networkPeer.VirtualPlayer.Index] = null;
		if (!IsServer)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer2 in NetworkPeers)
		{
			if (!networkPeer2.IsServerPeer)
			{
				BeginModuleEventAsServer(networkPeer2);
				WriteMessage(new DeletePlayer(networkPeer.Index, isDisconnected));
				EndModuleEventAsServer();
			}
		}
	}

	[MBCallback(null, false)]
	internal static void HandleDisconnect()
	{
		_handler.OnDisconnectedFromServer();
		foreach (IUdpNetworkHandler networkHandler in NetworkHandlers)
		{
			networkHandler.OnDisconnectedFromServer();
		}
		MyPeer = null;
	}

	public static void StartReplay()
	{
		_handler.OnStartReplay();
	}

	public static void EndReplay()
	{
		_handler.OnEndReplay();
	}

	public static void PreStartMultiplayerOnServer()
	{
		MBCommon.CurrentGameType = (IsDedicatedServer ? MBCommon.GameType.MultiServer : MBCommon.GameType.MultiClientServer);
		ClientPeerIndex = -1;
	}

	public static void StartMultiplayerOnServer(int port)
	{
		Debug.Print("StartMultiplayerOnServer");
		PreStartMultiplayerOnServer();
		InitializeServerSide(port);
		StartMultiplayer();
	}

	[MBCallback(null, false)]
	internal static bool HandleNetworkPacketAsServer(MBNetworkPeer networkPeer)
	{
		return HandleNetworkPacketAsServer(networkPeer.NetworkPeer);
	}

	internal static bool HandleNetworkPacketAsServer(NetworkCommunicator networkPeer)
	{
		if (networkPeer == null)
		{
			Debug.Print("networkPeer == null");
			return false;
		}
		bool bufferReadValid = true;
		try
		{
			int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.NetworkComponentEventTypeFromClientCompressionInfo, ref bufferReadValid);
			if (bufferReadValid)
			{
				if (num >= 0 && num < _gameNetworkMessageIdsFromClient.Count)
				{
					GameNetworkMessage gameNetworkMessage = Activator.CreateInstance(_gameNetworkMessageIdsFromClient[num]) as GameNetworkMessage;
					gameNetworkMessage.MessageId = num;
					bufferReadValid = gameNetworkMessage.Read();
					if (bufferReadValid)
					{
						bool flag = false;
						bool flag2 = true;
						if (_fromClientBaseMessageHandlers.TryGetValue(num, out var value))
						{
							foreach (GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage> item in value)
							{
								bufferReadValid = bufferReadValid && item(networkPeer, gameNetworkMessage);
								if (!bufferReadValid)
								{
									break;
								}
							}
							flag2 = false;
							flag = value.Count != 0;
						}
						if (_fromClientMessageHandlers.TryGetValue(num, out var value2))
						{
							foreach (object item2 in value2)
							{
								Delegate method = item2 as Delegate;
								bufferReadValid = bufferReadValid && (bool)method.DynamicInvokeWithLog(networkPeer, gameNetworkMessage);
								if (!bufferReadValid)
								{
									break;
								}
							}
							flag2 = false;
							flag = flag || value2.Count != 0;
						}
						if (flag2)
						{
							Debug.FailedAssert("Unknown network messageId " + gameNetworkMessage, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\GameNetwork.cs", "HandleNetworkPacketAsServer", 760);
							bufferReadValid = false;
						}
						else if (!flag)
						{
							Debug.Print("Handler not found for network message " + gameNetworkMessage, 0, Debug.DebugColor.White, 17179869184uL);
						}
					}
				}
				else
				{
					Debug.Print("Handler not found for network message " + num, 0, Debug.DebugColor.White, 17179869184uL);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Print("error " + ex.Message);
			return false;
		}
		return bufferReadValid;
	}

	[MBCallback(null, false)]
	public static void HandleConsoleCommand(string command)
	{
		if (_handler != null)
		{
			_handler.OnHandleConsoleCommand(command);
		}
	}

	private static void InitializeServerSide(int port)
	{
		MBAPI.IMBNetwork.InitializeServerSide(port);
	}

	private static void TerminateServerSide()
	{
		MBAPI.IMBNetwork.TerminateServerSide();
		if (!IsDedicatedServer)
		{
			MBCommon.CurrentGameType = MBCommon.GameType.Single;
		}
	}

	private static void PrepareNewUdpSession(int peerIndex, int sessionKey)
	{
		MBAPI.IMBNetwork.PrepareNewUdpSession(peerIndex, sessionKey);
	}

	public static string GetActiveUdpSessionsIpAddress()
	{
		return MBAPI.IMBNetwork.GetActiveUdpSessionsIpAddress();
	}

	public static ICommunicator AddNewPlayerOnServer(PlayerConnectionInfo playerConnectionInfo, bool serverPeer, bool isAdmin)
	{
		bool flag = playerConnectionInfo == null;
		int num = (flag ? MBAPI.IMBNetwork.AddNewBotOnServer() : MBAPI.IMBNetwork.AddNewPlayerOnServer(serverPeer));
		Debug.Print("AddNewPlayerOnServer: " + playerConnectionInfo.Name + " index: " + num, 0, Debug.DebugColor.White, 17179869184uL);
		if (num >= 0)
		{
			int sessionKey = 0;
			if (!serverPeer)
			{
				sessionKey = GetSessionKeyForPlayer();
			}
			int num2 = -1;
			ICommunicator communicator = null;
			if (flag)
			{
				communicator = DummyCommunicator.CreateAsServer(num, "");
			}
			else
			{
				for (int i = 0; i < DisconnectedNetworkPeers.Count; i++)
				{
					PlayerData parameter = playerConnectionInfo.GetParameter<PlayerData>("PlayerData");
					if (parameter != null && DisconnectedNetworkPeers[i].VirtualPlayer.Id == parameter.PlayerId)
					{
						num2 = i;
						communicator = DisconnectedNetworkPeers[i];
						NetworkCommunicator networkCommunicator = communicator as NetworkCommunicator;
						networkCommunicator.UpdateIndexForReconnectingPlayer(num);
						networkCommunicator.UpdateConnectionInfoForReconnect(playerConnectionInfo, isAdmin);
						MBAPI.IMBPeer.SetUserData(num, new MBNetworkPeer(networkCommunicator));
						Debug.Print("RemoveFromDisconnectedPeers: " + networkCommunicator.UserName, 0, Debug.DebugColor.White, 17179869184uL);
						DisconnectedNetworkPeers.RemoveAt(i);
						break;
					}
				}
				if (communicator == null)
				{
					communicator = NetworkCommunicator.CreateAsServer(playerConnectionInfo, num, isAdmin);
				}
			}
			VirtualPlayers[communicator.VirtualPlayer.Index] = communicator.VirtualPlayer;
			if (!flag)
			{
				NetworkCommunicator networkCommunicator2 = communicator as NetworkCommunicator;
				if (serverPeer && IsServer)
				{
					ClientPeerIndex = num;
					MyPeer = networkCommunicator2;
				}
				networkCommunicator2.SessionKey = sessionKey;
				networkCommunicator2.SetServerPeer(serverPeer);
				AddNetworkPeer(networkCommunicator2);
				playerConnectionInfo.NetworkPeer = networkCommunicator2;
				if (!serverPeer)
				{
					PrepareNewUdpSession(num, sessionKey);
				}
				if (num2 < 0)
				{
					BeginBroadcastModuleEvent();
					WriteMessage(new CreatePlayer(networkCommunicator2.Index, playerConnectionInfo.Name, num2));
					EndBroadcastModuleEvent(EventBroadcastFlags.AddToMissionRecord | EventBroadcastFlags.DontSendToPeers);
				}
				foreach (NetworkCommunicator networkPeer in NetworkPeers)
				{
					if (networkPeer != networkCommunicator2 && networkPeer != MyPeer)
					{
						BeginModuleEventAsServer(networkPeer);
						WriteMessage(new CreatePlayer(networkCommunicator2.Index, playerConnectionInfo.Name, num2));
						EndModuleEventAsServer();
					}
					if (!serverPeer)
					{
						bool isReceiverPeer = networkPeer == networkCommunicator2;
						BeginModuleEventAsServer(networkCommunicator2);
						WriteMessage(new CreatePlayer(networkPeer.Index, networkPeer.UserName, -1, isNonExistingDisconnectedPeer: false, isReceiverPeer));
						EndModuleEventAsServer();
					}
				}
				for (int j = 0; j < DisconnectedNetworkPeers.Count; j++)
				{
					NetworkCommunicator networkCommunicator3 = DisconnectedNetworkPeers[j];
					BeginModuleEventAsServer(networkCommunicator2);
					WriteMessage(new CreatePlayer(networkCommunicator3.Index, networkCommunicator3.UserName, j, isNonExistingDisconnectedPeer: true));
					EndModuleEventAsServer();
				}
				foreach (IUdpNetworkHandler networkHandler in NetworkHandlers)
				{
					networkHandler.HandleNewClientConnect(playerConnectionInfo);
				}
				_handler.OnPlayerConnectedToServer(networkCommunicator2);
			}
			return communicator;
		}
		return null;
	}

	public static AddPlayersResult AddNewPlayersOnServer(PlayerConnectionInfo[] playerConnectionInfos, bool serverPeer)
	{
		bool flag = MBAPI.IMBNetwork.CanAddNewPlayersOnServer(playerConnectionInfos.Length);
		NetworkCommunicator[] array = new NetworkCommunicator[playerConnectionInfos.Length];
		if (flag)
		{
			for (int i = 0; i < array.Length; i++)
			{
				object parameter = playerConnectionInfos[i].GetParameter<object>("IsAdmin");
				bool isAdmin = parameter != null && (bool)parameter;
				ICommunicator communicator = AddNewPlayerOnServer(playerConnectionInfos[i], serverPeer, isAdmin);
				array[i] = communicator as NetworkCommunicator;
			}
		}
		return new AddPlayersResult
		{
			NetworkPeers = array,
			Success = flag
		};
	}

	public static void ClientFinishedLoading(NetworkCommunicator networkPeer)
	{
		foreach (IUdpNetworkHandler networkHandler in NetworkHandlers)
		{
			networkHandler.HandleEarlyNewClientAfterLoadingFinished(networkPeer);
		}
		foreach (IUdpNetworkHandler networkHandler2 in NetworkHandlers)
		{
			networkHandler2.HandleNewClientAfterLoadingFinished(networkPeer);
		}
		foreach (IUdpNetworkHandler networkHandler3 in NetworkHandlers)
		{
			networkHandler3.HandleLateNewClientAfterLoadingFinished(networkPeer);
		}
		networkPeer.IsSynchronized = true;
		foreach (IUdpNetworkHandler networkHandler4 in NetworkHandlers)
		{
			networkHandler4.HandleNewClientAfterSynchronized(networkPeer);
		}
		foreach (IUdpNetworkHandler networkHandler5 in NetworkHandlers)
		{
			networkHandler5.HandleLateNewClientAfterSynchronized(networkPeer);
		}
	}

	public static void BeginModuleEventAsClient()
	{
		MBAPI.IMBNetwork.BeginModuleEventAsClient(isReliable: true);
	}

	public static void EndModuleEventAsClient()
	{
		MBAPI.IMBNetwork.EndModuleEventAsClient(isReliable: true);
	}

	public static void BeginModuleEventAsClientUnreliable()
	{
		MBAPI.IMBNetwork.BeginModuleEventAsClient(isReliable: false);
	}

	public static void EndModuleEventAsClientUnreliable()
	{
		MBAPI.IMBNetwork.EndModuleEventAsClient(isReliable: false);
	}

	public static void BeginModuleEventAsServer(NetworkCommunicator communicator)
	{
		BeginModuleEventAsServer(communicator.VirtualPlayer);
	}

	public static void BeginModuleEventAsServerUnreliable(NetworkCommunicator communicator)
	{
		BeginModuleEventAsServerUnreliable(communicator.VirtualPlayer);
	}

	public static void BeginModuleEventAsServer(VirtualPlayer peer)
	{
		MBAPI.IMBPeer.BeginModuleEvent(peer.Index, isReliable: true);
	}

	public static void EndModuleEventAsServer()
	{
		MBAPI.IMBPeer.EndModuleEvent(isReliable: true);
	}

	public static void BeginModuleEventAsServerUnreliable(VirtualPlayer peer)
	{
		MBAPI.IMBPeer.BeginModuleEvent(peer.Index, isReliable: false);
	}

	public static void EndModuleEventAsServerUnreliable()
	{
		MBAPI.IMBPeer.EndModuleEvent(isReliable: false);
	}

	public static void BeginBroadcastModuleEvent()
	{
		MBAPI.IMBNetwork.BeginBroadcastModuleEvent();
	}

	public static void EndBroadcastModuleEvent(EventBroadcastFlags broadcastFlags, NetworkCommunicator targetPlayer = null)
	{
		int targetPlayer2 = targetPlayer?.Index ?? (-1);
		MBAPI.IMBNetwork.EndBroadcastModuleEvent((int)broadcastFlags, targetPlayer2, isReliable: true);
	}

	public static double ElapsedTimeSinceLastUdpPacketArrived()
	{
		return MBAPI.IMBNetwork.ElapsedTimeSinceLastUdpPacketArrived();
	}

	public static void EndBroadcastModuleEventUnreliable(EventBroadcastFlags broadcastFlags, NetworkCommunicator targetPlayer = null)
	{
		int targetPlayer2 = targetPlayer?.Index ?? (-1);
		MBAPI.IMBNetwork.EndBroadcastModuleEvent((int)broadcastFlags, targetPlayer2, isReliable: false);
	}

	public static void UnSynchronizeEveryone()
	{
		Debug.Print("UnSynchronizeEveryone is called!", 0, Debug.DebugColor.White, 17179869184uL);
		foreach (NetworkCommunicator networkPeer in NetworkPeers)
		{
			networkPeer.IsSynchronized = false;
		}
		foreach (IUdpNetworkHandler networkHandler in NetworkHandlers)
		{
			networkHandler.OnEveryoneUnSynchronized();
		}
	}

	public static void AddRemoveMessageHandlers(NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new NetworkMessageHandlerRegisterer(mode);
		networkMessageHandlerRegisterer.Register<CreatePlayer>(HandleServerEventCreatePlayer);
		networkMessageHandlerRegisterer.Register<DeletePlayer>(HandleServerEventDeletePlayer);
	}

	public static void StartMultiplayerOnClient(string serverAddress, int port, int sessionKey, int playerIndex)
	{
		Debug.Print("StartMultiplayerOnClient");
		MBCommon.CurrentGameType = MBCommon.GameType.MultiClient;
		ClientPeerIndex = playerIndex;
		InitializeClientSide(serverAddress, port, sessionKey, playerIndex);
		StartMultiplayer();
		AddRemoveMessageHandlers(NetworkMessageHandlerRegisterer.RegisterMode.Add);
	}

	[MBCallback(null, false)]
	internal static bool HandleNetworkPacketAsClient()
	{
		if (!TWParallel.IsMainThread())
		{
			Debug.FailedAssert("Network messages should be handled from main thread", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\GameNetwork.cs", "HandleNetworkPacketAsClient", 1204);
		}
		bool bufferReadValid = true;
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.NetworkComponentEventTypeFromServerCompressionInfo, ref bufferReadValid);
		if (bufferReadValid && num >= 0 && num < _gameNetworkMessageIdsFromServer.Count)
		{
			GameNetworkMessage gameNetworkMessage = Activator.CreateInstance(_gameNetworkMessageIdsFromServer[num]) as GameNetworkMessage;
			gameNetworkMessage.MessageId = num;
			Debug.Print("Reading message: " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.White, 17179869184uL);
			bufferReadValid = gameNetworkMessage.Read();
			if (bufferReadValid)
			{
				if (!NetworkMain.GameClient.IsInGame && !IsReplay && !NetworkMain.CommunityClient.IsInGame)
				{
					Debug.Print("ignoring post mission message: " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.White, 17179869184uL);
				}
				else
				{
					bool flag = false;
					bool flag2 = true;
					if ((gameNetworkMessage.GetLogFilter() & MultiplayerLogging) != MultiplayerMessageFilter.None)
					{
						if (GameNetworkMessage.IsClientMissionOver)
						{
							Debug.Print("WARNING: Entering message processing while client mission is over");
						}
						Debug.Print("Processing message: " + gameNetworkMessage.GetType().Name + ": " + gameNetworkMessage.GetLogFormat(), 0, Debug.DebugColor.White, 17179869184uL);
					}
					if (_fromServerBaseMessageHandlers.TryGetValue(num, out var value))
					{
						foreach (GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage> item in value)
						{
							try
							{
								item(gameNetworkMessage);
							}
							catch
							{
								Debug.Print("Exception in handler of " + num, 0, Debug.DebugColor.White, 17179869184uL);
								Debug.Print("Exception in handler of " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.Red, 17179869184uL);
							}
						}
						flag2 = false;
						flag = value.Count != 0;
					}
					if (_fromServerMessageHandlers.TryGetValue(num, out var value2))
					{
						foreach (object item2 in value2)
						{
							(item2 as Delegate).DynamicInvokeWithLog(gameNetworkMessage);
						}
						flag2 = false;
						flag = flag || value2.Count != 0;
					}
					if (flag2)
					{
						Debug.Print("Invalid messageId " + num, 0, Debug.DebugColor.White, 17179869184uL);
						Debug.Print("Invalid messageId " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.White, 17179869184uL);
					}
					else if (!flag)
					{
						Debug.Print("No message handler found for " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.Red, 17179869184uL);
					}
				}
			}
			else
			{
				Debug.Print("Invalid message read for: " + gameNetworkMessage.GetType().Name, 0, Debug.DebugColor.White, 17179869184uL);
			}
		}
		else
		{
			Debug.Print("Invalid message id read: " + num, 0, Debug.DebugColor.White, 17179869184uL);
		}
		return bufferReadValid;
	}

	private static int GetSessionKeyForPlayer()
	{
		return new Random(DateTime.Now.Millisecond).Next(1, 4001);
	}

	public static NetworkCommunicator HandleNewClientConnect(PlayerConnectionInfo playerConnectionInfo, bool isAdmin)
	{
		NetworkCommunicator networkCommunicator = AddNewPlayerOnServer(playerConnectionInfo, serverPeer: false, isAdmin) as NetworkCommunicator;
		_handler.OnNewPlayerConnect(playerConnectionInfo, networkCommunicator);
		return networkCommunicator;
	}

	public static AddPlayersResult HandleNewClientsConnect(PlayerConnectionInfo[] playerConnectionInfos, bool isAdmin)
	{
		AddPlayersResult result = AddNewPlayersOnServer(playerConnectionInfos, isAdmin);
		if (result.Success)
		{
			for (int i = 0; i < playerConnectionInfos.Length; i++)
			{
				_handler.OnNewPlayerConnect(playerConnectionInfos[i], result.NetworkPeers[i]);
			}
		}
		return result;
	}

	public static void AddNetworkPeerToDisconnectAsServer(NetworkCommunicator networkPeer)
	{
		Debug.Print("adding peer to disconnect index:" + networkPeer.Index, 0, Debug.DebugColor.White, 17179869184uL);
		AddPeerToDisconnect(networkPeer);
		BeginModuleEventAsServer(networkPeer);
		WriteMessage(new DeletePlayer(networkPeer.Index, addToDisconnectList: false));
		EndModuleEventAsServer();
	}

	private static void HandleServerEventCreatePlayer(CreatePlayer message)
	{
		int playerIndex = message.PlayerIndex;
		string playerName = message.PlayerName;
		bool isReceiverPeer = message.IsReceiverPeer;
		NetworkCommunicator networkCommunicator;
		if (isReceiverPeer || message.IsNonExistingDisconnectedPeer || message.DisconnectedPeerIndex < 0)
		{
			networkCommunicator = NetworkCommunicator.CreateAsClient(playerName, playerIndex);
		}
		else
		{
			networkCommunicator = DisconnectedNetworkPeers[message.DisconnectedPeerIndex];
			networkCommunicator.UpdateIndexForReconnectingPlayer(message.PlayerIndex);
			Debug.Print("RemoveFromDisconnectedPeers: " + networkCommunicator.UserName, 0, Debug.DebugColor.White, 17179869184uL);
			DisconnectedNetworkPeers.RemoveAt(message.DisconnectedPeerIndex);
		}
		if (isReceiverPeer)
		{
			MyPeer = networkCommunicator;
		}
		if (message.IsNonExistingDisconnectedPeer)
		{
			AddToDisconnectedPeers(networkCommunicator);
		}
		else
		{
			VirtualPlayers[networkCommunicator.VirtualPlayer.Index] = networkCommunicator.VirtualPlayer;
			AddNetworkPeer(networkCommunicator);
		}
		_handler.OnPlayerConnectedToServer(networkCommunicator);
	}

	private static void HandleServerEventDeletePlayer(DeletePlayer message)
	{
		NetworkCommunicator networkCommunicator = NetworkPeers.FirstOrDefault((NetworkCommunicator networkPeer) => networkPeer.Index == message.PlayerIndex);
		if (networkCommunicator != null)
		{
			HandleRemovePlayerInternal(networkCommunicator, message.AddToDisconnectList);
		}
	}

	public static void InitializeClientSide(string serverAddress, int port, int sessionKey, int playerIndex)
	{
		MBAPI.IMBNetwork.InitializeClientSide(serverAddress, port, sessionKey, playerIndex);
	}

	public static void TerminateClientSide()
	{
		MBAPI.IMBNetwork.TerminateClientSide();
		MBCommon.CurrentGameType = MBCommon.GameType.Single;
	}

	public static Type GetSynchedMissionObjectReadableRecordTypeFromIndex(int typeIndex)
	{
		return _synchedMissionObjectClassTypes[typeIndex];
	}

	public static int GetSynchedMissionObjectReadableRecordIndexFromType(Type type)
	{
		for (int i = 0; i < _synchedMissionObjectClassTypes.Count; i++)
		{
			Type element = _synchedMissionObjectClassTypes[i];
			DefineSynchedMissionObjectType customAttribute = element.GetCustomAttribute<DefineSynchedMissionObjectType>();
			DefineSynchedMissionObjectTypeForMod customAttribute2 = element.GetCustomAttribute<DefineSynchedMissionObjectTypeForMod>();
			Type type2 = customAttribute?.Type ?? customAttribute2?.Type;
			Type type3 = type;
			while (type3 != null)
			{
				if (type3 == type2)
				{
					return i;
				}
				type3 = type3.BaseType;
			}
		}
		return -1;
	}

	public static void DestroyComponent(UdpNetworkComponent udpNetworkComponent)
	{
		RemoveNetworkHandler(udpNetworkComponent);
		NetworkComponents.Remove(udpNetworkComponent);
	}

	public static T AddNetworkComponent<T>() where T : UdpNetworkComponent
	{
		T val = (T)Activator.CreateInstance(typeof(T), new object[0]);
		NetworkComponents.Add(val);
		NetworkHandlers.Add(val);
		return val;
	}

	public static void AddNetworkHandler(IUdpNetworkHandler handler)
	{
		NetworkHandlers.Add(handler);
	}

	public static void RemoveNetworkHandler(IUdpNetworkHandler handler)
	{
		handler.OnUdpNetworkHandlerClose();
		NetworkHandlers.Remove(handler);
	}

	public static T GetNetworkComponent<T>() where T : UdpNetworkComponent
	{
		foreach (UdpNetworkComponent networkComponent in NetworkComponents)
		{
			if (networkComponent is T result)
			{
				return result;
			}
		}
		return null;
	}

	public static void WriteMessage(GameNetworkMessage message)
	{
		if ((message.GetLogFilter() & MultiplayerLogging) != MultiplayerMessageFilter.None)
		{
			Debug.Print("Writing message: " + message.GetLogFormat(), 0, Debug.DebugColor.White, 17179869184uL);
		}
		Type type = message.GetType();
		message.MessageId = _gameNetworkMessageTypesAll[type];
		message.Write();
	}

	private static void AddServerMessageHandler<T>(GameNetworkMessage.ServerMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
	{
		int key = _gameNetworkMessageTypesFromServer[typeof(T)];
		_fromServerMessageHandlers[key].Add(handler);
	}

	private static void AddServerBaseMessageHandler(GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage> handler, Type messageType)
	{
		int key = _gameNetworkMessageTypesFromServer[messageType];
		_fromServerBaseMessageHandlers[key].Add(handler);
	}

	private static void AddClientMessageHandler<T>(GameNetworkMessage.ClientMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
	{
		int key = _gameNetworkMessageTypesFromClient[typeof(T)];
		_fromClientMessageHandlers[key].Add(handler);
	}

	private static void AddClientBaseMessageHandler(GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage> handler, Type messageType)
	{
		int key = _gameNetworkMessageTypesFromClient[messageType];
		_fromClientBaseMessageHandlers[key].Add(handler);
	}

	private static void RemoveServerMessageHandler<T>(GameNetworkMessage.ServerMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
	{
		int key = _gameNetworkMessageTypesFromServer[typeof(T)];
		_fromServerMessageHandlers[key].Remove(handler);
	}

	private static void RemoveServerBaseMessageHandler(GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage> handler, Type messageType)
	{
		int key = _gameNetworkMessageTypesFromServer[messageType];
		_fromServerBaseMessageHandlers[key].Remove(handler);
	}

	private static void RemoveClientMessageHandler<T>(GameNetworkMessage.ClientMessageHandlerDelegate<T> handler) where T : GameNetworkMessage
	{
		int key = _gameNetworkMessageTypesFromClient[typeof(T)];
		_fromClientMessageHandlers[key].Remove(handler);
	}

	internal static void FindGameNetworkMessages()
	{
		Debug.Print("Searching Game NetworkMessages Methods", 0, Debug.DebugColor.White, 17179869184uL);
		_fromClientMessageHandlers = new Dictionary<int, List<object>>();
		_fromServerMessageHandlers = new Dictionary<int, List<object>>();
		_fromClientBaseMessageHandlers = new Dictionary<int, List<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>>>();
		_fromServerBaseMessageHandlers = new Dictionary<int, List<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>>>();
		_gameNetworkMessageTypesAll = new Dictionary<Type, int>();
		_gameNetworkMessageTypesFromClient = new Dictionary<Type, int>();
		_gameNetworkMessageTypesFromServer = new Dictionary<Type, int>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		List<Type> list = new List<Type>();
		List<Type> list2 = new List<Type>();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			if (CheckAssemblyForNetworkMessage(assembly))
			{
				CollectGameNetworkMessagesFromAssembly(assembly, list, list2);
			}
		}
		list.Sort((Type s1, Type s2) => s1.FullName.CompareTo(s2.FullName));
		list2.Sort((Type s1, Type s2) => s1.FullName.CompareTo(s2.FullName));
		_gameNetworkMessageIdsFromClient = new List<Type>(list.Count);
		for (int num = 0; num < list.Count; num++)
		{
			Type type = list[num];
			_gameNetworkMessageIdsFromClient.Add(type);
			_gameNetworkMessageTypesFromClient.Add(type, num);
			_gameNetworkMessageTypesAll.Add(type, num);
			_fromClientMessageHandlers.Add(num, new List<object>());
			_fromClientBaseMessageHandlers.Add(num, new List<GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage>>());
		}
		_gameNetworkMessageIdsFromServer = new List<Type>(list2.Count);
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			Type type2 = list2[num2];
			_gameNetworkMessageIdsFromServer.Add(type2);
			_gameNetworkMessageTypesFromServer.Add(type2, num2);
			_gameNetworkMessageTypesAll.Add(type2, num2);
			_fromServerMessageHandlers.Add(num2, new List<object>());
			_fromServerBaseMessageHandlers.Add(num2, new List<GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>>());
		}
		CompressionBasic.NetworkComponentEventTypeFromClientCompressionInfo = new CompressionInfo.Integer(0, list.Count - 1, maximumValueGiven: true);
		CompressionBasic.NetworkComponentEventTypeFromServerCompressionInfo = new CompressionInfo.Integer(0, list2.Count - 1, maximumValueGiven: true);
		Debug.Print("Found " + list.Count + " Client Game Network Messages", 0, Debug.DebugColor.White, 17179869184uL);
		Debug.Print("Found " + list2.Count + " Server Game Network Messages", 0, Debug.DebugColor.White, 17179869184uL);
	}

	internal static void FindSynchedMissionObjectTypes()
	{
		Debug.Print("Searching Game SynchedMissionObjects", 0, Debug.DebugColor.White, 17179869184uL);
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		_synchedMissionObjectClassTypes = new List<Type>();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			if (CheckAssemblyForNetworkMessage(assembly))
			{
				CollectSynchedMissionObjectTypesFromAssembly(assembly, _synchedMissionObjectClassTypes);
			}
		}
		_synchedMissionObjectClassTypes.Sort((Type s1, Type s2) => s1.FullName.CompareTo(s2.FullName));
	}

	private static void RemoveClientBaseMessageHandler(GameNetworkMessage.ClientMessageHandlerDelegate<GameNetworkMessage> handler, Type messageType)
	{
		int key = _gameNetworkMessageTypesFromClient[messageType];
		_fromClientBaseMessageHandlers[key].Remove(handler);
	}

	private static bool CheckAssemblyForNetworkMessage(Assembly assembly)
	{
		Assembly assembly2 = Assembly.GetAssembly(typeof(GameNetworkMessage));
		if (assembly == assembly2)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		for (int i = 0; i < referencedAssemblies.Length; i++)
		{
			if (referencedAssemblies[i].FullName == assembly2.FullName)
			{
				return true;
			}
		}
		return false;
	}

	public static void SetServerBandwidthLimitInMbps(double value)
	{
		MBAPI.IMBNetwork.SetServerBandwidthLimitInMbps(value);
	}

	public static void SetServerTickRate(double value)
	{
		MBAPI.IMBNetwork.SetServerTickRate(value);
	}

	public static void SetServerFrameRate(double value)
	{
		MBAPI.IMBNetwork.SetServerFrameRate(value);
	}

	public static void ResetDebugVariables()
	{
		MBAPI.IMBNetwork.ResetDebugVariables();
	}

	public static void PrintDebugStats()
	{
		MBAPI.IMBNetwork.PrintDebugStats();
	}

	public static float GetAveragePacketLossRatio()
	{
		return MBAPI.IMBNetwork.GetAveragePacketLossRatio();
	}

	public static void GetDebugUploadsInBits(ref DebugNetworkPacketStatisticsStruct networkStatisticsStruct, ref DebugNetworkPositionCompressionStatisticsStruct posStatisticsStruct)
	{
		MBAPI.IMBNetwork.GetDebugUploadsInBits(ref networkStatisticsStruct, ref posStatisticsStruct);
	}

	public static void PrintReplicationTableStatistics()
	{
		MBAPI.IMBNetwork.PrintReplicationTableStatistics();
	}

	public static void ClearReplicationTableStatistics()
	{
		MBAPI.IMBNetwork.ClearReplicationTableStatistics();
	}

	public static void ResetDebugUploads()
	{
		MBAPI.IMBNetwork.ResetDebugUploads();
	}

	public static void ResetMissionData()
	{
		MBAPI.IMBNetwork.ResetMissionData();
	}

	private static void AddPeerToDisconnect(NetworkCommunicator networkPeer)
	{
		MBAPI.IMBNetwork.AddPeerToDisconnect(networkPeer.Index);
	}

	public static void InitializeCompressionInfos()
	{
		CompressionBasic.ActionCodeCompressionInfo = new CompressionInfo.Integer(ActionIndexCache.act_none.Index, MBAnimation.GetNumActionCodes() - 1, maximumValueGiven: true);
		CompressionBasic.AnimationIndexCompressionInfo = new CompressionInfo.Integer(0, MBAnimation.GetNumAnimations() - 1, maximumValueGiven: true);
		CompressionBasic.CultureIndexCompressionInfo = new CompressionInfo.Integer(-1, MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().Count - 1, maximumValueGiven: true);
		CompressionBasic.SoundEventsCompressionInfo = new CompressionInfo.Integer(0, SoundEvent.GetTotalEventCount() - 1, maximumValueGiven: true);
		CompressionMission.ActionSetCompressionInfo = new CompressionInfo.Integer(0, MBActionSet.GetNumberOfActionSets() - 1, maximumValueGiven: true);
		CompressionMission.MonsterUsageSetCompressionInfo = new CompressionInfo.Integer(0, MBActionSet.GetNumberOfMonsterUsageSets() - 1, maximumValueGiven: true);
	}

	[MBCallback(null, false)]
	internal static void SyncRelevantGameOptionsToServer()
	{
		SyncRelevantGameOptionsToServer syncRelevantGameOptionsToServer = new SyncRelevantGameOptionsToServer();
		syncRelevantGameOptionsToServer.InitializeOptions();
		BeginModuleEventAsClient();
		WriteMessage(syncRelevantGameOptionsToServer);
		EndModuleEventAsClient();
	}

	private static void CollectGameNetworkMessagesFromAssembly(Assembly assembly, List<Type> gameNetworkMessagesFromClient, List<Type> gameNetworkMessagesFromServer)
	{
		Type typeFromHandle = typeof(GameNetworkMessage);
		bool? flag = null;
		List<Type> typesSafe = assembly.GetTypesSafe();
		for (int i = 0; i < typesSafe.Count; i++)
		{
			Type type = typesSafe[i];
			if (!typeFromHandle.IsAssignableFrom(type) || !(type != typeFromHandle) || !type.IsSealed || type.GetConstructor(Type.EmptyTypes) == null)
			{
				continue;
			}
			DefineGameNetworkMessageType customAttribute = type.GetCustomAttribute<DefineGameNetworkMessageType>();
			if (customAttribute != null)
			{
				if (!flag.HasValue || !flag.Value)
				{
					flag = false;
					switch (customAttribute.SendType)
					{
					case GameNetworkMessageSendType.FromServer:
					case GameNetworkMessageSendType.DebugFromServer:
						gameNetworkMessagesFromServer.Add(type);
						break;
					case GameNetworkMessageSendType.FromClient:
						gameNetworkMessagesFromClient.Add(type);
						break;
					}
				}
				continue;
			}
			DefineGameNetworkMessageTypeForMod customAttribute2 = type.GetCustomAttribute<DefineGameNetworkMessageTypeForMod>();
			if (customAttribute2 != null && (!flag.HasValue || flag.Value))
			{
				flag = true;
				switch (customAttribute2.SendType)
				{
				case GameNetworkMessageSendType.FromServer:
				case GameNetworkMessageSendType.DebugFromServer:
					gameNetworkMessagesFromServer.Add(type);
					break;
				case GameNetworkMessageSendType.FromClient:
					gameNetworkMessagesFromClient.Add(type);
					break;
				}
			}
		}
	}

	private static void CollectSynchedMissionObjectTypesFromAssembly(Assembly assembly, List<Type> synchedMissionObjectClassTypes)
	{
		Type typeFromHandle = typeof(ISynchedMissionObjectReadableRecord);
		bool? flag = null;
		List<Type> typesSafe = assembly.GetTypesSafe();
		for (int i = 0; i < typesSafe.Count; i++)
		{
			Type type = typesSafe[i];
			if (!typeFromHandle.IsAssignableFrom(type) || !(type != typeFromHandle))
			{
				continue;
			}
			if (type.GetCustomAttribute<DefineSynchedMissionObjectType>() != null)
			{
				if (!flag.HasValue || !flag.Value)
				{
					flag = false;
					synchedMissionObjectClassTypes.Add(type);
				}
			}
			else if (type.GetCustomAttribute<DefineSynchedMissionObjectTypeForMod>() != null && (!flag.HasValue || flag.Value))
			{
				flag = true;
				synchedMissionObjectClassTypes.Add(type);
			}
		}
	}
}
