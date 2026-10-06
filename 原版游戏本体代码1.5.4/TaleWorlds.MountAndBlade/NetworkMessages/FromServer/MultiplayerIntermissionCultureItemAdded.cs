using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionCultureItemAdded : GameNetworkMessage
{
	public string CultureId { get; private set; }

	public MultiplayerIntermissionCultureItemAdded()
	{
	}

	public MultiplayerIntermissionCultureItemAdded(string cultureId)
	{
		CultureId = cultureId;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		CultureId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(CultureId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Adding culture for voting with id: " + CultureId + ".";
	}
}
