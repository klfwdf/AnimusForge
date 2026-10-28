using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectVertexAnimation : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public int BeginKey { get; private set; }

	public int EndKey { get; private set; }

	public float Speed { get; private set; }

	public SetMissionObjectVertexAnimation(MissionObjectId missionObjectId, int beginKey, int endKey, float speed)
	{
		MissionObjectId = missionObjectId;
		BeginKey = beginKey;
		EndKey = endKey;
		Speed = speed;
	}

	public SetMissionObjectVertexAnimation()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		BeginKey = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationKeyCompressionInfo, ref bufferReadValid);
		EndKey = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationKeyCompressionInfo, ref bufferReadValid);
		Speed = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.VertexAnimationSpeedCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteIntToPacket(BeginKey, CompressionBasic.AnimationKeyCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(EndKey, CompressionBasic.AnimationKeyCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(Speed, CompressionBasic.VertexAnimationSpeedCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set Vertex Animation on MissionObject with ID: " + MissionObjectId;
	}
}
