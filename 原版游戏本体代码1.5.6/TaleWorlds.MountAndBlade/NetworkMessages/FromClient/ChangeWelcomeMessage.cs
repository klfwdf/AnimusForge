using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class ChangeWelcomeMessage : GameNetworkMessage
{
	public string NewWelcomeMessage { get; private set; }

	public ChangeWelcomeMessage(string newWelcomeMessage)
	{
		NewWelcomeMessage = newWelcomeMessage;
	}

	public ChangeWelcomeMessage()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		NewWelcomeMessage = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(NewWelcomeMessage);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Requested to change the welcome message to: " + NewWelcomeMessage;
	}
}
