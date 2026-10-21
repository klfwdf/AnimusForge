using System.Threading.Tasks;using System.Collections.Concurrent;using AnimusForge.Refactor.Modules;using AnimusForge.Refactor.Runtime;
using PendingAutomaticKingdomRebellionContext=AnimusForge.MyBehavior.PendingAutomaticKingdomRebellionContext;
using TaleWorlds.Core;
using TaleWorlds.Library;using TaleWorlds.CampaignSystem.Actions;using TaleWorlds.Localization;
using KingdomRebellionResolutionResult=AnimusForge.MyBehavior.KingdomRebellionResolutionResult;using KingdomRebellionCandidateInfo=AnimusForge.MyBehavior.KingdomRebellionCandidateInfo;using KingdomRebellionFollowerInfo=AnimusForge.MyBehavior.KingdomRebellionFollowerInfo;using ClanVisualSnapshot=AnimusForge.MyBehavior.ClanVisualSnapshot;using RebelFactionColorChoice=AnimusForge.MyBehavior.RebelFactionColorChoice;
using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Settlements;
using AnimusForge.Refactor.Adapters;using EventRecordEntry=AnimusForge.MyBehavior.EventRecordEntry;using RebelKingdomNamingResult=AnimusForge.MyBehavior.RebelKingdomNamingResult;
namespace AnimusForge;
// Domain naming context and retry/result application. Provider configuration/gateway stays in H3; no whole Campaign host capture.
internal sealed class KingdomRebellionRuntimeController
{
 // Legacy diagnostic prefixes are internal matching values, never localized in workers.
 internal const string AutomaticNamingFailurePrefix = "Automatic rebel kingdom naming background task failed: ";
 internal const string NamingRetryFailurePrefix = "Rebel kingdom naming retry background task failed: ";
 internal const string ForcedNamingFailurePrefix = "Forced rebel kingdom naming background task failed: ";
 private const int RebelKingdomNamingMaxAttempts=RebellionNamingOwner.MaxAttempts;
 private readonly WeeklyEventRecordStateOwner _records;
 private readonly Func<int> _requestInterval;
 internal int LastProcessedWeek=-1;
 internal readonly ConcurrentQueue<Action> NamingMainThreadActions=new ConcurrentQueue<Action>();
 private readonly KingdomRebellionPumpCapabilities _pump;
 private const int RebelKingdomInitialStabilityValue=50;
 private readonly Func<KingdomRebellionGameAdapter> _game;
 private readonly Func<KingdomStabilityGameAdapter> _stability;
 internal KingdomRebellionRuntimeController(WeeklyEventRecordStateOwner records,Func<int> requestInterval,Func<KingdomRebellionGameAdapter> game=null,Func<KingdomStabilityGameAdapter> stability=null,KingdomRebellionPumpCapabilities pump=null){_records=records;_requestInterval=requestInterval;_game=game;_stability=stability;_pump=pump;}
internal static string BuildRebelKingdomNamingSystemPrompt()
	{
		return RebellionNamingRules.BuildSystemPrompt(DuelSettings.GetSettings()?.KingdomRebellionSystemPrompt);
	}

internal string BuildRebelKingdomNamingUserPrompt(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null, IReadOnlyCollection<string> existingNames = null)
	{
		List<string> list = new List<string>();
		if (existingNames != null) list.AddRange(existingNames);
		else
		{
		try
		{
			foreach (Kingdom item in Kingdom.All.Where((Kingdom x) => x != null))
			{
				string text = (item.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		}
		string text2 = string.Join("、", list.Distinct(StringComparer.OrdinalIgnoreCase));
		List<Settlement> list2 = clan?.Settlements?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList() ?? new List<Settlement>();
		string text3 = (list2.Count > 0) ? string.Join("、", list2.Select(MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName)) : AdapterPromptCaptureTexts.PcapText("kingdom_rebel_none");
		string rebelSettlementSummaryForNamingPrompt = RebelNamingPromptSummaryCaptureAdapter.BuildRebelSettlementSummaryForNamingPrompt(list2);
		string rebelFollowerSummaryForNamingPrompt = RebelNamingPromptSummaryCaptureAdapter.BuildRebelFollowerSummaryForNamingPrompt(followerClans);
		string heroBackgroundForRebelNamingPrompt = KingdomRebellionGameAdapter.BuildHeroBackgroundForRebelNamingPrompt(clan?.Leader);
		string heroBackgroundForRebelNamingPrompt2 = KingdomRebellionGameAdapter.BuildHeroBackgroundForRebelNamingPrompt(oldKingdom?.Leader);
		string kingdomBackgroundForRebelNamingPrompt = KingdomRebellionGameAdapter.BuildKingdomBackgroundForRebelNamingPrompt(oldKingdom, _records);
		string rebelBackgroundForNamingPrompt = KingdomRebellionGameAdapter.BuildRebelBackgroundForNamingPrompt(oldKingdom, weekIndex, _records);
		EventRecordEntry weeklyReportRecordByWeek = _records.FindWeeklyReportRecordByWeek("world", "", weekIndex - 1);
		EventRecordEntry weeklyReportRecordByWeek2 = _records.FindWeeklyReportRecordByWeek("kingdom", MemoryEntityIdentityBannerlordAdapter.GetKingdomId(oldKingdom), weekIndex - 1);
		return RebellionNamingRules.BuildUserPrompt(new RebellionNamingFacts
		{
			WeekIndex = weekIndex, ClanName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan), LeaderName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(clan?.Leader),
			CultureName = (clan?.Culture?.Name?.ToString() ?? "").Trim(), KingdomName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(oldKingdom, AdapterPromptCaptureTexts.PcapText("kingdom_rebel_original")),
			KingName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(oldKingdom?.Leader), RulingClanName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(oldKingdom?.RulingClan),
			SettlementNames = text3, RebelBackground = rebelBackgroundForNamingPrompt,
			SettlementSummary = rebelSettlementSummaryForNamingPrompt, FollowerSummary = rebelFollowerSummaryForNamingPrompt,
			LeaderBackground = heroBackgroundForRebelNamingPrompt, KingBackground = heroBackgroundForRebelNamingPrompt2,
			KingdomBackground = kingdomBackgroundForRebelNamingPrompt,
			WorldWeekly = RebellionNamingRules.WeeklyLeadIn(weeklyReportRecordByWeek?.Title, weeklyReportRecordByWeek?.ShortSummary, weeklyReportRecordByWeek?.Summary),
			KingdomWeekly = RebellionNamingRules.WeeklyLeadIn(weeklyReportRecordByWeek2?.Title, weeklyReportRecordByWeek2?.ShortSummary, weeklyReportRecordByWeek2?.Summary), ExistingNames = text2
		});
	}

internal void BuildRebelKingdomNamingRequest(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans, out string systemPrompt, out string userPrompt, IReadOnlyCollection<string> existingNames = null)
	{
		systemPrompt = BuildRebelKingdomNamingSystemPrompt();
		userPrompt = BuildRebelKingdomNamingUserPrompt(clan, oldKingdom, weekIndex, followerClans, existingNames);
	}

internal static string[] CaptureRebellionExistingNames()
	{
		try { return Kingdom.All.Where(x => x != null).Select(x => (x.Name?.ToString() ?? "").Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(); }
		catch { return Array.Empty<string>(); }
	}

internal RebelKingdomNamingResult GenerateRebelKingdomNamingFromPrompts(string systemPrompt, string userPrompt, string logTarget, int maxAttempts = RebelKingdomNamingMaxAttempts, IReadOnlyCollection<string> existingNames = null)
	{
		var names = new HashSet<string>(existingNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		var outcome = RebellionNamingOwner.Generate(
			async () =>
			{
				var result = await ConfiguredChatApplicationAdapter.CallRebelKingdomNamingGatewayDetailed(systemPrompt, userPrompt);
				return result == null ? null : new RebellionNamingAttempt
				{
					Success = result.Success, Content = result.Content, ErrorMessage = result.ErrorMessage,
					ResponseBody = result.ResponseBody, IsRateLimit = result.IsRateLimit,
					IsRequestsPerMinuteLimit = result.IsRequestsPerMinuteLimit, IsQuotaLimit = result.IsQuotaLimit,
					RetryAfterSeconds = result.RetryAfterSeconds
				};
			}, name => names.Contains(name.Trim()), LlmRetryPrompt.BuildFailureDetail,
			(attempt, total, result) => Logger.LogEventPromptExchange((logTarget ?? "Rebellion kingdom naming") + " [attempt " + attempt + "/" + total + "]", "【System Prompt】\n" + (systemPrompt ?? "") + "\n\n【User Prompt】\n" + (userPrompt ?? ""), result?.Success == true ? result.Content ?? "" : ("Error: " + (result?.ErrorMessage ?? "unknown error"))),
			(attempt, total, reason) => Logger.Log("KingdomRebellion", "[WARN] Rebel kingdom naming attempt failed; retrying. target=" + (logTarget ?? "") + " attempt=" + attempt + "/" + total + " reason=" + (reason ?? "")),
			_requestInterval, maxAttempts);
		return new RebelKingdomNamingResult
		{
			Success = outcome.Success, FormalName = outcome.FormalName, ShortName = outcome.ShortName,
			EncyclopediaText = outcome.EncyclopediaText, AttemptsUsed = outcome.AttemptsUsed,
			FailureReason = outcome.FailureReason, IsRateLimit = outcome.IsRateLimit,
			IsRequestsPerMinuteLimit = outcome.IsRequestsPerMinuteLimit, IsQuotaLimit = outcome.IsQuotaLimit,
			RetryAfterSeconds = outcome.RetryAfterSeconds
		};
	}

internal RebelKingdomNamingResult GenerateRebelKingdomNaming(Clan clan, Kingdom oldKingdom, int weekIndex, IEnumerable<Clan> followerClans = null, int maxAttempts = RebelKingdomNamingMaxAttempts)
	{
		if (clan == null)
		{
			return BuildFailedRebelKingdomNamingResult(KingdomUiText.T("AF_UI_KingdomRebellion_NamingClanMissing", "The lead clan is missing; rebel kingdom naming cannot start."));
		}
		string[] rebellionExistingNames = CaptureRebellionExistingNames();
		BuildRebelKingdomNamingRequest(clan, oldKingdom, weekIndex, followerClans, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		return GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, "Rebellion kingdom naming - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan), maxAttempts, rebellionExistingNames);
	}

internal static RebelKingdomNamingResult BuildFailedRebelKingdomNamingResult(string failureReason, int attemptsUsed = 0)
	{
		return new RebelKingdomNamingResult
		{
			Success = false,
			FailureReason = (failureReason ?? "").Trim(),
			AttemptsUsed = Math.Max(0, attemptsUsed)
		};
	}

internal static bool IsRebelKingdomNamingSuccess(RebelKingdomNamingResult namingResult)
	{
		return namingResult != null && namingResult.Success && !string.IsNullOrWhiteSpace(namingResult.FormalName) && !string.IsNullOrWhiteSpace(namingResult.ShortName) && !string.IsNullOrWhiteSpace(namingResult.EncyclopediaText);
	}

internal static string BuildRebelKingdomNamingFailureExecutionMessage(RebelKingdomNamingResult namingResult)
	{
		int attempts = Math.Max(0, namingResult?.AttemptsUsed ?? 0);
		string text = attempts > 0
			? KingdomUiText.F("AF_UI_KingdomRebellion_NamingFailureRetried", "Rebel kingdom naming failed after {ATTEMPTS}/{MAX} automatic retries; kingdom founding was aborted.", ("ATTEMPTS", attempts), ("MAX", RebelKingdomNamingMaxAttempts))
			: KingdomUiText.T("AF_UI_KingdomRebellion_NamingFailureNoRetry", "Rebel kingdom naming did not complete; kingdom founding was aborted.");
		string failureReason = (namingResult?.FailureReason ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(failureReason))
		{
			// 原始 API 响应可能很长；交互窗口只保留恢复动作，详情由左下角通知和日志承载。
			text += KingdomUiText.T("AF_UI_KingdomRebellion_NamingFailureDetailsSuffix", " The last failure details were shown in the lower-left message and written to the log.");
		}
		return text + KingdomUiText.T("AF_UI_KingdomRebellion_NamingFailureRetrySuffix", " Please fill in the event/rebellion API settings again and retry.");
	}

internal static string LegacyRebellionRetiredMessage => KingdomUiText.T("AF_UI_KingdomRebellion_LegacyRetired", "The standalone stability rebellion system has been retired. Uprisings are managed by civil-war factions.");

// Compatibility entry points remain callable, but no longer roll, select clans or request names.
internal KingdomRebellionResolutionResult ResolveKingdomRebellion(Kingdom kingdom, int weekIndex, bool executeAction, bool forceTrigger)
    => new KingdomRebellionResolutionResult { Kingdom = kingdom, WeekIndex = weekIndex, Forced = forceTrigger, Message = LegacyRebellionRetiredMessage };

internal bool TryExecuteKingdomRebellion(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, List<Clan> followerClans, out string message)
{
    message = LegacyRebellionRetiredMessage;
    return false;
}

internal bool TryExecuteKingdomRebellionWithNaming(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, int relationToKing, int townCount, int castleCount, RebelKingdomNamingResult rebelKingdomNamingResult, List<Clan> followerClans, out string message, string civilWarFactionId = null)
    {
        // Physical founding is shared with faction wars. An untracked legacy request has no authority.
        if (string.IsNullOrWhiteSpace(civilWarFactionId) || !TeamModuleServices.CivilWar.IsRebellionRequestActive(civilWarFactionId))
        {
            message = LegacyRebellionRetiredMessage;
            return false;
        }

        message = "";
		string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, KingdomUiText.T("AF_UI_KingdomRebellion_ThisKingdom", "this kingdom"));
		string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan);
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			message = KingdomUiText.F("AF_UI_KingdomRebellion_ExecutionSkippedPlayerImmune", "{KINGDOM} is ruled by the player, and MCM player-kingdom stability rebellion immunity is enabled; this stability rebellion was skipped.", ("KINGDOM", kingdomDisplayName));
			Logger.Log("KingdomRebellion", "[SKIP] player kingdom stability rebellion immunity blocked execution. kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " forced=" + forceTrigger);
			return false;
		}
		if (!IsRebelKingdomNamingSuccess(rebelKingdomNamingResult))
		{
			message = BuildRebelKingdomNamingFailureExecutionMessage(rebelKingdomNamingResult);
			Logger.Log("KingdomRebellion", "[ERROR] Rebel kingdom creation blocked because naming failed. clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " attempts=" + (rebelKingdomNamingResult?.AttemptsUsed ?? 0) + " reason=" + (rebelKingdomNamingResult?.FailureReason ?? ""));
			return false;
		}
		string text = RebellionNamingRules.NormalizeName(rebelKingdomNamingResult?.FormalName ?? "", 24);
		string text2 = RebellionNamingRules.NormalizeName(rebelKingdomNamingResult?.ShortName ?? "", 14);
		string text3 = RebellionNamingRules.NormalizeLore(rebelKingdomNamingResult?.EncyclopediaText ?? "");
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2) || string.IsNullOrWhiteSpace(text3) || KingdomRebellionGameAdapter.IsDuplicateKingdomName(text))
		{
			message = KingdomUiText.T("AF_UI_KingdomRebellion_ExecutionInvalidNaming", "The rebel kingdom naming result was invalid or duplicated an existing kingdom name; kingdom founding was aborted. Please fill in the event/rebellion API settings again and retry.");
			Logger.Log("KingdomRebellion", "[ERROR] Rebel kingdom creation blocked by invalid naming result. clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " formal=" + text + " short=" + text2);
			return false;
		}
		if (Campaign.Current?.KingdomManager == null || clan == null || clan.Kingdom != kingdom || clan.IsEliminated || clan.Leader == null || !clan.Leader.IsAlive)
		{
			message = KingdomUiText.T("AF_UI_KingdomRebellion_ExecutionPreconditionChanged", "Pre-founding conditions changed; the clan did not leave the original kingdom.");
			return false;
		}
		ClanVisualSnapshot clanVisualSnapshot = KingdomRebellionGameAdapter.CaptureClanVisualSnapshot(clan);
		ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(clan, showNotification: true);
		KingdomRebellionGameAdapter.RestoreClanVisualSnapshot(clan, clanVisualSnapshot);
		RebelFactionColorChoice rebelFactionColorChoice = KingdomRebellionGameAdapter.BuildRandomUniqueRebelFactionColors(clan, kingdom, clanVisualSnapshot);
		KingdomRebellionGameAdapter.ApplyRebelFactionColorChoiceToClan(clan, rebelFactionColorChoice);
		KingdomManager kingdomManager = Campaign.Current?.KingdomManager;
		if (kingdomManager == null)
		{
			message = KingdomUiText.F("AF_UI_KingdomRebellion_ExecutionManagerMissing", "{CLAN} left {KINGDOM}, but KingdomManager was unavailable, so the new kingdom could not be completed.", ("CLAN", clanDisplayName), ("KINGDOM", kingdomDisplayName));
			Logger.Log("KingdomRebellion", "[ERROR] KingdomManager unavailable after rebellion leave. clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan));
			return true;
		}
		kingdomManager.CreateKingdom(new TextObject(text, null), new TextObject(text2, null), clan.Culture ?? kingdom.Culture, clan, null, new TextObject(text3, null), new TextObject(text, null), null);
		Kingdom kingdom2 = clan.Kingdom;
		if (kingdom2 == null || kingdom2 == kingdom || kingdom2.IsEliminated)
		{
			message = KingdomUiText.T("AF_UI_KingdomRebellion_ExecutionUnconfirmed", "The clan left the original kingdom, but the new kingdom has not been confirmed.");
			return false;
		}
		if (kingdom != null && !kingdom.IsEliminated)
		{
			_stability().SetKingdomStabilityValue(kingdom, KingdomStabilityPolicy.KingdomStabilityDefaultValue);
		}
		if (kingdom2 != null && !kingdom2.IsEliminated)
		{
			_game().MarkModCreatedRebelKingdom(kingdom2);
			_stability().SetKingdomStabilityValue(kingdom2, RebelKingdomInitialStabilityValue);
            MyBehavior.PacifyInheritedRebelWars(kingdom2, kingdom);
		}
		List<string> list = new List<string>();
		if (kingdom2 != null && followerClans != null && followerClans.Count > 0)
		{
			foreach (Clan followerClan in followerClans.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => MemoryEntityIdentityBannerlordAdapter.GetClanId(x), StringComparer.OrdinalIgnoreCase).Select((IGrouping<string, Clan> x) => x.First()))
			{
				try
				{
					if (followerClan.IsEliminated || followerClan.Kingdom != kingdom || followerClan.IsUnderMercenaryService || followerClan.IsClanTypeMercenary)
					{
						continue;
					}
					ChangeKingdomAction.ApplyByJoinToKingdomByDefection(followerClan, kingdom, kingdom2, default(CampaignTime), showNotification: true);
					list.Add(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(followerClan));
					KingdomRebellionGameAdapter.MarkClanVisualsDirty(followerClan);
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[WARN] Failed to attach follower clan to rebel kingdom. leader=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " follower=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(followerClan) + " error=" + ex.Message);
				}
			}
		}
		KingdomRebellionGameAdapter.MarkClanVisualsDirty(clan);
		string clanFortificationSummary = KingdomStabilityGameAdapter.BuildClanFortificationSummaryForPrompt(clan);
		message = KingdomUiText.F("AF_UI_KingdomRebellion_ExecutionSuccess", "{CLAN} left {KINGDOM} and founded {NEW_KINGDOM}.", ("CLAN", clanDisplayName), ("KINGDOM", kingdomDisplayName), ("NEW_KINGDOM", text));
		if (!string.IsNullOrWhiteSpace(clanFortificationSummary))
		{
			message = message + " " + clanFortificationSummary;
		}
		if (list.Count > 0)
		{
			message = message + KingdomUiText.F("AF_UI_KingdomRebellion_ExecutionFollowers", " Coalition follower clans: {FOLLOWERS}.", ("FOLLOWERS", string.Join(KingdomUiText.T("AF_UI_KingdomRebellion_ClanSeparator", ", "), list)));
		}
		_game().TryDiscontinueLandlessKingdom(kingdom, "rebellion_old_kingdom:" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan), requireKnownModRebelKingdom: false, allowPlayerKingdom: true);
		Logger.Log("KingdomRebellion", "[EXECUTE] week=" + weekIndex + " kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " forced=" + forceTrigger + " relation=" + relationToKing + " towns=" + townCount + " castles=" + castleCount + " newKingdomName=" + text + " namingAttempts=" + (rebelKingdomNamingResult?.AttemptsUsed ?? 0) + " followerCount=" + list.Count + " followers=" + string.Join("|", list) + " bgColor=0x" + rebelFactionColorChoice.BackgroundColor.ToString("X8") + " iconColor=0x" + rebelFactionColorChoice.IconColor.ToString("X8"));
		return true;
	}

internal void TryProcessWeeklyKingdomRebellions(int weekIndex)
	{
		while (!ProcessWeeklyKingdomRebellionsSlice(weekIndex))
		{
		}
	}

internal bool ProcessWeeklyKingdomRebellionsSlice(int weekIndex)
	{
		if (weekIndex <= 0 || LastProcessedWeek >= weekIndex)
		{
			ResetPendingWeeklyKingdomRebellionMaintenance();
			return true;
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			CancelPendingAutomaticKingdomRebellions("disabled_by_mcm");
			ResetPendingWeeklyKingdomRebellionMaintenance();
			LastProcessedWeek = Math.Max(LastProcessedWeek, weekIndex);
			Logger.Log("KingdomRebellion", "[SKIP] Kingdom stability and rebellion are disabled in MCM; skipping automatic rebellion checks for week=" + weekIndex + ".");
			return true;
		}
		try
		{
			_pump.Maintenance().BeginWeek(weekIndex, _pump.EditableKingdoms);
			if (_pump.Maintenance().TryTake(out Kingdom devEditableKingdom))
			{
				try
				{
					using (PerfProbe.Scope("MyBehavior.WeeklyKingdomRebellions.ResolveKingdom"))
					{
						int kingdomStabilityValue = _stability().GetKingdomStabilityValue(devEditableKingdom);
						int kingdomStabilityWeeklyBalancingDelta = KingdomStabilityPolicy.GetKingdomStabilityWeeklyBalancingDelta(kingdomStabilityValue)
                            + CivilWarWearinessRules.WeeklyStability(MyBehavior.GetMaxWarWearinessForExternal(devEditableKingdom));
						if (kingdomStabilityWeeklyBalancingDelta != 0)
						{
							_stability().SetKingdomStabilityValue(devEditableKingdom, kingdomStabilityValue + kingdomStabilityWeeklyBalancingDelta);
						}
TeamModuleServices.CivilWar.AdvanceWeek(devEditableKingdom, weekIndex, _stability().GetKingdomStabilityValue(devEditableKingdom), (target, delta) =>
								{
									int before = _stability().GetKingdomStabilityValue(target);
									_stability().SetKingdomStabilityValue(target, before + delta);
								}, _pump.RecentFacts(MemoryEntityIdentityBannerlordAdapter.GetKingdomId(devEditableKingdom), 8));

					}
				}
				catch (Exception ex)
				{
					Logger.Log("KingdomRebellion", "[ERROR] Weekly resolution failed for kingdom " + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(devEditableKingdom) + ": " + ex.Message);
				}
				return _pump.Maintenance().Complete && CompleteWeeklyKingdomRebellionMaintenance(weekIndex);
			}
			return CompleteWeeklyKingdomRebellionMaintenance(weekIndex);
		}
		catch (Exception ex2)
		{
			Logger.Log("KingdomRebellion", "[ERROR] ProcessWeeklyKingdomRebellionsSlice failed: " + ex2);
			ResetPendingWeeklyKingdomRebellionMaintenance();
			return true;
		}
	}

internal bool CompleteWeeklyKingdomRebellionMaintenance(int weekIndex)
	{
		LastProcessedWeek = Math.Max(LastProcessedWeek, weekIndex);
		ResetPendingWeeklyKingdomRebellionMaintenance();
		if (_pump.Automatic().ActivateIfQueued())
		{
			TryStartNextAutomaticKingdomRebellionAsync();
		}
		return true;
	}

internal void ResetPendingWeeklyKingdomRebellionMaintenance()
	{
		_pump.Maintenance().ResetWeek();
	}

internal void QueueAutomaticKingdomRebellion(KingdomRebellionResolutionResult result, string civilWarFactionId = null)
	{
        if (string.IsNullOrWhiteSpace(civilWarFactionId))
        {
            Logger.Log("KingdomRebellion", "[SKIP] retired standalone rebellion queue");
            return;
        }

		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			return;
		}
		if (result?.Kingdom == null || result.SelectedClan == null)
		{
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(result.Kingdom))
		{
			Logger.Log("KingdomRebellion", "[SKIP] player kingdom stability rebellion immunity blocked automatic queue. kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(result.Kingdom) + " clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(result.SelectedClan));
			return;
		}
		KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = result.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == result.SelectedClan);
		PendingAutomaticKingdomRebellionContext item = new PendingAutomaticKingdomRebellionContext
		{
			KingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(result.Kingdom),
			ClanId = MemoryEntityIdentityBannerlordAdapter.GetClanId(result.SelectedClan),
			WeekIndex = result.WeekIndex,
			StabilityValue = result.StabilityValue,
			StabilityTierText = result.StabilityTierText,
			RelationToKing = kingdomRebellionCandidateInfo?.RelationToKing ?? 0,
			TownCount = kingdomRebellionCandidateInfo?.TownCount ?? 0,
			CastleCount = kingdomRebellionCandidateInfo?.CastleCount ?? 0,
			FollowerClanIds = (result.SelectedFollowerClans ?? new List<Clan>()).Where((Clan x) => x != null && x != result.SelectedClan).Select(MemoryEntityIdentityBannerlordAdapter.GetClanId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
			,
			CivilWarFactionId = civilWarFactionId ?? ""
		};
		_pump.Automatic().Enqueue(item);
	}

internal void CancelPendingAutomaticKingdomRebellions(string reason)
	{
		_pump.Automatic().Cancel();
		Logger.Log("KingdomRebellion", "[CANCEL] Pending automatic rebellions cleared. reason=" + (reason ?? ""));
	}

internal void TryStartNextAutomaticKingdomRebellionAsync()
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			CancelPendingAutomaticKingdomRebellions("disabled_before_start");
			_pump.DeferredWeekly();
			return;
		}
		if (!_pump.Automatic().TryDequeue(out PendingAutomaticKingdomRebellionContext pendingAutomaticKingdomRebellionContext))
		{
			return;
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(pendingAutomaticKingdomRebellionContext.KingdomId);
		Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(pendingAutomaticKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingAutomaticKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		if (IsStaleCivilWarRebellion(pendingAutomaticKingdomRebellionContext))
		{
			ContinueAutomaticKingdomRebellionFlow();
			return;
		}
		if (kingdom == null || clan == null)
		{
			NotifyCivilWarRebellionFailed(pendingAutomaticKingdomRebellionContext, KingdomUiText.T("AF_UI_KingdomRebellion_ReasonTargetChanged", "the target kingdom or clan changed"));
			_pump.CompletionPopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, false, KingdomUiText.T("AF_UI_KingdomRebellion_AutomaticTargetChanged", "This week's automatic rebellion was triggered, but the target kingdom or clan changed; execution cannot continue."));
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			Logger.Log("KingdomRebellion", "[SKIP] player kingdom stability rebellion immunity blocked queued automatic rebellion before naming. kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan));
			NotifyCivilWarRebellionFailed(pendingAutomaticKingdomRebellionContext, KingdomUiText.T("AF_UI_KingdomRebellion_ReasonPlayerImmune", "player-kingdom stability rebellion immunity is enabled"));
			ContinueAutomaticKingdomRebellionFlow();
			return;
		}
		string[] rebellionExistingNames = CaptureRebellionExistingNames();
		long namingRequestVersion = _pump.Automatic().BeginNaming();
        if (clan == Clan.PlayerClan && !string.IsNullOrWhiteSpace(pendingAutomaticKingdomRebellionContext.CivilWarFactionId))
        {
            ShowPlayerRebelKingdomNamingInquiry(pendingAutomaticKingdomRebellionContext, namingRequestVersion, clan, kingdom, rebellionExistingNames);
            return;
        }
		BuildRebelKingdomNamingRequest(clan, kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex, list, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		InformationManager.ShowInquiry(new InquiryData(KingdomUiText.T("AF_UI_KingdomRebellion_GeneratingTitle", "Generating rebel kingdom name"), KingdomUiText.T("AF_UI_KingdomRebellion_GeneratingBody", "The system is requesting a new kingdom name and encyclopedia description from the LLM in the background.\n\nThe clan departure and kingdom founding will run only after this step finishes.\nPlease wait; the result will open automatically."), isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string logTarget = "Automatic rebellion kingdom naming - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult;
			try
			{
				namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, RebelKingdomNamingMaxAttempts, rebellionExistingNames);
			}
			catch (Exception ex)
			{
				namingResult = BuildFailedRebelKingdomNamingResult(AutomaticNamingFailurePrefix + ex.Message);
			}
			EnqueueKingdomRebellionNamingMainThreadAction(runtimeGeneration, delegate
			{
				if (_pump.Automatic().CompleteNaming(namingRequestVersion, pendingAutomaticKingdomRebellionContext))
					pendingAutomaticKingdomRebellionContext.NamingResult = namingResult;
			}, "automatic_rebellion_naming");
		});
	}

internal void ProcessPendingAutomaticKingdomRebellionResult()
	{
		if (!_pump.Automatic().TryTakeReady(out PendingAutomaticKingdomRebellionContext pendingAutomaticKingdomRebellionContext))
		{
			return;
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			CancelPendingAutomaticKingdomRebellions("disabled_before_execute");
			_pump.DeferredWeekly();
			return;
		}
		if (pendingAutomaticKingdomRebellionContext == null)
		{
			return;
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(pendingAutomaticKingdomRebellionContext.KingdomId);
		Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(pendingAutomaticKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingAutomaticKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		string executionMessage;
		bool success;
		// The civil-war faction may have timed out or dissolved while the name was being generated.
		if (IsStaleCivilWarRebellion(pendingAutomaticKingdomRebellionContext))
		{
			InformationManager.HideInquiry();
			ContinueAutomaticKingdomRebellionFlow();
			return;
		}
		if (kingdom == null || clan == null)
		{
			success = false;
			executionMessage = KingdomUiText.T("AF_UI_KingdomRebellion_AutomaticTargetChangedAfterNaming", "The rebel kingdom name was generated, but the target kingdom or clan changed; execution cannot continue.");
		}
		else if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			success = false;
			executionMessage = KingdomUiText.F("AF_UI_KingdomRebellion_AutomaticPlayerImmune", "{KINGDOM} is ruled by the player, and MCM player-kingdom stability rebellion immunity is enabled; this automatic stability rebellion was skipped.", ("KINGDOM", MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, KingdomUiText.T("AF_UI_KingdomRebellion_ThisKingdom", "this kingdom"))));
		}
		else
		{
			if (!IsRebelKingdomNamingSuccess(pendingAutomaticKingdomRebellionContext.NamingResult))
			{
				_pump.NamingFailurePopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, false);
				return;
			}
            WorldBulletinEventCaptureAdapter.CivilWarRebellionExecuting = !string.IsNullOrWhiteSpace(pendingAutomaticKingdomRebellionContext.CivilWarFactionId);
            try
            {
			success = TryExecuteKingdomRebellionWithNaming(clan, kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex, forceTrigger: false, pendingAutomaticKingdomRebellionContext.RelationToKing, pendingAutomaticKingdomRebellionContext.TownCount, pendingAutomaticKingdomRebellionContext.CastleCount, pendingAutomaticKingdomRebellionContext.NamingResult, list, out executionMessage, pendingAutomaticKingdomRebellionContext.CivilWarFactionId);
            }
            finally { WorldBulletinEventCaptureAdapter.CivilWarRebellionExecuting = false; }
			if (success && !string.IsNullOrWhiteSpace(pendingAutomaticKingdomRebellionContext.CivilWarFactionId))
			{
				TeamModuleServices.CivilWar.NotifyRebelKingdomCreated(pendingAutomaticKingdomRebellionContext.CivilWarFactionId, clan?.Kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex);
			}
            else if (success) TeamModuleServices.CivilWar.NoteKingdomRebellion(kingdom);
		}
		if (!success) NotifyCivilWarRebellionFailed(pendingAutomaticKingdomRebellionContext, executionMessage);
		_pump.CompletionPopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, success, executionMessage);
	}

private void ShowPlayerRebelKingdomNamingInquiry(PendingAutomaticKingdomRebellionContext context, long namingRequestVersion, Clan clan, Kingdom oldKingdom, string[] existingNames)
	{
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		HashSet<string> taken = new HashSet<string>(existingNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		string clanName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan);
		string oldName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(oldKingdom, KingdomUiText.T("AF_UI_KingdomRebellion_FormerKingdom", "the former kingdom"));
		string fallback = RebellionNamingRules.NormalizeName(KingdomUiText.F("AF_UI_KingdomRebellion_DefaultName", "{CLAN} League", ("CLAN", clanName)), 24);
		string text = KingdomUiText.F("AF_UI_KingdomRebellion_PlayerNamingBody", "Your faction is about to leave {OLD_KINGDOM} and establish a new kingdom led by the {CLAN} clan.\n\nEnter a new kingdom name (2–24 characters; it must be unique). Cancel uses “{FALLBACK}”.", ("OLD_KINGDOM", oldName), ("CLAN", clanName), ("FALLBACK", fallback));
		Func<string, Tuple<bool, string>> condition = input =>
		{
			string name = RebellionNamingRules.NormalizeName(input ?? "", 24);
			if (name.Length < 2) return new Tuple<bool, string>(false, KingdomUiText.T("AF_UI_KingdomRebellion_PlayerNamingTooShort", "The name must contain at least 2 characters."));
			if (taken.Contains(name)) return new Tuple<bool, string>(false, KingdomUiText.T("AF_UI_KingdomRebellion_PlayerNamingDuplicate", "A kingdom with that name already exists."));
			return new Tuple<bool, string>(true, "");
		};
		void Complete(string input)
		{
			if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || !_pump.Automatic().CompleteNaming(namingRequestVersion, context)) return;
			string formal = RebellionNamingRules.NormalizeName(input ?? "", 24);
			if (formal.Length < 2 || taken.Contains(formal)) formal = taken.Contains(fallback) ? RebellionNamingRules.NormalizeName(KingdomUiText.F("AF_UI_KingdomRebellion_AlternateDefaultName", "{CLAN} Independent League", ("CLAN", clanName)), 24) : fallback;
			string shortName = RebellionNamingRules.NormalizeName(formal, 14);
			context.NamingResult = new RebelKingdomNamingResult
			{
				FormalName = formal,
				ShortName = shortName,
				EncyclopediaText = KingdomUiText.F("AF_UI_KingdomRebellion_PlayerNamingEncyclopedia", "{FORMAL} was founded by the {CLAN} clan during its civil war against {OLD_KINGDOM}.", ("FORMAL", formal), ("CLAN", clanName), ("OLD_KINGDOM", oldName)),
				Success = true,
				AttemptsUsed = 0
			};
			Logger.Log("KingdomRebellion", "[PLAYER_NAMING] faction=" + (context.CivilWarFactionId ?? "") + " formal=" + formal);
		}
		InformationManager.ShowTextInquiry(new TextInquiryData(KingdomUiText.T("AF_UI_KingdomRebellion_PlayerNamingTitle", "Name the new kingdom"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, KingdomUiText.T("AF_UI_KingdomRebellion_PlayerNamingConfirm", "Found kingdom"), KingdomUiText.T("AF_UI_KingdomRebellion_PlayerNamingDefault", "Use default name"), Complete, () => Complete(""), shouldInputBeObfuscated: false, condition, "", fallback), pauseGameActiveState: true);
	}

internal static bool IsStaleCivilWarRebellion(PendingAutomaticKingdomRebellionContext context)
	{
		string factionId = context?.CivilWarFactionId;
		if (!string.IsNullOrWhiteSpace(factionId) && TeamModuleServices.CivilWar.IsRebellionRequestActive(factionId)) return false;
		Logger.Log("KingdomCivilWar", "[SKIP] stale civil war rebellion request faction=" + factionId);
		return true;
	}

internal static void NotifyCivilWarRebellionFailed(PendingAutomaticKingdomRebellionContext context, string reason)
	{
		if (!string.IsNullOrWhiteSpace(context?.CivilWarFactionId)) TeamModuleServices.CivilWar.NotifyRebellionFailed(context.CivilWarFactionId, reason);
	}

internal void RetryAutomaticKingdomRebellionNamingAsync(PendingAutomaticKingdomRebellionContext context)
	{
		_pump.ClearBlockedNamingUi();
		if (context == null)
		{
			ContinueAutomaticKingdomRebellionFlow();
			return;
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			CancelPendingAutomaticKingdomRebellions("disabled_before_rebellion_naming_retry");
			_pump.DeferredWeekly();
			return;
		}
		if (!_pump.Automatic().CanStart)
		{
			return;
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(context.KingdomId);
		Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(context.ClanId);
		List<Clan> list = (context.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		// The player may have spent weeks in the API repair flow; the civil-war faction may be gone by now.
		if (IsStaleCivilWarRebellion(context))
		{
			ContinueAutomaticKingdomRebellionFlow();
			return;
		}
		if (kingdom == null || clan == null)
		{
			NotifyCivilWarRebellionFailed(context, KingdomUiText.T("AF_UI_KingdomRebellion_ReasonTargetChanged", "the target kingdom or clan changed"));
			_pump.CompletionPopup(context, kingdom, clan, list, false, KingdomUiText.T("AF_UI_KingdomRebellion_RetryTargetChanged", "The target kingdom or clan changed before the rebellion naming retry could run."));
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			NotifyCivilWarRebellionFailed(context, KingdomUiText.T("AF_UI_KingdomRebellion_ReasonPlayerImmune", "player-kingdom stability rebellion immunity is enabled"));
			_pump.CompletionPopup(context, kingdom, clan, list, false, KingdomUiText.F("AF_UI_KingdomRebellion_AutomaticPlayerImmune", "{KINGDOM} is ruled by the player, and MCM player-kingdom stability rebellion immunity is enabled; this automatic stability rebellion was skipped.", ("KINGDOM", MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, KingdomUiText.T("AF_UI_KingdomRebellion_ThisKingdom", "this kingdom")))));
			return;
		}
		string[] rebellionExistingNames = CaptureRebellionExistingNames();
		BuildRebelKingdomNamingRequest(clan, kingdom, context.WeekIndex, list, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		long namingRequestVersion = _pump.Automatic().BeginNaming();
		InformationManager.ShowInquiry(new InquiryData(KingdomUiText.T("AF_UI_KingdomRebellion_RegeneratingTitle", "Regenerating rebel kingdom name"), KingdomUiText.T("AF_UI_KingdomRebellion_RegeneratingBody", "The system is requesting a new kingdom name and encyclopedia description again using the corrected event/rebellion API settings.\n\nThis round of automatic rebellion and the weekly report flow will remain paused until it finishes."), isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string logTarget = "Automatic rebellion kingdom naming retry - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult;
			try
			{
				namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, RebelKingdomNamingMaxAttempts, rebellionExistingNames);
			}
			catch (Exception ex)
			{
				namingResult = BuildFailedRebelKingdomNamingResult(NamingRetryFailurePrefix + ex.Message);
			}
			EnqueueKingdomRebellionNamingMainThreadAction(runtimeGeneration, delegate
			{
				if (_pump.Automatic().CompleteNaming(namingRequestVersion, context))
					context.NamingResult = namingResult;
			}, "automatic_rebellion_naming_retry");
		});
	}

internal void ContinueAutomaticKingdomRebellionFlow()
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			CancelPendingAutomaticKingdomRebellions("disabled_before_continue");
			_pump.DeferredWeekly();
			return;
		}
		if (_pump.Automatic().PendingCount > 0)
		{
			TryStartNextAutomaticKingdomRebellionAsync();
			return;
		}
		_pump.Automatic().FinishFlow();
		_pump.DeferredWeekly();
	}

internal void EnqueueKingdomRebellionNamingMainThreadAction(long runtimeGeneration, Action action, string source)
	{
		if (action == null)
		{
			return;
		}
		NamingMainThreadActions.Enqueue(delegate
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, source))
			{
				return;
			}
			action();
		});
	}

internal void ProcessKingdomRebellionNamingMainThreadActions()
	{
		int processed = 0;
		while (processed < 16 && NamingMainThreadActions.TryDequeue(out var action))
		{
			processed++;
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("KingdomRebellion", "[ERROR] naming main-thread action failed: " + ex);
			}
		}
	}

internal void ClearNamingMainThreadActions() { while (NamingMainThreadActions.TryDequeue(out var _)) { } }
}

internal sealed class KingdomRebellionPumpCapabilities
{
 internal Func<KingdomMaintenanceOwner<Kingdom>> Maintenance;
 internal Func<AutomaticKingdomRebellionOwner<MyBehavior.PendingAutomaticKingdomRebellionContext>> Automatic;
 internal Func<List<Kingdom>> EditableKingdoms;
 internal Func<string,int,List<string>> RecentFacts;
 internal Action DeferredWeekly;
 internal Action<MyBehavior.PendingAutomaticKingdomRebellionContext,Kingdom,Clan,List<Clan>,bool,string> CompletionPopup;
 internal Action<MyBehavior.PendingAutomaticKingdomRebellionContext,Kingdom,Clan,List<Clan>,bool> NamingFailurePopup;
 internal Action ClearBlockedNamingUi;
}
