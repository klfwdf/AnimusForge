using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003DE RID: 990
	public class CompanionGrievanceBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003C5E RID: 15454 RVA: 0x00102B7C File Offset: 0x00100D7C
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
			CampaignEvents.VillageLooted.AddNonSerializedListener(this, new Action<Village>(this.OnVillageRaided));
			CampaignEvents.PlayerDesertedBattleEvent.AddNonSerializedListener(this, new Action<int>(this.OnPlayerDesertedBattle));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
		}

		// Token: 0x06003C5F RID: 15455 RVA: 0x00102BFC File Offset: 0x00100DFC
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Hero, CompanionGrievanceBehavior.Grievance>>("_heroGrievances", ref this._heroGrievances);
			dataStore.SyncData<CompanionGrievanceBehavior.Grievance>("_currentGrievance", ref this._currentGrievance);
			dataStore.SyncData<CampaignTime[]>("_nextGrievableTimeForComplaintType", ref this._nextGrievableTimeForComplaintType);
		}

		// Token: 0x06003C60 RID: 15456 RVA: 0x00102C34 File Offset: 0x00100E34
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("companion_start_grievance", "start", "grievance_received", "{=PVyZ9vNB}{TITLE}, there is something I wish to discuss with you.", new ConversationSentence.OnConditionDelegate(this.companion_start_grievance_condition), null, 120, null);
			campaignGameStarter.AddPlayerLine("grievance_requested", "grievance_received", "grievance_noticed", "{=m72wpzG2}Go on, I'm listening.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLineWithVariation("companion_continue_grievance_desert_softspoken", "grievance_noticed", "grievance_listened", new ConversationSentence.OnConditionDelegate(this.companion_grievance_desert_battle_condition), null, 100, "", "", "", "", null).Variation(new object[] { "{=7ldEMGn6}I don't like running away from a battle like that.", "DefaultTag", 0 }).Variation(new object[] { "{=EqLQtlca}The way we just ran from the enemy back there... I don't want to get a name for being a coward.", "PersonaSoftspokenTag", 1 })
				.Variation(new object[] { "{=sXwOLNo9}I don't like turning my back on the enemy like that. For me, death in battle is better than dishonor.", "PersonaCurtTag", 1 })
				.Variation(new object[] { "{=TnW3i5ul}We ran back there. I despise running. I prefer to be the wolf, not a rabbit.", "PersonaCurtTag", 1, "KhuzaitTag", 1 })
				.Variation(new object[] { "{=IDJbtGks}We ran back there. It is shameful to turn your back on the enemy.", "PersonaCurtTag", 1, "SturgianTag", 1 })
				.Variation(new object[] { "{=PTj1WtJx}The way we ran back there... It shames me to think of it. Next time, let us fight and die rather than let men call us cowards!", "PersonaEarnestTag", 1 })
				.Variation(new object[] { "{=UTRvswWE}As I recall, when you first hired me, it was to fight, not run away. Now, I'm sure what you did back there was sensible, but still, I've earned a bit of a reputation for bravery and I don't care to be called a coward. Those foes we can't beat - let's try to stay a little more clear of them next time, shall we?", "PersonaIronicTag", 1 })
				.Variation(new object[] { "{=v7OCHday}I suppose back there we had to run away to fight another day, as the hero Cathalac once did. If you remember the story, though, for the next three years he sat by himself in a bog, unable to look anyone else in the eye. So let's not do that too often, shall we?", "PersonaIronicTag", 1, "BattanianTag", 1 })
				.Variation(new object[] { "{=u9tAQLUf}We ran away back there. I hope word does not get around. Not looking forward to seeing the snickers and grins on people's faces the next time we walk into a tavern. Stings worse than arrows, that does.", "PersonaIronicTag", 1, "VlandianTag", 1 })
				.Variation(new object[] { "{=gfoqoGTn}The way we ran away back there... I may have told you that I hoped one day the poets would write odes about me. I had intended that they would praise my heroism, not my ability to scamper to safety.", "PersonaIronicTag", 1, "AseraiTag", 1 });
			campaignGameStarter.AddDialogLineWithVariation("companion_continue_grievance_wage_softspoken", "grievance_noticed", "grievance_listened", new ConversationSentence.OnConditionDelegate(this.companion_grievance_wage_condition), null, 100, "", "", "", "", null).Variation(new object[] { "{=yuqLzmL9}I should remind you that I expect to be paid as you had promised.", "DefaultTag", 1 }).Variation(new object[] { "{=zBfB5vw8}I hope you don't mind me saying this, but... Your men shed their blood for you. It bothers me to hear that their wages are late.", "PersonaSoftspokenTag", 1 })
				.Variation(new object[] { "{=Bhtt6XPv}Your men's wages are late. That's not the kind of company I like to fight in.", "PersonaCurtTag", 1 })
				.Variation(new object[] { "{=0mVwnCES}I must say something. Your men say their wages are late. We should take care that this doesn't happen.", "PersonaEarnestTag", 1 })
				.Variation(new object[] { "{=TzqxgiQl}The men say their wages are late. Best uphold your end of the bargain with them, or they might not keep theirs. It would especially be tricky if they chose to void their contract during a battle, wouldn't you say?", "PersonaIronicTag", 1 });
			campaignGameStarter.AddDialogLineWithVariation("companion_continue_grievance_starve_softspoken", "grievance_noticed", "grievance_listened", new ConversationSentence.OnConditionDelegate(this.companion_grievance_starve_condition), null, 100, "", "", "", "", null).Variation(new object[] { "{=IPLyqdVX}I hear we're running low on food. We should watch our stocks better.", "DefaultTag", 1 }).Variation(new object[] { "{=ITboR6C1}The men say we're running low on food. We should be more careful of that.", "PersonaSoftspokenTag", 1 })
				.Variation(new object[] { "{=HkjaCc44}Your men say there's little to eat. They march, they fight. They deserve to eat.", "PersonaCurtTag", 1 })
				.Variation(new object[] { "{=acOOsQaC}The food's running out. That's not fair to the men. We should take care that the food doesn't run out.", "PersonaEarnestTag", 1 })
				.Variation(new object[] { "{=6UKdUrPs}The men say the food's running out. We expect them to die for us if needed. Least we can do is let them die on a full belly.", "PersonaIronicTag", 1 })
				.Variation(new object[] { "{=gChji1JO}About the food... These men are ready to spill their blood for you, but there won't be much blood in their veins to shed if their bellies are empty.", "PersonaIronicTag", 1, "BattanianTag", 1 })
				.Variation(new object[] { "{=IgGQUms4}About our food situation... The general Aricaros used to say that an army marches on its stomach. Can't get far on an empty one.", "PersonaIronicTag", 1, "EmpireTag", 1 })
				.Variation(new object[] { "{=ZNaQrIaP}About our food situation... We shouldn't let the men go hungry. A man's courage comes from his stomach, they say.", "PersonaIronicTag", 1, "PersonaIronicTag", 1 });
			campaignGameStarter.AddDialogLineWithVariation("companion_continue_grievance_raid_softspoken", "grievance_noticed", "grievance_listened", new ConversationSentence.OnConditionDelegate(this.companion_grievance_raid_condition), null, 100, "", "", "", "", null).Variation(new object[] { "{=zNvjSFaC}Pillaging villages is not what I signed up for.", "DefaultTag", 1 }).Variation(new object[] { "{=bpXgcBCp}What we did to that village... I don't like it. Those farmers, they're a lot like my people. I want to know it won't happen again.", "PersonaSoftspokenTag", 1 })
				.Variation(new object[] { "{=4bkLDxIU}What we did back there, to that village... I don't do that. I want no part of it.", "PersonaCurtTag", 1 })
				.Variation(new object[] { "{=VldAzBo5}I need to say something. What we did to that village - it was wrong. They're innocent farmers and they shouldn't have their homes and fields ransacked and burned like that. I won't do that again.", "PersonaEarnestTag", 1 })
				.Variation(new object[] { "{=pDa7kOja}I know war is cruel, but I don't want to make it crueler than necessary. I'd rather not have the blood of innocents on my conscience, if you don't mind. Let's not raid villages like that.", "PersonaIronicTag", 1 });
			campaignGameStarter.AddPlayerLine("grievance_1", "grievance_listened", "close_window", "{=OVeSBrhv}Very well, I will consider this when taking such actions.", null, new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_accepted_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("grievance_2", "grievance_listened", "close_window", "{=2wmKs6Is}As your leader I am able to decide the best course of action.", null, new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("grievance_3", "grievance_listened", "close_window", "{=fzKFQuFT}Perhaps you are not suitable for this party after all.", null, new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_rejected_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("companion_repeat_grievance", "start", "grievance_repeated", "{=baeO5Zkk}{TITLE}... {GRIEVANCE_SHORT_DESCRIPTION}", new ConversationSentence.OnConditionDelegate(this.companion_repeat_grievance_condition), null, 120, null);
			campaignGameStarter.AddDialogLine("companion_grievance_repetition_desert", "grievance_repeated", "close_window", "{=!}{GRIEVANCE_REPETITION}", new ConversationSentence.OnConditionDelegate(this.companion_grievance_desert_battle_condition), new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_consequence), 100, null);
			campaignGameStarter.AddDialogLine("companion_grievance_repetition_wage", "grievance_repeated", "close_window", "{=!}{GRIEVANCE_REPETITION}", new ConversationSentence.OnConditionDelegate(this.companion_grievance_wage_condition), new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_consequence), 100, null);
			campaignGameStarter.AddDialogLine("companion_grievance_repetition_starve", "grievance_repeated", "close_window", "{=!}{GRIEVANCE_REPETITION}", new ConversationSentence.OnConditionDelegate(this.companion_grievance_starve_condition), new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_consequence), 100, null);
			campaignGameStarter.AddDialogLine("companion_grievance_repetition_raid", "grievance_repeated", "close_window", "{=!}{GRIEVANCE_REPETITION}", new ConversationSentence.OnConditionDelegate(this.companion_grievance_raid_condition), new ConversationSentence.OnConsequenceDelegate(this.companion_grievance_consequence), 100, null);
		}

		// Token: 0x06003C61 RID: 15457 RVA: 0x00103374 File Offset: 0x00101574
		private bool companion_grievance_raid_condition()
		{
			return this._currentGrievance != null && this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.VillageRaided;
		}

		// Token: 0x06003C62 RID: 15458 RVA: 0x0010338E File Offset: 0x0010158E
		private bool companion_grievance_starve_condition()
		{
			return this._currentGrievance != null && this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.Starvation;
		}

		// Token: 0x06003C63 RID: 15459 RVA: 0x001033A8 File Offset: 0x001015A8
		private bool companion_grievance_desert_battle_condition()
		{
			return this._currentGrievance != null && this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.DesertedBattle;
		}

		// Token: 0x06003C64 RID: 15460 RVA: 0x001033C2 File Offset: 0x001015C2
		private bool companion_grievance_wage_condition()
		{
			return this._currentGrievance != null && this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.NoWage;
		}

		// Token: 0x06003C65 RID: 15461 RVA: 0x001033DC File Offset: 0x001015DC
		private bool companion_start_grievance_condition()
		{
			MBTextManager.SetTextVariable("TITLE", Hero.MainHero.IsFemale ? GameTexts.FindText("str_my_lady", null) : GameTexts.FindText("str_my_lord", null), false);
			return this._currentGrievance != null && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == this._currentGrievance.GrievingHero && this._currentGrievance.Count <= 1 && !this._currentGrievance.HasBeenSettled;
		}

		// Token: 0x06003C66 RID: 15462 RVA: 0x00103458 File Offset: 0x00101658
		private bool companion_repeat_grievance_condition()
		{
			if (this._currentGrievance != null && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == this._currentGrievance.GrievingHero)
			{
				MBTextManager.SetTextVariable("TITLE", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.MainHero, true), false);
				MBTextManager.SetTextVariable("GRIEVANCE_SHORT_DESCRIPTION", "{=scJ2eVhS}What I said to you before... [default, should not appear]", false);
				if (this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.DesertedBattle)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_SHORT_DESCRIPTION", "{=1G5M9nn2}What I mentioned about running from battle...", false);
				}
				else if (this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.NoWage)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_SHORT_DESCRIPTION", "{=p78FaTqe}What I said about our wages being paid on time...", false);
				}
				else if (this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.Starvation)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_SHORT_DESCRIPTION", "{=zfPQlDbQ}What I said about our food...", false);
				}
				else if (this._currentGrievance.TypeOfGrievance == CompanionGrievanceBehavior.GrievanceType.VillageRaided)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_SHORT_DESCRIPTION", "{=pQmUIjOQ}What I said about raiding villagers...", false);
				}
				MBTextManager.SetTextVariable("GRIEVANCE_REPETITION", "{=qNSOb7pJ}Once again, this is not something I'm happy with...", false);
				if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaEarnest)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_REPETITION", "{=YWu5Xfgz}I don't feel you're taking my complaint seriously.", false);
				}
				else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaIronic)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_REPETITION", "{=wScKLt7F}Let me put things this way... It's not grown on me at all since the last time it happened.", false);
				}
				else if (Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaCurt)
				{
					MBTextManager.SetTextVariable("GRIEVANCE_REPETITION", "{=dpzbyUCa}I don't care for it any more than I did before.", false);
				}
				return Hero.OneToOneConversationHero == this._currentGrievance.GrievingHero && this._currentGrievance.Count > 1;
			}
			return false;
		}

		// Token: 0x06003C67 RID: 15463 RVA: 0x001035E4 File Offset: 0x001017E4
		private void companion_grievance_accepted_consequence()
		{
			CompanionGrievanceBehavior.Grievance value = this._heroGrievances.FirstOrDefault((KeyValuePair<Hero, CompanionGrievanceBehavior.Grievance> t) => t.Value == this._currentGrievance && t.Key == this._currentGrievance.GrievingHero).Value;
			if (value != null)
			{
				value.HasBeenSettled = true;
				if (value.Count <= 1)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, this._currentGrievance.GrievingHero, 10, true);
				}
			}
			this._currentGrievance = null;
		}

		// Token: 0x06003C68 RID: 15464 RVA: 0x00103644 File Offset: 0x00101844
		private void companion_grievance_consequence()
		{
			if (this._currentGrievance.Count > 1)
			{
				int relationChange = (this._currentGrievance.HasBeenSettled ? (-5) : (-2));
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, this._currentGrievance.GrievingHero, relationChange, true);
			}
			this._currentGrievance = null;
		}

		// Token: 0x06003C69 RID: 15465 RVA: 0x00103691 File Offset: 0x00101891
		private void companion_grievance_rejected_consequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, this._currentGrievance.GrievingHero, -15, true);
			this._currentGrievance = null;
		}

		// Token: 0x06003C6A RID: 15466 RVA: 0x001036B2 File Offset: 0x001018B2
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06003C6B RID: 15467 RVA: 0x001036BC File Offset: 0x001018BC
		private void OnHourlyTick()
		{
			if (!MobileParty.MainParty.IsCurrentlyAtSea && GameStateManager.Current.ActiveState is MapState && MobileParty.MainParty.MapEvent == null && PlayerEncounter.Current == null)
			{
				foreach (KeyValuePair<Hero, CompanionGrievanceBehavior.Grievance> keyValuePair in this._heroGrievances)
				{
					CompanionGrievanceBehavior.Grievance value = keyValuePair.Value;
					if (value.GrievingHero.PartyBelongedTo == MobileParty.MainParty && value.HaveGrievance)
					{
						this._currentGrievance = value;
						ConversationCharacterData playerCharacterData = new ConversationCharacterData(Hero.MainHero.CharacterObject, MobileParty.MainParty.Party, false, false, false, false, false, false);
						ConversationCharacterData conversationPartnerData = new ConversationCharacterData(value.GrievingHero.CharacterObject, null, false, false, false, false, false, false);
						if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
						{
							conversationPartnerData.Party = MobileParty.MainParty.Party;
							CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "", "", false);
						}
						else
						{
							CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
						}
						value.HaveGrievance = false;
						value.NextGrievanceTime = CampaignTime.DaysFromNow((float)(4 + MBRandom.RandomInt(4)));
						break;
					}
				}
			}
		}

		// Token: 0x06003C6C RID: 15468 RVA: 0x00103810 File Offset: 0x00101A10
		private void DecideCompanionGrievances(CompanionGrievanceBehavior.GrievanceType eventType)
		{
			if (this._nextGrievableTimeForComplaintType[(int)eventType].IsFuture || MobileParty.MainParty.IsCurrentlyAtSea)
			{
				return;
			}
			foreach (Hero hero in Hero.MainHero.CompanionsInParty)
			{
				CompanionGrievanceBehavior.Grievance grievance;
				this._heroGrievances.TryGetValue(hero, out grievance);
				if (grievance == null)
				{
					CompanionGrievanceBehavior.GrievanceType grievanceTypeForCompanion = this.GetGrievanceTypeForCompanion(hero, eventType);
					if (grievanceTypeForCompanion != CompanionGrievanceBehavior.GrievanceType.Invalid)
					{
						grievance = new CompanionGrievanceBehavior.Grievance(hero, CampaignTime.Now, grievanceTypeForCompanion);
						this._heroGrievances.Add(hero, grievance);
						this._nextGrievableTimeForComplaintType[(int)eventType] = CampaignTime.DaysFromNow(4f);
						break;
					}
				}
				if (grievance != null && grievance.TypeOfGrievance == eventType && !grievance.HaveGrievance && grievance.NextGrievanceTime.IsPast)
				{
					grievance.HaveGrievance = true;
					this._nextGrievableTimeForComplaintType[(int)eventType] = CampaignTime.DaysFromNow(4f);
					grievance.Count++;
					break;
				}
			}
		}

		// Token: 0x06003C6D RID: 15469 RVA: 0x00103920 File Offset: 0x00101B20
		private CompanionGrievanceBehavior.GrievanceType GetGrievanceTypeForCompanion(Hero companionHero, CompanionGrievanceBehavior.GrievanceType type)
		{
			if ((type == CompanionGrievanceBehavior.GrievanceType.DesertedBattle && companionHero.GetTraitLevel(DefaultTraits.Valor) > 0) || (type == CompanionGrievanceBehavior.GrievanceType.Starvation && companionHero.GetTraitLevel(DefaultTraits.Generosity) > 0) || (type == CompanionGrievanceBehavior.GrievanceType.NoWage && companionHero.GetTraitLevel(DefaultTraits.Generosity) > 0) || (type == CompanionGrievanceBehavior.GrievanceType.VillageRaided && companionHero.GetTraitLevel(DefaultTraits.Mercy) > 0))
			{
				return type;
			}
			return CompanionGrievanceBehavior.GrievanceType.Invalid;
		}

		// Token: 0x06003C6E RID: 15470 RVA: 0x00103978 File Offset: 0x00101B78
		private void OnPlayerDesertedBattle(int sacrificedMenCount)
		{
			this.DecideCompanionGrievances(CompanionGrievanceBehavior.GrievanceType.DesertedBattle);
		}

		// Token: 0x06003C6F RID: 15471 RVA: 0x00103984 File Offset: 0x00101B84
		private void OnVillageRaided(Village village)
		{
			PartyBase party = village.Settlement.Party;
			MapEvent mapEvent = ((party != null) ? party.MapEvent : null);
			if (mapEvent != null)
			{
				using (IEnumerator<PartyBase> enumerator = mapEvent.InvolvedParties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current == PartyBase.MainParty)
						{
							this.DecideCompanionGrievances(CompanionGrievanceBehavior.GrievanceType.VillageRaided);
						}
					}
				}
			}
		}

		// Token: 0x06003C70 RID: 15472 RVA: 0x001039F4 File Offset: 0x00101BF4
		private void OnDailyTick()
		{
			if (PartyBase.MainParty.IsStarving)
			{
				this.DecideCompanionGrievances(CompanionGrievanceBehavior.GrievanceType.Starvation);
			}
			if (MobileParty.MainParty.HasUnpaidWages > 0f)
			{
				this.DecideCompanionGrievances(CompanionGrievanceBehavior.GrievanceType.NoWage);
			}
			foreach (KeyValuePair<Hero, CompanionGrievanceBehavior.Grievance> keyValuePair in this._heroGrievances)
			{
				CompanionGrievanceBehavior.Grievance value = keyValuePair.Value;
				if (value.NextGrievanceTime.ElapsedDaysUntilNow >= 56f)
				{
					value.HasBeenSettled = false;
					value.Count = 0;
				}
			}
		}

		// Token: 0x0400124D RID: 4685
		private Dictionary<Hero, CompanionGrievanceBehavior.Grievance> _heroGrievances = new Dictionary<Hero, CompanionGrievanceBehavior.Grievance>();

		// Token: 0x0400124E RID: 4686
		private CampaignTime[] _nextGrievableTimeForComplaintType = new CampaignTime[Enum.GetValues(typeof(CompanionGrievanceBehavior.GrievanceType)).Length];

		// Token: 0x0400124F RID: 4687
		private CompanionGrievanceBehavior.Grievance _currentGrievance;

		// Token: 0x04001250 RID: 4688
		private const int BaseGrievanceFrequencyInDays = 4;

		// Token: 0x04001251 RID: 4689
		private const int GrievanceObsolescenceDurationInDays = 56;

		// Token: 0x04001252 RID: 4690
		private const int GrievanceCooldownForSameTypeInDays = 1;

		// Token: 0x020007C8 RID: 1992
		public class CompanionGrievanceBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006291 RID: 25233 RVA: 0x001BB41D File Offset: 0x001B961D
			public CompanionGrievanceBehaviorTypeDefiner()
				: base(80000)
			{
			}

			// Token: 0x06006292 RID: 25234 RVA: 0x001BB42A File Offset: 0x001B962A
			protected override void DefineEnumTypes()
			{
				base.AddEnumDefinition(typeof(CompanionGrievanceBehavior.GrievanceType), 1, null);
			}

			// Token: 0x06006293 RID: 25235 RVA: 0x001BB43E File Offset: 0x001B963E
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(CompanionGrievanceBehavior.Grievance), 10, null);
			}

			// Token: 0x06006294 RID: 25236 RVA: 0x001BB453 File Offset: 0x001B9653
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<Hero, CompanionGrievanceBehavior.Grievance>));
			}
		}

		// Token: 0x020007C9 RID: 1993
		internal class Grievance
		{
			// Token: 0x1700151B RID: 5403
			// (get) Token: 0x06006295 RID: 25237 RVA: 0x001BB465 File Offset: 0x001B9665
			// (set) Token: 0x06006296 RID: 25238 RVA: 0x001BB46D File Offset: 0x001B966D
			[SaveableProperty(4)]
			public bool HaveGrievance { get; set; }

			// Token: 0x06006297 RID: 25239 RVA: 0x001BB476 File Offset: 0x001B9676
			public Grievance(Hero hero, CampaignTime time, CompanionGrievanceBehavior.GrievanceType type)
			{
				this.GrievingHero = hero;
				this.NextGrievanceTime = time;
				this.TypeOfGrievance = type;
				this.HasBeenSettled = false;
				this.Count = 1;
				this.HaveGrievance = true;
			}

			// Token: 0x06006298 RID: 25240 RVA: 0x001BB4A8 File Offset: 0x001B96A8
			internal static void AutoGeneratedStaticCollectObjectsGrievance(object o, List<object> collectedObjects)
			{
				((CompanionGrievanceBehavior.Grievance)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06006299 RID: 25241 RVA: 0x001BB4B6 File Offset: 0x001B96B6
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.GrievingHero);
				CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(this.NextGrievanceTime, collectedObjects);
			}

			// Token: 0x0600629A RID: 25242 RVA: 0x001BB4D5 File Offset: 0x001B96D5
			internal static object AutoGeneratedGetMemberValueHaveGrievance(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).HaveGrievance;
			}

			// Token: 0x0600629B RID: 25243 RVA: 0x001BB4E7 File Offset: 0x001B96E7
			internal static object AutoGeneratedGetMemberValueGrievingHero(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).GrievingHero;
			}

			// Token: 0x0600629C RID: 25244 RVA: 0x001BB4F4 File Offset: 0x001B96F4
			internal static object AutoGeneratedGetMemberValueNextGrievanceTime(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).NextGrievanceTime;
			}

			// Token: 0x0600629D RID: 25245 RVA: 0x001BB506 File Offset: 0x001B9706
			internal static object AutoGeneratedGetMemberValueTypeOfGrievance(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).TypeOfGrievance;
			}

			// Token: 0x0600629E RID: 25246 RVA: 0x001BB518 File Offset: 0x001B9718
			internal static object AutoGeneratedGetMemberValueHasBeenSettled(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).HasBeenSettled;
			}

			// Token: 0x0600629F RID: 25247 RVA: 0x001BB52A File Offset: 0x001B972A
			internal static object AutoGeneratedGetMemberValueCount(object o)
			{
				return ((CompanionGrievanceBehavior.Grievance)o).Count;
			}

			// Token: 0x04001F0D RID: 7949
			[SaveableField(1)]
			public Hero GrievingHero;

			// Token: 0x04001F0E RID: 7950
			[SaveableField(2)]
			public CampaignTime NextGrievanceTime;

			// Token: 0x04001F0F RID: 7951
			[SaveableField(3)]
			public CompanionGrievanceBehavior.GrievanceType TypeOfGrievance;

			// Token: 0x04001F11 RID: 7953
			[SaveableField(5)]
			public bool HasBeenSettled;

			// Token: 0x04001F12 RID: 7954
			[SaveableField(6)]
			public int Count;
		}

		// Token: 0x020007CA RID: 1994
		internal enum GrievanceType
		{
			// Token: 0x04001F14 RID: 7956
			Invalid,
			// Token: 0x04001F15 RID: 7957
			NoWage,
			// Token: 0x04001F16 RID: 7958
			Starvation,
			// Token: 0x04001F17 RID: 7959
			VillageRaided,
			// Token: 0x04001F18 RID: 7960
			DesertedBattle
		}
	}
}
