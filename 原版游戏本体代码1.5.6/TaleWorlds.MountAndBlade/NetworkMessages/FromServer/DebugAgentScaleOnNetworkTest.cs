using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.DebugFromServer)]
internal sealed class DebugAgentScaleOnNetworkTest : GameNetworkMessage
{
	internal int AgentToTestIndex { get; private set; }

	internal float ScaleToTest { get; private set; }

	public DebugAgentScaleOnNetworkTest()
	{
	}

	internal DebugAgentScaleOnNetworkTest(int agentToTestIndex, float scale)
	{
		AgentToTestIndex = agentToTestIndex;
		ScaleToTest = scale;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentToTestIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		ScaleToTest = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.DebugScaleValueCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentToTestIndex);
		GameNetworkMessage.WriteFloatToPacket(ScaleToTest, CompressionMission.DebugScaleValueCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "DebugAgentScaleOnNetworkTest";
	}
}
