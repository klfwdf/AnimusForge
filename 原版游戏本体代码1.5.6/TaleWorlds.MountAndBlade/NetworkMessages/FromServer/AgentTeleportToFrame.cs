using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AgentTeleportToFrame : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec2 Direction { get; private set; }

	public AgentTeleportToFrame(int agentIndex, Vec3 position, Vec2 direction)
	{
		AgentIndex = agentIndex;
		Position = position;
		Direction = direction.Normalized();
	}

	public AgentTeleportToFrame()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec2ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Teleporting agent with agent-index: ", AgentIndex, " to frame with position: ", Position, " and direction: ", Direction);
	}
}
