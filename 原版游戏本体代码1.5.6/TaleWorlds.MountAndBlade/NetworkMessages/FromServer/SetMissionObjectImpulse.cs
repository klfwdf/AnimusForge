using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectImpulse : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec3 Impulse { get; private set; }

	public SetMissionObjectImpulse(MissionObjectId missionObjectId, Vec3 position, Vec3 impulse)
	{
		MissionObjectId = missionObjectId;
		Position = position;
		Impulse = impulse;
	}

	public SetMissionObjectImpulse()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.LocalPositionCompressionInfo, ref bufferReadValid);
		Impulse = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.ImpulseCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.LocalPositionCompressionInfo);
		GameNetworkMessage.WriteVec3ToPacket(Impulse, CompressionBasic.ImpulseCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return "Set impulse on MissionObject with ID: " + MissionObjectId;
	}
}
