using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectFrameOverTime : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public float Duration { get; private set; }

	public SetMissionObjectFrameOverTime(MissionObjectId missionObjectId, ref MatrixFrame frame, float duration)
	{
		MissionObjectId = missionObjectId;
		Frame = frame;
		Duration = duration;
	}

	public SetMissionObjectFrameOverTime()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
		Duration = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.FlagCapturePointDurationCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteMatrixFrameToPacket(Frame);
		GameNetworkMessage.WriteFloatToPacket(Duration, CompressionMission.FlagCapturePointDurationCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set Move-to-frame on MissionObject with ID: ", MissionObjectId, " over a period of ", Duration, " seconds.");
	}
}
