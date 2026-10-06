using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectGlobalFrame : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public SetMissionObjectGlobalFrame(MissionObjectId missionObjectId, ref MatrixFrame frame)
	{
		MissionObjectId = missionObjectId;
		Frame = frame;
	}

	public SetMissionObjectGlobalFrame()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Vec3 s = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		Vec3 f = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		Vec3 u = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid);
		Vec3 scalingVector = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.ScaleCompressionInfo, ref bufferReadValid);
		Vec3 o = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		if (bufferReadValid)
		{
			Frame = new MatrixFrame(new Mat3(in s, in f, in u), in o);
			Frame.Scale(in scalingVector);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		Vec3 scaleVector = Frame.rotation.GetScaleVector();
		MatrixFrame frame = Frame;
		frame.Scale(new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z));
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteVec3ToPacket(frame.rotation.f, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(frame.rotation.s, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(frame.rotation.u, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(scaleVector, CompressionBasic.ScaleCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(frame.origin, CompressionBasic.PositionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Global Frame on MissionObject with ID: " + MissionObjectId;
	}
}
