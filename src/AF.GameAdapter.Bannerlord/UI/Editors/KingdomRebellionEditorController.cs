using System; using System.Collections.Generic; using System.Linq; using System.Text; using System.Threading.Tasks;
using TaleWorlds.CampaignSystem; using TaleWorlds.Core; using TaleWorlds.Library;
using TaleWorlds.Localization;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
// Existing UI/result coordination only. Naming policy, game effects and the main-thread action queue remain sole H1 authorities.
internal sealed class KingdomRebellionEditorController
{
 private const int RebelKingdomNamingMaxAttempts=RebellionNamingOwner.MaxAttempts;
 private readonly Func<KingdomRebellionRuntimeController> _runtime;
 private readonly Func<AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext>> _automatic;
 private readonly Func<long> _captureGeneration;
 private readonly Action<Kingdom> _openStabilityDetail;
 private static string Ui(string id, string fallback) => new TextObject("{=" + id + "}" + fallback).ToString();
 private static string Ui(string id, string fallback, string variableName, string variableValue)
 {
  var text = new TextObject("{=" + id + "}" + fallback);
  text.SetTextVariable(variableName, variableValue ?? string.Empty);
  return text.ToString();
 }
 internal KingdomRebellionEditorController(Func<KingdomRebellionRuntimeController> runtime,
  Func<AutomaticKingdomRebellionOwner<PendingAutomaticKingdomRebellionContext>> automatic,
  Func<long> captureGeneration,Action<Kingdom> openStabilityDetail)
 { _runtime=runtime;_automatic=automatic;_captureGeneration=captureGeneration;_openStabilityDetail=openStabilityDetail; }
 internal bool DevForcedInProgress;
 internal bool PendingDevReady;
 internal PendingDevForcedKingdomRebellionContext PendingDevContext;
 internal PendingAutomaticKingdomRebellionContext BlockedAutomaticContext;
 internal PendingDevForcedKingdomRebellionContext BlockedDevContext;
 internal bool ReopenAfterApiConfig;
 internal long ReopenAfterApiConfigUtcTicks;
internal void OpenKingdomRebellionApiRepairFlow()
	{
		ReopenAfterApiConfig = true;
		ReopenAfterApiConfigUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(300.0).Ticks;
		if (!ModOnboardingBehavior.OpenEventAndRebellionApiRepairFlow())
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_ApiRepairMissing", "The API setup guide was not found. Check the event/rebellion API Base URL, API key, and model in MCM first.")));
		}
	}
internal void ProcessKingdomRebellionApiRepairResume()
	{
		if (!ReopenAfterApiConfig)
		{
			return;
		}
		if (InformationManager.IsAnyInquiryActive() || DateTime.UtcNow.Ticks < ReopenAfterApiConfigUtcTicks)
		{
			return;
		}
		ReopenAfterApiConfig = false;
		if (BlockedAutomaticContext != null)
		{
			PendingAutomaticKingdomRebellionContext context = BlockedAutomaticContext;
			Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(context.KingdomId);
			Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(context.ClanId);
			List<Clan> followerClans = (context.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan).ToList();
			ShowAutomaticKingdomRebellionNamingFailurePopup(context, kingdom, clan, followerClans, afterApiRepair: true);
			return;
		}
		if (BlockedDevContext != null)
		{
			PendingDevForcedKingdomRebellionContext context2 = BlockedDevContext;
			Kingdom kingdom2 = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(context2.KingdomId);
			Clan clan2 = MemoryEntityIdentityBannerlordAdapter.FindClanById(context2.ClanId);
			List<Clan> followerClans2 = (context2.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan2).ToList();
			ShowDevForcedKingdomRebellionNamingFailurePopup(context2, kingdom2, clan2, followerClans2, afterApiRepair: true);
		}
	}
internal void ProcessPendingDevForcedKingdomRebellionResult()
	{
		if (!PendingDevReady)
		{
			return;
		}
		PendingDevForcedKingdomRebellionContext pendingDevForcedKingdomRebellionContext = PendingDevContext;
		PendingDevReady = false;
		PendingDevContext = null;
		DevForcedInProgress = false;
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			InformationManager.HideInquiry();
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_DisabledForced", "Kingdom stability and rebellion are disabled in MCM. This forced rebellion was cancelled.")));
			return;
		}
		if (pendingDevForcedKingdomRebellionContext == null)
		{
			return;
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId);
		Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(pendingDevForcedKingdomRebellionContext.ClanId);
		List<Clan> list = (pendingDevForcedKingdomRebellionContext.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null).ToList();
		string text;
		bool flag;
		if (kingdom == null || clan == null)
		{
			flag = false;
			text = Ui("AF_UI_KingdomRebellion_TargetChanged", "The rebellion name was generated, but the target kingdom or clan changed before execution could continue.");
		}
		else if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			flag = false;
			text = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomRebellion_ThisKingdom", "this kingdom")) + Ui("AF_UI_KingdomRebellion_PlayerImmunitySkipped", " is ruled by the player, and MCM player-kingdom rebellion immunity is enabled. This forced stability rebellion was skipped.");
		}
		else
		{
			if (!KingdomRebellionRuntimeController.IsRebelKingdomNamingSuccess(pendingDevForcedKingdomRebellionContext.NamingResult))
			{
				ShowDevForcedKingdomRebellionNamingFailurePopup(pendingDevForcedKingdomRebellionContext, kingdom, clan, list, afterApiRepair: false);
				return;
			}
			flag = _runtime().TryExecuteKingdomRebellionWithNaming(clan, kingdom, pendingDevForcedKingdomRebellionContext.WeekIndex, forceTrigger: true, pendingDevForcedKingdomRebellionContext.RelationToKing, pendingDevForcedKingdomRebellionContext.TownCount, pendingDevForcedKingdomRebellionContext.CastleCount, pendingDevForcedKingdomRebellionContext.NamingResult, list, out text);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (pendingDevForcedKingdomRebellionContext.NamingResult != null)
		{
			if (!flag)
			{
				ReportRebelKingdomNamingFailure(pendingDevForcedKingdomRebellionContext.NamingResult);
			}
			AppendRebelKingdomNamingResultLines(stringBuilder, pendingDevForcedKingdomRebellionContext.NamingResult);
			stringBuilder.AppendLine();
		}
		if (list.Count > 0)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FollowerClans", "Coalition clans:"));
			stringBuilder.AppendLine("- " + string.Join(Ui("AF_UI_KingdomRebellion_ClanSeparator", ", "), list.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_ExecutionResult", "Execution result:"));
		stringBuilder.AppendLine((text ?? "").Trim());
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(flag ? Ui("AF_UI_KingdomRebellion_ForcedCompleted", "Forced rebellion completed") : Ui("AF_UI_KingdomRebellion_ForcedFailed", "Forced rebellion failed"), stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Ui("AF_UI_KingdomRebellion_BackToDetails", "Back to details"), "", delegate
		{
			_openStabilityDetail(kingdom ?? MemoryEntityIdentityBannerlordAdapter.FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId));
		}, null));
	}
internal void ShowDevForcedKingdomRebellionNamingFailurePopup(PendingDevForcedKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_ForcedNamingFailedBody", "The forced rebellion was ready, but the rebel kingdom name was not generated. The system will not continue with a local fallback name."));
		stringBuilder.AppendLine();
		if (kingdom != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_KingdomLabel", "Kingdom:") + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomRebellion_UnknownKingdom", "an unknown kingdom")));
		}
		if (clan != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_LeadClanLabel", "Lead clan:") + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FollowerClansInline", "Coalition clans:") + string.Join(", ", followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		ReportRebelKingdomNamingFailure(context?.NamingResult);
		AppendRebelKingdomNamingResultLines(stringBuilder, context?.NamingResult);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(afterApiRepair ? Ui("AF_UI_KingdomRebellion_ApiRepairReturned", "The API setup flow returned. Generate the rebel kingdom name again, or return to kingdom stability details.") : Ui("AF_UI_KingdomRebellion_ApiRepairRequired", "Fill in the event/rebellion API settings first. After fixing them, return here to generate the name again."));
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(afterApiRepair ? Ui("AF_UI_KingdomRebellion_RetryNaming", "Retry rebel kingdom naming") : Ui("AF_UI_KingdomRebellion_NamingFailedTitle", "Rebel kingdom naming failed"), stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, afterApiRepair ? Ui("AF_UI_KingdomRebellion_RegenerateName", "Generate name again") : Ui("AF_UI_KingdomRebellion_AdjustApi", "Adjust API settings"), Ui("AF_UI_KingdomRebellion_BackToDetails", "Back to details"), delegate
		{
			if (afterApiRepair)
			{
				RetryBlockedDevForcedKingdomRebellionNaming();
			}
			else
			{
				BlockedDevContext = context;
				BlockedAutomaticContext = null;
				OpenKingdomRebellionApiRepairFlow();
			}
		}, delegate
		{
			if (ReferenceEquals(BlockedDevContext, context))
			{
				BlockedDevContext = null;
			}
			ReopenAfterApiConfig = false;
			_openStabilityDetail(kingdom ?? MemoryEntityIdentityBannerlordAdapter.FindKingdomById(context?.KingdomId));
		}), pauseGameActiveState: true);
	}
internal void RetryBlockedDevForcedKingdomRebellionNaming()
	{
		PendingDevForcedKingdomRebellionContext context = BlockedDevContext;
		BlockedDevContext = null;
		ReopenAfterApiConfig = false;
		if (context == null)
		{
			return;
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(context.KingdomId);
		Clan clan = MemoryEntityIdentityBannerlordAdapter.FindClanById(context.ClanId);
		List<Clan> list = (context.FollowerClanIds ?? new List<string>()).Select(MemoryEntityIdentityBannerlordAdapter.FindClanById).Where((Clan x) => x != null && x != clan).ToList();
		if (kingdom == null || clan == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_RetryTargetChanged", "The target kingdom or clan changed before the rebellion naming retry could run.")));
			_openStabilityDetail(kingdom);
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_RetryPlayerImmune", "The kingdom is ruled by the player, and MCM player-kingdom rebellion immunity is enabled. The forced stability rebellion cannot be retried.")));
			_openStabilityDetail(kingdom);
			return;
		}
		StartDevForcedKingdomRebellionAsync(kingdom, clan, context.WeekIndex, context.RelationToKing, context.TownCount, context.CastleCount, list);
	}
internal void StartDevForcedKingdomRebellionAsync(Kingdom kingdom, Clan clan, int weekIndex, int relationToKing, int townCount, int castleCount, List<Clan> followerClans)
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_DisabledStart", "Kingdom stability and rebellion are disabled in MCM. The forced rebellion cannot start.")));
			return;
		}
		if (DevForcedInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_AlreadyRunning", "A forced rebellion task is already running in the background. Please wait.")));
			return;
		}
		if (kingdom == null || clan == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_TargetMissing", "The forced rebellion cannot start: the target kingdom or clan was not found.")));
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomRebellion_PlayerImmuneStart", "The kingdom is ruled by the player, and MCM player-kingdom rebellion immunity is enabled. The forced stability rebellion cannot start.")));
			return;
		}
		List<Clan> list = followerClans?.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => MemoryEntityIdentityBannerlordAdapter.GetClanId(x), StringComparer.OrdinalIgnoreCase).Select((IGrouping<string, Clan> x) => x.First()).ToList() ?? new List<Clan>();
		string[] rebellionExistingNames = KingdomRebellionRuntimeController.CaptureRebellionExistingNames();
		_runtime().BuildRebelKingdomNamingRequest(clan, kingdom, weekIndex, list, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		DevForcedInProgress = true;
		PendingDevReady = false;
		PendingDevContext = null;
		InformationManager.ShowInquiry(new InquiryData(Ui("AF_UI_KingdomRebellion_GeneratingTitle", "Generating rebel kingdom name"), Ui("AF_UI_KingdomRebellion_GeneratingBody", "The system is requesting a new kingdom name and encyclopedia description from the LLM in the background.\n\nThe clan departure and kingdom founding will run only after this step finishes.\nPlease wait; the result will open automatically."), isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		long runtimeGeneration = _captureGeneration();
		string logTarget = "叛乱建国命名 - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);
		PendingDevForcedKingdomRebellionContext pendingContext = new PendingDevForcedKingdomRebellionContext
		{
			KingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom),
			ClanId = MemoryEntityIdentityBannerlordAdapter.GetClanId(clan),
			WeekIndex = weekIndex,
			RelationToKing = relationToKing,
			TownCount = townCount,
			CastleCount = castleCount,
			FollowerClanIds = list.Select(MemoryEntityIdentityBannerlordAdapter.GetClanId).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
		};
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult;
			try
			{
				namingResult = _runtime().GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, RebelKingdomNamingMaxAttempts, rebellionExistingNames);
			}
			catch (Exception ex)
			{
				namingResult = KingdomRebellionRuntimeController.BuildFailedRebelKingdomNamingResult(KingdomRebellionRuntimeController.ForcedNamingFailurePrefix + ex.Message);
			}
			_runtime().EnqueueKingdomRebellionNamingMainThreadAction(runtimeGeneration, delegate
			{
				pendingContext.NamingResult = namingResult;
				PendingDevContext = pendingContext;
				PendingDevReady = true;
			}, "dev_forced_rebellion_naming");
		});
	}
internal void ShowAutomaticKingdomRebellionCompletionPopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool success, string executionMessage)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (kingdom != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_KingdomLabel", "Kingdom:") + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomRebellion_UnknownKingdom", "an unknown kingdom")));
		}
		if (!string.IsNullOrWhiteSpace(context?.StabilityTierText))
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_StabilityAtTrigger", "Stability at trigger:") + context.StabilityValue + " (" + KingdomStabilityUiText.Tier(context.StabilityValue) + ")");
		}
		if (clan != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_LeadClanLabel", "Lead clan:") + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
		}
		if (context?.NamingResult != null)
		{
			if (!success)
			{
				ReportRebelKingdomNamingFailure(context.NamingResult);
			}
			stringBuilder.AppendLine();
			AppendRebelKingdomNamingResultLines(stringBuilder, context.NamingResult);
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FollowerClans", "Coalition clans:"));
			stringBuilder.AppendLine("- " + string.Join(Ui("AF_UI_KingdomRebellion_ClanSeparator", ", "), followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_ExecutionResult", "Execution result:"));
		stringBuilder.AppendLine((executionMessage ?? "").Trim());
		if (_automatic().PendingCount > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_PendingAutomatic", "There are still {COUNT} automatic rebellions pending. Click Continue to open the next one.", "COUNT", _automatic().PendingCount.ToString()));
		}
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(success ? Ui("AF_UI_KingdomRebellion_AutomaticCompleted", "Automatic rebellion completed") : Ui("AF_UI_KingdomRebellion_AutomaticFailed", "Automatic rebellion failed"), stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Ui("AF_UI_KingdomRebellion_Continue", "Continue"), "", delegate
		{
			_runtime().ContinueAutomaticKingdomRebellionFlow();
		}, null), pauseGameActiveState: true);
	}
internal void ShowAutomaticKingdomRebellionNamingFailurePopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_AutomaticNamingFailedBody", "This week's automatic rebellion was triggered, but the rebel kingdom name was not generated. The system will not continue with a local fallback name."));
		stringBuilder.AppendLine();
		if (kingdom != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_KingdomLabel", "Kingdom:") + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomRebellion_UnknownKingdom", "an unknown kingdom")));
		}
		if (clan != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_LeadClanLabel", "Lead clan:") + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FollowerClansInline", "Coalition clans:") + string.Join(", ", followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		ReportRebelKingdomNamingFailure(context?.NamingResult);
		AppendRebelKingdomNamingResultLines(stringBuilder, context?.NamingResult);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(afterApiRepair ? Ui("AF_UI_KingdomRebellion_ApiRepairReturnedAutomatic", "The API setup flow returned. Generate the rebel kingdom name again, or skip this automatic rebellion.") : Ui("AF_UI_KingdomRebellion_ApiRepairRequiredAutomatic", "Fill in the event/rebellion API settings first. After fixing them, return here to generate the name again."));
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(afterApiRepair ? Ui("AF_UI_KingdomRebellion_RetryNaming", "Retry rebel kingdom naming") : Ui("AF_UI_KingdomRebellion_NamingFailedTitle", "Rebel kingdom naming failed"), stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, afterApiRepair ? Ui("AF_UI_KingdomRebellion_RegenerateName", "Generate name again") : Ui("AF_UI_KingdomRebellion_AdjustApi", "Adjust API settings"), Ui("AF_UI_KingdomRebellion_Skip", "Skip this"), delegate
		{
			if (afterApiRepair)
			{
				_runtime().RetryAutomaticKingdomRebellionNamingAsync(context);
			}
			else
			{
				BlockedAutomaticContext = context;
				BlockedDevContext = null;
				OpenKingdomRebellionApiRepairFlow();
			}
		}, delegate
		{
			if (ReferenceEquals(BlockedAutomaticContext, context))
			{
				BlockedAutomaticContext = null;
			}
			ReopenAfterApiConfig = false;
			KingdomRebellionRuntimeController.NotifyCivilWarRebellionFailed(context, Ui("AF_UI_KingdomRebellion_ReasonNamingSkipped", "the rebel kingdom naming was skipped"));
			_runtime().ContinueAutomaticKingdomRebellionFlow();
		}), pauseGameActiveState: true);
	}
internal static void AppendRebelKingdomNamingResultLines(StringBuilder stringBuilder, RebelKingdomNamingResult namingResult)
	{
		if (stringBuilder == null)
		{
			return;
		}
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_NamingResult", "Naming result:"));
		if (KingdomRebellionRuntimeController.IsRebelKingdomNamingSuccess(namingResult))
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FormalName", "- Formal name:") + " " + ((namingResult.FormalName ?? "").Trim()));
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_ShortName", "- Short name:") + " " + ((namingResult.ShortName ?? "").Trim()));
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_Source", "- Source: LLM"));
			if (!string.IsNullOrWhiteSpace(namingResult.EncyclopediaText))
			{
				stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_Encyclopedia", "- Encyclopedia:") + " " + namingResult.EncyclopediaText.Trim());
			}
			return;
		}
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_StatusFailed", "- Status: failed"));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_Attempts", "- Automatic retries:") + " " + Math.Max(namingResult?.AttemptsUsed ?? 0, 0) + "/" + RebelKingdomNamingMaxAttempts);
		if (namingResult?.IsRequestsPerMinuteLimit == true)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_RpmLimit", "- Rate-limit check: possible RPM limit"));
		}
		else if (namingResult?.IsQuotaLimit == true)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_QuotaLimit", "- Rate-limit check: possible quota or balance limit"));
		}
		else if (namingResult?.IsRateLimit == true)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_RateLimit", "- Rate-limit check: endpoint rate limit"));
		}
		if (namingResult?.RetryAfterSeconds != null)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_RetryAfter", "- Endpoint suggested wait:") + " " + namingResult.RetryAfterSeconds.Value + " seconds");
		}
		if (!string.IsNullOrWhiteSpace(namingResult?.FailureReason))
		{
			// 避免完整模型/API 响应把重试或修复按钮推出可视区域。
			stringBuilder.AppendLine(Ui("AF_UI_KingdomRebellion_FailureDetailsShown", "- The last failure details were shown in the lower-left message and written to the log."));
		}
	}
internal static void ReportRebelKingdomNamingFailure(RebelKingdomNamingResult namingResult)
	{
		string failureReason = (namingResult?.FailureReason ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(failureReason))
		{
			// 仅在失败 UI 已经准备显示时报告一次，避免后台重试过程反复打断玩家。
			NonBlockingErrorReport.Show(Ui("AF_UI_KingdomRebellion_NamingFailedTitle", "Rebel kingdom naming failed"), failureReason,
				true, NamingFailureDisplay(failureReason));
		}
	}

	internal static string NamingFailureDisplay(string raw)
	{
		string text = raw ?? "";
		if (text.StartsWith(KingdomRebellionRuntimeController.ForcedNamingFailurePrefix, StringComparison.Ordinal))
			return Ui("AF_UI_KingdomRebellion_NamingForcedBackgroundFailurePrefix", "Background forced-rebellion kingdom naming task failed: ") + text.Substring(KingdomRebellionRuntimeController.ForcedNamingFailurePrefix.Length);
		if (text.StartsWith(KingdomRebellionRuntimeController.NamingRetryFailurePrefix, StringComparison.Ordinal))
			return Ui("AF_UI_KingdomRebellion_NamingRetryBackgroundFailurePrefix", "Background rebel kingdom naming retry failed: ") + text.Substring(KingdomRebellionRuntimeController.NamingRetryFailurePrefix.Length);
		if (text.StartsWith(KingdomRebellionRuntimeController.AutomaticNamingFailurePrefix, StringComparison.Ordinal))
			return Ui("AF_UI_KingdomRebellion_NamingBackgroundFailurePrefix", "Background rebel kingdom naming task failed: ") + text.Substring(KingdomRebellionRuntimeController.AutomaticNamingFailurePrefix.Length);
		return text;
	}
}
