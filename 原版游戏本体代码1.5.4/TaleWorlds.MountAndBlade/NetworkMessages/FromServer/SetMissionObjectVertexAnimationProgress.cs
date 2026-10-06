using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectVertexAnimationProgress : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public float Progress { get; private set; }

	public SetMissionObjectVertexAnimationProgress(MissionObjectId missionObjectId, float progress)
	{
		MissionObjectId = missionObjectId;
		Progress = progress;
	}

	public SetMissionObjectVertexAnimationProgress()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Progress = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationProgressCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteFloatToPacket(Progress, CompressionBasic.AnimationProgressCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Set progress of Vertex Animation on MissionObject with ID: ", MissionObjectId, " to: ", Progress);
	}
}
