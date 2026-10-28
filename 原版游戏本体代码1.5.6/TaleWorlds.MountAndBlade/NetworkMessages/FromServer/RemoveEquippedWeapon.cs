using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class RemoveEquippedWeapon : GameNetworkMessage
{
	public EquipmentIndex SlotIndex { get; private set; }

	public int AgentIndex { get; private set; }

	public RemoveEquippedWeapon(int agentIndex, EquipmentIndex slot)
	{
		AgentIndex = agentIndex;
		SlotIndex = slot;
	}

	public RemoveEquippedWeapon()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)SlotIndex, CompressionMission.ItemSlotCompressionInfo);
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		SlotIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items | MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Remove equipped weapon from SlotIndex: ", SlotIndex, " on agent with agent-index: ", AgentIndex);
	}
}
