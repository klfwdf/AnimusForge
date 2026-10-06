using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class RequestUseObject : GameNetworkMessage
{
	public MissionObjectId UsableMissionObjectId { get; private set; }

	public int UsedObjectPreferenceIndex { get; private set; }

	public RequestUseObject(MissionObjectId usableMissionObjectId, int usedObjectPreferenceIndex)
	{
		UsableMissionObjectId = usableMissionObjectId;
		UsedObjectPreferenceIndex = usedObjectPreferenceIndex;
	}

	public RequestUseObject()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		UsableMissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		UsedObjectPreferenceIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.WieldSlotCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(UsableMissionObjectId);
		GameNetworkMessage.WriteIntToPacket(UsedObjectPreferenceIndex, CompressionMission.WieldSlotCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Request to use UsableMissionObject with ID: " + UsableMissionObjectId;
	}
}
