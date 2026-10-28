using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class FlagRaisingStatus : GameNetworkMessage
{
	public float Progress { get; private set; }

	public CaptureTheFlagFlagDirection Direction { get; private set; }

	public float Speed { get; private set; }

	public FlagRaisingStatus()
	{
	}

	public FlagRaisingStatus(float currProgress, CaptureTheFlagFlagDirection direction, float speed)
	{
		Progress = currProgress;
		Direction = direction;
		Speed = speed;
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Progress = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.FlagClassicProgressCompressionInfo, ref bufferReadValid);
		Direction = (CaptureTheFlagFlagDirection)GameNetworkMessage.ReadIntFromPacket(CompressionMission.FlagDirectionEnumCompressionInfo, ref bufferReadValid);
		if (bufferReadValid && Direction != CaptureTheFlagFlagDirection.None && Direction != CaptureTheFlagFlagDirection.Static)
		{
			Speed = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.FlagSpeedCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteFloatToPacket(Progress, CompressionMission.FlagClassicProgressCompressionInfo);
		GameNetworkMessage.WriteIntToPacket((int)Direction, CompressionMission.FlagDirectionEnumCompressionInfo);
		if (Direction != CaptureTheFlagFlagDirection.None && Direction != CaptureTheFlagFlagDirection.Static)
		{
			GameNetworkMessage.WriteFloatToPacket(Speed, CompressionMission.FlagSpeedCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Updating flag movement: Progress: ", Progress, ", Direction: ", Direction, ", Speed: ", Speed);
	}
}
