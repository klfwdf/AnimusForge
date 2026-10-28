using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AddPrefabComponentToAgentBone : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public string PrefabName { get; private set; }

	public sbyte BoneIndex { get; private set; }

	public AddPrefabComponentToAgentBone(int agentIndex, string prefabName, sbyte boneIndex)
	{
		AgentIndex = agentIndex;
		PrefabName = prefabName;
		BoneIndex = boneIndex;
	}

	public AddPrefabComponentToAgentBone()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		PrefabName = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		BoneIndex = (sbyte)GameNetworkMessage.ReadIntFromPacket(CompressionMission.BoneIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteStringToPacket(PrefabName);
		GameNetworkMessage.WriteIntToPacket(BoneIndex, CompressionMission.BoneIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Add prefab component: " + PrefabName + " on bone with index: " + BoneIndex + " on agent with agent-index: " + AgentIndex;
	}
}
