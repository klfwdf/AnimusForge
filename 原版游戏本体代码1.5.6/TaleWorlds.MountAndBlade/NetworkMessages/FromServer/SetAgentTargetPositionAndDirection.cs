using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentTargetPositionAndDirection : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public Vec2 Position { get; private set; }

	public Vec3 Direction { get; private set; }

	public SetAgentTargetPositionAndDirection(int agentIndex, ref Vec2 position, ref Vec3 direction)
	{
		AgentIndex = agentIndex;
		Position = position;
		Direction = direction;
	}

	public SetAgentTargetPositionAndDirection()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteVec2ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set TargetPositionAndDirection: ", Position, " ", Direction, " on Agent with agent-index: ", AgentIndex);
	}
}
