using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SpawnAttachedWeaponOnCorpse : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int AttachedIndex { get; private set; }

	public int ForcedIndex { get; private set; }

	public SpawnAttachedWeaponOnCorpse(int agentIndex, int attachedIndex, int forcedIndex)
	{
		AgentIndex = agentIndex;
		AttachedIndex = attachedIndex;
		ForcedIndex = forcedIndex;
	}

	public SpawnAttachedWeaponOnCorpse()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		AttachedIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponAttachmentIndexCompressionInfo, ref bufferReadValid);
		ForcedIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket(AttachedIndex, CompressionMission.WeaponAttachmentIndexCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ForcedIndex, CompressionBasic.MissionObjectIDCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		return "SpawnAttachedWeaponOnCorpse with agent-index: " + AgentIndex + ", and with ID: " + ForcedIndex;
	}
}
