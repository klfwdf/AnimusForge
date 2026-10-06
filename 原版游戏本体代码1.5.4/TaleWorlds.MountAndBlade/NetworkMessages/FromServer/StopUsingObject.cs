using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class StopUsingObject : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public bool IsSuccessful { get; private set; }

	public StopUsingObject(int agentIndex, bool isSuccessful)
	{
		AgentIndex = agentIndex;
		IsSuccessful = isSuccessful;
	}

	public StopUsingObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		IsSuccessful = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteBoolToPacket(IsSuccessful);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed | MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Stop using Object on Agent with agent-index: " + AgentIndex;
	}
}
