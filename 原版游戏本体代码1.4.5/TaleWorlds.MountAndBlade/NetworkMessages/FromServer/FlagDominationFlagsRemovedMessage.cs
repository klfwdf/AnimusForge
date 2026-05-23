using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class FlagDominationFlagsRemovedMessage : GameNetworkMessage
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
		return "Flags got removed.";
	}
}
