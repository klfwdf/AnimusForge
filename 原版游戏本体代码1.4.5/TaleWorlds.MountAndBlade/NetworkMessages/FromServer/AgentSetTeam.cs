using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AgentSetTeam : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int TeamIndex { get; private set; }

	public AgentSetTeam(int agentIndex, int teamIndex)
	{
		AgentIndex = agentIndex;
		TeamIndex = teamIndex;
	}

	public AgentSetTeam()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Assign agent with agent-index: " + AgentIndex + " to team: " + TeamIndex;
	}
}
