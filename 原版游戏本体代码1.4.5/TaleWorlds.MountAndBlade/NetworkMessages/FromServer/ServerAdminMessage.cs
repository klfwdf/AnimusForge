using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ServerAdminMessage : GameNetworkMessage
{
	public string Message { get; private set; }

	public bool IsAdminBroadcast { get; private set; }

	public ServerAdminMessage(string message, bool isAdminBroadcast)
	{
		Message = message;
		IsAdminBroadcast = isAdminBroadcast;
	}

	public ServerAdminMessage()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(Message);
		GameNetworkMessage.WriteBoolToPacket(IsAdminBroadcast);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		IsAdminBroadcast = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Messaging;
	}

	protected override string OnGetLogFormat()
	{
		return "Admin message from server: " + Message;
	}
}
