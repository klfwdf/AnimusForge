using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class PlayerMessageAll : GameNetworkMessage
{
	public NetworkCommunicator Player { get; private set; }

	public string Message { get; private set; }

	public PlayerMessageAll(NetworkCommunicator player, string message)
	{
		Player = player;
		Message = message;
	}

	public PlayerMessageAll()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
		GameNetworkMessage.WriteStringToPacket(Message);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Messaging;
	}

	protected override string OnGetLogFormat()
	{
		return "Receiving Player message to all: " + Message;
	}
}
