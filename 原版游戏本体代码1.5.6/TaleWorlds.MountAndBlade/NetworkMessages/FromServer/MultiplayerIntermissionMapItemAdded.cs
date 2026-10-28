using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class MultiplayerIntermissionMapItemAdded : GameNetworkMessage
{
	public string MapId { get; private set; }

	public MultiplayerIntermissionMapItemAdded()
	{
	}

	public MultiplayerIntermissionMapItemAdded(string mapId)
	{
		MapId = mapId;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MapId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(MapId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Adding map for voting with id: " + MapId + ".";
	}
}
