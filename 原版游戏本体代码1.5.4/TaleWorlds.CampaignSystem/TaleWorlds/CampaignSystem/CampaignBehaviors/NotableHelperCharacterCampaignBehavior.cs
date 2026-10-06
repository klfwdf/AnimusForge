using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class NotableHelperCharacterCampaignBehavior : CampaignBehaviorBase
{
	private bool _addNotableHelperCharacters;

	public override void RegisterEvents()
	{
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMissionEnded(IMission mission)
	{
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && !Settlement.CurrentSettlement.IsUnderSiege)
		{
			_addNotableHelperCharacters = true;
		}
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && mobileParty != null && mobileParty == MobileParty.MainParty)
		{
			_addNotableHelperCharacters = true;
		}
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		Location locationWithId = LocationComplex.Current.GetLocationWithId("center");
		Location locationWithId2 = LocationComplex.Current.GetLocationWithId("village_center");
		if (_addNotableHelperCharacters && (CampaignMission.Current.Location == locationWithId || CampaignMission.Current.Location == locationWithId2))
		{
			SpawnNotableHelperCharacters(settlement);
			_addNotableHelperCharacters = false;
		}
	}

	private void SpawnNotableHelperCharacters(Settlement settlement)
	{
		int num = settlement.Notables.Count((Hero x) => x.IsGangLeader);
		int characterToSpawnCount = settlement.Notables.Count((Hero x) => x.IsPreacher);
		int characterToSpawnCount2 = settlement.Notables.Count((Hero x) => x.IsArtisan);
		int characterToSpawnCount3 = settlement.Notables.Count((Hero x) => x.IsRuralNotable || x.IsHeadman);
		int characterToSpawnCount4 = settlement.Notables.Count((Hero x) => x.IsMerchant);
		SpawnNotableHelperCharacter(settlement.Culture.GangleaderBodyguard, "_gangleader_bodyguard", "sp_gangleader_bodyguard", num * 2);
		SpawnNotableHelperCharacter(settlement.Culture.PreacherNotary, "_merchant_notary", "sp_preacher_notary", characterToSpawnCount);
		SpawnNotableHelperCharacter(settlement.Culture.ArtisanNotary, "_merchant_notary", "sp_artisan_notary", characterToSpawnCount2);
		SpawnNotableHelperCharacter(settlement.Culture.RuralNotableNotary, "_merchant_notary", "sp_rural_notable_notary", characterToSpawnCount3);
		SpawnNotableHelperCharacter(settlement.Culture.MerchantNotary, "_merchant_notary", "sp_merchant_notary", characterToSpawnCount4);
	}

	private void SpawnNotableHelperCharacter(CharacterObject character, string actionSetSuffix, string tag, int characterToSpawnCount)
	{
		Location location = LocationComplex.Current.GetLocationWithId("center") ?? LocationComplex.Current.GetLocationWithId("village_center");
		while (characterToSpawnCount > 0)
		{
			Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(character, out var minimumAge, out var maximumAge, "Notary");
			AgentData agentData = new AgentData(new SimpleAgentOrigin(character)).Monster(monsterWithSuffix).NoHorses(noHorses: true).Age(MBRandom.RandomInt(minimumAge, maximumAge));
			LocationCharacter locationCharacter = new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, tag, fixedLocation: true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, actionSetSuffix), useCivilianEquipment: true);
			location.AddCharacter(locationCharacter);
			characterToSpawnCount--;
		}
	}
}
