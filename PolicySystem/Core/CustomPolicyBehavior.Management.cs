using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	public void OnEngineTick()
	{
		CustomPolicyComposePopup.ProcessDeferredCloseAction();
		LocalPolicyComposePopup.ProcessDeferredCloseAction();
		while (MainThreadActions.TryDequeue(out var action))
		{
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Log("main thread action failed: " + ex);
			}
		}
		if (_activePolicyEffects.Count == 0 && _pendingActivePolicyEffectWork.Count == 0)
		{
			return;
		}
		int currentDay = GetCurrentCampaignDay();
		EnsureActivePolicyEffectWorkScheduled(currentDay);
		if (_pendingActivePolicyEffectWork.Count > 0)
		{
			using (PerfProbe.Scope("CustomPolicy.ProcessActivePolicyEffects"))
			{
				ProcessActivePolicyEffects(currentDay);
			}
		}
	}

	public static void OpenFromTerminal()
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("自定义政策功能尚未初始化。", Colors.Red));
			return;
		}
		behavior.OpenComposePopup();
	}

	public static void OpenRecordHistoryFromTerminal(Action onClose = null)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData("政策记录", "自定义政策功能尚未初始化。", true, false, "返回", "", onClose, null), pauseGameActiveState: true);
			return;
		}
		behavior.OpenRecordHistoryPopup(onClose);
	}

	internal static void OpenLocalPolicyManagementFromTerminal(Action onClose = null)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策", "地方政策功能尚未初始化。", true, false, "返回", "", onClose, null), pauseGameActiveState: true);
			return;
		}
		behavior.OpenLocalPolicyManagementPopup(onClose);
	}

	private void OpenLocalPolicyManagementPopup(Action onClose)
	{
		bool hasFief = GetPlayerOwnedLocalPolicyFiefs().Count > 0;
		List<InquiryElement> items = new List<InquiryElement>
		{
			new InquiryElement("publish_local", "发布地方政策", null, isEnabled: hasFief, hasFief ? "选择玩家家族拥有的城镇或城堡作为作用封地。" : "玩家家族当前没有城镇或城堡，无法发布。"),
			new InquiryElement("local_records", "地方政策记录", null, isEnabled: true, "查看政策状态、目标、效果剩余天数、费用和延长历史，并可延长效果或废除政策。")
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("地方政策", "地方政策由智能服务独立评议，成功后立即结算并在所选封地范围生效。", items, isExitShown: true, 1, 1, "确定", "返回", delegate(List<InquiryElement> selected)
		{
			if (selected == null || selected.Count == 0)
			{
				onClose?.Invoke();
				return;
			}
			string id = selected[0].Identifier as string;
			if (string.Equals(id, "publish_local", StringComparison.Ordinal))
			{
				OpenLocalPolicyComposePopup(() => OpenLocalPolicyManagementPopup(onClose));
			}
			else if (string.Equals(id, "local_records", StringComparison.Ordinal))
			{
				OpenLocalPolicyHistoryPopup(() => OpenLocalPolicyManagementPopup(onClose));
			}
			else
			{
				onClose?.Invoke();
			}
		}, delegate(List<InquiryElement> _)
		{
			onClose?.Invoke();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	private void OpenLocalPolicyComposePopup(Action onCancel)
	{
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage("上一份政策仍在等待评议，请稍候。", Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		List<Settlement> fiefs = GetPlayerOwnedLocalPolicyFiefs();
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, fiefs.Count > 0);
		LocalPolicyComposeData data = new LocalPolicyComposeData
		{
			DateText = FormatCurrentCampaignDate(),
			CanPublish = eligibility.CanPublish,
			BlockReason = eligibility.CanPublish ? "请选择作用封地并填写政策。" : eligibility.Reason,
			Fiefs = fiefs.Select(BuildLocalPolicyFiefUiData).ToList()
		};
		if (!LocalPolicyComposePopup.Show(data, SubmitLocalPolicyFromPopup, RequestPlayerPolicyAutoDraft, onCancel))
		{
			InformationManager.DisplayMessage(new InformationMessage("打开地方政策发布界面失败。", Colors.Red));
		}
	}

	private void SubmitLocalPolicyFromPopup(string policyName, string policyContent, string durationText, string capturedDateText, List<string> selectedFiefIds)
	{
		policyName = NormalizePolicyName(policyName);
		policyContent = NormalizePolicyContent(policyContent);
		selectedFiefIds = NormalizeIdList(selectedFiefIds);
		int manualDurationDays = 0;
		if (!string.IsNullOrWhiteSpace(durationText)
			&& (!int.TryParse(durationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out manualDurationDays) || manualDurationDays <= 0))
		{
			InformationManager.DisplayMessage(new InformationMessage("持续天数必须留空或填写正整数。", Colors.Yellow));
			OpenLocalPolicyComposePopup(null);
			return;
		}
		if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(policyContent) || selectedFiefIds.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("请填写政策名、政策内容并至少选择一个封地。", Colors.Yellow));
			OpenLocalPolicyComposePopup(null);
			return;
		}
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage("上一份政策仍在等待评议，请稍候。", Colors.Yellow));
			return;
		}
		List<Settlement> validFiefs = ResolveOwnedLocalPolicyFiefs(selectedFiefIds);
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, validFiefs.Count > 0);
		if (!eligibility.CanPublish)
		{
			InformationManager.DisplayMessage(new InformationMessage(eligibility.Reason, Colors.Yellow));
			OpenLocalPolicyComposePopup(null);
			return;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		LocalPolicyMentionTargetSelection mentionTargets = ResolveLocalPolicyMentionTargets(policyName, policyContent, playerKingdom, validFiefs);
		PolicyTargetWorldSnapshot semanticTargetSnapshot = PolicyTargetSemanticRouter.CaptureWorldSnapshot();
		PolicyDraftRequest request = new PolicyDraftRequest
		{
			ProposerClanId = Clan.PlayerClan?.StringId ?? "",
			RequestId = Guid.NewGuid().ToString("N"),
			ScopeKind = PolicyScopeLocal,
			SelectedFiefIds = validFiefs.Select(x => x.StringId).ToList(),
			LocalMentionedClanIds = NormalizeIdList(mentionTargets.ClanIds),
			LocalMentionedSettlementIds = NormalizeIdList(mentionTargets.SettlementIds),
			LocalMentionedCurrentRulingClan = mentionTargets.FollowCurrentRulingClan,
			LocalMentionSummary = BuildLocalPolicyMentionSummary(mentionTargets),
			TargetHandles = BuildLocalPolicyTargetHandles(mentionTargets, validFiefs, playerKingdom),
			ManualDurationDays = manualDurationDays,
			PolicyName = policyName,
			PolicyContent = policyContent,
			DateText = string.IsNullOrWhiteSpace(capturedDateText) ? FormatCurrentCampaignDate() : capturedDateText,
			SubmittedDay = GetCurrentCampaignDay(),
			PlayerKingdomId = playerKingdom?.StringId ?? "",
			PlayerKingdomName = playerKingdom == null ? "" : GetKingdomName(playerKingdom),
			IssuerKingdomId = playerKingdom?.StringId ?? "",
			IssuerKingdomName = playerKingdom == null ? "" : GetKingdomName(playerKingdom),
			UseAiEvaluatedCost = options.UseAiEvaluatedCost,
			GoldCost = options.UseAiEvaluatedCost ? 0 : options.GoldCost,
			InfluenceCost = 0f,
			EvaluatorPrompt = options.EvaluatorPrompt,
			EvaluatorPromptIsDefault = options.EvaluatorPromptIsDefault,
			PublicFeedbackTargetChars = NormalizePolicyPublicFeedbackTargetChars(options.PublicFeedbackTargetChars),
			PromptContext = BuildLocalPolicyPromptContextBundle(validFiefs, playerKingdom, options, mentionTargets),
			KnowledgeMentionedEntities = BuildPolicyKnowledgeMentionedEntitiesSnapshot(policyName, policyContent, playerKingdom),
			SemanticTargetSnapshot = semanticTargetSnapshot
		};
		CaptureUnifiedPolicyHistoryForRequest(request, playerKingdom);
		request.KnowledgeContext = BuildPolicyKnowledgeContextForMainOnly(request);
		if (!TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"无法冻结本次政策评议配置：" + freezeError,
				Colors.Yellow));
			return;
		}
		_generationInProgress = true;
		ShowPolicyWaitPopupAndPause(request);
		Task.Run(async delegate
		{
			PolicyGenerationResult result = await GeneratePolicyResultAsync(request);
			MainThreadActions.Enqueue(delegate { CompletePolicyGeneration(request, result); });
		});
	}

	private void OpenComposePopup()
	{
		if (_generationInProgress)
		{
			PolicyDebugLog("open-blocked", "generation already in progress");
			InformationManager.DisplayMessage(new InformationMessage("上一份政策仍在等待评议，请稍候。", Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateEligibility(options);
		string dateText = FormatCurrentCampaignDate();
		string statusText = eligibility.CanPublish ? BuildReadyStatus(options) : eligibility.Reason;
		List<PolicyComposeTargetData> targets = BuildPolicyComposeTargets();
		bool shown = CustomPolicyComposePopup.Show(
			"发布王国政策",
			"政策名",
			"政策内容 / AI编写原文",
			dateText,
			eligibility.CanPublish,
			statusText,
			targets,
			SubmitPolicyFromPopup,
			RequestPlayerPolicyAutoDraft,
			delegate { });
		if (!shown)
		{
			PolicyDebugLog("open-failed", "CustomPolicyComposePopup.Show returned false");
			InformationManager.DisplayMessage(new InformationMessage("打开自定义政策撰写界面失败。", Colors.Red));
		}
	}

	private List<PolicyComposeTargetData> BuildPolicyComposeTargets()
	{
		List<PolicyComposeTargetData> result = new List<PolicyComposeTargetData>();
		Kingdom playerKingdom = GetPlayerKingdom();
		if (playerKingdom != null)
		{
			result.Add(new PolicyComposeTargetData
			{
				TargetId = playerKingdom.StringId ?? "",
				ScopeKind = PolicyScopeKingdom,
				DisplayText = "本国：" + GetKingdomName(playerKingdom),
				HintText = "沿用王国议程、政策成本与通过流程。",
				IsSelected = true
			});
		}
		foreach (Kingdom vassal in (IsPlayerRuler(playerKingdom)
			? VassalageBehavior.GetPlayerDirectVassalKingdomsForExternal()
			: new List<Kingdom>())
			.Where(x => x != null)
			.OrderBy(GetKingdomName, StringComparer.OrdinalIgnoreCase))
		{
			VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(vassal.StringId, out int independence, out int breakawayThreshold, out int rulerRelation, out string rulerName);
			result.Add(new PolicyComposeTargetData
			{
				TargetId = vassal.StringId ?? "",
				ScopeKind = PolicyScopeVassal,
				DisplayText = "附庸：" + GetKingdomName(vassal),
				HintText = "当前独立度 " + independence.ToString(CultureInfo.InvariantCulture) + "/100；脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSigned(rulerRelation) + "）；发布立即生效并随机增加 5–10 点独立度。"
			});
		}
		return result;
	}

	private void SubmitPolicyFromPopup(string policyName, string policyContent, string durationText, string capturedDateText, string selectedTargetId)
	{
		policyName = NormalizePolicyName(policyName);
		policyContent = NormalizePolicyContent(policyContent);
		int manualDurationDays = 0;
		if (!string.IsNullOrWhiteSpace(durationText)
			&& (!int.TryParse(durationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out manualDurationDays) || manualDurationDays <= 0))
		{
			InformationManager.DisplayMessage(new InformationMessage("持续天数必须留空或填写正整数。", Colors.Yellow));
			OpenComposePopup();
			return;
		}
		PolicyDebugLog("submit", "submit clicked nameLength=" + policyName.Length.ToString(CultureInfo.InvariantCulture)
			+ " contentLength=" + policyContent.Length.ToString(CultureInfo.InvariantCulture)
			+ " manualDurationDays=" + manualDurationDays.ToString(CultureInfo.InvariantCulture)
			+ " capturedDate=" + (capturedDateText ?? ""));
		if (string.IsNullOrWhiteSpace(policyName))
		{
			InformationManager.DisplayMessage(new InformationMessage("政策名不能为空。", Colors.Yellow));
			OpenComposePopup();
			return;
		}
		if (string.IsNullOrWhiteSpace(policyContent))
		{
			InformationManager.DisplayMessage(new InformationMessage("政策内容不能为空。", Colors.Yellow));
			OpenComposePopup();
			return;
		}
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage("上一份政策仍在等待评议，请稍候。", Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateEligibility(options);
		if (!eligibility.CanPublish)
		{
			PolicyDebugLog("submit-blocked", "eligibility failed: " + (eligibility.Reason ?? ""));
			InformationManager.DisplayMessage(new InformationMessage(eligibility.Reason, Colors.Yellow));
			OpenComposePopup();
			return;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		string targetId = (selectedTargetId ?? "").Trim();
		bool isOwnKingdomTarget = playerKingdom != null && (string.IsNullOrWhiteSpace(targetId) || string.Equals(playerKingdom.StringId ?? "", targetId, StringComparison.OrdinalIgnoreCase));
		Kingdom policyKingdom = isOwnKingdomTarget
			? playerKingdom
			: VassalageBehavior.GetPlayerDirectVassalKingdomsForExternal().FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", targetId, StringComparison.OrdinalIgnoreCase));
		bool isVassalTarget = !isOwnKingdomTarget && policyKingdom != null;
		if (policyKingdom == null || (isVassalTarget && !IsPlayerRuler(playerKingdom)))
		{
			InformationManager.DisplayMessage(new InformationMessage("所选附庸国已经失效，或你已不再是宗主国统治者。", Colors.Yellow));
			OpenComposePopup();
			return;
		}
		MentionedWorldEntities knowledgeMentionedEntities = BuildPolicyKnowledgeMentionedEntitiesSnapshot(policyName, policyContent, policyKingdom);
		PolicyTargetWorldSnapshot semanticTargetSnapshot = PolicyTargetSemanticRouter.CaptureWorldSnapshot();
		List<PolicyTargetHandleSaveData> targetHandles = BuildKingdomPolicyTargetHandles(
			policyName,
			policyContent,
			policyKingdom,
			isVassalTarget ? playerKingdom : null,
			semanticTargetSnapshot);
		PolicyPromptContextBundle promptContext = BuildPolicyPromptContextBundle(policyKingdom, options);
		if (isVassalTarget)
		{
			VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(policyKingdom.StringId, out int currentIndependence, out int breakawayThreshold, out int rulerRelation, out string rulerName);
			promptContext.ExtensionContext = (promptContext.ExtensionContext ?? "")
				+ "\n\n【附庸国政策上下文】\n宗主国：" + GetKingdomName(playerKingdom)
				+ "\n发布对象：" + GetKingdomName(policyKingdom)
				+ "\n当前独立度：" + currentIndependence.ToString(CultureInfo.InvariantCulture) + "/100。"
				+ "\n当前统治者：" + rulerName + "；与玩家关系：" + FormatSigned(rulerRelation) + "；脱离阈值：" + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "。";
		}
		PolicyDraftRequest request = new PolicyDraftRequest
		{
			ProposerClanId = Clan.PlayerClan?.StringId ?? "",
			RequestId = Guid.NewGuid().ToString("N"),
			ScopeKind = isVassalTarget ? PolicyScopeVassal : PolicyScopeKingdom,
			IssuerKingdomId = playerKingdom?.StringId ?? "",
			IssuerKingdomName = playerKingdom == null ? "" : GetKingdomName(playerKingdom),
			ManualDurationDays = manualDurationDays,
			PolicyName = policyName,
			PolicyContent = policyContent,
			DateText = string.IsNullOrWhiteSpace(capturedDateText) ? FormatCurrentCampaignDate() : capturedDateText,
			SubmittedDay = GetCurrentCampaignDay(),
			PlayerKingdomId = policyKingdom.StringId ?? "",
			PlayerKingdomName = GetKingdomName(policyKingdom),
			TargetHandles = targetHandles,
			UseAiEvaluatedCost = isVassalTarget ? false : options.UseAiEvaluatedCost,
			GoldCost = isVassalTarget ? 0 : (options.UseAiEvaluatedCost ? 0 : options.GoldCost),
			InfluenceCost = 0f,
			EvaluatorPrompt = options.EvaluatorPrompt,
			EvaluatorPromptIsDefault = options.EvaluatorPromptIsDefault,
			PublicFeedbackTargetChars = NormalizePolicyPublicFeedbackTargetChars(options.PublicFeedbackTargetChars),
			PromptContext = promptContext,
			KnowledgeMentionedEntities = knowledgeMentionedEntities,
			SemanticTargetSnapshot = semanticTargetSnapshot
		};
		CaptureUnifiedPolicyHistoryForRequest(request, policyKingdom);
		request.KnowledgeContext = BuildPolicyKnowledgeContextForMainOnly(request);
		if (!TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"无法冻结本次政策评议配置：" + freezeError,
				Colors.Yellow));
			return;
		}
		_generationInProgress = true;
		ShowPolicyWaitPopupAndPause(request);
		Task.Run(async delegate
		{
			PolicyGenerationResult result = await GeneratePolicyResultAsync(request);
			MainThreadActions.Enqueue(delegate
			{
				CompletePolicyGeneration(request, result);
			});
		});
	}

	private void RetryPolicyGenerationFromFailurePopup(PolicyDraftRequest request)
	{
		if (request == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("原政策请求已不可用，无法重试。", Colors.Yellow));
			return;
		}
		try
		{
			PolicyDebugLog("manual-retry", BuildPolicyRequestLogPrefix(request));
			string durationText = request.ManualDurationDays > 0
				? request.ManualDurationDays.ToString(CultureInfo.InvariantCulture)
				: string.Empty;
			SubmitPolicyFromPopup(
				request.PolicyName,
				request.PolicyContent,
				durationText,
				request.DateText,
				request.PlayerKingdomId);
		}
		catch (Exception ex)
		{
			_generationInProgress = false;
			PolicySystemLog.Failure("Player", "manual-retry-submit-failed", BuildPolicyRequestLogPrefix(request), ex.ToString());
			ShowPolicyGenerationRetryPopup(request, new PolicyGenerationResult
			{
				FailureStage = "手动重试提交",
				Error = "重新校验并提交原政策时发生异常，详细技术信息已写入日志。"
			});
		}
	}

	private static bool TryFreezePlayerPolicyGenerationSettings(
		PolicyDraftRequest request,
		out string error)
	{
		error = string.Empty;
		if (request == null)
		{
			error = "政策请求为空。";
			return false;
		}
		if (!PolicyLlmClient.TryResolvePlayerPolicyProfile(
			DuelSettings.GetPlayerPolicyApiSourceForExternal(),
			DuelSettings.GetPlayerPolicyFollowSelectedApiTokensForExternal(),
			DuelSettings.GetPlayerPolicyCustomMaxTokensForExternal(),
			PolicyEvaluationTemperature,
			out PolicyApiExecutionProfile apiProfile,
			out error))
		{
			return false;
		}

		PolicyEffectRetrievalContext retrievalContext = string.Equals(
			request.ScopeKind,
			PolicyScopeLocal,
			StringComparison.OrdinalIgnoreCase)
			? PolicyEffectRetrievalContext.PlayerLocal
			: string.Equals(request.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				? PolicyEffectRetrievalContext.PlayerVassal
				: PolicyEffectRetrievalContext.PlayerKingdom;
		int effectiveDetailCount = DuelSettings.GetEffectivePlayerPolicyEffectModuleDetailCountForExternal(
			out int configuredDetailCount,
			out _);
		PolicyGenerationSettingsSnapshot snapshot = new PolicyGenerationSettingsSnapshot
		{
			ApiProfile = apiProfile.Clone(),
			RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
			ScopeKind = request.ScopeKind ?? PolicyScopeKingdom,
			IssuerKingdomId = request.IssuerKingdomId ?? string.Empty,
			ProposerClanId = request.ProposerClanId ?? string.Empty,
			TargetKingdomId = request.PlayerKingdomId ?? string.Empty,
			PolicyName = request.PolicyName ?? string.Empty,
			PolicyContent = request.PolicyContent ?? string.Empty,
			DateText = request.DateText ?? string.Empty,
			KnowledgeContext = request.KnowledgeContext ?? string.Empty,
			PromptContext = request.PromptContext == null
				? null
				: new PolicyPromptContextBundle
				{
					PolicyRuleContext = request.PromptContext.PolicyRuleContext ?? string.Empty,
					WorldContextCompact = request.PromptContext.WorldContextCompact ?? string.Empty,
					WorldContextFull = request.PromptContext.WorldContextFull ?? string.Empty,
					ExtensionContext = request.PromptContext.ExtensionContext ?? string.Empty
				},
			SelectedFiefIds = NormalizeIdList(request.SelectedFiefIds),
			MentionedClanIds = NormalizeIdList(request.LocalMentionedClanIds),
			MentionedSettlementIds = NormalizeIdList(request.LocalMentionedSettlementIds),
			FollowCurrentRulingClan = request.LocalMentionedCurrentRulingClan,
			EvaluatorPrompt = request.EvaluatorPrompt ?? string.Empty,
			EvaluatorPromptIsDefault = request.EvaluatorPromptIsDefault,
			UseAiEvaluatedCost = request.UseAiEvaluatedCost,
			PublicFeedbackTargetChars = request.PublicFeedbackTargetChars,
			ManualDurationDays = request.ManualDurationDays,
			ConfiguredDetailCount = configuredDetailCount,
			EffectiveDetailCount = effectiveDetailCount,
			EffectPostprocessMaxTokens = DuelSettings.GetPlayerPolicyEffectPostprocessMaxTokensForExternal(),
			RetrievalContext = retrievalContext,
			EnabledModuleIds = PolicyEffectModuleRetrievalSettings.GetEnabledModules(retrievalContext)
				.Where(module => module?.Descriptor?.PromptVisible == true)
				.Select(module => module.Id)
				.Distinct(StringComparer.Ordinal)
				.ToList(),
			TargetWorldSnapshot = request.SemanticTargetSnapshot,
			HistoryEntries = (request.PolicyHistoryEntries ?? new List<NpcPolicyHistoryEntry>()).ToList(),
			EnemyKingdoms = (request.EnemyKingdoms ?? new List<PolicyEnemyKingdomSnapshot>())
				.Where(enemy => enemy != null)
				.Select(enemy => new PolicyEnemyKingdomSnapshot
				{
					KingdomId = enemy.KingdomId ?? string.Empty,
					KingdomName = enemy.KingdomName ?? string.Empty
				})
				.ToList(),
			InitialTargetHandles = NormalizePolicyTargetHandles(request.TargetHandles)
		};
		request.SelectedFiefIds = new List<string>(snapshot.SelectedFiefIds);
		request.LocalMentionedClanIds = new List<string>(snapshot.MentionedClanIds);
		request.LocalMentionedSettlementIds = new List<string>(snapshot.MentionedSettlementIds);
		request.TargetHandles = NormalizePolicyTargetHandles(snapshot.InitialTargetHandles);
		request.PolicyHistoryEntries = snapshot.HistoryEntries.ToList();
		request.EnemyKingdoms = snapshot.EnemyKingdoms.ToList();
		request.SemanticTargetSnapshot = snapshot.TargetWorldSnapshot;
		request.PolicyName = snapshot.PolicyName;
		request.PolicyContent = snapshot.PolicyContent;
		request.DateText = snapshot.DateText;
		request.KnowledgeContext = snapshot.KnowledgeContext;
		request.PromptContext = snapshot.PromptContext;
		request.GenerationSettings = snapshot;
		return true;
	}

	private void ShowPolicyWaitPopupAndPause(PolicyDraftRequest request)
	{
		try
		{
			BeginPolicyWaitPause();
			if (_policyWaitPopupShown)
			{
				return;
			}
			bool isLocal = IsLocalPolicyRequest(request);
			bool isVassal = IsVassalPolicyRequest(request);
			InformationManager.ShowInquiry(new InquiryData(
				isLocal ? "等待地方政策评议" : (isVassal ? "等待附庸国政策评议" : "等待政策评议"),
				isLocal
					? "地方政策《" + request.PolicyName + "》正在由智能服务评议。\n\n游戏时间已暂停；成功后会立即结算并只对所选封地及附属村庄生效，不进入王国议程。"
					: (isVassal
						? "附庸国政策《" + request.PolicyName + "》正在由智能服务评议。\n\n游戏时间已暂停；成功后会直接生效、写入附庸国与世界政策记录，并结算独立度。"
						: "政策《" + request.PolicyName + "》已经提交给朝廷与民众评议。\n\n游戏时间已暂停，智能服务完成判断后会自动发布结果并显示民众反馈与影响效果。"),
				isAffirmativeOptionShown: false,
				isNegativeOptionShown: false,
				"",
				"",
				null,
				null),
				pauseGameActiveState: true,
				prioritize: true);
			_policyWaitPopupShown = true;
		}
		catch (Exception ex)
		{
			_policyWaitPopupShown = false;
			Log("show wait popup failed " + BuildPolicyRequestLogPrefix(request) + " error=" + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("政策正在评议中，游戏时间已暂停。", Colors.Yellow));
		}
	}

	private void BeginPolicyWaitPause()
	{
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return;
			}
			if (!_waitTimeLocked)
			{
				_previousTimeControlMode = campaign.TimeControlMode;
				_previousTimeControlLock = campaign.TimeControlModeLock;
				campaign.TimeControlMode = CampaignTimeControlMode.Stop;
				campaign.SetTimeControlModeLock(true);
				_waitTimeLocked = true;
			}
			else
			{
				campaign.SetTimeSpeed(0);
			}
		}
		catch (Exception ex)
		{
			Log("wait pause failed: " + ex.Message);
		}
	}

	private void EndPolicyWaitPause(string reason, PolicyDraftRequest request = null)
	{
		bool hadWaitPopup = _policyWaitPopupShown;
		_policyWaitPopupShown = false;
		if (hadWaitPopup)
		{
			try
			{
				InformationManager.HideInquiry();
			}
			catch
			{
			}
		}
		if (!_waitTimeLocked)
		{
			return;
		}
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign != null)
			{
				campaign.SetTimeControlModeLock(_previousTimeControlLock);
				if (!_previousTimeControlLock)
				{
					campaign.TimeControlMode = _previousTimeControlMode;
				}
			}
			Log("wait released " + BuildPolicyRequestLogPrefix(request) + " reason=" + (reason ?? "") + " popupShown=" + hadWaitPopup);
		}
		catch (Exception ex)
		{
			Log("wait release failed: " + ex.Message);
		}
		_waitTimeLocked = false;
	}

	private static string ExtractMainFeedbackForPopup(string mainRaw)
	{
		string text = CleanLlmText(mainRaw);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = text.Replace("**", "").Trim();
		int start = text.IndexOf("民众反馈", StringComparison.Ordinal);
		if (start >= 0)
		{
			text = text.Substring(start + "民众反馈".Length);
		}
		int end = text.IndexOf("影响摘要", StringComparison.Ordinal);
		if (end >= 0)
		{
			text = text.Substring(0, end);
		}
		text = StripMainOutputLabel(text);
		text = CleanPolicyDisplayText(text);
		return text;
	}

	private static string ExtractMainImpactSummaryForPopup(string mainRaw)
	{
		string text = CleanLlmText(mainRaw);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = text.Replace("**", "").Trim();
		int start = text.IndexOf("影响摘要", StringComparison.Ordinal);
		if (start >= 0)
		{
			text = text.Substring(start + "影响摘要".Length);
			int end = text.IndexOf('\n');
			if (end >= 0)
			{
				text = text.Substring(0, end);
			}
			text = StripMainOutputLabel(text);
			text = CleanPolicyDisplayText(text);
			return LimitDisplayChars(text, 120);
		}
		return "";
	}

	private static string StripMainOutputLabel(string text)
	{
		text = CleanLlmText(text).Replace("**", "").Trim();
		while (text.StartsWith("：", StringComparison.Ordinal) || text.StartsWith(":", StringComparison.Ordinal) || text.StartsWith("-", StringComparison.Ordinal) || text.StartsWith("—", StringComparison.Ordinal))
		{
			text = text.Substring(1).TrimStart();
		}
		return text.Trim();
	}

	private static string LimitDisplayChars(string text, int maxChars)
	{
		text = CleanLlmText(text);
		if (string.IsNullOrWhiteSpace(text) || maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, Math.Max(1, maxChars - 1)).TrimEnd() + "…";
	}

	private static string CleanPolicyDisplayText(string text)
	{
		text = CleanLlmText(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "```\\s*(json)?", "", RegexOptions.IgnoreCase).Replace("```", "");
		text = text.Replace("**", "").Trim();
		if (text.StartsWith("{", StringComparison.Ordinal) && text.EndsWith("}", StringComparison.Ordinal)
			&& (text.IndexOf("\"effects\"", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("\"impactSummary\"", StringComparison.OrdinalIgnoreCase) >= 0
				|| text.IndexOf("\"publicFeedback\"", StringComparison.OrdinalIgnoreCase) >= 0))
		{
			return "";
		}
		text = Regex.Replace(text, "\\[(AFEF|ACTION|REWARD|DUEL|VASSALAGE|KINGDOM|WORLD_MAP|PARTY_TRANSFER|DIPLOMACY|VOTE_DEAL)[^\\]]*\\]", "", RegexOptions.IgnoreCase);
		for (int i = 0; i < 3; i++)
		{
			string cleaned = Regex.Replace(text, "^\\s*(民众反馈|反馈|publicFeedback)\\s*[：:]\\s*", "", RegexOptions.IgnoreCase).Trim();
			if (string.Equals(cleaned, text, StringComparison.Ordinal))
			{
				break;
			}
			text = cleaned;
		}
		int impactIndex = text.IndexOf("影响摘要", StringComparison.Ordinal);
		if (impactIndex > 0)
		{
			text = text.Substring(0, impactIndex).Trim();
		}
		return Regex.Replace(text, "\\s+", " ").Trim();
	}

	private static string BuildImpactPopupText(PolicyDraftRequest request, string feedback, PolicyApplicationResult application, bool costDeducted)
	{
		StringBuilder sb = new StringBuilder();
		feedback = CleanPolicyDisplayText(feedback);
		sb.AppendLine("《" + request.PolicyName + "》");
		sb.AppendLine("日期：" + request.DateText);
		sb.AppendLine();
		sb.AppendLine("【民众反馈】");
		sb.AppendLine(string.IsNullOrWhiteSpace(feedback) ? "民众尚未形成明确反馈。" : feedback.Trim());
		sb.AppendLine();
		sb.AppendLine("【政策效果】");
		if (application?.KingdomEffects != null && application.KingdomEffects.Count > 0)
		{
			foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
			{
				foreach (string line in BuildPlayerVisibleEffectLines(effect))
				{
					sb.AppendLine("- " + line + "。");
				}
			}
		}
		else
		{
			sb.AppendLine("未产生可落地的模块效果。");
		}
		if (application?.NoticeLines != null)
		{
			HashSet<string> shownNotices = new HashSet<string>(StringComparer.Ordinal);
			foreach (string line in application.NoticeLines.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				string visibleNotice = CleanPlayerVisiblePolicySystemText(
					line,
					"部分效果未能落地，详细信息已写入日志。");
				if (!string.IsNullOrWhiteSpace(visibleNotice) && shownNotices.Add(visibleNotice))
				{
					sb.AppendLine("- " + visibleNotice);
				}
			}
		}
		sb.AppendLine();
		if (costDeducted)
		{
			if (request?.UseAiEvaluatedCost == true)
			{
				sb.AppendLine(BuildAiEvaluatedCostPaymentText(request) + "政策效果已按各模块规则结算，数值效果会在有效状态内生效。你可以继续发布新的政策。");
			}
			else
			{
				sb.AppendLine(BuildPlayerPolicyGoldCostSummary(request) + "。政策效果已按各模块规则结算，数值效果会在有效状态内生效。你可以继续发布新的政策。");
			}
		}
		else
		{
			sb.AppendLine(IsVassalPolicyRequest(request)
				? "本次未扣除费用。"
				: "本次未扣除费用。" + BuildPlayerPolicyGoldCostSummary(request) + "。");
		}
		string popupText = sb.ToString().TrimEnd();
		return popupText;
	}

	internal static void DisplayKingdomPolicyAnnouncementMessage(string source, string policyId, string kingdomName, string policyName, string policyContent)
	{
		try
		{
			string issuer = CompactPolicyContextText(kingdomName ?? "");
			string name = CompactPolicyContextText(policyName ?? "");
			string content = CompactPolicyContextText(policyContent ?? "");
			if (string.IsNullOrWhiteSpace(name))
			{
				name = "未命名政策";
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				content = "未记录政策正文。";
			}
			string policySubject = string.IsNullOrWhiteSpace(issuer) ? "王国" : issuer;
			InformationManager.DisplayMessage(new InformationMessage(
				"【王国政策】" + policySubject + "颁布《" + name + "》：" + content,
				Color.FromUint(4294945331u)));
			PolicySystemLog.Write("Notice", "policy-announcement-displayed",
				"source=" + (source ?? "")
				+ " policyId=" + (policyId ?? "")
				+ " contentChars=" + content.Length.ToString(CultureInfo.InvariantCulture));
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Notice", "policy-announcement-failed",
				"source=" + (source ?? "") + " policyId=" + (policyId ?? "") + " " + ex);
		}
	}

	internal static bool DisplayKingdomPolicyFeedbackMessage(string source, string policyId, string kingdomName, string policyName, string publicFeedback)
	{
		try
		{
			string issuer = CompactPolicyContextText(kingdomName ?? "");
			string name = CompactPolicyContextText(policyName ?? "");
			string feedback = CompactPolicyContextText(CleanPolicyDisplayText(publicFeedback ?? ""));
			if (string.IsNullOrWhiteSpace(name))
			{
				name = "未命名政策";
			}
			if (string.IsNullOrWhiteSpace(feedback))
			{
				feedback = "民众尚未形成明确反馈。";
			}
			string policySubject = string.IsNullOrWhiteSpace(issuer) ? "王国" : issuer;
			InformationManager.DisplayMessage(new InformationMessage(
				"【民众反馈】" + policySubject + "《" + name + "》：" + feedback,
				Color.FromUint(4278242559u)));
			PolicySystemLog.Write("Notice", "policy-feedback-displayed",
				"source=" + (source ?? "")
				+ " policyId=" + (policyId ?? "")
				+ " feedbackChars=" + feedback.Length.ToString(CultureInfo.InvariantCulture));
			return true;
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Notice", "policy-feedback-failed",
				"source=" + (source ?? "") + " policyId=" + (policyId ?? "") + " " + ex);
			return false;
		}
	}

	private PolicyHistoryData BuildPolicyHistoryData()
	{
		List<PolicyRecordSaveData> records = LoadPolicyRecordHistory();
		Dictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId = BuildActivePolicyEffectHistoryIndex();
		PolicyHistoryData data = new PolicyHistoryData
		{
			TitleText = "政策记录",
			SubtitleText = "显示最近 " + MaxPolicyRecordHistoryCount.ToString(CultureInfo.InvariantCulture) + " 条已发布并生效的政策。持续效果状态会随游戏日期更新。",
			EmptyStateText = "还没有成功发布并生效的政策。",
			CloseText = "返回政策管理"
		};
		foreach (PolicyRecordSaveData record in records)
		{
			string effectSummary = BuildPolicyRecordEffectSummary(record);
			string runtimeStatus = BuildPolicyRuntimeStatusText(record?.RecordId, activeEffectsByRecordId);
			data.Records.Add(new PolicyHistoryRecordData
			{
				DateText = string.IsNullOrWhiteSpace(record.DateText) ? "未知日期" : record.DateText.Trim(),
				PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? "未命名政策" : "《" + record.PolicyName.Trim() + "》",
				CostText = BuildPolicyRecordCostText(record),
				ContentSectionTitleText = "【政策内容】",
				ContentSummaryText = string.IsNullOrWhiteSpace(record.PolicyContentSummary) ? "（没有记录政策内容摘要）" : CleanLlmText(record.PolicyContentSummary),
				FeedbackSectionTitleText = "【民众反馈】",
				FeedbackSummaryText = string.IsNullOrWhiteSpace(record.PublicFeedbackSummary) ? "民众反馈未记录。" : CleanPolicyDisplayText(record.PublicFeedbackSummary),
				ImpactSectionTitleText = "【政策效果】",
				ImpactSummaryText = AppendPolicyRuntimeStatus(
					string.IsNullOrWhiteSpace(effectSummary) ? CleanPolicyDisplayText(record.ImpactSummary ?? "") : effectSummary,
					runtimeStatus)
			});
		}
		return data;
	}

	private Dictionary<string, List<ActivePolicyEffectSaveData>> BuildActivePolicyEffectHistoryIndex()
	{
		Dictionary<string, List<ActivePolicyEffectSaveData>> result = new Dictionary<string, List<ActivePolicyEffectSaveData>>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			try
			{
				ActivePolicyEffectSaveData effect = GetActivePolicyEffectForWork(item.Key, item.Value ?? string.Empty);
				string recordId = (effect?.RecordId ?? string.Empty).Trim();
				if (recordId.Length == 0)
				{
					continue;
				}
				if (!result.TryGetValue(recordId, out List<ActivePolicyEffectSaveData> effects))
				{
					effects = new List<ActivePolicyEffectSaveData>();
					result[recordId] = effects;
				}
				effects.Add(effect);
			}
			catch (Exception ex)
			{
				PolicyDebugLog("policy-history-runtime-skip", "effectId=" + (item.Key ?? string.Empty) + " error=" + ex.Message);
			}
		}
		return result;
	}

	private static string BuildPolicyRuntimeStatusText(
		string recordId,
		IReadOnlyDictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId)
	{
		if (string.IsNullOrWhiteSpace(recordId)
			|| activeEffectsByRecordId == null
			|| !activeEffectsByRecordId.TryGetValue(recordId.Trim(), out List<ActivePolicyEffectSaveData> effects)
			|| effects == null
			|| effects.Count == 0)
		{
			return string.Empty;
		}
		bool rollbackPending = effects.Any(IsPolicyEffectRollbackPending);
		bool compensationPending = effects.Any(effect =>
			PolicyEffectActivationCoordinator.HasPendingDailyCompensation(effect?.ModuleEffects)
			|| PolicyEffectActivationCoordinator.HasPendingScheduledCompensation(effect?.ModuleEffects));
		bool maintenancePaused = effects.Any(effect => IsPolicyEffectWithinDuration(effect) && !IsPolicyEffectMaintenanceFunded(effect));
		bool hasActiveEffect = effects.Any(IsPolicyEffectWithinDuration);
		List<string> lines = new List<string>();
		if (rollbackPending)
		{
			lines.Add("结束处理未完成，系统会继续重试恢复已应用效果");
		}
		if (compensationPending)
		{
			lines.Add("效果恢复中，补偿完成前相关模块暂停继续结算");
		}
		if (maintenancePaused)
		{
			lines.Add("维护费不足，数值效果已暂停；资金满足后会自动恢复");
		}
		if (lines.Count == 0 && hasActiveEffect)
		{
			lines.Add("正常生效");
		}
		return string.Join("；", lines);
	}

	private static string AppendPolicyRuntimeStatus(string text, string runtimeStatus)
	{
		string visibleText = string.IsNullOrWhiteSpace(text) ? "未记录影响效果。" : text.TrimEnd();
		return string.IsNullOrWhiteSpace(runtimeStatus)
			? visibleText
			: visibleText + "\n【运行状态】" + runtimeStatus.Trim();
	}

	private static string BuildPolicyRuntimeStatusSuffix(
		string recordId,
		IReadOnlyDictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId)
	{
		string runtimeStatus = BuildPolicyRuntimeStatusText(recordId, activeEffectsByRecordId);
		return string.IsNullOrWhiteSpace(runtimeStatus) ? string.Empty : "；运行状态：" + runtimeStatus;
	}

	private List<PolicyRecordSaveData> LoadPolicyRecordHistory()
	{
		List<PolicyRecordSaveData> records = new List<PolicyRecordSaveData>();
		foreach (KeyValuePair<string, string> item in _policyRecordHistory)
		{
			try
			{
				PolicyRecordSaveData record = JsonConvert.DeserializeObject<PolicyRecordSaveData>(item.Value ?? "");
				if (record != null)
				{
					if (string.IsNullOrWhiteSpace(record.RecordId))
					{
						record.RecordId = item.Key ?? "";
					}
					records.Add(record);
				}
			}
			catch (Exception ex)
			{
				PolicyDebugLog("history-load-skip", "invalid policy record key=" + (item.Key ?? "") + " error=" + ex.Message);
			}
		}
		return records
			.OrderByDescending(x => x.SubmittedDay)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(MaxPolicyRecordHistoryCount)
			.ToList();
	}

	private static string BuildPolicyRecordCostText(PolicyRecordSaveData record)
	{
		if (record?.UseAiEvaluatedCost == true)
		{
			if (record.RequiredInfluenceCost <= 0.0001f && record.InfluenceCost <= 0.0001f)
			{
				return "智能评估消耗：启动所需 " + FormatGoldCostText(record.RequiredGoldCost)
					+ "；启动实付 " + FormatGoldCostText(record.GoldCost)
					+ "；每日维护 " + FormatGoldCostText(record.DailyMaintenanceGoldCost)
					+ "；累计维护 " + FormatGoldCostText(record.TotalMaintenancePaidGold)
					+ "；全部效果 " + FormatPercent(record.GoldEffectScale <= 0f && record.RequiredGoldCost <= 0 ? 1f : record.GoldEffectScale);
			}
			return "智能评估消耗：完整需 " + FormatCostText(record.RequiredGoldCost, record.RequiredInfluenceCost)
				+ "；已支付 " + FormatCostText(record.GoldCost, record.InfluenceCost)
				+ "；经济/民生 " + FormatPercent(record.GoldEffectScale <= 0f && record.RequiredGoldCost <= 0 ? 1f : record.GoldEffectScale)
				+ "，政治/秩序 " + FormatPercent(record.InfluenceEffectScale <= 0f && record.RequiredInfluenceCost <= 0f ? 1f : record.InfluenceEffectScale);
		}
		if ((record?.InfluenceCost ?? 0f) > 0.0001f)
		{
			return "已支付：" + FormatCostText(record?.GoldCost ?? 0, record?.InfluenceCost ?? 0f);
		}
		int fixedStartupRequired = Math.Max(0, record?.RequiredGoldCost ?? 0);
		if (fixedStartupRequired == 0 && (record?.GoldCost ?? 0) > 0)
		{
			fixedStartupRequired = record.GoldCost;
		}
		return "启动所需 " + FormatGoldCostText(fixedStartupRequired)
			+ "；启动实付 " + FormatGoldCostText(record?.GoldCost ?? 0)
			+ "；每日维护 " + FormatGoldCostText(record?.DailyMaintenanceGoldCost ?? 0)
			+ "；累计维护 " + FormatGoldCostText(record?.TotalMaintenancePaidGold ?? 0);
	}

	private void TrimPolicyRecordHistory()
	{
		try
		{
			if (_policyRecordHistory.Count <= MaxPolicyRecordHistoryCount)
			{
				return;
			}
			List<PolicyRecordSaveData> keepRecords = LoadPolicyRecordHistory();
			HashSet<string> keepIds = new HashSet<string>(keepRecords.Select(x => x.RecordId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
			foreach (string key in _policyRecordHistory.Keys.ToList())
			{
				if (!keepIds.Contains(key))
				{
					_policyRecordHistory.Remove(key);
				}
			}
		}
		catch (Exception ex)
		{
			PolicyDebugLog("history-trim-failed", ex.Message);
		}
	}

	private static string BuildPolicyHistoryFallbackText(PolicyHistoryData data)
	{
		if (data == null)
		{
			return "尚无成功落地的政策记录。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(data.SubtitleText))
		{
			stringBuilder.AppendLine(data.SubtitleText);
			stringBuilder.AppendLine();
		}
		if (data.Records == null || data.Records.Count <= 0)
		{
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(data.EmptyStateText) ? "尚无成功落地的政策记录。" : data.EmptyStateText);
			return stringBuilder.ToString().TrimEnd();
		}
		for (int i = 0; i < data.Records.Count; i++)
		{
			PolicyHistoryRecordData record = data.Records[i];
			stringBuilder.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture) + ". " + record.DateText + "  " + record.PolicyNameText + "  " + record.CostText);
			if (!string.IsNullOrWhiteSpace(record.ContentSummaryText))
			{
				stringBuilder.AppendLine("【政策内容】");
				stringBuilder.AppendLine(record.ContentSummaryText);
			}
			if (!string.IsNullOrWhiteSpace(record.FeedbackSummaryText))
			{
				stringBuilder.AppendLine("【民众反馈】");
				stringBuilder.AppendLine(record.FeedbackSummaryText);
			}
			if (!string.IsNullOrWhiteSpace(record.ImpactSummaryText))
			{
				stringBuilder.AppendLine("【影响效果】");
				stringBuilder.AppendLine(record.ImpactSummaryText);
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private static string BuildPolicyEffectSummary(PolicyApplicationResult application)
	{
		if (application?.KingdomEffects == null || application.KingdomEffects.Count <= 0)
		{
			return "未产生可落地的模块效果。";
		}
		List<string> lines = new List<string>();
		foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
		{
			foreach (string visibleLine in BuildPlayerVisibleEffectLines(effect))
			{
				string line = visibleLine;
				if (!string.IsNullOrWhiteSpace(effect.Reason))
				{
					string visibleReason = CleanPlayerVisiblePolicySystemText(effect.Reason, string.Empty);
					if (!string.IsNullOrWhiteSpace(visibleReason))
					{
						line += "｜原因：" + visibleReason;
					}
				}
				lines.Add(line.TrimEnd('。') + "。");
			}
		}
		return string.Join("\n", lines);
	}

	private static string BuildPolicyRecordEffectSummary(PolicyRecordSaveData record)
	{
		if (record?.Effects == null || record.Effects.Count <= 0)
		{
			return CleanPlayerVisiblePolicySystemText(record?.ImpactSummary, "未记录影响效果。");
		}
		List<string> lines = new List<string>();
		foreach (PolicyRecordEffectSaveData effect in record.Effects.Where(x => x != null))
		{
			string status = effect.IsPermanentEffect && !effect.IsEnded
				? "永久生效"
				: effect.IsEnded || effect.RemainingDays <= 0
					? "已结束"
					: "剩余 " + effect.RemainingDays.ToString(CultureInfo.InvariantCulture) + "/" + effect.TotalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天";
			if (!string.IsNullOrWhiteSpace(effect.EndReason)
				&& (effect.IsEnded || (!effect.IsPermanentEffect && effect.RemainingDays <= 0)))
			{
				status += "：" + BuildPlayerVisiblePolicyEndReason(effect.EndReason);
			}
			foreach (string visibleLine in BuildPlayerVisibleEffectLines(
				FirstNonEmpty(effect.TargetLabel, effect.KingdomName),
				effect.ModuleEffects,
				effect.TotalDurationDays))
			{
				lines.Add(visibleLine + "｜状态：" + status + "。");
			}
		}
		return string.Join("\n", lines);
	}

	private static List<string> BuildPlayerVisibleEffectLines(AppliedKingdomEffect effect)
	{
		if (effect == null)
		{
			return new List<string> { "未知目标｜未记录模块效果" };
		}
		return BuildPlayerVisibleEffectLines(
			FirstNonEmpty(effect.TargetLabel, effect.KingdomName),
			effect.ModuleEffects,
			effect.DurationDays);
	}

	private static List<string> BuildPlayerVisibleEffectLines(
		string targetName,
		IEnumerable<PolicyEffectInstanceSaveData> moduleEffects,
		int durationDays)
	{
		return BuildPlayerVisibleEffectLinesForExternal(targetName, moduleEffects, durationDays);
	}

	internal static List<string> BuildPlayerVisibleEffectLinesForExternal(
		string fallbackTargetName,
		IEnumerable<PolicyEffectInstanceSaveData> moduleEffects,
		int durationDays)
	{
		List<PolicyEffectInstanceSaveData> instances = (moduleEffects ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		string durationText = durationDays < 0
			? string.Empty
			: durationDays == 0
			? "永久"
			: "持续 " + durationDays.ToString(CultureInfo.InvariantCulture) + " 天";
		string durationSuffix = durationText.Length <= 0 ? string.Empty : "｜期限：" + durationText;
		if (instances.Count <= 0)
		{
			return new List<string>
			{
				BuildPlayerVisibleTargetLabel(null, fallbackTargetName) + "｜无模块效果" + durationSuffix
			};
		}

		List<string> lines = new List<string>();
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> targetGroup in instances.GroupBy(BuildPlayerVisibleTargetGroupingKey))
		{
			PolicyEffectCanonicalTargetSet targetSet = targetGroup.FirstOrDefault()?.TargetSet;
			string targetLabel = BuildPlayerVisibleTargetLabel(targetSet, fallbackTargetName);
			List<string> values = BuildPlayerVisibleEffectValues(targetGroup);
			if (values.Count <= 0)
			{
				values.Add("无模块效果");
			}
			lines.AddRange(values
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Select(value => targetLabel + "｜" + value.Trim() + durationSuffix));
		}
		return lines;
	}

	private static string BuildPlayerVisibleTargetGroupingKey(PolicyEffectInstanceSaveData instance)
	{
		PolicyEffectCanonicalTargetSet targetSet = instance?.TargetSet;
		if (targetSet == null)
		{
			return "fallback";
		}
		return string.Join("\u001f", new[]
		{
			JoinStableTargetIds(targetSet.SettlementIds),
			JoinStableTargetIds(targetSet.TownIds),
			JoinStableTargetIds(targetSet.VillageIds),
			JoinStableTargetIds(targetSet.ClanIds),
			JoinStableTargetIds(targetSet.KingdomIds),
			JoinStableTargetIds(targetSet.HeroIds),
			JoinStableTargetIds(targetSet.ParentSettlementIds),
			targetSet.FollowCurrentRulingClan ? "1" : "0"
		});
	}

	private static string JoinStableTargetIds(IEnumerable<string> values)
	{
		return string.Join("\u001e", (values ?? Enumerable.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
	}

	private static string BuildPlayerVisibleTargetLabel(
		PolicyEffectCanonicalTargetSet targetSet,
		string fallbackTargetName)
	{
		List<string> heroIds = DistinctTargetIds(targetSet?.HeroIds);
		if (heroIds.Count > 0)
		{
			List<string> names = heroIds
				.Select(id => TryResolvePlayerVisibleHero(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildNamedOrCountTargetLabel("人物", names, heroIds.Count, "位人物");
		}

		List<string> clanIds = DistinctTargetIds(targetSet?.ClanIds);
		if (clanIds.Count > 0)
		{
			List<string> names = clanIds.Select(id =>
			{
				Clan clan = TryResolvePlayerVisibleClan(id);
				if (clan == null)
				{
					return string.Empty;
				}
				string clanName = clan.Name?.ToString() ?? string.Empty;
				string leaderName = clan.Leader?.Name?.ToString() ?? string.Empty;
				return string.IsNullOrWhiteSpace(leaderName)
					? clanName
					: clanName + "（领袖：" + leaderName + "）";
			}).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
			return BuildNamedOrCountTargetLabel("家族", names, clanIds.Count, "个家族");
		}

		List<string> villageIds = DistinctTargetIds(targetSet?.VillageIds);
		if (villageIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				"村庄",
				ResolveSettlementNames(villageIds),
				villageIds.Count,
				"个村庄");
		}

		List<string> townIds = DistinctTargetIds(targetSet?.TownIds);
		if (townIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				"城镇",
				ResolveSettlementNames(townIds),
				townIds.Count,
				"座城镇");
		}

		List<string> settlementIds = DistinctTargetIds(targetSet?.SettlementIds);
		if (settlementIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				"定居点",
				ResolveSettlementNames(settlementIds),
				settlementIds.Count,
				"处定居点");
		}

		List<string> kingdomIds = DistinctTargetIds(targetSet?.KingdomIds);
		if (kingdomIds.Count > 0)
		{
			List<string> names = kingdomIds
				.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildNamedOrCountTargetLabel("王国", names, kingdomIds.Count, "个王国");
		}

		List<string> parentIds = DistinctTargetIds(targetSet?.ParentSettlementIds);
		if (parentIds.Count > 0)
		{
			List<string> names = ResolveSettlementNames(parentIds)
				.Select(name => name + "的附属村庄")
				.ToList();
			return BuildNamedOrCountTargetLabel("附属村庄", names, parentIds.Count, "组附属村庄");
		}

		string fallback = string.IsNullOrWhiteSpace(fallbackTargetName) ? "未知目标" : fallbackTargetName.Trim();
		return "目标：" + fallback;
	}

	private static Hero TryResolvePlayerVisibleHero(string heroId)
	{
		try
		{
			return Hero.Find(heroId);
		}
		catch
		{
			return null;
		}
	}

	private static Clan TryResolvePlayerVisibleClan(string clanId)
	{
		try
		{
			return ResolveClanById(clanId);
		}
		catch
		{
			return null;
		}
	}

	private static Kingdom TryResolvePlayerVisibleKingdom(string kingdomId)
	{
		try
		{
			return ResolveKingdomStatic(kingdomId);
		}
		catch
		{
			return null;
		}
	}

	private static List<string> DistinctTargetIds(IEnumerable<string> values)
	{
		return (values ?? Enumerable.Empty<string>())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static List<string> ResolveSettlementNames(IEnumerable<string> settlementIds)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		return (settlementIds ?? Enumerable.Empty<string>())
			.Select(id => behavior?.ResolveSettlementById(id)?.Name?.ToString() ?? string.Empty)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.ToList();
	}

	private static string BuildNamedOrCountTargetLabel(
		string category,
		IReadOnlyList<string> resolvedNames,
		int totalCount,
		string countUnit)
	{
		List<string> names = (resolvedNames ?? Array.Empty<string>())
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (names.Count <= 0)
		{
			return "目标：" + totalCount.ToString(CultureInfo.InvariantCulture) + " " + countUnit;
		}
		string summary = string.Join("、", names.Take(3));
		if (totalCount > names.Take(3).Count())
		{
			summary += "等 " + totalCount.ToString(CultureInfo.InvariantCulture) + " " + countUnit;
		}
		return "目标" + category + "：" + summary;
	}

	private static List<string> BuildPlayerVisibleEffectValues(IEnumerable<PolicyEffectInstanceSaveData> moduleEffects)
	{
		return PolicyEffectSaveCodec.DescribePlayerVisibleInstances(moduleEffects);
	}

	private static string CleanPlayerVisiblePolicySystemText(string text, string fallback)
	{
		string cleaned = CleanPolicyDisplayText(text);
		return string.IsNullOrWhiteSpace(cleaned) || Regex.IsMatch(cleaned, "[A-Za-z]")
			? (fallback ?? string.Empty)
			: cleaned;
	}

	private static string BuildPlayerVisiblePolicyEndReason(string reason)
	{
		if ((reason ?? string.Empty).StartsWith(PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal))
		{
			return "等待效果回滚";
		}
		return CleanPlayerVisiblePolicySystemText(reason, "内部状态已记录");
	}

	private static string CompactPolicyContextText(string text)
	{
		text = (text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return Regex.Replace(text, "\\s+", " ");
	}

	private static bool HasAnyActualAppliedEffect(PolicyApplicationResult application)
	{
		try
		{
			return application?.KingdomEffects != null && application.KingdomEffects.Any(effect => effect != null
				&& (effect.IsPermanentEffect || effect.DurationDays > 0)
				&& (effect.ModuleEffects?.Any(instance => instance != null) ?? false));
		}
		catch
		{
			return false;
		}
	}

	private static LocalPolicyRecordSaveData NormalizeLocalPolicyRecord(LocalPolicyRecordSaveData record)
	{
		if (record == null) return null;
		record.Version = Math.Max(6, record.Version);
		record.ScopeKind = string.Equals(record.ScopeKind ?? "", PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
			? PolicyScopeVassal
			: PolicyScopeLocal;
		record.OriginalTargetFiefIds = NormalizeIdList(record.OriginalTargetFiefIds);
		record.TargetFiefIds = NormalizeIdList(record.TargetFiefIds);
		record.OriginalTargets ??= new List<LocalPolicyTargetSnapshotSaveData>();
		record.Renewals ??= new List<LocalPolicyRenewalSaveData>();
		record.Effects ??= new List<LocalPolicyEffectRecordSaveData>();
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
		{
			effect.TargetClanIds = NormalizeIdList(effect.TargetClanIds);
			effect.DirectTargetSettlementIds = NormalizeIdList(effect.DirectTargetSettlementIds);
			effect.ModuleEffects = (effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Where(instance => instance != null)
				.ToList();
			effect.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
				effect.ModuleEffects,
				effect.ExecutionReceipts,
				effect.ModuleEffects.Select(instance => instance.ExecutionReceipt));
		}
		record.OriginalDurationDays = record.IsPermanentEffect ? 0 : Math.Max(1, record.OriginalDurationDays);
		record.RemainingDays = Math.Max(0, record.RemainingDays);
		record.DailyMaintenanceGoldCost = Math.Max(0, record.DailyMaintenanceGoldCost);
		record.TotalMaintenancePaidGold = Math.Max(0, record.TotalMaintenancePaidGold);
		record.GoldEffectScale = float.IsNaN(record.GoldEffectScale) || float.IsInfinity(record.GoldEffectScale) ? 0f : Math.Max(0f, Math.Min(1f, record.GoldEffectScale));
		List<LocalPolicyEffectRecordSaveData> normalizedEffects = new List<LocalPolicyEffectRecordSaveData>();
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
		{
			effect.TargetScope = NormalizeLocalPolicyTargetScope(effect.TargetScope);
			if (string.IsNullOrWhiteSpace(effect.TargetScope))
			{
				effect.TargetScope = normalizedEffects.Count == 0 ? LocalPolicyTargetScopeSource : LocalPolicyTargetScopeMentioned;
			}
			effect.ActiveEffectId = (effect.ActiveEffectId ?? "").Trim();
			effect.TargetHandle = (effect.TargetHandle ?? "").Trim();
			if (string.IsNullOrWhiteSpace(effect.TargetHandle))
			{
				effect.TargetHandle = string.Equals(effect.TargetScope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase)
					? "S"
					: "legacy-mentioned";
			}
			effect.TargetLabel = CleanPolicyDisplayText(effect.TargetLabel ?? "");
			effect.TargetClanIds = NormalizeIdList(effect.TargetClanIds);
			effect.DirectTargetSettlementIds = NormalizeIdList(effect.DirectTargetSettlementIds);
			effect.RemainingDays = Math.Max(0, effect.RemainingDays);
			effect.IsPermanentEffect = record.IsPermanentEffect || effect.IsPermanentEffect;
			normalizedEffects.Add(effect);
		}
		record.Effects = normalizedEffects;
		LocalPolicyEffectRecordSaveData sourceEffect = record.Effects.FirstOrDefault(x => string.Equals(x.TargetScope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase));
		if (sourceEffect == null && record.Effects.Count > 0)
		{
			sourceEffect = record.Effects.First();
			sourceEffect.TargetScope = LocalPolicyTargetScopeSource;
		}
		if (sourceEffect != null
			&& string.IsNullOrWhiteSpace(sourceEffect.ActiveEffectId)
			&& !string.IsNullOrWhiteSpace(record.ActiveEffectId))
		{
			sourceEffect.ActiveEffectId = record.ActiveEffectId.Trim();
		}
		record.ActiveEffectId = sourceEffect?.ActiveEffectId ?? "";
		if (string.IsNullOrWhiteSpace(record.Status))
		{
			record.Status = LocalPolicyStatusActive;
		}
		if (string.IsNullOrWhiteSpace(record.EffectStatus))
		{
			record.EffectStatus = record.IsPermanentEffect || record.RemainingDays > 0
				? LocalPolicyStatusActive
				: LocalPolicyStatusExpired;
		}
		return record;
	}

	private List<LocalPolicyRecordSaveData> LoadLocalPolicyRecords()
	{
		List<LocalPolicyRecordSaveData> records = new List<LocalPolicyRecordSaveData>();
		foreach (KeyValuePair<string, string> item in _localPolicyRecords)
		{
			try
			{
				JObject rawRecord = JObject.Parse(item.Value ?? "");
				if (!PolicyEffectSaveCodec.TryNormalizeLocalV1ToV6(
					rawRecord,
					out JObject normalizedRecord,
					out _,
					out string migrationError))
				{
					PolicyDebugLog("local-history-load-skip", "key=" + (item.Key ?? "") + " error=" + migrationError);
					continue;
				}
				LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(
					normalizedRecord.ToObject<LocalPolicyRecordSaveData>());
				if (record != null)
				{
					if (string.IsNullOrWhiteSpace(record.RecordId)) record.RecordId = item.Key;
					records.Add(record);
				}
			}
			catch (Exception ex)
			{
				PolicyDebugLog("local-history-load-skip", "key=" + (item.Key ?? "") + " error=" + ex.Message);
			}
		}
		return records.OrderByDescending(x => x.SubmittedDay).ThenByDescending(x => x.CreatedUtcTicks).ToList();
	}

	private void TrimLocalPolicyRecords()
	{
		try
		{
			List<LocalPolicyRecordSaveData> records = LoadLocalPolicyRecords();
			HashSet<string> keep = new HashSet<string>(records.Where(x => string.Equals(x.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
				.Select(x => x.RecordId).Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
			foreach (string id in records.Where(x => !string.Equals(x.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => x.SubmittedDay).ThenByDescending(x => x.CreatedUtcTicks).Take(MaxEndedLocalPolicyRecords)
				.Select(x => x.RecordId).Where(x => !string.IsNullOrWhiteSpace(x))) keep.Add(id);
			foreach (string key in _localPolicyRecords.Keys.ToList()) if (!keep.Contains(key)) _localPolicyRecords.Remove(key);
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-history-trim-failed", ex.Message);
		}
	}

	private LocalPolicyHistoryData BuildLocalPolicyHistoryData()
	{
		LocalPolicyHistoryData data = new LocalPolicyHistoryData();
		Dictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId = BuildActivePolicyEffectHistoryIndex();
		foreach (LocalPolicyRecordSaveData record in LoadLocalPolicyRecords())
		{
			if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
			{
				data.Records.Add(BuildVassalPolicyHistoryRecordData(record, activeEffectsByRecordId));
				continue;
			}
			List<Settlement> sourceFiefs = ResolveOwnedLocalPolicyFiefs(record.TargetFiefIds);
			List<Settlement> sourceSettlements = ExpandLocalPolicySettlements(sourceFiefs);
			string targetText;
			if (sourceSettlements.Count > 0)
			{
				targetText = BuildLocalPolicyEffectTargetLabel(
					LocalPolicyTargetScopeSource,
					"S",
					"发布地",
					Array.Empty<string>(),
					Array.Empty<string>(),
					false,
					sourceSettlements);
			}
			else
			{
				List<string> originalNames = (record.OriginalTargets ?? new List<LocalPolicyTargetSnapshotSaveData>())
					.Where(x => x != null)
					.Select(x => x.Name)
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				targetText = "发布地：" + (originalNames.Count <= 0
					? "无剩余目标"
					: string.Join("、", originalNames.Take(6))
						+ (originalNames.Count > 6 ? "，另" + (originalNames.Count - 6).ToString(CultureInfo.InvariantCulture) + "处" : ""));
			}
			List<LocalPolicyEffectRecordSaveData> mentionedEffects = record.Effects
				.Where(x => x != null && string.Equals(x.TargetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase))
				.ToList();
			foreach (LocalPolicyEffectRecordSaveData mentionedEffect in mentionedEffects)
			{
				List<Settlement> currentMentioned = ResolveLocalMentionedPolicySettlements(
					mentionedEffect.TargetClanIds,
					mentionedEffect.DirectTargetSettlementIds,
					mentionedEffect.FollowCurrentRulingClan,
					sourceFiefs);
				targetText += "；" + BuildLocalPolicyEffectTargetLabel(
					mentionedEffect.TargetScope,
					mentionedEffect.TargetHandle,
					mentionedEffect.TargetLabel,
					mentionedEffect.TargetClanIds,
					mentionedEffect.DirectTargetSettlementIds,
					mentionedEffect.FollowCurrentRulingClan,
					currentMentioned);
			}
			string statusText = GetLocalPolicyStatusText(record.Status);
			string effectStatusText = GetLocalPolicyStatusText(record.EffectStatus);
			string renewalHistory = record.Renewals.Count <= 0
				? "延长效果历史：无"
				: "延长效果历史：\n" + string.Join("\n", record.Renewals.Select(x => "- " + (x.DateText ?? "未知日期") + "：增加 " + x.AddedDays.ToString(CultureInfo.InvariantCulture) + " 天"));
			data.Records.Add(new LocalPolicyHistoryRecordData
			{
				ScopeKind = PolicyScopeLocal,
				RecordId = record.RecordId,
				DateText = string.IsNullOrWhiteSpace(record.DateText) ? "未知日期" : record.DateText,
				PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? "未命名地方政策" : record.PolicyName,
				StatusText = statusText,
				TargetText = targetText,
				RemainingText = record.IsPermanentEffect
					? "数值效果：永久"
					: "效果剩余 " + record.RemainingDays.ToString(CultureInfo.InvariantCulture) + " 天；原始效果周期 " + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天",
				ContentText = record.PolicyContent ?? "",
				FeedbackText = string.IsNullOrWhiteSpace(record.PublicFeedback) ? "未记录民众反馈。" : record.PublicFeedback,
				EffectText = BuildLocalPolicyEffectText(record),
				CostText = "启动所需 " + record.RequiredGoldCost.ToString(CultureInfo.InvariantCulture)
					+ "；启动实付 " + record.InitialActualGoldCost.ToString(CultureInfo.InvariantCulture)
					+ "；每日维护 " + record.DailyMaintenanceGoldCost.ToString(CultureInfo.InvariantCulture)
					+ "；累计维护 " + record.TotalMaintenancePaidGold.ToString(CultureInfo.InvariantCulture)
					+ " 第纳尔；效果比例 " + FormatPercent(record.GoldEffectScale),
				CycleText = "政策状态：" + statusText + "；效果状态：" + effectStatusText
					+ (string.IsNullOrWhiteSpace(record.EndReason) ? "" : "（" + BuildPlayerVisiblePolicyEndReason(record.EndReason) + "）")
					+ "；延长次数 " + record.RenewalCount.ToString(CultureInfo.InvariantCulture)
					+ BuildPolicyRuntimeStatusSuffix(record.RecordId, activeEffectsByRecordId),
				RenewalText = renewalHistory,
				CanRenew = !record.IsPermanentEffect
					&& string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
					&& record.TargetFiefIds.Count > 0,
				CanAbolish = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			});
		}
		return data;
	}

	private LocalPolicyHistoryRecordData BuildVassalPolicyHistoryRecordData(
		LocalPolicyRecordSaveData record,
		IReadOnlyDictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId)
	{
		bool relationValid = VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int currentIndependence, out int breakawayThreshold, out int rulerRelation, out string rulerName);
		string targetName = FirstNonEmpty(record.TargetKingdomName, ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName)?.Name?.ToString(), "未知附庸国");
		string issuerName = FirstNonEmpty(record.IssuerKingdomName, "未知宗主国");
		string targetText = "宗主国：" + issuerName + "；目标附庸国：" + targetName;
		if (relationValid)
		{
			targetText += "；当前独立度 " + currentIndependence.ToString(CultureInfo.InvariantCulture)
				+ "/100；脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture)
				+ "（" + rulerName + "关系 " + FormatSigned(rulerRelation) + "）";
		}
		else
		{
			targetText += "；臣属关系已失效";
		}
		string statusText = GetLocalPolicyStatusText(record.Status);
		int initialNetChange = record.InitialIndependenceCost + record.VassalQualityIndependenceDelta;
		string costText = "首次独立度结算：" + record.IndependenceBefore.ToString(CultureInfo.InvariantCulture)
			+ " + 随机费用 " + record.InitialIndependenceCost.ToString(CultureInfo.InvariantCulture)
			+ " + 政策修正 " + FormatSigned(record.VassalQualityIndependenceDelta)
			+ " = " + record.IndependenceAfter.ToString(CultureInfo.InvariantCulture)
			+ "（净变化 " + FormatSigned(initialNetChange) + "）"
			+ "；累计随机费用 " + record.TotalIndependenceCost.ToString(CultureInfo.InvariantCulture);
		if (!string.IsNullOrWhiteSpace(record.IndependenceReason))
		{
			costText += "；修正原因：" + record.IndependenceReason;
		}
		string renewalHistory = record.Renewals.Count <= 0
			? "续约历史：无"
			: "续约历史：\n" + string.Join("\n", record.Renewals.Select(x =>
				"- " + (x.DateText ?? "未知日期")
				+ "：独立度 " + x.IndependenceBefore.ToString(CultureInfo.InvariantCulture)
				+ " + " + x.IndependenceCost.ToString(CultureInfo.InvariantCulture)
				+ " = " + x.IndependenceAfter.ToString(CultureInfo.InvariantCulture)
				+ "，增加 " + x.AddedDays.ToString(CultureInfo.InvariantCulture) + " 天"));
		bool renewableStatus = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(record.Status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase);
		return new LocalPolicyHistoryRecordData
		{
			ScopeKind = PolicyScopeVassal,
			RecordId = record.RecordId,
			DateText = string.IsNullOrWhiteSpace(record.DateText) ? "未知日期" : record.DateText,
			PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? "未命名附庸国政策" : record.PolicyName,
			StatusText = statusText,
			TargetText = targetText,
			RemainingText = record.IsPermanentEffect
				? "数值效果：永久"
				: "剩余 " + record.RemainingDays.ToString(CultureInfo.InvariantCulture) + " 天；原始周期 " + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天",
			ContentText = record.PolicyContent ?? "",
			FeedbackText = string.IsNullOrWhiteSpace(record.PublicFeedback) ? "未记录政策反馈。" : record.PublicFeedback,
			EffectText = BuildLocalPolicyEffectText(record),
			CostText = costText,
			CycleText = "状态：" + statusText + (string.IsNullOrWhiteSpace(record.EndReason) ? "" : "（" + BuildPlayerVisiblePolicyEndReason(record.EndReason) + "）")
				+ "；续约次数 " + record.RenewalCount.ToString(CultureInfo.InvariantCulture)
				+ BuildPolicyRuntimeStatusSuffix(record.RecordId, activeEffectsByRecordId),
			RenewalText = renewalHistory,
			CanRenew = !record.IsPermanentEffect && renewableStatus && relationValid && IsPlayerRuler(GetPlayerKingdom()),
			CanAbolish = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
		};
	}

	private static string BuildLocalPolicyEffectText(LocalPolicyRecordSaveData record)
	{
		record = NormalizeLocalPolicyRecord(record);
		if (record?.Effects == null || record.Effects.Count == 0)
		{
			return "无模块效果";
		}
		bool isVassalPolicy = string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		List<string> lines = new List<string>();
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
		{
			string label = isVassalPolicy
				? FirstNonEmpty(effect.TargetKingdomName, effect.TargetLabel, "未知国家")
				: (!string.IsNullOrWhiteSpace(effect.TargetLabel)
				? effect.TargetLabel
				: (string.Equals(effect.TargetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase)
					? "本国提及目标效果"
					: "发布地效果"));
			foreach (string value in BuildPlayerVisibleEffectLinesForExternal(label, effect.ModuleEffects, -1)
				.Where(value => !string.IsNullOrWhiteSpace(value)))
			{
				string visibleReason = CleanPlayerVisiblePolicySystemText(effect.Reason, string.Empty);
				lines.Add(value.Trim()
					+ (string.IsNullOrWhiteSpace(visibleReason) ? "" : "｜原因：" + visibleReason));
			}
		}
		return lines.Count <= 0 ? "无模块效果" : string.Join("\n", lines);
	}

	private static string GetLocalPolicyStatusText(string status)
	{
		if (string.Equals(status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)) return "有效";
		if (string.Equals(status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase)) return "自然到期";
		if (string.Equals(status, LocalPolicyStatusTargetsLost, StringComparison.OrdinalIgnoreCase)) return "全部失地";
		if (string.Equals(status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)) return "玩家废除";
		if (string.Equals(status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase)) return "臣属关系终止";
		return "已结束";
	}

	private static List<Settlement> GetPlayerOwnedLocalPolicyFiefs()
	{
		try
		{
			return (Clan.PlayerClan?.Settlements ?? Enumerable.Empty<Settlement>())
				.Where(IsPlayerOwnedLocalPolicyFief)
				.OrderBy(x => x.Name?.ToString() ?? x.StringId, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private static List<Settlement> ResolveOwnedLocalPolicyFiefs(IEnumerable<string> fiefIds)
	{
		HashSet<string> ids = new HashSet<string>(NormalizeIdList(fiefIds), StringComparer.OrdinalIgnoreCase);
		return GetPlayerOwnedLocalPolicyFiefs().Where(x => ids.Contains(x.StringId ?? "")).ToList();
	}

	private static List<Settlement> GetBoundVillageSettlements(Settlement fief)
	{
		try
		{
			return (fief?.BoundVillages ?? Enumerable.Empty<Village>())
				.Where(x => x?.Settlement != null)
				.Select(x => x.Settlement)
				.Distinct()
				.ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private static List<Settlement> ExpandLocalPolicySettlements(IEnumerable<Settlement> fiefs)
	{
		List<Settlement> result = new List<Settlement>();
		foreach (Settlement fief in (fiefs ?? Enumerable.Empty<Settlement>()).Where(x => x != null))
		{
			result.Add(fief);
			result.AddRange(GetBoundVillageSettlements(fief));
		}
		return result.Where(x => x != null).GroupBy(x => x.StringId ?? "", StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
	}

	private static string GetLocalPolicyFiefTypeText(Settlement fief)
	{
		return fief?.IsCastle == true ? "城堡" : "城镇";
	}

	private static LocalPolicyTargetSnapshotSaveData BuildLocalPolicyTargetSnapshot(Settlement fief)
	{
		return new LocalPolicyTargetSnapshotSaveData
		{
			FiefId = fief?.StringId ?? "",
			Name = fief?.Name?.ToString() ?? fief?.StringId ?? "未知封地",
			TypeText = GetLocalPolicyFiefTypeText(fief),
			BoundVillageNames = GetBoundVillageSettlements(fief).Select(x => x.Name?.ToString() ?? x.StringId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
		};
	}

	private void OpenRecordHistoryPopup(Action onClose)
	{
		PolicyHistoryData data = BuildPolicyHistoryData();
		if (!CustomPolicyHistoryPopup.Show(data, onClose))
		{
			InformationManager.ShowInquiry(new InquiryData(data.TitleText ?? "政策记录", BuildPolicyHistoryFallbackText(data), true, false, "返回", "", onClose, null), pauseGameActiveState: true, prioritize: false);
		}
	}

	private void OpenLocalPolicyHistoryPopup(Action onClose)
	{
		TrimLocalPolicyRecords();
		LocalPolicyHistoryData data = BuildLocalPolicyHistoryData();
		if (!LocalPolicyHistoryPopup.Show(data,
			recordId => RequestRenewLocalPolicy(recordId, onClose),
			recordId => RequestAbolishLocalPolicy(recordId, onClose),
			onClose))
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策记录", "打开地方政策记录界面失败。", true, false, "返回", "", onClose, null), pauseGameActiveState: true);
		}
	}

	private void RequestRenewLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("地方政策记录不存在。", Colors.Red));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (record.IsPermanentEffect)
		{
			InformationManager.DisplayMessage(new InformationMessage("永久效果不需要延长。", Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
		{
			RequestRenewVassalPolicy(record, onClose);
			return;
		}
		if (!string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("已被玩家废除的地方政策不能再延长效果。", Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		List<Settlement> ownedTargets = ResolveOwnedLocalPolicyFiefs(record.TargetFiefIds);
		if (ownedTargets.Count <= 0)
		{
			record.EffectStatus = LocalPolicyStatusTargetsLost;
			record.EndReason = "延长效果时已无任何原目标归玩家所有";
			record.TargetFiefIds.Clear();
			record.RemainingDays = 0;
			_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
			InformationManager.ShowInquiry(new InquiryData("无法延长效果", "原目标封地已经全部失去，数值效果无法恢复；政策记录仍会保留，可由玩家主动废除。", true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("延长地方政策效果", "是否为《" + record.PolicyName + "》增加一个完整效果周期（" + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天）？\n\n延长效果不再次收取启动费，不重新调用智能服务，也不会重放一次性效果或再次发布民众反馈。", true, true, "确认延长", "取消",
			() => ConfirmRenewLocalPolicy(record.RecordId, onClose),
			() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
	}

	private void ConfirmRenewLocalPolicy(string recordId, Action onClose)
	{
		bool hadOldRecord = _localPolicyRecords.TryGetValue(recordId ?? string.Empty, out string oldRecordRaw);
		Dictionary<string, string> oldActiveRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		bool renewalCommitted = false;
		try
		{
			LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
			if (record == null) throw new InvalidOperationException("地方政策记录不存在。");
			if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				|| record.IsPermanentEffect
				|| !string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("该地方政策当前不能延长效果。");
			}
			oldActiveRaw = SnapshotPolicyEffectRawByRecordId(record.RecordId, PolicyScopeLocal);
			List<Settlement> ownedTargets = ResolveOwnedLocalPolicyFiefs(record.TargetFiefIds);
			if (ownedTargets.Count <= 0) throw new InvalidOperationException("原目标封地已经全部失去。");
			const int charge = 0;
			List<ActivePolicyEffectSaveData> activeEffects = LoadActiveLocalPolicyEffectsByRecordId(record.RecordId);
			ActivePolicyEffectSaveData sourceActive = activeEffects.FirstOrDefault(x => !IsMentionedLocalPolicyEffect(x));
			int renewedRemainingDays = sourceActive == null ? record.OriginalDurationDays : checked(sourceActive.RemainingDays + record.OriginalDurationDays);
			int renewedTotalDurationDays = sourceActive == null ? record.OriginalDurationDays : checked(sourceActive.TotalDurationDays + record.OriginalDurationDays);
			int submittedDay = sourceActive?.SubmittedDay ?? GetCurrentCampaignDay();
			int lastAppliedDay = sourceActive?.LastAppliedDay ?? GetCurrentCampaignDay();
			long createdUtcTicks = sourceActive?.CreatedUtcTicks > 0L
				? sourceActive.CreatedUtcTicks
				: DateTime.UtcNow.Ticks;
			bool maintenanceFunded = sourceActive?.MaintenanceFunded ?? true;
			int lastMaintenanceSettlementDay = sourceActive?.LastMaintenanceSettlementDay ?? GetCurrentCampaignDay();
			int lastEffectProcessedDay = sourceActive?.LastEffectProcessedDay ?? GetCurrentCampaignDay();
			int renewedTotalPaidGold = record.TotalPaidGold;
			List<ActivePolicyEffectSaveData> renewedShellEffects = new List<ActivePolicyEffectSaveData>();
			List<LocalPolicyEffectRecordSaveData> renewedShellRecords = new List<LocalPolicyEffectRecordSaveData>();
			record.ActiveEffectId = string.Empty;
			foreach (LocalPolicyEffectRecordSaveData effectRecord in record.Effects.Where(x => x != null))
			{
				ActivePolicyEffectSaveData active = CreateActiveLocalPolicyEffectFromRecord(
					record,
					effectRecord,
					ownedTargets,
					renewedRemainingDays,
					renewedTotalDurationDays,
					submittedDay,
					lastAppliedDay,
					createdUtcTicks,
					maintenanceFunded,
					lastMaintenanceSettlementDay,
					lastEffectProcessedDay);
				Kingdom activeTargetKingdom = string.IsNullOrWhiteSpace(active.TargetKingdomId)
					? GetPlayerKingdom()
					: ResolveKingdomByIdOrName(active.TargetKingdomId, active.TargetKingdomName);
				RefreshActivePolicyEffectCanonicalTargets(active, ownedTargets, activeTargetKingdom);
				BindRenewedPolicyEffectInstanceMetadata(active);
				active.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(active.ModuleEffects);
				if ((active.TargetSettlementIds?.Count ?? 0) == 0
					&& !(active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
						.Any(instance => HasDynamicPolicyEffectTargetDefinition(instance?.TargetSet)))
				{
					if (!IsMentionedLocalPolicyEffect(active))
					{
						throw new InvalidOperationException("发布地政策效果已经没有可执行目标。");
					}
					effectRecord.ActiveEffectId = string.Empty;
					effectRecord.RemainingDays = 0;
					continue;
				}
				effectRecord.ModuleEffects = ClonePolicyEffectSaveDataList(active.ModuleEffects);
				effectRecord.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
					effectRecord.ModuleEffects,
					active.ExecutionReceipts,
					effectRecord.ModuleEffects.Select(instance => instance?.ExecutionReceipt));
				effectRecord.ActiveEffectId = string.Empty;
				effectRecord.RemainingDays = renewedRemainingDays;
				renewedShellEffects.Add(active);
				renewedShellRecords.Add(effectRecord);
			}
			if (!renewedShellEffects.Any(active => !IsMentionedLocalPolicyEffect(active)))
			{
				throw new InvalidOperationException("发布地政策效果已经无法恢复。");
			}
			if (!TryMergeRenewedPolicyEffectShells(renewedShellEffects, string.Empty, out ActivePolicyEffectSaveData renewedActive, out string mergeError))
			{
				throw new InvalidOperationException("地方政策多目标效果无法合并：" + mergeError);
			}
			List<ActivePolicyEffectSaveData> renewedActiveEffects = new List<ActivePolicyEffectSaveData> { renewedActive };
			foreach (LocalPolicyEffectRecordSaveData effectRecord in renewedShellRecords)
			{
				effectRecord.ActiveEffectId = renewedActive.EffectId;
			}
			record.ActiveEffectId = renewedActive.EffectId;
			record.Status = LocalPolicyStatusActive;
			record.EffectStatus = LocalPolicyStatusActive;
			record.EndReason = "";
			record.TargetFiefIds = ownedTargets.Select(x => x.StringId).ToList();
			record.RemainingDays = renewedRemainingDays;
			record.RenewalCount++;
			record.TotalPaidGold = renewedTotalPaidGold;
			record.Renewals.Add(new LocalPolicyRenewalSaveData { Day = GetCurrentCampaignDay(), DateText = FormatCurrentCampaignDate(), PaidGold = charge, AddedDays = record.OriginalDurationDays });
			if (!TryPrepareRenewedPolicyEffectsForCommit(renewedActiveEffects, out string lifecycleError))
			{
				throw new InvalidOperationException("延长效果的模块生命周期准备失败：" + lifecycleError);
			}
			if (!TrySynchronizeRenewalRecordEffects(record, renewedActiveEffects, out string synchronizationError))
			{
				throw new InvalidOperationException("延长效果的 record shell 同步失败：" + synchronizationError);
			}
			if (!TrySerializePreparedRenewalState(record, renewedActiveEffects, out string renewedRecordRaw, out Dictionary<string, string> renewedActiveRaw, out string serializationError))
			{
				throw new InvalidOperationException("延长效果状态验证失败：" + serializationError);
			}
			ReplacePreparedPolicyRenewalState(
				record.RecordId,
				hadOldRecord,
				oldRecordRaw,
				oldActiveRaw,
				renewedRecordRaw,
				renewedActiveRaw,
				renewedActiveEffects);
			renewalCommitted = true;
			EnqueueRenewedPolicyEffectWork(renewedActiveEffects);
			InvokeLocalPolicyLifecycleMemoryHook("renewed", record.RecordId, record.TargetFiefIds);
			InformationManager.ShowInquiry(new InquiryData("效果已延长", "《" + record.PolicyName + "》的数值效果已增加 " + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天，当前剩余 " + record.RemainingDays.ToString(CultureInfo.InvariantCulture) + " 天。", true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			if (renewalCommitted)
			{
				PolicySystemLog.Write("Local", "renew-post-commit-failed", ex.ToString());
				InformationManager.DisplayMessage(new InformationMessage("地方政策效果已延长，但界面或通知刷新失败。", Colors.Yellow));
				return;
			}
			PolicySystemLog.Write("Local", "renew-failed", ex.ToString());
			InformationManager.ShowInquiry(new InquiryData("延长效果失败", "延长效果时发生异常，详细技术信息已写入日志。", true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
	}

	private void RequestAbolishLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null || !string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("只有当前有效的地方政策可以废除。", Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.ShowInquiry(new InquiryData("停止附庸国政策", "确定立即停止《" + record.PolicyName + "》吗？\n\n该政策在所有国家的持续效果会立即停止；独立度不会变化，不会退款、不会刷新冷却，且以后不能续约。", true, true, "确认停止", "取消",
				() => ConfirmAbolishLocalPolicy(record.RecordId, onClose),
				() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("废除地方政策", "确定立即废除《" + record.PolicyName + "》吗？\n\n效果会立即停止，已支付费用不退还，且此政策以后不能再延长效果。", true, true, "确认废除", "取消",
			() => ConfirmAbolishLocalPolicy(record.RecordId, onClose),
			() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
	}

	private void ConfirmAbolishLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null) { OpenLocalPolicyHistoryPopup(onClose); return; }
		bool isVassalPolicy = string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		DispatchPolicyEffectRecordAbolishedBeforeRemoval(
			record.RecordId,
			"record:" + record.RecordId + ":abolished:player",
			"player");
		if (isVassalPolicy)
		{
			RemoveVassalPolicyEffectsByRecordId(record.RecordId);
			foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
			{
				NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(record.RecordId, effect.ActiveEffectId, effect.TargetKingdomId, 0, isEnded: true);
			}
		}
		else
		{
			RemoveLocalPolicyEffectsByRecordId(record.RecordId);
		}
		record.ActiveEffectId = "";
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
		{
			effect.ActiveEffectId = "";
			effect.RemainingDays = 0;
		}
		record.Status = LocalPolicyStatusAbolished;
		record.EndReason = isVassalPolicy ? "玩家主动停止" : "玩家主动废除";
		record.RemainingDays = 0;
		_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
		_activePolicyEffectModelCache.Clear();
		if (!isVassalPolicy) InvokeLocalPolicyLifecycleMemoryHook("abolished", record.RecordId, record.TargetFiefIds);
		TrimLocalPolicyRecords();
		string title = isVassalPolicy ? "附庸国政策已停止" : "地方政策已废除";
		string body = isVassalPolicy
			? "《" + record.PolicyName + "》在所有国家的持续效果已经停止；独立度不变。"
			: "《" + record.PolicyName + "》的效果已经停止；费用不退还。";
		InformationManager.ShowInquiry(new InquiryData(title, body, true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
	}

	private List<ActivePolicyEffectSaveData> LoadActiveLocalPolicyEffectsByRecordId(string recordId)
	{
		List<ActivePolicyEffectSaveData> result = new List<ActivePolicyEffectSaveData>();
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			try
			{
				ActivePolicyEffectSaveData effect = GetActivePolicyEffectForWork(item.Key, item.Value ?? "");
				if (IsLocalActivePolicyEffect(effect)
					&& IsPolicyEffectWithinDuration(effect)
					&& string.Equals(effect.RecordId ?? "", recordId ?? "", StringComparison.OrdinalIgnoreCase))
				{
					result.Add(effect);
				}
			}
			catch
			{
			}
		}
		return result;
	}

	private List<ActivePolicyEffectSaveData> LoadActiveVassalPolicyEffectsByRecordId(string recordId)
	{
		List<ActivePolicyEffectSaveData> result = new List<ActivePolicyEffectSaveData>();
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			try
			{
				ActivePolicyEffectSaveData effect = GetActivePolicyEffectForWork(item.Key, item.Value ?? "");
				if (IsVassalActivePolicyEffect(effect)
					&& !effect.Ended
					&& effect.RemainingDays > 0
					&& string.Equals(effect.RecordId ?? "", recordId ?? "", StringComparison.OrdinalIgnoreCase))
				{
					result.Add(effect);
				}
			}
			catch
			{
			}
		}
		return result;
	}

	private void RequestRenewVassalPolicy(LocalPolicyRecordSaveData record, Action onClose)
	{
		if (record == null
			|| (!string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(record.Status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase)))
		{
			InformationManager.DisplayMessage(new InformationMessage("关系终止或玩家停止的附庸国政策不能续约。", Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom targetKingdom = ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName);
		if (!IsPlayerRuler(playerKingdom)
			|| !string.Equals(playerKingdom?.StringId ?? "", record.IssuerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			|| targetKingdom == null
			|| targetKingdom.IsEliminated
			|| !VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int currentIndependence, out int breakawayThreshold, out int rulerRelation, out string rulerName))
		{
			OnVassalRelationshipEndedInternal(record.TargetKingdomId, "续约时目标已不再是直属附庸国");
			InformationManager.ShowInquiry(new InquiryData("无法续约", "目标国家已不再是玩家王国的直属附庸国，政策已经终止。", true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		int independenceCost = Math.Max(0, record.InitialIndependenceCost);
		InformationManager.ShowInquiry(new InquiryData(
			"续约附庸国政策",
			"是否为《" + record.PolicyName + "》增加一个完整周期（" + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天）？\n\n本次会重复增加首次随机费用 " + independenceCost.ToString(CultureInfo.InvariantCulture) + " 点独立度；当前独立度为 " + currentIndependence.ToString(CultureInfo.InvariantCulture) + "/100，脱离阈值为 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSigned(rulerRelation) + "）。不会重新调用智能服务、不会重复政策好坏修正或重放任何一次性模块，也不会新增世界政策事件。",
			true,
			true,
			"确认续约",
			"取消",
			() => ConfirmRenewVassalPolicy(record.RecordId, onClose),
			() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
	}

	private void ConfirmRenewVassalPolicy(string recordId, Action onClose)
	{
		bool hadOldRecord = _localPolicyRecords.TryGetValue(recordId ?? string.Empty, out string oldRecordRaw);
		Dictionary<string, string> oldActiveRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		bool independenceSideEffectStarted = false;
		bool independenceSideEffectReturned = false;
		bool independenceSideEffectSucceeded = false;
		bool policyStateCommitAttempted = false;
		bool renewalCommitted = false;
		try
		{
			LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
			if (record == null || !string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("附庸国政策记录不存在。");
			}
			if (!string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(record.Status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("该政策当前不能续约。");
			}
			oldActiveRaw = SnapshotPolicyEffectRawByRecordId(record.RecordId, PolicyScopeVassal);
			Kingdom playerKingdom = GetPlayerKingdom();
			Kingdom targetKingdom = ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName);
			if (!IsPlayerRuler(playerKingdom)
				|| !string.Equals(playerKingdom?.StringId ?? "", record.IssuerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
				|| targetKingdom == null
				|| targetKingdom.IsEliminated
				|| !VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int independenceBefore, out int breakawayThreshold, out int rulerRelation, out string rulerName))
			{
				OnVassalRelationshipEndedInternal(record.TargetKingdomId, "确认续约时目标已不再是直属附庸国");
				throw new InvalidOperationException("目标国家已不再是玩家王国的直属附庸国。");
			}

			List<ActivePolicyEffectSaveData> activeEffects = LoadActiveVassalPolicyEffectsByRecordId(record.RecordId);
			ActivePolicyEffectSaveData sourceActive = activeEffects.FirstOrDefault(x => string.Equals(x.TargetKingdomId ?? "", record.TargetKingdomId ?? "", StringComparison.OrdinalIgnoreCase));
			int currentDay = GetCurrentCampaignDay();
			int renewedRemainingDays = checked(Math.Max(0, sourceActive?.RemainingDays ?? 0) + record.OriginalDurationDays);
			int renewedTotalDurationDays = checked(Math.Max(0, sourceActive?.TotalDurationDays ?? 0) + record.OriginalDurationDays);
			if (renewedTotalDurationDays <= 0) renewedTotalDurationDays = record.OriginalDurationDays;
			int submittedDay = sourceActive?.SubmittedDay ?? currentDay;
			int lastAppliedDay = sourceActive?.LastAppliedDay ?? currentDay;
			int independenceCost = Math.Max(0, record.InitialIndependenceCost);
			int expectedIndependenceAfter = (int)Math.Max(0L, Math.Min(100L, (long)independenceBefore + independenceCost));
			bool expectedBreakaway = expectedIndependenceAfter >= breakawayThreshold;
			if (!VassalageBehavior.TryPrepareDirectVassalPolicyIndependenceForExternal(
				record.RecordId + ":renewal:" + (record.RenewalCount + 1).ToString(CultureInfo.InvariantCulture),
				record.TargetKingdomId,
				independenceCost,
				0,
				out VassalPolicyExternalCommitPlan externalPlan,
				out string externalPrepareError))
			{
				throw new InvalidOperationException("续约外部提交准备失败：" + externalPrepareError);
			}
			List<ActivePolicyEffectSaveData> renewedShellEffects = new List<ActivePolicyEffectSaveData>();
			List<LocalPolicyEffectRecordSaveData> renewedShellRecords = new List<LocalPolicyEffectRecordSaveData>();
			record.ActiveEffectId = "";
			foreach (LocalPolicyEffectRecordSaveData effectRecord in record.Effects.Where(x => x != null))
			{
				Kingdom effectKingdom = ResolveKingdomByIdOrName(effectRecord.TargetKingdomId, effectRecord.TargetKingdomName);
				if (effectKingdom == null || effectKingdom.IsEliminated)
				{
					effectRecord.ActiveEffectId = "";
					effectRecord.RemainingDays = 0;
					continue;
				}
				ActivePolicyEffectSaveData active = CreateActiveVassalPolicyEffectFromRecord(
					record,
					effectRecord,
					effectKingdom,
					renewedRemainingDays,
					renewedTotalDurationDays,
					submittedDay,
					lastAppliedDay);
				RefreshKingdomPolicyEffectCanonicalTargets(active, effectKingdom);
				BindRenewedPolicyEffectInstanceMetadata(active);
				active.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(active.ModuleEffects);
				active.TargetFiefIds = new List<string>(active.TargetSettlementIds);
				effectRecord.ModuleEffects = ClonePolicyEffectSaveDataList(active.ModuleEffects);
				effectRecord.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
					effectRecord.ModuleEffects,
					active.ExecutionReceipts,
					effectRecord.ModuleEffects.Select(instance => instance?.ExecutionReceipt));
				effectRecord.ActiveEffectId = string.Empty;
				effectRecord.RemainingDays = renewedRemainingDays;
				renewedShellEffects.Add(active);
				renewedShellRecords.Add(effectRecord);
			}
			if (!renewedShellEffects.Any(active => string.Equals(
				active.TargetKingdomId ?? string.Empty,
				record.TargetKingdomId ?? string.Empty,
				StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidOperationException("目标附庸国的政策效果已经无法恢复。");
			}
			if (!TryMergeRenewedPolicyEffectShells(renewedShellEffects, record.TargetKingdomId, out ActivePolicyEffectSaveData renewedActive, out string mergeError))
			{
				throw new InvalidOperationException("附庸国政策多目标效果无法合并：" + mergeError);
			}
			List<ActivePolicyEffectSaveData> renewedActiveEffects = new List<ActivePolicyEffectSaveData> { renewedActive };
			foreach (LocalPolicyEffectRecordSaveData effectRecord in renewedShellRecords)
			{
				effectRecord.ActiveEffectId = renewedActive.EffectId;
			}
			record.ActiveEffectId = renewedActive.EffectId;

			record.Status = LocalPolicyStatusActive;
			record.EndReason = "";
			record.RemainingDays = renewedRemainingDays;
			record.RenewalCount++;
			record.TotalIndependenceCost = checked(record.TotalIndependenceCost + independenceCost);
			record.IndependenceBefore = independenceBefore;
			record.IndependenceAfter = expectedIndependenceAfter;
			record.ExternalTransactionId = externalPlan.TransactionId;
			record.ExternalAgreementId = externalPlan.AgreementId;
			record.ExternalIdempotencyKey = externalPlan.IdempotencyKey;
			record.ExternalCommitState = "externalCommitPending";
			record.ExternalInputsCaptured = true;
			record.ExternalPublicationCost = externalPlan.PublicationCost;
			record.ExternalQualityDelta = externalPlan.QualityDelta;
			record.ExternalIndependenceBefore = externalPlan.IndependenceBefore;
			record.ExternalIndependenceExpected = externalPlan.IndependenceExpected;
			record.ExternalIndependenceActual = externalPlan.IndependenceBefore;
			record.ExternalBreakawayExpected = externalPlan.BreakawayExpected;
			record.ExternalBreakawayActual = false;
			LocalPolicyRenewalSaveData renewal = new LocalPolicyRenewalSaveData
			{
				Day = currentDay,
				DateText = FormatCurrentCampaignDate(),
				IndependenceCost = independenceCost,
				IndependenceBefore = independenceBefore,
				IndependenceAfter = expectedIndependenceAfter,
				AddedDays = record.OriginalDurationDays
			};
			record.Renewals.Add(renewal);
			if (!TryPrepareRenewedPolicyEffectsForCommit(renewedActiveEffects, out string lifecycleError))
			{
				throw new InvalidOperationException("续约模块生命周期准备失败：" + lifecycleError);
			}
			if (!TrySynchronizeRenewalRecordEffects(record, renewedActiveEffects, out string synchronizationError))
			{
				throw new InvalidOperationException("续约 record shell 同步失败：" + synchronizationError);
			}
			if (!TrySerializePreparedRenewalState(record, renewedActiveEffects, out string continuingRecordRaw, out Dictionary<string, string> renewedActiveRaw, out string serializationError))
			{
				throw new InvalidOperationException("续约状态验证失败：" + serializationError);
			}
			LocalPolicyRecordSaveData breakawayRecord = CloneRenewalRecord(record);
			MarkPreparedVassalRenewalRelationshipEnded(breakawayRecord, "续约独立度达到脱离阈值");
			if (!TrySerializePreparedRenewalState(breakawayRecord, Array.Empty<ActivePolicyEffectSaveData>(), out string breakawayRecordRaw, out Dictionary<string, string> _, out string breakawaySerializationError))
			{
				throw new InvalidOperationException("脱离后的续约状态验证失败：" + breakawaySerializationError);
			}

			policyStateCommitAttempted = true;
			ReplacePreparedPolicyRenewalState(
				record.RecordId,
				hadOldRecord,
				oldRecordRaw,
				oldActiveRaw,
				continuingRecordRaw,
				renewedActiveRaw,
				renewedActiveEffects);
			independenceSideEffectStarted = true;
			VassalPolicyExternalCommitResult externalResult
				= VassalageBehavior.CommitDirectVassalPolicyIndependenceForExternal(externalPlan, record.PolicyName);
			independenceSideEffectReturned = true;
			VassalPolicyExternalCommitObservation observation = externalResult?.Observation
				?? VassalageBehavior.ObserveDirectVassalPolicyIndependenceForExternal(externalPlan);
			bool observable = observation?.Observable == true;
			bool unchanged = observable && observation.AgreementMatches
				&& observation.IndependenceActual == independenceBefore && !observation.BreakawayActual;
			if (externalResult == null || externalResult.Kind == VassalPolicyExternalCommitResultKind.Unknown || !observable)
			{
				throw new InvalidOperationException("续约独立度提交结果暂不可观察，已保留 externalCommitPending 供每日幂等对账。");
			}
			if ((externalResult.Kind == VassalPolicyExternalCommitResultKind.Unchanged
					|| externalResult.Kind == VassalPolicyExternalCommitResultKind.Conflict)
				&& unchanged)
			{
				TryRestorePolicyRenewalSnapshot(recordId, PolicyScopeVassal, hadOldRecord, oldRecordRaw, oldActiveRaw);
				throw new InvalidOperationException("续约独立度外部提交未发生，已恢复提交前 AF 状态。");
			}
			independenceSideEffectSucceeded = true;
			int appliedBefore = independenceBefore;
			bool brokeAway = observation.BreakawayActual || !observation.AgreementPresent;
			int independenceAfter = brokeAway ? expectedIndependenceAfter : observation.IndependenceActual;
			PersistVassalExternalCommitState(
				record.RecordId,
				renewedActive.EffectId,
				externalPlan,
				externalResult.Kind == VassalPolicyExternalCommitResultKind.Committed
					|| externalResult.Kind == VassalPolicyExternalCommitResultKind.AlreadyCommitted
					? "externalCommitted" : "externalCommittedReconciled",
				observation,
				externalResult.Error);
			if (brokeAway)
			{
				TryMarkVassalPolicyRelationshipEndedRecord(
					record.RecordId,
					"vassal_policy_independence_threshold",
					activeEffectId: string.Empty,
					out _);
			}
			renewalCommitted = true;
			if (!brokeAway)
			{
				foreach (ActivePolicyEffectSaveData active in renewedActiveEffects)
				{
					NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(record.RecordId, active.EffectId, active.TargetKingdomId, renewedRemainingDays, isEnded: false);
				}
				EnqueueRenewedPolicyEffectWork(renewedActiveEffects);
			}
			NpcRulerPolicyBehavior.TouchPlayerPolicyCooldownForExternal(record.RecordId, currentDay);
			string resultText = "《" + record.PolicyName + "》已增加 " + record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture) + " 天；独立度 " + appliedBefore.ToString(CultureInfo.InvariantCulture) + " + " + independenceCost.ToString(CultureInfo.InvariantCulture) + " = " + independenceAfter.ToString(CultureInfo.InvariantCulture) + "/100；脱离阈值 " + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSigned(rulerRelation) + "）。";
			if (brokeAway) resultText += "\n\n独立度达到脱离阈值，臣属关系已经解除，政策全部持续效果已立即停止。";
			InformationManager.ShowInquiry(new InquiryData("续约成功", resultText, true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			if (renewalCommitted)
			{
				PolicySystemLog.Write("VassalPolicy", "renew-post-commit-failed", ex.ToString());
				InformationManager.DisplayMessage(new InformationMessage("附庸国政策已续约，但续约后的界面或通知刷新失败。", Colors.Yellow));
				return;
			}
			if (!policyStateCommitAttempted
				&& (independenceSideEffectSucceeded || (independenceSideEffectStarted && !independenceSideEffectReturned)))
			{
				TryRestorePolicyRenewalSnapshot(recordId, PolicyScopeVassal, hadOldRecord, oldRecordRaw, oldActiveRaw);
			}
			PolicySystemLog.Write("VassalPolicy", "renew-failed", ex
				+ " independenceSideEffectStarted=" + independenceSideEffectStarted.ToString(CultureInfo.InvariantCulture)
				+ " independenceSideEffectReturned=" + independenceSideEffectReturned.ToString(CultureInfo.InvariantCulture)
				+ " independenceSideEffectSucceeded=" + independenceSideEffectSucceeded.ToString(CultureInfo.InvariantCulture)
				+ (independenceSideEffectSucceeded
					? " externalSideEffectRollback=unavailable"
					: (independenceSideEffectStarted && !independenceSideEffectReturned ? " externalSideEffectOutcome=unknown" : string.Empty)));
			InformationManager.ShowInquiry(new InquiryData("续约失败", "续约处理发生异常，详细技术信息已写入日志。", true, false, "知道了", "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
	}

	private Dictionary<string, string> SnapshotPolicyEffectRawByRecordId(string recordId, string scopeKind)
	{
		Dictionary<string, string> snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			try
			{
				JObject raw = JObject.Parse(item.Value ?? string.Empty);
				string rawRecordId = ((string)raw.GetValue("RecordId", StringComparison.OrdinalIgnoreCase) ?? string.Empty).Trim();
				string rawScope = ((string)raw.GetValue("ScopeKind", StringComparison.OrdinalIgnoreCase) ?? string.Empty).Trim();
				if (string.Equals(rawRecordId, normalizedRecordId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(rawScope, scopeKind ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				{
					snapshot[item.Key] = item.Value ?? string.Empty;
				}
			}
			catch (Exception ex)
			{
				PolicySystemLog.Write("Effect", "renew-snapshot-skip", "effectId=" + (item.Key ?? string.Empty) + " error=" + ex.Message);
			}
		}
		return snapshot;
	}

	private static void BindRenewedPolicyEffectInstanceMetadata(ActivePolicyEffectSaveData active)
	{
		foreach (PolicyEffectInstanceSaveData instance in active?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
		{
			if (instance == null)
			{
				continue;
			}
			instance.PolicyId = active.RecordId ?? instance.PolicyId ?? string.Empty;
			instance.SourceScope = active.ScopeKind ?? instance.SourceScope ?? string.Empty;
		}
	}

	private bool TryMergeRenewedPolicyEffectShells(
		IEnumerable<ActivePolicyEffectSaveData> shellEffects,
		string preferredTargetKingdomId,
		out ActivePolicyEffectSaveData merged,
		out string error)
	{
		merged = null;
		error = string.Empty;
		List<ActivePolicyEffectSaveData> shells = (shellEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
			.Where(active => active != null)
			.ToList();
		if (shells.Count == 0)
		{
			error = "没有可合并的续约 target shell";
			return false;
		}
		string preferredKingdomId = (preferredTargetKingdomId ?? string.Empty).Trim();
		ActivePolicyEffectSaveData source = shells.FirstOrDefault(active =>
			string.Equals(active.ScopeKind ?? string.Empty, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase)
			&& !IsMentionedLocalPolicyEffect(active))
			?? shells.FirstOrDefault(active => preferredKingdomId.Length > 0
				&& string.Equals(active.TargetKingdomId ?? string.Empty, preferredKingdomId, StringComparison.OrdinalIgnoreCase))
			?? shells[0];
		foreach (ActivePolicyEffectSaveData shell in shells)
		{
			if (!string.Equals(shell.RecordId ?? string.Empty, source.RecordId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(shell.ScopeKind ?? string.Empty, source.ScopeKind ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				|| shell.TotalDurationDays != source.TotalDurationDays
				|| shell.RemainingDays != source.RemainingDays
				|| shell.SubmittedDay != source.SubmittedDay
				|| shell.LastAppliedDay != source.LastAppliedDay)
			{
				error = "target shell 的 record/scope/duration/progress 不一致";
				return false;
			}
			HashSet<string> shellInstanceIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (PolicyEffectInstanceSaveData instance in shell.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				string instanceId = (instance?.InstanceId ?? string.Empty).Trim();
				if (instanceId.Length == 0 || !shellInstanceIds.Add(instanceId))
				{
					error = "target shell 包含空或重复的 InstanceId";
					return false;
				}
			}
		}

		List<PolicyEffectInstanceSaveData> allInstances = shells
			.SelectMany(active => active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		List<string> instanceOrder = new List<string>();
		Dictionary<string, List<PolicyEffectInstanceSaveData>> instancesById
			= new Dictionary<string, List<PolicyEffectInstanceSaveData>>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in allInstances)
		{
			string instanceId = instance.InstanceId.Trim();
			if (!instancesById.TryGetValue(instanceId, out List<PolicyEffectInstanceSaveData> grouped))
			{
				grouped = new List<PolicyEffectInstanceSaveData>();
				instancesById.Add(instanceId, grouped);
				instanceOrder.Add(instanceId);
			}
			grouped.Add(instance);
		}
		if (instanceOrder.Count == 0 || instanceOrder.Count > PolicyEffectSaveCodec.MaxInstancesPerPolicy)
		{
			error = "合并后的逻辑模块实例数量无效";
			return false;
		}

		List<PolicyEffectInstanceSaveData> mergedInstances = new List<PolicyEffectInstanceSaveData>(instanceOrder.Count);
		foreach (string instanceId in instanceOrder)
		{
			List<PolicyEffectInstanceSaveData> grouped = instancesById[instanceId];
			PolicyEffectInstanceSaveData canonical = ClonePolicyEffectSaveData(grouped[0]);
			canonical.TargetSet = NormalizePolicyEffectCanonicalTargetSet(canonical.TargetSet);
			for (int index = 1; index < grouped.Count; index++)
			{
				if (!AreRenewedPolicyEffectInstancesMergeCompatible(canonical, grouped[index], out string incompatibility))
				{
					error = "InstanceId " + instanceId + " 的 target shell 不兼容：" + incompatibility;
					return false;
				}
				canonical.TargetSet = MergePolicyEffectCanonicalTargetSets(canonical.TargetSet, grouped[index].TargetSet);
			}
			mergedInstances.Add(canonical);
		}

		merged = source;
		merged.ModuleEffects = mergedInstances;
		merged.TargetFiefIds = NormalizeIdList(shells.SelectMany(active => active.TargetFiefIds ?? new List<string>()))
			.OrderBy(id => id, StringComparer.Ordinal).ToList();
		merged.TargetSettlementIds = CollectPolicyEffectPrimarySettlementIds(merged.ModuleEffects);
		merged.TargetClanIds = NormalizeIdList(shells.SelectMany(active => active.TargetClanIds ?? new List<string>()))
			.OrderBy(id => id, StringComparer.Ordinal).ToList();
		merged.DirectTargetSettlementIds = NormalizeIdList(shells.SelectMany(active => active.DirectTargetSettlementIds ?? new List<string>()))
			.OrderBy(id => id, StringComparer.Ordinal).ToList();
		merged.FollowCurrentRulingClan = shells.Any(active => active.FollowCurrentRulingClan);
		merged.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
			merged.ModuleEffects,
			shells.SelectMany(active => active.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>()),
			allInstances.Select(instance => instance.ExecutionReceipt));
		Dictionary<string, PolicyEffectInstanceSaveData> mergedById = merged.ModuleEffects
			.ToDictionary(instance => instance.InstanceId, StringComparer.Ordinal);
		foreach (PolicyEffectExecutionReceipt receipt in merged.ExecutionReceipts)
		{
			if (!mergedById.TryGetValue(receipt.InstanceId ?? string.Empty, out PolicyEffectInstanceSaveData owner))
			{
				continue;
			}
			receipt.TargetSet = NormalizePolicyEffectCanonicalTargetSet(owner.TargetSet);
			owner.ExecutionReceipt = ClonePolicyEffectExecutionReceipt(receipt);
		}
		return true;
	}

	private static bool AreRenewedPolicyEffectInstancesMergeCompatible(
		PolicyEffectInstanceSaveData canonical,
		PolicyEffectInstanceSaveData candidate,
		out string error)
	{
		error = string.Empty;
		if (canonical == null || candidate == null)
		{
			error = "实例为空";
			return false;
		}
		if (!string.Equals(canonical.ModuleId ?? string.Empty, candidate.ModuleId ?? string.Empty, StringComparison.Ordinal)
			|| !string.Equals(canonical.SourceModuleId ?? string.Empty, candidate.SourceModuleId ?? string.Empty, StringComparison.Ordinal)
			|| canonical.PayloadSchemaVersion != candidate.PayloadSchemaVersion
			|| !PolicyEffectTokensEqual(canonical.Payload, candidate.Payload))
		{
			error = "module/payload 不一致";
			return false;
		}
		if (!string.Equals(canonical.PolicyId ?? string.Empty, candidate.PolicyId ?? string.Empty, StringComparison.Ordinal)
			|| !string.Equals(canonical.SourceScope ?? string.Empty, candidate.SourceScope ?? string.Empty, StringComparison.Ordinal)
			|| !string.Equals(canonical.Reason ?? string.Empty, candidate.Reason ?? string.Empty, StringComparison.Ordinal)
			|| canonical.LifecycleState != candidate.LifecycleState
			|| canonical.StateSchemaVersion != candidate.StateSchemaVersion
			|| !PolicyEffectTokensEqual(canonical.RuntimeState, candidate.RuntimeState)
			|| canonical.StartDay != candidate.StartDay
			|| canonical.EndDay != candidate.EndDay)
		{
			error = "policy/runtime/lifecycle/timeline 不一致";
			return false;
		}
		return true;
	}

	private bool TryPrepareRenewedPolicyEffectsForCommit(
		IEnumerable<ActivePolicyEffectSaveData> activeEffects,
		out string error)
	{
		error = string.Empty;
		foreach (ActivePolicyEffectSaveData active in activeEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
		{
			if (!PolicyEffectActivationCoordinator.ReconcileMechanismLifecycleStates(
				active?.ModuleEffects,
				out _,
				out error))
			{
				return false;
			}
			if (!TryValidateRenewedActivePolicyEffect(active, out error))
			{
				return false;
			}
			foreach (PolicyEffectInstanceSaveData instance in active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				if (!PolicyEffectModuleCatalog.TryGet(instance?.ModuleId, out IPolicyEffectModule module))
				{
					continue;
				}
				if (!PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out string normalizeError)
					|| normalized == null
					|| normalized.IsInert
					|| normalized.RuntimeInstance == null)
				{
					error = module.Id + " 续约实例无效：" + FirstNonEmpty(normalizeError, normalized?.InertReason);
					return false;
				}
				string idempotencyKey = instance.InstanceId + ":renew:" + instance.EndDay.ToString("0.###", CultureInfo.InvariantCulture);
				PolicyEffectPrepareResult prepare = module.Prepare(new PolicyEffectCompileContext
				{
					InstanceId = instance.InstanceId,
					PolicyId = instance.PolicyId,
					Module = module,
					TargetSet = instance.TargetSet,
					Payload = normalized.RuntimeInstance.Payload,
					IdempotencyKey = idempotencyKey,
					StartDay = instance.StartDay,
					EndDay = instance.EndDay,
					SourceScope = instance.SourceScope,
					Reason = instance.Reason
				}, normalized.RuntimeInstance.Payload);
				if (prepare?.Success != true || prepare.PreparedInstance?.Instance == null)
				{
					error = module.Id + " 续约 Prepare 失败：" + (prepare?.Error ?? "未返回 prepared instance");
					return false;
				}
				if (IsNonRenewableOneTimePolicyEffect(module))
				{
					instance.LifecycleState = PolicyEffectLifecycleState.Completed;
					continue;
				}
				if (!(module is IPolicyEffectLifecycleModule lifecycle)
					|| instance.LifecycleState == PolicyEffectLifecycleState.Failed
					|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack)
				{
					continue;
				}
				PolicyEffectExecutionReceipt existingReceipt = instance.ExecutionReceipt
					?? (active.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
						.LastOrDefault(receipt => string.Equals(receipt?.InstanceId, instance.InstanceId, StringComparison.Ordinal));
				PolicyEffectExecutionResult renewed = lifecycle.OnRenewed(new PolicyEffectExecutionContext
				{
					PreparedInstance = prepare.PreparedInstance,
					CampaignDay = GetCurrentCampaignDay(),
					// Renewal lifecycle callbacks may only update RuntimeState. External game mechanics belong in OneShot/Daily executors so this pre-commit step remains rollback-safe.
					GameBridge = null,
					ExistingReceipt = existingReceipt,
					IdempotencyKey = idempotencyKey,
					RuntimeState = instance.RuntimeState?.DeepClone()
				});
				if (renewed?.Success != true)
				{
					error = module.Id + " OnRenewed 失败：" + (renewed?.Error ?? "未返回成功结果");
					return false;
				}
				if (renewed.RuntimeState != null)
				{
					instance.RuntimeState = renewed.RuntimeState.DeepClone();
				}
				if (renewed.Receipt != null)
				{
					if (!string.Equals(renewed.Receipt.InstanceId ?? string.Empty, instance.InstanceId ?? string.Empty, StringComparison.Ordinal)
						|| !string.Equals(renewed.Receipt.ModuleId ?? string.Empty, module.Id, StringComparison.OrdinalIgnoreCase)
						|| string.IsNullOrWhiteSpace(renewed.Receipt.ReceiptId)
						|| renewed.Receipt.TargetSet == null)
					{
						error = module.Id + " OnRenewed 返回了无效回执";
						return false;
					}
					PolicyEffectExecutionReceipt receipt = ClonePolicyEffectExecutionReceipt(renewed.Receipt);
					instance.ExecutionReceipt = receipt;
					active.ExecutionReceipts ??= new List<PolicyEffectExecutionReceipt>();
					active.ExecutionReceipts.RemoveAll(item =>
						string.Equals(item?.InstanceId, receipt.InstanceId, StringComparison.Ordinal)
						|| string.Equals(item?.ReceiptId, receipt.ReceiptId, StringComparison.Ordinal));
					active.ExecutionReceipts.Add(receipt);
				}
			}
			active.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
				active.ModuleEffects,
				active.ExecutionReceipts,
				active.ModuleEffects.Select(instance => instance?.ExecutionReceipt));
			if (!TryValidateRenewedActivePolicyEffect(active, out error))
			{
				return false;
			}
		}
		return true;
	}

	private bool TryValidateRenewedActivePolicyEffect(ActivePolicyEffectSaveData active, out string error)
	{
		error = string.Empty;
		if (active == null
			|| active.Version != 8
			|| string.IsNullOrWhiteSpace(active.EffectId)
			|| string.IsNullOrWhiteSpace(active.RecordId)
			|| active.TotalDurationDays <= 0
			|| active.RemainingDays <= 0
			|| active.Ended
			|| (active.ModuleEffects?.Count ?? 0) <= 0
			|| active.ModuleEffects.Count > PolicyEffectSaveCodec.MaxInstancesPerPolicy)
		{
			error = "Active v8 延长效果结构不完整";
			return false;
		}
		if (IsLocalActivePolicyEffect(active)
			&& (active.TargetSettlementIds?.Count ?? 0) == 0
			&& !(active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Any(instance => HasDynamicPolicyEffectTargetDefinition(instance?.TargetSet)))
		{
			error = "地方续约 Active 缺少当前定居点目标";
			return false;
		}
		if (IsVassalActivePolicyEffect(active))
		{
			Kingdom target = ResolveKingdomByIdOrName(active.TargetKingdomId, active.TargetKingdomName);
			if (target == null || target.IsEliminated)
			{
				error = "附庸国续约 Active 的目标王国无效";
				return false;
			}
		}
		HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
		Dictionary<string, string> moduleIdByInstanceId = new Dictionary<string, string>(StringComparer.Ordinal);
		int totalPayloadBytes = 0;
		foreach (PolicyEffectInstanceSaveData instance in active.ModuleEffects)
		{
			string instanceId = (instance?.InstanceId ?? string.Empty).Trim();
			string normalizeError = string.Empty;
			if (instanceId.Length == 0 || !instanceIds.Add(instanceId)
				|| !PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out normalizeError))
			{
				error = "续约模块实例无效：" + FirstNonEmpty(instanceId, normalizeError);
				return false;
			}
			totalPayloadBytes += Encoding.UTF8.GetByteCount(instance.Payload.ToString(Formatting.None));
			if (totalPayloadBytes > PolicyEffectSaveCodec.MaxTotalPayloadBytes)
			{
				error = "续约模块 payload 总量超过 32KiB";
				return false;
			}
			moduleIdByInstanceId[instanceId] = instance.ModuleId ?? string.Empty;
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
			{
				if (!HasAnyPolicyEffectCanonicalTarget(instance.TargetSet))
				{
					error = "未知模块续约实例缺少可保留的规范目标：" + (instance.ModuleId ?? string.Empty);
					return false;
				}
				continue;
			}
			if (normalized == null || normalized.IsInert || normalized.RuntimeInstance == null)
			{
				error = module.Id + " 续约 payload 无法执行：" + (normalized?.InertReason ?? string.Empty);
				return false;
			}
			string targetError = string.Empty;
			if (!PolicyEffectModuleCatalog.IsAllowedForScope(module, active.ScopeKind)
				|| (!HasPolicyEffectCanonicalTargetsForModule(module, instance.TargetSet)
					&& !HasDynamicPolicyEffectTargetDefinition(instance.TargetSet))
				|| !AreRenewedModuleTargetsResolvable(module, instance.TargetSet, out targetError))
			{
				error = module.Id + " 续约目标或作用域无效：" + targetError;
				return false;
			}
			if (IsNonRenewableOneTimePolicyEffect(module)
				&& instance.LifecycleState != PolicyEffectLifecycleState.Completed)
			{
				error = module.Id + " 续约时不得重放 OneShot";
				return false;
			}
		}
		HashSet<string> receiptIds = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> receiptInstanceIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectExecutionReceipt receipt in active.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
		{
			string receiptId = (receipt?.ReceiptId ?? string.Empty).Trim();
			string receiptInstanceId = (receipt?.InstanceId ?? string.Empty).Trim();
			if (receipt == null
				|| receiptId.Length == 0
				|| !receiptIds.Add(receiptId)
				|| !receiptInstanceIds.Add(receiptInstanceId)
				|| !moduleIdByInstanceId.TryGetValue(receiptInstanceId, out string moduleId)
				|| !string.Equals(moduleId, receipt.ModuleId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				|| receipt.TargetSet == null)
			{
				error = "续约 Active 包含无效、重复或孤立的执行回执";
				return false;
			}
		}
		return true;
	}

	private bool AreRenewedModuleTargetsResolvable(
		IPolicyEffectModule module,
		PolicyEffectCanonicalTargetSet targetSet,
		out string error)
	{
		error = string.Empty;
		bool hasDynamicTargetDefinition = HasDynamicPolicyEffectTargetDefinition(targetSet);
		foreach (PolicyEffectTargetKind targetKind in module?.Descriptor?.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
		{
			IEnumerable<string> ids;
			switch (targetKind)
			{
				case PolicyEffectTargetKind.Settlement: ids = targetSet?.SettlementIds; break;
				case PolicyEffectTargetKind.Town: ids = targetSet?.TownIds; break;
				case PolicyEffectTargetKind.Village: ids = targetSet?.VillageIds; break;
				case PolicyEffectTargetKind.Clan: ids = targetSet?.ClanIds; break;
				case PolicyEffectTargetKind.Kingdom: ids = targetSet?.KingdomIds; break;
				case PolicyEffectTargetKind.Hero: ids = targetSet?.HeroIds; break;
				default: ids = Array.Empty<string>(); break;
			}
			List<string> normalizedIds = NormalizeIdList(ids);
			if (normalizedIds.Count == 0)
			{
				if (hasDynamicTargetDefinition)
				{
					continue;
				}
				error = targetKind + " 目标为空";
				return false;
			}
			foreach (string id in normalizedIds)
			{
				bool valid;
				switch (targetKind)
				{
					case PolicyEffectTargetKind.Settlement: valid = ResolvePolicyEffectSettlementById(id) != null; break;
					case PolicyEffectTargetKind.Town: valid = ResolvePolicyEffectSettlementById(id)?.Town != null; break;
					case PolicyEffectTargetKind.Village: valid = ResolvePolicyEffectSettlementById(id)?.Village != null; break;
					case PolicyEffectTargetKind.Clan: valid = ResolveClanById(id) != null; break;
					case PolicyEffectTargetKind.Kingdom:
						Kingdom kingdom = ResolveKingdomByIdOrName(id, string.Empty);
						valid = kingdom != null && !kingdom.IsEliminated;
						break;
					case PolicyEffectTargetKind.Hero:
						try { valid = Hero.Find(id) != null; }
						catch { valid = false; }
						break;
					default: valid = false; break;
				}
				if (!valid)
				{
					error = targetKind + " 目标不存在：" + id;
					return false;
				}
			}
		}
		return true;
	}

	private static bool HasDynamicPolicyEffectTargetDefinition(PolicyEffectCanonicalTargetSet targetSet)
	{
		return targetSet != null
			&& ((targetSet.TargetPlans?.Count ?? 0) > 0
				|| (targetSet.SelectorIds?.Count ?? 0) > 0
				|| (targetSet.SelectorHandles ?? Enumerable.Empty<string>())
					.Any(handle => !string.IsNullOrWhiteSpace(handle))
				|| targetSet.FollowCurrentRulingClan);
	}

	private static bool TrySynchronizeRenewalRecordEffects(
		LocalPolicyRecordSaveData record,
		IEnumerable<ActivePolicyEffectSaveData> activeEffects,
		out string error)
	{
		error = string.Empty;
		Dictionary<string, ActivePolicyEffectSaveData> activeById = (activeEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
			.Where(active => active != null && !string.IsNullOrWhiteSpace(active.EffectId))
			.ToDictionary(active => active.EffectId, StringComparer.OrdinalIgnoreCase);
		foreach (LocalPolicyEffectRecordSaveData effectRecord in record?.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
		{
			if (effectRecord == null || string.IsNullOrWhiteSpace(effectRecord.ActiveEffectId))
			{
				continue;
			}
			if (!activeById.TryGetValue(effectRecord.ActiveEffectId, out ActivePolicyEffectSaveData active))
			{
				error = "record shell 引用了不存在的 Active：" + effectRecord.ActiveEffectId;
				return false;
			}
			Dictionary<string, PolicyEffectInstanceSaveData> activeByInstanceId = (active.ModuleEffects
				?? new List<PolicyEffectInstanceSaveData>())
				.Where(instance => instance != null && !string.IsNullOrWhiteSpace(instance.InstanceId))
				.ToDictionary(instance => instance.InstanceId.Trim(), StringComparer.Ordinal);
			List<PolicyEffectInstanceSaveData> synchronizedShellInstances = new List<PolicyEffectInstanceSaveData>();
			HashSet<string> shellInstanceIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (PolicyEffectInstanceSaveData shellInstance in effectRecord.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				string instanceId = (shellInstance?.InstanceId ?? string.Empty).Trim();
				if (instanceId.Length == 0 || !shellInstanceIds.Add(instanceId))
				{
					error = "record shell 包含空或重复的 InstanceId";
					return false;
				}
				if (!activeByInstanceId.TryGetValue(instanceId, out PolicyEffectInstanceSaveData activeInstance))
				{
					error = "record shell 的逻辑实例未进入 Active：" + instanceId;
					return false;
				}
				PolicyEffectCanonicalTargetSet shellTargetSet = NormalizePolicyEffectCanonicalTargetSet(shellInstance.TargetSet);
				if (!HasAnyPolicyEffectCanonicalTarget(shellTargetSet))
				{
					error = "record shell 的逻辑实例缺少目标：" + instanceId;
					return false;
				}
				PolicyEffectInstanceSaveData synchronized = ClonePolicyEffectSaveData(activeInstance);
				synchronized.TargetSet = shellTargetSet;
				if (synchronized.ExecutionReceipt != null)
				{
					synchronized.ExecutionReceipt.TargetSet = NormalizePolicyEffectCanonicalTargetSet(shellTargetSet);
				}
				synchronizedShellInstances.Add(synchronized);
			}
			if (synchronizedShellInstances.Count == 0)
			{
				error = "record shell 不包含模块实例";
				return false;
			}
			effectRecord.RemainingDays = active.RemainingDays;
			effectRecord.ModuleEffects = synchronizedShellInstances;
			effectRecord.ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
				effectRecord.ModuleEffects,
				active.ExecutionReceipts,
				effectRecord.ModuleEffects.Select(instance => instance.ExecutionReceipt));
		}
		return true;
	}

	private bool TrySerializePreparedRenewalState(
		LocalPolicyRecordSaveData record,
		IEnumerable<ActivePolicyEffectSaveData> activeEffects,
		out string recordRaw,
		out Dictionary<string, string> activeRawById,
		out string error)
	{
		recordRaw = string.Empty;
		activeRawById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		error = string.Empty;
		try
		{
			List<ActivePolicyEffectSaveData> actives = (activeEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
				.Where(active => active != null)
				.ToList();
			foreach (ActivePolicyEffectSaveData active in actives)
			{
				if (!TryValidateRenewedActivePolicyEffect(active, out error))
				{
					return false;
				}
				string raw = JsonConvert.SerializeObject(active, Formatting.None);
				if (Encoding.UTF8.GetByteCount(raw) > PolicyEffectSaveCodec.MaxSerializedEnvelopeBytes
					|| !PolicyEffectSaveCodec.TryNormalizeActiveV4ToV8(JObject.Parse(raw), out _, out _, out error))
				{
					error = FirstNonEmpty(error, "续约 Active 序列化结果超过限制或无法规范化");
					return false;
				}
				if (activeRawById.ContainsKey(active.EffectId))
				{
					error = "续约 Active effectId 重复：" + active.EffectId;
					return false;
				}
				activeRawById.Add(active.EffectId, raw);
			}
			if (record == null
				|| record.Version != 6
				|| string.IsNullOrWhiteSpace(record.RecordId)
				|| (!string.Equals(record.ScopeKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase)
					&& !string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)))
			{
				error = "续约 record v6 结构不完整";
				return false;
			}
			HashSet<string> referencedActiveIds = new HashSet<string>(
				(record.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
					.Where(effect => effect != null && !string.IsNullOrWhiteSpace(effect.ActiveEffectId))
					.Select(effect => effect.ActiveEffectId.Trim()),
				StringComparer.OrdinalIgnoreCase);
			if (!referencedActiveIds.SetEquals(activeRawById.Keys)
				|| (activeRawById.Count > 0 && !activeRawById.ContainsKey(record.ActiveEffectId ?? string.Empty))
				|| (activeRawById.Count == 0 && !string.IsNullOrWhiteSpace(record.ActiveEffectId)))
			{
				error = "续约 record 与 Active 引用集合不一致";
				return false;
			}
			recordRaw = JsonConvert.SerializeObject(record, Formatting.None);
			if (Encoding.UTF8.GetByteCount(recordRaw) > PolicyEffectSaveCodec.MaxSerializedEnvelopeBytes
				|| !PolicyEffectSaveCodec.TryNormalizeLocalV1ToV6(JObject.Parse(recordRaw), out _, out _, out error))
			{
				error = FirstNonEmpty(error, "续约 record 序列化结果超过限制或无法规范化");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			recordRaw = string.Empty;
			activeRawById.Clear();
			error = "续约状态序列化失败：" + ex.Message;
			return false;
		}
	}

	private void ReplacePreparedPolicyRenewalState(
		string recordId,
		bool hadOldRecord,
		string oldRecordRaw,
		IReadOnlyDictionary<string, string> oldActiveRaw,
		string newRecordRaw,
		IReadOnlyDictionary<string, string> newActiveRaw,
		IEnumerable<ActivePolicyEffectSaveData> newActiveEffects)
	{
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		Dictionary<string, ActivePolicyEffectSaveData> newActiveById = (newActiveEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
			.Where(active => active != null && !string.IsNullOrWhiteSpace(active.EffectId))
			.ToDictionary(active => active.EffectId, StringComparer.OrdinalIgnoreCase);
		if (normalizedRecordId.Length == 0
			|| string.IsNullOrWhiteSpace(newRecordRaw)
			|| newActiveById.Count != (newActiveRaw?.Count ?? 0))
		{
			throw new InvalidOperationException("续约提交数据不完整。");
		}
		foreach (string newEffectId in newActiveById.Keys)
		{
			if (_activePolicyEffects.ContainsKey(newEffectId)
				&& !(oldActiveRaw?.ContainsKey(newEffectId) ?? false))
			{
				throw new InvalidOperationException("续约 effectId 与现有 Active 冲突：" + newEffectId);
			}
		}
		try
		{
			foreach (string oldEffectId in oldActiveRaw?.Keys ?? Enumerable.Empty<string>())
			{
				_activePolicyEffects.Remove(oldEffectId);
				_activePolicyEffectRuntimeCache.Remove(oldEffectId);
				_queuedActivePolicyEffectIds.Remove(oldEffectId);
			}
			foreach (KeyValuePair<string, string> item in newActiveRaw ?? new Dictionary<string, string>())
			{
				_activePolicyEffects[item.Key] = item.Value;
				_activePolicyEffectRuntimeCache[item.Key] = new ActivePolicyEffectRuntimeEntry
				{
					Raw = item.Value,
					Effect = newActiveById[item.Key]
				};
			}
			_localPolicyRecords[normalizedRecordId] = newRecordRaw;
			_activePolicyEffectModelCache.Clear();
			RebuildActivePolicyEffectRuntimeIndex();
		}
		catch (Exception commitError)
		{
			string restoreError = string.Empty;
			try
			{
				foreach (string newEffectId in newActiveById.Keys)
				{
					_activePolicyEffects.Remove(newEffectId);
				}
				foreach (KeyValuePair<string, string> item in oldActiveRaw ?? new Dictionary<string, string>())
				{
					_activePolicyEffects[item.Key] = item.Value;
				}
				if (hadOldRecord)
				{
					_localPolicyRecords[normalizedRecordId] = oldRecordRaw ?? string.Empty;
				}
				else
				{
					_localPolicyRecords.Remove(normalizedRecordId);
				}
				_activePolicyEffectRuntimeCache.Clear();
				_activePolicyEffectModelCache.Clear();
				RebuildActivePolicyEffectRuntimeIndex();
			}
			catch (Exception restoreException)
			{
				restoreError = restoreException.ToString();
			}
			PolicySystemLog.Write("Effect", "renew-commit-rollback", "recordId=" + normalizedRecordId
				+ " commitError=" + commitError
				+ (string.IsNullOrWhiteSpace(restoreError) ? " restored=true" : " restored=false restoreError=" + restoreError));
			throw new InvalidOperationException("续约状态提交失败；" + (string.IsNullOrWhiteSpace(restoreError) ? "已恢复旧快照。" : "旧快照恢复也失败。"), commitError);
		}
	}

	private void TryRestorePolicyRenewalSnapshot(
		string recordId,
		string scopeKind,
		bool hadOldRecord,
		string oldRecordRaw,
		IReadOnlyDictionary<string, string> oldActiveRaw)
	{
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		try
		{
			foreach (string currentEffectId in SnapshotPolicyEffectRawByRecordId(normalizedRecordId, scopeKind).Keys)
			{
				_activePolicyEffects.Remove(currentEffectId);
			}
			foreach (KeyValuePair<string, string> item in oldActiveRaw ?? new Dictionary<string, string>())
			{
				_activePolicyEffects[item.Key] = item.Value;
			}
			if (hadOldRecord)
			{
				_localPolicyRecords[normalizedRecordId] = oldRecordRaw ?? string.Empty;
			}
			else
			{
				_localPolicyRecords.Remove(normalizedRecordId);
			}
			_activePolicyEffectRuntimeCache.Clear();
			_activePolicyEffectModelCache.Clear();
			RebuildActivePolicyEffectRuntimeIndex();
			PolicySystemLog.Write("VassalPolicy", "renew-external-side-effect-state-restored", "recordId=" + normalizedRecordId
				+ " externalSideEffectRollback=unavailable");
		}
		catch (Exception restoreError)
		{
			PolicySystemLog.Write("VassalPolicy", "renew-external-side-effect-state-restore-failed", "recordId=" + normalizedRecordId
				+ " externalSideEffectRollback=unavailable error=" + restoreError);
		}
	}

	private void EnqueueRenewedPolicyEffectWork(IEnumerable<ActivePolicyEffectSaveData> activeEffects)
	{
		int currentDay = GetCurrentCampaignDay();
		foreach (ActivePolicyEffectSaveData active in activeEffects ?? Enumerable.Empty<ActivePolicyEffectSaveData>())
		{
			if (active != null && currentDay > active.SubmittedDay && active.LastAppliedDay < currentDay)
			{
				EnqueueActivePolicyEffectWork(active.EffectId);
			}
		}
	}

	private static LocalPolicyRecordSaveData CloneRenewalRecord(LocalPolicyRecordSaveData record)
	{
		return record == null
			? null
			: JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(JsonConvert.SerializeObject(record, Formatting.None));
	}

	private static void MarkPreparedVassalRenewalRelationshipEnded(LocalPolicyRecordSaveData record, string reason)
	{
		if (record == null)
		{
			return;
		}
		record.Status = LocalPolicyStatusRelationshipEnded;
		record.EndReason = string.IsNullOrWhiteSpace(reason) ? "臣属关系终止" : reason.Trim();
		record.RemainingDays = 0;
		record.ActiveEffectId = string.Empty;
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
		{
			if (effect == null)
			{
				continue;
			}
			effect.ActiveEffectId = string.Empty;
			effect.RemainingDays = 0;
		}
	}

	private static void TryCompensateFailedLocalPolicyRenewalGold(string recordId, int goldBeforeRenewal)
	{
		Hero player = Hero.MainHero;
		if (player == null)
		{
			PolicySystemLog.Write("Local", "renew-gold-compensation-failed", "recordId=" + (recordId ?? string.Empty) + " reason=player-missing");
			return;
		}
		int missingGold = Math.Max(0, goldBeforeRenewal - Math.Max(0, player.Gold));
		if (missingGold <= 0)
		{
			return;
		}
		try
		{
			GiveGoldAction.ApplyBetweenCharacters(null, player, missingGold, true);
			int remainingMissing = Math.Max(0, goldBeforeRenewal - Math.Max(0, player.Gold));
			if (remainingMissing > 0)
			{
				PolicySystemLog.Write("Local", "renew-gold-compensation-failed", "recordId=" + (recordId ?? string.Empty)
					+ " attempted=" + missingGold.ToString(CultureInfo.InvariantCulture)
					+ " stillMissing=" + remainingMissing.ToString(CultureInfo.InvariantCulture));
			}
		}
		catch (Exception compensationError)
		{
			PolicySystemLog.Write("Local", "renew-gold-compensation-failed", "recordId=" + (recordId ?? string.Empty)
				+ " amount=" + missingGold.ToString(CultureInfo.InvariantCulture)
				+ " error=" + compensationError);
		}
	}

	private sealed class PolicyEffectReceiptCandidate
	{
		internal PolicyEffectExecutionReceipt Receipt;

		internal int SourcePriority;

		internal long Sequence;
	}

	private static List<PolicyEffectExecutionReceipt> SelectPolicyEffectExecutionReceiptsForInstances(
		IEnumerable<PolicyEffectInstanceSaveData> instances,
		params IEnumerable<PolicyEffectExecutionReceipt>[] receiptSets)
	{
		List<PolicyEffectInstanceSaveData> instanceList = (instances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null && !string.IsNullOrWhiteSpace(instance.InstanceId))
			.ToList();
		Dictionary<string, PolicyEffectInstanceSaveData> instanceById = new Dictionary<string, PolicyEffectInstanceSaveData>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in instanceList)
		{
			string instanceId = instance.InstanceId.Trim();
			if (!instanceById.ContainsKey(instanceId))
			{
				instanceById[instanceId] = instance;
			}
		}

		Dictionary<string, PolicyEffectReceiptCandidate> candidateByReceiptId = new Dictionary<string, PolicyEffectReceiptCandidate>(StringComparer.Ordinal);
		long sequence = 0;
		IEnumerable<PolicyEffectExecutionReceipt>[] sources = receiptSets ?? Array.Empty<IEnumerable<PolicyEffectExecutionReceipt>>();
		for (int sourcePriority = 0; sourcePriority < sources.Length; sourcePriority++)
		{
			foreach (PolicyEffectExecutionReceipt receipt in sources[sourcePriority] ?? Enumerable.Empty<PolicyEffectExecutionReceipt>())
			{
				sequence++;
				string receiptId = (receipt?.ReceiptId ?? string.Empty).Trim();
				string instanceId = (receipt?.InstanceId ?? string.Empty).Trim();
				if (receipt == null
					|| receiptId.Length == 0
					|| instanceId.Length == 0
					|| receipt.TargetSet == null
					|| float.IsNaN(receipt.CampaignDay)
					|| float.IsInfinity(receipt.CampaignDay)
					|| !instanceById.TryGetValue(instanceId, out PolicyEffectInstanceSaveData instance)
					|| !string.Equals(receipt.ModuleId ?? string.Empty, instance.ModuleId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					|| GetPolicyEffectReceiptSemanticRank(receipt.Status) <= 0)
				{
					continue;
				}
				PolicyEffectReceiptCandidate candidate = new PolicyEffectReceiptCandidate
				{
					Receipt = receipt,
					SourcePriority = sourcePriority,
					Sequence = sequence
				};
				if (!candidateByReceiptId.TryGetValue(receiptId, out PolicyEffectReceiptCandidate existing)
					|| IsPreferredPolicyEffectReceiptCandidate(candidate, existing))
				{
					candidateByReceiptId[receiptId] = candidate;
				}
			}
		}

		Dictionary<string, PolicyEffectReceiptCandidate> selectedByInstanceId = new Dictionary<string, PolicyEffectReceiptCandidate>(StringComparer.Ordinal);
		foreach (PolicyEffectReceiptCandidate candidate in candidateByReceiptId.Values)
		{
			string instanceId = candidate.Receipt.InstanceId.Trim();
			if (!selectedByInstanceId.TryGetValue(instanceId, out PolicyEffectReceiptCandidate existing)
				|| IsPreferredPolicyEffectReceiptCandidate(candidate, existing))
			{
				selectedByInstanceId[instanceId] = candidate;
			}
		}

		List<PolicyEffectExecutionReceipt> selectedReceipts = new List<PolicyEffectExecutionReceipt>();
		HashSet<string> emittedInstanceIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in instanceList)
		{
			string instanceId = instance.InstanceId.Trim();
			if (!selectedByInstanceId.TryGetValue(instanceId, out PolicyEffectReceiptCandidate selected))
			{
				instance.ExecutionReceipt = null;
				continue;
			}
			PolicyEffectExecutionReceipt authoritative = ClonePolicyEffectExecutionReceipt(selected.Receipt);
			instance.ExecutionReceipt = ClonePolicyEffectExecutionReceipt(authoritative);
			if (emittedInstanceIds.Add(instanceId))
			{
				selectedReceipts.Add(authoritative);
			}
		}
		return selectedReceipts;
	}

	private static bool IsPreferredPolicyEffectReceiptCandidate(
		PolicyEffectReceiptCandidate candidate,
		PolicyEffectReceiptCandidate current)
	{
		int candidateStatusRank = GetPolicyEffectReceiptSemanticRank(candidate?.Receipt?.Status ?? PolicyEffectExecutionStatus.Skipped);
		int currentStatusRank = GetPolicyEffectReceiptSemanticRank(current?.Receipt?.Status ?? PolicyEffectExecutionStatus.Skipped);
		if (candidateStatusRank != currentStatusRank)
		{
			// A rollback is terminal for an instance; settled evidence must also outrank failed/skipped copies.
			return candidateStatusRank > currentStatusRank;
		}
		int dayComparison = (candidate?.Receipt?.CampaignDay ?? float.MinValue)
			.CompareTo(current?.Receipt?.CampaignDay ?? float.MinValue);
		if (dayComparison != 0)
		{
			return dayComparison > 0;
		}
		if ((candidate?.SourcePriority ?? -1) != (current?.SourcePriority ?? -1))
		{
			// Callers pass instance-attached receipts last, making them authoritative on exact semantic/time ties.
			return (candidate?.SourcePriority ?? -1) > (current?.SourcePriority ?? -1);
		}
		if ((candidate?.Sequence ?? -1) != (current?.Sequence ?? -1))
		{
			return (candidate?.Sequence ?? -1) > (current?.Sequence ?? -1);
		}
		return string.CompareOrdinal(candidate?.Receipt?.ReceiptId ?? string.Empty, current?.Receipt?.ReceiptId ?? string.Empty) > 0;
	}

	private static int GetPolicyEffectReceiptSemanticRank(PolicyEffectExecutionStatus status)
	{
		switch (status)
		{
			case PolicyEffectExecutionStatus.RolledBack:
				return 4;
			case PolicyEffectExecutionStatus.Applied:
			case PolicyEffectExecutionStatus.AlreadyApplied:
				return 3;
			case PolicyEffectExecutionStatus.Failed:
				return 2;
			case PolicyEffectExecutionStatus.Skipped:
				return 1;
			default:
				return 0;
		}
	}

	private static List<PolicyEffectInstanceSaveData> CreateRenewedPolicyEffectInstances(
		IEnumerable<PolicyEffectInstanceSaveData> recordedInstances,
		IEnumerable<PolicyEffectExecutionReceipt> availableReceipts,
		int addedDurationDays,
		int renewedTotalDurationDays)
	{
		List<PolicyEffectInstanceSaveData> renewed = ClonePolicyEffectSaveDataList(recordedInstances);
		List<PolicyEffectExecutionReceipt> authoritativeReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
			renewed,
			availableReceipts,
			renewed.Select(instance => instance?.ExecutionReceipt));
		Dictionary<string, PolicyEffectExecutionReceipt> receiptByInstance = authoritativeReceipts
			.ToDictionary(receipt => receipt.InstanceId.Trim(), StringComparer.Ordinal);
		foreach (PolicyEffectInstanceSaveData instance in renewed)
		{
			if (receiptByInstance.TryGetValue((instance.InstanceId ?? "").Trim(), out PolicyEffectExecutionReceipt receipt))
			{
				instance.ExecutionReceipt = ClonePolicyEffectExecutionReceipt(receipt);
			}
			else
			{
				instance.ExecutionReceipt = null;
			}
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
			{
				continue;
			}
			if (IsNonRenewableOneTimePolicyEffect(module))
			{
				// A renewal restores the saved outcome; it must never execute an already settled one-shot again.
				instance.LifecycleState = PolicyEffectLifecycleState.Completed;
				continue;
			}
			if (instance.LifecycleState == PolicyEffectLifecycleState.Failed
				|| instance.LifecycleState == PolicyEffectLifecycleState.RolledBack)
			{
				continue;
			}
			instance.LifecycleState = PolicyEffectLifecycleState.Active;
			int addedDays = Math.Max(0, addedDurationDays);
			if (addedDays > 0)
			{
				instance.EndDay = instance.EndDay > instance.StartDay
					? instance.EndDay + addedDays
					: instance.StartDay + Math.Max(1, renewedTotalDurationDays);
			}
		}
		return renewed;
	}

	private static bool IsNonRenewableOneTimePolicyEffect(IPolicyEffectModule module)
	{
		PolicyEffectExecutionKind kind = module?.Descriptor?.ExecutionKind
			?? PolicyEffectExecutionKind.OneShot;
		return kind == PolicyEffectExecutionKind.OneShot
			|| kind == PolicyEffectExecutionKind.ScheduledOnce;
	}

	private ActivePolicyEffectSaveData CreateActiveLocalPolicyEffectFromRecord(
		LocalPolicyRecordSaveData record,
		LocalPolicyEffectRecordSaveData effectRecord,
		List<Settlement> ownedTargets,
		int remainingDays,
		int totalDurationDays,
		int submittedDay,
		int lastAppliedDay,
		long createdUtcTicks,
		bool maintenanceFunded,
		int lastMaintenanceSettlementDay,
		int lastEffectProcessedDay)
	{
		string effectId = Guid.NewGuid().ToString("N");
		string targetScope = NormalizeLocalPolicyTargetScope(effectRecord?.TargetScope);
		bool isMentioned = string.Equals(targetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase);
		List<Settlement> targetSettlements = isMentioned
			? ResolveLocalMentionedPolicySettlements(
				effectRecord?.TargetClanIds,
				effectRecord?.DirectTargetSettlementIds,
				effectRecord?.FollowCurrentRulingClan == true,
				ownedTargets)
			: ExpandLocalPolicySettlements(ownedTargets);
		List<PolicyEffectExecutionReceipt> availableReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
			effectRecord?.ModuleEffects,
			effectRecord?.ExecutionReceipts,
			effectRecord?.ModuleEffects?.Select(instance => instance?.ExecutionReceipt));
		List<PolicyEffectInstanceSaveData> moduleEffects = CreateRenewedPolicyEffectInstances(
			effectRecord?.ModuleEffects,
			availableReceipts,
			record?.OriginalDurationDays ?? 0,
			totalDurationDays);
		ActivePolicyEffectSaveData active = new ActivePolicyEffectSaveData
		{
			Version = 8,
			ModuleEffects = moduleEffects,
			ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
				moduleEffects,
				availableReceipts,
				moduleEffects.Select(instance => instance.ExecutionReceipt)),
			ScopeKind = PolicyScopeLocal,
			LocalTargetScope = isMentioned ? LocalPolicyTargetScopeMentioned : LocalPolicyTargetScopeSource,
			TargetHandle = effectRecord?.TargetHandle ?? (isMentioned ? "legacy-mentioned" : "S"),
			TargetLabel = effectRecord?.TargetLabel ?? "",
			TargetFiefIds = isMentioned
				? targetSettlements.Where(x => x.IsTown || x.IsCastle).Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
				: ownedTargets.Select(x => x.StringId).ToList(),
			TargetSettlementIds = targetSettlements.Select(x => x.StringId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			TargetClanIds = NormalizeIdList(effectRecord?.TargetClanIds),
			DirectTargetSettlementIds = NormalizeIdList(effectRecord?.DirectTargetSettlementIds),
			FollowCurrentRulingClan = effectRecord?.FollowCurrentRulingClan == true,
			EffectId = effectId,
			RecordId = record.RecordId,
			ProposerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
			IssuerKingdomId = FirstNonEmpty(record.IssuerKingdomId, record.TargetKingdomId, GetPlayerKingdom()?.StringId),
			PolicyName = record.PolicyName,
			DateText = FormatCurrentCampaignDate(),
			SubmittedDay = Math.Max(0, submittedDay),
			CreatedUtcTicks = createdUtcTicks > 0L ? createdUtcTicks : DateTime.UtcNow.Ticks,
			TargetKingdomId = GetPlayerKingdom()?.StringId ?? "",
			TargetKingdomName = GetPlayerKingdom() == null ? "" : GetKingdomName(GetPlayerKingdom()),
			TotalDurationDays = Math.Max(1, totalDurationDays),
			RemainingDays = Math.Max(1, remainingDays),
			IsPermanentEffect = false,
			DailyMaintenanceGoldCost = Math.Max(0, record.DailyMaintenanceGoldCost),
			TotalMaintenancePaidGold = Math.Max(0, record.TotalMaintenancePaidGold),
			MaintenanceChargeEnabled = true,
			MaintenanceFunded = maintenanceFunded,
			LastMaintenanceSettlementDay = lastMaintenanceSettlementDay,
			LastEffectProcessedDay = lastEffectProcessedDay,
			LastAppliedDay = Math.Max(0, lastAppliedDay),
			Reason = effectRecord?.Reason ?? "",
			Ended = false,
			EndReason = ""
		};
		return active;
	}

	private ActivePolicyEffectSaveData CreateActiveVassalPolicyEffectFromRecord(
		LocalPolicyRecordSaveData record,
		LocalPolicyEffectRecordSaveData effectRecord,
		Kingdom targetKingdom,
		int remainingDays,
		int totalDurationDays,
		int submittedDay,
		int lastAppliedDay)
	{
		if (record == null || effectRecord == null || targetKingdom == null)
		{
			throw new InvalidOperationException("附庸国政策效果记录无效。");
		}
		string effectId = Guid.NewGuid().ToString("N");
		List<PolicyEffectExecutionReceipt> availableReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
			effectRecord.ModuleEffects,
			effectRecord.ExecutionReceipts,
			effectRecord.ModuleEffects?.Select(instance => instance?.ExecutionReceipt));
		List<PolicyEffectInstanceSaveData> moduleEffects = CreateRenewedPolicyEffectInstances(
			effectRecord.ModuleEffects,
			availableReceipts,
			record.OriginalDurationDays,
			totalDurationDays);
		ActivePolicyEffectSaveData active = new ActivePolicyEffectSaveData
		{
			Version = 8,
			ModuleEffects = moduleEffects,
			ExecutionReceipts = SelectPolicyEffectExecutionReceiptsForInstances(
				moduleEffects,
				availableReceipts,
				moduleEffects.Select(instance => instance.ExecutionReceipt)),
			ScopeKind = PolicyScopeVassal,
			TargetHandle = effectRecord.TargetHandle ?? "",
			TargetLabel = effectRecord.TargetLabel ?? effectRecord.TargetKingdomName ?? "",
			EffectId = effectId,
			RecordId = record.RecordId,
			ProposerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
			IssuerKingdomId = FirstNonEmpty(record.IssuerKingdomId, targetKingdom.StringId),
			PolicyName = record.PolicyName,
			DateText = FormatCurrentCampaignDate(),
			SubmittedDay = Math.Max(0, submittedDay),
			CreatedUtcTicks = DateTime.UtcNow.Ticks,
			TargetKingdomId = targetKingdom.StringId ?? effectRecord.TargetKingdomId ?? "",
			TargetKingdomName = GetKingdomName(targetKingdom),
			TotalDurationDays = Math.Max(1, totalDurationDays),
			RemainingDays = Math.Max(1, remainingDays),
			LastAppliedDay = Math.Max(0, lastAppliedDay),
			Reason = effectRecord.Reason ?? "",
			Ended = false,
			EndReason = ""
		};
		return active;
	}

	private static void InvokeLocalPolicyLifecycleMemoryHook(string eventKind, string recordId, IEnumerable<string> targetFiefIds)
	{
		// Reserved internal extension point. Local policy lifecycle events intentionally do not write NPC/AFEF memory yet.
	}

	private static void ShowPolicyRenewalResultPopup(string policyObjectId, PolicyDraftRequest request, PolicyApplicationResult application)
	{
		string sequencePolicyObjectId = (policyObjectId ?? "").Trim();
		string policyName = string.IsNullOrWhiteSpace(request?.PolicyName) ? "未命名政策" : request.PolicyName.Trim();
		int actualGoldCost = Math.Max(0, request?.GoldCost ?? 0);
		int durationDays = application?.KingdomEffects?.Where(effect => effect != null)
			.Select(effect => Math.Max(0, effect.DurationDays))
			.DefaultIfEmpty(0)
			.Max() ?? 0;
		StringBuilder body = new StringBuilder();
		body.Append("《").Append(policyName).Append("》已续期");
		if (durationDays > 0)
		{
			body.Append(' ').Append(durationDays.ToString(CultureInfo.InvariantCulture)).Append(" 天");
		}
		body.AppendLine("。");
		body.Append("本次续期消耗：").Append(actualGoldCost.ToString(CultureInfo.InvariantCulture)).Append(" 第纳尔。");
		if (request != null && request.GoldEffectScale < 0.9999f)
		{
			body.AppendLine().Append("本期效果按 ").Append(FormatPercent(request.GoldEffectScale)).Append(" 生效。");
		}
		BeginPolicySuccessResultSequence(sequencePolicyObjectId);
		InformationManager.ShowInquiry(new InquiryData("政策已续期", body.ToString(), true, false, "知道了", "", delegate
		{
			CompletePolicySuccessResultSequence(sequencePolicyObjectId);
		}, null), pauseGameActiveState: true);
	}

	private static void ShowPolicySuccessResultPopup(string policyObjectId, string impactText)
	{
		string sequencePolicyObjectId = (policyObjectId ?? "").Trim();
		string bodyText = impactText ?? "";
		BeginPolicySuccessResultSequence(sequencePolicyObjectId);
		bool shown = CustomPolicyResultPopup.Show("政策已经发布", bodyText, "知道了", delegate
		{
			CompletePolicySuccessResultSequence(sequencePolicyObjectId);
		});
		if (!shown)
		{
			InformationManager.ShowInquiry(new InquiryData("政策已经发布", bodyText, true, false, "知道了", "", delegate
			{
				CompletePolicySuccessResultSequence(sequencePolicyObjectId);
			}, null), pauseGameActiveState: true);
		}
	}

	private static void ShowPolicyCommitFailureResultPopup(string policyObjectId)
	{
		string sequencePolicyObjectId = (policyObjectId ?? string.Empty).Trim();
		BeginPolicyApprovalResultSequence(sequencePolicyObjectId);
		try
		{
			InformationManager.ShowInquiry(new InquiryData(
				"政策落地失败",
				"议程投票已经通过，但政策效果、账本或费用的原子提交失败。AnimusForge 已撤销本次采用，政策未生效且不会显示为现行政策。详细原因已写入 PolicySystem.txt。",
				true,
				false,
				"知道了",
				string.Empty,
				delegate { CompletePolicySuccessResultSequence(sequencePolicyObjectId); },
				null),
				pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "commit-failure-popup-failed", "policy=" + sequencePolicyObjectId + " error=" + ex.Message);
			CompletePolicySuccessResultSequence(sequencePolicyObjectId);
		}
	}

	private static bool HasAnyTimedPolicyEffect(PolicyApplicationResult application)
	{
		try
		{
			return application?.KingdomEffects != null
				&& application.KingdomEffects.Any(effect => effect != null
					&& (effect.IsPermanentEffect || effect.DurationDays > 0));
		}
		catch
		{
			return false;
		}
	}
}
