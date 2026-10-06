using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectFrame : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public SetMissionObjectFrame(MissionObjectId missionObjectId, ref MatrixFrame frame)
	{
		MissionObjectId = missionObjectId;
		Frame = frame;
	}

	public SetMissionObjectFrame()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteMatrixFrameToPacket(Frame);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Frame on MissionObject with ID: " + MissionObjectId;
	}
}
