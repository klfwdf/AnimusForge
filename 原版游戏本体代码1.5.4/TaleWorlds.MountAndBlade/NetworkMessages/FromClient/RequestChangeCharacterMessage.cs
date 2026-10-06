using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestChangeCharacterMessage : GameNetworkMessage
{
	public NetworkCommunicator NetworkPeer { get; private set; }

	public RequestChangeCharacterMessage(NetworkCommunicator networkPeer)
	{
		NetworkPeer = networkPeer;
	}

	public RequestChangeCharacterMessage()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(NetworkPeer);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		NetworkPeer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return NetworkPeer.UserName + " has requested to change character.";
	}
}
