using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors;

public class StealthCharactersCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedPoints)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (!settlement.IsHideout)
		{
			Location location = settlement.LocationComplex.GetListOfLocations().First();
			if (unusedPoints.TryGetValue("stealth_agent", out var value) && value > 0)
			{
				location.AddLocationCharacters(CreateStealthCharacter, settlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
			}
			if (unusedPoints.TryGetValue("stealth_agent_forced", out value) && value > 0)
			{
				location.AddLocationCharacters(CreteForcedStealthCharacter, settlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
			}
			if (unusedPoints.TryGetValue("disguise_default_agent", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateDisguiseDefaultCharacter, settlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
			}
			if (unusedPoints.TryGetValue("disguise_officer_agent", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateDisguiseOfficerCharacter, settlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
			}
			if (unusedPoints.TryGetValue("disguise_shadow_agent", out value) && value > 0)
			{
				location.AddLocationCharacters(CreateDisguiseShadowTargetCharacter, settlement.Culture, LocationCharacter.CharacterRelations.Enemy, value);
			}
		}
	}

	private LocationCharacter CreateStealthCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		return CreateStealthAgentInternal("stealth_agent", "stealth_character");
	}

	private LocationCharacter CreteForcedStealthCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		LocationCharacter locationCharacter = CreateStealthAgentInternal("stealth_agent_forced", "stealth_character");
		locationCharacter.ForceSpawnInSpecialTargetTag = true;
		return locationCharacter;
	}

	private LocationCharacter CreateDisguiseDefaultCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		return CreateStealthAgentInternal("disguise_default_agent", "disguise_default_character");
	}

	private LocationCharacter CreateDisguiseOfficerCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		return CreateStealthAgentInternal("disguise_officer_agent", "disguise_officer_character");
	}

	private LocationCharacter CreateDisguiseShadowTargetCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		return CreateStealthAgentInternal("disguise_shadow_agent", "disguise_shadow_target");
	}

	private LocationCharacter CreateStealthAgentInternal(string spawnTag, string characterId)
	{
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(characterId);
		Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out var minimumAge, out var maximumAge);
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(characterObject)).Monster(FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minimumAge, maximumAge)), SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors, spawnTag, fixedLocation: true, LocationCharacter.CharacterRelations.Enemy, null, useCivilianEquipment: true);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
