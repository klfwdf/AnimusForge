using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class FormationWipedMessage : GameNetworkMessage
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
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "FormationWipedMessage";
	}
}
