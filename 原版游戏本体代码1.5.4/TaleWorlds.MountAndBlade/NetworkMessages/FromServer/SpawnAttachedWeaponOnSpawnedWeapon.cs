using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SpawnAttachedWeaponOnSpawnedWeapon : GameNetworkMessage
{
	public MissionObjectId SpawnedWeaponId { get; private set; }

	public int AttachmentIndex { get; private set; }

	public int ForcedIndex { get; private set; }

	public SpawnAttachedWeaponOnSpawnedWeapon(MissionObjectId spawnedWeaponId, int attachmentIndex, int forcedIndex)
	{
		SpawnedWeaponId = spawnedWeaponId;
		AttachmentIndex = attachmentIndex;
		ForcedIndex = forcedIndex;
	}

	public SpawnAttachedWeaponOnSpawnedWeapon()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		SpawnedWeaponId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		AttachmentIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.WeaponAttachmentIndexCompressionInfo, ref bufferReadValid);
		ForcedIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteMissionObjectIdToPacket(SpawnedWeaponId);
		GameNetworkMessage.WriteIntToPacket(AttachmentIndex, CompressionMission.WeaponAttachmentIndexCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ForcedIndex, CompressionBasic.MissionObjectIDCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("SpawnAttachedWeaponOnSpawnedWeapon with Spawned Weapon ID: ", SpawnedWeaponId, " AttachmentIndex: ", AttachmentIndex, " Attached Weapon ID: ", ForcedIndex);
	}
}
