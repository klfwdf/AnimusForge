using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentIsPlayer : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public bool IsPlayer { get; private set; }

	public SetAgentIsPlayer(int agentIndex, bool isPlayer)
	{
		AgentIndex = agentIndex;
		IsPlayer = isPlayer;
	}

	public SetAgentIsPlayer()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		IsPlayer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteBoolToPacket(IsPlayer);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Controller is player on Agent with agent-index: " + AgentIndex + (IsPlayer ? " - TRUE." : " - FALSE.");
	}
}
