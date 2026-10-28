using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentTargetPosition : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public Vec2 Position { get; private set; }

	public SetAgentTargetPosition(int agentIndex, ref Vec2 position)
	{
		AgentIndex = agentIndex;
		Position = position;
	}

	public SetAgentTargetPosition()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteVec2ToPacket(Position, CompressionBasic.PositionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Target Position: ", Position, " on Agent with agent-index: ", AgentIndex);
	}
}
