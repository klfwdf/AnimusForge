using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class PollResponse : GameNetworkMessage
{
	public bool Accepted { get; private set; }

	public PollResponse(bool accepted)
	{
		Accepted = accepted;
	}

	public PollResponse()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Accepted = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(Accepted);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Receiving poll response: " + (Accepted ? "Accepted." : "Not accepted.");
	}
}
