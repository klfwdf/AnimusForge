using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000DF RID: 223
	public class TownMerchantsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000AFF RID: 2815 RVA: 0x00051F95 File Offset: 0x00050195
		public override void RegisterEvents()
		{
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x00051FAE File Offset: 0x000501AE
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x00051FB0 File Offset: 0x000501B0
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
			Location locationWithId = PlayerEncounter.LocationEncounter.Settlement.LocationComplex.GetLocationWithId("center");
			if (CampaignMission.Current.Location == locationWithId && Campaign.Current.IsDay)
			{
				this.AddTradersToCenter(unusedUsablePointCount);
			}
		}

		// Token: 0x06000B02 RID: 2818 RVA: 0x00051FF8 File Offset: 0x000501F8
		private void AddTradersToCenter(Dictionary<string, int> unusedUsablePointCount)
		{
			Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
			int count;
			if (unusedUsablePointCount.TryGetValue("sp_merchant", out count))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(TownMerchantsCampaignBehavior.CreateMerchant), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
			if (unusedUsablePointCount.TryGetValue("sp_horse_merchant", out count))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(TownMerchantsCampaignBehavior.CreateHorseTrader), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
			if (unusedUsablePointCount.TryGetValue("sp_armorer", out count))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(TownMerchantsCampaignBehavior.CreateArmorer), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
			if (unusedUsablePointCount.TryGetValue("sp_weaponsmith", out count))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(TownMerchantsCampaignBehavior.CreateWeaponsmith), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
			if (unusedUsablePointCount.TryGetValue("sp_blacksmith", out count))
			{
				locationWithId.AddLocationCharacters(new CreateLocationCharacterDelegate(TownMerchantsCampaignBehavior.CreateBlacksmith), Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, count);
			}
		}

		// Token: 0x06000B03 RID: 2819 RVA: 0x000520FC File Offset: 0x000502FC
		private static LocationCharacter CreateBlacksmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject blacksmith = culture.Blacksmith;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(blacksmith.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(blacksmith, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(blacksmith, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_blacksmith", true, relation, null, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000B04 RID: 2820 RVA: 0x00052194 File Offset: 0x00050394
		private static LocationCharacter CreateMerchant(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject merchant = culture.Merchant;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(merchant.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(merchant, out minValue, out maxValue, "");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(merchant, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_merchant", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000B05 RID: 2821 RVA: 0x00052248 File Offset: 0x00050448
		private static LocationCharacter CreateHorseTrader(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject horseMerchant = culture.HorseMerchant;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(horseMerchant.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(horseMerchant, out minValue, out maxValue, "");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(horseMerchant, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_horse_merchant", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000B06 RID: 2822 RVA: 0x000522FC File Offset: 0x000504FC
		private static LocationCharacter CreateArmorer(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject armorer = culture.Armorer;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(armorer.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(armorer, out minValue, out maxValue, "");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(armorer, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_armorer", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000B07 RID: 2823 RVA: 0x000523B0 File Offset: 0x000505B0
		private static LocationCharacter CreateWeaponsmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			CharacterObject weaponsmith = culture.Weaponsmith;
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(weaponsmith.Race, "_settlement");
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(weaponsmith, out minValue, out maxValue, "");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(weaponsmith, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
			return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_weaponsmith", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_weaponsmith"), true, false, null, false, false, true, null, false);
		}
	}
}
