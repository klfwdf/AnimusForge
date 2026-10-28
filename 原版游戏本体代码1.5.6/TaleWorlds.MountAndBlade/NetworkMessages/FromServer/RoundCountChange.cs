using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RoundCountChange : GameNetworkMessage
{
	public int RoundCount { get; private set; }

	public RoundCountChange(int roundCount)
	{
		RoundCount = roundCount;
	}

	public RoundCountChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RoundCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MissionRoundCountCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(RoundCount, CompressionMission.MissionRoundCountCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Change round count to: " + RoundCount;
	}
}
