using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SpawnWeaponAsDropFromAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public EquipmentIndex EquipmentIndex { get; private set; }

	public Vec3 Velocity { get; private set; }

	public Vec3 AngularVelocity { get; private set; }

	public Mission.WeaponSpawnFlags WeaponSpawnFlags { get; private set; }

	public int ForcedIndex { get; private set; }

	public SpawnWeaponAsDropFromAgent(int agentIndex, EquipmentIndex equipmentIndex, Vec3 velocity, Vec3 angularVelocity, Mission.WeaponSpawnFlags weaponSpawnFlags, int forcedIndex)
	{
		AgentIndex = agentIndex;
		EquipmentIndex = equipmentIndex;
		Velocity = velocity;
		AngularVelocity = angularVelocity;
		WeaponSpawnFlags = weaponSpawnFlags;
		ForcedIndex = forcedIndex;
	}

	public SpawnWeaponAsDropFromAgent()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		EquipmentIndex = (EquipmentIndex)GameNetworkMessage.ReadIntFromPacket(CompressionMission.ItemSlotCompressionInfo, ref bufferReadValid);
		WeaponSpawnFlags = (Mission.WeaponSpawnFlags)GameNetworkMessage.ReadUintFromPacket(CompressionMission.SpawnedItemWeaponSpawnFlagCompressionInfo, ref bufferReadValid);
		if (WeaponSpawnFlags.HasAnyFlag(Mission.WeaponSpawnFlags.WithPhysics))
		{
			Velocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemVelocityCompressionInfo, ref bufferReadValid);
			AngularVelocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemAngularVelocityCompressionInfo, ref bufferReadValid);
		}
		else
		{
			Velocity = Vec3.Zero;
			AngularVelocity = Vec3.Zero;
		}
		ForcedIndex = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteIntToPacket((int)EquipmentIndex, CompressionMission.ItemSlotCompressionInfo);
		GameNetworkMessage.WriteUintToPacket((uint)WeaponSpawnFlags, CompressionMission.SpawnedItemWeaponSpawnFlagCompressionInfo);
		if (WeaponSpawnFlags.HasAnyFlag(Mission.WeaponSpawnFlags.WithPhysics))
		{
			GameNetworkMessage.WriteVec3ToPacket(Velocity, CompressionMission.SpawnedItemVelocityCompressionInfo);
			GameNetworkMessage.WriteVec3ToPacket(AngularVelocity, CompressionMission.SpawnedItemAngularVelocityCompressionInfo);
		}
		GameNetworkMessage.WriteIntToPacket(ForcedIndex, CompressionBasic.MissionObjectIDCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Items;
	}

	protected override string OnGetLogFormat()
	{
		return string.Concat("Spawn Weapon from agent with agent-index: ", AgentIndex, " from equipment index: ", EquipmentIndex, ", and with ID: ", ForcedIndex);
	}
}
