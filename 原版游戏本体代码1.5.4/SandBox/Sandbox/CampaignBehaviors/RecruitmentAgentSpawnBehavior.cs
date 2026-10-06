using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace SandBox.CampaignBehaviors;

public class RecruitmentAgentSpawnBehavior : CampaignBehaviorBase
{
	private RecruitmentCampaignBehavior RecruitmentBehavior => Campaign.Current.CampaignBehaviorManager.GetBehavior<RecruitmentCampaignBehavior>();

	public override void RegisterEvents()
	{
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
		CampaignEvents.MercenaryNumberChangedInTown.AddNonSerializedListener(this, OnMercenaryNumberChanged);
		CampaignEvents.MercenaryTroopChangedInTown.AddNonSerializedListener(this, OnMercenaryTroopChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		Location locationWithId = settlement.LocationComplex.GetLocationWithId("tavern");
		if (CampaignMission.Current.Location == locationWithId)
		{
			AddMercenaryCharacterToTavern(settlement);
		}
	}

	private void CheckIfMercenaryCharacterNeedsToRefresh(Settlement settlement, CharacterObject oldTroopType)
	{
		if (!settlement.IsTown || settlement != Settlement.CurrentSettlement || PlayerEncounter.LocationEncounter == null || settlement.LocationComplex == null || (CampaignMission.Current != null && GameStateManager.Current.ActiveState == CampaignMission.Current.State))
		{
			return;
		}
		if (oldTroopType != null)
		{
			Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern").RemoveAllCharacters((LocationCharacter x) => x.Character.Occupation == oldTroopType.Occupation);
		}
		AddMercenaryCharacterToTavern(settlement);
	}

	private void OnMercenaryNumberChanged(Town town, int oldNumber, int newNumber)
	{
		if (RecruitmentBehavior != null)
		{
			CheckIfMercenaryCharacterNeedsToRefresh(town.Owner.Settlement, RecruitmentBehavior.GetMercenaryData(town).TroopType);
		}
	}

	private void OnMercenaryTroopChanged(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
	{
		CheckIfMercenaryCharacterNeedsToRefresh(town.Owner.Settlement, oldTroopType);
	}

	private void AddMercenaryCharacterToTavern(Settlement settlement)
	{
		if (settlement.LocationComplex != null && settlement.IsTown && RecruitmentBehavior != null && RecruitmentBehavior.GetMercenaryData(settlement.Town).HasAvailableMercenary())
		{
			Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("tavern")?.AddLocationCharacters(CreateMercenary, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
		}
	}

	private LocationCharacter CreateMercenary(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject troopType = RecruitmentBehavior.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType;
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(troopType.Race, "_settlement");
		return new LocationCharacter(new AgentData(new SimpleAgentOrigin(troopType)).Monster(monsterWithSuffix).NoHorses(noHorses: true), SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors, "spawnpoint_mercenary", fixedLocation: true, relation, null, useCivilianEquipment: false);
	}
}
