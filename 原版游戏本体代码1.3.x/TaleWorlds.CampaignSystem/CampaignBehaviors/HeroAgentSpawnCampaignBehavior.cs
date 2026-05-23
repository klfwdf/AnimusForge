using System;
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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F3 RID: 1011
	public class HeroAgentSpawnCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003F0D RID: 16141 RVA: 0x0011C1DC File Offset: 0x0011A3DC
		public override void RegisterEvents()
		{
			CampaignEvents.PrisonersChangeInSettlement.AddNonSerializedListener(this, new Action<Settlement, FlattenedTroopRoster, Hero, bool>(this.OnPrisonersChangeInSettlement));
			CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, new Action<Town, Hero, Hero>(this.OnGovernorChanged));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
		}

		// Token: 0x06003F0E RID: 16142 RVA: 0x0011C28A File Offset: 0x0011A48A
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003F0F RID: 16143 RVA: 0x0011C28C File Offset: 0x0011A48C
		private void RefreshLocationOfHeroesForPlayersCurrentSettlement()
		{
			if (LocationComplex.Current != null && Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage) && LocationComplex.Current == Settlement.CurrentSettlement.LocationComplex)
			{
				Settlement currentSettlement = Settlement.CurrentSettlement;
				List<Hero> list = currentSettlement.HeroesWithoutParty.ToList<Hero>();
				Hero hero = (currentSettlement.MapFaction.IsKingdomFaction ? ((Kingdom)currentSettlement.MapFaction).Leader : currentSettlement.OwnerClan.Leader);
				Hero hero2 = ((hero != null) ? hero.Spouse : null);
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
				foreach (MobileParty mobileParty in currentSettlement.Parties)
				{
					if (mobileParty.LeaderHero != null && mobileParty.LeaderHero != Hero.MainHero)
					{
						list.Add(mobileParty.LeaderHero);
					}
				}
				foreach (Hero hero3 in list)
				{
					this.RefreshLocationOfHeroForSettlement(hero3, currentSettlement);
				}
			}
		}

		// Token: 0x06003F10 RID: 16144 RVA: 0x0011C438 File Offset: 0x0011A638
		private void RefreshLocationOfHeroForSettlement(Hero hero, Settlement settlement)
		{
			Location locationOfCharacter = settlement.LocationComplex.GetLocationOfCharacter(hero);
			HeroAgentLocationModel.HeroLocationDetail heroLocationDetail;
			Location locationForHero = Campaign.Current.Models.HeroAgentLocationModel.GetLocationForHero(hero, settlement, out heroLocationDetail);
			if (locationOfCharacter == null && locationForHero != null)
			{
				LocationCharacter locationCharacter = this.CreateLocationCharacterForHero(hero, settlement, heroLocationDetail);
				locationForHero.AddCharacter(locationCharacter);
				return;
			}
			if (locationOfCharacter != null && locationOfCharacter != locationForHero)
			{
				LocationCharacter locationCharacterOfHero = settlement.LocationComplex.GetLocationCharacterOfHero(hero);
				settlement.LocationComplex.ChangeLocation(locationCharacterOfHero, locationOfCharacter, locationForHero);
			}
		}

		// Token: 0x06003F11 RID: 16145 RVA: 0x0011C4A8 File Offset: 0x0011A6A8
		private void SetAgentDataProperties(Hero hero, HeroAgentLocationModel.HeroLocationDetail locationReason, ref AgentData agentData)
		{
			Monster monster = new Monster();
			if (locationReason == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || locationReason == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion)
			{
				monster = FaceGen.GetBaseMonsterFromRace(hero.CharacterObject.Race);
			}
			else
			{
				monster = FaceGen.GetMonsterWithSuffix(hero.CharacterObject.Race, "_settlement");
			}
			agentData.Monster(monster);
			agentData.NoHorses(true);
			if (locationReason != HeroAgentLocationModel.HeroLocationDetail.Wanderer)
			{
				IFaction mapFaction = hero.MapFaction;
				uint color = ((mapFaction != null) ? mapFaction.Color : 4291609515U);
				IFaction mapFaction2 = hero.MapFaction;
				uint color2 = ((mapFaction2 != null) ? mapFaction2.Color : 4291609515U);
				agentData.ClothingColor1(color).ClothingColor2(color2);
			}
		}

		// Token: 0x06003F12 RID: 16146 RVA: 0x0011C540 File Offset: 0x0011A740
		private LocationCharacter CreateLocationCharacterForHero(Hero hero, Settlement settlement, HeroAgentLocationModel.HeroLocationDetail heroLocationDetail)
		{
			AgentData agentData = null;
			if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.NobleBelongingToNoParty || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Prisoner)
			{
				agentData = new AgentData(new SimpleAgentOrigin(hero.CharacterObject, -1, null, default(UniqueTroopDescriptor)));
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion)
			{
				agentData = new AgentData(new PartyAgentOrigin(PartyBase.MainParty, hero.CharacterObject, -1, default(UniqueTroopDescriptor), false, false));
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PartyLeader)
			{
				agentData = new AgentData(new PartyAgentOrigin(hero.PartyBelongedTo.Party, hero.CharacterObject, -1, default(UniqueTroopDescriptor), false, false));
			}
			else
			{
				agentData = new AgentData(new PartyAgentOrigin(null, hero.CharacterObject, -1, default(UniqueTroopDescriptor), false, false));
			}
			this.SetAgentDataProperties(hero, heroLocationDetail, ref agentData);
			LocationCharacter.AddBehaviorsDelegate addBehaviorsDelegate = ((heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion) ? new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors) : new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors));
			string text = "";
			bool forceSpawnOnSpecialTargetTag = false;
			if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.SettlementKingQueen)
			{
				text = "sp_throne";
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Prisoner)
			{
				text = "sp_prisoner";
				forceSpawnOnSpecialTargetTag = true;
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Notable)
			{
				if (settlement.IsFortification)
				{
					text = (hero.IsArtisan ? "sp_notable_artisan" : (hero.IsMerchant ? "sp_notable_merchant" : (hero.IsPreacher ? "sp_notable_preacher" : (hero.IsGangLeader ? "sp_notable_gangleader" : (hero.IsRuralNotable ? "sp_notable_rural_notable" : ((hero.GovernorOf == hero.CurrentSettlement.Town) ? "sp_governor" : "sp_notable"))))));
					MBReadOnlyList<Workshop> ownedWorkshops = hero.OwnedWorkshops;
					if (ownedWorkshops.Count != 0)
					{
						for (int i = 0; i < ownedWorkshops.Count; i++)
						{
							if (!ownedWorkshops[i].WorkshopType.IsHidden)
							{
								text = text + "_" + ownedWorkshops[i].Tag;
								break;
							}
						}
					}
				}
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage)
			{
				text = "sp_notable_rural_notable";
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Wanderer)
			{
				text = "npc_common";
			}
			else
			{
				text = "sp_notable";
			}
			bool fixedLocation = heroLocationDetail != HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage;
			LocationCharacter.CharacterRelations characterRelation = LocationCharacter.CharacterRelations.Neutral;
			if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion)
			{
				characterRelation = LocationCharacter.CharacterRelations.Friendly;
			}
			string actionSetCode = "";
			if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.SettlementKingQueen || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.NobleBelongingToNoParty || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PartyLeader)
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_lord");
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Prisoner)
			{
				actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_villager");
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Notable)
			{
				if (settlement.IsFortification)
				{
					string suffix = (hero.IsArtisan ? "_villager_artisan" : (hero.IsMerchant ? "_villager_merchant" : (hero.IsPreacher ? "_villager_preacher" : (hero.IsGangLeader ? "_villager_gangleader" : (hero.IsRuralNotable ? "_villager_ruralnotable" : (hero.IsFemale ? "_lord" : "_villager_merchant"))))));
					actionSetCode = ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, suffix);
				}
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PartylessHeroInsideVillage)
			{
				actionSetCode = null;
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.Wanderer)
			{
				actionSetCode = ((settlement.Culture.StringId.ToLower() == "aserai" || settlement.Culture.StringId.ToLower() == "khuzait") ? ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_warrior_in_aserai_tavern") : ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, hero.IsFemale, "_warrior_in_tavern"));
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember)
			{
				actionSetCode = null;
			}
			else
			{
				Debug.FailedAssert("action Set Code is not set properly with a location reason!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\HeroAgentSpawnCampaignBehavior.cs", "CreateLocationCharacterForHero", 280);
			}
			bool useCivilianEquipment = true;
			if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion)
			{
				useCivilianEquipment = !PlayerEncounter.LocationEncounter.Settlement.IsVillage;
			}
			else if (heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PartyLeader)
			{
				useCivilianEquipment = !settlement.IsVillage;
			}
			bool isVisualTracked = heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.PlayerClanMember || heroLocationDetail == HeroAgentLocationModel.HeroLocationDetail.MainPartyCompanion;
			return new LocationCharacter(agentData, addBehaviorsDelegate, text, fixedLocation, characterRelation, actionSetCode, useCivilianEquipment, false, null, false, isVisualTracked, true, null, forceSpawnOnSpecialTargetTag);
		}

		// Token: 0x06003F13 RID: 16147 RVA: 0x0011C942 File Offset: 0x0011AB42
		private void OnGovernorChanged(Town town, Hero oldGovernor, Hero newGovernor)
		{
			if (LocationComplex.Current != null)
			{
				if (oldGovernor != null)
				{
					this.RefreshLocationOfHeroForSettlement(oldGovernor, town.Settlement);
				}
				if (newGovernor != null)
				{
					this.RefreshLocationOfHeroForSettlement(newGovernor, town.Settlement);
				}
			}
		}

		// Token: 0x06003F14 RID: 16148 RVA: 0x0011C96B File Offset: 0x0011AB6B
		private void OnMissionEnded(IMission mission)
		{
			if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && Settlement.CurrentSettlement != null && !Hero.MainHero.IsPrisoner && !Settlement.CurrentSettlement.IsUnderSiege)
			{
				this.RefreshLocationOfHeroesForPlayersCurrentSettlement();
			}
		}

		// Token: 0x06003F15 RID: 16149 RVA: 0x0011C9A0 File Offset: 0x0011ABA0
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && settlement.LocationComplex == LocationComplex.Current)
			{
				this.RefreshLocationOfHeroesForPlayersCurrentSettlement();
			}
		}

		// Token: 0x06003F16 RID: 16150 RVA: 0x0011C9C3 File Offset: 0x0011ABC3
		public void OnSettlementLeft(MobileParty mobileParty, Settlement settlement)
		{
			if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null && settlement.LocationComplex == LocationComplex.Current && mobileParty != MobileParty.MainParty && mobileParty.LeaderHero != null)
			{
				this.RefreshLocationOfHeroForSettlement(mobileParty.LeaderHero, settlement);
			}
		}

		// Token: 0x06003F17 RID: 16151 RVA: 0x0011C9FD File Offset: 0x0011ABFD
		private void OnGameLoadFinished()
		{
			if (!Hero.MainHero.IsPrisoner && Settlement.CurrentSettlement != null && !Settlement.CurrentSettlement.IsUnderSiege)
			{
				this.RefreshLocationOfHeroesForPlayersCurrentSettlement();
			}
		}

		// Token: 0x06003F18 RID: 16152 RVA: 0x0011CA24 File Offset: 0x0011AC24
		private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisoner)
		{
			if (capturerParty.IsSettlement)
			{
				this.OnPrisonersChangeInSettlement(capturerParty.Settlement, null, prisoner, false);
			}
		}

		// Token: 0x06003F19 RID: 16153 RVA: 0x0011CA40 File Offset: 0x0011AC40
		public void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
		{
			if (settlement != null && settlement.IsFortification && LocationComplex.Current == settlement.LocationComplex)
			{
				if (prisonerHero != null && prisonerHero != Hero.OneToOneConversationHero)
				{
					this.RefreshLocationOfHeroForSettlement(prisonerHero, settlement);
				}
				if (prisonerRoster != null)
				{
					foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in prisonerRoster)
					{
						if (flattenedTroopRosterElement.Troop.IsHero && prisonerHero != Hero.OneToOneConversationHero)
						{
							this.RefreshLocationOfHeroForSettlement(flattenedTroopRosterElement.Troop.HeroObject, settlement);
						}
					}
				}
			}
		}
	}
}
