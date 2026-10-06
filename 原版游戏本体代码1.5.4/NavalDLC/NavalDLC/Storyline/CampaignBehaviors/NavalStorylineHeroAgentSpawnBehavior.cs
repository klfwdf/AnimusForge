using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineHeroAgentSpawnBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMissionEnded(IMission mission)
	{
		if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && !Settlement.CurrentSettlement.IsUnderSiege && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.HasPort && NavalStorylineData.IsNavalStoryLineActive())
		{
			AddNavalStorylineHeroesInsideMainPartyToPort(Settlement.CurrentSettlement);
		}
	}

	private void OnGameLoadFinished()
	{
		if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && !Settlement.CurrentSettlement.IsUnderSiege && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.HasPort && NavalStorylineData.IsNavalStoryLineActive())
		{
			AddNavalStorylineHeroesInsideMainPartyToPort(Settlement.CurrentSettlement);
		}
	}

	private void AddNavalStorylineHeroesInsideMainPartyToPort(Settlement settlement)
	{
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character.IsHero && NavalStorylineData.IsNavalStorylineHero(character.HeroObject))
			{
				Hero heroObject = character.HeroObject;
				AddNavalStorylineHeroToPortAsLocationCharacter(heroObject);
			}
		}
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && !Settlement.CurrentSettlement.IsUnderSiege && Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.HasPort && NavalStorylineData.IsNavalStoryLineActive())
		{
			AddNavalStorylineHeroesInsideMainPartyToPort(Settlement.CurrentSettlement);
		}
	}

	private void AddNavalStorylineHeroToPortAsLocationCharacter(Hero storylineHero)
	{
		Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(storylineHero.CharacterObject.Race, "_settlement");
		uint color = (uint)(((int?)storylineHero.MapFaction?.Color) ?? (-3357781));
		uint color2 = (uint)(((int?)storylineHero.MapFaction?.Color) ?? (-3357781));
		AgentData agentData = new AgentData(new SimpleAgentOrigin(storylineHero.CharacterObject)).ClothingColor1(color).ClothingColor2(color2).Monster(monsterWithSuffix)
			.NoHorses(noHorses: true);
		LocationCharacter locationCharacter = new LocationCharacter(actionSetCode: ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, storylineHero.IsFemale, "_lord"), agentData: agentData, addBehaviorsDelegate: SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, spawnTag: "sp_notable", fixedLocation: true, characterRelation: LocationCharacter.CharacterRelations.Neutral, useCivilianEquipment: true);
		LocationComplex.Current.GetLocationWithId("port").AddCharacter(locationCharacter);
	}
}
