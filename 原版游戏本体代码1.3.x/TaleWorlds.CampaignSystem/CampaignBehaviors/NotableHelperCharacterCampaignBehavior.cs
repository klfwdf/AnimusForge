using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041A RID: 1050
	public class NotableHelperCharacterCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004295 RID: 17045 RVA: 0x00140CB4 File Offset: 0x0013EEB4
		public override void RegisterEvents()
		{
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
		}

		// Token: 0x06004296 RID: 17046 RVA: 0x00140D06 File Offset: 0x0013EF06
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004297 RID: 17047 RVA: 0x00140D08 File Offset: 0x0013EF08
		private void OnMissionEnded(IMission mission)
		{
			if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && !Settlement.CurrentSettlement.IsUnderSiege)
			{
				this._addNotableHelperCharacters = true;
			}
		}

		// Token: 0x06004298 RID: 17048 RVA: 0x00140D3E File Offset: 0x0013EF3E
		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && mobileParty != null && mobileParty == MobileParty.MainParty)
			{
				this._addNotableHelperCharacters = true;
			}
		}

		// Token: 0x06004299 RID: 17049 RVA: 0x00140D60 File Offset: 0x0013EF60
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			Location locationWithId = LocationComplex.Current.GetLocationWithId("center");
			Location locationWithId2 = LocationComplex.Current.GetLocationWithId("village_center");
			if (this._addNotableHelperCharacters && (CampaignMission.Current.Location == locationWithId || CampaignMission.Current.Location == locationWithId2))
			{
				this.SpawnNotableHelperCharacters(settlement);
				this._addNotableHelperCharacters = false;
			}
		}

		// Token: 0x0600429A RID: 17050 RVA: 0x00140DC8 File Offset: 0x0013EFC8
		private void SpawnNotableHelperCharacters(Settlement settlement)
		{
			int num = settlement.Notables.Count((Hero x) => x.IsGangLeader);
			int characterToSpawnCount = settlement.Notables.Count((Hero x) => x.IsPreacher);
			int characterToSpawnCount2 = settlement.Notables.Count((Hero x) => x.IsArtisan);
			int characterToSpawnCount3 = settlement.Notables.Count((Hero x) => x.IsRuralNotable || x.IsHeadman);
			int characterToSpawnCount4 = settlement.Notables.Count((Hero x) => x.IsMerchant);
			this.SpawnNotableHelperCharacter(settlement.Culture.GangleaderBodyguard, "_gangleader_bodyguard", "sp_gangleader_bodyguard", num * 2);
			this.SpawnNotableHelperCharacter(settlement.Culture.PreacherNotary, "_merchant_notary", "sp_preacher_notary", characterToSpawnCount);
			this.SpawnNotableHelperCharacter(settlement.Culture.ArtisanNotary, "_merchant_notary", "sp_artisan_notary", characterToSpawnCount2);
			this.SpawnNotableHelperCharacter(settlement.Culture.RuralNotableNotary, "_merchant_notary", "sp_rural_notable_notary", characterToSpawnCount3);
			this.SpawnNotableHelperCharacter(settlement.Culture.MerchantNotary, "_merchant_notary", "sp_merchant_notary", characterToSpawnCount4);
		}

		// Token: 0x0600429B RID: 17051 RVA: 0x00140F3C File Offset: 0x0013F13C
		private void SpawnNotableHelperCharacter(CharacterObject character, string actionSetSuffix, string tag, int characterToSpawnCount)
		{
			Location location = LocationComplex.Current.GetLocationWithId("center") ?? LocationComplex.Current.GetLocationWithId("village_center");
			while (characterToSpawnCount > 0)
			{
				Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
				int minValue;
				int maxValue;
				Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(character, out minValue, out maxValue, "Notary");
				AgentData agentData = new AgentData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).NoHorses(true).Age(MBRandom.RandomInt(minValue, maxValue));
				LocationCharacter locationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), tag, true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, actionSetSuffix), true, false, null, false, false, true, null, false);
				location.AddCharacter(locationCharacter);
				characterToSpawnCount--;
			}
		}

		// Token: 0x04001304 RID: 4868
		private bool _addNotableHelperCharacters;
	}
}
