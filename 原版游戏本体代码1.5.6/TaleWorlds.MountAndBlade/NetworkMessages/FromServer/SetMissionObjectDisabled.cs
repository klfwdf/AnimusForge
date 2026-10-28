using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SetMissionObjectDisabled : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public SetMissionObjectDisabled(MissionObjectId missionObjectId)
	{
		MissionObjectId = missionObjectId;
	}

	public SetMissionObjectDisabled()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjects;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Mission Object with ID: ", MissionObjectId, " has been disabled.");
	}
}
