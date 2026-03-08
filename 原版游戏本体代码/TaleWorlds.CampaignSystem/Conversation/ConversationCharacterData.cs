using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000233 RID: 563
	public struct ConversationCharacterData : ISerializableObject
	{
		// Token: 0x060021EE RID: 8686 RVA: 0x00095BB8 File Offset: 0x00093DB8
		public ConversationCharacterData(CharacterObject character, PartyBase party = null, bool noHorse = false, bool noWeapon = false, bool spawnAfterFight = false, bool isCivilianEquipmentRequiredForLeader = false, bool isCivilianEquipmentRequiredForBodyGuardCharacters = false, bool noBodyguards = false)
		{
			this.Character = character;
			this.Party = party;
			this.NoHorse = noHorse;
			this.NoWeapon = noWeapon;
			this.NoBodyguards = noBodyguards;
			this.SpawnedAfterFight = spawnAfterFight;
			this.IsCivilianEquipmentRequiredForLeader = isCivilianEquipmentRequiredForLeader;
			this.IsCivilianEquipmentRequiredForBodyGuardCharacters = isCivilianEquipmentRequiredForBodyGuardCharacters;
		}

		// Token: 0x060021EF RID: 8687 RVA: 0x00095BF8 File Offset: 0x00093DF8
		void ISerializableObject.DeserializeFrom(IReader reader)
		{
			MBGUID objectId = new MBGUID(reader.ReadUInt());
			this.Character = (CharacterObject)MBObjectManager.Instance.GetObject(objectId);
			int index = reader.ReadInt();
			this.Party = ConversationCharacterData.FindParty(index);
			this.NoHorse = reader.ReadBool();
			this.NoWeapon = reader.ReadBool();
			this.SpawnedAfterFight = reader.ReadBool();
		}

		// Token: 0x060021F0 RID: 8688 RVA: 0x00095C60 File Offset: 0x00093E60
		void ISerializableObject.SerializeTo(IWriter writer)
		{
			writer.WriteUInt(this.Character.Id.InternalValue);
			writer.WriteInt((this.Party == null) ? (-1) : this.Party.Index);
			writer.WriteBool(this.NoHorse);
			writer.WriteBool(this.NoWeapon);
			writer.WriteBool(this.SpawnedAfterFight);
		}

		// Token: 0x060021F1 RID: 8689 RVA: 0x00095CC8 File Offset: 0x00093EC8
		private static PartyBase FindParty(int index)
		{
			MobileParty mobileParty = Campaign.Current.CampaignObjectManager.FindFirst<MobileParty>((MobileParty x) => x.Party.Index == index);
			if (mobileParty != null)
			{
				return mobileParty.Party;
			}
			Settlement settlement = Settlement.All.FirstOrDefaultQ((Settlement x) => x.Party.Index == index);
			if (settlement != null)
			{
				return settlement.Party;
			}
			return null;
		}

		// Token: 0x040009ED RID: 2541
		public CharacterObject Character;

		// Token: 0x040009EE RID: 2542
		public PartyBase Party;

		// Token: 0x040009EF RID: 2543
		public bool NoHorse;

		// Token: 0x040009F0 RID: 2544
		public bool NoWeapon;

		// Token: 0x040009F1 RID: 2545
		public bool NoBodyguards;

		// Token: 0x040009F2 RID: 2546
		public bool SpawnedAfterFight;

		// Token: 0x040009F3 RID: 2547
		public bool IsCivilianEquipmentRequiredForLeader;

		// Token: 0x040009F4 RID: 2548
		public bool IsCivilianEquipmentRequiredForBodyGuardCharacters;
	}
}
