using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ExistingObjectsEnd : GameNetworkMessage
{
	protected override bool OnRead()
	{
		return true;
	}

	protected override void OnWrite()
	{
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.General;
	}

	protected override string OnGetLogFormat()
	{
		return "Finished receiving existing objects";
	}
}
