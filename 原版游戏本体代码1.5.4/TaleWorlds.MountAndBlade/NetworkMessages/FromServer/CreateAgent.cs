using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class CreateAgent : GameNetworkMessage
{
	public int AgentIndex { get; private set; }

	public int MountAgentIndex { get; private set; }

	public NetworkCommunicator Peer { get; private set; }

	public BasicCharacterObject Character { get; private set; }

	public Monster Monster { get; private set; }

	public MissionEquipment MissionEquipment { get; private set; }

	public Equipment SpawnEquipment { get; private set; }

	public BodyProperties BodyPropertiesValue { get; private set; }

	public int BodyPropertiesSeed { get; private set; }

	public bool IsFemale { get; private set; }

	public int TeamIndex { get; private set; }

	public Vec3 Position { get; private set; }

	public Vec2 Direction { get; private set; }

	public int FormationIndex { get; private set; }

	public bool IsPlayerAgent { get; private set; }

	public uint ClothingColor1 { get; private set; }

	public uint ClothingColor2 { get; private set; }

	public CreateAgent(int agentIndex, BasicCharacterObject character, Monster monster, Equipment spawnEquipment, MissionEquipment missionEquipment, BodyProperties bodyPropertiesValue, int bodyPropertiesSeed, bool isFemale, int agentTeamIndex, int agentFormationIndex, uint clothingColor1, uint clothingColor2, int mountAgentIndex, Equipment mountAgentSpawnEquipment, bool isPlayerAgent, Vec3 position, Vec2 direction, NetworkCommunicator peer)
	{
		AgentIndex = agentIndex;
		MountAgentIndex = mountAgentIndex;
		Peer = peer;
		Character = character;
		Monster = monster;
		SpawnEquipment = new Equipment();
		MissionEquipment = new MissionEquipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			MissionEquipment[equipmentIndex] = missionEquipment[equipmentIndex];
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.ArmorItemEndSlot; equipmentIndex2++)
		{
			SpawnEquipment[equipmentIndex2] = spawnEquipment.GetEquipmentFromSlot(equipmentIndex2);
		}
		if (MountAgentIndex >= 0)
		{
			SpawnEquipment[EquipmentIndex.ArmorItemEndSlot] = mountAgentSpawnEquipment[EquipmentIndex.ArmorItemEndSlot];
			SpawnEquipment[EquipmentIndex.HorseHarness] = mountAgentSpawnEquipment[EquipmentIndex.HorseHarness];
		}
		else
		{
			SpawnEquipment[EquipmentIndex.ArmorItemEndSlot] = default(EquipmentElement);
			SpawnEquipment[EquipmentIndex.HorseHarness] = default(EquipmentElement);
		}
		BodyPropertiesValue = bodyPropertiesValue;
		BodyPropertiesSeed = bodyPropertiesSeed;
		IsFemale = isFemale;
		TeamIndex = agentTeamIndex;
		Position = position;
		Direction = direction;
		FormationIndex = agentFormationIndex;
		ClothingColor1 = clothingColor1;
		ClothingColor2 = clothingColor2;
		IsPlayerAgent = isPlayerAgent;
	}

	public CreateAgent()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Character = (BasicCharacterObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		Monster = (Monster)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
		AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		MountAgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
		Peer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
		SpawnEquipment = new Equipment();
		MissionEquipment = new MissionEquipment();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			MissionEquipment[equipmentIndex] = ModuleNetworkData.ReadWeaponReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid);
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex2++)
		{
			SpawnEquipment.AddEquipmentToSlotWithoutAgent(equipmentIndex2, ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref bufferReadValid));
		}
		IsPlayerAgent = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		BodyPropertiesSeed = ((!IsPlayerAgent) ? GameNetworkMessage.ReadIntFromPacket(CompressionBasic.RandomSeedCompressionInfo, ref bufferReadValid) : 0);
		BodyPropertiesValue = GameNetworkMessage.ReadBodyPropertiesFromPacket(ref bufferReadValid);
		IsFemale = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		Position = GameNetworkMessage.ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
		Direction = GameNetworkMessage.ReadVec2FromPacket(CompressionBasic.UnitVectorCompressionInfo, ref bufferReadValid).Normalized();
		FormationIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
		ClothingColor1 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		ClothingColor2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteObjectReferenceToPacket(Character, CompressionBasic.GUIDCompressionInfo);
		GameNetworkMessage.WriteObjectReferenceToPacket(Monster, CompressionBasic.GUIDCompressionInfo);
		GameNetworkMessage.WriteAgentIndexToPacket(AgentIndex);
		GameNetworkMessage.WriteAgentIndexToPacket(MountAgentIndex);
		GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Peer);
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			ModuleNetworkData.WriteWeaponReferenceToPacket(MissionEquipment[equipmentIndex]);
		}
		for (EquipmentIndex equipmentIndex2 = EquipmentIndex.NumAllWeaponSlots; equipmentIndex2 < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex2++)
		{
			ModuleNetworkData.WriteItemReferenceToPacket(SpawnEquipment.GetEquipmentFromSlot(equipmentIndex2));
		}
		GameNetworkMessage.WriteBoolToPacket(IsPlayerAgent);
		if (!IsPlayerAgent)
		{
			GameNetworkMessage.WriteIntToPacket(BodyPropertiesSeed, CompressionBasic.RandomSeedCompressionInfo);
		}
		GameNetworkMessage.WriteBodyPropertiesToPacket(BodyPropertiesValue);
		GameNetworkMessage.WriteBoolToPacket(IsFemale);
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
		GameNetworkMessage.WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		GameNetworkMessage.WriteVec2ToPacket(Direction, CompressionBasic.UnitVectorCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(ClothingColor1, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(ClothingColor2, CompressionBasic.ColorCompressionInfo);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Agents;
	}

	protected override string OnGetLogFormat()
	{
		return "Create an agent with index: " + AgentIndex + ((Peer != null) ? (", belonging to peer with Name: " + Peer.UserName + ", and peer-index: " + Peer.Index) : "") + ((MountAgentIndex == -1) ? "" : (", owning a mount with index: " + MountAgentIndex));
	}
}
