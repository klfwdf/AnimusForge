using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class LobbyNetworkComponent : UdpNetworkComponent
{
	public const int MaxForcedAvatarIndex = 100;

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClientOrReplay)
		{
			registerer.RegisterBaseHandler<InitializeLobbyPeer>(HandleServerEventInitializeLobbyPeer);
		}
	}

	private void HandleServerEventInitializeLobbyPeer(GameNetworkMessage baseMessage)
	{
		InitializeLobbyPeer initializeLobbyPeer = (InitializeLobbyPeer)baseMessage;
		NetworkCommunicator peer = initializeLobbyPeer.Peer;
		VirtualPlayer virtualPlayer = peer.VirtualPlayer;
		virtualPlayer.Id = initializeLobbyPeer.ProvidedId;
		virtualPlayer.IsFemale = initializeLobbyPeer.IsFemale;
		virtualPlayer.BannerCode = initializeLobbyPeer.BannerCode;
		virtualPlayer.BodyProperties = initializeLobbyPeer.BodyProperties;
		virtualPlayer.ChosenBadgeIndex = initializeLobbyPeer.ChosenBadgeIndex;
		peer.ForcedAvatarIndex = initializeLobbyPeer.ForcedAvatarIndex;
	}

	public override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		PlayerData parameter = networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData");
		Dictionary<int, List<int>> parameter2 = networkPeer.PlayerConnectionInfo.GetParameter<Dictionary<int, List<int>>>("UsedCosmetics");
		VirtualPlayer virtualPlayer = networkPeer.VirtualPlayer;
		virtualPlayer.Id = parameter.PlayerId;
		virtualPlayer.BannerCode = parameter.Sigil;
		virtualPlayer.BodyProperties = parameter.BodyProperties;
		virtualPlayer.IsFemale = parameter.IsFemale;
		virtualPlayer.ChosenBadgeIndex = parameter.ShownBadgeIndex;
		virtualPlayer.UsedCosmetics = parameter2;
		networkPeer.IsMuted = parameter.IsMuted;
	}

	public override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		VirtualPlayer virtualPlayer = networkPeer.VirtualPlayer;
		GameNetwork.BeginBroadcastModuleEvent();
		GameNetwork.WriteMessage(new InitializeLobbyPeer(networkPeer, virtualPlayer, -1));
		GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord | GameNetwork.EventBroadcastFlags.DontSendToPeers);
		foreach (NetworkCommunicator networkPeersIncludingDisconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
		{
			if (networkPeersIncludingDisconnectedPeer.IsSynchronized || networkPeersIncludingDisconnectedPeer == networkPeer)
			{
				bool flag = GameNetwork.VirtualPlayers[networkPeersIncludingDisconnectedPeer.VirtualPlayer.Index] != networkPeersIncludingDisconnectedPeer.VirtualPlayer;
				if (networkPeersIncludingDisconnectedPeer != networkPeer && !flag && networkPeersIncludingDisconnectedPeer != GameNetwork.MyPeer)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeersIncludingDisconnectedPeer);
					GameNetwork.WriteMessage(new InitializeLobbyPeer(networkPeer, virtualPlayer, -1));
					GameNetwork.EndModuleEventAsServer();
				}
				if (!networkPeer.IsServerPeer)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new InitializeLobbyPeer(networkPeersIncludingDisconnectedPeer, networkPeersIncludingDisconnectedPeer.VirtualPlayer, -1));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
	}

	public override void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
	}

	public override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
	}

	public override void OnUdpNetworkHandlerTick(float dt)
	{
	}
}
