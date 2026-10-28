using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectAnimationChannelParameter : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public int ChannelNo { get; private set; }

	public float Parameter { get; private set; }

	public SetMissionObjectAnimationChannelParameter(MissionObjectId missionObjectId, int channelNo, float parameter)
	{
		MissionObjectId = missionObjectId;
		ChannelNo = channelNo;
		Parameter = parameter;
	}

	public SetMissionObjectAnimationChannelParameter()
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
		Parameter = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.AnimationProgressCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(ChannelNo == 1);
		GameNetworkMessage.WriteFloatToPacket(Parameter, CompressionBasic.AnimationProgressCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set animation parameter: " + Parameter + " on channel: " + ChannelNo + " of MissionObject with ID: " + MissionObjectId;
	}
}
