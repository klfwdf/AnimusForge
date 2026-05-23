using System;
using System.Collections.Concurrent;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerPermissionHandler : UdpNetworkComponent
{
	private ChatBox _chatBox;

	private ConcurrentDictionary<(PlayerId PlayerId, Permission Permission), bool> _registeredEvents = new ConcurrentDictionary<(PlayerId, Permission), bool>();

	public event Action<PlayerId, bool> OnPlayerPlatformMuteChanged;

	public MultiplayerPermissionHandler()
	{
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
	}

	public override void OnUdpNetworkHandlerClose()
	{
		base.OnUdpNetworkHandlerClose();
		HandleClientDisconnect();
	}

	private void HandleClientDisconnect()
	{
		foreach (var key in _registeredEvents.Keys)
		{
			PlatformServices.Instance.UnregisterPermissionChangeEvent(key.PlayerId, key.Permission, VoicePermissionChanged);
			_registeredEvents.TryRemove((key.PlayerId, key.Permission), out var _);
		}
	}

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClient)
		{
			registerer.RegisterBaseHandler<InitializeLobbyPeer>(HandleServerEventInitializeLobbyPeer);
		}
	}

	private void HandleServerEventInitializeLobbyPeer(GameNetworkMessage baseMessage)
	{
		InitializeLobbyPeer initializeLobbyPeer = (InitializeLobbyPeer)baseMessage;
		if (GameNetwork.MyPeer != null && initializeLobbyPeer.Peer != GameNetwork.MyPeer)
		{
			if (PlatformServices.Instance.RegisterPermissionChangeEvent(initializeLobbyPeer.ProvidedId, Permission.CommunicateUsingText, TextPermissionChanged))
			{
				_registeredEvents[(initializeLobbyPeer.ProvidedId, Permission.CommunicateUsingText)] = true;
			}
			if (PlatformServices.Instance.RegisterPermissionChangeEvent(initializeLobbyPeer.ProvidedId, Permission.CommunicateUsingVoice, VoicePermissionChanged))
			{
				_registeredEvents[(initializeLobbyPeer.ProvidedId, Permission.CommunicateUsingVoice)] = true;
			}
		}
	}

	public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
		base.OnPlayerDisconnectedFromServer(networkPeer);
		bool value;
		if (PlatformServices.Instance.UnregisterPermissionChangeEvent(networkPeer.VirtualPlayer.Id, Permission.CommunicateUsingText, TextPermissionChanged))
		{
			_registeredEvents.TryRemove((networkPeer.VirtualPlayer.Id, Permission.CommunicateUsingText), out value);
		}
		if (PlatformServices.Instance.UnregisterPermissionChangeEvent(networkPeer.VirtualPlayer.Id, Permission.CommunicateUsingVoice, VoicePermissionChanged))
		{
			_registeredEvents.TryRemove((networkPeer.VirtualPlayer.Id, Permission.CommunicateUsingVoice), out value);
		}
	}

	private void TextPermissionChanged(PlayerId targetPlayerId, Permission permission, bool hasPermission)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!(targetPlayerId != networkPeer.VirtualPlayer.Id))
			{
				networkPeer.GetComponent<MissionPeer>();
				bool flag = !hasPermission;
				_chatBox.SetPlayerMutedFromPlatform(targetPlayerId, flag);
				this.OnPlayerPlatformMuteChanged?.Invoke(targetPlayerId, flag);
			}
		}
	}

	private void VoicePermissionChanged(PlayerId targetPlayerId, Permission permission, bool hasPermission)
	{
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!(targetPlayerId != networkPeer.VirtualPlayer.Id))
			{
				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				bool flag = !hasPermission;
				component.SetMutedFromPlatform(flag);
				this.OnPlayerPlatformMuteChanged?.Invoke(targetPlayerId, flag);
			}
		}
	}
}
