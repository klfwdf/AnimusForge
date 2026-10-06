using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RoundWinnerChange : GameNetworkMessage
{
	public BattleSideEnum RoundWinner { get; private set; }

	public RoundWinnerChange(BattleSideEnum roundWinner)
	{
		RoundWinner = roundWinner;
	}

	public RoundWinnerChange()
	{
		RoundWinner = BattleSideEnum.None;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		RoundWinner = (BattleSideEnum)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)RoundWinner, CompressionMission.TeamSideCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Change round winner to: " + RoundWinner;
	}
}
