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
		string text3 = (list2.Count > 0) ? string.Join("、", list2.Select(MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName)) : "无";
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
			CultureName = (clan?.Culture?.Name?.ToString() ?? "").Trim(), KingdomName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(oldKingdom, "原王国"),
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
			(attempt, total, result) => Logger.LogEventPromptExchange((logTarget ?? "叛乱建国命名") + " [尝试 " + attempt + "/" + total + "]", "【System Prompt】\n" + (systemPrompt ?? "") + "\n\n【User Prompt】\n" + (userPrompt ?? ""), result?.Success == true ? result.Content ?? "" : ("错误: " + (result?.ErrorMessage ?? "未知错误"))),
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
			return BuildFailedRebelKingdomNamingResult("主导家族为空，无法请求命名。");
		}
		string[] rebellionExistingNames = CaptureRebellionExistingNames();
		BuildRebelKingdomNamingRequest(clan, oldKingdom, weekIndex, followerClans, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		return GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, "叛乱建国命名 - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan), maxAttempts, rebellionExistingNames);
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
		string text = attempts > 0 ? ("叛乱建国命名已连续自动重试 " + attempts + "/" + RebelKingdomNamingMaxAttempts + " 次仍失败，已中止本次建国。") : "叛乱建国命名未能完成，已中止本次建国。";
		string failureReason = (namingResult?.FailureReason ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(failureReason))
		{
			// 原始 API 响应可能很长；交互窗口只保留恢复动作，详情由左下角通知和日志承载。
			text += " 最后一次失败详情已显示在左下角消息并写入日志。";
		}
		return text + " 请重新填写事件/叛乱API信息后再试。";
	}

internal KingdomRebellionResolutionResult ResolveKingdomRebellion(Kingdom kingdom, int weekIndex, bool executeAction, bool forceTrigger)
	{
		KingdomRebellionResolutionResult kingdomRebellionResolutionResult = new KingdomRebellionResolutionResult
		{
			Kingdom = kingdom,
			WeekIndex = weekIndex,
			Forced = forceTrigger
		};
		if (kingdom == null)
		{
			kingdomRebellionResolutionResult.Message = "找不到目标王国。";
			return kingdomRebellionResolutionResult;
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			kingdomRebellionResolutionResult.Message = "王国稳定度与叛乱功能已在 MCM 中关闭，跳过叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		int kingdomStabilityValue = _stability().GetKingdomStabilityValue(kingdom);
		kingdomRebellionResolutionResult.StabilityValue = kingdomStabilityValue;
		kingdomRebellionResolutionResult.StabilityTierText = KingdomStabilityGameAdapter.GetKingdomStabilityTierText(kingdomStabilityValue);
		kingdomRebellionResolutionResult.TriggerChance = KingdomStabilityPolicy.GetKingdomRebellionWeeklyChance(kingdomStabilityValue);
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			kingdomRebellionResolutionResult.TriggerChance = 0f;
			kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，跳过本模组稳定度叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		if (kingdom.IsEliminated)
		{
			kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "已灭亡，跳过叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		if (kingdom.Leader == null)
		{
			kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "当前缺少有效领袖，跳过叛乱判定。";
			return kingdomRebellionResolutionResult;
		}
		var captured = new Dictionary<Clan, RebellionClanFacts>();
		kingdomRebellionResolutionResult.Candidates = _game().EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false, captured: captured);
		foreach (KingdomRebellionCandidateInfo item in kingdomRebellionResolutionResult.Candidates.Where((KingdomRebellionCandidateInfo x) => x != null && x.Clan != null))
		{
			item.PreviewFollowerClanNames = _game().EvaluateKingdomRebellionFollowers(kingdom, item.Clan, forceTrigger: false, captured: captured).Where((KingdomRebellionFollowerInfo x) => x != null && x.Eligible && x.Clan != null).Select((KingdomRebellionFollowerInfo x) => MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(x.Clan)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		if (!forceTrigger)
		{
			if (kingdomRebellionResolutionResult.TriggerChance <= 0f)
			{
				kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "当前稳定度为“" + kingdomRebellionResolutionResult.StabilityTierText + "”，本周没有叛乱概率。";
				return kingdomRebellionResolutionResult;
			}
			bool passed = RebellionRules.PassChance(false, kingdomRebellionResolutionResult.TriggerChance, () => MBRandom.RandomFloat, out float randomFloat);
			kingdomRebellionResolutionResult.Roll = randomFloat;
			kingdomRebellionResolutionResult.PassedChanceGate = passed;
			if (!kingdomRebellionResolutionResult.PassedChanceGate)
			{
				kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "本周叛乱抽签未命中。当前档位“" + kingdomRebellionResolutionResult.StabilityTierText + "”，概率 " + KingdomStabilityGameAdapter.FormatKingdomRebellionChance(kingdomRebellionResolutionResult.TriggerChance) + "，本次掷值 " + randomFloat.ToString("0.000") + "。";
				return kingdomRebellionResolutionResult;
			}
		}
		else
		{
			kingdomRebellionResolutionResult.PassedChanceGate = true;
		}
		KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = RebellionRules.FirstEligible(kingdomRebellionResolutionResult.Candidates, x => x != null && x.Eligible && x.Clan != null);
		if (kingdomRebellionCandidateInfo == null)
		{
			kingdomRebellionResolutionResult.Message = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + "当前没有满足条件的带城叛乱候选家族。";
			return kingdomRebellionResolutionResult;
		}
		kingdomRebellionResolutionResult.SelectedClan = kingdomRebellionCandidateInfo.Clan;
		kingdomRebellionResolutionResult.FollowerCandidates = _game().EvaluateKingdomRebellionFollowers(kingdom, kingdomRebellionCandidateInfo.Clan, forceTrigger: false, captured: captured);
		kingdomRebellionResolutionResult.SelectedFollowerClans = kingdomRebellionResolutionResult.FollowerCandidates.Where((KingdomRebellionFollowerInfo x) => x != null && x.Eligible && x.Clan != null).Select((KingdomRebellionFollowerInfo x) => x.Clan).ToList();
		if (!executeAction)
		{
			string text = (kingdomRebellionResolutionResult.SelectedFollowerClans.Count > 0) ? ("；预计跟随家族 " + string.Join("、", kingdomRebellionResolutionResult.SelectedFollowerClans.Select(MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName))) : "；当前没有会联合响应的其他家族。";
			kingdomRebellionResolutionResult.Message = "当前最可能反出的家族是 " + kingdomRebellionCandidateInfo.ClanName + "（关系 " + kingdomRebellionCandidateInfo.RelationToKing + "，评分 " + kingdomRebellionCandidateInfo.Score.ToString("0.0") + "）" + text;
			return kingdomRebellionResolutionResult;
		}
		kingdomRebellionResolutionResult.Executed = TryExecuteKingdomRebellion(kingdomRebellionCandidateInfo.Clan, kingdom, weekIndex, forceTrigger, kingdomRebellionResolutionResult.SelectedFollowerClans, out var message);
		kingdomRebellionResolutionResult.Message = message;
		return kingdomRebellionResolutionResult;
	}

internal bool TryExecuteKingdomRebellion(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, List<Clan> followerClans, out string message)
	{
		message = "";
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			message = "执行叛乱失败：该王国当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫。";
			return false;
		}
		if (!_game().TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger: true, out var note, out var relationToKing, out var townCount, out var castleCount))
		{
			message = "执行叛乱失败：" + note;
			return false;
		}
		try
		{
			RebelKingdomNamingResult rebelKingdomNamingResult = GenerateRebelKingdomNaming(clan, kingdom, weekIndex, followerClans);
			return TryExecuteKingdomRebellionWithNaming(clan, kingdom, weekIndex, forceTrigger, relationToKing, townCount, castleCount, rebelKingdomNamingResult, followerClans, out message);
		}
		catch (Exception ex)
		{
			message = "执行叛乱失败：" + ex.Message;
			Logger.Log("KingdomRebellion", "[ERROR] TryExecuteKingdomRebellion failed: " + ex);
			return false;
		}
	}

internal bool TryExecuteKingdomRebellionWithNaming(Clan clan, Kingdom kingdom, int weekIndex, bool forceTrigger, int relationToKing, int townCount, int castleCount, RebelKingdomNamingResult rebelKingdomNamingResult, List<Clan> followerClans, out string message)
	{
		message = "";
		string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国");
		string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan);
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			message = kingdomDisplayName + " 当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，本次稳定度叛乱已跳过。";
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
			message = "叛乱建国命名结果无效或与现有王国重名，已中止建国。请重新填写事件/叛乱API信息后再试。";
			Logger.Log("KingdomRebellion", "[ERROR] Rebel kingdom creation blocked by invalid naming result. clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " formal=" + text + " short=" + text2);
			return false;
		}
		if (Campaign.Current?.KingdomManager == null || clan == null || clan.Kingdom != kingdom || clan.IsEliminated || clan.Leader == null || !clan.Leader.IsAlive)
		{
			message = "建国前条件已变化，未离开原王国。";
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
			message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，但当前未找到 KingdomManager，未能继续建立新王国。";
			Logger.Log("KingdomRebellion", "[ERROR] KingdomManager unavailable after rebellion leave. clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan));
			return true;
		}
		kingdomManager.CreateKingdom(new TextObject(text, null), new TextObject(text2, null), clan.Culture ?? kingdom.Culture, clan, null, new TextObject(text3, null), new TextObject(text, null), null);
		Kingdom kingdom2 = clan.Kingdom;
		if (kingdom2 == null || kingdom2 == kingdom || kingdom2.IsEliminated)
		{
			message = "家族已离开原王国，但建国结果尚未确认。";
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
		string clanFortificationSummary = KingdomStabilityGameAdapter.BuildClanFortificationSummary(clan);
		message = clanDisplayName + " 已从 " + kingdomDisplayName + " 反出，并建立了 " + text + "。";
		if (!string.IsNullOrWhiteSpace(clanFortificationSummary))
		{
			message = message + " " + clanFortificationSummary;
		}
		if (list.Count > 0)
		{
			message = message + " 联合响应家族：" + string.Join("、", list) + "。";
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
			Logger.Log("KingdomRebellion", "[SKIP] 王国稳定度与叛乱功能已在 MCM 中关闭，跳过 week=" + weekIndex + " 的自动叛乱判定。");
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
								if (TeamModuleServices.CivilWar.HasTrackedKingdom(devEditableKingdom) || TeamModuleServices.CivilWar.BlocksNewOffensiveWar(devEditableKingdom))
							{
								return _pump.Maintenance().Complete && CompleteWeeklyKingdomRebellionMaintenance(weekIndex);
							}
							KingdomRebellionResolutionResult kingdomRebellionResolutionResult = ResolveKingdomRebellion(devEditableKingdom, weekIndex, executeAction: false, forceTrigger: false);
						if (kingdomRebellionResolutionResult != null && kingdomRebellionResolutionResult.PassedChanceGate && kingdomRebellionResolutionResult.SelectedClan != null)
						{
							QueueAutomaticKingdomRebellion(kingdomRebellionResolutionResult);
						}
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
			NotifyCivilWarRebellionFailed(pendingAutomaticKingdomRebellionContext, "目标王国或家族状态已变化");
			_pump.CompletionPopup(pendingAutomaticKingdomRebellionContext, kingdom, clan, list, false, "本周自动叛乱已命中，但目标王国或家族状态已变化，无法继续执行。");
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			Logger.Log("KingdomRebellion", "[SKIP] player kingdom stability rebellion immunity blocked queued automatic rebellion before naming. kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " clan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan));
			NotifyCivilWarRebellionFailed(pendingAutomaticKingdomRebellionContext, "玩家王国稳定度叛乱免疫");
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
		InformationManager.ShowInquiry(new InquiryData("正在生成叛乱建国命名", "系统正在为本周自动叛乱生成新王国的名称与百科简介。\n\n这一步完成前不会继续本轮自动叛乱与周报流程。\n请稍候，结果完成后会自动弹出。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string logTarget = "自动叛乱建国命名 - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult;
			try
			{
				namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, RebelKingdomNamingMaxAttempts, rebellionExistingNames);
			}
			catch (Exception ex)
			{
				namingResult = BuildFailedRebelKingdomNamingResult("叛乱建国命名后台任务异常：" + ex.Message);
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
			executionMessage = "叛乱命名已完成，但目标王国或家族状态已变化，无法继续执行。";
		}
		else if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			success = false;
			executionMessage = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + " 当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，本次自动稳定度叛乱已跳过。";
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
			success = TryExecuteKingdomRebellionWithNaming(clan, kingdom, pendingAutomaticKingdomRebellionContext.WeekIndex, forceTrigger: false, pendingAutomaticKingdomRebellionContext.RelationToKing, pendingAutomaticKingdomRebellionContext.TownCount, pendingAutomaticKingdomRebellionContext.CastleCount, pendingAutomaticKingdomRebellionContext.NamingResult, list, out executionMessage);
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
		string oldName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(oldKingdom, "原王国");
		string fallback = RebellionNamingRules.NormalizeName(clanName + "同盟", 24);
		string text = "你的派系即将脱离" + oldName + "，以" + clanName + "家族为首建立新王国。\n\n请输入新王国的名称（2~24 个字，不能与现有王国重名）。取消则使用“" + fallback + "”。";
		Func<string, Tuple<bool, string>> condition = input =>
		{
			string name = RebellionNamingRules.NormalizeName(input ?? "", 24);
			if (name.Length < 2) return new Tuple<bool, string>(false, "名称至少 2 个字。");
			if (taken.Contains(name)) return new Tuple<bool, string>(false, "已有同名王国。");
			return new Tuple<bool, string>(true, "");
		};
		void Complete(string input)
		{
			if (!SaveRuntimeGuard.IsCurrentGeneration(runtimeGeneration) || !_pump.Automatic().CompleteNaming(namingRequestVersion, context)) return;
			string formal = RebellionNamingRules.NormalizeName(input ?? "", 24);
			if (formal.Length < 2 || taken.Contains(formal)) formal = taken.Contains(fallback) ? RebellionNamingRules.NormalizeName(clanName + "自立同盟", 24) : fallback;
			string shortName = RebellionNamingRules.NormalizeName(formal, 14);
			context.NamingResult = new RebelKingdomNamingResult
			{
				FormalName = formal,
				ShortName = shortName,
				EncyclopediaText = formal + "由" + clanName + "家族在反抗" + oldName + "的内战中建立。",
				Success = true,
				AttemptsUsed = 0
			};
			Logger.Log("KingdomRebellion", "[PLAYER_NAMING] faction=" + (context.CivilWarFactionId ?? "") + " formal=" + formal);
		}
		InformationManager.ShowTextInquiry(new TextInquiryData("为新王国命名", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "建国", "使用默认名", Complete, () => Complete(""), shouldInputBeObfuscated: false, condition, "", fallback), pauseGameActiveState: true);
	}

internal static bool IsStaleCivilWarRebellion(PendingAutomaticKingdomRebellionContext context)
	{
		string factionId = context?.CivilWarFactionId;
		if (string.IsNullOrWhiteSpace(factionId) || TeamModuleServices.CivilWar.IsRebellionRequestActive(factionId)) return false;
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
			NotifyCivilWarRebellionFailed(context, "目标王国或家族状态已变化");
			_pump.CompletionPopup(context, kingdom, clan, list, false, "叛乱命名重试前目标王国或家族状态已变化，无法继续执行。");
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			NotifyCivilWarRebellionFailed(context, "玩家王国稳定度叛乱免疫");
			_pump.CompletionPopup(context, kingdom, clan, list, false, MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国") + " 当前由玩家作为国王统治，且 MCM 已开启玩家王国稳定度叛乱免疫，本次自动稳定度叛乱已跳过。");
			return;
		}
		string[] rebellionExistingNames = CaptureRebellionExistingNames();
		BuildRebelKingdomNamingRequest(clan, kingdom, context.WeekIndex, list, out var systemPrompt, out var userPrompt, rebellionExistingNames);
		long namingRequestVersion = _pump.Automatic().BeginNaming();
		InformationManager.ShowInquiry(new InquiryData("正在重新生成叛乱建国命名", "系统正在按修正后的事件/叛乱API配置重新请求新王国名称与百科简介。\n\n这一步完成前不会继续本轮自动叛乱与周报流程。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string logTarget = "自动叛乱建国命名重试 - " + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan);
		Task.Run(delegate
		{
			RebelKingdomNamingResult namingResult;
			try
			{
				namingResult = GenerateRebelKingdomNamingFromPrompts(systemPrompt, userPrompt, logTarget, RebelKingdomNamingMaxAttempts, rebellionExistingNames);
			}
			catch (Exception ex)
			{
				namingResult = BuildFailedRebelKingdomNamingResult("叛乱建国命名重试后台任务异常：" + ex.Message);
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
