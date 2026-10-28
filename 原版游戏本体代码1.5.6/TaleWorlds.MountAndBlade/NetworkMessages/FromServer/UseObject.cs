using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class UseObject : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public MissionObjectId UsableGameObjectId { get; private set; }

	public UseObject(int agentIndex, MissionObjectId usableGameObjectId)
	{
		AgentIndex = agentIndex;
		UsableGameObjectId = usableGameObjectId;
	}

	public UseObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		UsableGameObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteMissionObjectIdToPacket((UsableGameObjectId.Id >= 0) ? UsableGameObjectId : MissionObjectId.Invalid);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents | MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		string text = "Use UsableMissionObject with ID: ";
		text = ((!(UsableGameObjectId != MissionObjectId.Invalid)) ? (text + "null") : (text + UsableGameObjectId));
		text += " by Agent with name: ";
		if (AgentIndex >= 0)
		{
			return text + "agent-index: " + AgentIndex;
		}
		return text + "null";
	}
}
