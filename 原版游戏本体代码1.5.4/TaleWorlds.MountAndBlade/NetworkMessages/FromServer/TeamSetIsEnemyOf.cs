using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class TeamSetIsEnemyOf : GameNetworkMessage
{
	public int Team1Index { get; private set; }

	public int Team2Index { get; private set; }

	public bool IsEnemyOf { get; private set; }

	public TeamSetIsEnemyOf(int team1Index, int team2Index, bool isEnemyOf)
	{
		Team1Index = team1Index;
		Team2Index = team2Index;
		IsEnemyOf = isEnemyOf;
	}

	public TeamSetIsEnemyOf()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteTeamIndexToPacket(Team1Index);
		GameNetworkMessage.WriteTeamIndexToPacket(Team2Index);
		GameNetworkMessage.WriteBoolToPacket(IsEnemyOf);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Team1Index = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		Team2Index = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		IsEnemyOf = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return Team1Index + " is now " + (IsEnemyOf ? "" : "not an ") + "enemy of " + Team2Index;
	}
}
