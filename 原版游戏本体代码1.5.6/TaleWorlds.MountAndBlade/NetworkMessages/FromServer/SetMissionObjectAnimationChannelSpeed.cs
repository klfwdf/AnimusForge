using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectAnimationChannelSpeed : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public int ChannelNo { get; private set; }

	public float Speed { get; private set; }

	public SetMissionObjectAnimationChannelSpeed(MissionObjectId missionObjectId, int channelNo, float speed)
	{
		MissionObjectId = missionObjectId;
		ChannelNo = channelNo;
		Speed = speed;
	}

	public SetMissionObjectAnimationChannelSpeed()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		bool flag = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (bufferReadValid)
		{
			ChannelNo = (flag ? 1 : 0);
		}
		Speed = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationSpeedCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(ChannelNo == 1);
		GameNetworkMessage.WriteFloatToPacket(Speed, CompressionBasic.AnimationSpeedCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set animation speed: " + Speed + " on channel: " + ChannelNo + " of MissionObject with ID: " + MissionObjectId;
	}
}
