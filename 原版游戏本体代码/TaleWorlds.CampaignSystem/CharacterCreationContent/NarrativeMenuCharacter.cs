using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x0200020F RID: 527
	public class NarrativeMenuCharacter
	{
		// Token: 0x170007DB RID: 2011
		// (get) Token: 0x06001FC9 RID: 8137 RVA: 0x0008E68C File Offset: 0x0008C88C
		// (set) Token: 0x06001FCA RID: 8138 RVA: 0x0008E694 File Offset: 0x0008C894
		public BodyProperties BodyProperties { get; private set; }

		// Token: 0x170007DC RID: 2012
		// (get) Token: 0x06001FCB RID: 8139 RVA: 0x0008E69D File Offset: 0x0008C89D
		// (set) Token: 0x06001FCC RID: 8140 RVA: 0x0008E6A5 File Offset: 0x0008C8A5
		public int Race { get; private set; }

		// Token: 0x170007DD RID: 2013
		// (get) Token: 0x06001FCD RID: 8141 RVA: 0x0008E6AE File Offset: 0x0008C8AE
		// (set) Token: 0x06001FCE RID: 8142 RVA: 0x0008E6B6 File Offset: 0x0008C8B6
		public bool IsFemale { get; set; }

		// Token: 0x170007DE RID: 2014
		// (get) Token: 0x06001FCF RID: 8143 RVA: 0x0008E6BF File Offset: 0x0008C8BF
		// (set) Token: 0x06001FD0 RID: 8144 RVA: 0x0008E6C7 File Offset: 0x0008C8C7
		public MBEquipmentRoster Equipment { get; private set; }

		// Token: 0x170007DF RID: 2015
		// (get) Token: 0x06001FD1 RID: 8145 RVA: 0x0008E6D0 File Offset: 0x0008C8D0
		// (set) Token: 0x06001FD2 RID: 8146 RVA: 0x0008E6D8 File Offset: 0x0008C8D8
		public string AnimationId { get; private set; }

		// Token: 0x170007E0 RID: 2016
		// (get) Token: 0x06001FD3 RID: 8147 RVA: 0x0008E6E1 File Offset: 0x0008C8E1
		// (set) Token: 0x06001FD4 RID: 8148 RVA: 0x0008E6E9 File Offset: 0x0008C8E9
		public MountCreationKey MountCreationKey { get; private set; }

		// Token: 0x170007E1 RID: 2017
		// (get) Token: 0x06001FD5 RID: 8149 RVA: 0x0008E6F2 File Offset: 0x0008C8F2
		// (set) Token: 0x06001FD6 RID: 8150 RVA: 0x0008E6FA File Offset: 0x0008C8FA
		public string Item1Id { get; private set; }

		// Token: 0x170007E2 RID: 2018
		// (get) Token: 0x06001FD7 RID: 8151 RVA: 0x0008E703 File Offset: 0x0008C903
		// (set) Token: 0x06001FD8 RID: 8152 RVA: 0x0008E70B File Offset: 0x0008C90B
		public string Item2Id { get; private set; }

		// Token: 0x170007E3 RID: 2019
		// (get) Token: 0x06001FD9 RID: 8153 RVA: 0x0008E714 File Offset: 0x0008C914
		// (set) Token: 0x06001FDA RID: 8154 RVA: 0x0008E71C File Offset: 0x0008C91C
		public EquipmentIndex RightHandEquipmentIndex { get; private set; }

		// Token: 0x170007E4 RID: 2020
		// (get) Token: 0x06001FDB RID: 8155 RVA: 0x0008E725 File Offset: 0x0008C925
		// (set) Token: 0x06001FDC RID: 8156 RVA: 0x0008E72D File Offset: 0x0008C92D
		public EquipmentIndex LeftHandEquipmentIndex { get; private set; }

		// Token: 0x06001FDD RID: 8157 RVA: 0x0008E738 File Offset: 0x0008C938
		public NarrativeMenuCharacter(string stringId, BodyProperties bodyProperties, int race, bool isFemale)
		{
			this.StringId = stringId;
			this.BodyProperties = bodyProperties;
			this.Race = race;
			this.IsFemale = isFemale;
			this.IsHuman = true;
			this.SpawnPointEntityId = "spawnpoint_player_1";
			this.AnimationId = "act_inventory_idle_start";
			this.Equipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
		}

		// Token: 0x06001FDE RID: 8158 RVA: 0x0008E79F File Offset: 0x0008C99F
		public NarrativeMenuCharacter(string stringId)
		{
			this.StringId = stringId;
			this.IsHuman = false;
			this.SpawnPointEntityId = "spawnpoint_mount_1";
			this.AnimationId = "act_inventory_idle_start";
		}

		// Token: 0x06001FDF RID: 8159 RVA: 0x0008E7CB File Offset: 0x0008C9CB
		public void UpdateBodyProperties(BodyProperties bodyProperties, int race, bool isFemale)
		{
			this.BodyProperties = bodyProperties;
			this.Race = race;
			this.IsFemale = isFemale;
		}

		// Token: 0x06001FE0 RID: 8160 RVA: 0x0008E7E2 File Offset: 0x0008C9E2
		public void SetEquipment(MBEquipmentRoster equipment)
		{
			this.Equipment = equipment;
		}

		// Token: 0x06001FE1 RID: 8161 RVA: 0x0008E7EB File Offset: 0x0008C9EB
		public void SetAnimationId(string animationId)
		{
			this.AnimationId = animationId;
		}

		// Token: 0x06001FE2 RID: 8162 RVA: 0x0008E7F4 File Offset: 0x0008C9F4
		public void SetRightHandItem(string itemId)
		{
			this.Item1Id = itemId;
		}

		// Token: 0x06001FE3 RID: 8163 RVA: 0x0008E7FD File Offset: 0x0008C9FD
		public void SetLeftHandItem(string itemId)
		{
			this.Item2Id = itemId;
		}

		// Token: 0x06001FE4 RID: 8164 RVA: 0x0008E806 File Offset: 0x0008CA06
		public void EquipRightHandItemWithEquipmentIndex(EquipmentIndex item)
		{
			this.RightHandEquipmentIndex = item;
		}

		// Token: 0x06001FE5 RID: 8165 RVA: 0x0008E80F File Offset: 0x0008CA0F
		public void EquipLeftHandItemWithEquipmentIndex(EquipmentIndex item)
		{
			this.LeftHandEquipmentIndex = item;
		}

		// Token: 0x06001FE6 RID: 8166 RVA: 0x0008E818 File Offset: 0x0008CA18
		public void SetSpawnPointEntityId(string spawnPointEntityId)
		{
			this.SpawnPointEntityId = spawnPointEntityId;
		}

		// Token: 0x06001FE7 RID: 8167 RVA: 0x0008E824 File Offset: 0x0008CA24
		public void ChangeAge(float age)
		{
			BodyProperties bodyProperties = this.BodyProperties;
			this.BodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, age);
		}

		// Token: 0x06001FE8 RID: 8168 RVA: 0x0008E846 File Offset: 0x0008CA46
		public void SetMountCreationKey(MountCreationKey mountCreationKey)
		{
			this.MountCreationKey = mountCreationKey;
		}

		// Token: 0x06001FE9 RID: 8169 RVA: 0x0008E84F File Offset: 0x0008CA4F
		public void SetHorseItemId(string itemId)
		{
			this.Item1Id = itemId;
		}

		// Token: 0x06001FEA RID: 8170 RVA: 0x0008E858 File Offset: 0x0008CA58
		public void SetHarnessItemId(string itemId)
		{
			this.Item2Id = itemId;
		}

		// Token: 0x04000947 RID: 2375
		public readonly string StringId;

		// Token: 0x04000948 RID: 2376
		public readonly bool IsHuman;

		// Token: 0x04000949 RID: 2377
		public string SpawnPointEntityId;
	}
}
