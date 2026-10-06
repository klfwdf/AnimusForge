using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SelectAllFormations : GameNetworkMessage
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
		return MultiplayerMessageFilter.Formations;
	}

	protected override string OnGetLogFormat()
	{
		return "Select all formations";
	}
}
