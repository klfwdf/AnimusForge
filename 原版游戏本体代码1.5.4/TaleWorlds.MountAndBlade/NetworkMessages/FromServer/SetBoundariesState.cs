using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetBoundariesState : GameNetworkMessage
{
	public bool IsOutside { get; private set; }

	public float StateStartTimeInSeconds { get; private set; }

	public SetBoundariesState()
	{
	}

	public SetBoundariesState(bool isOutside)
	{
		IsOutside = isOutside;
	}

	public SetBoundariesState(bool isOutside, long stateStartTimeInTicks)
		: this(isOutside)
	{
		StateStartTimeInSeconds = (float)stateStartTimeInTicks / 10000000f;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(IsOutside);
		if (IsOutside)
		{
			GameNetworkMessage.WriteFloatToPacket(StateStartTimeInSeconds, CompressionMatchmaker.MissionTimeCompressionInfo);
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		IsOutside = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (IsOutside)
		{
			StateStartTimeInSeconds = GameNetworkMessage.ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.AgentsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		if (!IsOutside)
		{
			return "I am now inside the level boundaries";
		}
		return "I am now outside of the level boundaries";
	}
}
