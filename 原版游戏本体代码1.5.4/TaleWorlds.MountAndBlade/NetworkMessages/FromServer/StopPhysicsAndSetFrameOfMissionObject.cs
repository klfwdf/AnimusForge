using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class StopPhysicsAndSetFrameOfMissionObject : GameNetworkMessage
{
	public MissionObjectId ObjectId { get; private set; }

	public MissionObjectId ParentId { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public StopPhysicsAndSetFrameOfMissionObject(MissionObjectId objectId, MissionObjectId parentId, MatrixFrame frame)
	{
		ObjectId = objectId;
		ParentId = parentId;
		Frame = frame;
	}

	public StopPhysicsAndSetFrameOfMissionObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		ObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		ParentId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Frame = GameNetworkMessage.ReadNonUniformTransformFromPacket(CompressionBasic.PositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(ObjectId);
		GameNetworkMessage.WriteMissionObjectIdToPacket((ParentId.Id >= 0) ? ParentId : MissionObjectId.Invalid);
		GameNetworkMessage.WriteNonUniformTransformToPacket(Frame, CompressionBasic.PositionCompressionInfo, CompressionBasic.LowResQuaternionCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Stop physics and set frame of MissionObject with ID: ", ObjectId, " Parent Index: ", ParentId);
	}
}
