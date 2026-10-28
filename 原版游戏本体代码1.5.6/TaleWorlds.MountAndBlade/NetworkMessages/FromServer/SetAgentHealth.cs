using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetAgentHealth : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int Health { get; private set; }

	public SetAgentHealth(int agentIndex, int newHealth)
	{
		AgentIndex = agentIndex;
		Health = newHealth;
	}

	public SetAgentHealth()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Health = GameNetworkMessage.ReadIntFromPacket(CompressionMission.AgentHealthCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket(Health, CompressionMission.AgentHealthCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set agent health to: " + Health + ", for agent-index: " + AgentIndex;
	}
}
