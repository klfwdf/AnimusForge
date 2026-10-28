using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.CampaignBehaviors;

public class TownMerchantsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Location locationWithId = PlayerEncounter.LocationEncounter.Settlement.LocationComplex.GetLocationWithId("center");
		if (CampaignMission.Current.Location == locationWithId && Campaign.Current.IsDay)
		{
			AddTradersToCenter(unusedUsablePointCount);
		}
	}

	private void AddTradersToCenter(Dictionary<string, int> unusedUsablePointCount)
	{
		Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
		if (unusedUsablePointCount.TryGetValue("sp_merchant", out var value))
		{
			locationWithId.AddLocationCharacters(CreateMerchant, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
		if (unusedUsablePointCount.TryGetValue("sp_horse_merchant", out value))
		{
			locationWithId.AddLocationCharacters(CreateHorseTrader, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
		if (unusedUsablePointCount.TryGetValue("sp_armorer", out value))
		{
			locationWithId.AddLocationCharacters(CreateArmorer, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
		if (unusedUsablePointCount.TryGetValue("sp_weaponsmith", out value))
		{
			locationWithId.AddLocationCharacters(CreateWeaponsmith, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
		if (unusedUsablePointCount.TryGetValue("sp_blacksmith", out value))
		{
			locationWithId.AddLocationCharacters(CreateBlacksmith, Settlement.CurrentSettlement.Culture, LocationCharacter.CharacterRelations.Neutral, value);
		}
	}

	private static LocationCharacter CreateBlacksmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject blacksmith = culture.Blacksmith;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(blacksmith.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(blacksmith, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(blacksmith)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_blacksmith", fixedLocation: true, relation, null, useCivilianEquipment: true);
	}

	private static LocationCharacter CreateMerchant(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject merchant = culture.Merchant;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(merchant.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(merchant, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(merchant)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_merchant", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateHorseTrader(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject horseMerchant = culture.HorseMerchant;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(horseMerchant.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(horseMerchant, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(horseMerchant)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_horse_merchant", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateArmorer(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject armorer = culture.Armorer;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(armorer.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(armorer, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(armorer)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_armorer", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_seller"), useCivilianEquipment: true);
	}

	private static LocationCharacter CreateWeaponsmith(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject weaponsmith = culture.Weaponsmith;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(weaponsmith.Race, "_settlement");
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(weaponsmith, out var minimumAge, out var maximumAge);
		AgentData agentData = new AgentData(new SimpleAgentOrigin(weaponsmith)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "sp_weaponsmith", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_weaponsmith"), useCivilianEquipment: true);
	}
}
