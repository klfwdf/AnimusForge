using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E2 RID: 994
	public class CrimeCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003D05 RID: 15621 RVA: 0x00108D98 File Offset: 0x00106F98
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterGameCreated));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterGameCreated));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroDeath));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
		}

		// Token: 0x06003D06 RID: 15622 RVA: 0x00108E18 File Offset: 0x00107018
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003D07 RID: 15623 RVA: 0x00108E1C File Offset: 0x0010701C
		private void OnDailyTick()
		{
			foreach (Clan clan in Clan.NonBanditFactions)
			{
				float dailyCrimeRatingChange = clan.DailyCrimeRatingChange;
				if (!clan.IsEliminated && !dailyCrimeRatingChange.ApproximatelyEqualsTo(0f, 1E-05f))
				{
					ChangeCrimeRatingAction.Apply(clan, dailyCrimeRatingChange, false);
				}
			}
			foreach (Kingdom kingdom in Kingdom.All)
			{
				float dailyCrimeRatingChange2 = kingdom.DailyCrimeRatingChange;
				if (!kingdom.IsEliminated && !dailyCrimeRatingChange2.ApproximatelyEqualsTo(0f, 1E-05f))
				{
					ChangeCrimeRatingAction.Apply(kingdom, dailyCrimeRatingChange2, false);
				}
			}
		}

		// Token: 0x06003D08 RID: 15624 RVA: 0x00108EF4 File Offset: 0x001070F4
		private void OnAfterGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
		}

		// Token: 0x06003D09 RID: 15625 RVA: 0x00108F00 File Offset: 0x00107100
		private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddGameMenu("town_inside_criminal", "{=XgA2JgVR}You are brought to the town square to face judgment.", new OnInitDelegate(CrimeCampaignBehavior.town_inside_criminal_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_punishment", "{=8iDpmu0L}Accept corporal punishment", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_pay_by_punishment_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_pay_by_punishment_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_give_punishment_and_money", "{=Xi1wpR2L}Accept corporal punishment and pay {FINE}{GOLD_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_punishment_and_money_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_punishment_and_money_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_give_your_life", "{=bVi0JKSx}You will be executed. You must face it as bravely as you can", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_your_life_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_your_life_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_influence", "{=1cMS6415}Pay {FINE}{INFLUENCE_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_influence_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_influence_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_money", "{=870ZCp1J}Pay {FINE}{GOLD_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_money_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_money_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_ignore_charges", "{=UQhRKJb9}Ignore the charges", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_ignore_charges_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_ignore_charges_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenu("town_discuss_criminal_surrender", "{=lwVwe4qU}You are discussing the terms of your surrender.", new OnInitDelegate(CrimeCampaignBehavior.town_discuss_criminal_surrender_on_init), GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_punishment", "{=8iDpmu0L}Accept corporal punishment", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_pay_by_punishment_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_pay_by_punishment_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_give_punishment_and_money", "{=Xi1wpR2L}Accept corporal punishment and pay {FINE}{GOLD_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_punishment_and_money_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_punishment_and_money_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_give_your_life", "{=VSzwMDJ2}You will be put to death", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_your_life_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_your_life_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_influence", "{=1cMS6415}Pay {FINE}{INFLUENCE_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_influence_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_influence_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_money", "{=870ZCp1J}Pay {FINE}{GOLD_ICON}", new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_money_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.criminal_inside_menu_give_money_on_consequence), false, -1, false, null);
			campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_back", GameTexts.FindText("str_back", null).ToString(), new GameMenuOption.OnConditionDelegate(CrimeCampaignBehavior.town_discuss_criminal_surrender_on_condition), new GameMenuOption.OnConsequenceDelegate(CrimeCampaignBehavior.town_discuss_criminal_surrender_back_on_consequence), true, -1, false, null);
		}

		// Token: 0x06003D0A RID: 15626 RVA: 0x001091A4 File Offset: 0x001073A4
		private void OnHeroDeath(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
		{
			if (victim == Hero.MainHero)
			{
				foreach (Clan clan in Clan.NonBanditFactions)
				{
					if (!clan.IsEliminated)
					{
						ChangeCrimeRatingAction.Apply(clan, -clan.MainHeroCrimeRating, true);
					}
				}
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (!kingdom.IsEliminated)
					{
						ChangeCrimeRatingAction.Apply(kingdom, -kingdom.MainHeroCrimeRating, true);
					}
				}
			}
		}

		// Token: 0x06003D0B RID: 15627 RVA: 0x0010925C File Offset: 0x0010745C
		private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
		{
			if (side1Faction == Hero.MainHero.MapFaction || side2Faction == Hero.MainHero.MapFaction)
			{
				IFaction faction = ((side1Faction == Hero.MainHero.MapFaction) ? side2Faction : side1Faction);
				float num = Campaign.Current.Models.CrimeModel.DeclareWarCrimeRatingThreshold * 0.5f;
				if (faction.MainHeroCrimeRating > num)
				{
					ChangeCrimeRatingAction.Apply(faction, num - faction.MainHeroCrimeRating, true);
				}
			}
		}

		// Token: 0x06003D0C RID: 15628 RVA: 0x001092C8 File Offset: 0x001074C8
		private static bool CanPayCriminalRatingValueWith(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
		{
			if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction))
			{
				if (paymentMethod == CrimeModel.PaymentMethod.Gold)
				{
					return true;
				}
				if (CrimeCampaignBehavior.IsCriminalPlayerInSameKingdomOf(faction))
				{
					if (paymentMethod == CrimeModel.PaymentMethod.Influence)
					{
						return true;
					}
				}
				else if (paymentMethod == CrimeModel.PaymentMethod.Punishment)
				{
					return true;
				}
			}
			else if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
			{
				if (CrimeCampaignBehavior.IsCriminalPlayerInSameKingdomOf(faction))
				{
					if (paymentMethod == CrimeModel.PaymentMethod.Gold)
					{
						return true;
					}
					if (paymentMethod == CrimeModel.PaymentMethod.Influence)
					{
						return true;
					}
				}
				else
				{
					if (paymentMethod.HasAnyFlag(CrimeModel.PaymentMethod.Execution))
					{
						return Hero.MainHero.Gold < (int)PayForCrimeAction.GetClearCrimeCost(faction, CrimeModel.PaymentMethod.Gold);
					}
					if (paymentMethod.HasAllFlags(CrimeModel.PaymentMethod.Gold | CrimeModel.PaymentMethod.Punishment))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06003D0D RID: 15629 RVA: 0x00109370 File Offset: 0x00107570
		private static bool IsCriminalPlayerInSameKingdomOf(IFaction faction)
		{
			Clan clan = faction as Clan;
			return Hero.MainHero.Clan == faction || Hero.MainHero.Clan.Kingdom == faction || (clan != null && Hero.MainHero.Clan.Kingdom == clan.Kingdom);
		}

		// Token: 0x06003D0E RID: 15630 RVA: 0x001093C1 File Offset: 0x001075C1
		[GameMenuInitializationHandler("town_discuss_criminal_surrender")]
		[GameMenuInitializationHandler("town_inside_criminal")]
		public static void game_menu_town_criminal_on_init(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.Town.WaitMeshName);
		}

		// Token: 0x06003D0F RID: 15631 RVA: 0x001093DD File Offset: 0x001075DD
		public static void town_inside_criminal_on_init(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.CurrentSettlement == null)
			{
				PlayerEncounter.EnterSettlement();
			}
		}

		// Token: 0x06003D10 RID: 15632 RVA: 0x001093F0 File Offset: 0x001075F0
		public static void town_discuss_criminal_surrender_on_init(MenuCallbackArgs args)
		{
		}

		// Token: 0x06003D11 RID: 15633 RVA: 0x001093F2 File Offset: 0x001075F2
		public static bool criminal_inside_menu_pay_by_punishment_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			args.Tooltip = new TextObject("{=yGvsbUWc}Beware that you may die from punishment!", null);
			return CrimeCampaignBehavior.CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Punishment);
		}

		// Token: 0x06003D12 RID: 15634 RVA: 0x00109420 File Offset: 0x00107620
		public static void criminal_inside_menu_pay_by_punishment_on_consequence(MenuCallbackArgs args)
		{
			PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Punishment);
			if (Hero.MainHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.Murdered)
			{
				if (Campaign.Current.CurrentMenuContext != null)
				{
					if (Settlement.CurrentSettlement.IsCastle)
					{
						GameMenu.SwitchToMenu("castle_outside");
						return;
					}
					GameMenu.SwitchToMenu("town_outside");
					return;
				}
				else
				{
					PlayerEncounter.Finish(true);
				}
			}
		}

		// Token: 0x06003D13 RID: 15635 RVA: 0x00109480 File Offset: 0x00107680
		public static bool criminal_inside_menu_give_money_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Trade;
			int num = (int)PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
			args.Text.SetTextVariable("FINE", num);
			args.Text.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			if (Hero.MainHero.Gold < num)
			{
				args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.", null);
				args.IsEnabled = false;
			}
			return CrimeCampaignBehavior.CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
		}

		// Token: 0x06003D14 RID: 15636 RVA: 0x00109504 File Offset: 0x00107704
		public static void criminal_inside_menu_give_money_on_consequence(MenuCallbackArgs args)
		{
			PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_outside");
				return;
			}
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x06003D15 RID: 15637 RVA: 0x00109538 File Offset: 0x00107738
		public static bool criminal_inside_menu_give_influence_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
			float clearCrimeCost = PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
			args.Text.SetTextVariable("FINE", clearCrimeCost.ToString("F1"));
			args.Text.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
			if (Clan.PlayerClan.Influence < clearCrimeCost)
			{
				args.Tooltip = new TextObject("{=rMagXCrI}You don't have enough influence to get the charges dropped.", null);
				args.IsEnabled = false;
			}
			return CrimeCampaignBehavior.CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
		}

		// Token: 0x06003D16 RID: 15638 RVA: 0x001095C5 File Offset: 0x001077C5
		public static void criminal_inside_menu_give_influence_on_consequence(MenuCallbackArgs args)
		{
			PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_outside");
				return;
			}
			GameMenu.SwitchToMenu("town_outside");
		}

		// Token: 0x06003D17 RID: 15639 RVA: 0x001095F8 File Offset: 0x001077F8
		public static bool criminal_inside_menu_give_punishment_and_money_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			int num = (int)PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
			args.Text.SetTextVariable("FINE", num);
			args.Text.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			if (Hero.MainHero.Gold < num)
			{
				args.Tooltip = new TextObject("{=ETKyjOkJ}You don't have enough denars to pay the fine.", null);
				args.IsEnabled = false;
			}
			else
			{
				args.Tooltip = new TextObject("{=yGvsbUWc}Beware that you may die from punishment!", null);
			}
			return CrimeCampaignBehavior.CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold | CrimeModel.PaymentMethod.Punishment);
		}

		// Token: 0x06003D18 RID: 15640 RVA: 0x00109690 File Offset: 0x00107890
		public static void criminal_inside_menu_give_punishment_and_money_on_consequence(MenuCallbackArgs args)
		{
			PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold | CrimeModel.PaymentMethod.Punishment);
			if (Hero.MainHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.Murdered)
			{
				if (Campaign.Current.CurrentMenuContext != null)
				{
					if (Settlement.CurrentSettlement.IsCastle)
					{
						GameMenu.SwitchToMenu("castle_outside");
						return;
					}
					GameMenu.SwitchToMenu("town_outside");
					return;
				}
				else
				{
					PlayerEncounter.Finish(true);
				}
			}
		}

		// Token: 0x06003D19 RID: 15641 RVA: 0x001096EE File Offset: 0x001078EE
		public static bool criminal_inside_menu_give_your_life_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			return CrimeCampaignBehavior.CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Execution);
		}

		// Token: 0x06003D1A RID: 15642 RVA: 0x00109708 File Offset: 0x00107908
		public static void criminal_inside_menu_give_your_life_on_consequence(MenuCallbackArgs args)
		{
			PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Execution);
		}

		// Token: 0x06003D1B RID: 15643 RVA: 0x0010971A File Offset: 0x0010791A
		public static bool criminal_inside_menu_ignore_charges_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return CrimeCampaignBehavior.IsCriminalPlayerInSameKingdomOf(Settlement.CurrentSettlement.MapFaction);
		}

		// Token: 0x06003D1C RID: 15644 RVA: 0x00109733 File Offset: 0x00107933
		public static void criminal_inside_menu_ignore_charges_on_consequence(MenuCallbackArgs args)
		{
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle");
				return;
			}
			GameMenu.SwitchToMenu("town");
		}

		// Token: 0x06003D1D RID: 15645 RVA: 0x00109756 File Offset: 0x00107956
		public static void town_discuss_criminal_surrender_back_on_consequence(MenuCallbackArgs args)
		{
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_guard");
				return;
			}
			GameMenu.SwitchToMenu("town_guard");
		}

		// Token: 0x06003D1E RID: 15646 RVA: 0x00109779 File Offset: 0x00107979
		public static bool town_discuss_criminal_surrender_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
			return true;
		}
	}
}
