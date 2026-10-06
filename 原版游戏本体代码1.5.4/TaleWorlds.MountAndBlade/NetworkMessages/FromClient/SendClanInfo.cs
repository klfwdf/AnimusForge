using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SendClanInfo : GameNetworkMessage
{
	public string ClanName { get; private set; }

	public SendClanInfo(string clanName)
	{
		ClanName = clanName ?? string.Empty;
	}

	public SendClanInfo()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ClanName = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(ClanName);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Peers;
	}

	protected override string OnGetLogFormat()
	{
		return "Client sent clan info: " + ClanName;
	}
}
