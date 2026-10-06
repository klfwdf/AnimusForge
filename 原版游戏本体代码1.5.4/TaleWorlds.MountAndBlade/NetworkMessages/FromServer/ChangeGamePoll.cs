using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ChangeGamePoll : GameNetworkMessage
{
	public string GameType { get; private set; }

	public string Map { get; private set; }

	public ChangeGamePoll(string gameType, string map)
	{
		GameType = gameType;
		Map = map;
	}

	public ChangeGamePoll()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		GameType = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		Map = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(GameType);
		GameNetworkMessage.WriteStringToPacket(Map);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return "Poll started: Change Map to: " + Map + " and GameType to: " + GameType;
	}
}
