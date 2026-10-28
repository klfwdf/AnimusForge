using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class ServerPerformanceStateReplicationMessage : GameNetworkMessage
{
	internal ServerPerformanceState ServerPerformanceProblemState { get; private set; }

	public ServerPerformanceStateReplicationMessage()
	{
	}

	internal ServerPerformanceStateReplicationMessage(ServerPerformanceState serverPerformanceProblemState)
	{
		ServerPerformanceProblemState = serverPerformanceProblemState;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ServerPerformanceProblemState = (ServerPerformanceState)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.ServerPerformanceStateCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket((int)ServerPerformanceProblemState, CompressionBasic.ServerPerformanceStateCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "ServerPerformanceStateReplicationMessage";
	}
}
