using Helpers;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SiegeAmbushCampaignBehavior : CampaignBehaviorBase
{
	private const int SiegeAmbushCooldownPeriodAsHours = 24;

	private CampaignTime _lastAmbushTime;

	public override void RegisterEvents()
	{
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEventEnded);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		_lastAmbushTime = CampaignTime.Never;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastAmbushTime", ref _lastAmbushTime);
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (mapEvent.IsSiegeAmbush)
		{
			_lastAmbushTime = CampaignTime.Now;
		}
	}

	private void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
		if (siegeEvent == PlayerSiege.PlayerSiegeEvent)
		{
			_lastAmbushTime = CampaignTime.Never;
		}
	}

	private void HourlyTick()
	{
		if (PlayerSiege.PlayerSiegeEvent != null && _lastAmbushTime == CampaignTime.Never && PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete)
		{
			_lastAmbushTime = CampaignTime.Now;
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		MapEvent battle = PlayerEncounter.Battle;
		if (battle != null && battle.IsSiegeAmbush)
		{
			PlayerEncounter.Current.FinalizeBattle();
			PlayerEncounter.Current.SetIsSallyOutAmbush(value: false);
		}
	}

	private bool CanMainHeroAmbush(out TextObject reason)
	{
		if (_lastAmbushTime.ElapsedHoursUntilNow < 24f)
		{
			reason = new TextObject("{=lCYPxuWN}The enemy is alert, you cannot ambush right now.");
			return false;
		}
		if (Hero.MainHero.IsWounded)
		{
			reason = new TextObject("{=pQaQW1As}You cannot ambush right now due to your wounds.");
			return false;
		}
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		if (playerSiegeEvent.BesiegerCamp.LeaderParty.MapEvent != null && MobileParty.MainParty.MapEvent == null)
		{
			reason = new TextObject("{=GAh1gNYn}Enemies are already engaged in battle.");
			return false;
		}
		if (playerSiegeEvent.GetPreparedSiegeEnginesAsDictionary(playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker)).Count <= 0)
		{
			reason = new TextObject("{=f4g7r0xF}The enemy does not have any vulnerabilities.");
			return false;
		}
		if (playerSiegeEvent.BesiegedSettlement.SettlementWallSectionHitPointsRatioList.AnyQ((float r) => r <= 0f))
		{
			reason = new TextObject("{=Nzt8Xkro}You cannot ambush because the settlement walls are breached.");
			return false;
		}
		reason = TextObject.GetEmpty();
		return true;
	}

	private void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddGameMenuOption("menu_siege_strategies", "menu_siege_strategies_ambush", "{=LEKzuGzi}Ambush", menu_siege_strategies_ambush_condition, menu_siege_strategies_ambush_on_consequence);
	}

	private bool menu_siege_strategies_ambush_condition(MenuCallbackArgs args)
	{
		if (PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Defender)
		{
			return false;
		}
		args.optionLeaveType = GameMenuOption.LeaveType.SiegeAmbush;
		if (!CanMainHeroAmbush(out var reason))
		{
			args.IsEnabled = false;
			args.Tooltip = reason;
		}
		return true;
	}

	private void menu_siege_strategies_ambush_on_consequence(MenuCallbackArgs args)
	{
		_lastAmbushTime = CampaignTime.Now;
		if (PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.SiegeEvent != null && !PlayerEncounter.EncounterSettlement.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
		{
			PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party, forcePlayerOutFromSettlement: false);
		}
		PlayerEncounter.Current.SetIsSallyOutAmbush(value: true);
		PlayerEncounter.StartBattle();
		MenuHelper.EncounterAttackConsequence(args);
	}
}
