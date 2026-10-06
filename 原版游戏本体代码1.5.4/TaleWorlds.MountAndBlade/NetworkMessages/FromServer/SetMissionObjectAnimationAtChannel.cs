using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectAnimationAtChannel : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public int ChannelNo { get; private set; }

	public int AnimationIndex { get; private set; }

	public float AnimationSpeed { get; private set; }

	public SetMissionObjectAnimationAtChannel(MissionObjectId missionObjectId, int channelNo, int animationIndex, float animationSpeed)
	{
		MissionObjectId = missionObjectId;
		ChannelNo = channelNo;
		AnimationIndex = animationIndex;
		AnimationSpeed = animationSpeed;
	}

	public SetMissionObjectAnimationAtChannel()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		ChannelNo = (GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid) ? 1 : 0);
		AnimationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.AnimationIndexCompressionInfo, ref bufferReadValid);
		bool flag = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		AnimationSpeed = (flag ? GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationSpeedCompressionInfo, ref bufferReadValid) : 1f);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(ChannelNo == 1);
		GameNetworkMessage.WriteIntToPacket(AnimationIndex, CompressionBasic.AnimationIndexCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(AnimationSpeed != 1f);
		if (AnimationSpeed != 1f)
		{
			GameNetworkMessage.WriteFloatToPacket(AnimationSpeed, CompressionBasic.AnimationSpeedCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set animation: " + AnimationIndex + " on channel: " + ChannelNo + " of MissionObject with ID: " + MissionObjectId;
	}
}
