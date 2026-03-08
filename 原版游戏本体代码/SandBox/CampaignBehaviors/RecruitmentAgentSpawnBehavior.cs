using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D9 RID: 217
	public class RecruitmentAgentSpawnBehavior : CampaignBehaviorBase
	{
		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000A48 RID: 2632 RVA: 0x0004E126 File Offset: 0x0004C326
		private RecruitmentCampaignBehavior RecruitmentBehavior
		{
			get
			{
				return Campaign.Current.CampaignBehaviorManager.GetBehavior<RecruitmentCampaignBehavior>();
			}
		}

		// Token: 0x06000A49 RID: 2633 RVA: 0x0004E138 File Offset: 0x0004C338
		public override void RegisterEvents()
		{
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
			CampaignEvents.MercenaryNumberChangedInTown.AddNonSerializedListener(this, new Action<Town, int, int>(this.OnMercenaryNumberChanged));
			CampaignEvents.MercenaryTroopChangedInTown.AddNonSerializedListener(this, new Action<Town, CharacterObject, CharacterObject>(this.OnMercenaryTroopChanged));
		}

		// Token: 0x06000A4A RID: 2634 RVA: 0x0004E18A File Offset: 0x0004C38A
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000A4B RID: 2635 RVA: 0x0004E18C File Offset: 0x0004C38C
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("tavern");
			if (CampaignMission.Current.Location == locationWithId)
			{
				this.AddMercenaryCharacterToTavern(settlement);
			}
		}

		// Token: 0x06000A4C RID: 2636 RVA: 0x0004E1CC File Offset: 0x0004C3CC
		private void CheckIfMercenaryCharacterNeedsToRefresh(Settlement settlement, CharacterObject oldTroopType)
		{
			if (settlement.IsTown && settlement == Settlement.CurrentSettlement && PlayerEncounter.LocationEncounter != null && settlement.LocationComplex != null && (CampaignMission.Current == null || GameStateManager.Current.ActiveState != CampaignMission.Current.State))
			{
				if (oldTroopType != null)
				{
					Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern").RemoveAllCharacters((LocationCharacter x) => x.Character.Occupation == oldTroopType.Occupation);
				}
				this.AddMercenaryCharacterToTavern(settlement);
			}
		}

		// Token: 0x06000A4D RID: 2637 RVA: 0x0004E256 File Offset: 0x0004C456
		private void OnMercenaryNumberChanged(Town town, int oldNumber, int newNumber)
		{
			if (this.RecruitmentBehavior != null)
			{
				this.CheckIfMercenaryCharacterNeedsToRefresh(town.Owner.Settlement, this.RecruitmentBehavior.GetMercenaryData(town).TroopType);
			}
		}

		// Token: 0x06000A4E RID: 2638 RVA: 0x0004E282 File Offset: 0x0004C482
		private void OnMercenaryTroopChanged(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
		{
			this.CheckIfMercenaryCharacterNeedsToRefresh(town.Owner.Settlement, oldTroopType);
		}

		// Token: 0x06000A4F RID: 2639 RVA: 0x0004E298 File Offset: 0x0004C498
		private void AddMercenaryCharacterToTavern(Settlement settlement)
		{
			if (settlement.LocationComplex != null && settlement.IsTown && this.RecruitmentBehavior != null && this.RecruitmentBehavior.GetMercenaryData(settlement.Town).HasAvailableMercenary(Occupation.NotAssigned))
			{
				Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern");
				if (locationWithId != null)
				{
					locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateMercenary), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
				}
			}
		}

		// Token: 0x06000A50 RID: 2640 RVA: 0x0004E308 File Offset: 0x0004C508
		private LocationCharacter CreateMercenary(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject troopType = this.RecruitmentBehavior.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(troopType.Race, "_settlement");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(troopType, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).NoHorses(true), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "spawnpoint_mercenary", true, relation, null, false, false, null, false, false, true, null, false);
		}
	}
}
