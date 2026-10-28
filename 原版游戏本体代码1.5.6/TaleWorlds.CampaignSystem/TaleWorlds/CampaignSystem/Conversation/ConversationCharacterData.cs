using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Conversation;

public struct ConversationCharacterData(CharacterObject character, PartyBase party = null, bool noHorse = false, bool noWeapon = false, bool spawnAfterFight = false, bool isCivilianEquipmentRequiredForLeader = false, bool isCivilianEquipmentRequiredForBodyGuardCharacters = false, bool noBodyguards = false) : ISerializableObject
{
	public CharacterObject Character = character;

	public PartyBase Party = party;

	public bool NoHorse = noHorse;

	public bool NoWeapon = noWeapon;

	public bool NoBodyguards = noBodyguards;

	public bool SpawnedAfterFight = spawnAfterFight;

	public bool IsCivilianEquipmentRequiredForLeader = isCivilianEquipmentRequiredForLeader;

	public bool IsCivilianEquipmentRequiredForBodyGuardCharacters = isCivilianEquipmentRequiredForBodyGuardCharacters;

	void ISerializableObject.DeserializeFrom(IReader reader)
	{
		MBGUID objectId = new MBGUID(reader.ReadUInt());
		Character = (CharacterObject)MBObjectManager.Instance.GetObject(objectId);
		int index = reader.ReadInt();
		Party = FindParty(index);
		NoHorse = reader.ReadBool();
		NoWeapon = reader.ReadBool();
		SpawnedAfterFight = reader.ReadBool();
	}

	void ISerializableObject.SerializeTo(IWriter writer)
	{
		writer.WriteUInt(Character.Id.InternalValue);
		writer.WriteInt((Party == null) ? (-1) : Party.Index);
		writer.WriteBool(NoHorse);
		writer.WriteBool(NoWeapon);
		writer.WriteBool(SpawnedAfterFight);
	}

	private static PartyBase FindParty(int index)
	{
		MobileParty mobileParty = Campaign.Current.CampaignObjectManager.FindFirst((MobileParty x) => x.Party.Index == index);
		if (mobileParty != null)
		{
			return mobileParty.Party;
		}
		return Settlement.All.FirstOrDefaultQ((Settlement x) => x.Party.Index == index)?.Party;
	}
}
