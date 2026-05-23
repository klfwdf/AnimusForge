using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentPrefabComponentVisibility : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int ComponentIndex { get; private set; }

	public bool Visibility { get; private set; }

	public SetAgentPrefabComponentVisibility(int agentIndex, int componentIndex, bool visibility)
	{
		AgentIndex = agentIndex;
		ComponentIndex = componentIndex;
		Visibility = visibility;
	}

	public SetAgentPrefabComponentVisibility()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		ComponentIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentPrefabComponentIndexCompressionInfo, ref bufferReadValid);
		Visibility = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket(ComponentIndex, CompressionMission.AgentPrefabComponentIndexCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(Visibility);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Component with index: " + ComponentIndex + " to be " + (Visibility ? "visible" : "invisible") + " on Agent with agent-index: " + AgentIndex;
	}
}
