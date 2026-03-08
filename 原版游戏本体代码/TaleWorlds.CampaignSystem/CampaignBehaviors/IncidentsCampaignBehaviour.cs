using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000402 RID: 1026
	public class IncidentsCampaignBehaviour : CampaignBehaviorBase, INonReadyObjectHandler
	{
		// Token: 0x06003FBC RID: 16316 RVA: 0x0011FC6C File Offset: 0x0011DE6C
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
			CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, new Action<GameMenu, GameMenuOption>(this.OnGameMenuOptionSelected));
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.ConversationEnded.AddNonSerializedListener(this, new Action<IEnumerable<CharacterObject>>(this.ConversationEnded));
			CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeirSelectionOver));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
			CampaignEvents.OnIncidentResolvedEvent.AddNonSerializedListener(this, new Action<Incident>(this.OnIncidentResolved));
		}

		// Token: 0x06003FBD RID: 16317 RVA: 0x0011FD5F File Offset: 0x0011DF5F
		private void OnIncidentResolved(Incident incident)
		{
			this._incidentsOnCooldown.Add(incident, CampaignTime.Now);
			this._lastGlobalIncidentCooldown = CampaignTime.Now + this.GetCooldownTime();
		}

		// Token: 0x06003FBE RID: 16318 RVA: 0x0011FD88 File Offset: 0x0011DF88
		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty == MobileParty.MainParty && Campaign.Current.CurrentMenuContext == null)
			{
				this._canInvokeSettlementEvent = true;
			}
		}

		// Token: 0x06003FBF RID: 16319 RVA: 0x0011FDA5 File Offset: 0x0011DFA5
		private void OnSettlementLeft(MobileParty mobileParty, Settlement settlement)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				this._canInvokeSettlementEvent = false;
			}
		}

		// Token: 0x06003FC0 RID: 16320 RVA: 0x0011FDB6 File Offset: 0x0011DFB6
		private void OnNewGameCreated(CampaignGameStarter obj)
		{
			this._lastGlobalIncidentCooldown = CampaignTime.Now + this.GetCooldownTime();
		}

		// Token: 0x06003FC1 RID: 16321 RVA: 0x0011FDCE File Offset: 0x0011DFCE
		void INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()
		{
			this.InitializeIncidents();
		}

		// Token: 0x06003FC2 RID: 16322 RVA: 0x0011FDD8 File Offset: 0x0011DFD8
		private void ConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter && MobileParty.MainParty.CurrentSettlement == null && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
			{
				this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter);
			}
		}

		// Token: 0x06003FC3 RID: 16323 RVA: 0x0011FE38 File Offset: 0x0011E038
		private void OnMapEventEnded(MapEvent evt)
		{
			if (!evt.IsPlayerMapEvent || evt.IsNavalMapEvent)
			{
				return;
			}
			if (!evt.HasWinner || evt.DefeatedSide == evt.PlayerSide)
			{
				return;
			}
			if ((evt.IsFieldBattle || evt.IsHideoutBattle) && !evt.AttackerSide.IsSurrendered && !evt.DefenderSide.IsSurrendered && MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
			{
				this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle);
			}
		}

		// Token: 0x06003FC4 RID: 16324 RVA: 0x0011FED0 File Offset: 0x0011E0D0
		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (!this._canInvokeSettlementEvent)
			{
				return;
			}
			if (MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
			{
				if (args.MenuContext.GameMenu.StringId == "town")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown);
					this._canInvokeSettlementEvent = false;
				}
				if (args.MenuContext.GameMenu.StringId == "village")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.EnteringVillage);
					this._canInvokeSettlementEvent = false;
				}
				if (args.MenuContext.GameMenu.StringId == "castle")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.EnteringCastle);
					this._canInvokeSettlementEvent = false;
				}
			}
		}

		// Token: 0x06003FC5 RID: 16325 RVA: 0x0011FFA0 File Offset: 0x0011E1A0
		private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption option)
		{
			if (MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks) < Campaign.Current.Models.IncidentModel.GetIncidentTriggerGlobalProbability())
			{
				if (gameMenu.StringId == "town" && option.IdString == "town_leave")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement);
				}
				if (gameMenu.StringId == "castle" && option.IdString == "leave")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle | IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement);
				}
				if (gameMenu.StringId == "village" && option.IdString == "leave")
				{
					this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage | IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement);
				}
			}
		}

		// Token: 0x06003FC6 RID: 16326 RVA: 0x00120062 File Offset: 0x0011E262
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Incident, CampaignTime>>("_incidentsOnCooldown", ref this._incidentsOnCooldown);
			dataStore.SyncData<CampaignTime>("_lastGlobalIncidentCooldown", ref this._lastGlobalIncidentCooldown);
			dataStore.SyncData<long>("_activeIncidentSeed", ref this._activeIncidentSeed);
		}

		// Token: 0x06003FC7 RID: 16327 RVA: 0x0012009A File Offset: 0x0011E29A
		private void OnHeirSelectionOver(Hero obj)
		{
			this._lastGlobalIncidentCooldown = CampaignTime.Now + CampaignTime.Hours(1f);
			this._incidentsOnCooldown.Clear();
		}

		// Token: 0x06003FC8 RID: 16328 RVA: 0x001200C4 File Offset: 0x0011E2C4
		private void OnHourlyTick()
		{
			float num = MobileParty.MainParty.RandomFloatWithSeed((uint)CampaignTime.Now.NumTicks);
			if (MobileParty.MainParty.SiegeEvent != null && num < Campaign.Current.Models.IncidentModel.GetIncidentTriggerProbabilityDuringSiege())
			{
				this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege);
				return;
			}
			if (Campaign.Current.CurrentMenuContext != null && (Campaign.Current.CurrentMenuContext.GameMenu.StringId == "town_wait_menus" || Campaign.Current.CurrentMenuContext.GameMenu.StringId == "village_wait_menus") && num < Campaign.Current.Models.IncidentModel.GetIncidentTriggerProbabilityDuringWait())
			{
				this.TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger.WaitingInSettlement);
			}
		}

		// Token: 0x06003FC9 RID: 16329 RVA: 0x00120188 File Offset: 0x0011E388
		private void TryInvokeIncident(IncidentsCampaignBehaviour.IncidentTrigger trigger)
		{
			if (((trigger & IncidentsCampaignBehaviour.IncidentTrigger.EnteringCastle) != (IncidentsCampaignBehaviour.IncidentTrigger)0 || (trigger & IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown) != (IncidentsCampaignBehaviour.IncidentTrigger)0 || (trigger & IncidentsCampaignBehaviour.IncidentTrigger.EnteringVillage) != (IncidentsCampaignBehaviour.IncidentTrigger)0) && (MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement.IsSettlementBusy(this)))
			{
				return;
			}
			if (((trigger & IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown) != (IncidentsCampaignBehaviour.IncidentTrigger)0 || (trigger & IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage) != (IncidentsCampaignBehaviour.IncidentTrigger)0 || (trigger & IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement) != (IncidentsCampaignBehaviour.IncidentTrigger)0) && MobileParty.MainParty.LastVisitedSettlement.IsSettlementBusy(this))
			{
				return;
			}
			if (Hero.MainHero.IsPrisoner || Campaign.Current.ConversationManager.IsConversationFlowActive)
			{
				return;
			}
			if (this._lastGlobalIncidentCooldown.IsPast)
			{
				this.CheckIncidentsOnCooldown();
				this._activeIncidentSeed = CampaignTime.Now.NumTicks;
				IReadOnlyList<Incident> occurableEventsForTrigger = this.GetOccurableEventsForTrigger(trigger);
				if (occurableEventsForTrigger.Count > 0)
				{
					Incident randomElement = occurableEventsForTrigger.GetRandomElement<Incident>();
					this.InvokeIncident(randomElement);
				}
			}
		}

		// Token: 0x06003FCA RID: 16330 RVA: 0x00120254 File Offset: 0x0011E454
		private CampaignTime GetCooldownTime()
		{
			CampaignTime minGlobalCooldownTime = Campaign.Current.Models.IncidentModel.GetMinGlobalCooldownTime();
			CampaignTime maxGlobalCooldownTime = Campaign.Current.Models.IncidentModel.GetMaxGlobalCooldownTime();
			return CampaignTime.Hours(MBRandom.RandomFloatRanged((float)minGlobalCooldownTime.ToHours, (float)maxGlobalCooldownTime.ToHours));
		}

		// Token: 0x06003FCB RID: 16331 RVA: 0x001202A8 File Offset: 0x0011E4A8
		private void CheckIncidentsOnCooldown()
		{
			List<Incident> list = new List<Incident>();
			foreach (KeyValuePair<Incident, CampaignTime> keyValuePair in this._incidentsOnCooldown)
			{
				if (keyValuePair.Value + keyValuePair.Key.Cooldown <= CampaignTime.Now)
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (Incident key in list)
			{
				this._incidentsOnCooldown.Remove(key);
			}
		}

		// Token: 0x06003FCC RID: 16332 RVA: 0x00120370 File Offset: 0x0011E570
		private IReadOnlyList<Incident> GetOccurableEventsForTrigger(IncidentsCampaignBehaviour.IncidentTrigger trigger)
		{
			List<Incident> list = new List<Incident>();
			foreach (Incident incident in MBObjectManager.Instance.GetObjectTypeList<Incident>())
			{
				if (!this._incidentsOnCooldown.ContainsKey(incident) && (incident.Trigger & trigger) != (IncidentsCampaignBehaviour.IncidentTrigger)0 && incident.CanIncidentBeInvoked())
				{
					list.Add(incident);
				}
			}
			return list;
		}

		// Token: 0x06003FCD RID: 16333 RVA: 0x001203F0 File Offset: 0x0011E5F0
		private void InvokeIncident(Incident incident)
		{
			MapState mapState = GameStateManager.Current.LastOrDefault<MapState>();
			if (mapState != null)
			{
				mapState.NextIncident = incident;
			}
		}

		// Token: 0x06003FCE RID: 16334 RVA: 0x00120412 File Offset: 0x0011E612
		private Incident RegisterIncident(string id, string title, string description, IncidentsCampaignBehaviour.IncidentTrigger trigger, IncidentsCampaignBehaviour.IncidentType type, CampaignTime cooldown, Func<TextObject, bool> condition)
		{
			Incident incident = Game.Current.ObjectManager.RegisterPresumedObject<Incident>(new Incident(id));
			incident.Initialize(title, description, trigger, type, cooldown, condition);
			return incident;
		}

		// Token: 0x06003FCF RID: 16335 RVA: 0x0012043C File Offset: 0x0011E63C
		private void InitializeIncidents()
		{
			Incident incident = this.RegisterIncident("incident_rooster_theft", "{=0EG0iB9p}Rooster theft", "{=8zFwYZeQ}As you ride off, a villager runs after you accusing one of your men of stealing a rooster. A few squawks silence the culprit's attempt to deny the crime. Honor requires you to uphold the laws of the land, but your men expect a generous {RANK} to value those who shed blood for {PRONOUN} over those who do not.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
			{
				description.SetTextVariable("RANK", Hero.MainHero.IsLord ? "lord" : "captain");
				description.SetTextVariable("PRONOUN", Hero.MainHero.IsFemale ? "her" : "him");
				return PartyBase.MainParty.MemberRoster.TotalRegulars >= 25;
			});
			string text161 = "{=0k4T7dzu}Return the rooster and have your man whipped.";
			List<IncidentEffect> list = new List<IncidentEffect>();
			list.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -50));
			list.Add(IncidentEffect.MoraleChange(-5f));
			list.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10));
			incident.AddOption(text161, list, null, null);
			string text2 = "{=Ppzvp7Mu}Tell the peasant that the rooster is just part of the debt they owe their protectors.";
			List<IncidentEffect> list2 = new List<IncidentEffect>();
			list2.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			incident.AddOption(text2, list2, null, null);
			string text3 = "{=NdcsYWrF}Throw the peasant a few silvers in compensation.";
			List<IncidentEffect> list3 = new List<IncidentEffect>();
			list3.Add(IncidentEffect.GoldChange(() => -10));
			list3.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list3.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			incident.AddOption(text3, list3, null, null);
			Incident incident2 = this.RegisterIncident("incident_suitable_boy", "{=7y4Fbx8K}A Suitable Boy", "{=jB4tmY9W}As you leave the village, a young woman approaches you carrying a months-old baby, surrounded by her relatives. She points at one of your men and says that he was the father, and that he should stay here and wed her. The lad, a strong fellow but not good with words, is too flustered to speak. You can't exactly recall if you passed through here a year ago or not.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 30);
			Incident incident3 = incident2;
			string text4 = "{=ThnC13fF}Tell the lad to search his conscience and do what is right, under the eyes of Heaven";
			List<IncidentEffect> list4 = new List<IncidentEffect>();
			list4.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			List<IncidentEffect> list5 = list4;
			IncidentEffect[] array = new IncidentEffect[2];
			array[0] = IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1);
			array[1] = IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10);
			list5.Add(IncidentEffect.Group(array).WithChance(0.5f));
			incident3.AddOption(text4, list4, null, null);
			Incident incident4 = incident2;
			string text5 = "{=bjDjwN9r}Congratulate the lad on finding a wife and throw the couple a purse of coins as a wedding gift";
			List<IncidentEffect> list6 = new List<IncidentEffect>();
			list6.Add(IncidentEffect.GoldChange(() => -50));
			list6.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list6.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1));
			incident4.AddOption(text5, list6, null, null);
			Incident incident5 = incident2;
			string text6 = "{=sb9zWc3I}Give your blessing to the marriage and hurl huge handfuls of coins to the villagers, declaring that this will be a day that all shall remember.";
			List<IncidentEffect> list7 = new List<IncidentEffect>();
			list7.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list7.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list7.Add(IncidentEffect.GoldChange(() => -1000));
			list7.Add(IncidentEffect.RenownChange(5f));
			incident5.AddOption(text6, list7, null, null);
			incident2.AddOption("{=eP8MrIEE}Tell the villagers to get themselves a plough horse instead of trying to poach one of your men", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.MoraleChange(5f)
			}, null, null);
			Incident incident6 = this.RegisterIncident("incident_spoiled_food_supplies", "{=5aPGeeDf}Spoiled Food Supplies", "{=RsWbJJS7}A delegation of your troops approach you. They show you a heel of bread from your stores, crawling with maggots. A piece of dried meat is streaked with mold. Your quartermaster appears to have packed the goods carelessly, allowing moisture and pests to get inside.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FoodConsumption, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 10);
			incident6.AddOption("{=p0gqO74k}Money is tight and you've eaten worse in your time. Tell the lads to think of it as seasoning", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.WoundTroopsRandomly(0.05f).WithChance(0.5f)
			}, null, null);
			string text7 = "{=DcaV3yt9}Order your men to throw away the spoiled food";
			List<IncidentEffect> list8 = new List<IncidentEffect>();
			list8.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list8.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetSpoiledFoodItem|25_18), () => -2));
			incident6.AddOption(text7, list8, null, null);
			incident6.AddOption("{=YbqueBpd}Punish your quartermaster for storing it improperly, and take the money to replace it out of his pay. Discipline must prevail", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.MoraleChange(5f)
			}, null, null);
			incident6.AddOption("{=fxfInKgI}Wolf down the food yourself and smack your lips, telling the men that they are lucky to have such fine rations.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.TraitChange(DefaultTraits.Valor, 50),
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.HealthChance(-10).WithChance(0.5f)
			}, null, null);
			Incident incident7 = this.RegisterIncident("incident_desperate_times", "{=GNdltUJj}Desperate Times", "{=CZamxmfc}As you pass through the town gates, you are thronged by a group of refugees from the countryside. They are gaunt, with sunken eyes. They say they were unable to sow their fields this year for fear of bandits, and were forced to eat their seed grain. They ask for any scraps you can offer.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.PlightOfCivilians, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.IsTown)
				{
					return MobileParty.MainParty.LastVisitedSettlement.Town.Villages.Any((Village x) => x.VillageState == Village.VillageStates.Looted);
				}
				return false;
			});
			string text8 = "{=BjUtGJg6}Break out your food and allow them to fill their bellies before your depart";
			List<IncidentEffect> list9 = new List<IncidentEffect>();
			list9.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list9.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetDesperateTimesFoodItem|25_22), () => -1));
			incident7.AddOption(text8, list9, null, null);
			string text9 = "{=JO6D30FU}Give them sufficient money to replace their seed grain, even though the townspeople may complain that it raises prices";
			List<IncidentEffect> list10 = new List<IncidentEffect>();
			list10.Add(IncidentEffect.GoldChange(() => -200));
			list10.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list10.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			incident7.AddOption(text9, list10, null, null);
			incident7.AddOption("{=BslqBNQj}Tell them that you cannot help", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) }, null, null);
			Incident incident8 = this.RegisterIncident("incident_no_country_for_the_afflicted", "{=rflKyqOT}No Country for the Afflicted", "{=NORgVfas}As you prepare to leave, a delegation of townspeople approach you. A group of lepers has encamped before the walls, and they beg passers-by for alms. The townspeople are worried that they might spread the disease and, at any rate, are frightening away potential customers.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.PlightOfCivilians, CampaignTime.Days(60f), (TextObject description) => true);
			string text10 = "{=yfsqkUcb}Tell the townspeople that Heaven wishes to test their compassion, and that they should treat the lepers with kindness.";
			List<IncidentEffect> list11 = new List<IncidentEffect>();
			list11.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list11.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			list11.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10));
			incident8.AddOption(text10, list11, null, null);
			string text11 = "{=z6vCG954}Give the lepers some money and food on the condition that they try to find a more welcoming town somewhere else";
			List<IncidentEffect> list12 = new List<IncidentEffect>();
			list12.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list12.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list12.Add(IncidentEffect.GoldChange(() => -100));
			incident8.AddOption(text11, list12, null, null);
			string text12 = "{=egSS7oyV}Drive the lepers away, telling them they must have sinned grievously to be so afflicted";
			List<IncidentEffect> list13 = new List<IncidentEffect>();
			list13.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list13.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10));
			incident8.AddOption(text12, list13, null, null);
			incident8.AddOption("{=zcMJEpHC}Don't get involved", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, -100) }, null, null);
			Incident incident9 = this.RegisterIncident("incident_veterans_demand_privileges", "{=j8Aq3lRW}Veterans demand privileges", "{=FbKc1AwE}Several of your newer recruits approach you. Your {HIGH_TIER_TROOPS} have been demanding the freshest cuts of meat and highest quality bread from your stores, letting them make do with gristle and stale crusts. The veterans say that this is the privilege of experience, and that it will motivate your recruits to improve themselves.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				CharacterObject characterObject = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetVeteranMaxTierTroop|25_35();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("HIGH_TIER_TROOPS", characterObject.Name);
				return PartyBase.MainParty.ItemRoster.Any((ItemRosterElement x) => !x.IsEmpty && x.EquipmentElement.Item == DefaultItems.Meat);
			});
			string text13 = "{=56hS9BRG}Admonish the {HIGH_TIER_TROOPS} for their indiscipline, even though they will be humiliated and may even desert";
			List<IncidentEffect> list14 = new List<IncidentEffect>();
			list14.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list14.Add(IncidentEffect.MoraleChange(5f));
			list14.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetVeteranMaxTierTroop|25_35), -1).WithChance(0.5f));
			incident9.AddOption(text13, list14, delegate(TextObject text)
			{
				CharacterObject characterObject = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetVeteranMaxTierTroop|25_35();
				text.SetTextVariable("HIGH_TIER_TROOPS", characterObject.Name);
				return true;
			}, null);
			incident9.AddOption("{=EAu32UBG}Tell your recruits that your veterans have indeed earned the right to the best food", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
			}, null, null);
			string text14 = "{=37YTPzfG}Tell your men that they have all earned the right to your esteem, and break out an extra ration of {FOOD_OR_DRINK_ITEM} to show your appreciation";
			List<IncidentEffect> list15 = new List<IncidentEffect>();
			list15.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list15.Add(IncidentEffect.MoraleChange(5f));
			list15.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetVeteranFoodItem|25_34), () => -1));
			incident9.AddOption(text14, list15, delegate(TextObject text)
			{
				ItemObject itemObject = this.<InitializeIncidents>g__GetVeteranFoodItem|25_34();
				text.SetTextVariable("FOOD_OR_DRINK_ITEM", itemObject.Name);
				return true;
			}, null);
			incident9.AddOption("{=USdND5SY}You propose a compromise - a slightly better ration for veteran troops - knowing it will avoid a fight but come across as indecisive.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
			}, null, null);
			Incident incident10 = this.RegisterIncident("incident_an_affair_of_honor", "{=gUKUisS6}An Affair of Honor", "{=akvIjedh}Two of your cavalrymen request permission to fight a duel. Both had been wooing a merchant's daughter in {TOWN}, and one slighted the other in front of her. Some commanders encourage their men to duel as a boost to their fiery sense of pride, but others think that such fights erode their discipline as soldiers and, of course, carry the risk of injury.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				bool flag = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
					where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero
					select x).Sum((TroopRosterElement x) => x.Number) >= 5;
				bool flag2 = MobileParty.MainParty.MemberRoster.GetTroopRoster().Count((TroopRosterElement x) => x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero) >= 2;
				if (!flag || !flag2)
				{
					return false;
				}
				description.SetTextVariable("TOWN", MobileParty.MainParty.LastVisitedSettlement.Name);
				return true;
			});
			incident10.AddOption("{=24pUil64}You like that fiery spirit! Let them fight until one falls and cannot rise", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.PartyExperienceChance(200),
				IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetFirstNobleTroop|25_44), 1).WithChance(0.5f),
				IncidentEffect.KillTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetSecondNobleTroop|25_45), 1).WithChance(0.5f)
			}, null, null);
			incident10.AddOption("{=37lEhqtv}Let them fight until the first drop of blood is shed", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.PartyExperienceChance(100),
				IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetFirstNobleTroop|25_44), 1).WithChance(0.5f),
				IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetSecondNobleTroop|25_45), 1).WithChance(0.5f)
			}, null, null);
			incident10.AddOption("{=cexRoZPF}You will allow no such foolishness. Save their anger for the enemy", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			incident10.AddOption("{=ZYopfLDg}Tell your men to throw a die, leaving the matter to fate.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, -100) }, null, null);
			Incident incident11 = this.RegisterIncident("incident_troops_fight_over_insult", "{=4mC7AIPk}Troops Fight over Insult", "{=HpkqVXQ3}A fight breaks out in your camp. A high-born {CAVALRYMAN} demanded a {FOOTMAN} water his horse, even though the {FOOTMAN} is a veteran nearly twice the {CAVALRYMAN}'s age. The two then exchanged insults, and now the {FOOTMAN} demands a duel while the {CAVALRYMAN}, who is only willing to duel his social equal, demands the {FOOTMAN} be told to watch his place.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetInsultCavalryman|25_54();
				CharacterObject characterObject2 = this.<InitializeIncidents>g__GetInsultFootman|25_55();
				if (characterObject2 == null || characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("CAVALRYMAN", characterObject.Name);
				description.SetTextVariable("FOOTMAN", characterObject2.Name);
				return MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopsPredicate|25_52)) && MobileParty.MainParty.MemberRoster.GetTroopRoster().Any(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__FootmenTroopsPredicate|25_53));
			});
			string text15 = "{=QGcozvvK}Only blood can wipe out an insult! Let the two have it out, until one falls and cannot rise";
			List<IncidentEffect> list16 = new List<IncidentEffect>();
			list16.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 200));
			list16.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list16.Add(IncidentEffect.Select(IncidentEffect.Select(IncidentEffect.KillTroopsRandomly(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__FootmenTroopsPredicate|25_53), () => 1), IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetInsultCavalryman|25_54), 1), 0.5f), IncidentEffect.Select(IncidentEffect.KillTroopsRandomly(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopsPredicate|25_52), () => 1), IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetInsultFootman|25_55), 1), 0.5f), 0.5f));
			list16.Add(IncidentEffect.PartyExperienceChance(200));
			incident11.AddOption(text15, list16, null, null);
			string text16 = "{=HbQDtv60}Reprimand the cavalryman, even though he is fiery and might desert";
			List<IncidentEffect> list17 = new List<IncidentEffect>();
			list17.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, -100));
			list17.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list17.Add(IncidentEffect.MoraleChange(5f));
			list17.Add(IncidentEffect.KillTroopsRandomly(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopsPredicate|25_52), () => 1).WithChance(0.25f));
			incident11.AddOption(text16, list17, null, null);
			incident11.AddOption("{=7YCEpRij}Reprimand the footman, who you know will grumble but will have the sense to swallow his pride", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident11.AddOption("{=OGDApTnl}Tell them both to stop behaving like fools", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			Incident incident12 = this.RegisterIncident("incident_arrow_proofing", "{=JKO2Rmlr}Arrow-proofing", "{=ZCqZvb7K}As you leave the village, one of your soldiers proudly shows you a magic oil he bought from a peddlar, who told him that, if he annoints himself with it, it will prevent him from being wounded in battle. ", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident12.AddOption("{=CNYKy7q3}Congratulate the soldier on his wise purchase, and tell him that you expect to see him in the thick of the fray in the next battle", new List<IncidentEffect>
			{
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			incident12.AddOption("{=CXZxDvry}Tell your soldiers they should rely on their own strength instead of magic", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) }, null, null);
			incident12.AddOption("{=NHwt84uk}Chastise your man for being so gullible", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			Incident incident13 = this.RegisterIncident("incident_purebred_horse", "{=eWh4BDkx}Purebred Horse", "{=J9HLJpk7}As you leave the village, one of your cavalry recruits proudly presents to you a new horse he purchased. Maybe it's not the fastest beast on four legs, he says, but it's strong and spirited - he saw it fighting another of the trader's horses. You run an eye over the beast and see that your man was sold a mule, its ears cleverly cropped to resemble a horse's. The creature is strong enough but far too stubborn to serve as a cavalry mount. You know that your man will be the laughingstock of your company for weeks.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent));
			string text17 = "{=DHcl6JU5}Give him one of your own horses, add the mule to your pack train, and tell him to talk to you before he makes any more purchases";
			List<IncidentEffect> list18 = new List<IncidentEffect>();
			list18.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetPurebredHorseItem|25_61), () => -1));
			list18.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("mule"), () => 1));
			list18.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			incident13.AddOption(text17, list18, null, null);
			string text18 = "{=3uHIXyDk}Your men could use a laugh. Let the recruit show off his prize to his mates and take the humiliation, even if it's enough to drive him to desert";
			List<IncidentEffect> list19 = new List<IncidentEffect>();
			list19.Add(IncidentEffect.MoraleChange(5f));
			list19.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character.HasMount() && !x.Character.IsHero && !x.Character.IsHero, () => 1).WithChance(0.25f));
			incident13.AddOption(text18, list19, null, null);
			incident13.AddOption("{=HXVVJYQr}Tell him curtly what he did and that he needs to go get his old mount back from the trader, even if it costs him a few denars", new List<IncidentEffect>
			{
				IncidentEffect.MoraleChange(-2f),
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100)
			}, null, null);
			Incident incident14 = this.RegisterIncident("incident_veteran_mentor", "{=1jmYaNxu}Veteran Mentor", "{=CObK3sDa}As you leave the village, a greybeard walks up to you carrying a cudgel. Although he's too old to fight, he says, he was a warrior once and claims to know a trick move, a way of suddenly closing with your opponent and tripping them with a spear, which he offers to teach to your troops for a price. He shows you his battle-scars, which are indeed impressive.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => true);
			incident14.AddOption("{=aME3jqrO}Tell him that tricks don't win battles, but bravery does.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.MoraleChange(5f)
			}, null, null);
			string text19 = "{=43GeTG0K}Accept, even if it costs you time, recognizing that a warrior who survived so many battles probably knows a thing or two.";
			List<IncidentEffect> list20 = new List<IncidentEffect>();
			list20.Add(IncidentEffect.GoldChange(() => -500));
			list20.Add(IncidentEffect.PartyExperienceChance(200));
			list20.Add(IncidentEffect.DisorganizeParty());
			incident14.AddOption(text19, list20, null, null);
			string text20 = "{=JvUb1wr4}Accept, and tell him to demonstrate his technique with a sword on one of your men, so that the lesson sticks.";
			List<IncidentEffect> list21 = new List<IncidentEffect>();
			list21.Add(IncidentEffect.GoldChange(() => -500));
			list21.Add(IncidentEffect.PartyExperienceChance(400));
			list21.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list21.Add(IncidentEffect.WoundTroopsRandomly(1));
			incident14.AddOption(text20, list21, null, null);
			Incident incident15 = this.RegisterIncident("incident_horsefly_plague", "{=S4YyXUPU}Horsefly Plague", "{=NJ0BBaaC}One of your cavalry commanders reported that some horses seem mysteriously weak. You see their listless eyes and swollen bellies and recognize it - swamp fever. This plague is spread by horseflies, and it will infect other animals if the horse is not killed", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.AnimalIllness, CampaignTime.Days(60f), (TextObject description) => (from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.HasHorseComponent
				select x).Sum((ItemRosterElement x) => x.Amount) >= 4);
			incident15.AddOption("{=SIVBGe7H}Isolate the infected horses immediately to prevent the spread ", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
				IncidentEffect.ChangeItemsAmount(new Func<List<ItemObject>>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetHorseflyPlagueHorseItems|25_73), -1),
				IncidentEffect.ChangeItemsAmount(new Func<List<ItemObject>>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetHorseflyPlagueHorseItems|25_73), -3).WithChance(0.3f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			incident15.AddOption("{=A4voELZr}Order the disposal of the infected horses to avoid further contamination.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.ChangeItemsAmount(new Func<List<ItemObject>>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetHorseflyPlagueHorseItems|25_73), -2)
			}, null, null);
			incident15.AddOption("{=ZCWziaI4}Ignore the outbreak and continue marching", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.ChangeItemsAmount(new Func<List<ItemObject>>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetHorseflyPlagueHorseItems|25_73), -1),
				IncidentEffect.ChangeItemsAmount(new Func<List<ItemObject>>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetHorseflyPlagueHorseItems|25_73), -3).WithChance(0.8f)
			}, null, null);
			Incident incident16 = this.RegisterIncident("incident_glanders_outbreak", "{=QjWhMbL1}Glanders Outbreak", "{=xH4KUzVS}As you leave town, you notice one of your horses has a swollen head and a bloody nose. You recognize this disease. It can spread to your men, and not only this animal is at risk but every other animal that drank from the same trough.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.AnimalIllness, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopsToDemotePredicate|25_79)).Sum((TroopRosterElement x) => x.Number) >= 5);
			incident16.AddOption("{=AIABabCI}Kill the horse and the two other horses it shared a stable with.", new List<IncidentEffect>
			{
				IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopsToDemotePredicate|25_79), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_80), 3, true),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100)
			}, null, null);
			Incident incident17 = incident16;
			string text21 = "{=IBIbetoC}Kill the horse alone, hoping for the best.";
			list4 = new List<IncidentEffect>();
			list4.Add(IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopsToDemotePredicate|25_79), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_80), 1, true));
			List<IncidentEffect> list22 = list4;
			IncidentEffect[] array2 = new IncidentEffect[2];
			array2[0] = IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopsToDemotePredicate|25_79), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_80), 3, true);
			array2[1] = IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 2);
			list22.Add(IncidentEffect.Group(array2).WithChance(0.25f));
			incident17.AddOption(text21, list4, null, null);
			Incident incident18 = incident16;
			string text22 = "{=bWz49lZu}Allow the cavalryman to care for the animal, even though it puts him and others at risk.";
			list4 = new List<IncidentEffect>();
			List<IncidentEffect> list23 = list4;
			IncidentEffect effectOne = IncidentEffect.TraitChange(DefaultTraits.Mercy, 100);
			IncidentEffect[] array3 = new IncidentEffect[3];
			array3[0] = IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopsToDemotePredicate|25_79), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_80), 3, true);
			array3[1] = IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 2);
			array3[2] = IncidentEffect.TraitChange(DefaultTraits.Calculating, -200);
			list23.Add(IncidentEffect.Select(effectOne, IncidentEffect.Group(array3).WithChance(0.25f), 0.5f));
			incident18.AddOption(text22, list4, null, null);
			Incident incident19 = this.RegisterIncident("incident_colicky_horses", "{=k4kbVxTL}Colicky Horses", "{=Bi8fKaGw}As you leave town, one of your cavalrymen finds his mount lying on the ground, whining in pain and refusing to get up. You're sure that he was careless and fed the animal too quickly after your arrival, as he was eager to get to the town's tavern.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.AnimalIllness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemotePredicate|25_87)).Sum((TroopRosterElement x) => x.Number) >= 15);
			incident19.AddOption("{=bNTKEu9p}Provide what care you can, even though it will slow you down and there is no sure chance of success", new List<IncidentEffect>
			{
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemotePredicate|25_87), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_88), 1, true).WithChance(0.5f)
			}, null, null);
			incident19.AddOption("{=9yFSbR36}Whip the responsible cavalryman for his negligence, and let him buy his own remount.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			incident19.AddOption("{=qRZpuLae}End the animal's misery quickly, and tell the horseman that you will not trust another poor beast to his care.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.DemoteTroopsRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemotePredicate|25_87), new Func<CharacterObject, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__TroopToDemoteToPredicate|25_88), 1, true)
			}, null, null);
			Incident incident20 = this.RegisterIncident("incident_camp_fever", "{=l2rWAm4N}Camp Fever", "{=Q5MXehRY}As you break camp, you find that two of your men are bathed in sweat and cannot rouse themselves. They are confused, and one is starting to show pock-marks on his chest. You recognize the symptoms as camp fever, and fear it will spread quickly. Their companions want to tend to them, but you know that if you don't quarantine the men the disease is likely to spread. ", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.Illness, CampaignTime.Days(60f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 50);
			incident20.AddOption("{=htMJ1uiy}Load the sick onto a cart and give them water, but otherwise let their comrades have as little contact with them as possible. ", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(2, 0.25f)
			}, null, null);
			string text23 = "{=4v8LpVb8}Send some men to fetch a doctor and medicine, accepting that it will delay your march";
			List<IncidentEffect> list24 = new List<IncidentEffect>();
			list24.Add(IncidentEffect.GoldChange(() => -200));
			list24.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list24.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, -100));
			list24.Add(IncidentEffect.DisorganizeParty());
			incident20.AddOption(text23, list24, null, null);
			string text24 = "{=c5uy0bb9}Let your men treat the sick as you march";
			List<IncidentEffect> list25 = new List<IncidentEffect>();
			list25.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1).WithChance(0.1f));
			list25.Add(IncidentEffect.WoundTroopsRandomly(2).WithChance(0.6f));
			incident20.AddOption(text24, list25, null, null);
			Incident incident21 = this.RegisterIncident("incident_bloody_flux", "{=LEHxD9ik}Bloody Flux", "{=3FwbFdbw}As you inspect your siege camp, you come across disgusting evidence that one or more of your men has contracted the bloody flux. It will pass from man to man as they share food and water, and you know what it can do to a besieging army if left unchecked.", IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege, IncidentsCampaignBehaviour.IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 70);
			incident21.AddOption("{=MSARXv6p}Identify the sick and make them camp outside your palisades, tending to themselves.", new List<IncidentEffect>
			{
				IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(0.05f, 0.5f),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			incident21.AddOption("{=Qnb8wrL0}Treat your men but trust in strict preventative measures, like digging latrines outside camp and forcing your men to go far afield for water, that they are likely to resent.", new List<IncidentEffect>
			{
				IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(3, 0.25f),
				IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(5, 0.25f).WithChance(0.5f),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			incident21.AddOption("{=zdPb0vtU}Treat the sick and make no additional demands on your men, trusting in luck to see you through this.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.WoundTroopsRandomlyWithChanceOfDeath(0.1f, 0.3f)
			}, null, null);
			Incident incident22 = this.RegisterIncident("incident_lice_and_fleas", "{=CX8WdvvR}Lice and Fleas", "{=b4BaOUKn}Lice are part of military life but several of your men are absolutely teaming with the vermin. Perhaps they are especially virulent in these parts, or perhaps they thrive in this weather. You fear that they will spread camp fever or other diseases.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.Illness, CampaignTime.Days(60f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 45);
			incident22.AddOption("{=ybnqAVbP}Force the afflicted to shave every strand of their hair and beard", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident22.AddOption("{=PAW2b2Y2}Require your men to go through the laborious process of picking clean their bodies and clothes", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			incident22.AddOption("{=irLNaJ3m}Tell your men to give themselves a good scratch and keep marching", new List<IncidentEffect>
			{
				IncidentEffect.WoundTroopsRandomly(3).WithChance(0.5f),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			Incident incident23 = this.RegisterIncident("incident_saint_of_the_woods", "{=SqpOlZhl}Saint of the Woods", "{=RrHS8JOx}As you depart the scene of your victory, a man dressed in white robes emerges from a nearby wood. He tells you that you owe your victory to the intervention of Hageos Tristamos, a holy man from a century ago who assists those who fight in a noble cause, and asks that you donate half the loot to the local shrine in gratitude, where it will be used to feed the poor.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= 3);
			string text25 = "{=4dIYUMkA}Donate the spoils of battle to the saint's shrine";
			List<IncidentEffect> list26 = new List<IncidentEffect>();
			list26.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list26.Add(IncidentEffect.GoldChange(() => -300));
			incident23.AddOption(text25, list26, null, null);
			string text26 = "{=Ioc2z1Ap}Declare that you owe your victory to no saint but to your men, and give them the silver";
			List<IncidentEffect> list27 = new List<IncidentEffect>();
			list27.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list27.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list27.Add(IncidentEffect.MoraleChange(3f));
			list27.Add(IncidentEffect.GoldChange(() => -300));
			incident23.AddOption(text26, list27, null, null);
			string text27 = "{=2auYPXJ8}Declare that you owe your victory to your own right arm, and keep the silver";
			List<IncidentEffect> list28 = new List<IncidentEffect>();
			list28.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -200));
			list28.Add(IncidentEffect.GoldChange(() => 300));
			incident23.AddOption(text27, list28, null, null);
			string text28 = "{=NLbvOuFt}Fall on your knees in tears, repent of your sins, and leave a huge donation";
			List<IncidentEffect> list29 = new List<IncidentEffect>();
			list29.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list29.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list29.Add(IncidentEffect.RenownChange(5f));
			list29.Add(IncidentEffect.GoldChange(() => -500));
			incident23.AddOption(text28, list29, null, null);
			Incident incident24 = this.RegisterIncident("incident_servants_of_mercy", "{=FYvrShj3}Servants of Mercy", "{=t0iKT1VJ}As the dust of battle settles, you see a group of women tending to the wounded. They explain that they are the Servants of Hope, mendicant sisters pledged to assist the afflicted in the hope of winning mercy from Heaven. They offer to accompany your party for a few days and tend to your men's wounds, asking nothing in return.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.Illness, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalWoundedRegulars >= 3);
			incident24.AddOption("{=5ByLt1QP}Tell the Sisters that they will slow you down, and your men can heal on their own.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			incident24.AddOption("{=STLwVkdO}Accept the Sisters' help", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.HealTroopsRandomly(3)
			}, null, null);
			Incident incident25 = this.RegisterIncident("incident_right_of_pillage", "{=p0bMRIum}Right of Pillage", "{=NWJ87xjB}As you prepare to march forth from newly captured {TOWN_NAME}, your men stop you. They shed their blood for your wars and you denied them their customary right to sack it. They say that it you will not let them enrich themselves from the enemy, then you should enrich them from your purse", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				BesiegeSettlementLogEntry besiegeSettlementLogEntry = IncidentsCampaignBehaviour.<InitializeIncidents>g__FindLastBesiegeSettlementLogEntry|25_103();
				if (besiegeSettlementLogEntry == null)
				{
					return false;
				}
				SiegeAftermathLogEntry siegeAftermathLogEntry = IncidentsCampaignBehaviour.<InitializeIncidents>g__FindLastSiegeAftermathLogEntry|25_104(besiegeSettlementLogEntry);
				if (siegeAftermathLogEntry == null)
				{
					return false;
				}
				if (siegeAftermathLogEntry.GameTime < besiegeSettlementLogEntry.GameTime)
				{
					return false;
				}
				description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
			});
			string text29 = "{=lm37mXGH}Let your men return and sack the town while it is still defenseless";
			List<IncidentEffect> list30 = new List<IncidentEffect>();
			list30.Add(IncidentEffect.Custom(null, delegate
			{
				BesiegeSettlementLogEntry besiegeSettlementLogEntry = IncidentsCampaignBehaviour.<InitializeIncidents>g__FindLastBesiegeSettlementLogEntry|25_103();
				if (((besiegeSettlementLogEntry != null) ? besiegeSettlementLogEntry.OwnerClanBeforeBesiege : null) == null || besiegeSettlementLogEntry.OwnerClanBeforeBesiege.IsEliminated)
				{
					return new List<TextObject>();
				}
				SiegeAftermathAction.ApplyAftermath(MobileParty.MainParty, MobileParty.MainParty.LastVisitedSettlement, SiegeAftermathAction.SiegeAftermath.Pillage, besiegeSettlementLogEntry.OwnerClanBeforeBesiege, new Dictionary<MobileParty, float>());
				return new List<TextObject>();
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=6QuPiTsY}Consequences of a town sacked", null)
			}));
			incident25.AddOption(text29, list30, null, null);
			string text30 = "{=MLIyVwfx}Pay them yourself";
			List<IncidentEffect> list31 = new List<IncidentEffect>();
			list31.Add(IncidentEffect.GoldChange(() => -1000));
			list31.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list31.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list31.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			incident25.AddOption(text30, list31, null, null);
			incident25.AddOption("{=HG7WWmX8}Punish the troublemakers for sewing mutiny", new List<IncidentEffect> { IncidentEffect.MoraleChange(-20f) }, null, null);
			incident25.AddOption("{=tK6IkgCK}Tell your men that Heaven expects them to show mercy to the innocent", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-10f)
			}, null, null);
			Incident incident26 = this.RegisterIncident("incident_donative_demand", "{=p8ESONLB}Donative Demand", "{=qaGbN6wX}It has not escaped your men’s notice that you have accumulated a fair amount of money in your treasury. A delegation approaches you and suggests that this would be a good time for the traditional donative, as Calradian emperors of old were known to give their legions.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.Gold >= 5000 && MobileParty.MainParty.MemberRoster.TotalRegulars >= 20);
			incident26.AddOption("{=Z7RAahk3}Convince your men that the money would be better spent on supplies and recruits to ensure their survival", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-2f)
			}, null, null);
			incident26.AddOption("{=YOhKn6YV}Tell your men that they can do as they wish with their wages, but you are not in the habit of subsidizing wine vendors, women of questionable virtue and gambling sharks more than necessary", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-2f)
			}, null, null);
			string text31 = "{=ocsX04bn}\"I am a river of wealth unto you, my brave warriors!\"";
			List<IncidentEffect> list32 = new List<IncidentEffect>();
			list32.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list32.Add(IncidentEffect.MoraleChange(5f));
			list32.Add(IncidentEffect.GoldChange(() => -500));
			incident26.AddOption(text31, list32, null, null);
			incident26.AddOption("{=NrmCF6qb}Erupt in a towering rage, so they will think twice about bothering you again", new List<IncidentEffect>
			{
				IncidentEffect.MoraleChange(-5f),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			Incident incident27 = this.RegisterIncident("incident_vedmaks_treasure", "{=6bF0LqJo}Vedmak's Treasure", "{=tfckGAIA}As you prepare to leave, one of your men says the he was approached by a villager. There is a particularly ill-tempered vedmak on the edge of town who makes his living not from healing or love-potions but from black magic, and who threatens to curse villagers unless they pay him. They asked your men to drive the vedmak away and burn his hut, and told them that they can have his ill-gotten gains.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "sturgia" && MobileParty.MainParty.LastVisitedSettlement.IsVillage && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident27.AddOption("{=byocyGXf}Convince your men that those who anger with vedmaks are likely to see their nose shrivel up or their fingers fall off", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100)
			}, null, null);
			string text32 = "{=8gEBPabt}Let them go and demand they split their haul with you";
			List<IncidentEffect> list33 = new List<IncidentEffect>();
			list33.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5));
			list33.Add(IncidentEffect.MoraleChange(5f));
			list33.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list33.Add(IncidentEffect.GoldChange(() => 200));
			incident27.AddOption(text32, list33, null, null);
			incident27.AddOption("{=3zP80NBJ}Tell them that village disputes are a matter for a proper inquest by the authorities", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			Incident incident28 = this.RegisterIncident("incident_the_deer_departed", "{=IG19KaLK}The Deer Departed", "{=oxBlBLWF}You noticed that the deer around this Khuzait village are unusually unafraid of humans. One of your men manages to shoot one. The villagers approach him afterwards and beg him not to eat the animal. Their former shaman transformed himself into a deer some years ago, they say, and while they don't know if the animal your man killed is him or not, they want to perform the burial rights properly, just to be sure.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.HuntingForaging, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
				if (((lastVisitedSettlement != null) ? lastVisitedSettlement.MapFaction.StringId : null) == "khuzait")
				{
					return MobileParty.MainParty.MemberRoster.GetTroopRoster().Any((TroopRosterElement x) => !x.Character.IsHero && x.Character.IsRanged);
				}
				return false;
			});
			string text33 = "{=xJ0FKCtA}Let the villagers take the deer, even though your men scoff at your superstition";
			List<IncidentEffect> list34 = new List<IncidentEffect>();
			list34.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list34.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list34.Add(IncidentEffect.MoraleChange(-5f));
			list34.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 5));
			incident28.AddOption(text33, list34, null, null);
			string text34 = "{=p8yBnCG2}Let your men enjoy their meal, but commend the villagers for making a good attempt to get their hands on a piece of perfectly good meat";
			List<IncidentEffect> list35 = new List<IncidentEffect>();
			list35.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list35.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list35.Add(IncidentEffect.MoraleChange(5f));
			list35.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3));
			list35.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			incident28.AddOption(text34, list35, null, null);
			string text35 = "{=0wRiev5G}Show genuine remorse and participate in the burial rites for the deer, just in case, even if your troops make comments behind your back";
			List<IncidentEffect> list36 = new List<IncidentEffect>();
			list36.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list36.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list36.Add(IncidentEffect.MoraleChange(-5f));
			list36.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 20));
			incident28.AddOption(text35, list36, null, null);
			Incident incident29 = this.RegisterIncident("incident_wedding_celebration", "{=G16Nlaaq}Wedding Celebration", "{=dkwLMXDz}As you break camp, several of your veterans come forward, somewhat sheepishly. They push one of their number forward and say that he married a young lass at the last village that you visited. They ask if they can share one of the amphorae of wine that they've noticed you are carrying in celebration.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 10);
			string text36 = "{=Edn4syjZ}Indulge your men, even though you have little doubt that the wedding is an invention";
			List<IncidentEffect> list37 = new List<IncidentEffect>();
			list37.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list37.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list37.Add(IncidentEffect.MoraleChange(5f));
			list37.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => -1));
			incident29.AddOption(text36, list37, null, null);
			incident29.AddOption("{=u514BSH6}Tell them that you will not reward deceit", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			incident29.AddOption("{=axNOvMcw}Pour all your wine onto the ground, telling them that you cannot tolerate the presence of anything that erodes their morals and discipline", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.MoraleChange(5f)
			}, null, null);
			Incident incident30 = this.RegisterIncident("incident_kannic_splendors", "{=msK9baMp}Kannic Splendors", "{=a1cuuevH}One of your men comes up to you excitedly. A peddler in the marketplace sold him a map to a treasure buried by a long-dead Kannic sorceror in a wadi near here. He wants a bit of time to check it out and promises you a fifth share of anything he finds. You've heard tales of gold found in ancient tombs near here, but in this case you'd give a hundred to one odds that this leads to either a grave that was pillaged centuries ago or someone's underground irrigation cistern, and at any rate the locals won't appreciate your man smashing things up with pick and shovel.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
				return ((lastVisitedSettlement != null) ? lastVisitedSettlement.MapFaction.StringId : null) == "aserai" && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
			});
			incident30.AddOption("{=12bIzNkB}Tell him that no one sells maps to treasures that they could just as easily dig up themselves", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			string text37 = "{=bi8UfCdg}Indulge your man, and wait for his to complete his errand";
			List<IncidentEffect> list38 = new List<IncidentEffect>();
			list38.Add(IncidentEffect.DisorganizeParty());
			list38.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list38.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			list38.Add(IncidentEffect.GoldChange(() => 1000).WithChance(0.01f));
			incident30.AddOption(text37, list38, null, null);
			incident30.AddOption("{=KQhA1bQq}Tell him that the Kannic sorcerors sealed jinn into their treasure chambers with the power to turn a man's skin inside-out, and you couldn't possibly let him go", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100)
			}, null, null);
			Incident incident31 = this.RegisterIncident("incident_solider_is_wanted_criminal", "{=eN5vqUuF}Soldier is Wanted Criminal", "{=z4RgNBkq}On your way out of the village, a delegation led by the elder approaches you. They point to a {TROOP_TYPE} in your ranks - a good warrior, but none too outgoing. They say he used to farm in this village, but fled after he killed his neighbor in a quarrel over boundary stones. The accused snaps back with a sudden vehemence - his victim was a cheat, and deserved what he got. Under the law, murder requires the death penalty, though custom allows blood money to be paid instead.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				CharacterObject characterObject = this.<InitializeIncidents>g__GetWantedCriminalTroop|25_129();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_TYPE", characterObject.Name);
				return (MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "empire" || MobileParty.MainParty.LastVisitedSettlement.MapFaction.StringId == "vlandia") && MobileParty.MainParty.MemberRoster.TotalRegulars >= 40;
			});
			string text38 = "{=HXj3itpQ}Your {TROOP_TYPE} confessed. There is no option but to let the law take its course";
			List<IncidentEffect> list39 = new List<IncidentEffect>();
			list39.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 300));
			list39.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list39.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character == this.<InitializeIncidents>g__GetWantedCriminalTroop|25_129(), () => 1));
			list39.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10));
			incident31.AddOption(text38, list39, delegate(TextObject text)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetWantedCriminalTroop|25_129();
				text.SetTextVariable("TROOP_TYPE", characterObject.Name);
				return true;
			}, null);
			string text39 = "{=XvgESxLA}Tell the villagers that they will have to accept blood money, even though they would prefer vengeance";
			List<IncidentEffect> list40 = new List<IncidentEffect>();
			list40.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list40.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list40.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list40.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			list40.Add(IncidentEffect.GoldChange(() => -300));
			incident31.AddOption(text39, list40, null, null);
			string text40 = "{=OjPetQa7}Tell the villagers that they will need to wait until the next life for justice, because you need your fighter in this one";
			List<IncidentEffect> list41 = new List<IncidentEffect>();
			list41.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list41.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list41.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			incident31.AddOption(text40, list41, null, null);
			Incident incident32 = this.RegisterIncident("incident_soldier_in_debt", "{=GDHHAyQ5}Soldier in Debt", "{=brV8zgmJ}As you leave town, a merchant approaches you. He shows you a document indicating that one of your {TROOP_TYPE} took out a loan, to be repaid on departure with a quarter part as interest. Your man shrugs, calls the merchant a thief for loaning at such ruinous rates, and says he lost the money gambling anyway.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
			{
				CharacterObject characterObject = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetSoldierInDebtTroop|25_139();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_TYPE", characterObject.Name);
				return Hero.MainHero.Clan.Kingdom != null && !Hero.MainHero.Clan.IsUnderMercenaryService;
			});
			incident32.AddOption("{=z0yA6wmV}Force your soldier to repay the loan with full interest", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			string text41 = "{=qeWTPZge}Tell the merchant that he is lucky not to be chased out of camp for gouging your men";
			List<IncidentEffect> list42 = new List<IncidentEffect>();
			list42.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list42.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list42.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			incident32.AddOption(text41, list42, null, null);
			string text42 = "{=KMb0CHWI}Pay back the loan out of your own purse, even if you might get a reputation for being easily manipulated";
			List<IncidentEffect> list43 = new List<IncidentEffect>();
			list43.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list43.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list43.Add(IncidentEffect.GoldChange(() => -100));
			list43.Add(IncidentEffect.MoraleChange(5f));
			incident32.AddOption(text42, list43, null, null);
			Incident incident33 = this.RegisterIncident("incident_apples_from_heaven", "{=DCy85lqp}Apples from Heaven", "{=OBdQ2q3d}As you enter the village, some of your men tell you that they've found a group of what look like wild apple trees in full bloom by a stream. They'd fetch a fine price in the village market. It doesn't look like anyone owns the land, but you know that once they're discovered a half-dozen claimants will press title.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringVillage, IncidentsCampaignBehaviour.IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 5);
			incident33.AddOption("{=n6gqNNJ0}Tell your men to help themselves.", new List<IncidentEffect>
			{
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
			}, null, null);
			incident33.AddOption("{=mezu0Pej}Inform the villagers of your discovery", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			Incident incident34 = this.RegisterIncident("incident_tempting_berries", "{=o7kYGTjF}Tempting Berries", "{=OmYT9wnV}Your party stumbles upon some bushes with ripe berries near a village, allowing for a refreshing break during the journey. You haven't seen berries quite like these before, but they do look tasty.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 5);
			incident34.AddOption("{=xSoKTeLL}Allow your men to collect some berries from the bushes", new List<IncidentEffect> { IncidentEffect.Select(IncidentEffect.Group(new IncidentEffect[]
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(5f)
			}), IncidentEffect.WoundTroopsRandomly(3), 0.2f) }, null, null);
			incident34.AddOption("{=E8IpjMAw}The bushes may belong to someone. Leave them to the local farmers", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident34.AddOption("{=y0Ior5zV}If they are ripe why aren't they are already eaten by animals or collected? They might be poisonous. Avoid them", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.MoraleChange(-1f)
			}, null, null);
			Incident incident35 = this.RegisterIncident("incident_hunters_paradise", "{=zQ2rtAQE}Hunter's Paradise", "{=fjWw8D1x}Your men spot several deer on the outskirts of the village, a rare thing in a land where meat and hides are valuable. The villagers tell you that there was an unusual crop of acorns this year, while fear of bandits has prevented them from hunting as they normally would. They invite you to shoot a few deer, as there will be plenty to go around anyway and the beasts are trampling the crops.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringVillage, IncidentsCampaignBehaviour.IncidentType.HuntingForaging, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			string text43 = "{=E659QFHp}Free meat, a bit of fun and practice for your men. Sound the hunting horn.";
			List<IncidentEffect> list44 = new List<IncidentEffect>();
			list44.Add(IncidentEffect.DisorganizeParty());
			list44.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list44.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3));
			list44.Add(IncidentEffect.PartyExperienceChance(200));
			list44.Add(IncidentEffect.WoundTroopsRandomly(2).WithChance(0.5f));
			incident35.AddOption(text43, list44, null, null);
			string text44 = "{=jON8A2UY}A bunch of loud amateur hunters will scare the game and probably injure themselves. Let your scout bag an animal or two, while the rest of your party marches ahead.";
			List<IncidentEffect> list45 = new List<IncidentEffect>();
			list45.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 2));
			list45.Add(IncidentEffect.DisorganizeParty());
			incident35.AddOption(text44, list45, null, null);
			incident35.AddOption("{=5rjUJaW4}Hunting requires patience. You've got real battles to fight.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) }, null, null);
			Incident incident36 = this.RegisterIncident("incident_boar_in_a_thicket", "{=Fa0J7a6M}Boar in a Thicket", "{=sQA7Z3Pp}As you set out from the village, a local hunter tells you that he can lead you to a nest made by a particularly large boar in a nearby thicket. The animal is a threat to the village and too fierce for him, he says. It already killed two of his dogs. Your men could probably get it and claim a meal of fresh pork, though not without risk.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.HuntingForaging, CampaignTime.Days(60f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			Incident incident37 = incident36;
			string text45 = "{=chZrxxe1}The hunt is on! Surround the thicket with spearmen and drive the boar out";
			list4 = new List<IncidentEffect>();
			List<IncidentEffect> list46 = list4;
			IncidentEffect[] array4 = new IncidentEffect[3];
			array4[0] = IncidentEffect.MoraleChange(5f);
			array4[1] = IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("meat"), () => 3);
			array4[2] = IncidentEffect.TraitChange(DefaultTraits.Calculating, -100);
			list46.Add(IncidentEffect.Select(IncidentEffect.Group(array4), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 1), 0.75f));
			incident37.AddOption(text45, list4, null, null);
			incident36.AddOption("{=Hl2EsIob}Leave the animal be", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) }, null, null);
			Incident incident38 = this.RegisterIncident("incident_broken_wagon", "{=8FobMTCH}Broken-down Wagon", "{=FJzoOKIU}You come across a trader whose wagon has gotten stuck in the road after his draught horse went lame. He is desperate to sell his wares before they spoil, and offers them at a bargain price.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.IsCurrentlyAtSea)
				{
					return MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent || x.EquipmentElement.Item.StringId == "mule");
				}
				return false;
			});
			string text46 = "{=ywaqOBG6}Give some draught animal to him so that he can continue on his way.";
			List<IncidentEffect> list47 = new List<IncidentEffect>();
			list47.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list47.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetBrokenWagonDraughtAnimalItem|25_156), () => -1));
			incident38.AddOption(text46, list47, null, null);
			string text47 = "{=1bobtmxl}Sell him your cheapest horse so that he can continue.";
			List<IncidentEffect> list48 = new List<IncidentEffect>();
			list48.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list48.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list48.Add(IncidentEffect.GoldChange(() => 200));
			list48.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetBrokenWagonCheapestHorseItem|25_157), () => -1));
			incident38.AddOption(text47, list48, null, null);
			string text48 = "{=buCYa33Q}Buy his wares at bargain price.";
			List<IncidentEffect> list49 = new List<IncidentEffect>();
			list49.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list49.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list49.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("cotton"), () => 1));
			list49.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => 1));
			list49.Add(IncidentEffect.GoldChange(() => -100));
			incident38.AddOption(text48, list49, null, null);
			incident38.AddOption("{=wIakrAb}Explain to him that you cannot help.", new List<IncidentEffect>(), null, null);
			Incident incident39 = this.RegisterIncident("incident_honor_the_slain_foe", "{=bGxsp5Le}Honor the Slain Foe", "{=lFaBQPxk}Your {CULTURE_NAME} troops ask if they can take charge of the {CULTURE_BURIAL_STYLE} of the {CULTURE_NAME} in the enemy's ranks, declaring that they feel a duty to their kin even if they fought in a bad cause.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Days(60f), delegate(TextObject description)
			{
				TroopRosterElement troopRosterElement = this.<InitializeIncidents>g__GetHonorTheSlainFoeRandomTroop|25_170();
				if (troopRosterElement.Character == null)
				{
					return false;
				}
				description.SetTextVariable("CULTURE_NAME", troopRosterElement.Character.Culture.Name);
				string stringId = troopRosterElement.Character.Culture.StringId;
				if (stringId == "nord" || stringId == "battania" || stringId == "khuzait")
				{
					description.SetTextVariable("CULTURE_BURIAL_STYLE", "{=LntPXkbN}cremation");
				}
				else
				{
					description.SetTextVariable("CULTURE_BURIAL_STYLE", "{=AbdmPy2k}burial");
				}
				return true;
			});
			incident39.AddOption("{=9mHMjIqd}Refuse, knowing that separate rituals will breed suspicion among the different cultures in your ranks", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident39.AddOption("{=ssAwOnse}Grant their request, as there is no shame in honoring a valiant foe", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.MoraleChange(-10f)
			}, null, null);
			Incident incident40 = this.RegisterIncident("incident_local_hero", "{=lfYEujfb}Local Hero", "{=9ab6HwCS}As you prepare to leave the village, the elder approaches you. His nephew is to be married, and it would be a great honor if a warrior of your renown would bless the couple. Your men suppress grins and jostle each other, and you know they are looking forward to the chance to get royally drunk.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.Clan.Renown >= 500f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident40.AddOption("{=Huu4DySE}Accept the invitation and let the men have their fun", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			string text49 = "{=LamSdYjF}Accept the invitation, offering a handsome gift to the couple";
			List<IncidentEffect> list50 = new List<IncidentEffect>();
			list50.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list50.Add(IncidentEffect.GoldChange(() => -100));
			list50.Add(IncidentEffect.MoraleChange(10f));
			list50.Add(IncidentEffect.DisorganizeParty());
			incident40.AddOption(text49, list50, null, null);
			incident40.AddOption("{=Pw3qq9nw}Apologize, even though it will disappoint your men", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			Incident incident41 = this.RegisterIncident("incident_false_money", "{=Cba9zsDh}False Money", "{=FKkG15p7}As you leave the village, a trader comes up to you and says the \"silver\" denars that some of your men just gave him are just polished iron. You can indeed see some specks of rust. He demands that you pay him in full. It's entirely possible that your men tried to pass off some false coinage, but the trader's \"evidence\" could equally easily have come from his own stock.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 25);
			incident41.AddOption("{=ZZXWjvQG}Have your men turn out their pockets. If there are any other tin coins there, assume they are at fault.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident41.AddOption("{=lnF2ci30}Tell the merchant that once he accepts currency, it is his fault and his responsibility", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			Incident incident42 = this.RegisterIncident("incident_coin_clipping", "{=IFtXJbfK}Coin Clipping", "{=7fpGI4Jb}Shortly after the loot is distributed, you come across several of your men rubbing the edges of the coins that you paid them against a rock. You recognize what they're doing - clipping, or shaving off a bit of the silver to melt down in the hope that no one will notice.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) >= 40 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident42.AddOption("{=D750cNUH}Turn a blind eye, even though the practice is considered little better than counterfeiting", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, -100) }, null, null);
			incident42.AddOption("{=EM8bWb1b}Demand that your men stop, and inform any merchants in the next town that they should look very carefully at whatever coins your men give them.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			string text50 = "{=rRVEjUWH}Show them a few tricks from your rogue's repertoire";
			List<IncidentEffect> list51 = new List<IncidentEffect>();
			list51.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -200));
			list51.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list51.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list51.Add(IncidentEffect.GoldChange(() => 200));
			list51.Add(IncidentEffect.SkillChange(DefaultSkills.Roguery, 100f));
			incident42.AddOption(text50, list51, null, null);
			Incident incident43 = this.RegisterIncident("incident_ice_march", "{=5LOG5afj}Ice March", "{=rm4ApCIW}Now that the fighting has ended you notice that the weather has turned bitterly cold, and a wind is picking up. Soon after you return to the march, you notice that several of your men have fallen behind. Your men are shivering in the wind, however, and are in no mood to wait. You can march on and hope they catch up to you, or set a slower pace.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.HardTravel, CampaignTime.Days(60f), delegate(TextObject description)
			{
				MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(MobileParty.MainParty.Position.ToVec2());
				return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 30 && (weatherEventInPosition == MapWeatherModel.WeatherEvent.HeavyRain || weatherEventInPosition == MapWeatherModel.WeatherEvent.LightRain);
			});
			string text51 = "{=bXoYv9a2}Continue at a normal marching pace";
			List<IncidentEffect> list52 = new List<IncidentEffect>();
			list52.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list52.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => (int)(0.05f * (float)MobileParty.MainParty.MemberRoster.TotalRegulars)).WithChance(0.5f));
			incident43.AddOption(text51, list52, null, null);
			incident43.AddOption("{=gxGlWxVC}Slow down until the stragglers can catch up", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
				IncidentEffect.MoraleChange(-2f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			Incident incident44 = this.RegisterIncident("incident_sandstorm_warning", "{=cSa3IFw5}Sandstorm Warning", "{=yJMpjiP7}As you set out again after the battle, you feel a few scattered drops of rain. Then there is a rush of wind, and a wall of dust engulfs you. The blowing grains sting your face, and you can see but a few arms' lengths ahead. You know that it is a simple matter for men to get lost in the notorious sandstorms of the Nahhas, only to be found later dead of thirst or buried in a dune.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.HardTravel, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.Position.IsValid() && (!MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 20) && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) == TerrainType.Desert);
			incident44.AddOption("{=vzOfEiKx}Wait briefly in the lee of a dune until the wind eases up, then press cautiously ahead", new List<IncidentEffect>
			{
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100)
			}, null, null);
			string text52 = "{=HZwt5rVy}Keep marching, even though there is a good chance some of your men might be separated.";
			List<IncidentEffect> list53 = new List<IncidentEffect>();
			list53.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list53.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => (int)(0.05f * (float)MobileParty.MainParty.MemberRoster.TotalRegulars)).WithChance(0.5f));
			incident44.AddOption(text52, list53, null, null);
			Incident incident45 = this.RegisterIncident("incident_vintage_of_victory", "{=1cgOQB5K}Vintage of Victory", "{=ZoIXaxj9}Among the loot from your last battle you discover a vat of fine wine from {WINE_SETTLEMENT_NAME}. You know that you could sell it for a considerable sum, but your men just risked their lives and don't seem inclined to pocket silver tomorrow for a drink today.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Days(60f), delegate(TextObject description)
			{
				Village seededRandomElement = IncidentHelper.GetSeededRandomElement<Village>((from x in Village.All
					where x.VillageType == DefaultVillageTypes.VineYard
					select x).ToList<Village>(), this._activeIncidentSeed);
				if (seededRandomElement == null)
				{
					return false;
				}
				description.SetTextVariable("WINE_SETTLEMENT_NAME", seededRandomElement.Name);
				return MobileParty.MainParty.MemberRoster.TotalManCount >= 20;
			});
			string text53 = "{=hj27QkT3}Keep it to sell, telling your men that the money will be spent on keeping them safe";
			List<IncidentEffect> list54 = new List<IncidentEffect>();
			list54.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list54.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list54.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("wine"), () => 1));
			incident45.AddOption(text53, list54, null, null);
			incident45.AddOption("{=SdQEKdeo}Smash the top off the amphorae and let them drink their fill.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.MoraleChange(2f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			Incident incident46 = this.RegisterIncident("incident_no_time_to_mourn", "{=GL0jmbbJ}No Time to Mourn", "{=Qrj5hN2k}It is time to bury your dead, but the ground is frozen solid and the wood from nearby trees is too wet to make a proper pyre. You can give the bodies a hasty burial in a nearby gully, even though wolves or other animals may find them, or take the bodies with you to bury them as soon as you can.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Days(60f), delegate(TextObject description)
			{
				PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<PlayerBattleEndedLogEntry>((PlayerBattleEndedLogEntry x) => true);
				return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.PlayerCasualties > 0 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
			});
			incident46.AddOption("{=2GMj6ZaW}Carry the bodies with you until you find dry wood or softer ground", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(2f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			incident46.AddOption("{=0CCgjMTG}Cover the bodies as best you can and hope for the best", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-2f)
			}, null, null);
			Incident incident47 = this.RegisterIncident("incident_no_mood_for_mercy", "{=X8dgb937}No Mood for Mercy", "{=uXendo01}It was a hard-fought battle, and among your fallen is a young good-natured {TROOP_TYPE} who kept his comrades' spirits up during hard times. Your men’s faces are grim as they go out to collect the wounded of both sides. You can tell that, if they have their way, not many injured enemies will be brought back to your camp still alive as prisoners.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetNoMoodForMercyRandomTroop|25_190();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_TYPE", characterObject.Name);
				PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<PlayerBattleEndedLogEntry>((PlayerBattleEndedLogEntry x) => true);
				return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.PlayerCasualties > 0;
			});
			incident47.AddOption("{=5SWqBZSh}Demand that your men respect the customs of war and tend to those who gave quarter", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(-15f)
			}, null, null);
			incident47.AddOption("{=3CaDxb8R}Remind your men that war is war, the {TROOP_TYPE} knew the risks, and prisoners bring ransoms", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(-5f)
			}, delegate(TextObject text)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetNoMoodForMercyRandomTroop|25_190();
				text.SetTextVariable("TROOP_TYPE", characterObject.Name);
				return true;
			}, null);
			string text54 = "{=mBkCqpEj}Tell your men that they can avenge their comrade as they see fit";
			List<IncidentEffect> list55 = new List<IncidentEffect>();
			list55.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list55.Add(IncidentEffect.MoraleChange(10f));
			list55.Add(IncidentEffect.RemovePrisonersRandomlyWithPredicate((TroopRosterElement x) => true, 3));
			incident47.AddOption(text54, list55, null, null);
			Incident incident48 = this.RegisterIncident("incident_misplaced_vengeance", "{=fiesVorG}Misplaced Vengeance", "{=i0SwEBdf}Throughout your visit to {VILLAGE}, the villagers were worried that the enemy who sacked {LOOTED_VILLAGE} might be sending patrols their way. As you leave, you find out that their fears were not misplaced. The first thing you see is a flight of ravens in the sky. Your men begin to mutter to themselves, but soon they stumble across a sight that silences them - a pile of burned corpses, stacked by a crossroad, left by the {ENEMY} as their calling card. You press on, but you soon notice that your men are casting an eye towards your prisoners. They may not have been directly involved in what happened in {LOOTED_VILLAGE}, but you doubt your men care much.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				if (!MobileParty.MainParty.LastVisitedSettlement.IsVillage)
				{
					return false;
				}
				Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
				Village lootedVillage = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetMisplacedVengeanceLootedVillage|25_196();
				if (lootedVillage == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE", village.Name);
				description.SetTextVariable("LOOTED_VILLAGE", lootedVillage.Name);
				description.SetTextVariable("ENEMY", lootedVillage.Settlement.MapFaction.Name);
				if (MobileParty.MainParty.LastVisitedSettlement.MapFaction == Hero.MainHero.MapFaction && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5)
				{
					return (from x in MobileParty.MainParty.PrisonRoster.GetTroopRoster()
						where x.Character.Culture == lootedVillage.Settlement.Culture && !x.Character.IsHero
						select x).Sum((TroopRosterElement x) => x.Number) > 5;
				}
				return false;
			});
			string text55 = "{=SfO3uwdX}Take a brief walk to clear your head, and when you come back to find the prisoners gone, do not ask questions";
			List<IncidentEffect> list56 = new List<IncidentEffect>();
			list56.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list56.Add(IncidentEffect.MoraleChange(5f));
			list56.Add(IncidentEffect.Custom(null, delegate
			{
				Village lootedVillage = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetMisplacedVengeanceLootedVillage|25_196();
				IncidentEffect.RemovePrisonersRandomlyWithPredicate(delegate(TroopRosterElement x)
				{
					CultureObject culture = x.Character.Culture;
					Village lootedVillage2 = lootedVillage;
					return culture == ((lootedVillage2 != null) ? lootedVillage2.Settlement.Culture : null);
				}, 5).Consequence();
				TextObject textObject = new TextObject("{=vyH9slae}Lost {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?} of {CULTURE} culture", null).SetTextVariable("AMOUNT", 5);
				string tag = "CULTURE";
				Village lootedVillage3 = lootedVillage;
				TextObject item = textObject.SetTextVariable(tag, (lootedVillage3 != null) ? lootedVillage3.Settlement.Culture.Name : null);
				return new List<TextObject> { item };
			}, delegate(IncidentEffect effect)
			{
				Village village = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetMisplacedVengeanceLootedVillage|25_196();
				return new List<TextObject> { new TextObject("{=gejYla80}Lose {AMOUNT} {?AMOUNT > 1}prisoners{?}prisoner{\\?} of {CULTURE} culture", null).SetTextVariable("AMOUNT", 5).SetTextVariable("CULTURE", (village != null) ? village.Settlement.Culture.Name : null) };
			}));
			incident48.AddOption(text55, list56, null, null);
			incident48.AddOption("{=qljKyPrR}Tell your men that the prisoners must be kept safe and that you will hold them responsible for anything that happens", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-2f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			Incident incident49 = this.RegisterIncident("incident_sleeping_sentry", "{=qFa4Oik0}Sleeping Sentry", "{=gQigaCkD}One of your {RECRUIT} fell asleep on guard. He was new to your company and not popular, and your men are angry. He put their lives at risk, and they demand he be turned over to them to face the ancient Calradic penalty - to run the gauntlet of men with sticks who will beat him, an ordeal that frequently ends in death.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Days(60f), delegate(TextObject description)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetSleepingSentryTroop|25_203();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("RECRUIT", characterObject.Name);
				return !MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.MemberRoster.TotalRegulars >= 35;
			});
			string text56 = "{=oIknbpZl}Force the recruit to run the gauntlet";
			List<IncidentEffect> list57 = new List<IncidentEffect>();
			list57.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list57.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list57.Add(IncidentEffect.MoraleChange(5f));
			list57.Add(IncidentEffect.Select(IncidentEffect.WoundTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetSleepingSentryTroop|25_203), 1), IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => x.Character.Tier <= 2, () => 1), 0.5f));
			incident49.AddOption(text56, list57, null, null);
			incident49.AddOption("{=0lqAKHfU}Tell your men to be forgiving, as they were once recruits themselves", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.MoraleChange(-10f)
			}, null, null);
			Incident incident50 = this.RegisterIncident("incident_trade_proposal", "{=IaUhqDw6}Trade Proposal", "{=sU4t3v9r}A passing trader offers to buy {GOOD} from your party at well over the price that it would fetch in the nearest town. No doubt some other merchant has fed him bad information on prices, either to put a rival out of business or as a joke.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingEncounter, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Days(60f), delegate(TextObject description)
			{
				ItemRosterElement itemRosterElement = this.<InitializeIncidents>g__GetTradeProposalGood|25_208();
				if (itemRosterElement.EquipmentElement.Item == null)
				{
					return false;
				}
				description.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
				if (!MobileParty.MainParty.IsCurrentlyAtSea)
				{
					return MobileParty.MainParty.ItemRoster.Any((ItemRosterElement x) => x.Amount > 0);
				}
				return false;
			});
			incident50.AddOption("{=b9S5bNbq}As a merchant he should know his trade. Keep silent and take the offer", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.Custom(null, delegate
				{
					ItemRosterElement itemRosterElement = this.<InitializeIncidents>g__GetTradeProposalGood|25_208();
					MobileParty.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
					GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, itemRosterElement.EquipmentElement.ItemValue * 2, false);
					TextObject textObject = new TextObject("{=JmG1KrTw}Lost 1 {GOOD} and gained {AMOUNT}{GOLD_ICON}", null);
					textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
					textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 2);
					return new List<TextObject> { textObject };
				}, delegate(IncidentEffect effect)
				{
					ItemRosterElement itemRosterElement = this.<InitializeIncidents>g__GetTradeProposalGood|25_208();
					TextObject textObject = new TextObject("{=baqpnA0F}Lose 1 {GOOD}, gain {AMOUNT}{GOLD_ICON}", null);
					textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
					textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 2);
					return new List<TextObject> { textObject };
				})
			}, null, null);
			incident50.AddOption("{=0Wwwa77J}Tell the trader that you have no wish to profit from his ignorance", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
			}, null, null);
			incident50.AddOption("{=QWHwvRtC}As a fool and his money are soon parted, you'll do him a favor by saving him some time. Haggle for more", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
				IncidentEffect.Custom(null, delegate
				{
					ItemRosterElement itemRosterElement = this.<InitializeIncidents>g__GetTradeProposalGood|25_208();
					MobileParty.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
					GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, itemRosterElement.EquipmentElement.ItemValue * 3, false);
					TextObject textObject = new TextObject("{=JmG1KrTw}Lost 1 {GOOD} and gained {AMOUNT}{GOLD_ICON}", null);
					textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
					textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 3);
					return new List<TextObject> { textObject };
				}, delegate(IncidentEffect effect)
				{
					ItemRosterElement itemRosterElement = this.<InitializeIncidents>g__GetTradeProposalGood|25_208();
					TextObject textObject = new TextObject("{=baqpnA0F}Lose 1 {GOOD}, gain {AMOUNT}{GOLD_ICON}", null);
					textObject.SetTextVariable("GOOD", itemRosterElement.EquipmentElement.Item.Name);
					textObject.SetTextVariable("AMOUNT", itemRosterElement.EquipmentElement.ItemValue * 3);
					return new List<TextObject> { textObject };
				})
			}, null, null);
			Incident incident51 = this.RegisterIncident("incident_singing_for_supper", "{=9uaAUCas}Singing for Supper", "{=iV57q3Tm}A strolling minstrel offers to compose an epic ballad about your heroic deeds, potentially increasing your renown for a little amount of coin. Do you accept his offer, and if so, do you offer him any guidance on what to sing?", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Days(60f), (TextObject description) => true);
			string text57 = "{=AxGaZNry}Let him do it and reward him generously, but tell him to describe your losses and faults as well";
			List<IncidentEffect> list58 = new List<IncidentEffect>();
			list58.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 200));
			list58.Add(IncidentEffect.GoldChange(() => -500));
			list58.Add(IncidentEffect.RenownChange(20f));
			list58.Add(IncidentEffect.InfluenceChange(-100f));
			incident51.AddOption(text57, list58, null, null);
			incident51.AddOption("{=m35ezdZG}You don't need a bard to recount your heroic deeds", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			string text58 = "{=bJBLAnrc}Shower him with gold and tell him that he should feel free to embellish your story as he sees fit";
			List<IncidentEffect> list59 = new List<IncidentEffect>();
			list59.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list59.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list59.Add(IncidentEffect.GoldChange(() => -1000));
			list59.Add(IncidentEffect.RenownChange(20f));
			list59.Add(IncidentEffect.InfluenceChange(200f));
			incident51.AddOption(text58, list59, null, null);
			string text59 = "{=HbOFGhTG}Tragic tales are always the most popular. Ensure the bard does not leave out the daemon who sits on your shoulder, your bouts of madness, and the witch who foretold your terrible but heroic death";
			List<IncidentEffect> list60 = new List<IncidentEffect>();
			list60.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -300));
			list60.Add(IncidentEffect.RenownChange(30f));
			list60.Add(IncidentEffect.GoldChange(() => -200));
			incident51.AddOption(text59, list60, null, null);
			Incident incident52 = this.RegisterIncident("incident_love_marriage", "{=QpkXLmjE}Love Marriage", "{=Tq4O8vic}One of your {KNIGHT} asks you for a favor. His daughter plans to marry a man from the back alleys of this town with a reputation as a no-good layabout. He has refused permission but asks you to do what you can to insure the two do not elope.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				CharacterObject character = IncidentHelper.GetSeededRandomElement<TroopRosterElement>(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__LoveMarriageTroopPredicate|25_220)).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
				if (character == null)
				{
					return false;
				}
				description.SetTextVariable("KNIGHT", character.Name);
				return MobileParty.MainParty.LastVisitedSettlement.IsFortification && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan;
			});
			string text60 = "{=uppCchb0}Let them marry. Love must triumph over a father's pride";
			List<IncidentEffect> list61 = new List<IncidentEffect>();
			list61.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list61.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list61.Add(IncidentEffect.KillTroopsRandomly(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__LoveMarriageTroopPredicate|25_220), () => 1).WithChance(0.5f));
			incident52.AddOption(text60, list61, null, null);
			incident52.AddOption("{=oQmuxCSk}Do your best to explain to the girl that marriages are affairs of the head, not of the heart, and she had better think of her future and that of her children and not elope.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 200).WithChance(0.5f)
			}, null, null);
			incident52.AddOption("{=GMQROhTM}Order your troops to give the upstart a sound beating. Who does he think he is, the hero of some foolish romantic poem?", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
				IncidentEffect.MoraleChange(5f)
			}, null, null);
			Incident incident53 = this.RegisterIncident("incident_riddles_on_the_road", "{=bBd0jAvo}Riddles on the Road", "{=S2PV4Qeb}As you ride out of the village, an old man in rags comes up to you and grasps the halter of your horse. \"You must tell me!\" he barks. \"What is the tale told by the sword?\" Your men gather in to see if you will humor this seemingly mad hermit.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), (TextObject description) => PartyBase.MainParty.MemberRoster.TotalRegulars >= 10);
			incident53.AddOption("{=2dW3cBQy}That steel is forged from iron, and long hard marches breed tough warriors.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident53.AddOption("{=PHo98vjr}That a sword is all you need, because if you own one, you can take whatever else you need from those without.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 200),
				IncidentEffect.RenownChange(10f)
			}, null, null);
			incident53.AddOption("{=xhuGmF9r}That the force of the blade lies only in the hand that wields it.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Valor, 100) }, null, null);
			incident53.AddOption("{=yMoBOgxx}\"You tell me,\" you say, with a mad laugh, as you deal him a slice that draws blood.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.RenownChange(20f)
			}, null, null);
			Incident incident54 = this.RegisterIncident("incident_deadly_reputation", "{=XeX5x8jw}A Deadly Reputation", "{=GpUBP10Q}As you leave the village, a merchant who clearly wants your approval shouts out, \"{LOCAL_KING} has slain his thousands, but {PLAYER} his tens of thousands!\"", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || MobileParty.MainParty.LastVisitedSettlement.MapFaction.Leader == null)
				{
					return false;
				}
				description.SetTextVariable("LOCAL_KING", MobileParty.MainParty.LastVisitedSettlement.MapFaction.Leader.Name);
				description.SetTextVariable("PLAYER", Hero.MainHero.Name);
				return Hero.MainHero.Clan.Kingdom != null && Hero.MainHero.Clan.Kingdom.Leader != Hero.MainHero;
			});
			string text61 = "{=sxH5NHba}\"I have, and it is but an appetite-whetter before the main meal.\"";
			List<IncidentEffect> list62 = new List<IncidentEffect>();
			list62.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list62.Add(IncidentEffect.RenownChange(20f));
			list62.Add(IncidentEffect.HeroRelationChange(() => Hero.MainHero.Clan.Kingdom.Leader, -1));
			incident54.AddOption(text61, list62, null, null);
			string text62 = "{=acBdTQxp}\"All I have done has been in the service of {KING}. I am but his will made flesh.\"";
			List<IncidentEffect> list63 = new List<IncidentEffect>();
			list63.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list63.Add(IncidentEffect.HeroRelationChange(() => Hero.MainHero.Clan.Kingdom.Leader, 5));
			incident54.AddOption(text62, list63, delegate(TextObject text)
			{
				text.SetTextVariable("KING", Hero.MainHero.Clan.Kingdom.Leader.Name);
				return true;
			}, null);
			incident54.AddOption("{=naVH8uSO}\"Any blood I have shed may have been necessary but I hope that Heaven will forgive me.\"", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) }, null, null);
			incident54.AddOption("{=iR3ZuTSW}Acknowledge him by howling like an animal and swirling your weapon about, as mad behavior makes for good stories.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
				IncidentEffect.RenownChange(20f)
			}, null, null);
			Incident incident55 = this.RegisterIncident("incident_intriguing_rumors", "{=iYfbN2yb}Intriguing Rumors", "{=aqZyQ3Ca}A man, dressed as a nobleman's servant, approaches you. \"I hear that you and {RIVALLORD.NAME} aren't on the best of terms. Well, what can you expect from a man who can't manage his own family? I was recently employed at his household, and know that his wife was unusually close to her groom... I can spread the word, if you make it worth my while.\"", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				Hero hero = this.<InitializeIncidents>g__GetIntriguingRumorsRivalLord|25_228();
				if (hero == null)
				{
					return false;
				}
				description.SetCharacterProperties("RIVALLORD", hero.CharacterObject, false);
				return true;
			});
			incident55.AddOption("{=Gjd6nP29}You will not sully the good name of anyone with an obviously false rumor", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) }, null, null);
			string text63 = "{=jE9aKaYd}People will believe a noble's former servant. This man's favor is worth a purse of silver to you.";
			List<IncidentEffect> list64 = new List<IncidentEffect>();
			list64.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list64.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list64.Add(IncidentEffect.GoldChange(() => -200));
			list64.Add(IncidentEffect.InfluenceChange(10f));
			list64.Add(IncidentEffect.Custom(null, delegate
			{
				Hero hero = this.<InitializeIncidents>g__GetIntriguingRumorsRivalLord|25_228();
				if (hero != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, hero, -5, true);
					hero.AddInfluenceWithKingdom(-10f);
					TextObject textObject = new TextObject("{=WkIylUOa}{RIVALLORD.NAME} lost {AMOUNT} influence.", null);
					textObject.SetCharacterProperties("RIVALLORD", hero.CharacterObject, false);
					textObject.SetTextVariable("AMOUNT", 10);
					return new List<TextObject> { textObject };
				}
				return new List<TextObject>();
			}, delegate(IncidentEffect effect)
			{
				Hero hero = this.<InitializeIncidents>g__GetIntriguingRumorsRivalLord|25_228();
				TextObject textObject = new TextObject("{=NQKDUIGg}{RIVALLORD.NAME} loses {AMOUNT} influence", null);
				textObject.SetCharacterProperties("RIVALLORD", hero.CharacterObject, false);
				textObject.SetTextVariable("AMOUNT", 10);
				return new List<TextObject> { textObject };
			}));
			incident55.AddOption(text63, list64, null, null);
			incident55.AddOption("{=bZUMOAjU}Crack a few jokes to your men at {RIVALLORD.NAME}'s expense, but otherwise do nothing", new List<IncidentEffect> { IncidentEffect.MoraleChange(5f) }, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetIntriguingRumorsRivalLord|25_228();
				text.SetCharacterProperties("RIVALLORD", hero.CharacterObject, false);
				return true;
			}, null);
			Incident incident56 = this.RegisterIncident("incident_charitable_acts_recognition", "{=pjy9Z4bt}Charitable Acts Recognition", "{=vaez5WXH}As you leave town, you see a much larger number of beggars gathered than you would expect. Some of their limps look unconvincing. Your reputation for compassion and generosity may have gotten around.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.PlightOfCivilians, CampaignTime.Days(60f), (TextObject description) => Hero.MainHero.GetTraitLevel(DefaultTraits.Generosity) > 1 || Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) > 1);
			string text64 = "{=DFIPsN1W}If they come to you in need, it is not for you to question them. Have your men distribute alms as usual.";
			List<IncidentEffect> list65 = new List<IncidentEffect>();
			list65.Add(IncidentEffect.GoldChange(() => -200));
			list65.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list65.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			incident56.AddOption(text64, list65, null, null);
			string text65 = "{=CKKIJZcB}Have some of your men boil up a soup. Keep a watchful eye on all the beggars, especially those who move too vigorously to the front of the line, and serve only those who seem genuinely too frail to work.";
			List<IncidentEffect> list66 = new List<IncidentEffect>();
			list66.Add(IncidentEffect.GoldChange(() => -200));
			list66.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list66.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list66.Add(IncidentEffect.DisorganizeParty());
			incident56.AddOption(text65, list66, null, null);
			incident56.AddOption("{=T1pwrLN9}Tell them that you are no chicken to be plucked. You will reward neither liars nor those who keep company with liars.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			Incident incident57 = this.RegisterIncident("incident_the_pavillon", "{=rG4b9Tft}The Pavillon", "{=qBsa5KC5}As you leave town you come across an encampment of traveling entertainers. You see tattered but colorful tents, you hear wailing but enticing music, and smell the scent of cheap incense and meat that is probably slightly spoiled but spiced so that you would never notice. A group of women in some noble's thrown-away silks, their faces made up skillfully with charcoal and sheep's blood, beckon to your men. What do you do?", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				CultureObject culture = MobileParty.MainParty.LastVisitedSettlement.Culture;
				return PartyBase.MainParty.MemberRoster.TotalRegulars >= 5 && culture != null && (culture.StringId == "aserai" || culture.StringId == "khuzait" || culture.StringId == "empire_s");
			});
			incident57.AddOption("{=lPAC4x1e}Tell your men to steer clear of such dubious enticements", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident57.AddOption("{=xYuNxo3v}Let your men have some fun, but tell them to watch their purses and stay out of fights", new List<IncidentEffect>
			{
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.WoundTroopsRandomly(1).WithChance(0.5f),
				IncidentEffect.DisorganizeParty()
			}, null, null);
			string text66 = "{=mmlk2tXh}The fun is on you! Hurl a fistful of silver at the entertainers and tell them your men deserve nothing but the best";
			List<IncidentEffect> list67 = new List<IncidentEffect>();
			list67.Add(IncidentEffect.MoraleChange(5f));
			list67.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list67.Add(IncidentEffect.GoldChange(() => -100));
			incident57.AddOption(text66, list67, null, null);
			string text67 = "{=lzK4PmYL}Rob the entertainers. No doubt their wealth is ill-gotten, and to whom will they complain?";
			List<IncidentEffect> list68 = new List<IncidentEffect>();
			list68.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list68.Add(IncidentEffect.GoldChange(() => 200));
			incident57.AddOption(text67, list68, null, null);
			Incident incident58 = this.RegisterIncident("incident_abundance_of_troublemakers", "{=GFGuSmN9}Abundance of troublemakers", "{=mZir8Ogn}As you prepare to leave, the village elder approaches you. There's a gang of four lads who are constantly picking fights with the other villagers, he says. Even their own families don't want them around for fear they'll end up killing someone and cause a blood feud. He begs you to take them into your ranks, where they can learn a bit of discipline.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null);
			string text68 = "{=ePstZcmu}Accept the recruits. The first chance you get, find fault with their gear and give them a sound thrashing, so they'll know to mend their ways.";
			List<IncidentEffect> list69 = new List<IncidentEffect>();
			list69.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list69.Add(IncidentEffect.ChangeTroopAmount(() => MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 4));
			incident58.AddOption(text68, list69, null, null);
			string text69 = "{=Dogg1b9a}Take the recruits. Try not to be too hard on them at first, even though they'll probably corrode your party's discipline.";
			List<IncidentEffect> list70 = new List<IncidentEffect>();
			list70.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list70.Add(IncidentEffect.MoraleChange(-10f));
			list70.Add(IncidentEffect.ChangeTroopAmount(() => MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 4));
			incident58.AddOption(text69, list70, null, null);
			incident58.AddOption("{=EIbmlJRP}Tell the elder he'll need to find some other way to handle his troublemakers.", new List<IncidentEffect>(), null, null);
			Incident incident59 = this.RegisterIncident("incident_ennoble_a_fighter", "{=wLozeWS9}Ennoble a fighter", "{=FPnUWAj1}After the battle, one of your {TROOPS} from your party approaches you, dreamed of riding with your cavalry as a {NOBLE_TIER}. He has the money to buy his own horse, and he certainly distinguished himself in the fight. You know however that this will go down badly - with the high-born cavalry, who prefer to mingle with their own class, but probably even more so with his comrades, who will think he scorns them.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				CharacterObject characterObject = this.<InitializeIncidents>g__GetRegularTroop|25_244();
				if (characterObject == null)
				{
					return false;
				}
				CharacterObject characterObject2 = this.<InitializeIncidents>g__GetNobleTroop|25_245(characterObject.Culture);
				if (characterObject2 == null)
				{
					return false;
				}
				description.SetTextVariable("TROOPS", characterObject.Name);
				description.SetTextVariable("NOBLE_TIER", characterObject2.Name);
				return true;
			});
			incident59.AddOption("{=HpAbeiKP}Courage and skill deserve a reward, even if the result is not what you get", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
				IncidentEffect.MoraleChange(-10f),
				IncidentEffect.UpgradeTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetRegularTroop|25_244), new Func<CharacterObject>(this.<InitializeIncidents>g__GetNobleTroopForUpgrade|25_246), 1, () => this._activeIncidentSeed)
			}, null, null);
			Incident incident60 = incident59;
			string text70 = "{=oL1qlaxH}Explain to him your reservations, even though you can tell he won't like to hear them, and let him make up his own mind";
			list4 = new List<IncidentEffect>();
			list4.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list4.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list4.Add(IncidentEffect.Select(IncidentEffect.Group(new IncidentEffect[]
			{
				IncidentEffect.MoraleChange(-5f),
				IncidentEffect.UpgradeTroop(new Func<CharacterObject>(this.<InitializeIncidents>g__GetRegularTroop|25_244), new Func<CharacterObject>(this.<InitializeIncidents>g__GetNobleTroopForUpgrade|25_246), 1, () => this._activeIncidentSeed)
			}), IncidentEffect.Custom(null, () => new List<TextObject>(), (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=lobJVVWT}Nothing happens", null)
			}), 0.5f));
			incident60.AddOption(text70, list4, null, null);
			incident59.AddOption("{=uJYOhEx8}Commend him, but tell him to stick with his comrades", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			Incident incident61 = this.RegisterIncident("incident_family_claims_bandit", "{=6IA1S2XU}Family Claims Bandit", "{=BG8Ph5La}As you leave {VILLAGE_NAME}, a woman approaches you and grasps your horse's halter. Among your prisoners is her son, she says. You captured him when he was part of a gang of bandits, but she tells you tearfully that he is a good boy who fell in with bad company. She swears she'll ensure that he doesn't stray again from the correct path.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return MobileParty.MainParty.PrisonRoster.TotalRegulars > 0 && MobileParty.MainParty.PrisonRoster.GetTroopRoster().Any(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__BanditPrisonerPredicate|25_254));
			});
			incident61.AddOption("{=QDbl4lB7}Release your captive, even if some will mutter that you have a soft heart and a soft head", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.RemovePrisonersRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__BanditPrisonerPredicate|25_254), 1)
			}, null, null);
			incident61.AddOption("{=9yrWWbHF}Tell the woman that her son shall face the same penalties as any other bandit you caught", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100)
			}, null, null);
			string text71 = "{=6sAI5uOm}Tell the village that they must pay the lad's ransom, so that he will know there is no profit in banditry";
			List<IncidentEffect> list71 = new List<IncidentEffect>();
			list71.Add(IncidentEffect.GoldChange(() => 100));
			list71.Add(IncidentEffect.RemovePrisonersRandomlyWithPredicate(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__BanditPrisonerPredicate|25_254), 1));
			list71.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list71.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 50));
			incident61.AddOption(text71, list71, null, null);
			Incident incident62 = this.RegisterIncident("incident_turn_pike", "{=kl13BJZB}Turnpike", "{=VauaVUNS}You notice a footpath leading through your estate which could be quickly improved to be used by wagons and be a shortcut between the main trade roads. You could charge tolls and turn it into a turnpike, though the locals would resent the increased traffic and fees on their customary route.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
				{
					return false;
				}
				Building building = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetTurnPikeBuilding|25_257();
				return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
			});
			string text72 = "{=aF4Q5TJc}Widen the road for wagons but charge tolls to use it";
			List<IncidentEffect> list72 = new List<IncidentEffect>();
			list72.Add(IncidentEffect.BuildingLevelChange(new Func<Building>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetTurnPikeBuilding|25_257), () => 1));
			list72.Add(IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10));
			list72.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			incident62.AddOption(text72, list72, null, null);
			string text73 = "{=xxVE2bw1}Improve the road, but charge no tolls";
			List<IncidentEffect> list73 = new List<IncidentEffect>();
			list73.Add(IncidentEffect.BuildingLevelChange(new Func<Building>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetTurnPikeBuilding|25_257), () => 1));
			list73.Add(IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10));
			list73.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list73.Add(IncidentEffect.GoldChange(() => -1000));
			incident62.AddOption(text73, list73, null, null);
			incident62.AddOption("{=wcbbBjcD}Let it be", new List<IncidentEffect>(), null, null);
			Incident incident63 = this.RegisterIncident("incident_endebted_farmers", "{=Vbs9aocS}Endebted Farmers", "{=cRMxKTa8}Several freeholding farmers in your fief have fallen into debt through what appears to be a combination of bad luck and risky investments - a plough horse that died, a crop ruined by frost. They are unable to pay their taxes, and ask for a reprieve for several years.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
				{
					return false;
				}
				Building building = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetEndebtedFarmersBuilding|25_265();
				return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
			});
			string text74 = "{=OM6tkdD7}You know an opportunity when you see it. Seize their lands. They can farm as your tenants";
			List<IncidentEffect> list74 = new List<IncidentEffect>();
			list74.Add(IncidentEffect.BuildingLevelChange(new Func<Building>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetEndebtedFarmersBuilding|25_265), () => 1));
			list74.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list74.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			incident63.AddOption(text74, list74, null, null);
			incident63.AddOption("{=jeWayyGY}A reprieve is fine, but you tack on a bit of interest so that you're not overwhelmed with hard luck stories", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, 100) }, null, null);
			string text75 = "{=WHWokYrT}Call in a favor with a moneylender in town to reduce their debts so they can pay their taxes";
			List<IncidentEffect> list75 = new List<IncidentEffect>();
			list75.Add(IncidentEffect.GoldChange(() => 1000));
			list75.Add(IncidentEffect.InfluenceChange(-20f));
			incident63.AddOption(text75, list75, null, null);
			string text76 = "{=3QobDImA}Help them get back on their feet, even if some will call say that you are too trusting";
			List<IncidentEffect> list76 = new List<IncidentEffect>();
			list76.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list76.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list76.Add(IncidentEffect.GoldChange(() => -200));
			incident63.AddOption(text76, list76, null, null);
			Incident incident64 = this.RegisterIncident("incident_fertility_festival", "{=o5LpNEIa}Fertility Festival", "{=lfJ6OHgX}The village has seen an unexpected rise in pregnancies. Some of your villagers ask your permission to hold a festival in honor of Agalea, protectress of motherhood. Agalea has never been recognized as a saint by the imperial Senate, and many consider her veneration to be a primitive Palaic holder. Her festivals tend to be rather raucous, befitting the theme of fertility, and also rather expensive. It would certainly set tongues wagging in the district. Do you grant permission?", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && (MobileParty.MainParty.LastVisitedSettlement.IsVillage && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan) && MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "empire");
			string text77 = "{=EFUNx0cv}A vat of wine for you to dance in honor of the lady Agalea!";
			List<IncidentEffect> list77 = new List<IncidentEffect>();
			list77.Add(IncidentEffect.GoldChange(() => -200));
			list77.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list77.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list77.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10));
			list77.Add(IncidentEffect.InfluenceChange(-100f));
			list77.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20));
			list77.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10));
			incident64.AddOption(text77, list77, null, null);
			string text78 = "{=NWNIVZF0}Give the villagers a stern lecture on the importance of sound doctrine and lawful worship";
			List<IncidentEffect> list78 = new List<IncidentEffect>();
			list78.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list78.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list78.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			list78.Add(IncidentEffect.InfluenceChange(100f));
			list78.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 5));
			incident64.AddOption(text78, list78, null, null);
			string text79 = "{=J9BsFeSS}Tell the villagers to keep things discrete";
			List<IncidentEffect> list79 = new List<IncidentEffect>();
			list79.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 10));
			list79.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5));
			incident64.AddOption(text79, list79, null, null);
			Incident incident65 = this.RegisterIncident("incident_migrant_influx", "{=OY5LLcxv}Migrant Influx", "{=2KaXj3Qa}The depredations of the {NEARBY_ENEMY_FACTION} has driven many farmers from their homes. Those who have land usually return to it, but many of the poorer are seeking a new place to live. You speak to a few encamped on the outskirts of your village. There is no land for them. They seem eager to work as tenants, but there is always a possibility of tensions with the more established village families.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				if (MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
				{
					return false;
				}
				Kingdom kingdom = Kingdom.All.FirstOrDefault((Kingdom k) => k.IsAtWarWith(Hero.MainHero.MapFaction));
				if (kingdom == null)
				{
					return false;
				}
				description.SetTextVariable("NEARBY_ENEMY_FACTION", kingdom.Name);
				return MobileParty.MainParty.LastVisitedSettlement.IsVillage && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan;
			});
			string text80 = "{=8JqCDWRd}Welcome them, and give them a donation to purchase small plots of land.";
			List<IncidentEffect> list80 = new List<IncidentEffect>();
			list80.Add(IncidentEffect.GoldChange(() => -500));
			list80.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list80.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20));
			list80.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10).WithChance(0.25f));
			incident65.AddOption(text80, list80, null, null);
			string text81 = "{=U4UJn9X4}Let them stay. Tell both newcomers and longtime residents to get along, and hope for the best.";
			List<IncidentEffect> list81 = new List<IncidentEffect>();
			list81.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 25));
			list81.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10).WithChance(0.5f));
			incident65.AddOption(text81, list81, null, null);
			incident65.AddOption("{=o4aQ2UEL}Tell them to make their homes someplace else.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) }, null, null);
			Incident incident66 = this.RegisterIncident("incident_nomads_wish_to_settle", "{=5vN2NZ8R}Nomads Wish to Settle", "{=U4Ybz08z}A desperate band of nomads has arrived at your village, seeking a place to settle down. Their herds were ravaged by an epidemic, they say, and they are ready to take up farming or work as laborers. Otherwise, they must starve or give themselves to another tribe as servants. The villagers are skeptical. Nomads are too lazy to be farmers, they say, preferring horse theft and feuds.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
				bool flag = new string[] { "khuzait", "aserai", "empire_s" }.Contains(village.Settlement.Culture.StringId);
				bool flag2 = Hero.MainHero.Clan.Fiefs.Contains(village.Bound.Town);
				return flag && flag2;
			});
			string text82 = "{=iSZCkpt3}Welcome them with open arms and a small donation to buy plots of land for their homes";
			List<IncidentEffect> list82 = new List<IncidentEffect>();
			list82.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list82.Add(IncidentEffect.GoldChange(() => -500));
			list82.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20));
			list82.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10).WithChance(0.25f));
			incident66.AddOption(text82, list82, null, null);
			string text83 = "{=taVbaFFW}Sternly instruct them to respect the traditions of settled life, even though the cold welcome might scare some away";
			List<IncidentEffect> list83 = new List<IncidentEffect>();
			list83.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list83.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list83.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 10));
			incident66.AddOption(text83, list83, null, null);
			incident66.AddOption("{=jUlHTiSU}Agree with the villagers that nomads are more trouble than they are worth.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, -100) }, null, null);
			Incident incident67 = this.RegisterIncident("incident_conflict_over_commons_both_yours", "{=uqB1fKFY}Conflict Over Commons", "{=raLLq10c}Grazing land has grown scarce near {TOWN_OR_CASTLE_NAME}, and a heath between {POORER_VILLAGE} and {RICHER_VILLAGE}, covered with thistle and formerly of little value, has become a point of contention. {POORER_VILLAGE} claims that they have been taking goats there for generations, but {RICHER_VILLAGE} has produced a document showing legal title.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
				{
					return false;
				}
				List<Village> source = (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
					orderby x.Hearth
					select x).ToList<Village>();
				Village village = source.LastOrDefault<Village>();
				Village village2 = source.FirstOrDefault<Village>();
				if (village == null || village2 == null || village == village2)
				{
					return false;
				}
				float num = Village.All.Sum((Village x) => x.Hearth) / (float)Village.All.Count;
				if (village.Hearth < num || village2.Hearth < num)
				{
					return false;
				}
				description.SetTextVariable("TOWN_OR_CASTLE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				description.SetTextVariable("RICHER_VILLAGE", village.Name);
				description.SetTextVariable("POORER_VILLAGE", village2.Name);
				return true;
			});
			string text84 = "{=cVzpV96W}The laws of the realm are clear: no matter how many years go by, the original owner retains the land unless it is legally sold or transferred.";
			List<IncidentEffect> list84 = new List<IncidentEffect>();
			list84.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 200));
			list84.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).LastOrDefault<Village>(), 5));
			list84.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).FirstOrDefault<Village>(), -10));
			incident67.AddOption(text84, list84, null, null);
			string text85 = "{=3zEtGAII}You agree with the ancient Palaic principle: the lands belong to the spirits of the earth, not to humans, and go to those who make the best use of them.";
			List<IncidentEffect> list85 = new List<IncidentEffect>();
			list85.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list85.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list85.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).LastOrDefault<Village>(), -5));
			list85.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).FirstOrDefault<Village>(), 10));
			incident67.AddOption(text85, list85, null, null);
			string text86 = "{=wlVCpQHS}Have them split it.";
			List<IncidentEffect> list86 = new List<IncidentEffect>();
			list86.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -50));
			list86.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).FirstOrDefault<Village>(), 5));
			incident67.AddOption(text86, list86, null, null);
			string text87 = "{=YWZFOX4S}Let them know that your decision is for sale at a price";
			List<IncidentEffect> list87 = new List<IncidentEffect>();
			list87.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list87.Add(IncidentEffect.GoldChange(() => 1000));
			list87.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).LastOrDefault<Village>(), 5));
			list87.Add(IncidentEffect.VillageHearthChange(() => (from x in MobileParty.MainParty.LastVisitedSettlement.Town.Villages
				orderby x.Hearth
				select x).FirstOrDefault<Village>(), -10));
			incident67.AddOption(text87, list87, null, null);
			Incident incident68 = this.RegisterIncident("incident_conflict_over_commons", "{=uqB1fKFY}Conflict Over Commons", "{=jGafCYMe}Villagers from your fief of {VILLAGE} have started going to a marsh on the road to {VILLAGE2} to gather reeds for baskets, supplementing their earnings. Hunters from {VILLAGE2}, owned by {LORD.NAME}, say they've been disturbing the local waterfowl. Neither of you has title to the marsh, but {VILLAGE2} says that its people started going there first. Your tenants say it's not their problem if they've figured out a better way to make use of the resources.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Hero.MainHero.Clan)
				{
					return false;
				}
				Village village = this.<InitializeIncidents>g__GetVillageOne|25_310();
				Village village2 = this.<InitializeIncidents>g__GetOtherVillage|25_311();
				if (((village2 != null) ? village2.Settlement.Owner : null) == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE", village.Name);
				description.SetTextVariable("VILLAGE2", village2.Name);
				description.SetCharacterProperties("LORD", village2.Settlement.Owner.CharacterObject, false);
				return true;
			});
			string text88 = "{=8gTYsXZM}Continue to send your reed gatherers there, and have them bring staves and slings to defend their rights";
			List<IncidentEffect> list88 = new List<IncidentEffect>();
			list88.Add(IncidentEffect.SettlementRelationChange(() => this.<InitializeIncidents>g__GetOtherVillage|25_311().Settlement, -10));
			list88.Add(IncidentEffect.HeroRelationChange(() => this.<InitializeIncidents>g__GetOtherVillage|25_311().Settlement.OwnerClan.Leader, -10));
			list88.Add(IncidentEffect.VillageHearthChange(new Func<Village>(this.<InitializeIncidents>g__GetVillageOne|25_310), -5).WithChance(0.25f));
			list88.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20));
			list88.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			incident68.AddOption(text88, list88, null, null);
			string text89 = "{=0xiAZr96}Tell {NOBLE.NAME} that your people will get their reeds elsewhere and stop frightening off the game.";
			List<IncidentEffect> list89 = new List<IncidentEffect>();
			list89.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5));
			list89.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, -100));
			list89.Add(IncidentEffect.HeroRelationChange(() => this.<InitializeIncidents>g__GetOtherVillage|25_311().Settlement.OwnerClan.Leader, 20));
			incident68.AddOption(text89, list89, delegate(TextObject text)
			{
				Village village = this.<InitializeIncidents>g__GetOtherVillage|25_311();
				text.SetCharacterProperties("NOBLE", village.Settlement.OwnerClan.Leader.CharacterObject, false);
				return true;
			}, null);
			Incident incident69 = this.RegisterIncident("incident_educational_advancements", "{=RZJ0f8qo}Educational Advancements", "{=2OQlwt7x}Villagers say that a wanderer has recently arrived and offered to teach their children to read. He is not only well-educated, but he has a gift for holding the attention of restless 10-year-olds. However, he seems to have spent time with one of the rebel bands that were common around here ten years back, and occasionally speaks of a coming reign of justice brought by Heaven, in a way that doesn't seem like one of his ironic jokes.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && Hero.MainHero.Clan.Fiefs.Contains(MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town));
			string text90 = "{=Wuga4ZqG}Let him stay. It's good for children to read, and it's not like they take their elders seriously anyway";
			List<IncidentEffect> list90 = new List<IncidentEffect>();
			list90.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5));
			list90.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10));
			list90.Add(IncidentEffect.InfluenceChange(20f));
			incident69.AddOption(text90, list90, null, null);
			incident69.AddOption("{=zEPpgYaf}Reading won't help a farmboy plough, but might goad him to do foolish things that get him killed", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, 100) }, null, null);
			string text91 = "{=3OerG5Ik}Take the teacher aside, and tell him the elder will pay him a handsome stipend that will stop abruptly if he breathes a word about politics or theology.";
			List<IncidentEffect> list91 = new List<IncidentEffect>();
			list91.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list91.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 10));
			list91.Add(IncidentEffect.GoldChange(() => -200));
			incident69.AddOption(text91, list91, null, null);
			Incident incident70 = this.RegisterIncident("incident_gold_rush", "{=8jG2vlOu}Gold Rush", "{=eQz8b4TH}The village is buzzing with a poorly kept secret. Following a rare but fierce rainstorm in the dry hills, fishermen have begun to find small flakes of gold in a local stream. If word gets out, there will be a huge influx of fortune-seekers into the area. The gold will last a few months, and it's not clear how much of it will benefit the village or its owner.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && Hero.MainHero.Clan.Fiefs.Contains(MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town));
			string text92 = "{=mEqk8UCG}This region could use more people. Let word get out.";
			List<IncidentEffect> list92 = new List<IncidentEffect>();
			list92.Add(IncidentEffect.VillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Village, 20));
			list92.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -10));
			list92.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list92.Add(IncidentEffect.InfestNearbyHideout(() => MobileParty.MainParty.LastVisitedSettlement));
			incident70.AddOption(text92, list92, null, null);
			string text93 = "{=aNGQJJkM}As liege of the village, the gold belongs to you. Post guards along the river to stop unauthorized prospecting";
			List<IncidentEffect> list93 = new List<IncidentEffect>();
			list93.Add(IncidentEffect.GoldChange(() => 1000));
			list93.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			list93.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -200));
			incident70.AddOption(text93, list93, null, null);
			string text94 = "{=cECwyvjY}Encourage the villagers to mine it stealthily, ensuring their cooperation by allowing them to keep it";
			List<IncidentEffect> list94 = new List<IncidentEffect>();
			list94.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list94.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, 100));
			list94.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Town, -5));
			incident70.AddOption(text94, list94, null, null);
			Incident incident71 = this.RegisterIncident("incident_wages_of_war_and_diseases", "{=7q53TLg7}Wages of war and diseases", "{=FZXYHC7s}Between the wars and an epidemic, the villages near {FIEF_NAME} are experiencing a shortage of people, especially young men. Fields are going fallow for lack of hands to plough them, especially those cleared only in recent generations where the title is in dispute. As liege, you can redistribute this unused land, but you will need to find labor.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Days(60f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
				{
					return false;
				}
				Building building = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetWagesOfWarAndDiseasesBuilding|25_330();
				if (building == null)
				{
					return false;
				}
				float num = Village.All.Average((Village x) => x.Hearth);
				if (MobileParty.MainParty.LastVisitedSettlement.BoundVillages.Average((Village x) => x.Hearth) > num)
				{
					return false;
				}
				description.SetTextVariable("FIEF_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return building.CurrentLevel == building.BuildingType.StartLevel;
			});
			string text95 = "{=wnvUsbSs}Seize the land as your own, attracting desperate peasants from surrounding districts with exploitative sharecropping agreements";
			List<IncidentEffect> list95 = new List<IncidentEffect>();
			list95.Add(IncidentEffect.BuildingLevelChange(new Func<Building>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetWagesOfWarAndDiseasesBuilding|25_330), () => 1));
			list95.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list95.Add(IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10));
			incident71.AddOption(text95, list95, null, null);
			string text96 = "{=8jCcqBSV}Distribute the land to some of your veterans as a reward for their service";
			List<IncidentEffect> list96 = new List<IncidentEffect>();
			list96.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list96.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 10));
			list96.Add(IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20));
			incident71.AddOption(text96, list96, null, null);
			string text97 = "{=QSEZfwVk}Distribute money to the region's farmers so they can hire agricultural laborers or provide attractive dowries for their daughters ";
			List<IncidentEffect> list97 = new List<IncidentEffect>();
			list97.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 200));
			list97.Add(IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 30));
			list97.Add(IncidentEffect.GoldChange(() => -500));
			list97.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10));
			incident71.AddOption(text97, list97, null, null);
			Incident incident72 = this.RegisterIncident("incident_successful_harvest", "{=MaaFv4Po}Successful Harvest", "{=evcKUaab}An early thaw and a warm spring has ensured a bumper crop, which the prayers of the villagers and the efforts of the local cats have protected from rats and birds. Such bounties occur rarely. What will you do?", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Days(60f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan && (CampaignTime.Now.GetSeasonOfYear == CampaignTime.Seasons.Summer || CampaignTime.Now.GetSeasonOfYear == CampaignTime.Seasons.Autumn));
			string text98 = "{=HT1mQuCI}Feast! Thank Heaven and the spirits of the land, and make merry";
			List<IncidentEffect> list98 = new List<IncidentEffect>();
			list98.Add(IncidentEffect.MoraleChange(10f));
			list98.Add(IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5));
			list98.Add(IncidentEffect.TownBoundVillageHearthChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10));
			list98.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 5));
			list98.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list98.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			incident72.AddOption(text98, list98, null, null);
			string text99 = "{=c5vobNeb}Encourage the farmers to sell the crops and make much-needed improvements to irrigation and roads";
			List<IncidentEffect> list99 = new List<IncidentEffect>();
			list99.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list99.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20));
			incident72.AddOption(text99, list99, null, null);
			string text100 = "{=ZaBiC5XV}Go through your accounts to see who might owe you back taxes";
			List<IncidentEffect> list100 = new List<IncidentEffect>();
			list100.Add(IncidentEffect.GoldChange(() => 1000));
			list100.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -200));
			incident72.AddOption(text100, list100, null, null);
			Incident incident73 = this.RegisterIncident("incident_haunted_dreams", "{=99w595qn}Haunted Dreams", "{=yXXbY1bb}You lie down for a well-deserved nap, but you slip into an uneasy dream. You are sailing on a wind-tossed sea. A wave crashes on your bow, sending spray across the decks. You look closely, and see that it is not water but blood sloshing around across your feet. You stare out into the sea, and the breakers take on the forms of men you have slain in battle.", IncidentsCampaignBehaviour.IncidentTrigger.WaitingInSettlement, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 200f && Campaign.Current.GetCampaignBehavior<IStatisticsCampaignBehavior>().GetNumberOfTroopsKnockedOrKilledByPlayer() > 0);
			incident73.AddOption("{=sxrBpIUk}Have I killed without good reason? Heaven wishes me to repent! I shall choose the peaceful path. When there is one, that is...", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			incident73.AddOption("{=a0MWe8GO}To where do we sail on this sea of blood? To glory, that is where!", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			incident73.AddOption("{=B1cyEaQA}Dreams are but a temporary madness. Heaven does not speak to us in this way.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, 100) }, null, null);
			Incident incident74 = this.RegisterIncident("incident_market_manipulation", "{=tGwbM59L}Market Manipulation", "{=5OuDl2Po}As you come into town, the merchant {MERCHANT_NAME} makes you an offer. Local merchants are on edge awaiting an incoming caravan from {PLACE}. If you claim that you found its wreckage, looted by bandits, the prices of goods would rise considerably. He offers to pay you 1000 denars, in addition to whatever extra you can make by selling your own wares once the prices rise.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.CurrentSettlement == null)
				{
					return false;
				}
				Town randomElement = Town.AllTowns.GetRandomElement<Town>();
				Hero hero = this.<InitializeIncidents>g__GetMarketManipulationMerchant|25_348();
				if (hero == null)
				{
					return false;
				}
				description.SetTextVariable("MERCHANT_NAME", hero.Name);
				description.SetTextVariable("PLACE", randomElement.Name);
				return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 20;
			});
			string text101 = "{=SLASexva}Accept, but leave enough room for doubt in your account that it won't look too bad if the caravan turns up.";
			List<IncidentEffect> list101 = new List<IncidentEffect>();
			list101.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list101.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list101.Add(IncidentEffect.GoldChange(() => 1000));
			list101.Add(IncidentEffect.SkillChange(DefaultSkills.Trade, 50f));
			list101.Add(IncidentEffect.Custom(null, delegate
			{
				TownMarketData marketData = Settlement.CurrentSettlement.Town.MarketData;
				foreach (ItemCategory itemCategory in from x in ItemCategories.All
					where x.IsTradeGood
					select x)
				{
					marketData.SetDemand(itemCategory, marketData.GetDemand(itemCategory) * 1.1f);
				}
				return new List<TextObject> { new TextObject("{=OI4jkYuY}Demands of trade goods raised by {AMOUNT}% in {TOWN}", null).SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) };
			}, (IncidentEffect effect) => new List<TextObject> { new TextObject("{=uiomHbfS}Raise demand of trade goods by {AMOUNT}% in {TOWN}", null).SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) }));
			incident74.AddOption(text101, list101, null, null);
			string text102 = "{=EyHD7gjy}Say you'll swear up and down that you saw the wreckage, damaging your reputation, but demand 2000 denars instead.";
			List<IncidentEffect> list102 = new List<IncidentEffect>();
			list102.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -200));
			list102.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -200));
			list102.Add(IncidentEffect.GoldChange(() => 2000));
			list102.Add(IncidentEffect.SkillChange(DefaultSkills.Trade, 100f));
			list102.Add(IncidentEffect.Custom(null, delegate
			{
				TownMarketData marketData = Settlement.CurrentSettlement.Town.MarketData;
				foreach (ItemCategory itemCategory in from x in ItemCategories.All
					where x.IsTradeGood
					select x)
				{
					marketData.SetDemand(itemCategory, marketData.GetDemand(itemCategory) * 1.1f);
				}
				return new List<TextObject> { new TextObject("{=OI4jkYuY}Demands of trade goods raised by {AMOUNT}% in {TOWN}", null).SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) };
			}, (IncidentEffect effect) => new List<TextObject> { new TextObject("{=uiomHbfS}Raise demand of trade goods by {AMOUNT}% in {TOWN}", null).SetTextVariable("AMOUNT", 10).SetTextVariable("TOWN", Settlement.CurrentSettlement.Name) }));
			incident74.AddOption(text102, list102, null, null);
			string text103 = "{=aMMxVIpa}Erupt in anger for all to see, and curse {MERCHANT} for taking you for a base liar.";
			List<IncidentEffect> list103 = new List<IncidentEffect>();
			list103.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list103.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list103.Add(IncidentEffect.SettlementRelationChange(() => Settlement.CurrentSettlement, 10));
			list103.Add(IncidentEffect.HeroRelationChange(new Func<Hero>(this.<InitializeIncidents>g__GetMarketManipulationMerchant|25_348), -20));
			incident74.AddOption(text103, list103, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetMarketManipulationMerchant|25_348();
				text.SetTextVariable("MERCHANT", hero.Name);
				return true;
			}, null);
			incident74.AddOption("{=UbZMlyk6}Take a pass on the matter.", new List<IncidentEffect>(), null, null);
			Incident incident75 = this.RegisterIncident("incident_gnostic_boys", "{=hewpozXg}Gnostic Boys", "{=H7Co1wqC}Two of your {NOBLE_TIER} have lately been causing a stir in the camp with their irreverent religious preaching. They propound that this world is a illusion, created by a being called the Demiurge for its own amusement, and humans are but game-pieces. You think this may be the Marcorionist doctrine, declared heretical by the imperial Senate some years ago. It is clearly disturbing your more traditional-minded troops and is probably also not good for your party's reputation.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage || MobileParty.MainParty.LastVisitedSettlement.Village.Bound.IsCastle)
				{
					return false;
				}
				CharacterObject characterObject = this.<InitializeIncidents>g__GetGnosticBoysNobleTroop|25_361();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("NOBLE_TIER", characterObject.Name);
				return true;
			});
			incident75.AddOption("{=WyuxrGqC}Tell them, in a towering fury, that if they do not wish to be burned then they had best either leave your party or cease this sacrilege", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.MoraleChange(20f),
				IncidentEffect.InfluenceChange(50f),
				IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetGnosticBoysNobleTroop|25_361), -2)
			}, null, null);
			string text104 = "{=4WZ5CWm8}Take them aside and tell them that while nihilism is fine for a boozy salon in {TOWN_NAME}, it's a bit much for men who must every day put their lives on the line.";
			List<IncidentEffect> list104 = new List<IncidentEffect>();
			list104.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list104.Add(IncidentEffect.MoraleChange(-20f).WithChance(0.5f));
			incident75.AddOption(text104, list104, delegate(TextObject text)
			{
				text.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Name);
				return true;
			}, null);
			incident75.AddOption("{=vzFoybv0}Tell your party that, if this is true, then men are free to choose their own purpose in this world - and anyone who stands with you should make his purpose to help those in need.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.InfluenceChange(-50f)
			}, null, null);
			incident75.AddOption("{=e07rQCcq}Tell the men that this is splendid news for brave men like themselves, as surely nothing would amuse the Demiurge more than a good fight.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.MoraleChange(20f),
				IncidentEffect.InfluenceChange(-50f)
			}, null, null);
			Incident incident76 = this.RegisterIncident("incident_speculative_investment", "{=12bmA5qD}Speculative Investment", "{=vPNDzUAF}You strike up a conversation with a trader in town, who is desperate for news of the wars. When you tell him about the looting of {VILLAGE_NAME}, he makes a proposition. The price of grain will almost certainly rise, and he intends to buy up food now to sell it later when the hunger hits. He needs money, and he will sell you {OTHER_GOOD} at far below their market value - 1000 denars. He asks you also to keep this information to yourself.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				VillageStateChangedLogEntry villageStateChangedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<VillageStateChangedLogEntry>((VillageStateChangedLogEntry x) => x.NewState == Village.VillageStates.Looted && x.Village.IsProducing(DefaultItems.Grain));
				if (villageStateChangedLogEntry == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE_NAME", villageStateChangedLogEntry.Village.Name);
				ItemObject itemObject = this.<InitializeIncidents>g__GetSpeculativeInvestmentOtherGood|25_365();
				description.SetTextVariable("OTHER_GOOD", itemObject.Name);
				return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 20;
			});
			string text105 = "{=aUui6FYp}All's fair in trade and war. You accept the deal.";
			List<IncidentEffect> list105 = new List<IncidentEffect>();
			list105.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 50));
			list105.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -50));
			list105.Add(IncidentEffect.GoldChange(() => -500));
			list105.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetSpeculativeInvestmentOtherGood|25_365), delegate
			{
				ItemObject itemObject = this.<InitializeIncidents>g__GetSpeculativeInvestmentOtherGood|25_365();
				return (int)Math.Round((double)(1000f / (float)itemObject.Value));
			}));
			incident76.AddOption(text105, list105, null, null);
			incident76.AddOption("{=2xvqOUJh}Tell him quietly that you will not profit off of the misery of others.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) }, null, null);
			incident76.AddOption("{=hnMUxRyt}Denounce him angrily in public as a food hoarder for all to hear, so that word gets around of your compassion and mercy.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -150),
				IncidentEffect.RenownChange(5f)
			}, null, null);
			Incident incident77 = this.RegisterIncident("incident_mad_dog", "{=iuAMSSTY}Mad Dog", "{=nyZpbVbv}Your men are very proud of their leader - perhaps too proud. Every time they go into town, they spread exaggerated tales of your recklessness in battle. You imagine that this could win you a few admiring recruits, but more and more you've caught a look that suggests fear or distrust, as though you're a wild animal or a demon.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 300f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident77.AddOption("{=bbH4PqtD}Let the tales spread! Sanity and caution do not inspire legend", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -300),
				IncidentEffect.RenownChange(10f),
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetMadDogTroop|25_370), 1)
			}, null, null);
			incident77.AddOption("{=Qbdym8vH}In a towering rage, tell your men that you will not tolerate lies of any sort.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100)
			}, null, null);
			incident77.AddOption("{=EzpB6hJG}Try to play down the image of a madman by being as friendly as possible on your next visit to town.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100)
			}, null, null);
			Incident incident78 = this.RegisterIncident("incident_besiegers_blues", "{=uTsdJCSU}Besiegers' Blues", "{=IxgJotDk}At first this siege gave your men a welcome respite from the hardships of the march, but now inactivity and boredom has started to take a toll. Even dice and gossip have lost their charms, and you're starting to see quarrels and lapses in discipline.", IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege, IncidentsCampaignBehaviour.IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.MemberRoster.TotalRegulars >= 40)
				{
					SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
					object obj;
					if (playerSiegeEvent == null)
					{
						obj = null;
					}
					else
					{
						SiegeEvent.SiegeEnginesContainer siegeEngines = playerSiegeEvent.BesiegerCamp.SiegeEngines;
						obj = ((siegeEngines != null) ? siegeEngines.SiegePreparations : null);
					}
					return obj != null;
				}
				return false;
			});
			incident78.AddOption("{=LUap9uP0}Drill your men for the upcoming assault, even though the ever-present risk of injury might put several out of commission when they're needed most.", new List<IncidentEffect>
			{
				IncidentEffect.PartyExperienceChance(100),
				IncidentEffect.WoundTroopsRandomly(0.05f).WithChance(0.5f)
			}, null, null);
			string text106 = "{=GTPPtara}March about the camp, searching for specks of rust on swords or sentries who aren't fully attentive to their watch. Give any violators hard, pointless tasks to teach them to keep their standards up.";
			List<IncidentEffect> list106 = new List<IncidentEffect>();
			list106.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list106.Add(IncidentEffect.SiegeProgressChange(() => PlayerSiege.PlayerSiegeEvent.BesiegerCamp.SiegeEngines.SiegePreparations.Progress * 0.2f));
			incident78.AddOption(text106, list106, null, null);
			incident78.AddOption("{=bp8kKb1A}Let the men enjoy their rest.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.MoraleChange(-20f)
			}, null, null);
			Incident incident79 = this.RegisterIncident("incident_swaggering_shield_brothers", "{=KtJYTHVi}Swaggering Shield-Brothers", "{=YCPF4qyz}You are approached by a pair of Skolderbroda. They are anxious to fight for you and ask only that you pay them a token sum. They say you that a warlord like you deserves the services of men such as themselves - praise to you, but a slight to the rest of your party. One of your troops tells you that he's sure that these men must have broken their vows to their company, or why else would they be so eager to join you?", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.Clan.Renown >= 150f && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident79.AddOption("{=Kqr9pVCU}Take them into your company. Your troops will need to bear any insult.", new List<IncidentEffect>
			{
				IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetSkolderbrotvaTroop|25_374), 2),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -200),
				IncidentEffect.MoraleChange(-5f)
			}, null, null);
			incident79.AddOption("{=78VrrDRM}Tell them that you believe them to be oathbreakers, and as such have no place in your ranks.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) }, null, null);
			string text107 = "{=1b9oYu6v}Tell them they can join you and be as arrogant as they like - so long as they beat one of your men in a duel to first blood.";
			list4 = new List<IncidentEffect>();
			list4.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			List<IncidentEffect> list107 = list4;
			IncidentEffect[] array5 = new IncidentEffect[2];
			array5[0] = IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetSkolderbrotvaTroop|25_374), 2);
			array5[1] = IncidentEffect.WoundTroop(() => (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				orderby x.Character.Tier descending
				select x).FirstOrDefault((TroopRosterElement x) => !x.Character.IsHero).Character, 1);
			list107.Add(IncidentEffect.Group(array5).WithChance(0.5f));
			incident79.AddOption(text107, list4, null, null);
			Incident incident80 = this.RegisterIncident("incident_ill_conceived_plans", "{=VSLbywBT}Ill-Conceived Plans", "{=K8DJmY4L}Several of your {TROOP_TYPE} have an idea for the next battle. They suggest that they be hidden in a supply wagon and left for the enemy to discover and drag into camp, whereupon they will leap out and wreak havoc. They are very proud of their scheme, even though you can think of half a dozen reasons it won't work.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.MemberRoster.TotalRegulars < 10)
				{
					return false;
				}
				CharacterObject character = IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
					where !x.Character.IsHero
					select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
				if (character == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_TYPE", character.Name);
				return true;
			});
			incident80.AddOption("{=X68PZrYv}Tell them that every people in Calradia has its own legend about warriors concealed in unlikely places, and you can't imagine them getting away with it.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			string text108 = "{=Gsazqfhj}Let them test their plan. Buy a hide, sew the men into it with their weapons, and gather the company to watch in amusement as they struggle to free themselves.";
			List<IncidentEffect> list108 = new List<IncidentEffect>();
			list108.Add(IncidentEffect.GoldChange(() => -50));
			list108.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 50));
			list108.Add(IncidentEffect.DisorganizeParty());
			list108.Add(IncidentEffect.PartyExperienceChance(100));
			incident80.AddOption(text108, list108, null, null);
			incident80.AddOption("{=YhxsQjGM}Nod quickly as though you weren't really listening and move on to some other task.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Calculating, -50) }, null, null);
			Incident incident81 = this.RegisterIncident("incident_heat_and_dust", "{=HoNCyyfW}Heat and Dust", "{=42bGMh4X}Your men were not expecting to spend so much time in the desert. They complain that baggage that they might gladly carry themselves in normal times becomes a crushing burden when the blazing sun is high in the sky. They have located a number of pack animals in the markets and ask you to buy them to lighten their own loads.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.HardTravel, CampaignTime.Years(1000f), (TextObject description) => Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) == TerrainType.Desert && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			string text109 = "{=ACzeec4e}Agree to purchase the camels.";
			List<IncidentEffect> list109 = new List<IncidentEffect>();
			list109.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 50));
			list109.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("camel"), () => 4));
			list109.Add(IncidentEffect.GoldChange(() => -1000));
			incident81.AddOption(text109, list109, null, null);
			incident81.AddOption("{=uD8su80f}Tell them that you need the money for other purposes.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -200) }, null, null);
			Incident incident82 = this.RegisterIncident("incident_preemptive_work", "{=Zd3jBBun}Preemptive Work", "{=3IphvZTK}In your absence, several families in your district have been hard at work draining a marsh and turning it into a field. The survey records in your treasury however indicate clearly that the marsh is part of your domain. They almost certainly did not know this, but you are entitled to claim the field or charge them for working it.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown | IncidentsCampaignBehaviour.IncidentTrigger.EnteringCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsFortification || MobileParty.MainParty.LastVisitedSettlement.OwnerClan != Clan.PlayerClan)
				{
					return false;
				}
				Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
				bool flag = false;
				for (float num = -3f; num <= 3f; num += 0.25f)
				{
					for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
					{
						if (num * num + num2 * num2 <= 9f)
						{
							CampaignVec2 campaignVec = lastVisitedSettlement.Position + new Vec2(num, num2);
							if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Desert)
							{
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
				Building building = IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetPreemptiveWorkBuilding|25_387();
				return building != null && building.CurrentLevel == building.BuildingType.StartLevel;
			});
			string text110 = "{=9e8I99t3}Transfer ownership to the farmers to encourage such enterprise, even if others may take advantage of you in the future.";
			List<IncidentEffect> list110 = new List<IncidentEffect>();
			list110.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list110.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -50));
			list110.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 20));
			incident82.AddOption(text110, list110, null, null);
			string text111 = "{=j0HmgDOA}Maintain your claim to the land, but allow the farmers to make use of it for only a token rent.";
			List<IncidentEffect> list111 = new List<IncidentEffect>();
			list111.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list111.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 50));
			list111.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 15));
			list111.Add(IncidentEffect.GoldChange(() => 500));
			incident82.AddOption(text111, list111, null, null);
			string text112 = "{=xcPJPtaT}Demand the land for yourself.";
			List<IncidentEffect> list112 = new List<IncidentEffect>();
			list112.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list112.Add(IncidentEffect.BuildingLevelChange(new Func<Building>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetPreemptiveWorkBuilding|25_387), () => 1));
			list112.Add(IncidentEffect.TownBoundVillageRelationChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -20));
			incident82.AddOption(text112, list112, null, null);
			Incident incident83 = this.RegisterIncident("incident_through_proper_channels", "{=S12S4pmL}Through the proper channels", "{=3eOOT66E}One of your tenants has a proposal for you. He proposes to dig a channel known as a qanah into a hillside, where the water table is high, which will allow him to flood some low-lying ground and turn it into an oasis garden. The barren hillside is your property, though at present it yields no income of any kind.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown | IncidentsCampaignBehaviour.IncidentTrigger.EnteringCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.CurrentSettlement == null)
				{
					return false;
				}
				Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
				bool flag = false;
				for (float num = -3f; num <= 3f; num += 0.25f)
				{
					for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
					{
						if (num * num + num2 * num2 <= 9f)
						{
							CampaignVec2 campaignVec = currentSettlement.Position + new Vec2(num, num2);
							if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Desert)
							{
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
				return flag && MobileParty.MainParty.CurrentSettlement.IsFortification && Hero.MainHero.Clan == MobileParty.MainParty.CurrentSettlement.OwnerClan;
			});
			string text113 = "{=7f06ovrO}Applaud his plan, and tell him that you will charge him nothing to sink a channel through your property.";
			List<IncidentEffect> list113 = new List<IncidentEffect>();
			list113.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list113.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 20));
			incident83.AddOption(text113, list113, null, null);
			string text114 = "{=iB2g7XBt}Insist he purchase the hillside from your at market rate.";
			List<IncidentEffect> list114 = new List<IncidentEffect>();
			list114.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list114.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 15));
			list114.Add(IncidentEffect.GoldChange(() => 300));
			incident83.AddOption(text114, list114, null, null);
			string text115 = "{=P8W5OQB9}Wait until he digs the channel and clears the fields, then charge him heavily to use the water without which his effort will be for nothing.";
			List<IncidentEffect> list115 = new List<IncidentEffect>();
			list115.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -200));
			list115.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 50));
			list115.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.CurrentSettlement.Town, 10));
			list115.Add(IncidentEffect.GoldChange(() => 500));
			incident83.AddOption(text115, list115, null, null);
			Incident incident84 = this.RegisterIncident("incident_dodgy_spice", "{=5OzX71s3}Dodgy Spice", "{=QEP3ntdb}Merchants are savvy to most attempts to pass off most types of shoddy goods as top quality, but one of your {TROOP.NAME} knows a few tricks that aren't in the usual rogue's repetoire. He suggests buying a selection of spices from the market, then sprinkling a bit of pomegranate rind in with the saffron, some cassia bark with the cinnamon, and a lead compound known as litharge with the pepper.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (Hero.MainHero.GetSkillValue(DefaultSkills.Trade) < 10)
				{
					return false;
				}
				TroopRosterElement troopRosterElement = this.<InitializeIncidents>g__GetRandomTroop|25_401();
				if (troopRosterElement.Character == null)
				{
					return false;
				}
				description.SetCharacterProperties("TROOP", troopRosterElement.Character, false);
				return true;
			});
			string text116 = "{=lrysxcAE}A bit of lead never hurt anyone! Try them all, and split the profits with your {TROOP.NAME}.";
			List<IncidentEffect> list116 = new List<IncidentEffect>();
			list116.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list116.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -300));
			list116.Add(IncidentEffect.GoldChange(() => -100));
			list116.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1));
			incident84.AddOption(text116, list116, delegate(TextObject text)
			{
				TroopRosterElement troopRosterElement = this.<InitializeIncidents>g__GetRandomTroop|25_401();
				text.SetCharacterProperties("TROOP", troopRosterElement.Character, false);
				return true;
			}, null);
			string text117 = "{=tDK8b1J8}You vaguely recall hearing that lead might be poisonous, but you'll try the others.";
			List<IncidentEffect> list117 = new List<IncidentEffect>();
			list117.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list117.Add(IncidentEffect.GoldChange(() => -150));
			list117.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1));
			incident84.AddOption(text117, list117, null, null);
			incident84.AddOption("{=MfbK2Sqa}Tell your man to save his ingenuity for battle, not trade.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -50)
			}, null, null);
			string text118 = "{=a5gLRsa4}Thank your man bruskly and try his trick, but pay him nothing.";
			List<IncidentEffect> list118 = new List<IncidentEffect>();
			list118.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -200));
			list118.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list118.Add(IncidentEffect.ChangeItemAmount(() => Game.Current.ObjectManager.GetObject<ItemObject>("salt"), () => 1));
			incident84.AddOption(text118, list118, null, null);
			Incident incident85 = this.RegisterIncident("incident_sorrows_of_war", "{=FL8rUSWo}Sorrows of War", "{=a939QfLJ}As you leave {TOWN_NAME}, a young woman throws herself in front of your horse. She raises her face to the sky, tears at her hair, and wails that her young husband signed up to fight for you and was slain in battle. \"You took him from me!\" she screams. It is not clear what she wants.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<PlayerBattleEndedLogEntry>((PlayerBattleEndedLogEntry x) => true);
				if (playerBattleEndedLogEntry == null)
				{
					return false;
				}
				if (!playerBattleEndedLogEntry.HasHeavyCausality)
				{
					return false;
				}
				description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return true;
			});
			string text119 = "{=8uabQDxp}Tell her, as soberly as you can, that her husband gave his life for a noble cause. Hand her a purse of 100 denars, and tell her that you are sure that he would be happy if she marries again, as long as he remains in her memory.";
			List<IncidentEffect> list119 = new List<IncidentEffect>();
			list119.Add(IncidentEffect.GoldChange(() => -100));
			list119.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 50));
			incident85.AddOption(text119, list119, null, null);
			incident85.AddOption("{=kZJvgtE3}Dismount, burst into tears, take her hands in yours and tell her that every life lost in your service tears at your heart, and that you want nothing more but for these wars to end.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
			}, null, null);
			incident85.AddOption("{=52l7VuWI}Tell her that Heaven expects her to bear her fate with dignity.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			string text120 = "{=Sz28bZOI}Have your purser speak to her at length about what she might need, as you keep your distance. ";
			List<IncidentEffect> list120 = new List<IncidentEffect>();
			list120.Add(IncidentEffect.DisorganizeParty());
			list120.Add(IncidentEffect.GoldChange(() => -50));
			incident85.AddOption(text120, list120, null, null);
			Incident incident86 = this.RegisterIncident("incident_souvenirs", "{=LAMmAUvt}Souvenirs", "{=PqzLYt6i}Some time recently - you're not sure exactly when - your men started a new post-battle tradition - going about the field collecting grisly trophies from slain enemies. Now you can't help noticing these prizes dangling from their belts or the halters of their horses.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PostBattle, CampaignTime.Years(1000f), (TextObject description) => Settlement.CurrentSettlement != null && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5);
			incident86.AddOption("{=1zsWhG2N}Tell your men as best you can that, while you respect their spirit, this will frighten local farmers who don't understand warriors' ways.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.MoraleChange(-20f)
			}, null, null);
			string text121 = "{=VHlwd8na}Take a lively interest in each of these ornaments, asking your men at which battle and from which fallen foe they were taken.";
			List<IncidentEffect> list121 = new List<IncidentEffect>();
			list121.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list121.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list121.Add(IncidentEffect.MoraleChange(20f));
			list121.Add(IncidentEffect.Custom(null, delegate
			{
				List<Settlement> list161 = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetNearbyVillages|25_417();
				List<TextObject> list162 = new List<TextObject>();
				foreach (Settlement settlement in list161)
				{
					ChangeRelationAction.ApplyPlayerRelation(settlement.Notables.FirstOrDefault<Hero>(), -5, true, true);
					TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.", null);
					textObject.SetTextVariable("AMOUNT", -5);
					textObject.SetTextVariable("SETTLEMENT", settlement.Name);
					list162.Add(textObject);
				}
				return list162;
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=4o7R829M}-5 relations with three surrounding villages", null)
			}));
			incident86.AddOption(text121, list121, null, null);
			incident86.AddOption("{=xoH5LaY0}Harangue your men for their barbarism, tell them to honor rather than mock their fallen foes.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			string text122 = "{=SLAFtiET}Pretend you saw nothing.";
			List<IncidentEffect> list122 = new List<IncidentEffect>();
			list122.Add(IncidentEffect.Custom(null, delegate
			{
				List<Settlement> list161 = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetNearbyVillages|25_417();
				List<TextObject> list162 = new List<TextObject>();
				foreach (Settlement settlement in list161)
				{
					ChangeRelationAction.ApplyPlayerRelation(settlement.Notables.FirstOrDefault<Hero>(), -5, true, true);
					TextObject textObject = new TextObject("{=8IzNumMa}{?AMOUNT > 0}Increased{?}Decreased{\\?} relationship with {SETTLEMENT} by {ABS(AMOUNT)}.", null);
					textObject.SetTextVariable("AMOUNT", -5);
					textObject.SetTextVariable("SETTLEMENT", settlement.Name);
					list162.Add(textObject);
				}
				return list162;
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=4o7R829M}-5 relations with three surrounding villages", null)
			}));
			incident86.AddOption(text122, list122, null, null);
			Incident incident87 = this.RegisterIncident("incident_back_taxes", "{=NFwuiSCw}Back taxes", "{=YAnTtrLs}Your reputation as a skilled trader generally does you good, but there is always a downside. When you arrive in {TOWN}, the market inspector descends from his tower to present you with a list of fees that you supposedly accumulated over the years, based on decrees that go back several centuries. The decrees may be genuine, but in all your talk with merchants, you never heard of anyone ever paying them.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.CurrentSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN", MobileParty.MainParty.CurrentSettlement.Name);
				return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 40;
			});
			string text123 = "{=RPyFwR6I}Erupt in fury, telling the inspector that there is no way you will be fleeced in this way, legally or illegally.";
			List<IncidentEffect> list123 = new List<IncidentEffect>();
			list123.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list123.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list123.Add(IncidentEffect.RenownChange(5f));
			list123.Add(IncidentEffect.CrimeRatingChange(() => MobileParty.MainParty.CurrentSettlement.MapFaction, -10f));
			incident87.AddOption(text123, list123, null, null);
			string text124 = "{=vzA2ZJog}Pay, but say for all to hear that selective application of obscure laws is little better than theft.";
			List<IncidentEffect> list124 = new List<IncidentEffect>();
			list124.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list124.Add(IncidentEffect.GoldChange(() => -300));
			incident87.AddOption(text124, list124, null, null);
			string text125 = "{=ogP9E5Hx}Reason with the inspector over a splendid dinner and an amphora of the finest wine that is of course on you.";
			List<IncidentEffect> list125 = new List<IncidentEffect>();
			list125.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -50));
			list125.Add(IncidentEffect.InfluenceChange(50f));
			list125.Add(IncidentEffect.GoldChange(() => -100));
			incident87.AddOption(text125, list125, null, null);
			string text126 = "{=9aYLFpC9}Just hand over the money to make the problem go away.";
			List<IncidentEffect> list126 = new List<IncidentEffect>();
			list126.Add(IncidentEffect.GoldChange(() => -200));
			list126.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, -100));
			incident87.AddOption(text126, list126, null, null);
			Incident incident88 = this.RegisterIncident("incident_third_party_arbitration", "{=trfafbJW}Third-Party Arbitration", "{=XQU3Tobe}Knowing your reputation as a trader, two merchants in {TOWN_NAME} are asking you to arbitrate a dispute between them. The two had drawn up a contract wherein each partner had a half-share in an incoming shipment of wine. When it arrived, however, the sea captain demanded twice the price. The wealthier of the two partners was at the docks. He took the initiative and purchased the goods, deciding to take a loss for the sake of his reputation as a reliable supplier to the local taverns, but the second is poorer and wants out of the deal.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.CurrentSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.CurrentSettlement.Name);
				return Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 35;
			});
			incident88.AddOption("{=nbshAaHo}The letter of the contract would favor the wealthier partner. Merchants should be careful what they sign.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -50),
				IncidentEffect.InfluenceChange(50f)
			}, null, null);
			incident88.AddOption("{=bsC5zEoI}Never mind the letter of the contract, the reasonable, decent move for the richer merchant would have been to consult with his partner.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.TraitChange(DefaultTraits.Honor, -50),
				IncidentEffect.InfluenceChange(50f)
			}, null, null);
			incident88.AddOption("{=Ib04QQJY}Tell them that the whole thing gives you a headache.", new List<IncidentEffect>(), null, null);
			string text127 = "{=0ZP5fDBX}Let the wealthier merchant know that your decision is for sale.";
			List<IncidentEffect> list127 = new List<IncidentEffect>();
			list127.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -300));
			list127.Add(IncidentEffect.GoldChange(() => 1000));
			incident88.AddOption(text127, list127, null, null);
			Incident incident89 = this.RegisterIncident("incident_tipping_the_scales", "{=OXzJbvd8}Tipping the scales", "{=fhjyEgld}Your troops are taking a bit longer than usual to get ready, and you've had time to relax a bit and watch goings-on at the market. One commodity vendor in particular has caught your attention. He always uses one set of weights for the sacks that he is buying from local farmers, and another for goods that he sells. You have little doubt that one or both of these sets is a false measure, allowing him to defraud his customers.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), (TextObject description) => Hero.MainHero.GetSkillValue(DefaultSkills.Trade) >= 30);
			incident89.AddOption("{=wjpmx20I}This isn't your affair, and you don't know if other merchants or corrupt officials may be in on it. Walk away.", new List<IncidentEffect>(), null, null);
			string text128 = "{=Uidhqph1}Denounce the merchant loudly, even if you run the risk of making enemies among the town elite.";
			List<IncidentEffect> list128 = new List<IncidentEffect>();
			list128.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list128.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -20).WithChance(0.5f));
			incident89.AddOption(text128, list128, null, null);
			string text129 = "{=MGfXReQC}Walk up to the merchant, and insist he use the lighter weights to sell you one measure of his goods. Then come back later and return them, insisting he use the heavier weights.";
			List<IncidentEffect> list129 = new List<IncidentEffect>();
			list129.Add(IncidentEffect.GoldChange(() => 200));
			list129.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -50));
			list129.Add(IncidentEffect.SkillChange(DefaultSkills.Roguery, 50f));
			incident89.AddOption(text129, list129, null, null);
			Incident incident90 = this.RegisterIncident("incident_injury_accident", "{=m9MbuMlw}Injury Accident", "{=1jAyaivZ}As you pass through the village streets, a dog dashes out from among some spectators and bites at the heels of one of your {CAVALRYMAN}'s horses, panicking it. The mount rears and gallops down the street, trampling a vegetable vendor who was unable to get out of its way. She will probably walk with a limp hereafter, and her family demands compensation.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				TroopRosterElement troopRosterElement = this.<InitializeIncidents>g__GetRandomCavalryTroop|25_434();
				if (troopRosterElement.Character == null)
				{
					return false;
				}
				description.SetTextVariable("CAVALRYMAN", troopRosterElement.Character.Name);
				return MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopPredicate|25_433)).Sum((TroopRosterElement x) => x.Number) >= 10;
			});
			string text130 = "{=jwsexTeh}No horseman can control a mount that is under attack from a dog. Refuse.";
			List<IncidentEffect> list130 = new List<IncidentEffect>();
			list130.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list130.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -20));
			incident90.AddOption(text130, list130, null, null);
			string text131 = "{=5HsJkbZz}You brought your men into their village, and you must take responsibility.";
			List<IncidentEffect> list131 = new List<IncidentEffect>();
			list131.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list131.Add(IncidentEffect.GoldChange(() => -100));
			incident90.AddOption(text131, list131, null, null);
			incident90.AddOption("{=te1A19Xe}Tell your {CAVALRYMAN} that he can sort it out.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, delegate(TextObject text)
			{
				TroopRosterElement troopRosterElement = this.<InitializeIncidents>g__GetRandomCavalryTroop|25_434();
				text.SetTextVariable("CAVALRYMAN", troopRosterElement.Character.Name);
				return true;
			}, null);
			Incident incident91 = this.RegisterIncident("incident_water_supplies", "{=xYjLaJxA}Water supplies", "{=EbNZg7jp}In hot climes, often the most vulnerable part of a fortress is its water supply. Most depend on wells but those can run dry. After days spent in front of {FORTRESS_NAME}, you've caught glimpses of the defenders slipping into the hills. You explore where they've gone, and eventually you come across a rudimentary cistern. You suspect that they must be refreshing their water here.", IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege, IncidentsCampaignBehaviour.IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.BesiegedSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
				if (!MobileParty.MainParty.Position.IsValid())
				{
					return false;
				}
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face);
				return faceTerrainType == TerrainType.Steppe || faceTerrainType == TerrainType.Desert;
			});
			string text132 = "{=57VuY9ln}Poison it! Some women and children of {FORTRESS_NAME} may well perish along with the defenders, but such is war.";
			List<IncidentEffect> list132 = new List<IncidentEffect>();
			list132.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list132.Add(IncidentEffect.Custom(null, delegate
			{
				MobileParty garrisonParty = MobileParty.MainParty.BesiegedSettlement.Town.GarrisonParty;
				int num = (int)((float)garrisonParty.MemberRoster.TotalRegulars * 0.1f);
				List<TroopRosterElement> list161 = (from x in garrisonParty.MemberRoster.GetTroopRoster()
					where !x.Character.IsHero && x.Number > 0
					select x).ToList<TroopRosterElement>();
				while (num > 0 && list161.Count > 0)
				{
					int index = MBRandom.RandomInt(list161.Count);
					TroopRosterElement troopRosterElement = list161[index];
					int num2 = MBRandom.RandomInt(1, Math.Min(num, troopRosterElement.Number) + 1);
					garrisonParty.MemberRoster.AddToCounts(troopRosterElement.Character, -num2, false, 0, 0, true, -1);
					troopRosterElement.Number -= num2;
					list161[index] = troopRosterElement;
					if (troopRosterElement.Number == 0)
					{
						list161.RemoveAt(index);
					}
					num -= num2;
				}
				return new List<TextObject>
				{
					new TextObject("{=pS3PKbYB}10% of the garrison perished.", null)
				};
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=gyRuGBMF}10% of the garrison perishes", null)
			}));
			list132.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.BesiegedSettlement.Town, -20));
			incident91.AddOption(text132, list132, delegate(TextObject text)
			{
				text.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
				return true;
			}, null);
			string text133 = "{=bGaD3LoG}The best blows are struck not to the body but to the spirit. Let the defenders know you found it by diverting it to a pool where your men can bathe on the hottest, thirstiest days of the siege.";
			List<IncidentEffect> list133 = new List<IncidentEffect>();
			list133.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list133.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list133.Add(IncidentEffect.SiegeProgressChange(() => 0.1f));
			incident91.AddOption(text133, list133, null, null);
			incident91.AddOption("{=iiwhVNBb}You respect the defenders' cunning. Simply observe how they slip out of the fortress, by ropes or hidden gates, and learn from it.", new List<IncidentEffect> { IncidentEffect.SkillChange(DefaultSkills.Engineering, 100f) }, null, null);
			Incident incident92 = this.RegisterIncident("incident_mining", "{=O4iUPSXs}Mining", "{=0DSopB7F}After observing the terrain around {FORTRESS_NAME}, you think conditions might be appropriate for mining - digging a tunnel under a wall, then collapsing it. This can open a breach but is time-consuming and risky. If you encounter rock underground, you may find all your efforts go to waste. If the ground is too soft, the tunnel may collapse and bury your men. Most troops are reluctant to spend time in dark, confined tunnels unless they are paid extra, or forced.", IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege, IncidentsCampaignBehaviour.IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.BesiegedSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
				return MobileParty.MainParty.BesiegedSettlement != null && Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 30;
			});
			Incident incident93 = incident92;
			string text134 = "{=SR6inO3d}Double the pay of any troops willing to dig the mine, and tunnel carefully.";
			list4 = new List<IncidentEffect>();
			list4.Add(IncidentEffect.GoldChange(() => -500));
			List<IncidentEffect> list134 = list4;
			IncidentEffect effectOne2 = IncidentEffect.Group(new IncidentEffect[]
			{
				IncidentEffect.BreachSiegeWall(1),
				IncidentEffect.MoraleChange(5f),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.RenownChange(5f),
				IncidentEffect.SkillChange(DefaultSkills.Engineering, 50f)
			});
			IncidentEffect[] array6 = new IncidentEffect[2];
			array6[0] = IncidentEffect.TraitChange(DefaultTraits.Valor, -100);
			array6[1] = IncidentEffect.SiegeProgressChange(() => -0.2f);
			list134.Add(IncidentEffect.Select(effectOne2, IncidentEffect.Group(array6), 0.6f));
			incident93.AddOption(text134, list4, null, null);
			Incident incident94 = incident92;
			string text135 = "{=kntd1zYo}Tunnel quickly, threatening to execute anyone who refuses for insubordination";
			list4 = new List<IncidentEffect>();
			List<IncidentEffect> list135 = list4;
			IncidentEffect[] array7 = new IncidentEffect[4];
			array7[0] = IncidentEffect.SiegeProgressChange(() => 0.2f);
			array7[1] = IncidentEffect.TraitChange(DefaultTraits.Generosity, -100);
			array7[2] = IncidentEffect.RenownChange(5f);
			array7[3] = IncidentEffect.SkillChange(DefaultSkills.Engineering, 50f);
			IncidentEffect effectOne3 = IncidentEffect.Group(array7);
			IncidentEffect[] array8 = new IncidentEffect[3];
			array8[0] = IncidentEffect.TraitChange(DefaultTraits.Calculating, -100);
			array8[1] = IncidentEffect.SiegeProgressChange(() => -0.1f);
			array8[2] = IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 10);
			list135.Add(IncidentEffect.Select(effectOne3, IncidentEffect.Group(array8), 0.6f));
			incident94.AddOption(text135, list4, null, null);
			incident92.AddOption("{=lCumAw9j}Rely on the usual methods", new List<IncidentEffect>(), null, null);
			Incident incident95 = this.RegisterIncident("incident_at_the_breach", "{=OaCU1zfR}At the Breach", "{=rpvUQUmq}You've been studying the walls of {FORTRESS_NAME} for days, and you see what appears to be a crack in one place - possibly the result of an old earthquake, or just negligence. It's hard to hit with your catapults and the ground is too rocky to mine, but you reckon troops with pickaxes by themselves could bring it down - assuming they were willing to brave the storm of arrows and rocks that would no doubt come pouring from the battlements above.", IncidentsCampaignBehaviour.IncidentTrigger.DuringSiege, IncidentsCampaignBehaviour.IncidentType.Siege, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.BesiegedSettlement == null)
				{
					return false;
				}
				description.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.BesiegedSettlement.Name);
				return MobileParty.MainParty.BesiegedSettlement != null && Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 20;
			});
			string text136 = "{=PJYgbG2h}\"A purse of silver for every stone brought to me from that wall!\"";
			List<IncidentEffect> list136 = new List<IncidentEffect>();
			list136.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list136.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list136.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => true, () => 10));
			list136.Add(IncidentEffect.GoldChange(() => -500));
			list136.Add(IncidentEffect.SiegeProgressChange(() => 0.2f).WithChance(0.8f));
			incident95.AddOption(text136, list136, null, null);
			string text137 = "{=ffJrKMFm}Order your recruits to swarm the base of the wall and bring it down. They can be replaced.";
			List<IncidentEffect> list137 = new List<IncidentEffect>();
			list137.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list137.Add(IncidentEffect.KillTroopsRandomlyOrderedByTier((TroopRosterElement x) => true, () => 10));
			list137.Add(IncidentEffect.BreachSiegeWall(1));
			incident95.AddOption(text137, list137, null, null);
			incident95.AddOption("{=wJ3Nh230}A breach is not worth the losses.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, -100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100)
			}, null, null);
			Incident incident96 = this.RegisterIncident("incident_failed_inspection", "{=KBh8fb89}Failed Inspection", "{=jUBTrdbE}You take a quick look over your men before you ride out, and you are shocked to notice the state of their equipment. In particular, several of the men have frayed bridles and shield straps that look like they might snap in battle. They are supposed to oil and replace their leather at their own expense, and clearly have not been doing so.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingSettlement, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.MemberRoster.TotalRegulars >= 40);
			incident96.AddOption("{=fyJ6aDOQ}Burst into a righteous fury and hand out extra punishments, so that no one will ever again dare neglect their equipment", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -200),
				IncidentEffect.PartyExperienceChance(100)
			}, null, null);
			incident96.AddOption("{=PZTyrsag}Sternly lecture them the risks of a strap snapping in battle. Have them do all their preparations for march over again, from the beginning.", new List<IncidentEffect>
			{
				IncidentEffect.PartyExperienceChance(20),
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 50)
			}, null, null);
			string text138 = "{=fQscCIxs}Buy them the best equipment you can from the market to replace their wares.";
			List<IncidentEffect> list138 = new List<IncidentEffect>();
			list138.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list138.Add(IncidentEffect.GoldChange(() => -200));
			incident96.AddOption(text138, list138, null, null);
			string text139 = "{=SLAFtiET}Pretend you saw nothing.";
			List<IncidentEffect> list139 = new List<IncidentEffect>();
			list139.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list139.Add(IncidentEffect.DemoteTroopsRandomlyWithPredicate((TroopRosterElement x) => !x.Character.IsHero, (CharacterObject x) => true, 2, false));
			incident96.AddOption(text139, list139, null, null);
			Incident incident97 = this.RegisterIncident("incident_the_quiet_life", "{=3M54uDKZ}The Quiet Life", "{=IaOfWrNF}As you pass through your lands, you see several of your {TROOP_NAME} casting envious eyes over the farmers in the fields. They are tired of wandering from place to place, they say, and hope to put down roots somewhere and start a family. Their contract of enlistment is not yet up but it occurs to you that if they settled in your estates, it would be a boon to your local militia.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown | IncidentsCampaignBehaviour.IncidentTrigger.EnteringCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.CurrentSettlement == null)
				{
					return false;
				}
				CharacterObject characterObject = this.<InitializeIncidents>g__GetTheQuietLifeTroop|25_465();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_NAME", characterObject.Name);
				return MobileParty.MainParty.CurrentSettlement.IsFortification && Hero.MainHero.Clan == MobileParty.MainParty.CurrentSettlement.OwnerClan && MobileParty.MainParty.MemberRoster.TotalRegulars >= 60;
			});
			string text140 = "{=SFUrdvHo}Grant them their wish, and encourage them to purchase land near {FORTRESS_NAME}.";
			List<IncidentEffect> list140 = new List<IncidentEffect>();
			list140.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list140.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetTheQuietLifeTroop|25_465), -3));
			list140.Add(IncidentEffect.SettlementMilitiaChange(() => MobileParty.MainParty.CurrentSettlement, 10));
			incident97.AddOption(text140, list140, delegate(TextObject text)
			{
				text.SetTextVariable("FORTRESS_NAME", MobileParty.MainParty.CurrentSettlement.Name);
				return true;
			}, null);
			string text141 = "{=3WFuf7OC}Give them money to purchase land near your fief in {VILLAGE_NAME}, so long as they take charge of training the local militia.";
			List<IncidentEffect> list141 = new List<IncidentEffect>();
			list141.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list141.Add(IncidentEffect.GoldChange(() => -300));
			list141.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetTheQuietLifeTroop|25_465), -3));
			list141.Add(IncidentEffect.SettlementMilitiaChange(() => MobileParty.MainParty.CurrentSettlement, 15));
			incident97.AddOption(text141, list141, delegate(TextObject text)
			{
				Village randomElement = MobileParty.MainParty.CurrentSettlement.Town.Villages.GetRandomElement<Village>();
				text.SetTextVariable("VILLAGE_NAME", randomElement.Name);
				return true;
			}, null);
			string text142 = "{=YIsU82Jd}Tell them they can go, but only if they forfeit back pay as compensation to you for losing them.";
			List<IncidentEffect> list142 = new List<IncidentEffect>();
			list142.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list142.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetTheQuietLifeTroop|25_465), -3));
			list142.Add(IncidentEffect.GoldChange(() => 300));
			incident97.AddOption(text142, list142, null, null);
			incident97.AddOption("{=KUb0DRxw}Refuse them permission.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			Incident incident98 = this.RegisterIncident("incident_job_offer", "{=xEktDm2z}Job Offer", "{=guEjfYhO}One of your {TROOP_NAME} had some drinks with the arena master in {TOWN} and left the tavern with a job offer - work as a trainer and occasional combatant. The signing bonus is generous. He is reluctant to leave you, and you'd be reluctant to lose a valuable soldier, but he'd be in a fine position to sing your praises.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				CharacterObject characterObject = this.<InitializeIncidents>g__GetJobOfferTroop|25_474();
				if (characterObject == null)
				{
					return false;
				}
				description.SetTextVariable("TROOP_NAME", characterObject.Name);
				description.SetTextVariable("TOWN", MobileParty.MainParty.LastVisitedSettlement.Name);
				return MobileParty.MainParty.MemberRoster.TotalRegulars >= 30 && Hero.MainHero.Clan.Renown >= 200f;
			});
			incident98.AddOption("{=0awbcZag}Give him your blessing and encourage him to bore everyone to tears with his old war stories.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.RenownChange(10f),
				IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetJobOfferTroop|25_474), -1)
			}, null, null);
			incident98.AddOption("{=Qb3axqdZ}Persuade him to stay. The arena is fine for practice but true glory is not won with blunted weapons.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100)
			}, null, null);
			string text143 = "{=klVc5afL}Let him go if he splits his signing bonus with you.";
			List<IncidentEffect> list143 = new List<IncidentEffect>();
			list143.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list143.Add(IncidentEffect.RenownChange(5f));
			list143.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(this.<InitializeIncidents>g__GetJobOfferTroop|25_474), -1));
			list143.Add(IncidentEffect.GoldChange(() => 200));
			incident98.AddOption(text143, list143, null, null);
			Incident incident99 = this.RegisterIncident("incident_secession_of_the_plebs", "{=VvwnbRx4}Secession of the Plebs", "{=GgYzaakh}Trade in {TOWN_NAME} has been booming, and prices and wages have been going up. The workers at your {WORKSHOP} have taken notice. Your superviser reports that they have all refused to come in to work until they receive a pay increase. He notes that there remain plenty of hungry mouths and willing hands in town, but that the current workers might cause trouble if they are replaced.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				Workshop workshop = this.<InitializeIncidents>g__GetSecessionOfThePlebsWorkshop|25_478();
				if (workshop == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN_NAME", workshop.Settlement.Name);
				description.SetTextVariable("WORKSHOP", workshop.Name);
				return true;
			});
			incident99.AddOption("{=5krpAZu6}Chide them gently for walking off the job before coming to you first, because of course you'll up their wages.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 200),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetSecessionOfThePlebsWorkshop|25_478), -0.1f)
			}, null, null);
			incident99.AddOption("{=zM2vbhrt}Sit down with them and negotiate a mutual understanding.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetSecessionOfThePlebsWorkshop|25_478), -0.05f)
			}, null, null);
			string text144 = "{=aTU2CgIU}Fire the lot of them, hire different workers, and post some troops outside to protect your shop.";
			List<IncidentEffect> list144 = new List<IncidentEffect>();
			list144.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list144.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list144.Add(IncidentEffect.KillTroopsRandomly((TroopRosterElement x) => !x.Character.IsHero, () => 2));
			incident99.AddOption(text144, list144, null, null);
			Incident incident100 = this.RegisterIncident("incident_occupational_safety", "{=l8rpMldr}Occupational Safety", "{=Eih6lrcN}Business is good at your {WORKSHOP} in {TOWN}, but in the rush to keep up with demand the normal precautions are not being taken. {INJURY_TEXT} He will live, but is unlikely to be able to do skilled work, and the rest of the workers are worried about future accidents.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.CurrentSettlement.IsTown)
				{
					return false;
				}
				WorkshopType workshopType = WorkshopType.Find("brewery");
				WorkshopType workshopType2 = WorkshopType.Find("smithy");
				Workshop workshop = this.<InitializeIncidents>g__GetOccupationalSafetyWorkshop|25_485();
				if (workshop == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN", workshop.Settlement.Name);
				description.SetTextVariable("WORKSHOP", workshop.Name);
				string variable = string.Empty;
				if (workshop.WorkshopType == workshopType)
				{
					variable = "{=JcoSd4oi}One man burned his hand in the oven.";
				}
				else if (workshop.WorkshopType == workshopType2)
				{
					variable = "{=lVnw8K2N}One man crushed his hand in the forge.";
				}
				description.SetTextVariable("INJURY_TEXT", variable);
				return true;
			});
			string text145 = "{=JdlbkM8C}Pay him a pension and order your supervisors to take more care, even if it slows down production.";
			List<IncidentEffect> list145 = new List<IncidentEffect>();
			list145.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list145.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, 100));
			list145.Add(IncidentEffect.GoldChange(() => -100));
			list145.Add(IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetOccupationalSafetyWorkshop|25_485), -0.1f));
			incident100.AddOption(text145, list145, null, null);
			string text146 = "{=OlvD0u1O}You're not going to pay for another man's clumsiness. Let work recommence at the same pace.";
			List<IncidentEffect> list146 = new List<IncidentEffect>();
			list146.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list146.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.CurrentSettlement.Town, -10));
			incident100.AddOption(text146, list146, null, null);
			incident100.AddOption("{=k4aNfNHg}As on the battlefield, so on the workshop floor. Let each team work at its own pace, receiving bonuses for output, but also taking responsibiliy for injuries.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -200),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetOccupationalSafetyWorkshop|25_485), 0.05f)
			}, null, null);
			Incident incident101 = this.RegisterIncident("incident_hand_in_the_strongbox", "{=OFuTQ6t8}Hand in the Strongbox", "{=82Fno8Vr}Your clerk at your {WORKSHOP} in {TOWN} has caught one of your foremen skimming from the funds you use to purchase raw materials. Luckily, he did not take much, and he readily confessed. How do you prevent this from happening in the future?", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				Workshop workshop = this.<InitializeIncidents>g__GetHandInTheStrongboxWorkshop|25_490();
				if (workshop == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN", workshop.Settlement.Name);
				description.SetTextVariable("WORKSHOP", workshop.Name);
				return true;
			});
			incident101.AddOption("{=47hUs9yg}You make an example of the foreman. You hand him over to the town authorities and encourage them to apply the full penalty - branding as a thief and exile from the town.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, -100),
				IncidentEffect.TraitChange(DefaultTraits.Honor, 100),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetHandInTheStrongboxWorkshop|25_490), -0.1f).WithChance(0.3f)
			}, null, null);
			incident101.AddOption("{=n2mOvIPt}You appeal to your men's better natures. You plea for leniency for your foreman, and beg them not to take advantage.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Mercy, 100),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetHandInTheStrongboxWorkshop|25_490), -0.05f)
			}, null, null);
			incident101.AddOption("{=cyZG4qHm}You institute a complex scheme of bookkeeping, even though it will make purchases more onerous and slow down production.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetHandInTheStrongboxWorkshop|25_490), -0.05f)
			}, null, null);
			incident101.AddOption("{=G8eaX10B}You treat all your workers as potential thieves, having them searched coming and going from the workshop.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			Incident incident102 = this.RegisterIncident("incident_jobs_for_the_lads", "{=0avcvATJ}Jobs for the Lads", "{=cJdoDJ4M}One prominent resident of {TOWN}, {ARTISAN_NAME}, approaches you. Your {WORKSHOP} has been hiring workers who have recently migrated in from the countryside. He requests that you fire them and take on some local boys from the back alleys. If you refuse, he won't be responsible if the carts carrying your shipments get swarmed by traffic going through tight gates, slowing down production.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.Workshop, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.CurrentSettlement.IsTown)
				{
					return false;
				}
				Workshop workshop = this.<InitializeIncidents>g__GetJobsForTheLadsWorkshop|25_495();
				if (workshop == null)
				{
					return false;
				}
				Hero hero = this.<InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496();
				if (hero == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN", workshop.Settlement.Name);
				description.SetTextVariable("WORKSHOP", workshop.Name);
				description.SetTextVariable("ARTISAN_NAME", hero.Name);
				return true;
			});
			incident102.AddOption("{=bdUzdzIe}Sadly bid farewell to the migrants and wish them well in future endeavors.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.HeroRelationChange(new Func<Hero>(this.<InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496), 10)
			}, null, null);
			incident102.AddOption("{=qoSObD62}Tell {ARTISAN_NAME} to do his worst - you're not firing men who've served you well.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.WorkshopProfitabilityChange(new Func<Workshop>(this.<InitializeIncidents>g__GetJobsForTheLadsWorkshop|25_495), -0.05f)
			}, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496();
				text.SetTextVariable("ARTISAN_NAME", hero.Name);
				return true;
			}, null);
			string text147 = "{=TbF5Kc4x}Ask {ARTISAN_NAME} if you can make a sizeable contribution to his guild's feast and funeral fund.";
			List<IncidentEffect> list147 = new List<IncidentEffect>();
			list147.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list147.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list147.Add(IncidentEffect.GoldChange(() => -200));
			list147.Add(IncidentEffect.HeroRelationChange(new Func<Hero>(this.<InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496), 10));
			incident102.AddOption(text147, list147, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496();
				text.SetTextVariable("ARTISAN_NAME", hero.Name);
				return true;
			}, null);
			Incident incident103 = this.RegisterIncident("incident_respect_for_the_slain", "{=EilA5dlq}Respect for the Slain", "{=pNVKWAKo}Throughout your visit to {VILLAGE}, one of your older {TROOP_TYPE} seems moody and downcast. As you march out, he suddenly stops, stares at a roadside stall, then breaks ranks from your party and starts shouting at the vendor. The story quickly comes tumbling out. He served in one of the Empire's old legions in a battle here against the {NEAREST_OTHER_CULTURE}, and the vendor is selling scraps of metal that were clearly looted from the graves of the dead.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				if (MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__HighTierTroopsPredicate|25_503)).ToList<TroopRosterElement>()
					.Sum((TroopRosterElement x) => x.Number) < 10)
				{
					return false;
				}
				CharacterObject characterObject = this.<InitializeIncidents>g__GetRespectForTheSlainSelectedTroop|25_504();
				if (characterObject == null)
				{
					return false;
				}
				CultureObject cultureObject = IncidentsCampaignBehaviour.<InitializeIncidents>g__GetRespectForTheSlainNearestOtherCulture|25_505();
				if (cultureObject == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE", MobileParty.MainParty.LastVisitedSettlement.Name);
				description.SetTextVariable("TROOP_TYPE", characterObject.Name);
				description.SetTextVariable("NEAREST_OTHER_CULTURE", cultureObject.Name);
				return true;
			});
			string text148 = "{=N2Sb9nS6}Purchase what old helmets and shield bosses you can and allow your man to build a small memorial on the battlefield, even though you know it will soon be looted afresh.";
			List<IncidentEffect> list148 = new List<IncidentEffect>();
			list148.Add(IncidentEffect.GoldChange(() => -200));
			list148.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list148.Add(IncidentEffect.DisorganizeParty());
			incident103.AddOption(text148, list148, null, null);
			incident103.AddOption("{=fqBRonK9}Do you not also not loot whatever you can from the dead at your victories? Tell your man to save his anger for wrongs done to the living.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, -100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			string text149 = "{=WGmKib15}Erupt in fury at the vendor and accuse him of grave desecration. It's unlikely that the thefts will stop, but your men will be pleased.";
			List<IncidentEffect> list149 = new List<IncidentEffect>();
			list149.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list149.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 50));
			list149.Add(IncidentEffect.RenownChange(2f));
			list149.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -5));
			incident103.AddOption(text149, list149, null, null);
			Incident incident104 = this.RegisterIncident("incident_just_rewards", "{=YwstWeDQ}Just Rewards", "{=Jo5OjwAU}Your men fought truly heroically in the last battle. As they help their wounded comrades back to camp and gather the slain, their weary faces sometimes look to you with expectation. If ever there was a time to give them some extra recognition of their valor and sacrifice, this would be it.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingBattle, IncidentsCampaignBehaviour.IncidentType.PartyCampLife, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				PlayerBattleEndedLogEntry playerBattleEndedLogEntry = Campaign.Current.LogEntryHistory.FindLastGameActionLog<PlayerBattleEndedLogEntry>((PlayerBattleEndedLogEntry x) => true);
				return playerBattleEndedLogEntry != null && playerBattleEndedLogEntry.IsAgainstGreatOdds && Hero.MainHero.Gold >= 2000 && PartyBase.MainParty.MemberRoster.TotalRegulars >= 5;
			});
			string text150 = "{=Wenvx1OE}Reach into your treasury and grab great fistfuls of silver denars, hurling it to your men and praising them by name for their deeds.";
			List<IncidentEffect> list150 = new List<IncidentEffect>();
			list150.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -100));
			list150.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 200));
			list150.Add(IncidentEffect.GoldChange(() => -(int)((float)Hero.MainHero.Gold * 0.5f)));
			list150.Add(IncidentEffect.RenownChange(5f));
			incident104.AddOption(text150, list150, null, null);
			string text151 = "{=w3a39PVT}Give them an extra week's pay as a bonus, telling them they deserve far more but it is all you can afford.";
			List<IncidentEffect> list151 = new List<IncidentEffect>();
			list151.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			list151.Add(IncidentEffect.GoldChange(() => MobileParty.MainParty.TotalWage * -7));
			incident104.AddOption(text151, list151, null, null);
			incident104.AddOption("{=WnlD7awi}Gruffly tell them that you were pleased they did their duty.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			Incident incident105 = this.RegisterIncident("incident_letter_of_reference", "{=q0HIXEal}Letter of Reference", "{=D7ZcX8XR}As you pass through the market, {NOTABLE.NAME} requests a minute of your time. {?NOTABLE.GENDER}Her{?}His{\\?} cousin is planning on doing some business in {TOWN}, and {?NOTABLE.GENDER}she{?}he{\\?} would like a letter of reference from you. You know next to nothing about the lad, although {NOTABLE.NAME} swears that he is honest and conscientious. You find you are receiving a number of these types of requests as your fame spreads.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown, IncidentsCampaignBehaviour.IncidentType.Profit, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				Hero hero = this.<InitializeIncidents>g__GetLetterOfReferenceNotable|25_516();
				if (hero == null || Hero.MainHero.Clan.Renown < 200f)
				{
					return false;
				}
				description.SetCharacterProperties("NOTABLE", hero.CharacterObject, false);
				description.SetTextVariable("TOWN", hero.CurrentSettlement.Name);
				return true;
			});
			incident105.AddOption("{=z15Ux7sh}Write truthfully that you don't know much about the bearer of the letter, but he comes from good family.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Honor, 100) }, null, null);
			incident105.AddOption("{=aZwaFhA6}Write effusively about the lad's qualities.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.TraitChange(DefaultTraits.Honor, -100),
				IncidentEffect.HeroRelationChange(new Func<Hero>(this.<InitializeIncidents>g__GetLetterOfReferenceNotable|25_516), 20)
			}, null, null);
			string text152 = "{=8rEX8rjv}Tell {NOTABLE.NAME} that you will write whatever {?NOTABLE.GENDER}she{?}he{\\?} wants, for the proper price.";
			List<IncidentEffect> list152 = new List<IncidentEffect>();
			list152.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list152.Add(IncidentEffect.GoldChange(() => 300));
			incident105.AddOption(text152, list152, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetLetterOfReferenceNotable|25_516();
				text.SetCharacterProperties("NOTABLE", hero.CharacterObject, false);
				return true;
			}, null);
			incident105.AddOption("{=DOPdMPla}Upbraid {NOTABLE.NAME} in the street for all to hear for daring to presume that you would recommend anyone that you do not know.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Honor, 200),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100),
				IncidentEffect.HeroRelationChange(new Func<Hero>(this.<InitializeIncidents>g__GetLetterOfReferenceNotable|25_516), -10)
			}, delegate(TextObject text)
			{
				Hero hero = this.<InitializeIncidents>g__GetLetterOfReferenceNotable|25_516();
				text.SetCharacterProperties("NOTABLE", hero.CharacterObject, false);
				return true;
			}, null);
			Incident incident106 = this.RegisterIncident("incident_tiller_and_wanderer", "{=OqlihYNW}The Tiller and the Wanderer", "{=nuqL3OAF}As you are leaving your fief you stumble across an altercation. A group of nomads, who pass by annually on their migration from summer to winter pastures, have allowed their herds to graze in your tenants' crops. Your troops far outnumber the nomads and your tenants are urging you to teach them a lesson, but of course their kin might retaliate at a later date.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingTown | IncidentsCampaignBehaviour.IncidentTrigger.LeavingCastle, IncidentsCampaignBehaviour.IncidentType.FiefManagement, CampaignTime.Years(1000f), (TextObject description) => MobileParty.MainParty.LastVisitedSettlement != null && (MobileParty.MainParty.LastVisitedSettlement.IsFortification && MobileParty.MainParty.LastVisitedSettlement.OwnerClan == Hero.MainHero.Clan) && (MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "aserai" || MobileParty.MainParty.LastVisitedSettlement.Culture.StringId == "khuzait"));
			string text153 = "{=fI8Bd468}Seize the nomads' herds to compensate you and your tenants for their crops.";
			List<IncidentEffect> list153 = new List<IncidentEffect>();
			list153.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetTillerAndWandererHorseItem|25_522), () => 3));
			list153.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list153.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list153.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, 10));
			list153.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 20));
			list153.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10));
			list153.Add(IncidentEffect.Custom(null, delegate
			{
				Clan clan = this.<InitializeIncidents>g__GetTillerAndWandererBanditPartyClan|25_523();
				for (int i = 0; i < 3; i++)
				{
					Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(MobileParty.MainParty.LastVisitedSettlement, MobileParty.NavigationType.Default, null);
					CampaignVec2 initialPosition = NavigationHelper.FindPointAroundPosition(MobileParty.MainParty.LastVisitedSettlement.GatePosition, MobileParty.NavigationType.Default, 5f, 0f, true, false);
					MobileParty mobileParty = BanditPartyComponent.CreateBanditParty("incident_tiller_and_wanderer_bandit_revenge_" + i, clan, hideout, false, null, initialPosition);
					mobileParty.MemberRoster.AddToCounts(clan.Culture.BanditBandit, MobileParty.MainParty.RandomIntWithSeed((uint)this._activeIncidentSeed, 5, 17), false, 0, 0, true, -1);
					mobileParty.InitializePartyTrade(200);
					mobileParty.SetCustomHomeSettlement(hideout.Settlement);
					mobileParty.Party.SetVisualAsDirty();
				}
				return new List<TextObject>
				{
					new TextObject("{=qxE0YAmo}3 bandit parties spawned nearby.", null)
				};
			}, (IncidentEffect effect) => new List<TextObject>
			{
				new TextObject("{=ayfKQixq}3 bandit parties spawn nearby", null)
			}));
			incident106.AddOption(text153, list153, null, null);
			string text154 = "{=ciQPfxEC}Demand that the nomads pay fair compensation for damaged crops, even though you know this will not settle the problem for long.";
			List<IncidentEffect> list154 = new List<IncidentEffect>();
			list154.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list154.Add(IncidentEffect.TownSecurityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -5));
			list154.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, 10));
			incident106.AddOption(text154, list154, null, null);
			string text155 = "{=fkQj3TIP}Tell the nomads that proud warriors such as themselves need not worry what these dirt-diggers think. Share a meal with them, and see if some will join you.";
			List<IncidentEffect> list155 = new List<IncidentEffect>();
			list155.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list155.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list155.Add(IncidentEffect.ChangeTroopAmount(new Func<CharacterObject>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__GetTillerAndWandererTroop|25_524), 4));
			list155.Add(IncidentEffect.SettlementRelationChange(() => MobileParty.MainParty.LastVisitedSettlement, -10));
			list155.Add(IncidentEffect.TownProsperityChange(() => MobileParty.MainParty.LastVisitedSettlement.Town, -10));
			incident106.AddOption(text155, list155, null, null);
			Incident incident107 = this.RegisterIncident("incident_cries_in_the_mist", "{=ELXomm1C}Cries in the Mist", "{=SAXnFQLq}The people of {VILLAGE_NAME} warned you to take care outside their village. When the mist gathers, The woods will be haunted by spirits whose shriek foretells death. Now the gloom is gathering, and you hear mutterings from your men. You are certain that they will soon be hearing wailing and seeing apparitions, real or imagined. The horses are becoming twitchy too, picking up on the mood of the men, and accidents are likely to happen.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.DreamsSongsAndSigns, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage)
				{
					return false;
				}
				Village village = MobileParty.MainParty.LastVisitedSettlement.Village;
				bool flag = false;
				for (float num = -3f; num <= 3f; num += 0.25f)
				{
					for (float num2 = -3f; num2 <= 3f; num2 += 0.25f)
					{
						if (num * num + num2 * num2 <= 9f)
						{
							CampaignVec2 campaignVec = new CampaignVec2(village.Settlement.Position.ToVec2() + new Vec2(num, num2), true);
							if (campaignVec.Face.IsValid() && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(campaignVec.Face) == TerrainType.Forest)
							{
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE_NAME", village.Name);
				return true;
			});
			string text156 = "{=uD05twgi}Tell your men to ignore the superstitions of villagers, and press on.";
			List<IncidentEffect> list156 = new List<IncidentEffect>();
			list156.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, 100));
			list156.Add(IncidentEffect.TraitChange(DefaultTraits.Valor, 100));
			list156.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, -100));
			list156.Add(IncidentEffect.MoraleChange(-20f));
			list156.Add(IncidentEffect.WoundTroopsRandomly((TroopRosterElement x) => x.Character.IsMounted, () => 2, true).WithChance(0.5f));
			list156.Add(IncidentEffect.DemoteTroopsRandomlyWithPredicate((TroopRosterElement x) => x.Character.IsMounted, (CharacterObject x) => !x.IsMounted, 1, true).WithChance(0.5f));
			incident107.AddOption(text156, list156, null, null);
			incident107.AddOption("{=BxpJKMYq}Dispel the spirits in the approved orthodox Calradian way, by loudly calling on Heaven to ward off the ghosts of Below.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Valor, 100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			incident107.AddOption("{=4Ef7xG1o}Propitiate the spirits as the villagers advised, by slaughtering a black cockerel that they sold one of your men.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, -100)
			}, null, null);
			Incident incident108 = this.RegisterIncident("incident_feeling_the_bite", "{=0yKJ0J0N}Feeling the Bite", "{=D9T64ejr}As you leave {VILLAGE_NAME}, several of the villagers approach you. They warn you that stagnant ponds from the last rain have yet to fully dry and have bred mosquitos. They offer to sell you a concoction of oils including rosemary, catmint and other herbs that they say will ward off all flying vermin.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.HardTravel, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null)
				{
					return false;
				}
				CampaignTime.Seasons getSeasonOfYear = CampaignTime.Now.GetSeasonOfYear;
				bool result = getSeasonOfYear == CampaignTime.Seasons.Spring || getSeasonOfYear == CampaignTime.Seasons.Summer;
				description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				return result;
			});
			string text157 = "{=zGVNqFnh}Buy vials of ointment for every member of your party, at the risk of looking gullible.";
			List<IncidentEffect> list157 = new List<IncidentEffect>();
			list157.Add(IncidentEffect.GoldChange(() => -100));
			list157.Add(IncidentEffect.TraitChange(DefaultTraits.Calculating, -50));
			list157.Add(IncidentEffect.TraitChange(DefaultTraits.Generosity, 100));
			incident108.AddOption(text157, list157, null, null);
			incident108.AddOption("{=qCoqaa70}Have your men wrap themselves up as best they can and press on.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -50) }, null, null);
			incident108.AddOption("{=VccCV5dX}Take circuitous routs to avoid the ponds.", new List<IncidentEffect>
			{
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 100)
			}, null, null);
			Incident incident109 = this.RegisterIncident("incident_nearly_a_gift_horse", "{=N19m9qmd}Nearly a Gift Horse", "{=WR7NCetE}As you leave {VILLAGE_NAME}, a scraggly young man waves to attract your attention. He leads a magnificent {RARE_HORSE}, and says that he will sell it to you for a mere 500 denars. He claims he found it wandering on a heath, almost dead from exhaustion and neglect, and he nursed it back to health. He won't go into town because he says he would be unfairly accused of theft.", IncidentsCampaignBehaviour.IncidentTrigger.LeavingVillage, IncidentsCampaignBehaviour.IncidentType.TroopSettlementRelation, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (MobileParty.MainParty.LastVisitedSettlement == null || !MobileParty.MainParty.LastVisitedSettlement.IsVillage)
				{
					return false;
				}
				ItemObject itemObject = this.<InitializeIncidents>g__GetNearlyAGiftHorseHorse|25_546();
				if (itemObject == null)
				{
					return false;
				}
				description.SetTextVariable("VILLAGE_NAME", MobileParty.MainParty.LastVisitedSettlement.Name);
				description.SetTextVariable("RARE_HORSE", itemObject.Name);
				description.SetTextVariable("TOWN_NAME", MobileParty.MainParty.LastVisitedSettlement.Village.Bound.Name);
				return true;
			});
			string text158 = "{=YVETzHXK}Plausible enough, and you have no time to check the provenance of every bargain you find. Buy the horse!";
			List<IncidentEffect> list158 = new List<IncidentEffect>();
			list158.Add(IncidentEffect.GoldChange(() => -500));
			list158.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -50));
			list158.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetNearlyAGiftHorseHorse|25_546), () => 1));
			incident109.AddOption(text158, list158, null, null);
			string text159 = "{=B9HD9PNw}A likely story... Place the man under arrest and send word to the authorities of {TOWN_NAME}, who have the time to sort things out.";
			List<IncidentEffect> list159 = new List<IncidentEffect>();
			list159.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, 100));
			list159.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -50));
			list159.Add(IncidentEffect.DisorganizeParty());
			incident109.AddOption(text159, list159, delegate(TextObject text)
			{
				Settlement lastVisitedSettlement = MobileParty.MainParty.LastVisitedSettlement;
				text.SetTextVariable("TOWN_NAME", lastVisitedSettlement.Village.Bound.Name);
				return true;
			}, null);
			incident109.AddOption("{=CyM5bAdB}You're not sure you trust the seller, but you're not handing over to officials who might just hang him and seize the horse themselves. Go on your way.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Mercy, 100) }, null, null);
			string text160 = "{=AwBJzppu}Take the horse without paying. Tell the seller that thieves have no right to complain of theft.";
			List<IncidentEffect> list160 = new List<IncidentEffect>();
			list160.Add(IncidentEffect.TraitChange(DefaultTraits.Mercy, -100));
			list160.Add(IncidentEffect.TraitChange(DefaultTraits.Honor, -100));
			list160.Add(IncidentEffect.ChangeItemAmount(new Func<ItemObject>(this.<InitializeIncidents>g__GetNearlyAGiftHorseHorse|25_546), () => 1));
			incident109.AddOption(text160, list160, null, null);
			Incident incident110 = this.RegisterIncident("incident_hammer_of_the_sun", "{=R80p9Ywt}Hammer of the Sun", "{=th5MgyAZ}As you enter {TOWN_NAME}, one of your {ASERAI_TROOPS} approaches you. Many of his comrades are not used to the heat of the desert, he says. They march after drinking wine through the evening but are not careful to properly take water throughout the day, as the sun beats down on them. He has seen men collapse and die from such carelessness and ignorance.", IncidentsCampaignBehaviour.IncidentTrigger.EnteringTown, IncidentsCampaignBehaviour.IncidentType.HardTravel, CampaignTime.Years(1000f), delegate(TextObject description)
			{
				if (!MobileParty.MainParty.Position.IsValid())
				{
					return false;
				}
				if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.Position.Face) != TerrainType.Desert || MobileParty.MainParty.MemberRoster.TotalRegulars < 50)
				{
					return false;
				}
				Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
				MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
				if (troopRoster.Count((TroopRosterElement x) => x.Character.Culture.StringId != "aserai" && !x.Character.IsHero) < 10)
				{
					return false;
				}
				CharacterObject character = IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in troopRoster
					where x.Character.Culture.StringId == "aserai" && !x.Character.IsHero
					select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
				if (character == null)
				{
					return false;
				}
				description.SetTextVariable("TOWN_NAME", currentSettlement.Name);
				description.SetTextVariable("ASERAI_TROOPS", character.Name);
				return true;
			});
			incident110.AddOption("{=lbSsRg8w}Your men will resent any curbs on their celebrations, even for their own health. Warn them but take no other precautions.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 100),
				IncidentEffect.KillTroopsRandomlyByChance(0.01f)
			}, null, null);
			incident110.AddOption("{=3Uub2e17}Tell your men that there shall be no wine tonight, and none henceforth until they learn how to handle themselves in the desert.", new List<IncidentEffect> { IncidentEffect.TraitChange(DefaultTraits.Generosity, -100) }, null, null);
			incident110.AddOption("{=cG9pkbqo}Let them men celebrate, but move slowly in the morning, pausing to make sure that each man is drinking from his water-skin.", new List<IncidentEffect>
			{
				IncidentEffect.TraitChange(DefaultTraits.Generosity, 50),
				IncidentEffect.TraitChange(DefaultTraits.Calculating, 50),
				IncidentEffect.DisorganizeParty(),
				IncidentEffect.WoundTroopsRandomlyByChance(0.01f)
			}, null, null);
		}

		// Token: 0x06003FD1 RID: 16337 RVA: 0x00129008 File Offset: 0x00127208
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetSpoiledFoodItem|25_18()
		{
			return IncidentHelper.GetSeededRandomElement<ItemRosterElement>((from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.IsFood && x.Amount >= 2
				select x).ToList<ItemRosterElement>(), this._activeIncidentSeed).EquipmentElement.Item;
		}

		// Token: 0x06003FD2 RID: 16338 RVA: 0x00129064 File Offset: 0x00127264
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetDesperateTimesFoodItem|25_22()
		{
			return IncidentHelper.GetSeededRandomElement<ItemRosterElement>((from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.IsFood && x.Amount >= 1
				select x).ToList<ItemRosterElement>(), this._activeIncidentSeed).EquipmentElement.Item;
		}

		// Token: 0x06003FD3 RID: 16339 RVA: 0x001290C0 File Offset: 0x001272C0
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetVeteranFoodItem|25_34()
		{
			return IncidentHelper.GetSeededRandomElement<ItemRosterElement>((from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.IsFood && x.Amount >= 1
				select x).ToList<ItemRosterElement>(), this._activeIncidentSeed).EquipmentElement.Item;
		}

		// Token: 0x06003FD5 RID: 16341 RVA: 0x00129144 File Offset: 0x00127344
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetFirstNobleTroop|25_44()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FD6 RID: 16342 RVA: 0x00129199 File Offset: 0x00127399
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetSecondNobleTroop|25_45()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier >= 4 && x.Character.HasMount() && !x.Character.IsHero && x.Character != this.<InitializeIncidents>g__GetFirstNobleTroop|25_44()
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FD8 RID: 16344 RVA: 0x0012920D File Offset: 0x0012740D
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetInsultCavalryman|25_54()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopsPredicate|25_52)).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FD9 RID: 16345 RVA: 0x00129248 File Offset: 0x00127448
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetInsultFootman|25_55()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__FootmenTroopsPredicate|25_53)).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FDB RID: 16347 RVA: 0x00129318 File Offset: 0x00127518
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetPurebredHorseItem|25_61()
		{
			return IncidentHelper.GetSeededRandomElement<ItemRosterElement>((from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.HasHorseComponent && x.Amount >= 1
				select x).ToList<ItemRosterElement>(), this._activeIncidentSeed).EquipmentElement.Item;
		}

		// Token: 0x06003FDC RID: 16348 RVA: 0x00129373 File Offset: 0x00127573
		[CompilerGenerated]
		internal static BesiegeSettlementLogEntry <InitializeIncidents>g__FindLastBesiegeSettlementLogEntry|25_103()
		{
			return Campaign.Current.LogEntryHistory.FindLastGameActionLog<BesiegeSettlementLogEntry>((BesiegeSettlementLogEntry x) => x.BesiegerHero == Hero.MainHero && x.Settlement == MobileParty.MainParty.LastVisitedSettlement);
		}

		// Token: 0x06003FDD RID: 16349 RVA: 0x001293A4 File Offset: 0x001275A4
		[CompilerGenerated]
		internal static SiegeAftermathLogEntry <InitializeIncidents>g__FindLastSiegeAftermathLogEntry|25_104(BesiegeSettlementLogEntry siegeLog)
		{
			return Campaign.Current.LogEntryHistory.FindLastGameActionLog<SiegeAftermathLogEntry>((SiegeAftermathLogEntry x) => x.PlayerWasInvolved && x.SiegeAftermath == SiegeAftermathAction.SiegeAftermath.ShowMercy && x.CapturedSettlement == siegeLog.Settlement);
		}

		// Token: 0x06003FDE RID: 16350 RVA: 0x001293DC File Offset: 0x001275DC
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetWantedCriminalTroop|25_129()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList<TroopRosterElement>();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(list, this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FE2 RID: 16354 RVA: 0x0012950B File Offset: 0x0012770B
		[CompilerGenerated]
		internal static CharacterObject <InitializeIncidents>g__GetSoldierInDebtTroop|25_139()
		{
			return MobileParty.MainParty.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement x) => !x.Character.IsHero).Character;
		}

		// Token: 0x06003FE3 RID: 16355 RVA: 0x00129548 File Offset: 0x00127748
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetBrokenWagonDraughtAnimalItem|25_156()
		{
			List<ItemRosterElement> list = (from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.HasHorseComponent || x.EquipmentElement.Item.StringId == "mule" || x.EquipmentElement.Item.StringId == "mule_unmountable"
				select x).ToList<ItemRosterElement>();
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)this._activeIncidentSeed, list.Count)].EquipmentElement.Item;
		}

		// Token: 0x06003FE4 RID: 16356 RVA: 0x001295B8 File Offset: 0x001277B8
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetBrokenWagonCheapestHorseItem|25_157()
		{
			List<ItemRosterElement> list = (from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.HasHorseComponent
				select x).ToList<ItemRosterElement>();
			return list[MobileParty.MainParty.RandomIntWithSeed((uint)this._activeIncidentSeed, list.Count)].EquipmentElement.Item;
		}

		// Token: 0x06003FE5 RID: 16357 RVA: 0x00129628 File Offset: 0x00127828
		[CompilerGenerated]
		private TroopRosterElement <InitializeIncidents>g__GetHonorTheSlainFoeRandomTroop|25_170()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed);
		}

		// Token: 0x06003FE8 RID: 16360 RVA: 0x00129784 File Offset: 0x00127984
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetNoMoodForMercyRandomTroop|25_190()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FEB RID: 16363 RVA: 0x0012986C File Offset: 0x00127A6C
		[CompilerGenerated]
		internal static Village <InitializeIncidents>g__GetMisplacedVengeanceLootedVillage|25_196()
		{
			Village result = null;
			float num = float.MaxValue;
			foreach (Village village in Village.All)
			{
				if (village.VillageState == Village.VillageStates.Looted && Hero.MainHero.MapFaction.IsAtWarWith(village.Settlement.MapFaction))
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty.LastVisitedSettlement.Village.Settlement, village.Settlement, false, false, MobileParty.NavigationType.All);
					if (distance <= num && distance <= 1000f)
					{
						result = village;
						num = distance;
					}
				}
			}
			return result;
		}

		// Token: 0x06003FEC RID: 16364 RVA: 0x0012992C File Offset: 0x00127B2C
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetSleepingSentryTroop|25_203()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where x.Character.Tier <= 2 && !x.Character.IsHero
				select x).ToList<TroopRosterElement>();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(list, this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FEE RID: 16366 RVA: 0x001299DF File Offset: 0x00127BDF
		[CompilerGenerated]
		private ItemRosterElement <InitializeIncidents>g__GetTradeProposalGood|25_208()
		{
			return IncidentHelper.GetSeededRandomElement<ItemRosterElement>((from x in MobileParty.MainParty.ItemRoster
				where x.EquipmentElement.Item.IsTradeGood
				select x).ToList<ItemRosterElement>(), this._activeIncidentSeed);
		}

		// Token: 0x06003FF5 RID: 16373 RVA: 0x00129D48 File Offset: 0x00127F48
		[CompilerGenerated]
		private Hero <InitializeIncidents>g__GetIntriguingRumorsRivalLord|25_228()
		{
			Kingdom kingdom = Hero.MainHero.Clan.Kingdom;
			List<Hero> list;
			if (kingdom == null)
			{
				list = null;
			}
			else
			{
				list = (from lord in kingdom.AliveLords
					where lord != Hero.MainHero && Hero.MainHero.GetRelation(lord) < 0
					select lord).ToList<Hero>();
			}
			return IncidentHelper.GetSeededRandomElement<Hero>(list, this._activeIncidentSeed);
		}

		// Token: 0x06003FFA RID: 16378 RVA: 0x00129EB4 File Offset: 0x001280B4
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetRegularTroop|25_244()
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier < 4 && x.Character.Tier > 1 && !x.Character.IsMounted && x.Number > 0 && CharacterHelper.GetTroopTree(x.Character.Culture.BasicTroop, -1f, float.MaxValue).Contains(x.Character) && this.<InitializeIncidents>g__GetNobleTroop|25_245(x.Character.Culture) != null
				select x).ToList<TroopRosterElement>();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(list, this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FFC RID: 16380 RVA: 0x00129F94 File Offset: 0x00128194
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetNobleTroop|25_245(CultureObject culture)
		{
			List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 4 && x.Character.IsMounted && x.Number > 0 && x.Character.Culture == culture && CharacterHelper.GetTroopTree(culture.EliteBasicTroop, -1f, float.MaxValue).Contains(x.Character)
				select x).ToList<TroopRosterElement>();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(list, this._activeIncidentSeed).Character;
		}

		// Token: 0x06003FFD RID: 16381 RVA: 0x00129FF0 File Offset: 0x001281F0
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetNobleTroopForUpgrade|25_246()
		{
			CharacterObject characterObject = this.<InitializeIncidents>g__GetRegularTroop|25_244();
			if (characterObject == null)
			{
				return null;
			}
			return this.<InitializeIncidents>g__GetNobleTroop|25_245(characterObject.Culture);
		}

		// Token: 0x06004001 RID: 16385 RVA: 0x0012A078 File Offset: 0x00128278
		[CompilerGenerated]
		private Village <InitializeIncidents>g__GetVillageOne|25_310()
		{
			return IncidentHelper.GetSeededRandomElement<Village>(MobileParty.MainParty.LastVisitedSettlement.Town.Villages, this._activeIncidentSeed);
		}

		// Token: 0x06004002 RID: 16386 RVA: 0x0012A09C File Offset: 0x0012829C
		[CompilerGenerated]
		private Village <InitializeIncidents>g__GetOtherVillage|25_311()
		{
			MBReadOnlyList<Village> villages = MobileParty.MainParty.LastVisitedSettlement.Town.Villages;
			Village village = this.<InitializeIncidents>g__GetVillageOne|25_310();
			Village result = null;
			float num = float.MaxValue;
			foreach (Village village2 in Village.All)
			{
				if (!villages.Contains(village2))
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(village.Settlement, village2.Settlement, false, false, MobileParty.NavigationType.Default);
					if (distance < num && village2.Settlement.OwnerClan != Clan.PlayerClan)
					{
						result = village2;
						num = distance;
					}
				}
			}
			return result;
		}

		// Token: 0x06004008 RID: 16392 RVA: 0x0012A270 File Offset: 0x00128470
		[CompilerGenerated]
		private Hero <InitializeIncidents>g__GetMarketManipulationMerchant|25_348()
		{
			return IncidentHelper.GetSeededRandomElement<Hero>((from x in MobileParty.MainParty.CurrentSettlement.Notables
				where x.Occupation == Occupation.Merchant
				select x).ToList<Hero>(), this._activeIncidentSeed);
		}

		// Token: 0x0600400B RID: 16395 RVA: 0x0012A354 File Offset: 0x00128554
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetGnosticBoysNobleTroop|25_361()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Culture.StringId == "empire" && x.Character.Tier >= 3 && x.Number >= 2
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x0600400D RID: 16397 RVA: 0x0012A414 File Offset: 0x00128614
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetSpeculativeInvestmentOtherGood|25_365()
		{
			return IncidentHelper.GetSeededRandomElement<ItemObject>(new List<ItemObject>
			{
				Game.Current.ObjectManager.GetObject<ItemObject>("wine"),
				Game.Current.ObjectManager.GetObject<ItemObject>("cotton"),
				Game.Current.ObjectManager.GetObject<ItemObject>("fur")
			}, this._activeIncidentSeed);
		}

		// Token: 0x06004010 RID: 16400 RVA: 0x0012A530 File Offset: 0x00128730
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetMadDogTroop|25_370()
		{
			List<CharacterObject> collection = CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.BasicTroop, 5f, 5f).ToList<CharacterObject>();
			List<CharacterObject> collection2 = CharacterHelper.GetTroopTree(MobileParty.MainParty.LastVisitedSettlement.Culture.EliteBasicTroop, 5f, 5f).ToList<CharacterObject>();
			List<CharacterObject> list = new List<CharacterObject>();
			list.AddRange(collection);
			list.AddRange(collection2);
			return IncidentHelper.GetSeededRandomElement<CharacterObject>(list, this._activeIncidentSeed);
		}

		// Token: 0x06004012 RID: 16402 RVA: 0x0012A634 File Offset: 0x00128834
		[CompilerGenerated]
		private TroopRosterElement <InitializeIncidents>g__GetRandomTroop|25_401()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed);
		}

		// Token: 0x06004015 RID: 16405 RVA: 0x0012A6F4 File Offset: 0x001288F4
		[CompilerGenerated]
		internal static List<Settlement> <InitializeIncidents>g__GetNearbyVillages|25_417()
		{
			return (from s in Settlement.All
				where s.IsVillage
				select s).OrderBy(delegate(Settlement s)
			{
				float num;
				return Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, s, false, MobileParty.NavigationType.All, out num);
			}).Take(3).ToList<Settlement>();
		}

		// Token: 0x06004016 RID: 16406 RVA: 0x0012A759 File Offset: 0x00128959
		[CompilerGenerated]
		private TroopRosterElement <InitializeIncidents>g__GetRandomCavalryTroop|25_434()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__CavalryTroopPredicate|25_433)).ToList<TroopRosterElement>(), this._activeIncidentSeed);
		}

		// Token: 0x06004019 RID: 16409 RVA: 0x0012A840 File Offset: 0x00128A40
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetTheQuietLifeTroop|25_465()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 4 && x.Number >= 3
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x0600401B RID: 16411 RVA: 0x0012A918 File Offset: 0x00128B18
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetJobOfferTroop|25_474()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>((from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
				where !x.Character.IsHero && x.Character.Tier >= 5
				select x).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x0600401D RID: 16413 RVA: 0x0012A9F4 File Offset: 0x00128BF4
		[CompilerGenerated]
		private Workshop <InitializeIncidents>g__GetSecessionOfThePlebsWorkshop|25_478()
		{
			List<Town> list = (from x in Town.AllTowns
				where x.Workshops.Any((Workshop y) => y.Owner == Hero.MainHero)
				select x).ToList<Town>();
			if (list.Count == 0)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<Workshop>((from w in IncidentHelper.GetSeededRandomElement<Town>(list, this._activeIncidentSeed).Workshops
				where w.Owner == Hero.MainHero
				select w).ToList<Workshop>(), this._activeIncidentSeed);
		}

		// Token: 0x0600401F RID: 16415 RVA: 0x0012AAC4 File Offset: 0x00128CC4
		[CompilerGenerated]
		private Workshop <InitializeIncidents>g__GetOccupationalSafetyWorkshop|25_485()
		{
			WorkshopType breweryWorkshopType = WorkshopType.Find("brewery");
			WorkshopType smithyWorkshopType = WorkshopType.Find("smithy");
			return IncidentHelper.GetSeededRandomElement<Workshop>((from w in MobileParty.MainParty.CurrentSettlement.Town.Workshops
				where w.Owner == Hero.MainHero && (w.WorkshopType == breweryWorkshopType || w.WorkshopType == smithyWorkshopType)
				select w).ToList<Workshop>(), this._activeIncidentSeed);
		}

		// Token: 0x06004021 RID: 16417 RVA: 0x0012ABCC File Offset: 0x00128DCC
		[CompilerGenerated]
		private Workshop <InitializeIncidents>g__GetHandInTheStrongboxWorkshop|25_490()
		{
			Town seededRandomElement = IncidentHelper.GetSeededRandomElement<Town>((from x in Town.AllTowns
				where x.Workshops.Any((Workshop y) => y.Owner == Hero.MainHero)
				select x).ToList<Town>(), this._activeIncidentSeed);
			if (seededRandomElement == null)
			{
				return null;
			}
			return IncidentHelper.GetSeededRandomElement<Workshop>((from w in seededRandomElement.Workshops
				where w.Owner == Hero.MainHero
				select w).ToList<Workshop>(), this._activeIncidentSeed);
		}

		// Token: 0x06004023 RID: 16419 RVA: 0x0012AC98 File Offset: 0x00128E98
		[CompilerGenerated]
		private Workshop <InitializeIncidents>g__GetJobsForTheLadsWorkshop|25_495()
		{
			return IncidentHelper.GetSeededRandomElement<Workshop>((from w in MobileParty.MainParty.CurrentSettlement.Town.Workshops
				where w.Owner == Hero.MainHero
				select w).ToList<Workshop>(), this._activeIncidentSeed);
		}

		// Token: 0x06004024 RID: 16420 RVA: 0x0012ACF0 File Offset: 0x00128EF0
		[CompilerGenerated]
		private Hero <InitializeIncidents>g__GetJobsForTheLadsArtisan|25_496()
		{
			return IncidentHelper.GetSeededRandomElement<Hero>((from n in MobileParty.MainParty.CurrentSettlement.Town.Settlement.Notables
				where n.GetTraitLevel(DefaultTraits.Honor) <= 0 && Hero.MainHero.GetRelation(n) < 10 && n.Occupation == Occupation.Artisan
				select n).ToList<Hero>(), this._activeIncidentSeed);
		}

		// Token: 0x06004028 RID: 16424 RVA: 0x0012AE0F File Offset: 0x0012900F
		[CompilerGenerated]
		private CharacterObject <InitializeIncidents>g__GetRespectForTheSlainSelectedTroop|25_504()
		{
			return IncidentHelper.GetSeededRandomElement<TroopRosterElement>(MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(new Func<TroopRosterElement, bool>(IncidentsCampaignBehaviour.<>c.<>9.<InitializeIncidents>g__HighTierTroopsPredicate|25_503)).ToList<TroopRosterElement>(), this._activeIncidentSeed).Character;
		}

		// Token: 0x06004029 RID: 16425 RVA: 0x0012AE4C File Offset: 0x0012904C
		[CompilerGenerated]
		internal static CultureObject <InitializeIncidents>g__GetRespectForTheSlainNearestOtherCulture|25_505()
		{
			Settlement settlement = (from s in Settlement.All
				where s.Culture != MobileParty.MainParty.LastVisitedSettlement.Culture
				orderby Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty.LastVisitedSettlement, s, false, false, MobileParty.NavigationType.All)
				select s).FirstOrDefault<Settlement>();
			if (settlement == null)
			{
				return null;
			}
			return settlement.Culture;
		}

		// Token: 0x0600402B RID: 16427 RVA: 0x0012AF80 File Offset: 0x00129180
		[CompilerGenerated]
		private Hero <InitializeIncidents>g__GetLetterOfReferenceNotable|25_516()
		{
			return IncidentHelper.GetSeededRandomElement<Hero>((from x in MobileParty.MainParty.LastVisitedSettlement.Town.Settlement.Notables
				where x.Occupation == Occupation.Merchant && x.GetTraitLevel(DefaultTraits.Honor) <= 0
				select x).ToList<Hero>(), this._activeIncidentSeed);
		}

		// Token: 0x0600402F RID: 16431 RVA: 0x0012B094 File Offset: 0x00129294
		[CompilerGenerated]
		private Clan <InitializeIncidents>g__GetTillerAndWandererBanditPartyClan|25_523()
		{
			if (MobileParty.MainParty.RandomFloatWithSeed((uint)this._activeIncidentSeed) >= 0.5f)
			{
				return Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "steppe_bandits");
			}
			return Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "desert_bandits");
		}

		// Token: 0x06004031 RID: 16433 RVA: 0x0012B1E7 File Offset: 0x001293E7
		[CompilerGenerated]
		private ItemObject <InitializeIncidents>g__GetNearlyAGiftHorseHorse|25_546()
		{
			return IncidentHelper.GetSeededRandomElement<ItemObject>((from x in Campaign.Current.AllItems
				where x.ItemCategory == DefaultItemCategories.Horse && x.Culture == MobileParty.MainParty.LastVisitedSettlement.Culture && x.Value > 500
				select x).ToList<ItemObject>(), this._activeIncidentSeed);
		}

		// Token: 0x040012CD RID: 4813
		private CampaignTime _lastGlobalIncidentCooldown = CampaignTime.Zero;

		// Token: 0x040012CE RID: 4814
		private Dictionary<Incident, CampaignTime> _incidentsOnCooldown = new Dictionary<Incident, CampaignTime>();

		// Token: 0x040012CF RID: 4815
		private long _activeIncidentSeed;

		// Token: 0x040012D0 RID: 4816
		private bool _canInvokeSettlementEvent;

		// Token: 0x02000800 RID: 2048
		[Flags]
		public enum IncidentTrigger
		{
			// Token: 0x04001FF9 RID: 8185
			LeavingVillage = 1,
			// Token: 0x04001FFA RID: 8186
			LeavingTown = 2,
			// Token: 0x04001FFB RID: 8187
			LeavingCastle = 4,
			// Token: 0x04001FFC RID: 8188
			LeavingSettlement = 8,
			// Token: 0x04001FFD RID: 8189
			LeavingEncounter = 16,
			// Token: 0x04001FFE RID: 8190
			LeavingBattle = 32,
			// Token: 0x04001FFF RID: 8191
			EnteringVillage = 64,
			// Token: 0x04002000 RID: 8192
			EnteringTown = 128,
			// Token: 0x04002001 RID: 8193
			EnteringCastle = 256,
			// Token: 0x04002002 RID: 8194
			WaitingInSettlement = 512,
			// Token: 0x04002003 RID: 8195
			DuringSiege = 1024
		}

		// Token: 0x02000801 RID: 2049
		public enum IncidentType
		{
			// Token: 0x04002005 RID: 8197
			TroopSettlementRelation,
			// Token: 0x04002006 RID: 8198
			FoodConsumption,
			// Token: 0x04002007 RID: 8199
			PlightOfCivilians,
			// Token: 0x04002008 RID: 8200
			PartyCampLife,
			// Token: 0x04002009 RID: 8201
			AnimalIllness,
			// Token: 0x0400200A RID: 8202
			Illness,
			// Token: 0x0400200B RID: 8203
			HuntingForaging,
			// Token: 0x0400200C RID: 8204
			PostBattle,
			// Token: 0x0400200D RID: 8205
			HardTravel,
			// Token: 0x0400200E RID: 8206
			Profit,
			// Token: 0x0400200F RID: 8207
			DreamsSongsAndSigns,
			// Token: 0x04002010 RID: 8208
			FiefManagement,
			// Token: 0x04002011 RID: 8209
			Siege,
			// Token: 0x04002012 RID: 8210
			Workshop
		}
	}
}
