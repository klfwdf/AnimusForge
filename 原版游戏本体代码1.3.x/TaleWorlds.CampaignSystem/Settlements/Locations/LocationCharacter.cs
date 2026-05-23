using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Settlements.Locations
{
	// Token: 0x020003C5 RID: 965
	public class LocationCharacter
	{
		// Token: 0x17000DC8 RID: 3528
		// (get) Token: 0x06003945 RID: 14661 RVA: 0x000E90AD File Offset: 0x000E72AD
		public CharacterObject Character
		{
			get
			{
				return (CharacterObject)this.AgentData.AgentCharacter;
			}
		}

		// Token: 0x17000DC9 RID: 3529
		// (get) Token: 0x06003946 RID: 14662 RVA: 0x000E90BF File Offset: 0x000E72BF
		public IAgentOriginBase AgentOrigin
		{
			get
			{
				return this.AgentData.AgentOrigin;
			}
		}

		// Token: 0x17000DCA RID: 3530
		// (get) Token: 0x06003947 RID: 14663 RVA: 0x000E90CC File Offset: 0x000E72CC
		public AgentData AgentData { get; }

		// Token: 0x17000DCB RID: 3531
		// (get) Token: 0x06003948 RID: 14664 RVA: 0x000E90D4 File Offset: 0x000E72D4
		public bool UseCivilianEquipment { get; }

		// Token: 0x17000DCC RID: 3532
		// (get) Token: 0x06003949 RID: 14665 RVA: 0x000E90DC File Offset: 0x000E72DC
		public string ActionSetCode { get; }

		// Token: 0x17000DCD RID: 3533
		// (get) Token: 0x0600394A RID: 14666 RVA: 0x000E90E4 File Offset: 0x000E72E4
		public string AlarmedActionSetCode { get; }

		// Token: 0x17000DCE RID: 3534
		// (get) Token: 0x0600394B RID: 14667 RVA: 0x000E90EC File Offset: 0x000E72EC
		// (set) Token: 0x0600394C RID: 14668 RVA: 0x000E90F4 File Offset: 0x000E72F4
		public string SpecialTargetTag { get; set; }

		// Token: 0x17000DCF RID: 3535
		// (get) Token: 0x0600394D RID: 14669 RVA: 0x000E90FD File Offset: 0x000E72FD
		// (set) Token: 0x0600394E RID: 14670 RVA: 0x000E9105 File Offset: 0x000E7305
		public bool ForceSpawnInSpecialTargetTag { get; set; }

		// Token: 0x17000DD0 RID: 3536
		// (get) Token: 0x0600394F RID: 14671 RVA: 0x000E910E File Offset: 0x000E730E
		public LocationCharacter.AddBehaviorsDelegate AddBehaviors { get; }

		// Token: 0x17000DD1 RID: 3537
		// (get) Token: 0x06003950 RID: 14672 RVA: 0x000E9116 File Offset: 0x000E7316
		public LocationCharacter.AfterAgentCreatedDelegate AfterAgentCreated { get; }

		// Token: 0x17000DD2 RID: 3538
		// (get) Token: 0x06003951 RID: 14673 RVA: 0x000E911E File Offset: 0x000E731E
		public bool FixedLocation { get; }

		// Token: 0x17000DD3 RID: 3539
		// (get) Token: 0x06003952 RID: 14674 RVA: 0x000E9126 File Offset: 0x000E7326
		// (set) Token: 0x06003953 RID: 14675 RVA: 0x000E912E File Offset: 0x000E732E
		public Alley MemberOfAlley { get; private set; }

		// Token: 0x17000DD4 RID: 3540
		// (get) Token: 0x06003954 RID: 14676 RVA: 0x000E9137 File Offset: 0x000E7337
		public ItemObject SpecialItem { get; }

		// Token: 0x06003955 RID: 14677 RVA: 0x000E9140 File Offset: 0x000E7340
		public LocationCharacter(AgentData agentData, LocationCharacter.AddBehaviorsDelegate addBehaviorsDelegate, string spawnTag, bool fixedLocation, LocationCharacter.CharacterRelations characterRelation, string actionSetCode, bool useCivilianEquipment, bool isFixedCharacter = false, ItemObject specialItem = null, bool isHidden = false, bool isVisualTracked = false, bool overrideBodyProperties = true, LocationCharacter.AfterAgentCreatedDelegate afterAgentCreated = null, bool forceSpawnOnSpecialTargetTag = false)
		{
			this.AgentData = agentData;
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				int seed = -2;
				if (overrideBodyProperties)
				{
					seed = (isFixedCharacter ? (Settlement.CurrentSettlement.StringId + "_" + this.Character.StringId).GetDeterministicHashCode() : agentData.AgentEquipmentSeed);
				}
				this.AgentData.BodyProperties(this.Character.GetBodyProperties(this.Character.Equipment, seed));
			}
			this.AddBehaviors = addBehaviorsDelegate;
			this.SpecialTargetTag = spawnTag;
			this.FixedLocation = fixedLocation;
			this.ActionSetCode = actionSetCode ?? TaleWorlds.Core.ActionSetCode.GenerateActionSetNameWithSuffix(this.AgentData.AgentMonster, this.AgentData.AgentCharacter.IsFemale, "_villager");
			this.AlarmedActionSetCode = TaleWorlds.Core.ActionSetCode.GenerateActionSetNameWithSuffix(this.AgentData.AgentMonster, this.AgentData.AgentIsFemale, "_villager");
			this.PrefabNamesForBones = new Dictionary<sbyte, string>();
			this.CharacterRelation = characterRelation;
			this.SpecialItem = specialItem;
			this.UseCivilianEquipment = useCivilianEquipment;
			this.AfterAgentCreated = afterAgentCreated;
			this.IsVisualTracked = isVisualTracked;
			if (forceSpawnOnSpecialTargetTag)
			{
				this.ForceSpawnInSpecialTargetTag = true;
			}
		}

		// Token: 0x06003956 RID: 14678 RVA: 0x000E926D File Offset: 0x000E746D
		public void SetAlleyOfCharacter(Alley alley)
		{
			this.MemberOfAlley = alley;
		}

		// Token: 0x06003957 RID: 14679 RVA: 0x000E9278 File Offset: 0x000E7478
		public static LocationCharacter CreateBodyguardHero(Hero hero, MobileParty party, LocationCharacter.AddBehaviorsDelegate addBehaviorsDelegate)
		{
			UniqueTroopDescriptor uniqueNo = new UniqueTroopDescriptor(FlattenedTroopRoster.GenerateUniqueNoFromParty(party, 0));
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(hero.CharacterObject.Race, "_settlement");
			return new LocationCharacter(new AgentData(new PartyAgentOrigin(PartyBase.MainParty, hero.CharacterObject, -1, uniqueNo, false, false)).Monster(monsterWithSuffix).NoHorses(true), addBehaviorsDelegate, null, false, LocationCharacter.CharacterRelations.Friendly, null, !PlayerEncounter.LocationEncounter.Settlement.IsVillage, false, null, false, false, true, null, false);
		}

		// Token: 0x040011A1 RID: 4513
		public bool IsVisualTracked;

		// Token: 0x040011AA RID: 4522
		public Dictionary<sbyte, string> PrefabNamesForBones;

		// Token: 0x040011AC RID: 4524
		public LocationCharacter.CharacterRelations CharacterRelation;

		// Token: 0x02000790 RID: 1936
		// (Invoke) Token: 0x060061E8 RID: 25064
		public delegate void AddBehaviorsDelegate(IAgent agent);

		// Token: 0x02000791 RID: 1937
		// (Invoke) Token: 0x060061EC RID: 25068
		public delegate void AfterAgentCreatedDelegate(IAgent agent);

		// Token: 0x02000792 RID: 1938
		public enum CharacterRelations
		{
			// Token: 0x04001E7E RID: 7806
			Neutral,
			// Token: 0x04001E7F RID: 7807
			Friendly,
			// Token: 0x04001E80 RID: 7808
			Enemy
		}
	}
}
