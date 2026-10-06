using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BattleWreckageCampaignBehavior : CampaignBehaviorBase
{
	private readonly struct BattleWreckageConsequence
	{
		public readonly string StringId;

		public readonly Func<BattleWreckage, bool> Condition;

		public readonly Action Consequence;

		public readonly float Chance;

		public readonly bool CanOverride;

		public BattleWreckageConsequence(string stringId, Func<BattleWreckage, bool> condition, Action consequence, float chance = 0.5f, bool canOverride = false)
		{
			StringId = stringId;
			Condition = condition;
			Consequence = consequence;
			Chance = chance;
			CanOverride = canOverride;
		}
	}

	private enum WreckageLeaveReason
	{
		LeaveWithoutInvestigation,
		AbandonInvestigation,
		InvestigationCompleted,
		PlayerInterrupted,
		WreckageExpired
	}

	private const float SmallWreckageCreationChance = 0.85f;

	private const string LandWreckageMenuBackgroundId = "wreckage_land";

	private const string NavalWreckageMenuBackgroundId = "wreckage_sea";

	private const string MenuIdBattleWreckage = "battle_wreckage";

	private const string MenuIdInvestigateWait = "battle_wreckage_investigate_wait_menu";

	private const string MenuIdResults = "battle_wreckage_results_menu";

	private const string MenuIdRecoverTroopsDecision = "battle_wreckage_recover_troops_decision";

	private const string BattleWreckageResultsConnectionMenuId = "battle_wreckage_results_connection_menu";

	private const string MenuIdBattleRemains = "battle_wreckage_remains";

	private const string RecoverTroopsConsequenceId = "RecoverTroopsConsequence";

	private const string GainTradeGoodsConsequenceId = "GainTradeGoodsConsequence";

	private const string GainGoldConsequenceId = "GainGoldConsequence";

	private const string UnlockHideoutConsequenceId = "UnlockHideoutConsequence";

	private const string NothingFoundConsequenceId = "NothingFoundConsequence";

	public const string MenuIdWreckageEncounterBlocking = "battle_wreckage_encounter_blocking";

	private const int MinDistanceFromSettlementToConsiderWreckageOnLand = 10;

	private const int MinDistanceFromSettlementToConsiderWreckageOnNaval = 20;

	private const int MinDistanceNeededFromWreckageOnLand = 20;

	private const int MinDistanceNeededFromWreckageOnNaval = 20;

	private const float UnlockHideoutMapsChance = 0.5f;

	private const int NormalWreckageInvestigationMinRequiredPartySize = 50;

	private const int EpicWreckageInvestigationMinRequiredPartySize = 90;

	private const string WreckageMenuSoundPath = "event:/ui/wreckage/wreckage_panel";

	private const int InterruptedSnapshotExpiryAsHours = 24;

	private const string RewardMenuSoundPath = "event:/ui/wreckage/generic_recover";

	private const string InvestigationSoundPath = "event:/ui/wreckage/investigate_the_battlefield";

	private BattleWreckage _encounteredBattleWreckage;

	private CampaignTime _playerInvestigationStartTime;

	private CampaignTime _requiredInvestigationDuration;

	private CampaignTime _lastPlayerWreckageInteractionTime;

	private bool _isEncounteredWreckageInterrupted;

	private readonly List<BattleWreckageConsequence> _wreckageConsequences = new List<BattleWreckageConsequence>();

	private string _currentOverriddenConsequenceId;

	private bool _isOverriddenConsequenceAccepted;

	private ItemRoster _lootedItems = new ItemRoster();

	private TroopRoster _lootedTroops = TroopRoster.CreateDummyTroopRoster();

	private int _lootedGoldAmount;

	private List<TextObject> _consequenceExplanations = new List<TextObject>();

	private readonly List<BattleWreckageConsequence> _selectedConsequences = new List<BattleWreckageConsequence>();

	private int _recoverTroopCountsToAdd;

	private const int SmallWreckageRecoverTroopCountsLimitMin = 2;

	private const int SmallWreckageRecoverTroopCountsLimitMax = 5;

	private const int NormalWreckageRecoverTroopCountsLimitMin = 5;

	private const int NormalWreckageRecoverTroopCountsLimitMax = 10;

	private const int EpicWreckageRecoverTroopCountsLimitMin = 10;

	private const int EpicWreckageRecoverTroopCountsLimitMax = 15;

	private const int SmallWreckageRecoverTroopsTierLimit = 2;

	private const int NormalWreckageRecoverTroopsTierLimit = 3;

	private const float CasualtyLootChance = 0.5f;

	private const float TradeGoodBonusLootMultiplier = 0.5f;

	private const int TradeGoodTargetValue = 1000;

	private const int RogueryXpBonusBase = 75;

	private const float TradeGoodBonusMultiplier = 1.2f;

	private bool HasOverriddenConsequence => !string.IsNullOrEmpty(_currentOverriddenConsequenceId);

	private int EpicWreckageRecoverTroopsTierLimit => Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier;

	public BattleWreckageCampaignBehavior()
	{
		AddWreckageConsequences();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTickEvent);
		CampaignEvents.OnPartyEncounterEvent.AddNonSerializedListener(this, OnPartyEncounter);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTakenEvent);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_playerInvestigationStartTime", ref _playerInvestigationStartTime);
		dataStore.SyncData("_requiredInvestigationDuration", ref _requiredInvestigationDuration);
		dataStore.SyncData("_consequenceExplanations", ref _consequenceExplanations);
		dataStore.SyncData("_lootedGoldAmount", ref _lootedGoldAmount);
		dataStore.SyncData("_encounteredBattleWreckage", ref _encounteredBattleWreckage);
		dataStore.SyncData("_lootedItems", ref _lootedItems);
		dataStore.SyncData("_lootedTroops", ref _lootedTroops);
		dataStore.SyncData("_currentOverriddenConsequenceId", ref _currentOverriddenConsequenceId);
		dataStore.SyncData("_isOverriddenConsequenceAccepted", ref _isOverriddenConsequenceAccepted);
		dataStore.SyncData("_recoverTroopCountsToAdd", ref _recoverTroopCountsToAdd);
		dataStore.SyncData("_lastPlayerWreckageInteractionTime", ref _lastPlayerWreckageInteractionTime);
		dataStore.SyncData("_isEncounteredWreckageInterrupted", ref _isEncounteredWreckageInterrupted);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	public void SetCurrentEncounteredBattleWreckage(BattleWreckage battleWreckage)
	{
		MobileParty.MainParty.SetMoveModeHold();
		_isEncounteredWreckageInterrupted = _encounteredBattleWreckage == battleWreckage;
		_encounteredBattleWreckage = battleWreckage;
		_playerInvestigationStartTime = CampaignTime.Now;
		if (!_isEncounteredWreckageInterrupted)
		{
			_requiredInvestigationDuration = CampaignTime.Hours(GetWreckageInvestigationDuration(_isEncounteredWreckageInterrupted).ResultNumber);
		}
		if (Campaign.Current.Models.BattleWreckageModel.CanPlayerInteractWithWreckage(out var _))
		{
			if (battleWreckage.IsInvestigated && battleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
			{
				GameMenu.ActivateGameMenu("battle_wreckage_remains");
			}
			else
			{
				GameMenu.ActivateGameMenu("battle_wreckage");
			}
		}
		else
		{
			GameMenu.ActivateGameMenu("battle_wreckage_encounter_blocking");
		}
	}

	private void OnHeroPrisonerTakenEvent(PartyBase capturerParty, Hero hero)
	{
		if (hero == Hero.MainHero && _encounteredBattleWreckage != null)
		{
			LeaveWreckageEncounter(WreckageLeaveReason.WreckageExpired);
		}
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("battle_wreckage", "{=!}{WRECKAGE_INIT_TEXT}", game_menu_battle_wreckage_init);
		campaignGameStarter.AddGameMenuOption("battle_wreckage", "investigate", "{=!}{WRECKAGE_INVESTIGATE_OPTION_TEXT}", game_menu_investigate_wreckage_condition, game_menu_investigate_wreckage_consequence);
		campaignGameStarter.AddGameMenuOption("battle_wreckage", "leave", "{=3sRdGQou}Leave", game_menu_wreckage_init_leave_option_condition, battle_wreckage_leave_without_start_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("battle_wreckage_results_menu", "{=!}{WRECKAGE_RESULTS_TEXT}", wreckage_results_menu_init);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_results_menu", "leave", "{=DM6luo3c}Continue", wreckage_results_menu_continue_option_condition, battle_wreckage_results_leave_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("battle_wreckage_results_connection_menu", "{=!}Player should not see this text", wreckage_results_connection_menu);
		campaignGameStarter.AddWaitGameMenu("battle_wreckage_investigate_wait_menu", "{=!}{INVESTIGATION_WAIT_MENU_TEXT}", game_menu_battle_wreckage_investigate_init, null, game_menu_battle_wreckage_investigate_consequence, game_menu_battle_wreckage_investigate_tick, GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_investigate_wait_menu", "leave", "{=3sRdGQou}Leave", investigation_menu_leave_option_condition, battle_wreckage_investigate_leave_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("battle_wreckage_recover_troops_decision", "{=!}{RECOVER_TROOPS_TEXT}", game_menu_recover_troops_decision_init);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_recover_troops_decision", "recover", "{=skLedR3E}Help the wounded", recover_troops_accept_option_condition, game_menu_recover_troops_accept_consequence);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_recover_troops_decision", "leave", "{=rClFoou7}Leave them", recover_troops_leave_option_condition, game_menu_recover_troops_decline_consequence);
		campaignGameStarter.AddGameMenu("battle_wreckage_remains", "{=!}{BATTLE_REMAINS_TEXT}", game_menu_battle_remains_init);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_remains", "leave", "{=3sRdGQou}Leave", battle_remains_leave_condition, battle_remains_leave_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("battle_wreckage_encounter_blocking", "{=!}{BATTLE_WRECKAGE_BLOCKED_TEXT}", wreckage_encounter_blocked_menu_init);
		campaignGameStarter.AddGameMenuOption("battle_wreckage_encounter_blocking", "leave", "{=3sRdGQou}Leave", wreckage_encounter_blocked_leave_on_condition, wreckage_encounter_blocked_consequence, isLeave: true);
	}

	private void AddWreckageConsequences()
	{
		_wreckageConsequences.Add(new BattleWreckageConsequence("RecoverTroopsConsequence", RecoverTroopsCondition, RecoverTroopsConsequence, 0.6f, canOverride: true));
		_wreckageConsequences.Add(new BattleWreckageConsequence("GainTradeGoodsConsequence", null, GainTradeGoodsConsequence, 0.6f));
		_wreckageConsequences.Add(new BattleWreckageConsequence("GainGoldConsequence", null, GainGoldConsequence));
		_wreckageConsequences.Add(new BattleWreckageConsequence("NothingFoundConsequence", null, NothingFoundConsequence, -1f));
	}

	private void wreckage_results_menu_init(MenuCallbackArgs args)
	{
		TextObject textObject = (_encounteredBattleWreckage.Position.IsOnLand ? ((MobileParty.MainParty.MemberRoster.TotalManCount <= 1) ? new TextObject("{=mDhGN2y1}You finished searching the battleground. You found {LISTED_ELEMENTS} among the fallen.") : new TextObject("{=qYbSovv1}Your men finished searching the battleground. They found {LISTED_ELEMENTS} among the fallen.")) : ((MobileParty.MainParty.MemberRoster.TotalManCount <= 1) ? new TextObject("{=uBM2UY8b}You finished searching the wreckage. They found {LISTED_ELEMENTS} among the debris.") : new TextObject("{=aUGSXFHX}Your men finished searching the wreckage. They found {LISTED_ELEMENTS} among the debris.")));
		TextObject variable = GameTexts.GameTextHelper.MergeTextObjectsWithComma(_consequenceExplanations, _consequenceExplanations.Count > 1);
		textObject.SetTextVariable("LISTED_ELEMENTS", variable);
		GameTexts.SetVariable("WRECKAGE_RESULTS_TEXT", textObject);
		if (_selectedConsequences.AnyQ((BattleWreckageConsequence x) => x.StringId != "NothingFoundConsequence"))
		{
			args.MenuContext.SetPanelSound("event:/ui/wreckage/generic_recover");
		}
		SetWreckageMenuBackgrounds(args);
	}

	private bool wreckage_results_menu_continue_option_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void battle_wreckage_results_leave_consequence(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		LeaveWreckageEncounter(WreckageLeaveReason.InvestigationCompleted);
	}

	private void game_menu_battle_wreckage_init(MenuCallbackArgs args)
	{
		if (_encounteredBattleWreckage.IsInvestigated)
		{
			Debug.FailedAssert("Investigated wreckages should be routed to battle remains menu, not here. Check SetCurrentEncounteredBattleWreckage routing.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\BattleWreckageCampaignBehavior.cs", "game_menu_battle_wreckage_init", 319);
			return;
		}
		bool isEncounteredWreckageInterrupted = _isEncounteredWreckageInterrupted;
		bool isInterruptedDuringRecovery = !string.IsNullOrEmpty(_currentOverriddenConsequenceId);
		ExplainedNumber wreckageInvestigationDuration = GetWreckageInvestigationDuration(isEncounteredWreckageInterrupted);
		GetWreckageMenuText(isEncounteredWreckageInterrupted, isInterruptedDuringRecovery, wreckageInvestigationDuration, out var menuText, out var investigateOptionText, out var investigationDurationTooltip);
		GameTexts.SetVariable("WRECKAGE_INIT_TEXT", menuText);
		GameTexts.SetVariable("WRECKAGE_INVESTIGATE_OPTION_TEXT", investigateOptionText);
		GameTexts.SetVariable("WRECKAGE_INVESTIGATE_OPTION_TOOLTIP_TEXT", investigationDurationTooltip);
		args.MenuContext.SetPanelSound("event:/ui/wreckage/wreckage_panel");
		SetWreckageMenuBackgrounds(args);
	}

	private void GetWreckageMenuText(bool isInterruptedWreckage, bool isInterruptedDuringRecovery, ExplainedNumber investigationDurationWithExplanations, out TextObject menuText, out TextObject investigateOptionText, out TextObject investigationDurationTooltip)
	{
		float resultNumber = investigationDurationWithExplanations.ResultNumber;
		TextObject textObject = TextObject.GetEmpty();
		TextObject textObject2;
		if (isInterruptedWreckage)
		{
			if (isInterruptedDuringRecovery)
			{
				investigateOptionText = new TextObject("{=npdmTVcg}Continue the rescue.");
				if (_encounteredBattleWreckage.Position.IsOnLand)
				{
					menuText = new TextObject("{=6fgFwhVd}You return to the battlefield that you had been searching. You can continue to tend the wounded you had been treating.");
				}
				else
				{
					menuText = new TextObject("{=Mbbm6eGd}You return to the wreckage that you had been searching. You can continue to tend the wounded you had been treating.");
				}
			}
			else if (_encounteredBattleWreckage.Position.IsOnLand)
			{
				menuText = new TextObject("{=4LiDro4x}You return to the battlefield that you had been searching.");
				investigateOptionText = new TextObject("{=XOgUKPHA}Continue investigating the battlefield.");
			}
			else
			{
				menuText = new TextObject("{=RBjAz2Gf}You return to the wreckage that you had been searching.");
				investigateOptionText = new TextObject("{=Aau831CL}Continue investigating the wreckage.");
			}
			textObject2 = TextObject.GetEmpty();
		}
		else
		{
			if (_encounteredBattleWreckage.Position.IsOnLand)
			{
				investigateOptionText = new TextObject("{=qNdiPWQO}Investigate the battlefield.");
				if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
				{
					menuText = new TextObject("{=xGf9VgY9}Carrion-birds wheel overhead, and the smell of death is in the air. You have come across the remnants of a major battle.");
				}
				else if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Normal)
				{
					menuText = new TextObject("{=AInsSCZ7}Carrion-birds wheel overhead, and the smell of death is in the air. You have come across the remnants of a battle.");
				}
				else
				{
					menuText = new TextObject("{=wALUbsrH}Carrion-birds wheel overhead, and the smell of death is in the air. You have come across the remnants of a skirmish.");
				}
			}
			else
			{
				investigateOptionText = new TextObject("{=sSCh8BIb}Investigate the wreckage.");
				if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
				{
					menuText = new TextObject("{=WRMZFxxK}Your look-outs spot gulls wheeling overhead, and broken timbers in the water. You have come across the remnants of a major battle at sea.");
				}
				else if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Normal)
				{
					menuText = new TextObject("{=bR39Nv4Y}Your look-outs spot gulls wheeling overhead, and broken timbers in the water. You have come across the remnants of a naval battle.");
				}
				else
				{
					menuText = new TextObject("{=E90zM22N}Your look-outs spot gulls wheeling overhead, and broken timbers in the water. You have come across the remnants of a duel between ships.");
				}
			}
			textObject2 = new TextObject("{=!}{newline}{LOCAL_REMAINING_HOURS_EXPLANATIONS}");
			textObject2.SetTextVariable("LOCAL_REMAINING_HOURS_EXPLANATIONS", investigationDurationWithExplanations.GetExplanations());
			textObject2.SetTextVariable("newline", "\n");
			int requiredPartySizeToInvestigateWreckageEfficiently = GetRequiredPartySizeToInvestigateWreckageEfficiently();
			if (MobileParty.MainParty.MemberRoster.TotalManCount < requiredPartySizeToInvestigateWreckageEfficiently && !isInterruptedWreckage)
			{
				textObject = new TextObject("{=MhEbgb8c}{newline}You need at least {REQUIRED_TROOP_COUNT} troops to fully investigate this wreckage. With fewer troops, you will only find minor spoils.");
				textObject.SetTextVariable("REQUIRED_TROOP_COUNT", requiredPartySizeToInvestigateWreckageEfficiently);
				textObject.SetTextVariable("newline", "\n");
			}
		}
		investigationDurationTooltip = new TextObject("{=J83ajkAA}Remaining Hours: {REMAINING_HOURS}{REMAINING_HOURS_EXPLANATIONS}{EFFECTIVE_INVESTIGATION_REASON}");
		investigationDurationTooltip.SetTextVariable("REMAINING_HOURS", resultNumber);
		investigationDurationTooltip.SetTextVariable("REMAINING_HOURS_EXPLANATIONS", textObject2);
		investigationDurationTooltip.SetTextVariable("EFFECTIVE_INVESTIGATION_REASON", textObject);
	}

	private void SetWreckageMenuBackgrounds(MenuCallbackArgs args)
	{
		string backgroundMeshName = "wreckage_land";
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			backgroundMeshName = "wreckage_sea";
		}
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
	}

	private bool game_menu_investigate_wreckage_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		args.Tooltip = new TextObject("{=!}{WRECKAGE_INVESTIGATE_OPTION_TOOLTIP_TEXT}");
		return true;
	}

	private bool game_menu_wreckage_init_leave_option_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void battle_wreckage_leave_without_start_consequence(MenuCallbackArgs args)
	{
		LeaveWreckageEncounter(WreckageLeaveReason.LeaveWithoutInvestigation);
	}

	private bool investigation_menu_leave_option_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void battle_wreckage_investigate_leave_consequence(MenuCallbackArgs args)
	{
		LeaveWreckageEncounter(WreckageLeaveReason.AbandonInvestigation);
	}

	private void LeaveWreckageEncounter(WreckageLeaveReason reason)
	{
		switch (reason)
		{
		case WreckageLeaveReason.InvestigationCompleted:
			_encounteredBattleWreckage.OnWreckageInvestigated();
			if (_encounteredBattleWreckage.WreckageTypeCategory != BattleWreckage.WreckageType.Epic)
			{
				RemoveWreckageFromMap(_encounteredBattleWreckage);
			}
			break;
		case WreckageLeaveReason.AbandonInvestigation:
			if (!MobileParty.MainParty.IsDisorganized)
			{
				MobileParty.MainParty.SetDisorganized(isDisorganized: true);
			}
			break;
		case WreckageLeaveReason.PlayerInterrupted:
		{
			CampaignTime campaignTime = CampaignTime.Now - _playerInvestigationStartTime;
			float valueInHours = TaleWorlds.Library.MathF.Max(1f, (float)(_requiredInvestigationDuration.ToHours - campaignTime.ToHours));
			_requiredInvestigationDuration = CampaignTime.Hours(valueInHours);
			_lastPlayerWreckageInteractionTime = CampaignTime.Now;
			break;
		}
		}
		if (PlayerEncounter.Current == null && Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
		_playerInvestigationStartTime = CampaignTime.Zero;
		_selectedConsequences.Clear();
		_lootedItems.Clear();
		_lootedTroops.Clear();
		_lootedGoldAmount = 0;
		_consequenceExplanations.Clear();
		if (reason != WreckageLeaveReason.PlayerInterrupted)
		{
			_encounteredBattleWreckage = null;
			_recoverTroopCountsToAdd = 0;
			_requiredInvestigationDuration = CampaignTime.Zero;
			_currentOverriddenConsequenceId = string.Empty;
			_isOverriddenConsequenceAccepted = false;
			_lastPlayerWreckageInteractionTime = CampaignTime.Zero;
		}
	}

	private void wreckage_results_connection_menu(MenuCallbackArgs args)
	{
		ApplyWreckageInvestigationResults();
	}

	private void game_menu_investigate_wreckage_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("battle_wreckage_investigate_wait_menu");
	}

	private void game_menu_battle_wreckage_investigate_init(MenuCallbackArgs args)
	{
		TextObject content;
		if (!_isOverriddenConsequenceAccepted)
		{
			float value = (float)((CampaignTime.Now - _playerInvestigationStartTime).ToHours / _requiredInvestigationDuration.ToHours);
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(MBMath.ClampFloat(value, 0f, 1f));
			content = (_encounteredBattleWreckage.Position.IsOnLand ? ((MobileParty.MainParty.MemberRoster.TotalManCount <= 1) ? new TextObject("{=8cTc5fLY}You are searching amid the fallen.") : new TextObject("{=V5pVt2tn}Your troops are searching amid the fallen.")) : ((MobileParty.MainParty.Ships.Count != 1) ? new TextObject("{=zPf03RS6}Your ships are searching amid the debris.") : new TextObject("{=sB9m2SKE}Your ship are searching amid the debris.")));
		}
		else
		{
			float value2 = ((_playerInvestigationStartTime != CampaignTime.Zero) ? ((float)((CampaignTime.Now - _playerInvestigationStartTime).ToHours / _requiredInvestigationDuration.ToHours)) : 0f);
			args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(MBMath.ClampFloat(value2, 0f, 1f));
			content = ((!_encounteredBattleWreckage.Position.IsOnLand) ? new TextObject("{=0MUhpB1E}You are rescuing survivors from the water.") : new TextObject("{=hk5aNGtg}You are treating the wounded troops."));
		}
		GameTexts.SetVariable("INVESTIGATION_WAIT_MENU_TEXT", content);
		args.MenuContext.SetPanelSound("event:/ui/wreckage/investigate_the_battlefield");
		SetWreckageMenuBackgrounds(args);
	}

	private void game_menu_battle_wreckage_investigate_tick(MenuCallbackArgs args, CampaignTime dt)
	{
		float progressOfWaitingInMenu = TaleWorlds.Library.MathF.Min((float)((CampaignTime.Now - _playerInvestigationStartTime).ToHours / _requiredInvestigationDuration.ToHours), 1f);
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(progressOfWaitingInMenu);
	}

	private void game_menu_battle_wreckage_investigate_consequence(MenuCallbackArgs args)
	{
		SelectWreckageConsequences();
	}

	private void wreckage_encounter_blocked_menu_init(MenuCallbackArgs args)
	{
		Campaign.Current.Models.BattleWreckageModel.CanPlayerInteractWithWreckage(out var explanation);
		GameTexts.SetVariable("BATTLE_WRECKAGE_BLOCKED_TEXT", explanation);
		args.MenuContext.SetPanelSound("event:/ui/wreckage/wreckage_panel");
		SetWreckageMenuBackgrounds(args);
	}

	private bool wreckage_encounter_blocked_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void wreckage_encounter_blocked_consequence(MenuCallbackArgs args)
	{
		LeaveWreckageEncounter(WreckageLeaveReason.LeaveWithoutInvestigation);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (!CanMapEventCreateWreckage(mapEvent, out var newWreckageType, out var wreckagesToDestroy))
		{
			return;
		}
		if (wreckagesToDestroy.Count > 0)
		{
			foreach (BattleWreckage item in wreckagesToDestroy)
			{
				RemoveWreckageFromMap(item);
			}
		}
		SpawnWreckage(mapEvent, newWreckageType);
	}

	private void SpawnWreckage(MapEvent mapEvent, BattleWreckage.WreckageType newWreckageType)
	{
		CampaignTime wreckageDestroyTimeOnCreation = GetWreckageDestroyTimeOnCreation(newWreckageType);
		BattleWreckage.CreateWreckage(mapEvent, newWreckageType, wreckageDestroyTimeOnCreation);
	}

	private bool CanMapEventCreateWreckage(MapEvent mapEvent, out BattleWreckage.WreckageType newWreckageType, out MBReadOnlyList<BattleWreckage> wreckagesToDestroy)
	{
		wreckagesToDestroy = new MBReadOnlyList<BattleWreckage>();
		newWreckageType = BattleWreckage.WreckageType.Invalid;
		if (!mapEvent.IsFieldBattle || !mapEvent.HasWinner || mapEvent.IsPlayerMapEvent || mapEvent.InvolvedParties.AnyQ((PartyBase x) => x.MobileParty?.IsCurrentlyUsedByAQuest ?? false))
		{
			return false;
		}
		if (Campaign.Current.Wreckages.CountQ((BattleWreckage x) => (!mapEvent.IsNavalMapEvent) ? x.Position.IsOnLand : (!x.Position.IsOnLand)) >= Campaign.Current.Models.BattleWreckageModel.GetMaxWreckageCountForMapEventType(mapEvent))
		{
			return false;
		}
		int num = mapEvent.AttackerSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars) + mapEvent.DefenderSide.Parties.SumQ((MapEventParty x) => x.WoundedInBattle.TotalRegulars + x.DiedInBattle.TotalRegulars);
		int wreckageCreationBattleSizeThreshold = Campaign.Current.Models.BattleWreckageModel.GetWreckageCreationBattleSizeThreshold(mapEvent);
		if (num < wreckageCreationBattleSizeThreshold)
		{
			return false;
		}
		newWreckageType = Campaign.Current.Models.BattleWreckageModel.GetWreckageTypeForMapEvent(mapEvent);
		if (!IsMapEventPartiesSuitableToCreateBattleWreckage(mapEvent, newWreckageType))
		{
			return false;
		}
		if (!CanBattleWreckageCreateBasedOnPosition(mapEvent, newWreckageType, out wreckagesToDestroy))
		{
			return false;
		}
		if (newWreckageType == BattleWreckage.WreckageType.Small && MBRandom.RandomFloat > 0.85f)
		{
			return false;
		}
		return true;
	}

	private bool CanBattleWreckageCreateBasedOnPosition(MapEvent mapEvent, BattleWreckage.WreckageType wreckageTypeToCreate, out MBReadOnlyList<BattleWreckage> wreckagesToDestroy)
	{
		wreckagesToDestroy = new MBReadOnlyList<BattleWreckage>();
		CampaignVec2 position = mapEvent.Position;
		int num = (position.IsOnLand ? 20 : 20);
		bool flag = wreckageTypeToCreate == BattleWreckage.WreckageType.Epic;
		MBList<BattleWreckage> wreckagesToDestroy2 = null;
		CampaignVec2 vec = new CampaignVec2(position.ToVec2(), isOnLand: false);
		if (vec.Face.IsValid())
		{
			TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(in vec);
			if (terrainTypeAtPosition == TerrainType.River || terrainTypeAtPosition == TerrainType.UnderBridge || terrainTypeAtPosition == TerrainType.NonNavigableRiver)
			{
				return false;
			}
		}
		foreach (BattleWreckage wreckage in Campaign.Current.Wreckages)
		{
			if (!(position.Distance(wreckage.Position) >= (float)num))
			{
				if (!flag)
				{
					return false;
				}
				if (!CanEpicWreckageReplaceNearbyWreckage(wreckage, ref wreckagesToDestroy2))
				{
					return false;
				}
			}
		}
		if (IsNearSettlement(position))
		{
			return false;
		}
		if (wreckagesToDestroy2 != null)
		{
			wreckagesToDestroy = wreckagesToDestroy2;
		}
		return true;
	}

	private bool CanEpicWreckageReplaceNearbyWreckage(BattleWreckage nearbyWreckage, ref MBList<BattleWreckage> wreckagesToDestroy)
	{
		switch (nearbyWreckage.WreckageTypeCategory)
		{
		case BattleWreckage.WreckageType.Epic:
			return false;
		case BattleWreckage.WreckageType.Small:
		case BattleWreckage.WreckageType.Normal:
			if (IsWreckageDestroyable(nearbyWreckage, isForCreationNewWreckage: true))
			{
				if (wreckagesToDestroy == null)
				{
					wreckagesToDestroy = new MBList<BattleWreckage>();
				}
				wreckagesToDestroy.Add(nearbyWreckage);
				return true;
			}
			break;
		}
		return false;
	}

	private bool IsNearSettlement(CampaignVec2 position)
	{
		int num = (position.IsOnLand ? 10 : 20);
		LocatableSearchData<Settlement> data = Settlement.StartFindingLocatablesAroundPosition(position.ToVec2(), num);
		return Settlement.FindNextLocatable(ref data) != null;
	}

	private bool IsMapEventPartiesSuitableToCreateBattleWreckage(MapEvent mapEvent, BattleWreckage.WreckageType wreckageType)
	{
		IEnumerable<PartyBase> involvedParties = mapEvent.InvolvedParties;
		switch (wreckageType)
		{
		case BattleWreckage.WreckageType.Small:
		{
			bool result = false;
			{
				foreach (PartyBase item in involvedParties)
				{
					MobileParty mobileParty = item.MobileParty;
					if (mobileParty.IsVillager || mobileParty.IsCaravan || mobileParty.IsPatrolParty)
					{
						result = true;
					}
					else if (mobileParty.IsLordParty)
					{
						return false;
					}
				}
				return result;
			}
		}
		case BattleWreckage.WreckageType.Normal:
		case BattleWreckage.WreckageType.Epic:
			if (mapEvent.IsNavalMapEvent)
			{
				return true;
			}
			foreach (PartyBase item2 in involvedParties)
			{
				if (item2.MobileParty.IsLordParty)
				{
					return true;
				}
			}
			return false;
		default:
			Debug.FailedAssert("Wreckage type should be defined, check this case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\BattleWreckageCampaignBehavior.cs", "IsMapEventPartiesSuitableToCreateBattleWreckage", 875);
			return false;
		}
	}

	private CampaignTime GetWreckageDestroyTimeOnCreation(BattleWreckage.WreckageType wreckageType)
	{
		float valueInDays = 0f;
		switch (wreckageType)
		{
		case BattleWreckage.WreckageType.Small:
			valueInDays = MBRandom.RandomFloatRanged(6f, 9f);
			break;
		case BattleWreckage.WreckageType.Normal:
			valueInDays = MBRandom.RandomFloatRanged(12f, 15f);
			break;
		case BattleWreckage.WreckageType.Epic:
			valueInDays = MBRandom.RandomFloatRanged(24f, 30f);
			break;
		default:
			Debug.FailedAssert("This case should not be possible for the wreckage, check this", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\BattleWreckageCampaignBehavior.cs", "GetWreckageDestroyTimeOnCreation", 904);
			break;
		}
		return CampaignTime.DaysFromNow(valueInDays);
	}

	private ExplainedNumber GetWreckageInvestigationDuration(bool isInterruptedWreckage)
	{
		ExplainedNumber result;
		if (isInterruptedWreckage)
		{
			result = new ExplainedNumber((int)_requiredInvestigationDuration.ToHours, includeDescriptions: true, new TextObject("{=RJYaI2XT}Remaining Investigation Duration"));
		}
		else
		{
			int num = 0;
			int num2 = 0;
			BattleWreckage.WreckageType wreckageTypeCategory = _encounteredBattleWreckage.WreckageTypeCategory;
			switch (wreckageTypeCategory)
			{
			case BattleWreckage.WreckageType.Epic:
				num = 30;
				num2 = 40;
				break;
			case BattleWreckage.WreckageType.Normal:
				num = 20;
				num2 = 30;
				break;
			default:
				num = 10;
				num2 = 20;
				break;
			}
			int num3 = 0;
			num3 = Hero.MainHero.RandomIntWithSeed((uint)CampaignTime.Now.ToDays, num, num2);
			result = new ExplainedNumber(num3, includeDescriptions: true, new TextObject("{=8tQRrAgZ}Base Investigation Duration"));
			int num4 = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery);
			if (num4 > 0)
			{
				if (num4 > 150)
				{
					num4 = 150;
				}
				int num5 = TaleWorlds.Library.MathF.Floor((float)num3 * 0.5f);
				int num6 = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Ceiling(MBMath.Map(num4, 0f, 150f, 0f, num5)), num5);
				if (num6 > 0)
				{
					result.Add(-num6, new TextObject("{=7KbpRJ5x}Player Roguery Skill Effect"));
				}
			}
			int requiredPartySizeToInvestigateWreckageEfficiently = GetRequiredPartySizeToInvestigateWreckageEfficiently();
			int totalManCount = MobileParty.MainParty.MemberRoster.TotalManCount;
			if (totalManCount >= requiredPartySizeToInvestigateWreckageEfficiently)
			{
				int num7 = wreckageTypeCategory switch
				{
					BattleWreckage.WreckageType.Epic => 180, 
					BattleWreckage.WreckageType.Normal => 90, 
					_ => 50, 
				} - requiredPartySizeToInvestigateWreckageEfficiently;
				if (num7 > 0)
				{
					int num8 = TaleWorlds.Library.MathF.Floor((float)num3 * 0.3f);
					int num9 = TaleWorlds.Library.MathF.Min((totalManCount - requiredPartySizeToInvestigateWreckageEfficiently) * num8 / num7, num8);
					if (num9 > 0)
					{
						result.Add(-num9, new TextObject("{=VFWSFv3S}Player Party Size Effect"));
					}
				}
			}
		}
		return result;
	}

	private int GetRequiredPartySizeToInvestigateWreckageEfficiently()
	{
		if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
		{
			return 90;
		}
		if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Normal)
		{
			return 50;
		}
		return 1;
	}

	private void RemoveWreckageFromMap(BattleWreckage wreckageToRemove)
	{
		wreckageToRemove.DestroyWreckage();
	}

	private void OnHourlyTick()
	{
		for (int num = Campaign.Current.Wreckages.Count - 1; num >= 0; num--)
		{
			BattleWreckage battleWreckage = Campaign.Current.Wreckages[num];
			if (IsWreckageDestroyable(battleWreckage))
			{
				RemoveWreckageFromMap(battleWreckage);
			}
		}
		if (_encounteredBattleWreckage != null && _lastPlayerWreckageInteractionTime != CampaignTime.Zero && (CampaignTime.Now - _lastPlayerWreckageInteractionTime).ToHours > 24.0)
		{
			LeaveWreckageEncounter(WreckageLeaveReason.WreckageExpired);
		}
	}

	private bool IsWreckageDestroyable(BattleWreckage battleWreckage, bool isForCreationNewWreckage = false)
	{
		if (_encounteredBattleWreckage != battleWreckage && !battleWreckage.IsVisible)
		{
			if (!isForCreationNewWreckage)
			{
				return battleWreckage.IsWreckageDestroyable;
			}
			return true;
		}
		return false;
	}

	private void OnPartyEncounter(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (defenderParty == PartyBase.MainParty && _encounteredBattleWreckage != null)
		{
			LeaveWreckageEncounter(WreckageLeaveReason.PlayerInterrupted);
		}
	}

	private void OnAiHourlyTickEvent(MobileParty mobileParty, PartyThinkParams partyThinkParams)
	{
		if (Campaign.Current.Wreckages.Count != 0 && TryGetTargetableWreckageForParty(mobileParty, out var targetWreckage))
		{
			float item = (mobileParty.IsCurrentlyAtSea ? 6f : 3f);
			MobileParty.NavigationType navigationType = ((!mobileParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
			CampaignVec2 position = NavigationHelper.FindPointAroundPosition(targetWreckage.Position, targetWreckage.Position.IsOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval, GetWreckageInvestigationRadiusForAiParties(targetWreckage));
			AIBehaviorData item2 = new AIBehaviorData(position, AiBehavior.GoToPoint, navigationType, willGatherArmy: false, isFromPort: false, isTargetingPort: false);
			partyThinkParams.AddBehaviorScore((item2, item));
		}
	}

	private bool CanAiPartyTargetWreckage(MobileParty mobileParty)
	{
		if (mobileParty.IsBandit && !mobileParty.IsBanditBossParty && mobileParty.CurrentSettlement == null && !mobileParty.IsCurrentlyUsedByAQuest && mobileParty.MapEvent == null)
		{
			Clan actualClan = mobileParty.ActualClan;
			if (actualClan == null || actualClan.IsBanditFaction)
			{
				if (!mobileParty.IsCurrentlyAtSea && !FactionHelper.IsLooterFaction(mobileParty.ActualClan))
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private float GetWreckageInvestigationRadiusForAiParties(BattleWreckage battleWreckage)
	{
		float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		if (!battleWreckage.Position.IsOnLand)
		{
			return getEncounterJoiningRadius * 0.5f;
		}
		return getEncounterJoiningRadius;
	}

	private bool TryGetTargetableWreckageForParty(MobileParty mobileParty, out BattleWreckage targetWreckage)
	{
		targetWreckage = null;
		if (!CanAiPartyTargetWreckage(mobileParty))
		{
			return false;
		}
		BattleWreckage battleWreckage = null;
		float num = 20f;
		foreach (BattleWreckage wreckage in Campaign.Current.Wreckages)
		{
			if (!wreckage.IsInvestigated && wreckage.Position.IsOnLand == mobileParty.Position.IsOnLand)
			{
				float num2 = wreckage.Position.Distance(mobileParty.Position);
				if (num2 < num)
				{
					battleWreckage = wreckage;
					num = num2;
				}
			}
		}
		if (battleWreckage != null)
		{
			int wreckageTargetingAiPartiesLimit = GetWreckageTargetingAiPartiesLimit(battleWreckage);
			float wreckageInvestigationRadiusForAiParties = GetWreckageInvestigationRadiusForAiParties(battleWreckage);
			LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(battleWreckage.Position.ToVec2(), wreckageInvestigationRadiusForAiParties);
			MobileParty mobileParty2 = MobileParty.FindNextLocatable(ref data);
			int num3 = 0;
			while (mobileParty2 != null)
			{
				if (mobileParty2 != mobileParty && CanAiPartyTargetWreckage(mobileParty2))
				{
					num3++;
					if (num3 == wreckageTargetingAiPartiesLimit)
					{
						return false;
					}
				}
				mobileParty2 = MobileParty.FindNextLocatable(ref data);
			}
			targetWreckage = battleWreckage;
			return true;
		}
		return false;
	}

	private int GetWreckageTargetingAiPartiesLimit(BattleWreckage wreckage)
	{
		if (wreckage.Position.IsOnLand)
		{
			if (wreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
			{
				return 6;
			}
			if (wreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Normal)
			{
				return 4;
			}
			return 2;
		}
		if (wreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic)
		{
			return 4;
		}
		if (wreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Normal)
		{
			return 3;
		}
		return 2;
	}

	private void SelectWreckageConsequences()
	{
		bool flag = false;
		if (!string.IsNullOrEmpty(_currentOverriddenConsequenceId))
		{
			flag = true;
			if (_isOverriddenConsequenceAccepted)
			{
				_selectedConsequences.Add(FindConsequenceWithId(_currentOverriddenConsequenceId));
			}
		}
		_wreckageConsequences.Shuffle();
		foreach (BattleWreckageConsequence wreckageConsequence in _wreckageConsequences)
		{
			if (!(wreckageConsequence.Chance <= 0f) && !(wreckageConsequence.CanOverride && flag) && (wreckageConsequence.Condition == null || wreckageConsequence.Condition(_encounteredBattleWreckage)) && MBRandom.RandomFloat < wreckageConsequence.Chance)
			{
				if (wreckageConsequence.CanOverride)
				{
					_selectedConsequences.Clear();
					_currentOverriddenConsequenceId = wreckageConsequence.StringId;
					wreckageConsequence.Consequence();
					return;
				}
				_selectedConsequences.Add(wreckageConsequence);
			}
		}
		if (_selectedConsequences.Count == 0)
		{
			_selectedConsequences.Add(FindConsequenceWithId("NothingFoundConsequence"));
		}
		InvokeSelectedConsequences();
		GameMenu.SwitchToMenu("battle_wreckage_results_connection_menu");
	}

	private void InvokeSelectedConsequences()
	{
		for (int i = 0; i < _selectedConsequences.Count; i++)
		{
			_selectedConsequences[i].Consequence();
		}
	}

	private void ApplyWreckageInvestigationResults()
	{
		if (_lootedTroops.Count > 0)
		{
			PartyScreenHelper.OpenScreenAsReceiveTroops(_lootedTroops, new TextObject("{=Ee0WbUjr}Recovered Troops"), delegate
			{
				_lootedTroops.Clear();
			});
		}
		else if (_lootedItems.Count > 0)
		{
			InventoryScreenHelper.OpenScreenAsReceiveItems(_lootedItems, new TextObject("{=ObwRgNuJ}Salvaged Goods"), delegate
			{
				_lootedItems.Clear();
			});
		}
		else if (_consequenceExplanations.Count > 0)
		{
			if (_lootedGoldAmount > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, _lootedGoldAmount, disableNotification: true);
				TextObject textObject = GameTexts.FindText("str_you_received_gold_with_icon");
				textObject.SetTextVariable("GOLD_AMOUNT", TaleWorlds.Library.MathF.Abs(_lootedGoldAmount));
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), string.Empty));
				_lootedGoldAmount = 0;
			}
			GameMenu.SwitchToMenu("battle_wreckage_results_menu");
		}
		else
		{
			Debug.FailedAssert("Explanation.Count == 0 case should not be possible, check this case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\BattleWreckageCampaignBehavior.cs", "ApplyWreckageInvestigationResults", 1377);
		}
	}

	private bool RecoverTroopsCondition(BattleWreckage wreckage)
	{
		int recoverTroopsTierLimit = GetRecoverTroopsTierLimit();
		foreach (TroopRosterElement item in wreckage.GetTotalWoundedInBattle())
		{
			if (!item.Character.IsHero && item.Character.Tier <= recoverTroopsTierLimit)
			{
				return true;
			}
		}
		foreach (TroopRosterElement item2 in wreckage.GetTotalDiedInBattle())
		{
			if (!item2.Character.IsHero && item2.Character.Tier <= recoverTroopsTierLimit)
			{
				return true;
			}
		}
		return false;
	}

	private void RecoverTroopsConsequence()
	{
		if (_currentOverriddenConsequenceId == "RecoverTroopsConsequence")
		{
			if (!_isOverriddenConsequenceAccepted)
			{
				CalculateAndShowRecoverTroopsDecision();
			}
			else
			{
				ApplyRecoverTroopsReward();
			}
		}
	}

	private void ApplyRecoverTroopsReward()
	{
		List<CharacterObject> list = new List<CharacterObject>();
		int recoverTroopsTierLimit = GetRecoverTroopsTierLimit();
		foreach (TroopRosterElement item in _encounteredBattleWreckage.GetTotalWoundedInBattle())
		{
			if (!item.Character.IsHero && item.Character.Tier <= recoverTroopsTierLimit)
			{
				list.Add(item.Character);
			}
		}
		foreach (TroopRosterElement item2 in _encounteredBattleWreckage.GetTotalDiedInBattle())
		{
			if (!item2.Character.IsHero && !list.Contains(item2.Character) && item2.Character.Tier <= recoverTroopsTierLimit)
			{
				list.Add(item2.Character);
			}
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		int effectiveTroopCountToRecover = GetEffectiveTroopCountToRecover(_recoverTroopCountsToAdd);
		for (int i = 0; i < effectiveTroopCountToRecover; i++)
		{
			CharacterObject randomElement = list.GetRandomElement();
			troopRoster.AddToCounts(randomElement, 1, insertAtFront: false, 1);
			MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Medicine).AddSkillXp(DefaultSkills.Medicine, randomElement.Tier * 10);
		}
		AddLootedTroop(troopRoster);
		AddConsequenceExplanation(new TextObject("{=c7tVGjOl}some wounded troops"));
	}

	private int GetRecoverTroopsTierLimit()
	{
		return GetEffectiveWreckageTypeForConsequence() switch
		{
			BattleWreckage.WreckageType.Epic => EpicWreckageRecoverTroopsTierLimit, 
			BattleWreckage.WreckageType.Normal => 3, 
			_ => 2, 
		};
	}

	private void CalculateAndShowRecoverTroopsDecision()
	{
		int minValue;
		int maxValue;
		switch (GetEffectiveWreckageTypeForConsequence())
		{
		case BattleWreckage.WreckageType.Epic:
			minValue = 10;
			maxValue = 15;
			break;
		case BattleWreckage.WreckageType.Normal:
			minValue = 5;
			maxValue = 10;
			break;
		default:
			minValue = 2;
			maxValue = 5;
			break;
		}
		int num = MBRandom.RandomInt(minValue, maxValue);
		int effectiveTroopCountToRecover = GetEffectiveTroopCountToRecover(num);
		_recoverTroopCountsToAdd = num;
		_requiredInvestigationDuration = CampaignTime.Hours(Math.Max(2, TaleWorlds.Library.MathF.Round((float)effectiveTroopCountToRecover * 1f)));
		GameMenu.SwitchToMenu("battle_wreckage_recover_troops_decision");
	}

	private int GetEffectiveTroopCountToRecover(int totalRecoverableTroopCount)
	{
		int skillValue = MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Medicine).GetSkillValue(DefaultSkills.Medicine);
		if (skillValue >= 150)
		{
			return totalRecoverableTroopCount;
		}
		int num = TaleWorlds.Library.MathF.Round((float)totalRecoverableTroopCount * 0.4f);
		int num2 = TaleWorlds.Library.MathF.Round(MBMath.Map(skillValue + 1, 0f, 150f, 0f, totalRecoverableTroopCount - num));
		return num + num2;
	}

	private void game_menu_recover_troops_decision_init(MenuCallbackArgs args)
	{
		int effectiveTroopCountToRecover = GetEffectiveTroopCountToRecover(_recoverTroopCountsToAdd);
		Hero heroWithHighestSkill = MobilePartyHelper.GetHeroWithHighestSkill(MobileParty.MainParty, DefaultSkills.Medicine);
		int skillValue = heroWithHighestSkill.GetSkillValue(DefaultSkills.Medicine);
		TextObject textObject;
		if (effectiveTroopCountToRecover == _recoverTroopCountsToAdd)
		{
			textObject = ((heroWithHighestSkill != Hero.MainHero) ? new TextObject("{=P8FS1iNL}You found {TOTAL_WOUNDED_COUNT} wounded survivors of the battle. As the party member with the highest skill in medicine, {HIGHEST_MEDICINE_SKILLED_MEMBER} believes that all of them can be saved. Treating them will take {RECOVER_DURATION_HOURS} more hours. Or, you can focus on gathering loot.") : new TextObject("{=7vU9GKFS}You found {TOTAL_WOUNDED_COUNT} wounded survivors of the battle. As the party member with the highest skill in medicine, {HIGHEST_MEDICINE_SKILLED_MEMBER} believe that all of them can be saved. Treating them will take {RECOVER_DURATION_HOURS} more hours. Or, you can focus on gathering loot."));
		}
		else
		{
			textObject = ((heroWithHighestSkill != Hero.MainHero) ? new TextObject("{=dfr903U9}You found {TOTAL_WOUNDED_COUNT} wounded survivors of the battle. As the party member with the highest skill in medicine, {HIGHEST_MEDICINE_SKILLED_MEMBER} believes that only {EFFECTIVE_COUNT} can be saved. Treating them will take {RECOVER_DURATION_HOURS} more hours. Or, you can focus on gathering loot.") : new TextObject("{=CK7Li6bL}You found {TOTAL_WOUNDED_COUNT} wounded survivors of the battle. As the party member with the highest skill in medicine, {HIGHEST_MEDICINE_SKILLED_MEMBER} believe that only {EFFECTIVE_COUNT} can be saved. Treating them will take {RECOVER_DURATION_HOURS} more hours. Or, you can focus on gathering loot."));
			textObject.SetTextVariable("EFFECTIVE_COUNT", effectiveTroopCountToRecover);
		}
		textObject.SetTextVariable("TOTAL_WOUNDED_COUNT", _recoverTroopCountsToAdd);
		textObject.SetTextVariable("HIGHEST_MEDICINE_SKILLED_MEMBER", (heroWithHighestSkill != null && heroWithHighestSkill != Hero.MainHero) ? heroWithHighestSkill.Name : GameTexts.FindText("str_you"));
		textObject.SetTextVariable("HIGHEST_MEDICINE_SKILL", skillValue);
		textObject.SetTextVariable("RECOVER_DURATION_HOURS", (int)_requiredInvestigationDuration.ToHours);
		GameTexts.SetVariable("RECOVER_TROOPS_TEXT", textObject);
		SetWreckageMenuBackgrounds(args);
	}

	private void game_menu_recover_troops_accept_consequence(MenuCallbackArgs args)
	{
		_playerInvestigationStartTime = CampaignTime.Now;
		_isOverriddenConsequenceAccepted = true;
		GameMenu.SwitchToMenu("battle_wreckage_investigate_wait_menu");
	}

	private void game_menu_recover_troops_decline_consequence(MenuCallbackArgs args)
	{
		_isOverriddenConsequenceAccepted = false;
		_recoverTroopCountsToAdd = 0;
		SelectWreckageConsequences();
	}

	private bool recover_troops_leave_option_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private bool recover_troops_accept_option_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void GainGoldConsequence()
	{
		int num = 0;
		num = GetEffectiveWreckageTypeForConsequence() switch
		{
			BattleWreckage.WreckageType.Epic => (!_encounteredBattleWreckage.Position.IsOnLand) ? MBRandom.RandomInt(7000, 10000) : MBRandom.RandomInt(5000, 8000), 
			BattleWreckage.WreckageType.Normal => (!_encounteredBattleWreckage.Position.IsOnLand) ? MBRandom.RandomInt(2000, 4500) : MBRandom.RandomInt(1500, 3000), 
			_ => (!_encounteredBattleWreckage.Position.IsOnLand) ? MBRandom.RandomInt(700, 1500) : MBRandom.RandomInt(500, 1000), 
		} + 1;
		TextObject textObject = new TextObject("{=!}{GOLD_GAIN}{GOLD_ICON}");
		textObject.SetTextVariable("GOLD_GAIN", num);
		textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		AddLootedGold(num);
		AddConsequenceExplanation(textObject);
		if (!(MBRandom.RandomFloat <= 0.5f))
		{
			return;
		}
		Hideout hideout = null;
		float num2 = float.MaxValue;
		foreach (Hideout item in Hideout.All)
		{
			if (IsHideoutSuitableToDiscoverFromWreckage(item))
			{
				float estimatedLandRatio;
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, item.Settlement, isTargetingPort: false, MobileParty.NavigationType.All, out estimatedLandRatio);
				if (distance < num2)
				{
					num2 = distance;
					hideout = item;
				}
			}
		}
		if (hideout != null)
		{
			hideout.Settlement.IsVisible = true;
			CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, hideout.Settlement.Party);
			Campaign.Current.VisualTrackerManager.RegisterObject(hideout.Settlement);
			AddConsequenceExplanation(new TextObject("{=JOquYB4T}a crude map to a nearby hideout"));
		}
	}

	private bool IsHideoutSuitableToDiscoverFromWreckage(Hideout hideoutToConsider)
	{
		if (hideoutToConsider.IsInfested && !hideoutToConsider.Settlement.IsVisible)
		{
			return hideoutToConsider.Settlement.Parties.Count > 0;
		}
		return false;
	}

	private void GainTradeGoodsConsequence()
	{
		GetTradeGoodTargetValueAndRogueryXp(out var rogueryXp, out var targetTradeGoodValue);
		int num = TaleWorlds.Library.MathF.Ceiling((float)targetTradeGoodValue * 1.2f);
		int num2 = (int)((float)num * 0.5f);
		int num3 = num - num2;
		num3 = TaleWorlds.Library.MathF.Round(MBRandom.RandomFloatRanged((float)num3 * 0.5f, num3));
		ItemRoster itemRoster = new ItemRoster();
		Dictionary<ItemObject, int> dictionary = new Dictionary<ItemObject, int>(64);
		Town closestTown = Town.AllTowns.MinBy((Town x) => x.Settlement.Position.Distance(MobileParty.MainParty.Position));
		int totalLootedValueFromCasualties = 0;
		ProcessCasualtyLootSource(_encounteredBattleWreckage.GetTotalDiedInBattle(), closestTown, dictionary, itemRoster, num2, ref totalLootedValueFromCasualties);
		ProcessCasualtyLootSource(_encounteredBattleWreckage.GetTotalWoundedInBattle(), closestTown, dictionary, itemRoster, num2, ref totalLootedValueFromCasualties);
		List<ItemObject> list = new List<ItemObject>();
		HashSet<ItemObject> hashSet = new HashSet<ItemObject>();
		SmithingModel smithingModel = Campaign.Current.Models.SmithingModel;
		for (CraftingMaterials craftingMaterials = CraftingMaterials.IronOre; craftingMaterials < CraftingMaterials.NumCraftingMats; craftingMaterials++)
		{
			ItemObject craftingMaterialItem = smithingModel.GetCraftingMaterialItem(craftingMaterials);
			if (craftingMaterialItem != null)
			{
				hashSet.Add(craftingMaterialItem);
			}
		}
		foreach (ItemObject allTradeGood in Items.AllTradeGoods)
		{
			if (!allTradeGood.NotMerchandise && !allTradeGood.IsBannerItem && allTradeGood.IsTradeGood && allTradeGood.ItemType != ItemObject.ItemTypeEnum.Invalid && !allTradeGood.IsAnimal && !allTradeGood.IsMountable && allTradeGood != DefaultItems.Trash && allTradeGood.Culture == null && !hashSet.Contains(allTradeGood))
			{
				list.Add(allTradeGood);
				GetOrCalculateAverageItemValue(allTradeGood, closestTown, dictionary);
			}
		}
		if (list.Count > 0)
		{
			int num4 = 0;
			int num5 = 0;
			do
			{
				ItemObject randomElement = list.GetRandomElement();
				int num6 = dictionary[randomElement];
				if (randomElement.Value > 0 && num6 + num4 <= num3)
				{
					itemRoster.AddToCounts(randomElement, 1);
					num4 += num6;
				}
				else
				{
					num5++;
				}
			}
			while (num5 < 5);
		}
		AddLootedItem(itemRoster);
		AddConsequenceExplanation(new TextObject("{=PsIAb1eF}some valuables"));
		Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryXp);
	}

	private void GetTradeGoodTargetValueAndRogueryXp(out int rogueryXp, out int targetTradeGoodValue)
	{
		BattleWreckage.WreckageType effectiveWreckageTypeForConsequence = GetEffectiveWreckageTypeForConsequence();
		bool isOnLand = _encounteredBattleWreckage.Position.IsOnLand;
		int num;
		int num2;
		switch (effectiveWreckageTypeForConsequence)
		{
		case BattleWreckage.WreckageType.Epic:
			num = 3;
			num2 = (isOnLand ? 7 : 8);
			break;
		case BattleWreckage.WreckageType.Normal:
			num = 2;
			num2 = (isOnLand ? 3 : 5);
			break;
		default:
			num = 1;
			num2 = (isOnLand ? 1 : 2);
			break;
		}
		rogueryXp = 75 * num * (isOnLand ? 1 : 2);
		targetTradeGoodValue = 1000 * num2;
	}

	private void ProcessCasualtyLootSource(MBList<TroopRosterElement> casualties, Town closestTown, Dictionary<ItemObject, int> itemValueCache, ItemRoster lootedItems, int targetTotalCasualtyValue, ref int totalLootedValueFromCasualties)
	{
		BattleRewardModel battleRewardModel = Campaign.Current.Models.BattleRewardModel;
		int num = casualties.Count - 1;
		while (num >= 0 && totalLootedValueFromCasualties < targetTotalCasualtyValue)
		{
			if (MBRandom.RandomFloat <= 0.5f)
			{
				CharacterObject character = casualties[num].Character;
				float expectedLootedItemValueFromCasualty = battleRewardModel.GetExpectedLootedItemValueFromCasualty(Hero.MainHero, character);
				EquipmentElement lootedItemFromTroop = battleRewardModel.GetLootedItemFromTroop(character, expectedLootedItemValueFromCasualty);
				if (lootedItemFromTroop.Item != null && !lootedItemFromTroop.Item.IsAnimal && !lootedItemFromTroop.Item.IsMountable)
				{
					int orCalculateAverageItemValue = GetOrCalculateAverageItemValue(lootedItemFromTroop.Item, closestTown, itemValueCache);
					if (totalLootedValueFromCasualties + orCalculateAverageItemValue <= targetTotalCasualtyValue)
					{
						lootedItems.AddToCounts(lootedItemFromTroop.Item, 1);
						totalLootedValueFromCasualties += orCalculateAverageItemValue;
					}
				}
			}
			num--;
		}
	}

	private int GetOrCalculateAverageItemValue(ItemObject item, Town closestTown, Dictionary<ItemObject, int> itemValues)
	{
		if (!itemValues.TryGetValue(item, out var value))
		{
			value = closestTown.GetItemPrice(item, MobileParty.MainParty, isSelling: true);
			itemValues.Add(item, value);
		}
		return value;
	}

	private void NothingFoundConsequence()
	{
		AddConsequenceExplanation(new TextObject("{=raa8Qxeh}nothing of value"));
	}

	private BattleWreckage.WreckageType GetEffectiveWreckageTypeForConsequence()
	{
		int requiredPartySizeToInvestigateWreckageEfficiently = GetRequiredPartySizeToInvestigateWreckageEfficiently();
		if (MobileParty.MainParty.MemberRoster.TotalManCount >= requiredPartySizeToInvestigateWreckageEfficiently)
		{
			return _encounteredBattleWreckage.WreckageTypeCategory;
		}
		if (_encounteredBattleWreckage.WreckageTypeCategory == BattleWreckage.WreckageType.Epic && MobileParty.MainParty.MemberRoster.TotalManCount >= 50)
		{
			return BattleWreckage.WreckageType.Normal;
		}
		return BattleWreckage.WreckageType.Small;
	}

	private BattleWreckageConsequence FindConsequenceWithId(string id)
	{
		return _wreckageConsequences.FirstOrDefaultQ((BattleWreckageConsequence x) => x.StringId == id);
	}

	private void AddLootedItem(ItemRoster lootedItems)
	{
		_lootedItems.Add(lootedItems);
	}

	private void AddLootedTroop(TroopRoster troops)
	{
		_lootedTroops.Add(troops);
	}

	private void AddLootedGold(int goldAmount)
	{
		_lootedGoldAmount += goldAmount;
	}

	private void AddConsequenceExplanation(TextObject explanation)
	{
		_consequenceExplanations.Add(explanation);
	}

	private void game_menu_battle_remains_init(MenuCallbackArgs args)
	{
		BattleWreckage encounteredBattleWreckage = _encounteredBattleWreckage;
		List<TextObject> list = new List<TextObject>();
		TextObject textObject = new TextObject("{=18KcBMFI}Date: {DATE}");
		textObject.SetTextVariable("DATE", encounteredBattleWreckage.BattleStartTime.ToString());
		list.Add(textObject);
		TextObject textObject2 = new TextObject("{=UQGaJM5S}Victor: {PARTY} ({FACTION})");
		textObject2.SetTextVariable("PARTY", encounteredBattleWreckage.GetWinnerPartyName());
		textObject2.SetTextVariable("FACTION", encounteredBattleWreckage.GetWinnerFaction().Name);
		list.Add(textObject2);
		TextObject textObject3 = new TextObject("{=pfVrdaAf}Defeated: {PARTY} ({FACTION})");
		textObject3.SetTextVariable("PARTY", encounteredBattleWreckage.GetDefeatedPartyName());
		textObject3.SetTextVariable("FACTION", encounteredBattleWreckage.GetDefeatedFaction().Name);
		list.Add(textObject3);
		TextObject textObject4 = new TextObject("{=TZTEWlgx}Battle Size: {COUNT} Troops");
		textObject4.SetTextVariable("COUNT", GetBattleRemainsWinnerTroopCount(encounteredBattleWreckage) + GetBattleRemainsDefeatedTroopCount(encounteredBattleWreckage));
		list.Add(textObject4);
		TextObject textObject5 = new TextObject("{=WSs33gt0}{FACTION} deployed {COUNT} troops");
		textObject5.SetTextVariable("FACTION", encounteredBattleWreckage.GetWinnerFaction().Name);
		textObject5.SetTextVariable("COUNT", GetBattleRemainsWinnerTroopCount(encounteredBattleWreckage));
		list.Add(textObject5);
		TextObject textObject6 = new TextObject("{=WSs33gt0}{FACTION} deployed {COUNT} troops");
		textObject6.SetTextVariable("FACTION", encounteredBattleWreckage.GetDefeatedFaction().Name);
		textObject6.SetTextVariable("COUNT", GetBattleRemainsDefeatedTroopCount(encounteredBattleWreckage));
		list.Add(textObject6);
		TextObject textObject7 = new TextObject("{=rjrVuhbW}{newline}Casualties:");
		textObject7.SetTextVariable("newline", "\n");
		list.Add(textObject7);
		TextObject textObject8 = new TextObject("{=!}{FACTION}: {COUNT}");
		textObject8.SetTextVariable("FACTION", encounteredBattleWreckage.GetWinnerFaction().Name);
		textObject8.SetTextVariable("COUNT", GetBattleRemainsWinnerCasualties(encounteredBattleWreckage));
		list.Add(textObject8);
		TextObject textObject9 = new TextObject("{=!}{FACTION}: {COUNT}");
		textObject9.SetTextVariable("FACTION", encounteredBattleWreckage.GetDefeatedFaction().Name);
		textObject9.SetTextVariable("COUNT", GetBattleRemainsDefeatedCasualties(encounteredBattleWreckage));
		list.Add(textObject9);
		List<TextObject> battleRemainsFallenHeroLines = GetBattleRemainsFallenHeroLines(encounteredBattleWreckage);
		if (battleRemainsFallenHeroLines.Count > 0)
		{
			list.Add(new TextObject("{=h70VQ44K}Fallen Heroes:"));
			list.AddRange(battleRemainsFallenHeroLines);
		}
		TextObject content = GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}"));
		GameTexts.SetVariable("BATTLE_REMAINS_TEXT", content);
		args.MenuContext.SetPanelSound("event:/ui/wreckage/wreckage_panel");
		SetWreckageMenuBackgrounds(args);
	}

	private bool battle_remains_leave_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void battle_remains_leave_consequence(MenuCallbackArgs args)
	{
		_encounteredBattleWreckage = null;
		if (PlayerEncounter.Current == null && Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
	}

	private int GetBattleRemainsWinnerTroopCount(BattleWreckage wreckage)
	{
		if (wreckage.WinnerSide != BattleSideEnum.Attacker)
		{
			return wreckage.DefenderHealthyTroopCountAtStart;
		}
		return wreckage.AttackerHealthyTroopCountAtStart;
	}

	private int GetBattleRemainsDefeatedTroopCount(BattleWreckage wreckage)
	{
		if (wreckage.WinnerSide != BattleSideEnum.Attacker)
		{
			return wreckage.AttackerHealthyTroopCountAtStart;
		}
		return wreckage.DefenderHealthyTroopCountAtStart;
	}

	private int GetBattleRemainsWinnerCasualties(BattleWreckage wreckage)
	{
		if (wreckage.WinnerSide == BattleSideEnum.Attacker)
		{
			return wreckage.AttackerWoundedInBattle.TotalRegulars + wreckage.AttackerDiedInBattle.TotalRegulars;
		}
		return wreckage.DefenderWoundedInBattle.TotalRegulars + wreckage.DefenderDiedInBattle.TotalRegulars;
	}

	private int GetBattleRemainsDefeatedCasualties(BattleWreckage wreckage)
	{
		if (wreckage.WinnerSide == BattleSideEnum.Attacker)
		{
			return wreckage.DefenderWoundedInBattle.TotalRegulars + wreckage.DefenderDiedInBattle.TotalRegulars;
		}
		return wreckage.AttackerWoundedInBattle.TotalRegulars + wreckage.AttackerDiedInBattle.TotalRegulars;
	}

	private List<TextObject> GetBattleRemainsFallenHeroLines(BattleWreckage wreckage)
	{
		List<TextObject> list = new List<TextObject>();
		TroopRoster[] array = new TroopRoster[2] { wreckage.AttackerDiedInBattle, wreckage.DefenderDiedInBattle };
		for (int i = 0; i < array.Length; i++)
		{
			foreach (TroopRosterElement item in array[i].GetTroopRoster())
			{
				if (item.Character.IsHero)
				{
					TextObject textObject = new TextObject("{=PSYezDpZ}- {HERO_NAME} (Killed)");
					textObject.SetTextVariable("HERO_NAME", item.Character.HeroObject.Name);
					list.Add(textObject);
				}
			}
		}
		return list;
	}
}
