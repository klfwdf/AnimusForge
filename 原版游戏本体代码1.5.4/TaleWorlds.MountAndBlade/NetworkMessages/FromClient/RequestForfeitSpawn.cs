using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestForfeitSpawn : GameNetworkMessage
{
	protected override void OnWrite()
	{
	}

	protected override bool OnRead()
	{
		return true;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Someone has requested to forfeit spawning.";
	}
}
