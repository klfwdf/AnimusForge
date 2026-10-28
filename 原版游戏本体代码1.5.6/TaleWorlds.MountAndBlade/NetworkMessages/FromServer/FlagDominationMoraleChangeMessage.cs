using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class FlagDominationMoraleChangeMessage : GameNetworkMessage
{
	public float Morale { get; private set; }

	public FlagDominationMoraleChangeMessage()
	{
	}

	public FlagDominationMoraleChangeMessage(float morale)
	{
		Morale = morale;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteFloatToPacket(Morale, CompressionMission.FlagDominationMoraleCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Morale = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.FlagDominationMoraleCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Morale synched: " + Morale;
	}
}
