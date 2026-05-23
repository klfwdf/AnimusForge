using System;
using NetworkMessages.FromServer;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public static class PeerExtensions
{
	public static void SendExistingObjects(this NetworkCommunicator peer, Mission mission)
	{
		MBAPI.IMBPeer.SendExistingObjects(peer.Index, mission.Pointer);
	}

	public static VirtualPlayer GetPeer(this PeerComponent peerComponent)
	{
		return peerComponent.Peer;
	}

	public static NetworkCommunicator GetNetworkPeer(this PeerComponent peerComponent)
	{
		return peerComponent.Peer.Communicator as NetworkCommunicator;
	}

	public static T GetComponent<T>(this NetworkCommunicator networkPeer) where T : PeerComponent
	{
		return networkPeer.VirtualPlayer.GetComponent<T>();
	}

	public static void RemoveComponent<T>(this NetworkCommunicator networkPeer, bool synched = true) where T : PeerComponent
	{
		networkPeer.VirtualPlayer.RemoveComponent<T>();
	}

	public static void RemoveComponent(this NetworkCommunicator networkPeer, PeerComponent component)
	{
		networkPeer.VirtualPlayer.RemoveComponent(component);
	}

	public static PeerComponent GetComponent(this NetworkCommunicator networkPeer, uint componentId)
	{
		return networkPeer.VirtualPlayer.GetComponent(componentId);
	}

	public static void AddComponent(this NetworkCommunicator networkPeer, Type peerComponentType)
	{
		networkPeer.VirtualPlayer.AddComponent(peerComponentType);
	}

	public static void AddComponent(this NetworkCommunicator networkPeer, uint componentId)
	{
		networkPeer.VirtualPlayer.AddComponent(componentId);
	}

	public static T AddComponent<T>(this NetworkCommunicator networkPeer) where T : PeerComponent, new()
	{
		if (networkPeer.GetComponent<T>() != null)
		{
			return networkPeer.TellClientToAddComponent<T>();
		}
		return networkPeer.VirtualPlayer.AddComponent<T>();
	}

	public static T TellClientToAddComponent<T>(this NetworkCommunicator networkPeer) where T : PeerComponent, new()
	{
		T component = networkPeer.GetComponent<T>();
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new AddPeerComponent(networkPeer, component.TypeId));
		GameNetwork.EndModuleEventAsServer();
		return component;
	}
}
