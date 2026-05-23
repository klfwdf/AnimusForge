using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class ChatBox : GameHandler
{
	private class QueuedMessageInfo
	{
		public readonly NetworkCommunicator SourcePeer;

		public readonly string Message;

		public readonly List<VirtualPlayer> ReceiverList;

		private const float _timeOutDuration = 3f;

		private DateTime _creationTime;

		public bool IsExpired => (DateTime.Now - _creationTime).TotalSeconds >= 3.0;

		public QueuedMessageInfo(NetworkCommunicator sourcePeer, string message, List<VirtualPlayer> receiverList)
		{
			SourcePeer = sourcePeer;
			Message = message;
			_creationTime = DateTime.Now;
			ReceiverList = receiverList;
		}
	}

	private static ChatBox _chatBox;

	private bool _isNetworkInitialized;

	public const string AdminMessageSoundEvent = "event:/ui/notification/alert";

	private List<PlayerId> _mutedPlayers = new List<PlayerId>();

	private List<PlayerId> _platformMutedPlayers = new List<PlayerId>();

	private ProfanityChecker _profanityChecker;

	private static List<QueuedMessageInfo> _queuedTeamMessages;

	private static List<QueuedMessageInfo> _queuedEveryoneMessages;

	public Action<NetworkCommunicator, string> OnMessageReceivedAtDedicatedServer;

	public bool IsContentRestricted { get; private set; }

	public bool NetworkReady
	{
		get
		{
			if (!GameNetwork.IsClient && !GameNetwork.IsServer)
			{
				if (NetworkMain.GameClient != null)
				{
					return NetworkMain.GameClient.Connected;
				}
				return false;
			}
			return true;
		}
	}

	public event PlayerMessageReceivedDelegate PlayerMessageReceived;

	public event WhisperMessageSentDelegate WhisperMessageSent;

	public event WhisperMessageReceivedDelegate WhisperMessageReceived;

	public event ErrorWhisperMessageReceivedDelegate ErrorWhisperMessageReceived;

	public event ServerMessageDelegate ServerMessage;

	public event ServerAdminMessageDelegate ServerAdminMessage;

	public event PlayerMutedDelegate OnPlayerMuteChanged;

	protected override void OnGameStart()
	{
		_chatBox = this;
	}

	public override void OnBeforeSave()
	{
	}

	public override void OnAfterSave()
	{
	}

	protected override void OnGameEnd()
	{
		_chatBox = null;
	}

	public void SendMessageToAll(string message)
	{
		SendMessageToAll(message, null);
	}

	public void SendMessageToAll(string message, List<VirtualPlayer> receiverList)
	{
		if (GameNetwork.IsClient && !IsContentRestricted)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new NetworkMessages.FromClient.PlayerMessageAll(message));
			GameNetwork.EndModuleEventAsClient();
		}
		else if (GameNetwork.IsServer)
		{
			ServerPrepareAndSendMessage(GameNetwork.MyPeer, toTeamOnly: false, message, receiverList);
		}
	}

	public void SendMessageToTeam(string message)
	{
		SendMessageToTeam(message, null);
	}

	public void SendMessageToTeam(string message, List<VirtualPlayer> receiverList)
	{
		if (GameNetwork.IsClient && !IsContentRestricted)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new NetworkMessages.FromClient.PlayerMessageTeam(message));
			GameNetwork.EndModuleEventAsClient();
		}
		else if (GameNetwork.IsServer)
		{
			ServerPrepareAndSendMessage(GameNetwork.MyPeer, toTeamOnly: true, message, receiverList);
		}
	}

	public void SendMessageToWhisperTarget(string message, string platformName, string whisperTarget)
	{
		if (NetworkMain.GameClient != null && NetworkMain.GameClient.Connected)
		{
			NetworkMain.GameClient.SendWhisper(whisperTarget, message);
			if (this.WhisperMessageSent != null)
			{
				this.WhisperMessageSent(message, whisperTarget);
			}
		}
	}

	private void OnServerMessage(string message)
	{
		if (this.ServerMessage != null)
		{
			this.ServerMessage(message);
		}
	}

	protected override void OnGameNetworkBegin()
	{
		_queuedTeamMessages = new List<QueuedMessageInfo>();
		_queuedEveryoneMessages = new List<QueuedMessageInfo>();
		_isNetworkInitialized = true;
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
	}

	private void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
		if (GameNetwork.IsClient)
		{
			networkMessageHandlerRegisterer.Register<NetworkMessages.FromServer.PlayerMessageTeam>(HandleServerEventPlayerMessageTeam);
			networkMessageHandlerRegisterer.Register<NetworkMessages.FromServer.PlayerMessageAll>(HandleServerEventPlayerMessageAll);
			networkMessageHandlerRegisterer.Register<ServerMessage>(HandleServerEventServerMessage);
			networkMessageHandlerRegisterer.Register<ServerAdminMessage>(HandleServerEventServerAdminMessage);
		}
		else if (GameNetwork.IsServer)
		{
			networkMessageHandlerRegisterer.Register<NetworkMessages.FromClient.PlayerMessageAll>(HandleClientEventPlayerMessageAll);
			networkMessageHandlerRegisterer.Register<NetworkMessages.FromClient.PlayerMessageTeam>(HandleClientEventPlayerMessageTeam);
		}
	}

	protected override void OnGameNetworkEnd()
	{
		base.OnGameNetworkEnd();
		AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
	}

	private void HandleServerEventPlayerMessageAll(NetworkMessages.FromServer.PlayerMessageAll message)
	{
		if (IsContentRestricted)
		{
			return;
		}
		ShouldShowPlayersMessage(message.Player.VirtualPlayer.Id, delegate(bool result)
		{
			if (result)
			{
				OnPlayerMessageReceived(message.Player, message.Message, toTeamOnly: false);
			}
		});
	}

	private void HandleServerEventPlayerMessageTeam(NetworkMessages.FromServer.PlayerMessageTeam message)
	{
		if (IsContentRestricted)
		{
			return;
		}
		ShouldShowPlayersMessage(message.Player.VirtualPlayer.Id, delegate(bool result)
		{
			if (result)
			{
				OnPlayerMessageReceived(message.Player, message.Message, toTeamOnly: true);
			}
		});
	}

	private void HandleServerEventServerMessage(ServerMessage message)
	{
		OnServerMessage(message.IsMessageTextId ? GameTexts.FindText(message.Message).ToString() : message.Message);
	}

	private void HandleServerEventServerAdminMessage(ServerAdminMessage message)
	{
		if (message.IsAdminBroadcast)
		{
			TextObject textObject = new TextObject("{=!}{ADMIN_TEXT}");
			textObject.SetTextVariable("ADMIN_TEXT", message.Message);
			MBInformationManager.AddQuickInformation(textObject, 5000);
			SoundEvent.PlaySound2D("event:/ui/notification/alert");
		}
		this.ServerAdminMessage?.Invoke(message.Message);
	}

	private bool HandleClientEventPlayerMessageAll(NetworkCommunicator networkPeer, NetworkMessages.FromClient.PlayerMessageAll message)
	{
		return ServerPrepareAndSendMessage(networkPeer, toTeamOnly: false, message.Message, message.ReceiverList);
	}

	private bool HandleClientEventPlayerMessageTeam(NetworkCommunicator networkPeer, NetworkMessages.FromClient.PlayerMessageTeam message)
	{
		return ServerPrepareAndSendMessage(networkPeer, toTeamOnly: true, message.Message, message.ReceiverList);
	}

	public static void ServerSendServerMessageToEveryone(string message)
	{
		_chatBox.OnServerMessage(message);
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new ServerMessage(message));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
	}

	private bool ServerPrepareAndSendMessage(NetworkCommunicator fromPeer, bool toTeamOnly, string message, List<VirtualPlayer> receiverList)
	{
		if (GameNetwork.IsDedicatedServer)
		{
			OnMessageReceivedAtDedicatedServer?.Invoke(fromPeer, message);
		}
		if (fromPeer.IsMuted || MultiplayerGlobalMutedPlayersManager.IsUserMuted(fromPeer.VirtualPlayer.Id))
		{
			GameNetwork.BeginModuleEventAsServer(fromPeer);
			GameNetwork.WriteMessage(new ServerMessage("str_multiplayer_muted_message", isMessageTextId: true));
			GameNetwork.EndModuleEventAsServer();
			return true;
		}
		if (_profanityChecker != null)
		{
			message = _profanityChecker.CensorText(message);
		}
		if (!GameNetwork.IsDedicatedServer && fromPeer != GameNetwork.MyPeer && !_mutedPlayers.Contains(fromPeer.VirtualPlayer.Id) && !PermaMuteList.IsPlayerMuted(fromPeer.VirtualPlayer.Id))
		{
			MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			if (component == null)
			{
				return false;
			}
			bool flag = false;
			if (toTeamOnly)
			{
				if (component == null)
				{
					return false;
				}
				MissionPeer component2 = fromPeer.GetComponent<MissionPeer>();
				if (component2 == null)
				{
					return false;
				}
				flag = component.Team == component2.Team;
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				OnPlayerMessageReceived(fromPeer, message, toTeamOnly);
			}
		}
		if (toTeamOnly)
		{
			ServerSendMessageToTeam(fromPeer, message, receiverList);
		}
		else
		{
			ServerSendMessageToEveryone(fromPeer, message, receiverList);
		}
		return true;
	}

	private static void ServerSendMessageToTeam(NetworkCommunicator networkPeer, string message, List<VirtualPlayer> receiverList)
	{
		if (!networkPeer.IsSynchronized)
		{
			_queuedTeamMessages.Add(new QueuedMessageInfo(networkPeer, message, receiverList));
			return;
		}
		MissionPeer missionPeer = networkPeer.GetComponent<MissionPeer>();
		if (missionPeer?.Team != null)
		{
			foreach (NetworkCommunicator item in GameNetwork.NetworkPeers.Where((NetworkCommunicator x) => !x.IsServerPeer && x.IsSynchronized && x.GetComponent<MissionPeer>().Team == missionPeer.Team))
			{
				if (receiverList == null || receiverList.Contains(item.VirtualPlayer))
				{
					GameNetwork.BeginModuleEventAsServer(item);
					GameNetwork.WriteMessage(new NetworkMessages.FromServer.PlayerMessageTeam(networkPeer, message));
					GameNetwork.EndModuleEventAsServer();
				}
			}
			return;
		}
		ServerSendMessageToEveryone(networkPeer, message, receiverList);
	}

	private static void ServerSendMessageToEveryone(NetworkCommunicator networkPeer, string message, List<VirtualPlayer> receiverList)
	{
		if (!networkPeer.IsSynchronized)
		{
			_queuedEveryoneMessages.Add(new QueuedMessageInfo(networkPeer, message, receiverList));
			return;
		}
		foreach (NetworkCommunicator item in GameNetwork.NetworkPeers.Where((NetworkCommunicator x) => !x.IsServerPeer && x.IsSynchronized))
		{
			if (receiverList == null || receiverList.Contains(item.VirtualPlayer))
			{
				GameNetwork.BeginModuleEventAsServer(item);
				GameNetwork.WriteMessage(new NetworkMessages.FromServer.PlayerMessageAll(networkPeer, message));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	public void ResetMuteList()
	{
		_mutedPlayers.Clear();
	}

	public static void AddWhisperMessage(string fromUserName, string messageBody)
	{
		_chatBox.OnWhisperMessageReceived(fromUserName, messageBody);
	}

	public static void AddErrorWhisperMessage(string toUserName)
	{
		_chatBox.OnErrorWhisperMessageReceived(toUserName);
	}

	private void OnWhisperMessageReceived(string fromUserName, string messageBody)
	{
		if (this.WhisperMessageReceived != null)
		{
			this.WhisperMessageReceived(fromUserName, messageBody);
		}
	}

	private void OnErrorWhisperMessageReceived(string toUserName)
	{
		if (this.ErrorWhisperMessageReceived != null)
		{
			this.ErrorWhisperMessageReceived(toUserName);
		}
	}

	private void OnPlayerMessageReceived(NetworkCommunicator networkPeer, string message, bool toTeamOnly)
	{
		if (this.PlayerMessageReceived != null)
		{
			this.PlayerMessageReceived(networkPeer, message, toTeamOnly);
		}
	}

	public void SetPlayerMuted(PlayerId playerID, bool isMuted)
	{
		if (isMuted)
		{
			OnPlayerMuted(playerID);
		}
		else
		{
			OnPlayerUnmuted(playerID);
		}
	}

	public void SetPlayerMutedFromPlatform(PlayerId playerID, bool isMuted)
	{
		if (isMuted && !_platformMutedPlayers.Contains(playerID))
		{
			_platformMutedPlayers.Add(playerID);
		}
		else if (!isMuted && _platformMutedPlayers.Contains(playerID))
		{
			_platformMutedPlayers.Remove(playerID);
		}
	}

	private void OnPlayerMuted(PlayerId mutedPlayer)
	{
		if (!_mutedPlayers.Contains(mutedPlayer))
		{
			_mutedPlayers.Add(mutedPlayer);
			this.OnPlayerMuteChanged?.Invoke(mutedPlayer, isMuted: true);
		}
	}

	private void OnPlayerUnmuted(PlayerId unmutedPlayer)
	{
		if (_mutedPlayers.Contains(unmutedPlayer))
		{
			_mutedPlayers.Remove(unmutedPlayer);
			this.OnPlayerMuteChanged?.Invoke(unmutedPlayer, isMuted: false);
		}
	}

	public bool IsPlayerMuted(PlayerId player)
	{
		if (!IsPlayerMutedFromGame(player))
		{
			return IsPlayerMutedFromPlatform(player);
		}
		return true;
	}

	public bool IsPlayerMutedFromPlatform(PlayerId player)
	{
		return _platformMutedPlayers.Contains(player);
	}

	public bool IsPlayerMutedFromGame(PlayerId player)
	{
		if (GameNetwork.IsDedicatedServer)
		{
			return _mutedPlayers.Contains(player);
		}
		PlatformServices.Instance.CheckPrivilege(Privilege.Chat, displayResolveUI: false, delegate(bool result)
		{
			IsContentRestricted = !result;
		});
		if (!_mutedPlayers.Contains(player))
		{
			return PermaMuteList.IsPlayerMuted(player);
		}
		return true;
	}

	private void ShouldShowPlayersMessage(PlayerId player, Action<bool> result)
	{
		if (IsPlayerMuted(player) || !NetworkMain.GameClient.SupportedFeatures.SupportsFeatures(Features.TextChat))
		{
			result(obj: false);
			return;
		}
		if (player.ProvidedType != NetworkMain.GameClient?.PlayerID.ProvidedType)
		{
			result(obj: true);
			return;
		}
		PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingText, player, delegate(bool res)
		{
			result(res);
		});
	}

	public void SetChatFilterLists(string[] profanityList, string[] allowList)
	{
		_profanityChecker = new ProfanityChecker(profanityList, allowList);
	}

	public void InitializeForMultiplayer()
	{
		PlatformServices.Instance.CheckPrivilege(Privilege.Chat, displayResolveUI: true, delegate(bool result)
		{
			IsContentRestricted = !result;
		});
	}

	public void InitializeForSinglePlayer()
	{
		IsContentRestricted = false;
	}

	public void OnLogin()
	{
		PlatformServices.Instance.CheckPrivilege(Privilege.Chat, displayResolveUI: false, delegate(bool chatPrivilegeResult)
		{
			IsContentRestricted = !chatPrivilegeResult;
		});
	}

	protected override void OnTick(float dt)
	{
		if (!GameNetwork.IsServer || !_isNetworkInitialized)
		{
			return;
		}
		for (int num = _queuedTeamMessages.Count - 1; num >= 0; num--)
		{
			QueuedMessageInfo queuedMessageInfo = _queuedTeamMessages[num];
			if (queuedMessageInfo.SourcePeer.IsSynchronized)
			{
				ServerSendMessageToTeam(queuedMessageInfo.SourcePeer, queuedMessageInfo.Message, queuedMessageInfo.ReceiverList);
				_queuedTeamMessages.RemoveAt(num);
			}
			else if (queuedMessageInfo.IsExpired)
			{
				_queuedTeamMessages.RemoveAt(num);
			}
		}
		for (int num2 = _queuedEveryoneMessages.Count - 1; num2 >= 0; num2--)
		{
			QueuedMessageInfo queuedMessageInfo2 = _queuedEveryoneMessages[num2];
			if (queuedMessageInfo2.SourcePeer.IsSynchronized)
			{
				ServerSendMessageToEveryone(queuedMessageInfo2.SourcePeer, queuedMessageInfo2.Message, queuedMessageInfo2.ReceiverList);
				_queuedEveryoneMessages.RemoveAt(num2);
			}
			else if (queuedMessageInfo2.IsExpired)
			{
				_queuedEveryoneMessages.RemoveAt(num2);
			}
		}
	}
}
