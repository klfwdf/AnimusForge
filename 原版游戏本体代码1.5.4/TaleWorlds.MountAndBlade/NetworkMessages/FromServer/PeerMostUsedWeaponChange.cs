using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PeerMostUsedWeaponChange : GameNetworkMessage
{
	public NetworkCommunicator Peer { get; private set; }

	public WeaponClass WeaponClass { get; private set; }

	public PeerMostUsedWeaponChange(NetworkCommunicator peer, WeaponClass weaponClass)
	{
		Peer = peer;
		WeaponClass = weaponClass;
	}

	public PeerMostUsedWeaponChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		WeaponClass = (WeaponClass)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponClassCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		GameNetworkMessage.WriteIntToPacket((int)WeaponClass, CompressionMission.WeaponClassCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Peer most-used weapon change for peer: " + (Peer?.UserName ?? "NULL") + " weapon class: " + WeaponClass;
	}
}
