using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SyncObjectHitpoints : GameNetworkMessage
{
	public MissionObjectId MissionObjectId { get; private set; }

	public float Hitpoints { get; private set; }

	public SyncObjectHitpoints(MissionObjectId missionObjectId, float hitpoints)
	{
		MissionObjectId = missionObjectId;
		Hitpoints = hitpoints;
	}

	public SyncObjectHitpoints()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		MissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		Hitpoints = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.UsableGameObjectHealthCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObjectId);
		GameNetworkMessage.WriteFloatToPacket(MathF.Max(Hitpoints, 0f), CompressionMission.UsableGameObjectHealthCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
	}

	protected override string OnGetLogFormat()
	{
		return "Synchronize HitPoints: " + Hitpoints + " of MissionObject with Id: " + MissionObjectId;
	}
}
