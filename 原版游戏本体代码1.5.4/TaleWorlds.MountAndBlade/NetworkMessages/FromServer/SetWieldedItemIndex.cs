using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetWieldedItemIndex : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public bool IsLeftHand { get; private set; }

	public bool IsWieldedInstantly { get; private set; }

	public bool IsWieldedOnSpawn { get; private set; }

	public EquipmentIndex WieldedItemIndex { get; private set; }

	public int MainHandCurrentUsageIndex { get; private set; }

	public SetWieldedItemIndex(int agentIndex, bool isLeftHand, bool isWieldedInstantly, bool isWieldedOnSpawn, EquipmentIndex wieldedItemIndex, int mainHandCurUsageIndex)
	{
		AgentIndex = agentIndex;
		IsLeftHand = isLeftHand;
		IsWieldedInstantly = isWieldedInstantly;
		IsWieldedOnSpawn = isWieldedOnSpawn;
		WieldedItemIndex = wieldedItemIndex;
		MainHandCurrentUsageIndex = mainHandCurUsageIndex;
	}

	public SetWieldedItemIndex()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		IsLeftHand = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsWieldedInstantly = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsWieldedOnSpawn = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		WieldedItemIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.WieldSlotCompressionInfo, ref bufferReadValid);
		MainHandCurrentUsageIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponUsageIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteBoolToPacket(IsLeftHand);
		GameNetworkMessage.WriteBoolToPacket(IsWieldedInstantly);
		GameNetworkMessage.WriteBoolToPacket(IsWieldedOnSpawn);
		GameNetworkMessage.WriteIntToPacket((int)WieldedItemIndex, CompressionMission.WieldSlotCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(MainHandCurrentUsageIndex, CompressionMission.WeaponUsageIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.EquipmentDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Wielded Item Index to: ", WieldedItemIndex, " on Agent with agent-index: ", AgentIndex);
	}
}
