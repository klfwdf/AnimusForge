using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.ExpeditionParade;

public sealed class ExpeditionParadeCampaignBehavior : CampaignBehaviorBase
{
	internal const int DefaultDisplayLimit = 48;

	private const string OptionText = "出征阅兵：勘测路线（阶段 0）";

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		// Runtime-only feature: no save data and no persistent campaign mutation.
	}

	private static void OnSessionLaunched(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}

		starter.AddGameMenuOption("town", "animusforge_expedition_parade_town", OptionText, ParadeCondition, ParadeConsequence, isLeave: false, -1);
		starter.AddGameMenuOption("castle", "animusforge_expedition_parade_castle", OptionText, ParadeCondition, ParadeConsequence, isLeave: false, -1);
		starter.AddGameMenuOption("village", "animusforge_expedition_parade_village", OptionText, ParadeCondition, ParadeConsequence, isLeave: false, -1);
		Logger.Log("ExpeditionParade", "Registered stage-0 route probe menu entries for town, castle, and village.");
	}

	private static bool ParadeCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		Settlement settlement = ResolveCurrentSettlement();
		bool enabled = ParadeEligibilityService.CanStartParade(settlement, Hero.MainHero, out TextObject reason);
		args.IsEnabled = enabled;
		args.Tooltip = reason;
		return settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
	}

	private static void ParadeConsequence(MenuCallbackArgs args)
	{
		Settlement settlement = ResolveCurrentSettlement();
		if (!ParadeEligibilityService.CanStartParade(settlement, Hero.MainHero, out TextObject reason))
		{
			InformationManager.DisplayMessage(new InformationMessage(reason.ToString()));
			return;
		}

		Location location = ResolveParadeLocation(settlement);
		ParadeRosterSnapshot snapshot = ParadeRosterSnapshot.Capture(MobileParty.MainParty.MemberRoster, DefaultDisplayLimit);
		ExpeditionParadeSession session = new(settlement.StringId, location.StringId, snapshot);
		ExpeditionParadeRuntime.Queue(session);
		try
		{
			IMission openedMission = PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(location, null, null, null);
			if (openedMission == null)
			{
				ExpeditionParadeRuntime.Clear("open_mission_returned_null");
				InformationManager.DisplayMessage(new InformationMessage("出征阅兵场景尚未就绪，请重试。"));
				return;
			}
			Logger.Log("ExpeditionParade", "Queued stage-0 scene probe. id=" + session.SessionId
				+ ", settlement=" + session.SettlementId
				+ ", location=" + session.LocationId
				+ ", roster=" + snapshot.BuildDiagnosticSummary());
		}
		catch (Exception ex)
		{
			ExpeditionParadeRuntime.Clear("open_mission_exception");
			Logger.Log("ExpeditionParade", "Open route-probe mission failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("进入出征阅兵勘测场景失败。"));
		}
	}

	private static void OnMissionStarted(IMission mission)
	{
		if (mission is not Mission concreteMission || ExpeditionParadeRuntime.Pending == null)
		{
			return;
		}

		Settlement settlement = ResolveCurrentSettlement();
		if (!ExpeditionParadeRuntime.TryActivate(concreteMission, settlement?.StringId, out ExpeditionParadeSession session))
		{
			return;
		}
		if (concreteMission.GetMissionBehavior<ExpeditionParadeMissionBehavior>() == null)
		{
			concreteMission.AddMissionBehavior(new ExpeditionParadeMissionBehavior(session));
			Logger.Log("ExpeditionParade", "Attached stage-0 scene probe. id=" + session.SessionId);
		}
	}

	private static void OnMissionEnded(IMission mission)
	{
		ExpeditionParadeSession active = ExpeditionParadeRuntime.Active;
		if (active != null)
		{
			ExpeditionParadeRuntime.Complete(active, "campaign_mission_ended");
		}
	}

	internal static Settlement ResolveCurrentSettlement()
	{
		return Settlement.CurrentSettlement
			?? PlayerEncounter.LocationEncounter?.Settlement
			?? MobileParty.MainParty?.CurrentSettlement;
	}

	internal static Location ResolveParadeLocation(Settlement settlement)
	{
		LocationComplex complex = settlement?.LocationComplex ?? LocationComplex.Current;
		if (complex == null)
		{
			return null;
		}
		return settlement?.IsVillage == true
			? complex.GetLocationWithId("village_center")
			: complex.GetLocationWithId("center");
	}
}
