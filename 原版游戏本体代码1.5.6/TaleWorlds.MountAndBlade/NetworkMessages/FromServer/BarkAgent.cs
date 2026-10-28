using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class BarkAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int IndexOfBark { get; private set; }

	public BarkAgent(int agent, int indexOfBark)
	{
		AgentIndex = agent;
		IndexOfBark = indexOfBark;
	}

	public BarkAgent()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		IndexOfBark = GameNetworkMessage.ReadIntFromPacket(CompressionMission.BarkIndexCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket(IndexOfBark, CompressionMission.BarkIndexCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.None;
	}

	protected override string OnGetLogFormat()
	{
		return "FromServer.BarkAgent agent-index: " + AgentIndex + ", IndexOfBark" + IndexOfBark;
	}
}
