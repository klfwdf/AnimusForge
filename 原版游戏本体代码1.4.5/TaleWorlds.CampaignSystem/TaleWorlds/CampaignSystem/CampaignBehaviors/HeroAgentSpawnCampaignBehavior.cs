using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class HeroAgentSpawnCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.PrisonersChangeInSettlement.AddNonSerializedListener(this, OnPrisonersChangeInSettlement);
		CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void RefreshLocationOfHeroesForPlayersCurrentSettlement()
	{
		if (LocationComplex.Current == null || Settlement.CurrentSettlement == null || (!Settlement.CurrentSettlement.IsFortification && !Settlement.CurrentSettlement.IsVillage) || LocationComplex.Current != Settlement.CurrentSettlement.LocationComplex)
		{
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		List<Hero> list = currentSettlement.HeroesWithoutParty.ToList();
		Hero hero = (currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).Leader : currentSettlement.OwnerClan.Leader);
		Hero hero2 = hero?.Spouse;
		if (hero != null)
		{
			list.Add(hero);
		}
		if (hero2 != null)
		{
			list.Add(hero2);
		}
		list.AddRange(Clan.PlayerClan.AliveLords);
		list.AddRange(Hero.MainHero.CompanionsInParty);
		list.AddRange(from x in currentSettlement.SettlementComponent.GetPrisonerHeroes()
			select x.HeroObject);
		foreach (MobileParty party in currentSettlement.Parties)
		{
			if (party.LeaderHero != null && party.LeaderHero != Hero.MainHero)
			{
				list.Add(party.LeaderHero);
			}
		}
		foreach (Hero item in list)
		{
			RefreshLocationOfHeroForSettlement(item, currentSettlement);
		}
	}

	private void RefreshLocationOfHeroForSettlement(Hero hero, Settlement settlement)
	{
		Location locationOfCharacter = settlement.LocationComplex.GetLocationOfCharacter(hero);
		HeroAgentLocationModel.HeroLocationDetail heroSpawnDetail;
		Location locationForHero = Campaign.Current.Models.HeroAgentLocationModel.GetLocationForHero(hero, settlement, out heroSpawnDetail);
		if (locationOfCharacter == null && locationForHero != null)
		{
			LocationCharacter locationCharacter = CreateLocationCharacterForHero(hero, settlement, heroSpawnDetail);
			locationForHero.AddCharacter(locationCharacter);
		}
		else if (locationOfCharacter != null && locationOfCharacter != locationForHero)
		{
			LocationCharacter locationCharacterOfHero = settlement.LocationComplex.GetLocationCharacterOfHero(hero);
			settlement.LocationComplex.ChangeLocation(locationCharacterOfHero, locationOfCharacter, locationForHero);
		}
	}

	private void SetAgentDataProperties(Hero hero, HeroAgentLocationModel.HeroLocationDetail locationReason, ref AgentData agentData)
	{
		Monster monster = new Monster();
		monster = ((locationReason != HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember && locationReason != HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion) ? FaceGen.GetMonsterWithSuffix(hero.CharacterObject.Race, "_settlement") : FaceGen.GetBaseMonsterFromRace(hero.CharacterObject.Race));
		agentData.Monster(monster);
		agentData.NoHorses(noHorses: true);
		if (locationReason != HeroAgentLocationModel.HeroLocationDetail.Wanderer)
		{
			uint color = (uint)(((int?)hero.MapFaction?.Color) ?? (-3357781));
			uint color2 = (uint)(((int?)hero.MapFaction?.Color) ?? (-3357781));
			agentData.ClothingColor1(color).ClothingColor2(color2);
		}
	}

	private LocationCharacter CreateLocationCharacterForHero(Hero hero, Settlement settlement, HeroAgentLocationModel.HeroLocationDetail heroLocationDetail)
	{
		AgentData agentData = null;
		switch (heroLocationDetail)
		{
		case HeroAgentLocationModel.HeroLocationDetail.NobleBelongingToNoParty:
		case HeroAgentLocationModel.HeroLocationDetail.Prisoner:
			agentData = new AgentData(new SimpleAgentOrigin(hero.CharacterObject));
			break;
		case HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember:
		case HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion:
			agentData = new AgentData(new PartyAgentOrigin(PartyBase.MainParty, hero.CharacterObject));
			break;
		case HeroAgentLocationModel.HeroLocationDetail.PartyLeader:
			agentData = new AgentData(new PartyAgentOrigin(hero.PartyBelongedTo.Party, hero.CharacterObject));
			break;
		default:
			agentData = new AgentData(new PartyAgentOrigin(null, hero.CharacterObject));
			break;
		}
		SetAgentDataProperties(hero, heroLocationDetail, ref agentData);
		LocationCharacter.AddBehaviorsDelegate addBehaviorsDelegate = ((heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion) ? new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors) : new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors));
		string text = "";
		bool forceSpawnOnSpecialTargetTag = false;
		switch (heroLocationDetail)
		{
		case HeroAgentLocationModel.HeroLocationDetail.SettlementKingQueen:
			text = "sp_throne";
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Prisoner:
			text = "sp_prisoner";
			forceSpawnOnSpecialTargetTag = true;
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Notable:
		{
			if (!settlement.IsFortification)
			{
				break;
			}
			text = (hero.IsArtisan ? "sp_notable_artisan" : (hero.IsMerchant ? "sp_notable_merchant" : (hero.IsPreacher ? "sp_notable_preacher" : (hero.IsGangLeader ? "sp_notable_gangleader" : (hero.IsRuralNotable ? "sp_notable_rural_notable" : ((hero.GovernorOf == hero.CurrentSettlement.Town) ? "sp_governor" : "sp_notable"))))));
			MBReadOnlyList<Workshop> ownedWorkshops = hero.OwnedWorkshops;
			if (ownedWorkshops.Count == 0)
			{
				break;
			}
			for (int i = 0; i < ownedWorkshops.Count; i++)
			{
				if (!ownedWorkshops[i].WorkshopType.IsHidden)
				{
					text = text + "_" + ownedWorkshops[i].Tag;
					break;
				}
			}
			break;
		}
		case HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage:
			text = "sp_notable_rural_notable";
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Wanderer:
			text = "npc_common";
			break;
		default:
			text = "sp_notable";
			break;
		}
		bool fixedLocation = heroLocationDetail != HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage;
		LocationCharacter.CharacterRelations characterRelation = LocationCharacter.CharacterRelations.Neutral;
		if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion)
		{
			characterRelation = LocationCharacter.CharacterRelations.Friendly;
		}
		string actionSetCode = "";
		switch (heroLocationDetail)
		{
		case HeroAgentLocationModel.HeroLocationDetail.SettlementKingQueen:
		case HeroAgentLocationModel.HeroLocationDetail.NobleBelongingToNoParty:
		case HeroAgentLocationModel.HeroLocationDetail.PartyLeader:
			actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_lord");
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Prisoner:
			actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_villager");
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Notable:
			if (settlement.IsFortification)
			{
				string text2 = null;
				text2 = (hero.IsArtisan ? "_villager_artisan" : (hero.IsMerchant ? "_villager_merchant" : (hero.IsPreacher ? "_villager_preacher" : (hero.IsGangLeader ? "_villager_gangleader" : (hero.IsRuralNotable ? "_villager_ruralnotable" : (hero.IsFemale ? "_lord" : "_villager_merchant"))))));
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, text2);
			}
			break;
		case HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage:
			actionSetCode = null;
			break;
		case HeroAgentLocationModel.HeroLocationDetail.Wanderer:
			actionSetCode = ((settlement.Culture.StringId.ToLower() == "aserai" || settlement.Culture.StringId.ToLower() == "khuzait") ? ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_warrior_in_aserai_tavern") : ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_warrior_in_tavern"));
			break;
		case HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember:
		case HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion:
			actionSetCode = null;
			break;
		default:
			Debug.FailedAssert("action Set Code is not set properly with a location reason!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroAgentSpawnCampaignBehavior.cs", "CreateLocationCharacterForHero", 280);
			break;
		}
		bool useCivilianEquipment = true;
		switch (heroLocationDetail)
		{
		case HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember:
		case HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion:
			useCivilianEquipment = !PlayerEncounter.LocationEncounter.Settlement.IsVillage;
			break;
		case HeroAgentLocationModel.HeroLocationDetail.PartyLeader:
			useCivilianEquipment = !settlement.IsVillage;
			break;
		}
		bool isVisualTracked = heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion;
		return new LocationCharacter(agentData, addBehaviorsDelegate, text, fixedLocation, characterRelation, actionSetCode, useCivilianEquipment, isFixedCharacter: false, null, isHidden: false, isVisualTracked, overrideBodyProperties: true, null, forceSpawnOnSpecialTargetTag);
	}

	private void OnGovernorChanged(Town town, Hero oldGovernor, Hero newGovernor)
	{
		if (LocationComplex.Current != null)
		{
			if (oldGovernor != null)
			{
				RefreshLocationOfHeroForSettlement(oldGovernor, town.Settlement);
			}
			if (newGovernor != null)
			{
				RefreshLocationOfHeroForSettlement(newGovernor, town.Settlement);
			}
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && !Settlement.CurrentSettlement.IsUnderSiege)
		{
			RefreshLocationOfHeroesForPlayersCurrentSettlement();
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && settlement.LocationComplex == LocationComplex.Current)
		{
			RefreshLocationOfHeroesForPlayersCurrentSettlement();
		}
	}

	public void OnSettlementLeft(MobileParty mobileParty, Settlement settlement)
	{
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && settlement.LocationComplex == LocationComplex.Current && mobileParty != MobileParty.MainParty && mobileParty.LeaderHero != null)
		{
			RefreshLocationOfHeroForSettlement(mobileParty.LeaderHero, settlement);
		}
	}

	private void OnGameLoadFinished()
	{
		if (!Hero.MainHero.IsPrisoner && Settlement.CurrentSettlement != null && !Settlement.CurrentSettlement.IsUnderSiege)
		{
			RefreshLocationOfHeroesForPlayersCurrentSettlement();
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisoner)
	{
		if (capturerParty.IsSettlement)
		{
			OnPrisonersChangeInSettlement(capturerParty.Settlement, null, prisoner, takenFromDungeon: false);
		}
	}

	public void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
	{
		if (settlement == null || !settlement.IsFortification || LocationComplex.Current != settlement.LocationComplex)
		{
			return;
		}
		if (prisonerHero != null && prisonerHero != Hero.OneToOneConversationHero)
		{
			RefreshLocationOfHeroForSettlement(prisonerHero, settlement);
		}
		if (prisonerRoster == null)
		{
			return;
		}
		foreach (FlattenedTroopRosterElement item in prisonerRoster)
		{
			if (item.Troop.IsHero && prisonerHero != Hero.OneToOneConversationHero)
			{
				RefreshLocationOfHeroForSettlement(item.Troop.HeroObject, settlement);
			}
		}
	}
}
