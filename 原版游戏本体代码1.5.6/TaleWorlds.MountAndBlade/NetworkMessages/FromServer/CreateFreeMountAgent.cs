using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateFreeMountAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentElement HorseItem { get; private set; }

	public EquipmentElement HorseHarnessItem { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec2 Direction { get; private set; }

	public CreateFreeMountAgent(Agent agent, Vec3 position, Vec2 direction)
	{
		AgentIndex = agent.Index;
		HorseItem = agent.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.ArmorItemEndSlot);
		HorseHarnessItem = agent.SpawnEquipment.GetEquipmentFromSlot(EquipmentIndex.HorseHarness);
		Position = position;
		Direction = direction.Normalized();
	}

	public CreateFreeMountAgent()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		HorseItem = ModuleNetworkData.ReadItemReferenceFromPacket(Game.Current.ObjectManager, ref bufferReadValid);
		HorseHarnessItem = ModuleNetworkData.ReadItemReferenceFromPacket(Game.Current.ObjectManager, ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		ModuleNetworkData.WriteItemReferenceToPacket(HorseItem);
		ModuleNetworkData.WriteItemReferenceToPacket(HorseHarnessItem);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec2ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Create a mount-agent with index: " + AgentIndex;
	}
}
