using System; using System.Collections.Generic; using System.Linq; using System.Text; using System.Threading.Tasks;
using TaleWorlds.CampaignSystem; using TaleWorlds.Core; using TaleWorlds.Library;
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
			InformationManager.DisplayMessage(new InformationMessage("未找到 API 配置引导，请先检查 MCM 中的事件/叛乱API Base URL、API Key 与模型名。"));
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
			InformationManager.DisplayMessage(new InformationMessage("王国稳定度与叛乱功能已在 MCM 中关闭，已取消本次强制叛乱。"));
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
			text = "叛乱命名已完成，但目标王国或家族状态已变化，无法继续执行。";
		}
		else if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			flag = false;
			text = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + " 当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，本次强制稳定度叛乱已跳过。";
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
			stringBuilder.AppendLine("联合响应家族：");
			stringBuilder.AppendLine("- " + string.Join("、", list.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("执行结果：");
		stringBuilder.AppendLine((text ?? "").Trim());
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(flag ? "强制叛乱执行完成" : "强制叛乱执行失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回详情", "", delegate
		{
			_openStabilityDetail(kingdom ?? MemoryEntityIdentityBannerlordAdapter.FindKingdomById(pendingDevForcedKingdomRebellionContext.KingdomId));
		}, null));
	}
internal void ShowDevForcedKingdomRebellionNamingFailurePopup(PendingDevForcedKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("强制叛乱已准备执行，但叛乱建国命名没有成功。系统不会使用本地国名继续建国。");
		stringBuilder.AppendLine();
		if (kingdom != null)
		{
			stringBuilder.AppendLine("王国：" + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "某王国"));
		}
		if (clan != null)
		{
			stringBuilder.AppendLine("主导家族：" + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine("联合响应家族：" + string.Join("、", followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		ReportRebelKingdomNamingFailure(context?.NamingResult);
		AppendRebelKingdomNamingResultLines(stringBuilder, context?.NamingResult);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(afterApiRepair ? "API 配置流程已返回。请重新生成叛乱建国命名，或返回王国稳定度详情。" : "请先重新填写事件/叛乱API信息。修正后可回到这里重新生成命名。");
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(afterApiRepair ? "重试叛乱建国命名" : "叛乱建国命名失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, afterApiRepair ? "重新生成命名" : "调整API信息", "返回详情", delegate
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
			InformationManager.DisplayMessage(new InformationMessage("叛乱命名重试前目标王国或家族状态已变化，无法继续执行。"));
			_openStabilityDetail(kingdom);
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			InformationManager.DisplayMessage(new InformationMessage("该王国当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，不能重试强制稳定度叛乱。"));
			_openStabilityDetail(kingdom);
			return;
		}
		StartDevForcedKingdomRebellionAsync(kingdom, clan, context.WeekIndex, context.RelationToKing, context.TownCount, context.CastleCount, list);
	}
internal void StartDevForcedKingdomRebellionAsync(Kingdom kingdom, Clan clan, int weekIndex, int relationToKing, int townCount, int castleCount, List<Clan> followerClans)
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			InformationManager.DisplayMessage(new InformationMessage("王国稳定度与叛乱功能已在 MCM 中关闭，不能启动强制叛乱。"));
			return;
		}
		if (DevForcedInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前已有一条强制叛乱任务正在后台运行，请稍候。"));
			return;
		}
		if (kingdom == null || clan == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法启动强制叛乱：找不到目标王国或家族。"));
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			InformationManager.DisplayMessage(new InformationMessage("该王国当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，不能启动强制稳定度叛乱。"));
			return;
		}
		List<Clan> list = followerClans?.Where((Clan x) => x != null && x != clan).GroupBy((Clan x) => MemoryEntityIdentityBannerlordAdapter.GetClanId(x), StringComparer.OrdinalIgnoreCase).Select((IGrouping<string, Clan> x) => x.First()).ToList() ?? new List<Clan>();
		string[] rebellionExistingNames = KingdomRebellionRuntimeController.CaptureRebellionExistingNames();
		_runtime().BuildRebelKingdomNamingRequest(clan, kingdom, weekIndex, list, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		DevForcedInProgress = true;
		PendingDevReady = false;
		PendingDevContext = null;
		InformationManager.ShowInquiry(new InquiryData("正在生成叛乱建国命名", "系统正在后台请求 LLM 为这次叛乱生成新王国的名称与百科简介。\n\n这一步完成后，才会真正执行家族反出与建国。\n请稍候，结果完成后会自动弹出。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
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
				namingResult = KingdomRebellionRuntimeController.BuildFailedRebelKingdomNamingResult("强制叛乱建国命名后台任务异常：" + ex.Message);
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
			stringBuilder.AppendLine("王国：" + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "某王国"));
		}
		if (!string.IsNullOrWhiteSpace(context?.StabilityTierText))
		{
			stringBuilder.AppendLine("触发时稳定度：" + context.StabilityValue + "（" + context.StabilityTierText + "）");
		}
		if (clan != null)
		{
			stringBuilder.AppendLine("主导家族：" + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
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
			stringBuilder.AppendLine("联合响应家族：");
			stringBuilder.AppendLine("- " + string.Join("、", followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("执行结果：");
		stringBuilder.AppendLine((executionMessage ?? "").Trim());
		if (_automatic().PendingCount > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("后续仍有 " + _automatic().PendingCount + " 场自动叛乱待处理。点击“继续”后将进入下一场。");
		}
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(success ? "自动叛乱执行完成" : "自动叛乱执行失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "继续", "", delegate
		{
			_runtime().ContinueAutomaticKingdomRebellionFlow();
		}, null), pauseGameActiveState: true);
	}
internal void ShowAutomaticKingdomRebellionNamingFailurePopup(PendingAutomaticKingdomRebellionContext context, Kingdom kingdom, Clan clan, List<Clan> followerClans, bool afterApiRepair)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("本周自动叛乱已命中，但叛乱建国命名没有成功。系统不会使用本地国名继续建国。");
		stringBuilder.AppendLine();
		if (kingdom != null)
		{
			stringBuilder.AppendLine("王国：" + MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "某王国"));
		}
		if (clan != null)
		{
			stringBuilder.AppendLine("主导家族：" + MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan));
		}
		if (followerClans != null && followerClans.Count > 0)
		{
			stringBuilder.AppendLine("联合响应家族：" + string.Join("、", followerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName)));
		}
		stringBuilder.AppendLine();
		ReportRebelKingdomNamingFailure(context?.NamingResult);
		AppendRebelKingdomNamingResultLines(stringBuilder, context?.NamingResult);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(afterApiRepair ? "API 配置流程已返回。请重新生成叛乱建国命名，或跳过本次自动叛乱。" : "请先重新填写事件/叛乱API信息。修正后可回到这里重新生成命名。");
		InformationManager.HideInquiry();
		InformationManager.ShowInquiry(new InquiryData(afterApiRepair ? "重试叛乱建国命名" : "叛乱建国命名失败", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, afterApiRepair ? "重新生成命名" : "调整API信息", "跳过本次", delegate
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
			KingdomRebellionRuntimeController.NotifyCivilWarRebellionFailed(context, "跳过了叛乱建国命名");
			_runtime().ContinueAutomaticKingdomRebellionFlow();
		}), pauseGameActiveState: true);
	}
internal static void AppendRebelKingdomNamingResultLines(StringBuilder stringBuilder, RebelKingdomNamingResult namingResult)
	{
		if (stringBuilder == null)
		{
			return;
		}
		stringBuilder.AppendLine("命名结果：");
		if (KingdomRebellionRuntimeController.IsRebelKingdomNamingSuccess(namingResult))
		{
			stringBuilder.AppendLine("- 正式名：" + ((namingResult.FormalName ?? "").Trim()));
			stringBuilder.AppendLine("- 简称：" + ((namingResult.ShortName ?? "").Trim()));
			stringBuilder.AppendLine("- 来源：LLM");
			if (!string.IsNullOrWhiteSpace(namingResult.EncyclopediaText))
			{
				stringBuilder.AppendLine("- 百科简介：" + namingResult.EncyclopediaText.Trim());
			}
			return;
		}
		stringBuilder.AppendLine("- 状态：失败");
		stringBuilder.AppendLine("- 自动重试次数：" + Math.Max(namingResult?.AttemptsUsed ?? 0, 0) + "/" + RebelKingdomNamingMaxAttempts);
		if (namingResult?.IsRequestsPerMinuteLimit == true)
		{
			stringBuilder.AppendLine("- 限流判断：疑似 RPM 超限");
		}
		else if (namingResult?.IsQuotaLimit == true)
		{
			stringBuilder.AppendLine("- 限流判断：疑似额度/余额不足");
		}
		else if (namingResult?.IsRateLimit == true)
		{
			stringBuilder.AppendLine("- 限流判断：接口限流");
		}
		if (namingResult?.RetryAfterSeconds != null)
		{
			stringBuilder.AppendLine("- 接口建议等待：" + namingResult.RetryAfterSeconds.Value + " 秒");
		}
		if (!string.IsNullOrWhiteSpace(namingResult?.FailureReason))
		{
			// 避免完整模型/API 响应把重试或修复按钮推出可视区域。
			stringBuilder.AppendLine("- 最后一次失败详情已显示在左下角消息并写入日志。");
		}
	}
internal static void ReportRebelKingdomNamingFailure(RebelKingdomNamingResult namingResult)
	{
		string failureReason = (namingResult?.FailureReason ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(failureReason))
		{
			// 仅在失败 UI 已经准备显示时报告一次，避免后台重试过程反复打断玩家。
			NonBlockingErrorReport.Show("叛乱建国命名失败", failureReason);
		}
	}
}
