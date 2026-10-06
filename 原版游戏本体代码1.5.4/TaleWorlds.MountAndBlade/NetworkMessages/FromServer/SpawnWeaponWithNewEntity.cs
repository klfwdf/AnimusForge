using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SpawnWeaponWithNewEntity : GameNetworkMessage
{
	public MissionWeapon Weapon { get; private set; }

	public Mission.WeaponSpawnFlags WeaponSpawnFlags { get; private set; }

	public int ForcedIndex { get; private set; }

	public MatrixFrame Frame { get; private set; }

	public MissionObjectId ParentMissionObjectId { get; private set; }

	public bool IsVisible { get; private set; }

	public bool HasLifeTime { get; private set; }

	public bool SpawnedOnACorpse { get; private set; }

	public SpawnWeaponWithNewEntity(MissionWeapon weapon, Mission.WeaponSpawnFlags weaponSpawnFlags, int forcedIndex, MatrixFrame frame, MissionObjectId parentMissionObjectId, bool isVisible, bool hasLifeTime, bool spawnedOnACorpse)
	{
		Weapon = weapon;
		WeaponSpawnFlags = weaponSpawnFlags;
		ForcedIndex = forcedIndex;
		Frame = frame;
		ParentMissionObjectId = parentMissionObjectId;
		IsVisible = isVisible;
		HasLifeTime = hasLifeTime;
		SpawnedOnACorpse = spawnedOnACorpse;
	}

	public SpawnWeaponWithNewEntity()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Weapon = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		Frame = GameNetworkMessage.ReadMatrixFrameFromPacket(ref bufferReadValid);
		WeaponSpawnFlags = (Mission.WeaponSpawnFlags)GameNetworkMessage.ReadUintFromPacket(CompressionMission.SpawnedItemWeaponSpawnFlagCompressionInfo, ref bufferReadValid);
		ForcedIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		ParentMissionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid);
		IsVisible = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		HasLifeTime = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		SpawnedOnACorpse = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		ModuleNetworkData.WriteWeaponReferenceToPacket(Weapon);
		GameNetworkMessage.WriteMatrixFrameToPacket(Frame);
		GameNetworkMessage.WriteUintToPacket((uint)WeaponSpawnFlags, CompressionMission.SpawnedItemWeaponSpawnFlagCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(ForcedIndex, CompressionBasic.MissionObjectIDCompressionInfo);
		GameNetworkMessage.WriteMissionObjectIdToPacket((ParentMissionObjectId.Id >= 0) ? ParentMissionObjectId : MissionObjectId.Invalid);
		GameNetworkMessage.WriteBoolToPacket(IsVisible);
		GameNetworkMessage.WriteBoolToPacket(HasLifeTime);
		GameNetworkMessage.WriteBoolToPacket(SpawnedOnACorpse);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Spawn Weapon with name: ", Weapon.Item.Name, ", and with ID: ", ForcedIndex);
	}
}
