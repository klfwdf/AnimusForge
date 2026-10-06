using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SynchronizeMissionObject : GameNetworkMessage
{
	private SynchedMissionObject _synchedMissionObject;

	public MissionObjectId MissionObjectId { get; private set; }

	public int RecordTypeIndex { get; private set; }

	public (BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) RecordPair { get; private set; }

	public SynchronizeMissionObject(SynchedMissionObject synchedMissionObject)
	{
		_synchedMissionObject = synchedMissionObject;
		MissionObjectId = synchedMissionObject.Id;
		RecordTypeIndex = GameNetwork.GetSynchedMissionObjectReadableRecordIndexFromType(synchedMissionObject.GetType());
	}

	public SynchronizeMissionObject()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteIntToPacket(RecordTypeIndex, CompressionMission.SynchedMissionObjectReadableRecordTypeIndex);
		_synchedMissionObject.WriteToNetwork();
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		RecordTypeIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SynchedMissionObjectReadableRecordTypeIndex, ref bufferReadValid);
		RecordPair = BaseSynchedMissionObjectReadableRecord.CreateFromNetworkWithTypeIndex(RecordTypeIndex);
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Synchronize MissionObject with Id: " + MissionObjectId;
	}
}
