using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CrimeCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnAfterGameCreated);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnAfterGameCreated);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroDeath);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnDailyTick()
	{
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			float dailyCrimeRatingChange = nonBanditFaction.DailyCrimeRatingChange;
			if (!nonBanditFaction.IsEliminated && !dailyCrimeRatingChange.ApproximatelyEqualsTo(0f))
			{
				ChangeCrimeRatingAction.Apply(nonBanditFaction, dailyCrimeRatingChange, showNotification: false);
			}
		}
		foreach (Kingdom item in Kingdom.All)
		{
			float dailyCrimeRatingChange2 = item.DailyCrimeRatingChange;
			if (!item.IsEliminated && !dailyCrimeRatingChange2.ApproximatelyEqualsTo(0f))
			{
				ChangeCrimeRatingAction.Apply(item, dailyCrimeRatingChange2, showNotification: false);
			}
		}
	}

	private void OnAfterGameCreated(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenu("town_inside_criminal", "{=XgA2JgVR}You are brought to the town square to face judgment.", town_inside_criminal_on_init);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_punishment", "{=8iDpmu0L}Accept corporal punishment", criminal_inside_menu_pay_by_punishment_on_condition, criminal_inside_menu_pay_by_punishment_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_give_punishment_and_money", "{=Xi1wpR2L}Accept corporal punishment and pay {FINE}{GOLD_ICON}", criminal_inside_menu_give_punishment_and_money_on_condition, criminal_inside_menu_give_punishment_and_money_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_give_your_life", "{=bVi0JKSx}You will be executed. You must face it as bravely as you can", criminal_inside_menu_give_your_life_on_condition, criminal_inside_menu_give_your_life_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_influence", "{=1cMS6415}Pay {FINE}{INFLUENCE_ICON}", criminal_inside_menu_give_influence_on_condition, criminal_inside_menu_give_influence_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_pay_by_money", "{=870ZCp1J}Pay {FINE}{GOLD_ICON}", criminal_inside_menu_give_money_on_condition, criminal_inside_menu_give_money_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_inside_criminal", "criminal_inside_menu_ignore_charges", "{=UQhRKJb9}Ignore the charges", criminal_inside_menu_ignore_charges_on_condition, criminal_inside_menu_ignore_charges_on_consequence);
		campaignGameSystemStarter.AddGameMenu("town_discuss_criminal_surrender", "{=lwVwe4qU}You are discussing the terms of your surrender.", town_discuss_criminal_surrender_on_init);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_punishment", "{=8iDpmu0L}Accept corporal punishment", criminal_inside_menu_pay_by_punishment_on_condition, criminal_inside_menu_pay_by_punishment_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_give_punishment_and_money", "{=Xi1wpR2L}Accept corporal punishment and pay {FINE}{GOLD_ICON}", criminal_inside_menu_give_punishment_and_money_on_condition, criminal_inside_menu_give_punishment_and_money_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_give_your_life", "{=VSzwMDJ2}You will be put to death", criminal_inside_menu_give_your_life_on_condition, criminal_inside_menu_give_your_life_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_influence", "{=1cMS6415}Pay {FINE}{INFLUENCE_ICON}", criminal_inside_menu_give_influence_on_condition, criminal_inside_menu_give_influence_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_pay_by_money", "{=870ZCp1J}Pay {FINE}{GOLD_ICON}", criminal_inside_menu_give_money_on_condition, criminal_inside_menu_give_money_on_consequence);
		campaignGameSystemStarter.AddGameMenuOption("town_discuss_criminal_surrender", "town_discuss_criminal_surrender_back", GameTexts.FindText("str_back").ToString(), town_discuss_criminal_surrender_on_condition, town_discuss_criminal_surrender_back_on_consequence, isLeave: true);
	}

	private void OnHeroDeath(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		if (victim != Hero.MainHero)
		{
			return;
		}
		foreach (Clan nonBanditFaction in Clan.NonBanditFactions)
		{
			if (!nonBanditFaction.IsEliminated)
			{
				ChangeCrimeRatingAction.Apply(nonBanditFaction, 0f - nonBanditFaction.MainHeroCrimeRating);
			}
		}
		foreach (Kingdom item in Kingdom.All)
		{
			if (!item.IsEliminated)
			{
				ChangeCrimeRatingAction.Apply(item, 0f - item.MainHeroCrimeRating);
			}
		}
	}

	private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		if (side1Faction == Hero.MainHero.MapFaction || side2Faction == Hero.MainHero.MapFaction)
		{
			IFaction faction = ((side1Faction == Hero.MainHero.MapFaction) ? side2Faction : side1Faction);
			float num = Campaign.Current.Models.CrimeModel.DeclareWarCrimeRatingThreshold * 0.5f;
			if (faction.MainHeroCrimeRating > num)
			{
				ChangeCrimeRatingAction.Apply(faction, num - faction.MainHeroCrimeRating);
			}
		}
	}

	private static bool CanPayCriminalRatingValueWith(IFaction faction, CrimeModel.PaymentMethod paymentMethod)
	{
		if (Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(Settlement.CurrentSettlement.MapFaction))
		{
			if (paymentMethod == CrimeModel.PaymentMethod.Gold)
			{
				return true;
			}
			if (IsCriminalPlayerInSameKingdomOf(faction))
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
			if (IsCriminalPlayerInSameKingdomOf(faction))
			{
				switch (paymentMethod)
				{
				case CrimeModel.PaymentMethod.Gold:
					return true;
				case CrimeModel.PaymentMethod.Influence:
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

	private static bool IsCriminalPlayerInSameKingdomOf(IFaction faction)
	{
		Clan clan = faction as Clan;
		if (Hero.MainHero.Clan != faction && Hero.MainHero.Clan.Kingdom != faction)
		{
			if (clan != null)
			{
				return Hero.MainHero.Clan.Kingdom == clan.Kingdom;
			}
			return false;
		}
		return true;
	}

	[GameMenuInitializationHandler("town_discuss_criminal_surrender")]
	[GameMenuInitializationHandler("town_inside_criminal")]
	public static void game_menu_town_criminal_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.Town.WaitMeshName);
	}

	public static void town_inside_criminal_on_init(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.CurrentSettlement == null)
		{
			PlayerEncounter.EnterSettlement();
		}
	}

	public static void town_discuss_criminal_surrender_on_init(MenuCallbackArgs args)
	{
	}

	public static bool criminal_inside_menu_pay_by_punishment_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
		args.Tooltip = new TextObject("{=yGvsbUWc}Beware that you may die from punishment!");
		return CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Punishment);
	}

	public static void criminal_inside_menu_pay_by_punishment_on_consequence(MenuCallbackArgs args)
	{
		PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Punishment);
		if (Hero.MainHero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Murdered)
		{
			return;
		}
		if (Campaign.Current.CurrentMenuContext != null)
		{
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_outside");
			}
			else
			{
				GameMenu.SwitchToMenu("town_outside");
			}
		}
		else
		{
			PlayerEncounter.Finish();
		}
	}

	public static bool criminal_inside_menu_give_money_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Trade;
		int num = (int)PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
		args.Text.SetTextVariable("FINE", num);
		args.Text.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		if (Hero.MainHero.Gold < num)
		{
			args.Tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
			args.IsEnabled = false;
		}
		return CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
	}

	public static void criminal_inside_menu_give_money_on_consequence(MenuCallbackArgs args)
	{
		PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
		if (Settlement.CurrentSettlement.IsCastle)
		{
			GameMenu.SwitchToMenu("castle_outside");
		}
		else
		{
			GameMenu.SwitchToMenu("town_outside");
		}
	}

	public static bool criminal_inside_menu_give_influence_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
		float clearCrimeCost = PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
		args.Text.SetTextVariable("FINE", clearCrimeCost.ToString("F1"));
		args.Text.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		if (Clan.PlayerClan.Influence < clearCrimeCost)
		{
			args.Tooltip = new TextObject("{=rMagXCrI}You don't have enough influence to get the charges dropped.");
			args.IsEnabled = false;
		}
		return CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
	}

	public static void criminal_inside_menu_give_influence_on_consequence(MenuCallbackArgs args)
	{
		PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Influence);
		if (Settlement.CurrentSettlement.IsCastle)
		{
			GameMenu.SwitchToMenu("castle_outside");
		}
		else
		{
			GameMenu.SwitchToMenu("town_outside");
		}
	}

	public static bool criminal_inside_menu_give_punishment_and_money_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		int num = (int)PayForCrimeAction.GetClearCrimeCost(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold);
		args.Text.SetTextVariable("FINE", num);
		args.Text.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		if (Hero.MainHero.Gold < num)
		{
			args.Tooltip = new TextObject("{=ETKyjOkJ}You don't have enough denars to pay the fine.");
			args.IsEnabled = false;
		}
		else
		{
			args.Tooltip = new TextObject("{=yGvsbUWc}Beware that you may die from punishment!");
		}
		return CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold | CrimeModel.PaymentMethod.Punishment);
	}

	public static void criminal_inside_menu_give_punishment_and_money_on_consequence(MenuCallbackArgs args)
	{
		PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Gold | CrimeModel.PaymentMethod.Punishment);
		if (Hero.MainHero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Murdered)
		{
			return;
		}
		if (Campaign.Current.CurrentMenuContext != null)
		{
			if (Settlement.CurrentSettlement.IsCastle)
			{
				GameMenu.SwitchToMenu("castle_outside");
			}
			else
			{
				GameMenu.SwitchToMenu("town_outside");
			}
		}
		else
		{
			PlayerEncounter.Finish();
		}
	}

	public static bool criminal_inside_menu_give_your_life_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
		return CanPayCriminalRatingValueWith(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Execution);
	}

	public static void criminal_inside_menu_give_your_life_on_consequence(MenuCallbackArgs args)
	{
		PayForCrimeAction.Apply(Settlement.CurrentSettlement.MapFaction, CrimeModel.PaymentMethod.Execution);
	}

	public static bool criminal_inside_menu_ignore_charges_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return IsCriminalPlayerInSameKingdomOf(Settlement.CurrentSettlement.MapFaction);
	}

	public static void criminal_inside_menu_ignore_charges_on_consequence(MenuCallbackArgs args)
	{
		if (Settlement.CurrentSettlement.IsCastle)
		{
			GameMenu.SwitchToMenu("castle");
		}
		else
		{
			GameMenu.SwitchToMenu("town");
		}
	}

	public static void town_discuss_criminal_surrender_back_on_consequence(MenuCallbackArgs args)
	{
		if (Settlement.CurrentSettlement.IsCastle)
		{
			GameMenu.SwitchToMenu("castle_guard");
		}
		else
		{
			GameMenu.SwitchToMenu("town_guard");
		}
	}

	public static bool town_discuss_criminal_surrender_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return true;
	}
}
