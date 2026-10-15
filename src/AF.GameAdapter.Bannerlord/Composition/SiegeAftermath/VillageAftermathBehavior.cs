using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Thin Bannerlord adapter for GCCZ village administration.
/// Authority, tags, values, wording and culture modes live in the standalone GCCZ core.
/// </summary>
public sealed partial class VillageAftermathBehavior : CampaignBehaviorBase
{
	private const uint SuccessColor = 0xFFB6F7A8u;
	private const uint WarningColor = 0xFFFFD27Fu;
	private const uint DestructiveColor = 0xFFFF7777u;

	private static Mission _activeMission;
	private static string _activeVillageId = "";
	private static string _pendingIncidentVillageId = "";
	private static DateTime _pendingIncidentQueuedUtc = DateTime.MinValue;
	private static TroopRoster _pendingVillageSelectedRoster;
	private static VillageAftermathAuthorityKind _activeAuthorityKind;
	private static bool _cultureInquiryPending;
	private static readonly HashSet<VillageAftermathActionKind> AppliedActions = new HashSet<VillageAftermathActionKind>();

	private Dictionary<string, string> _gradualCultureTargetByVillageId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, int> _gradualCultureFinishDayByVillageId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public override void RegisterEvents()
	{
		RegisterVillageEconomyEvents();
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, RegisterVillageGrowthHooks);
		RegisterNativeDispositionEvents();
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		SyncVillageEconomy(dataStore);
		SyncGovernance(dataStore);
		SyncNativeDispositionRequest(dataStore);
		dataStore.SyncData("_gcczVillageGradualCultureTargets_v1", ref _gradualCultureTargetByVillageId);
		dataStore.SyncData("_gcczVillageGradualCultureFinishDays_v1", ref _gradualCultureFinishDayByVillageId);
		_gradualCultureTargetByVillageId ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_gradualCultureFinishDayByVillageId ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearVillageEconomyForNewGame();
        _aidLastDay.Clear(); _aidDeliveryRemainder.Clear(); _aidRoundDay.Clear(); _aidRoundMask.Clear(); _aidBonuses.Clear(); _aidBonusExpires.Clear(); _aidMultiplier.Clear(); _purgeUntil.Clear();
		ClearNativeDispositionRequest();
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		ClearMissionState("new_game");
		_gradualCultureTargetByVillageId.Clear();
		_gradualCultureFinishDayByVillageId.Clear();
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		if (!string.IsNullOrWhiteSpace(_dispositionPayoutVillage)) _dispositionAwaitingExit = true;
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		ClearMissionState("game_loaded");
	}

	internal static bool QueueIncidentDispositionMission(Settlement villageSettlement, string source, bool hostileDisposition = false, TroopRoster selectedRoster = null)
	{
		if (villageSettlement?.IsVillage != true || villageSettlement.Village == null || string.IsNullOrWhiteSpace(villageSettlement.StringId))
		{
			return false;
		}
		_pendingIncidentVillageId = villageSettlement.StringId;
		_pendingHostileDisposition = hostileDisposition;
		_pendingVillageSelectedRoster = selectedRoster;
		_pendingIncidentQueuedUtc = DateTime.UtcNow;
		GcczDiagnosticLog.Log("VillageEntry", "queued incident disposition village=" + _pendingIncidentVillageId
			+ " source=" + (source ?? "N/A"));
		return true;
	}

	internal static bool TryConsumeQueuedIncidentDisposition(Settlement villageSettlement, string source, out bool hostileDisposition, out TroopRoster selectedRoster)
	{
		hostileDisposition = false;
		selectedRoster = null;
		if (villageSettlement?.IsVillage != true
			|| !string.Equals(_pendingIncidentVillageId, villageSettlement.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		bool expired = _pendingIncidentQueuedUtc == DateTime.MinValue
			|| (DateTime.UtcNow - _pendingIncidentQueuedUtc).TotalSeconds > 30d;
		hostileDisposition = !expired && _pendingHostileDisposition;
		selectedRoster = !expired ? _pendingVillageSelectedRoster : null;
		_pendingVillageSelectedRoster = null;
		_pendingHostileDisposition = false;
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		GcczDiagnosticLog.Log("VillageEntry", (expired ? "expired" : "consumed")
			+ " queued incident disposition village=" + (villageSettlement.StringId ?? "N/A")
			+ " source=" + (source ?? "N/A"));
		return !expired;
	}

	internal static bool HasQueuedDispositionForExternal(string settlementId)
		=> !string.IsNullOrEmpty(settlementId)
			&& string.Equals(_pendingIncidentVillageId, settlementId, StringComparison.OrdinalIgnoreCase)
			&& _pendingIncidentQueuedUtc != DateTime.MinValue
			&& (DateTime.UtcNow - _pendingIncidentQueuedUtc).TotalSeconds <= 30d;

	internal static void CancelQueuedIncidentDisposition(string source)
	{
		_pendingVillageSelectedRoster = null;
		_pendingHostileDisposition = false;
		if (!string.IsNullOrWhiteSpace(_pendingIncidentVillageId))
		{
			GcczDiagnosticLog.Log("VillageEntry", "cancelled queued incident disposition village=" + _pendingIncidentVillageId
				+ " source=" + (source ?? "N/A"));
		}
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
	}

	internal static bool TryActivateForSetsVillage(Settlement villageSettlement, Mission mission, string source, bool hostileDisposition = false)
	{
		if (!VillageDispositionSceneMissionBehavior.Admits(mission, villageSettlement?.StringId)) return false;
		if (hostileDisposition) return TryActivateHostileDisposition(villageSettlement, mission, source);
		try
		{
			if (mission == null || villageSettlement?.IsVillage != true || villageSettlement.Village == null)
			{
				return false;
			}
			Settlement bound = villageSettlement.Village.Bound;
			Clan playerClan = Clan.PlayerClan;
			Kingdom playerKingdom = playerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
			bool playerIsRuler = playerKingdom != null
				&& playerKingdom.Leader == Hero.MainHero;
			VillageAftermathEntryDecision decision = VillageAftermathEntryPolicy.Evaluate(
				new VillageAftermathEntryFacts(
					isVillage: true,
					hasBoundSettlement: bound != null,
					villageStateIsNormal: villageSettlement.Village.VillageState == Village.VillageStates.Normal,
					isUnderSiegeOrRaid: villageSettlement.IsUnderSiege || villageSettlement.Party?.MapEvent != null,
					playerClanOwnsBoundSettlement: bound?.OwnerClan == playerClan,
					playerIsKingdomRuler: playerIsRuler,
					boundSettlementBelongsToPlayerKingdom: playerKingdom != null && bound?.OwnerClan?.Kingdom == playerKingdom));
			if (!decision.Allowed)
			{
				GcczDiagnosticLog.LogVerbose("VillageEntry", "denied village=" + (villageSettlement.StringId ?? "N/A")
					+ " bound=" + (bound?.StringId ?? "N/A")
					+ " reason=" + decision.ReasonCode
					+ " source=" + (source ?? "N/A"));
				return false;
			}

			if (!PrepareForeignEconomy(villageSettlement)) return false;
			Departure.Reset(); DestructiveActions.Clear();
			ClearHostileDialogue();
			HostileDialogue.Begin(villageSettlement.StringId, GcczTownPromptResourceProvider.UseEnglishPrompt, GcczTownPromptResourceProvider.GetCatalog());
			_hostileDispositionActive = false;
			_hostileVillageMercyApplied = false;
			_activeMission = mission;
			SettlementEntryTroopSelectionBehavior.CompletePendingVictoryMenuForExternal(villageSettlement.StringId);
			_activeVillageId = villageSettlement.StringId ?? "";
			_activeAuthorityKind = decision.AuthorityKind;
			_cultureInquiryPending = false;
			AppliedActions.Clear();
			GcczDiagnosticLog.Log("VillageEntry", "activated village=" + _activeVillageId
				+ " bound=" + (bound?.StringId ?? "N/A")
				+ " authority=" + _activeAuthorityKind
				+ " source=" + (source ?? "N/A"));
			InformationManager.DisplayMessage(new InformationMessage(
				VUi("已进入友方村庄事务。可交涉、援助或下令；未作任何处置直接 TAB 离开会默认掠夺。", "Friendly village affairs started. Negotiate, aid or issue orders. Leaving with TAB before any disposition defaults to plunder."),
				Color.FromUint(SuccessColor)));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Activate failed: " + ex);
            if (EconomyOwner?._dispositionPayoutVillage == villageSettlement?.StringId) EconomyOwner._dispositionAwaitingExit = true;
            ClearMissionState("friendly_activation_failed");
			return false;
		}
	}

	internal static bool IsActive()
	{
		return _activeMission != null
			&& ReferenceEquals(_activeMission, Mission.Current)
			&& VillageDispositionSceneMissionBehavior.Admits(_activeMission, _activeVillageId)
			&& !string.IsNullOrWhiteSpace(_activeVillageId);
	}

	internal static bool IsActiveForMission(Mission mission)
	{
		return mission != null && IsActive() && ReferenceEquals(_activeMission, mission);
	}

	internal static void TickForSetsMission(Mission mission)
	{
		if (IsActiveForMission(mission)) { TickForeignBanners(mission); TickVillageMassacre(mission); }
		if (!IsActiveForMission(mission) || !_cultureInquiryPending)
		{
			return;
		}
		if (mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
		{
			return;
		}
		_cultureInquiryPending = false;
		try
		{
			OpenGovernanceCultureInquiry();
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Open culture inquiry failed: " + ex);
			TryDisplayFailureMessage(SiegeAftermathUiText.VillageCultureFailure(false));
		}
	}

	internal static void EndForSetsMission(Mission mission, string source)
	{
		if (mission == null || !ReferenceEquals(_activeMission, mission))
		{
			return;
		}
		if (EconomyOwner != null) EconomyOwner._dispositionAwaitingExit = true;
		ClearMissionState(source);
	}

	internal static List<PostprocessRuleEntry> BuildPostprocessRulesForExternal(int targetAgentIndex, bool replyIsDirectPlayerResponse)
	{
		if (!HasCurrentDispositionPromptSpeaker(null, null, targetAgentIndex) || !replyIsDirectPlayerResponse)
		{
			return new List<PostprocessRuleEntry>();
		}

		if (_hostileDispositionActive)
		{
			VillageDispositionSpeakerRole role = ResolveHostileSpeakerRole(targetAgentIndex, out _);
			bool hasSupport = EconomyOwner?.Assistance.CanSpendForSceneTurn(_activeVillageId, InitialVillageAssistance, ForeignPersuasionTurn) == true;
			return VillageDispositionActionCatalog.All.Where(spec => spec.HasDefinedEffect
				&& VillageDispositionSpeakerPolicy.CanAuthorize(spec, role, replyIsDirectPlayerResponse)
				&& (spec.Kind != VillageDispositionActionKind.Mercy || !_hostileVillageMercyApplied)
				&& (spec.OutcomeLevel <= 0 || hasSupport))
				.Select(spec => Rule(spec.Tag, VillageDispositionPromptProfile.BuildActionDescription(spec, GcczTownPromptResourceProvider.UseEnglishPrompt))).ToList();
		}

		return BuildFriendlyRules(targetAgentIndex);
	}

	internal static string BuildPostprocessContextForExternal(int targetAgentIndex, bool replyIsDirectPlayerResponse)
	{
		if (!HasCurrentDispositionPromptSpeaker(null, null, targetAgentIndex))
		{
			return string.Empty;
		}
		Settlement village = ResolveActiveVillage();
        var role = ResolveHostileSpeakerRole(targetAgentIndex, out _);
        bool canGovern = !_hostileDispositionActive && CanGovern(village);
        string context = VillageDispositionPromptProfile.BuildPostprocessContext(village?.Name?.ToString(), role,
            replyIsDirectPlayerResponse, _hostileVillageMercyApplied, _hostileDispositionActive, canGovern, GcczTownPromptResourceProvider.GetCatalog());
        return context + (!_hostileDispositionActive ? "\n" + GcczTownPromptResourceProvider.GetCatalog().GetRuntimeText("VillageAid_Postprocess") : string.Empty)
            + (canGovern && role == VillageDispositionSpeakerRole.PlayerCompanion ? "\n" + CultureRuleContext() : string.Empty);
	}

	internal static string NormalizePostprocessTagsForExternal(string raw, List<PostprocessRuleEntry> rules)
	{
		if (!IsActive() || string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}
		if (_hostileDispositionActive)
		{
			HashSet<string> foreignAllowed = new HashSet<string>((rules ?? new List<PostprocessRuleEntry>()).Where(rule => rule != null).Select(rule => rule.Tag), StringComparer.OrdinalIgnoreCase);
			return string.Join("\n", VillageDispositionActionCatalog.Extract(raw)
				.Where(spec => spec.HasDefinedEffect && foreignAllowed.Contains(spec.Tag)).Select(spec => spec.Tag));
		}
		HashSet<string> allowed = (rules ?? new List<PostprocessRuleEntry>())
			.Select(rule => (rule?.Tag ?? string.Empty).Trim())
			.Where(tag => !string.IsNullOrWhiteSpace(tag))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return FriendlyVillageActionCatalog.NormalizeInvocations(raw, allowed);
	}

	internal static bool TryProcessActionTagsForExternal(
		int targetAgentIndex,
		ref string text,
		out bool actionHandled,
		bool replyIsDirectPlayerResponse)
	{
		actionHandled = false;
		if (_hostileDispositionActive) return TryProcessHostileDisposition(targetAgentIndex, ref text, out actionHandled, replyIsDirectPlayerResponse);
		return TryProcessFriendlyDisposition(targetAgentIndex, ref text, out actionHandled, replyIsDirectPlayerResponse);
	}

	private static PostprocessRuleEntry Rule(string tag, string description)
	{
		return new PostprocessRuleEntry { Tag = tag, Description = description };
	}

	private static bool IsValidMissionSpeaker(int targetAgentIndex)
	{
		try
		{
			return targetAgentIndex >= 0
				&& _activeMission?.Agents?.Any(agent => agent != null && agent.Index == targetAgentIndex && agent.IsHuman && agent.IsActive()) == true;
		}
		catch
		{
			return false;
		}
	}

	private static void TryDisplayFailureMessage(string message)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message ?? string.Empty, Color.FromUint(WarningColor)));
		}
		catch
		{
		}
	}

	private void OnDailyTick()
	{
		RefreshAidMultipliers();
		try
		{
			foreach (string villageId in _gradualCultureFinishDayByVillageId.Keys.ToList())
			{
				Settlement village = Settlement.Find(villageId);
                if (!CanGovern(village))
                {
                    _gradualCultureFinishDayByVillageId.Remove(villageId); _gradualCultureTargetByVillageId.Remove(villageId);
                    InformationManager.DisplayMessage(new InformationMessage(VUi("移民融合已中止：失去村庄治理权。", "Migrant integration cancelled: governance authority lost.") + " " + village?.Name));
                    continue;
                }
                if (_gradualCultureFinishDayByVillageId[villageId] > CurrentDay) continue;
                string cultureId = _gradualCultureTargetByVillageId.TryGetValue(villageId, out string id) ? id : string.Empty;
				CultureObject culture = Game.Current?.ObjectManager?.GetObject<CultureObject>(cultureId);
				if (village?.IsVillage == true && culture != null)
				{
					bool applied = ApplyVillageCultureNow(
						village,
						culture,
						clearVolunteerTypes: true,
						spawnReplacementNotables: false,
						source: "village_gradual_education_completed");
					if (!applied)
					{
						Logger.Log("VillageAftermath", "Deferred gradual culture completion because persistence was unavailable. village="
							+ (village.StringId ?? villageId) + " target=" + (culture.StringId ?? "N/A"));
						continue;
					}
					RecordCultureCompletion(village, culture);
				}
				_gradualCultureFinishDayByVillageId.Remove(villageId);
				_gradualCultureTargetByVillageId.Remove(villageId);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Daily culture update failed: " + ex);
		}
	}

	private static bool ApplyVillageCultureNow(
		Settlement village,
		CultureObject targetCulture,
		bool clearVolunteerTypes,
		bool spawnReplacementNotables,
		string source)
	{
		if (!TryPersistVillageCulture(village, targetCulture, source))
		{
			return false;
		}

		ApplyVillageNotableCulture(village, targetCulture, clearVolunteerTypes, spawnReplacementNotables);
		LogVillageCultureApplied(village, targetCulture, spawnReplacementNotables);
		return true;
	}

	private static bool TryPersistVillageCulture(Settlement village, CultureObject targetCulture, string source)
	{
		return GcczSettlementCulturePersistenceBehavior.ApplyAndRemember(village, targetCulture, source);
	}

	private static void LogVillageCultureApplied(Settlement village, CultureObject targetCulture, bool spawnedReplacements)
	{
		GcczDiagnosticLog.Log("VillageCulture", "applied village=" + (village?.StringId ?? "N/A")
			+ " culture=" + (targetCulture?.StringId ?? "N/A") + " replacement=" + spawnedReplacements);
	}

	private static void ApplyVillageNotableCulture(
		Settlement village,
		CultureObject targetCulture,
		bool clearVolunteerTypes,
		bool spawnReplacementNotables)
	{
		foreach (Hero notable in village.Notables?.Where(hero => hero != null && hero.IsAlive && hero.IsRuralNotable).ToList() ?? new List<Hero>())
		{
			notable.Culture = targetCulture;
			if (clearVolunteerTypes && notable.VolunteerTypes != null)
			{
				for (int i = 0; i < notable.VolunteerTypes.Length; i++)
				{
					notable.VolunteerTypes[i] = null;
				}
			}
		}
		if (spawnReplacementNotables)
		{
			SpawnReplacementNotables(village, targetCulture, Occupation.Headman);
			SpawnReplacementNotables(village, targetCulture, Occupation.RuralNotable);
		}
	}

	private static void KillVillageNotables(Settlement village, string source)
	{
		foreach (Hero notable in village?.Notables?.Where(hero => hero != null && hero.IsAlive && hero.IsRuralNotable).ToList() ?? new List<Hero>())
		{
			try
			{
				KillCharacterAction.ApplyByMurder(notable, Hero.MainHero, false);
				if (notable.IsAlive)
				{
					KillCharacterAction.ApplyByRemove(notable, false, true);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VillageAftermath", "Kill rural notable failed. source=" + source + " hero=" + (notable.StringId ?? "N/A") + " error=" + ex.Message);
			}
		}
	}

	private static void SpawnReplacementNotables(Settlement village, CultureObject culture, Occupation occupation)
	{
		int targetCount = Campaign.Current?.Models?.NotableSpawnModel?.GetTargetNotableCountForSettlement(village, occupation) ?? 0;
		int currentCount = village?.Notables?.Count(hero => hero != null && hero.IsAlive && hero.Occupation == occupation) ?? 0;
		for (int i = currentCount; i < targetCount; i++)
		{
			Hero notable = HeroCreator.CreateNotable(occupation, village);
			if (notable == null)
			{
				continue;
			}
			notable.Culture = culture;
			if (notable.CurrentSettlement != village && notable.StayingInSettlement != village)
			{
				EnterSettlementAction.ApplyForCharacterOnly(notable, village);
			}
		}
	}

	private static Settlement ResolveActiveVillage()
	{
		return string.IsNullOrWhiteSpace(_activeVillageId) ? null : Settlement.Find(_activeVillageId);
	}

	private static int CurrentDay => Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));

	private static void ClearMissionState(string source)
	{
		_giftInquiryActive = false; ClearCultureProposal(); StopVillageMassacre(); Departure.Reset(); DestructiveActions.Clear();
		SiegeAiInterventionBehavior.ReleaseVillageEscortsForExternal(_activeMission);
		ClearForeignSceneOrders();
		ClearForeignEconomyScene();
		ClearHostileDialogue();
		_hostileDispositionActive = false;
		_hostileVillageMercyApplied = false;
		_pendingHostileDisposition = false;
		_pendingVillageSelectedRoster = null;
		if (_activeMission != null || !string.IsNullOrWhiteSpace(_activeVillageId))
		{
			GcczDiagnosticLog.Log("VillageEntry", "cleared village=" + (_activeVillageId ?? "N/A") + " source=" + (source ?? "N/A"));
		}
		_activeMission = null;
		_activeVillageId = "";
		_activeAuthorityKind = VillageAftermathAuthorityKind.None;
		_cultureInquiryPending = false;
		AppliedActions.Clear();
	}
}
