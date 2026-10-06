using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class UpdateRoundScores : GameNetworkMessage
{
	public int AttackerTeamScore { get; private set; }

	public int DefenderTeamScore { get; private set; }

	public UpdateRoundScores(int attackerTeamScore, int defenderTeamScore)
	{
		AttackerTeamScore = attackerTeamScore;
		DefenderTeamScore = defenderTeamScore;
	}

	public UpdateRoundScores()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AttackerTeamScore = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamScoreCompressionInfo, ref bufferReadValid);
		DefenderTeamScore = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamScoreCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(AttackerTeamScore, CompressionMission.TeamScoreCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(DefenderTeamScore, CompressionMission.TeamScoreCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission | MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Update round score. Attackers: " + AttackerTeamScore + ", defenders: " + DefenderTeamScore;
	}
}
