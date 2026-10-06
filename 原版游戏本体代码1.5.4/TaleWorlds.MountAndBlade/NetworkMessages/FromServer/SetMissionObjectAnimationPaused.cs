using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectAnimationPaused : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public bool IsPaused { get; private set; }

	public SetMissionObjectAnimationPaused(MissionObjectId missionObjectId, bool isPaused)
	{
		MissionObjectId = missionObjectId;
		IsPaused = isPaused;
	}

	public SetMissionObjectAnimationPaused()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		IsPaused = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteBoolToPacket(IsPaused);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Set animation to be: " + (IsPaused ? "Paused" : "Not paused") + " on MissionObject with ID: " + MissionObjectId;
	}
}
