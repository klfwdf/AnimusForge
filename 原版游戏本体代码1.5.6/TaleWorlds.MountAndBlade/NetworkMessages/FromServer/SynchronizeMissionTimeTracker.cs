using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SynchronizeMissionTimeTracker : GameNetworkMessage
{
	public float CurrentTime { get; private set; }

	public SynchronizeMissionTimeTracker(float currentTime)
	{
		CurrentTime = currentTime;
	}

	public SynchronizeMissionTimeTracker()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		CurrentTime = GameNetworkMessage.ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteFloatToPacket(CurrentTime, CompressionMatchmaker.MissionTimeCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return CurrentTime + " seconds have elapsed since the start of the mission.";
	}
}
