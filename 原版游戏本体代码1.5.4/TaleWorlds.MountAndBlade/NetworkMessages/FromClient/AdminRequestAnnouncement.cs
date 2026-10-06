using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class AdminRequestAnnouncement : GameNetworkMessage
{
	public string Message { get; private set; }

	public bool IsAdminBroadcast { get; private set; }

	public AdminRequestAnnouncement(string message, bool isAdminBroadcast)
	{
		Message = message;
		IsAdminBroadcast = isAdminBroadcast;
	}

	public AdminRequestAnnouncement()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		IsAdminBroadcast = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(Message);
		GameNetworkMessage.WriteBoolToPacket(IsAdminBroadcast);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Messaging;
	}

	protected override string OnGetLogFormat()
	{
		return "AdminRequestAnnouncement: " + Message + " " + IsAdminBroadcast;
	}
}
