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
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_NotInitialized", "Custom policy feature is not initialized."), Colors.Red));
			return;
		}
		behavior.OpenComposePopup();
	}

	public static void OpenRecordHistoryFromTerminal(Action onClose = null)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PolicyRecordsTitle", "Policy Records"), PolicyUiTexts.Get("AFPOL_UI_NotInitialized", "Custom policy feature is not initialized."), true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), "", onClose, null), pauseGameActiveState: true);
			return;
		}
		behavior.OpenRecordHistoryPopup(onClose);
	}

	internal static bool TryGetKingdomPolicyReReviewAvailabilityForExternal(
		string recordId,
		out bool canReReview,
		out string disabledReason)
	{
		canReReview = false;
		disabledReason = string.Empty;
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		return behavior != null
			&& behavior.TryGetKingdomPolicyReReviewAvailability(recordId, out canReReview, out disabledReason);
	}

	internal static void OpenKingdomPolicyReReviewFromWorldArchive(string recordId, Action onReturn)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_ReReviewPolicyTitle", "Re-review Policy"), PolicyUiTexts.Get("AFPOL_UI_NotInitialized", "Custom policy feature is not initialized."), true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), "", onReturn, null), pauseGameActiveState: true);
			return;
		}
		behavior.OpenPolicyReReview(recordId, onReturn);
	}

	internal static void OpenLocalPolicyManagementFromTerminal(Action onClose = null)
	{
		CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
		if (behavior == null)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicy", "Local Policy"), PolicyUiTexts.Get("AFPOL_UI_LocalPolicyNotInitialized", "Local policy feature is not initialized."), true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), "", onClose, null), pauseGameActiveState: true);
			return;
		}
		behavior.OpenLocalPolicyManagementPopup(onClose);
	}

	private void OpenLocalPolicyManagementPopup(Action onClose)
	{
		bool hasFief = GetPlayerOwnedLocalPolicyFiefs().Count > 0;
		List<InquiryElement> items = new List<InquiryElement>
		{
			new InquiryElement("publish_local", PolicyUiTexts.Get("AFPOL_UI_LocalPublishTitle", "Publish Local Policy"), null, isEnabled: hasFief, hasFief ? PolicyUiTexts.Get("AFPOL_UI_SelectFiefHint", "Select towns or castles owned by the player clan as target fiefs.") : PolicyUiTexts.Get("AFPOL_UI_NoOwnedFiefsWarning", "The player clan currently owns no towns or castles; cannot publish.")),
			new InquiryElement("local_records", PolicyUiTexts.Get("AFPOL_UI_LocalRecordsTitle", "Local Policy Records"), null, isEnabled: true, PolicyUiTexts.Get("AFPOL_UI_LocalRecordsHint", "View policy status, targets, remaining effect days, costs, and renewal history, and extend effects or abolish policies."))
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicy", "Local Policy"), PolicyUiTexts.Get("AFPOL_UI_LocalPolicyDescription", "Local policies are evaluated independently by the intelligence service, settled immediately upon success, and take effect within the selected fiefs."), items, isExitShown: true, 1, 1, PolicyUiTexts.Get("AFPOL_UI_Confirm", "Confirm"), PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), delegate(List<InquiryElement> selected)
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
		OpenLocalPolicyComposePopup(onCancel, null);
	}

	private void OpenLocalPolicyComposePopup(Action onCancel, PolicyReReviewContext reReviewContext)
	{
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PreviousPolicyPending", "The previous policy is still awaiting evaluation; please wait."), Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		List<Settlement> fiefs = GetPlayerOwnedLocalPolicyFiefs();
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, fiefs.Count > 0);
		LocalPolicyComposeData data = new LocalPolicyComposeData
		{
			DateText = FormatCurrentCampaignDate(),
			CanPublish = eligibility.CanPublish,
			BlockReason = eligibility.CanPublish
				? PolicyUiTexts.Get("AFPOL_UI_SelectFiefAndFillPolicy", "Please select target fiefs and fill in policy details.")
				: MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason),
			InitialPolicyName = reReviewContext?.PolicyName ?? string.Empty,
			InitialPolicyContent = reReviewContext?.PolicyContent ?? string.Empty,
			InitialDurationText = BuildPolicyReReviewDurationText(reReviewContext),
			RequireExplicitDuration = reReviewContext != null && !reReviewContext.DurationKnown,
			TitleText = reReviewContext == null ? PolicyUiTexts.Get("AFPOL_UI_LocalPublishTitle", "Publish Local Policy") : PolicyUiTexts.Get("AFPOL_UI_ReReviewLocalPolicyTitle", "Re-review Local Policy"),
			PublishText = reReviewContext == null ? PolicyUiTexts.Get("AFPOL_UI_LocalPublishTitle", "Publish Local Policy") : PolicyUiTexts.Get("AFPOL_UI_SubmitReReview", "Submit Re-review"),
			Fiefs = fiefs.Select(fief =>
			{
				LocalPolicyFiefData item = BuildLocalPolicyFiefUiData(fief);
				item.IsSelected = reReviewContext?.SelectedFiefIds?.Contains(fief.StringId, StringComparer.OrdinalIgnoreCase) == true;
				return item;
			}).ToList()
		};
		Action<string, string, string, string, List<string>> onPublish = reReviewContext == null
			? SubmitLocalPolicyFromPopup
			: (name, content, duration, date, fiefIds) => SubmitLocalPolicyFromPopup(name, content, duration, date, fiefIds, reReviewContext);
		if (!LocalPolicyComposePopup.Show(data, onPublish, RequestPlayerPolicyAutoDraft, onCancel))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_OpenLocalPublishFailed", "Failed to open local policy publishing interface."), Colors.Red));
		}
	}

	private void SubmitLocalPolicyFromPopup(string policyName, string policyContent, string durationText, string capturedDateText, List<string> selectedFiefIds)
	{
		SubmitLocalPolicyFromPopup(policyName, policyContent, durationText, capturedDateText, selectedFiefIds, null);
	}

	private void SubmitLocalPolicyFromPopup(string policyName, string policyContent, string durationText, string capturedDateText, List<string> selectedFiefIds, PolicyReReviewContext reReviewContext)
	{
		policyName = NormalizePolicyName(policyName);
		policyContent = NormalizePolicyContent(policyContent);
		selectedFiefIds = NormalizeIdList(selectedFiefIds);
		int manualDurationDays = 0;
		if (reReviewContext != null && !reReviewContext.DurationKnown && string.IsNullOrWhiteSpace(durationText))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_DurationRequired", "The old record has no confirmed duration; enter a positive integer number of days."), Colors.Yellow));
			OpenLocalPolicyComposePopup(null, reReviewContext);
			return;
		}
		if (!string.IsNullOrWhiteSpace(durationText)
			&& (!int.TryParse(durationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out manualDurationDays) || manualDurationDays <= 0))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_DurationPositive", "Duration must be blank or a positive integer number of days."), Colors.Yellow));
			OpenLocalPolicyComposePopup(null, reReviewContext);
			return;
		}
		if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(policyContent) || selectedFiefIds.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_FillNameContentAndFief", "Please enter a policy name, content, and select at least one fief."), Colors.Yellow));
			OpenLocalPolicyComposePopup(null, reReviewContext);
			return;
		}
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PreviousPolicyPending", "The previous policy is still awaiting evaluation; please wait."), Colors.Yellow));
			return;
		}
		List<Settlement> validFiefs = ResolveOwnedLocalPolicyFiefs(selectedFiefIds);
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, validFiefs.Count > 0);
		if (!eligibility.CanPublish)
		{
			InformationManager.DisplayMessage(new InformationMessage(MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason), Colors.Yellow));
			OpenLocalPolicyComposePopup(null, reReviewContext);
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
		if (!ApplyPolicyReReviewLineageToRequest(reReviewContext, request, out string reReviewError))
		{
			InformationManager.DisplayMessage(new InformationMessage(reReviewError, Colors.Yellow));
			OpenLocalPolicyHistoryPopup(null);
			return;
		}
		CaptureUnifiedPolicyHistoryForRequest(request, playerKingdom);
		request.KnowledgeContext = BuildPolicyKnowledgeContextForMainOnly(request);
		if (!TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
		{
			InformationManager.DisplayMessage(new InformationMessage(
				PolicyUiTexts.Get("AFPOL_UI_FreezeSettingsFailedPrefix", "Unable to freeze this policy evaluation configuration: ") + MapPlayerPolicyFailureReasonToDisplay(freezeError),
				Colors.Yellow));
			return;
		}
		PolicySystemLog.Lifecycle("Player", "generation-start", "started", new PolicyLogContext
		{
			RequestId = request.RequestId,
			GenerationId = request.RequestId,
			CampaignDay = request.SubmittedDay,
			TargetCount = validFiefs.Count,
			Gold = Math.Max(0, request.GoldCost),
			Influence = Math.Max(0f, request.InfluenceCost)
		});
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
		OpenComposePopup(null, null);
	}

	private void OpenComposePopup(PolicyReReviewContext reReviewContext)
	{
		OpenComposePopup(reReviewContext, null);
	}

	private void OpenComposePopup(PolicyReReviewContext reReviewContext, Action reReviewReturn)
	{
		if (_generationInProgress)
		{
			PolicyDebugLog("open-blocked", "generation already in progress");
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PreviousPolicyPending", "The previous policy is still awaiting evaluation; please wait."), Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateEligibility(options);
		string dateText = FormatCurrentCampaignDate();
		string statusText = eligibility.CanPublish
			? BuildReadyStatus(options)
			: MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason);
		List<PolicyComposeTargetData> targets = BuildPolicyComposeTargets();
		bool requireExplicitTargetSelection = false;
		if (reReviewContext != null)
		{
			bool vassalScope = string.Equals(reReviewContext.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
			targets = targets.Where(target => string.Equals(target.ScopeKind, vassalScope ? PolicyScopeVassal : PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase)).ToList();
			foreach (PolicyComposeTargetData target in targets)
			{
				target.IsSelected = string.Equals(target.TargetId ?? string.Empty, reReviewContext.SelectedTargetId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
			}
			requireExplicitTargetSelection = !targets.Any(target => target.IsSelected);
		}
		Action returnAction = reReviewContext == null
			? (Action)(delegate { })
			: reReviewReturn ?? (string.Equals(reReviewContext.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				? (Action)(() => OpenLocalPolicyHistoryPopup(null))
				: (Action)(() => OpenRecordHistoryPopup(null)));
		bool shown = CustomPolicyComposePopup.Show(
			reReviewContext == null ? PolicyUiTexts.Get("AFPOL_UI_KingdomPublishTitle", "Publish Kingdom Policy") : (string.Equals(reReviewContext.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase) ? PolicyUiTexts.Get("AFPOL_UI_ReReviewVassalPolicyTitle", "Re-review Vassal Policy") : PolicyUiTexts.Get("AFPOL_UI_ReReviewKingdomPolicyTitle", "Re-review Kingdom Policy")),
			PolicyUiTexts.Get("AFPOL_UI_PolicyNameLabel", "Policy Name"),
			PolicyUiTexts.Get("AFPOL_UI_LocalPolicyContentLabel", "Policy Content / AI Draft Input"),
			dateText,
			eligibility.CanPublish,
			statusText,
			targets,
			reReviewContext == null
				? SubmitPolicyFromPopup
				: (name, content, duration, date, targetId) => SubmitPolicyFromPopup(name, content, duration, date, targetId, reReviewContext, returnAction),
			RequestPlayerPolicyAutoDraft,
			returnAction,
			new PolicyComposePrefillData
			{
				PolicyName = reReviewContext?.PolicyName ?? string.Empty,
				PolicyContent = reReviewContext?.PolicyContent ?? string.Empty,
				DurationText = BuildPolicyReReviewDurationText(reReviewContext),
				PublishText = reReviewContext == null ? string.Empty : PolicyUiTexts.Get("AFPOL_UI_SubmitReReview", "Submit Re-review"),
				RequireExplicitDuration = reReviewContext != null && !reReviewContext.DurationKnown,
				RequireExplicitTargetSelection = requireExplicitTargetSelection
			});
		if (!shown)
		{
			PolicyDebugLog("open-failed", "CustomPolicyComposePopup.Show returned false");
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_OpenComposeFailed", "Failed to open custom policy drafting interface."), Colors.Red));
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
				DisplayText = PolicyUiTexts.Format("AFPOL_UI_TargetOwnKingdom", "Own kingdom: {NAME}", "NAME", GetKingdomName(playerKingdom)),
				HintText = PolicyUiTexts.Get("AFPOL_UI_TargetOwnKingdomHint", "Uses kingdom agenda, policy costs, and voting process."),
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
				DisplayText = PolicyUiTexts.Format("AFPOL_UI_TargetVassalKingdom", "Vassal: {NAME}", "NAME", GetKingdomName(vassal)),
				HintText = PolicyUiTexts.Format("AFPOL_UI_TargetVassalKingdomHint", "Current independence: {CURRENT}/100; Breakaway threshold: {THRESHOLD} ({RULER} relation: {RELATION}); Takes effect immediately upon publication and increases independence by 5-10.", "CURRENT", independence.ToString(CultureInfo.InvariantCulture), "THRESHOLD", breakawayThreshold.ToString(CultureInfo.InvariantCulture), "RULER", rulerName, "RELATION", FormatSigned(rulerRelation))
			});
		}
		return result;
	}

	private void SubmitPolicyFromPopup(string policyName, string policyContent, string durationText, string capturedDateText, string selectedTargetId)
	{
		SubmitPolicyFromPopup(policyName, policyContent, durationText, capturedDateText, selectedTargetId, null, null);
	}

	private void SubmitPolicyFromPopup(
		string policyName,
		string policyContent,
		string durationText,
		string capturedDateText,
		string selectedTargetId,
		PolicyReReviewContext reReviewContext,
		Action reReviewReturn)
	{
		policyName = NormalizePolicyName(policyName);
		policyContent = NormalizePolicyContent(policyContent);
		int manualDurationDays = 0;
		if (reReviewContext != null && !reReviewContext.DurationKnown && string.IsNullOrWhiteSpace(durationText))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_DurationRequired", "The old record has no confirmed duration; enter a positive integer number of days."), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
			return;
		}
		if (!string.IsNullOrWhiteSpace(durationText)
			&& (!int.TryParse(durationText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out manualDurationDays) || manualDurationDays <= 0))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_DurationPositive", "Duration must be blank or a positive integer number of days."), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
			return;
		}
		PolicyDebugLog("submit", "submit clicked nameLength=" + policyName.Length.ToString(CultureInfo.InvariantCulture)
			+ " contentLength=" + policyContent.Length.ToString(CultureInfo.InvariantCulture)
			+ " manualDurationDays=" + manualDurationDays.ToString(CultureInfo.InvariantCulture)
			+ " capturedDate=" + (capturedDateText ?? ""));
		if (string.IsNullOrWhiteSpace(policyName))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_NameCannotBeEmpty", "Policy name cannot be empty."), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
			return;
		}
		if (string.IsNullOrWhiteSpace(policyContent))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_ContentCannotBeEmpty", "Policy content cannot be empty."), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
			return;
		}
		if (_generationInProgress)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PreviousPolicyPending", "The previous policy is still awaiting evaluation; please wait."), Colors.Yellow));
			return;
		}
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions();
		PolicyEligibility eligibility = EvaluateEligibility(options);
		if (!eligibility.CanPublish)
		{
			PolicyDebugLog("submit-blocked", "eligibility failed: " + (eligibility.Reason ?? ""));
			InformationManager.DisplayMessage(new InformationMessage(MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
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
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_VassalInvalidOrNotRuler", "The selected vassal kingdom is no longer valid, or you are no longer the ruler of the suzerain kingdom."), Colors.Yellow));
			OpenComposePopup(reReviewContext, reReviewReturn);
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
				+ PolicyGenerationPromptTexts.Get("VassalWorldHeader", "\n\n[Vassal policy context]\nSuzerain kingdom: ") + GetPolicyPromptKingdomName(playerKingdom)
				+ PolicyGenerationPromptTexts.Get("VassalPublicationTarget", "\nPublication target: ") + GetPolicyPromptKingdomName(policyKingdom)
				+ PolicyGenerationPromptTexts.Get("VassalCurrentIndependence", "\nCurrent independence: ") + currentIndependence.ToString(CultureInfo.InvariantCulture) + "/100。"
				+ PolicyGenerationPromptTexts.Get("VassalCurrentRuler", "\nCurrent ruler: ") + rulerName + PolicyGenerationPromptTexts.Get("VassalPlayerRelation", "; Relation to player: ") + FormatSigned(rulerRelation) + PolicyGenerationPromptTexts.Get("VassalBreakawayThreshold", "; Breakaway threshold: ") + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "。";
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
		if (!ApplyPolicyReReviewLineageToRequest(reReviewContext, request, out string reReviewError))
		{
			InformationManager.DisplayMessage(new InformationMessage(reReviewError, Colors.Yellow));
			if (reReviewContext != null)
			{
				(reReviewReturn ?? (Action)(() => OpenRecordHistoryPopup(null)))();
			}
			return;
		}
		CaptureUnifiedPolicyHistoryForRequest(request, policyKingdom);
		request.KnowledgeContext = BuildPolicyKnowledgeContextForMainOnly(request);
		if (!TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
		{
			InformationManager.DisplayMessage(new InformationMessage(
				PolicyUiTexts.Get("AFPOL_UI_FreezeSettingsFailedPrefix", "Unable to freeze this policy evaluation configuration: ") + MapPlayerPolicyFailureReasonToDisplay(freezeError),
				Colors.Yellow));
			return;
		}
		PolicySystemLog.Lifecycle("Player", "generation-start", "started", new PolicyLogContext
		{
			RequestId = request.RequestId,
			GenerationId = request.RequestId,
			CampaignDay = request.SubmittedDay,
			TargetCount = targetHandles?.Count ?? 0,
			Gold = Math.Max(0, request.GoldCost),
			Influence = Math.Max(0f, request.InfluenceCost)
		});
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
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_RetryOriginalRequestUnavailable", "The original policy request is no longer available; cannot retry."), Colors.Yellow));
			return;
		}
		try
		{
			PolicyDebugLog("manual-retry", BuildPolicyRequestLogPrefix(request));
			string durationText = request.ManualDurationDays > 0
				? request.ManualDurationDays.ToString(CultureInfo.InvariantCulture)
				: string.Empty;
			PolicyReReviewContext reReviewContext = string.IsNullOrWhiteSpace(request.ReReviewRootRecordId)
				? null
				: new PolicyReReviewContext
				{
					ScopeKind = request.ScopeKind ?? PolicyScopeKingdom,
					SourceRecordId = request.ReReviewSourceRecordId ?? string.Empty,
					RootRecordId = request.ReReviewRootRecordId ?? string.Empty,
					SupersedesRecordId = request.SupersedesRecordId ?? string.Empty,
					PolicyName = request.PolicyName ?? string.Empty,
					PolicyContent = request.PolicyContent ?? string.Empty,
					DurationKnown = true,
					IsPermanentEffect = request.ManualDurationDays <= 0,
					DurationDays = Math.Max(0, request.ManualDurationDays),
					SelectedTargetId = request.PlayerKingdomId ?? string.Empty,
					SelectedFiefIds = NormalizeIdList(request.SelectedFiefIds)
				};
			SubmitPolicyFromPopup(
				request.PolicyName,
				request.PolicyContent,
				durationText,
				request.DateText,
				request.PlayerKingdomId,
				reReviewContext,
				null);
		}
		catch (Exception ex)
		{
			_generationInProgress = false;
			PolicySystemLog.Failure("Player", "manual-retry-submit-failed", BuildPolicyRequestLogPrefix(request), ex.ToString());
			ShowPolicyGenerationRetryPopup(request, new PolicyGenerationResult
			{
				FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_036"),
				Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_001")
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
			error = PolicyUiTexts.Get("AFPOL_UI_Management_error_request_empty_0", "Policy request is empty.");
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
			PolicyRecentActiveCount = DuelSettings.GetPolicyRecentActiveCount(),
			PolicyRelatedActiveCount = DuelSettings.GetPolicyRelatedActiveCount(),
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
				isLocal ? PolicyUiTexts.Get("AFPOL_UI_WaitingLocalPolicyEval", "Awaiting Local Policy Evaluation") : (isVassal ? PolicyUiTexts.Get("AFPOL_UI_WaitingVassalPolicyEval", "Awaiting Vassal Policy Evaluation") : PolicyUiTexts.Get("AFPOL_UI_WaitingPolicyEval", "Awaiting Policy Evaluation")),
				isLocal
					? PolicyUiTexts.Format("AFPOL_UI_LocalPolicySubmittedEvaluating", "Local policy \"{NAME}\" is being evaluated by the intelligence service.\n\nGame time is paused. Upon success, it will settle immediately and take effect only on selected fiefs and bound villages without entering kingdom agenda.", "NAME", request.PolicyName)
					: (isVassal
						? PolicyUiTexts.Format("AFPOL_UI_VassalPolicySubmittedEvaluating", "Vassal policy \"{NAME}\" is being evaluated by the intelligence service.\n\nGame time is paused. Upon success, it takes effect directly, is written to vassal and world policy records, and settles independence.", "NAME", request.PolicyName)
						: PolicyUiTexts.Format("AFPOL_UI_PolicySubmittedEvaluating", "Policy \"{NAME}\" has been submitted for court and public evaluation.\n\nGame time is paused. Once the intelligence service completes evaluation, results will be published automatically along with public feedback and effects.", "NAME", request.PolicyName)),
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
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PolicyEvaluatingPaused", "Policy is being evaluated; game time is paused."), Colors.Yellow));
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
		int start = text.IndexOf(PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_071"), StringComparison.Ordinal);
		if (start >= 0)
		{
			text = text.Substring(start + PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_071").Length);
		}
		int end = text.IndexOf(PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_072"), StringComparison.Ordinal);
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
		int start = text.IndexOf(PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_072"), StringComparison.Ordinal);
		if (start >= 0)
		{
			text = text.Substring(start + PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_072").Length);
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
			string cleaned = Regex.Replace(text, PolicyCompatibilityTexts.Get("Legacy_CleanPolicyDisplayText_073"), "", RegexOptions.IgnoreCase).Trim();
			if (string.Equals(cleaned, text, StringComparison.Ordinal))
			{
				break;
			}
			text = cleaned;
		}
		int impactIndex = text.IndexOf(PolicyCompatibilityTexts.Get("Legacy_ExtractMainFeedbackForPopup_072"), StringComparison.Ordinal);
		if (impactIndex > 0)
		{
			text = text.Substring(0, impactIndex).Trim();
		}
		return Regex.Replace(text, "\\s+", " ").Trim();
	}

	private static string MapPlayerPolicyNarrativeFallbackToDisplay(string text)
	{
		switch (text ?? string.Empty)
		{
			case var rawPolicyCase45 when string.Equals(rawPolicyCase45, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_074"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackUnclearDefault", "The public has heard of the new policy, but feedback is not yet clear.");
			case var rawPolicyCase46 when string.Equals(rawPolicyCase46, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_075"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_LocalPolicyFeedbackUnclearDefault", "The people of the selected fiefs and attached villages have heard of this local policy, but feedback is not yet clear.");
			case var rawPolicyCase47 when string.Equals(rawPolicyCase47, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_076"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PolicyImpactDefault", "Policy impact is determined by the evaluator and world state.");
			case var rawPolicyCase48 when string.Equals(rawPolicyCase48, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_077"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_VassalIndependenceNeutralDefault", "The policy was assessed as neutral for the vassal's independence tendency.");
			default:
				return text ?? string.Empty;
		}
	}

	private static string BuildImpactPopupText(PolicyDraftRequest request, string feedback, PolicyApplicationResult application, bool costDeducted)
	{
		StringBuilder sb = new StringBuilder();
		feedback = MapPlayerPolicyNarrativeFallbackToDisplay(CleanPolicyDisplayText(feedback));
		sb.AppendLine(PolicyUiTexts.Format("AFPOL_UI_PolicyNameHeader", "{NAME}", "NAME", request.PolicyName ?? string.Empty));
		sb.AppendLine(PolicyUiTexts.Format("AFPOL_UI_DateLabel", "Date: {DATE}", "DATE", request.DateText));
		sb.AppendLine();
		sb.AppendLine(PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackSection", "[Public Feedback]"));
		sb.AppendLine(string.IsNullOrWhiteSpace(feedback) ? PolicyUiTexts.Get("AFPOL_UI_FeedbackUnclear", "The public has not formed clear feedback yet.") : feedback.Trim());
		sb.AppendLine();
		sb.AppendLine(PolicyUiTexts.Get("AFPOL_UI_PolicyImpactSectionTitle", "[Policy Effects]"));
		if (application?.KingdomEffects != null && application.KingdomEffects.Count > 0)
		{
			foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
			{
				foreach (string line in BuildPlayerVisibleEffectLines(effect))
				{
					sb.AppendLine("- " + line + PolicyUiTexts.Get("AFPOL_UI_SentencePeriod", "."));
				}
			}
		}
		else
		{
			sb.AppendLine(PolicyUiTexts.Get("AFPOL_UI_NoApplicableModuleEffects", "No applicable module effects were produced."));
		}
		if (application?.NoticeLines != null)
		{
			HashSet<string> shownNotices = new HashSet<string>(StringComparer.Ordinal);
			foreach (string line in application.NoticeLines.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				string visibleNotice = CleanPolicyDisplaySystemText(
					line,
					PolicyUiTexts.Get("AFPOL_UI_PartialEffectsFailedNotice", "Some effects failed to take effect; details have been written to the log."));
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
				sb.AppendLine(BuildAiEvaluatedCostPaymentText(request) + PolicyUiTexts.Get("AFPOL_UI_PolicySettledNotice", "Policy effects have been settled per module rules and numeric effects will remain active during validity. You may continue publishing new policies."));
			}
			else
			{
				sb.AppendLine(BuildPlayerPolicyGoldCostSummaryForDisplay(request) + PolicyUiTexts.Get("AFPOL_UI_SentencePeriod", ".") + PolicyUiTexts.Get("AFPOL_UI_PolicySettledNotice", "Policy effects have been settled per module rules and numeric effects will remain active during validity. You may continue publishing new policies."));
			}
		}
		else
		{
			sb.AppendLine(IsVassalPolicyRequest(request)
				? PolicyUiTexts.Get("AFPOL_UI_NoCostDeducted", "No costs were deducted this time.")
				: PolicyUiTexts.Get("AFPOL_UI_NoCostDeducted", "No costs were deducted this time.") + BuildPlayerPolicyGoldCostSummaryForDisplay(request) + PolicyUiTexts.Get("AFPOL_UI_SentencePeriod", "."));
		}
		string popupText = sb.ToString().TrimEnd();
		return popupText;
	}

	internal static void DisplayKingdomPolicyAnnouncementMessage(string source, string policyId, string kingdomName, string policyName, string policyContent)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(
				BuildKingdomPolicyAnnouncementText(kingdomName, policyName, policyContent),
				Color.FromUint(4294945331u)));
			string logMessage = "source=" + (source ?? "")
				+ " policyId=" + (policyId ?? "")
				+ " contentChars=" + CompactPolicyContextText(policyContent ?? "").Length.ToString(CultureInfo.InvariantCulture);
			PolicySystemLog.Lifecycle("Notice", "policy-announcement-displayed", "event", new PolicyLogContext
			{
				PolicyId = policyId,
				TargetKingdomName = kingdomName,
				TargetName = kingdomName,
				MessageChars = logMessage.Length,
				MessageHash = PolicySystemLog.HashSensitive(logMessage)
			});
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Notice", "policy-announcement-failed",
				"source=" + (source ?? "") + " policyId=" + (policyId ?? "") + " " + ex);
		}
	}

	internal static void DisplayPolicyAnnouncementMessage(string source, NpcRulerPolicyRecord record)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(
				BuildPolicyAnnouncementText(record),
				Color.FromUint(4294945331u)));
			string logMessage = "source=" + (source ?? "")
				+ " policyId=" + (record?.PolicyId ?? "")
				+ " policyKind=" + (record?.PolicyKind ?? "")
				+ " contentChars=" + CompactPolicyContextText(record?.PolicyContent ?? "").Length.ToString(CultureInfo.InvariantCulture);
			PolicySystemLog.Lifecycle("Notice", "policy-announcement-displayed", "event", new PolicyLogContext
			{
				PolicyId = record?.PolicyId,
				IssuerKingdomId = record?.IssuerKingdomId,
				IssuerKingdomName = record?.IssuerKingdomName,
				TargetKingdomId = record?.KingdomId,
				TargetKingdomName = record?.KingdomName,
				TargetName = record?.KingdomName,
				MessageChars = logMessage.Length,
				MessageHash = PolicySystemLog.HashSensitive(logMessage)
			});
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Notice", "policy-announcement-failed",
				"source=" + (source ?? "") + " policyId=" + (record?.PolicyId ?? "") + " " + ex);
		}
	}

	private static string BuildPolicyAnnouncementText(NpcRulerPolicyRecord record)
	{
		if (record?.IsPlayerPolicy == true
			&& string.Equals(record.PolicyKind ?? string.Empty, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
		{
			string issuer = CompactPolicyContextText(record.IssuerKingdomName ?? string.Empty);
			string target = CompactPolicyContextText(record.KingdomName ?? string.Empty);
			string name = CompactPolicyContextText(record.PolicyName ?? string.Empty);
			string content = CompactPolicyContextText(record.PolicyContent ?? string.Empty);
			if (string.IsNullOrWhiteSpace(name))
			{
				name = PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy");
			}
			if (string.IsNullOrWhiteSpace(content))
			{
				content = PolicyUiTexts.Get("AFPOL_UI_NoPolicyContentRecorded", "Policy content not recorded.");
			}
			string publisher = string.IsNullOrWhiteSpace(issuer) ? PolicyUiTexts.Get("AFPOL_UI_SuzerainPlayer", "Suzerain Player") : PolicyUiTexts.Format("AFPOL_UI_SuzerainPlayerWithName", "Suzerain Player ({NAME})", "NAME", issuer);
			string targetLabel = string.IsNullOrWhiteSpace(target) ? PolicyUiTexts.Get("AFPOL_UI_TargetVassalDefault", "Target Vassal") : PolicyUiTexts.Format("AFPOL_UI_TargetVassalWithName", "Vassal {NAME}", "NAME", target);
			return PolicyUiTexts.Format("AFPOL_UI_VassalAnnouncementFormat", "[Vassal Policy] {PUBLISHER} enacted \"{NAME}\", taking effect on {TARGET}: {CONTENT}", "PUBLISHER", publisher, "NAME", name, "TARGET", targetLabel, "CONTENT", content);
		}
		return BuildKingdomPolicyAnnouncementText(record?.KingdomName, record?.PolicyName, record?.PolicyContent);
	}

	private static string BuildKingdomPolicyAnnouncementText(string kingdomName, string policyName, string policyContent)
	{
		string issuer = CompactPolicyContextText(kingdomName ?? string.Empty);
		string name = CompactPolicyContextText(policyName ?? string.Empty);
		string content = CompactPolicyContextText(policyContent ?? string.Empty);
		if (string.IsNullOrWhiteSpace(name))
		{
			name = PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy");
		}
		if (string.IsNullOrWhiteSpace(content))
		{
			content = PolicyUiTexts.Get("AFPOL_UI_NoPolicyContentRecorded", "Policy content not recorded.");
		}
		string policySubject = string.IsNullOrWhiteSpace(issuer) ? PolicyUiTexts.Get("AFPOL_UI_KingdomDefault", "Kingdom") : issuer;
		return PolicyUiTexts.Format("AFPOL_UI_KingdomAnnouncementFormat", "[Kingdom Policy] {ISSUER} enacted \"{NAME}\": {CONTENT}", "ISSUER", policySubject, "NAME", name, "CONTENT", content);
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
				name = PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy");
			}
			if (string.IsNullOrWhiteSpace(feedback))
			{
				feedback = PolicyUiTexts.Get("AFPOL_UI_FeedbackUnclear", "The public has not formed clear feedback yet.");
			}
			string policySubject = string.IsNullOrWhiteSpace(issuer) ? PolicyUiTexts.Get("AFPOL_UI_KingdomDefault", "Kingdom") : issuer;
			InformationManager.DisplayMessage(new InformationMessage(
				PolicyUiTexts.Format("AFPOL_UI_PublicFeedbackFormat", "[Public Feedback] {SUBJECT} \"{NAME}\": {FEEDBACK}", "SUBJECT", policySubject, "NAME", name, "FEEDBACK", feedback),
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
			TitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyRecordsTitle", "Policy Records"),
			SubtitleText = PolicyUiTexts.Format("AFPOL_UI_PolicyHistorySubtitle", "Showing latest {COUNT} published policies; legacy policies have ceased effects but history is preserved.", "COUNT", MaxPolicyRecordHistoryCount.ToString(CultureInfo.InvariantCulture)),
			EmptyStateText = PolicyUiTexts.Get("AFPOL_UI_NoSuccessfulPoliciesYet", "No successfully published and enacted policies yet."),
			CloseText = PolicyUiTexts.Get("AFPOL_UI_BackToPolicyManagement", "Back to Policy Management")
		};
		foreach (PolicyRecordSaveData record in records)
		{
			if (IsPolicyHistoryDeleted("player_kingdom", record.RecordId)) continue;
			string effectSummary = BuildPolicyRecordEffectSummary(record);
			string runtimeStatus = BuildPolicyRuntimeStatusText(record?.RecordId, activeEffectsByRecordId);
			bool canReReview = TryBuildPolicyReReviewContext(record?.RecordId, out _, out _);
			data.Records.Add(new PolicyHistoryRecordData
			{
				RecordId = record.RecordId,
				HistoryKey = BuildPolicyHistoryKey("player_kingdom", record.RecordId),
				CanDelete = CanDeletePolicyHistoryRecord(BuildPolicyHistoryKey("player_kingdom", record.RecordId), out _),
				CanReReview = canReReview,
				ReReviewText = PolicyUiTexts.Get("AFPOL_UI_ReReview", "Re-review"),
				DateText = string.IsNullOrWhiteSpace(record.DateText) ? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date") : PolicyDateTextProjection.ForUi(record.DateText.Trim()),
				PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy") : PolicyUiTexts.Format("AFPOL_UI_PolicyNameHeader", "{NAME}", "NAME", record.PolicyName.Trim()),
				CostText = BuildPolicyRecordCostText(record),
				ContentSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyContentSection", "[Policy Content]"),
				ContentSummaryText = string.IsNullOrWhiteSpace(record.PolicyContentSummary) ? PolicyUiTexts.Get("AFPOL_UI_NoContentSummary", "(No policy content summary recorded)") : CleanLlmText(record.PolicyContentSummary),
				FeedbackSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackSection", "[Public Feedback]"),
				FeedbackSummaryText = MapPlayerPolicyNarrativeFallbackToDisplay(string.IsNullOrWhiteSpace(record.PublicFeedbackSummary)
					? PolicyUiTexts.Get("AFPOL_UI_FeedbackNotRecorded", "Public feedback not recorded.")
					: CleanPolicyDisplayText(record.PublicFeedbackSummary)),
				ImpactSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyImpactSectionTitle", "[Policy Effects]"),
				ImpactSummaryText = AppendPolicyRuntimeStatus(
					MapPlayerPolicyNarrativeFallbackToDisplay(string.IsNullOrWhiteSpace(effectSummary)
						? CleanPolicyDisplayText(record.ImpactSummary ?? "")
						: effectSummary),
					runtimeStatus)
			});
		}
		HashSet<string> representedRecordIds = new HashSet<string>(
			data.Records.Select(record => record.RecordId).Where(id => !string.IsNullOrWhiteSpace(id)),
			StringComparer.OrdinalIgnoreCase);
		foreach (DynamicPolicySaveData dynamic in LoadDynamicPolicies()
			.Where(record => record != null
				&& string.Equals(record.Source, "player", StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(record.RecordId)
				&& !string.IsNullOrWhiteSpace(record.PolicyContent)
				&& !IsPolicyHistoryDeleted("player_kingdom", record.RecordId)
				&& !representedRecordIds.Contains(record.RecordId))
			.OrderByDescending(record => record.CreatedUtcTicks)
			.Take(Math.Max(0, MaxPolicyRecordHistoryCount - data.Records.Count)))
		{
			NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(dynamic.RecordId, out NpcRulerPolicyRecord unified);
			data.Records.Add(new PolicyHistoryRecordData
			{
				RecordId = dynamic.RecordId,
				HistoryKey = BuildPolicyHistoryKey("player_kingdom", dynamic.RecordId),
				CanDelete = CanDeletePolicyHistoryRecord(BuildPolicyHistoryKey("player_kingdom", dynamic.RecordId), out _),
				CanReReview = TryBuildPolicyReReviewContext(dynamic.RecordId, out _, out _),
				ReReviewText = PolicyUiTexts.Get("AFPOL_UI_ReReview", "Re-review"),
				DateText = PolicyDateTextProjection.ForUi(FirstNonEmpty(unified?.GameDate, PolicyUiTexts.Get("AFPOL_UI_LegacySaveDateNotRecorded", "Legacy save date not recorded"))),
				PolicyNameText = PolicyUiTexts.Format("AFPOL_UI_PolicyNameHeader", "{NAME}", "NAME", FirstNonEmpty(dynamic.PolicyName, unified?.PolicyName, PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy"))),
				CostText = PolicyUiTexts.Get("AFPOL_UI_LegacySaveCostNotice", "Legacy cost info was not fully preserved; re-evaluation will recalculate using current settings."),
				ContentSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyFullTextSectionTitle", "[Policy Full Text]"),
				ContentSummaryText = CleanLlmText(dynamic.PolicyContent),
				FeedbackSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackSection", "[Public Feedback]"),
				FeedbackSummaryText = MapPlayerPolicyNarrativeFallbackToDisplay(FirstNonEmpty(unified?.PublicFeedback, PolicyUiTexts.Get("AFPOL_UI_LegacySaveNoFeedback", "Legacy save did not preserve public feedback."))),
				ImpactSectionTitleText = PolicyUiTexts.Get("AFPOL_UI_LegacyPolicyStatusSectionTitle", "[Legacy Policy Status]"),
				ImpactSummaryText = PolicyUiTexts.Get("AFPOL_UI_LegacyPolicyUpgradedStopped", "Legacy policy system upgrade has stopped continuous effects.") + (string.IsNullOrWhiteSpace(dynamic.SecondaryEffects) ? string.Empty : "\n" + CleanPolicyDisplayText(dynamic.SecondaryEffects))
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
			lines.Add(PolicyUiTexts.Get("AFPOL_UI_StatusEndPending", "End processing incomplete; system will retry reverting applied effects."));
		}
		if (compensationPending)
		{
			lines.Add(PolicyUiTexts.Get("AFPOL_UI_StatusReverting", "Effects reverting; related modules paused until compensation finishes."));
		}
		if (maintenancePaused)
		{
			lines.Add(PolicyUiTexts.Get("AFPOL_UI_StatusMaintenancePaused", "Insufficient maintenance funds; numeric effects paused, will resume when funds allow."));
		}
		if (lines.Count == 0 && hasActiveEffect)
		{
			lines.Add(PolicyUiTexts.Get("AFPOL_UI_StatusActive", "Active"));
		}
		return string.Join(PolicyUiTexts.Get("AFPOL_UI_RuntimeStatusSeparator", "; "), lines);
	}

	private static string AppendPolicyRuntimeStatus(string text, string runtimeStatus)
	{
		string visibleText = string.IsNullOrWhiteSpace(text) ? PolicyUiTexts.Get("AFPOL_UI_NoRecordedImpact", "No impact effects recorded.") : text.TrimEnd();
		return string.IsNullOrWhiteSpace(runtimeStatus)
			? visibleText
			: visibleText + PolicyUiTexts.Format("AFPOL_UI_RuntimeStatusSection", "\n[Runtime Status] {STATUS}", "STATUS", runtimeStatus.Trim());
	}

	private static string BuildPolicyRuntimeStatusSuffix(
		string recordId,
		IReadOnlyDictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId)
	{
		string runtimeStatus = BuildPolicyRuntimeStatusText(recordId, activeEffectsByRecordId);
		return string.IsNullOrWhiteSpace(runtimeStatus) ? string.Empty : PolicyUiTexts.Get("AFPOL_UI_RuntimeStatusSeparator", "; ") + PolicyUiTexts.Get("AFPOL_UI_RuntimeStatusInlineLabel", "Runtime status: ") + runtimeStatus;
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
				return PolicyUiTexts.Format("AFPOL_UI_RecordCostAiGoldOnly",
					"AI-evaluated cost: startup required {REQUIRED}; startup paid {PAID}; daily upkeep {DAILY}; cumulative upkeep {TOTAL}; all effects {SCALE}",
					"REQUIRED", FormatManagedGoldCostText(record.RequiredGoldCost),
					"PAID", FormatManagedGoldCostText(record.GoldCost),
					"DAILY", FormatManagedGoldCostText(record.DailyMaintenanceGoldCost),
					"TOTAL", FormatManagedGoldCostText(record.TotalMaintenancePaidGold),
					"SCALE", FormatPercent(record.GoldEffectScale <= 0f && record.RequiredGoldCost <= 0 ? 1f : record.GoldEffectScale));
			}
			return PolicyUiTexts.Format("AFPOL_UI_RecordCostAiMixed",
				"AI-evaluated cost: full requirement {REQUIRED}; paid {PAID}; economy/livelihood {GOLD_SCALE}, politics/order {INFLUENCE_SCALE}",
				"REQUIRED", FormatManagedCostText(record.RequiredGoldCost, record.RequiredInfluenceCost),
				"PAID", FormatManagedCostText(record.GoldCost, record.InfluenceCost),
				"GOLD_SCALE", FormatPercent(record.GoldEffectScale <= 0f && record.RequiredGoldCost <= 0 ? 1f : record.GoldEffectScale),
				"INFLUENCE_SCALE", FormatPercent(record.InfluenceEffectScale <= 0f && record.RequiredInfluenceCost <= 0f ? 1f : record.InfluenceEffectScale));
		}
		if ((record?.InfluenceCost ?? 0f) > 0.0001f)
		{
			return PolicyUiTexts.Format("AFPOL_UI_RecordCostPaid", "Paid: {COST}", "COST", FormatManagedCostText(record?.GoldCost ?? 0, record?.InfluenceCost ?? 0f));
		}
		int fixedStartupRequired = Math.Max(0, record?.RequiredGoldCost ?? 0);
		if (fixedStartupRequired == 0 && (record?.GoldCost ?? 0) > 0)
		{
			fixedStartupRequired = record.GoldCost;
		}
		return PolicyUiTexts.Format("AFPOL_UI_RecordCostFixedLine",
			"Startup required {REQUIRED}; startup paid {PAID}; daily upkeep {DAILY}; cumulative upkeep {TOTAL}",
			"REQUIRED", FormatManagedGoldCostText(fixedStartupRequired),
			"PAID", FormatManagedGoldCostText(record?.GoldCost ?? 0),
			"DAILY", FormatManagedGoldCostText(record?.DailyMaintenanceGoldCost ?? 0),
			"TOTAL", FormatManagedGoldCostText(record?.TotalMaintenancePaidGold ?? 0));
	}

	// Display-only cost fragments for the policy history panel. The shared helpers in
	// CustomPolicyBehavior.Generation.cs keep their original text because other consumers persist it.
	private static string FormatManagedGoldCostText(int goldCost)
	{
		return goldCost > 0
			? PolicyUiTexts.Format("AFPOL_UI_CostGoldAmount", "{AMOUNT} denars", "AMOUNT", goldCost.ToString(CultureInfo.InvariantCulture))
			: PolicyUiTexts.Get("AFPOL_UI_CostGoldNone", "No denars");
	}

	private static string FormatManagedCostText(int goldCost, float influenceCost)
	{
		bool hasGold = goldCost > 0;
		bool hasInfluence = influenceCost > 0.0001f;
		if (!hasGold && !hasInfluence)
		{
			return PolicyUiTexts.Get("AFPOL_UI_CostGoldAndInfluenceNone", "No denars or influence");
		}
		if (hasGold && hasInfluence)
		{
			return PolicyUiTexts.Format("AFPOL_UI_CostGoldAndInfluenceAmount", "{GOLD} denars, {INFLUENCE} influence", "GOLD", goldCost.ToString(CultureInfo.InvariantCulture), "INFLUENCE", FormatNumber(influenceCost));
		}
		if (hasGold)
		{
			return PolicyUiTexts.Format("AFPOL_UI_CostGoldAmount", "{AMOUNT} denars", "AMOUNT", goldCost.ToString(CultureInfo.InvariantCulture));
		}
		return PolicyUiTexts.Format("AFPOL_UI_CostInfluenceAmount", "{AMOUNT} influence", "AMOUNT", FormatNumber(influenceCost));
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
			return PolicyUiTexts.Get("AFPOL_UI_PolicyHistoryEmpty", "No successfully enacted policy records yet.");
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(data.SubtitleText))
		{
			stringBuilder.AppendLine(data.SubtitleText);
			stringBuilder.AppendLine();
		}
		if (data.Records == null || data.Records.Count <= 0)
		{
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(data.EmptyStateText) ? PolicyUiTexts.Get("AFPOL_UI_PolicyHistoryEmpty", "No successfully enacted policy records yet.") : data.EmptyStateText);
			return stringBuilder.ToString().TrimEnd();
		}
		for (int i = 0; i < data.Records.Count; i++)
		{
			PolicyHistoryRecordData record = data.Records[i];
			stringBuilder.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture) + ". " + record.DateText + "  " + record.PolicyNameText + "  " + record.CostText);
			if (!string.IsNullOrWhiteSpace(record.ContentSummaryText))
			{
				stringBuilder.AppendLine(PolicyUiTexts.Get("AFPOL_UI_PolicyContentSection", "[Policy Content]"));
				stringBuilder.AppendLine(record.ContentSummaryText);
			}
			if (!string.IsNullOrWhiteSpace(record.FeedbackSummaryText))
			{
				stringBuilder.AppendLine(PolicyUiTexts.Get("AFPOL_UI_PolicyFeedbackSection", "[Public Feedback]"));
				stringBuilder.AppendLine(record.FeedbackSummaryText);
			}
			if (!string.IsNullOrWhiteSpace(record.ImpactSummaryText))
			{
				stringBuilder.AppendLine(PolicyUiTexts.Get("AFPOL_UI_PolicyImpactSectionTitle", "[Policy Effects]"));
				stringBuilder.AppendLine(record.ImpactSummaryText);
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEnd();
	}

	// Raw summaries preserve the historical fact/prompt payload; UI uses the display methods below.
	private static string BuildRawPolicyEffectSummary(PolicyApplicationResult application)
	{
		if (application?.KingdomEffects == null || application.KingdomEffects.Count <= 0)
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectSummary_078");
		}
		List<string> lines = new List<string>();
		foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
		{
			foreach (string visibleLine in BuildRawPolicyEffectLines(effect))
			{
				string line = visibleLine;
				if (!string.IsNullOrWhiteSpace(effect.Reason))
				{
					string visibleReason = CleanPlayerVisiblePolicySystemText(effect.Reason, string.Empty);
					if (!string.IsNullOrWhiteSpace(visibleReason))
					{
						line += PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectSummary_079") + visibleReason;
					}
				}
				lines.Add(line.TrimEnd('。') + "。");
			}
		}
		return string.Join("\n", lines);
	}

	private static string BuildRawPolicyRecordEffectSummary(PolicyRecordSaveData record, bool includeRemainingDays = true)
	{
		if (record?.Effects == null || record.Effects.Count <= 0)
		{
			return CleanPlayerVisiblePolicySystemText(record?.ImpactSummary, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyRecordEffectSummary_080"));
		}
		List<string> lines = new List<string>();
		foreach (PolicyRecordEffectSaveData effect in record.Effects.Where(x => x != null))
		{
			string status = effect.IsPermanentEffect && !effect.IsEnded
				? PolicyUiTexts.Get("AFPOL_UI_PermanentActive", "Permanently active")
				: effect.IsEnded || effect.RemainingDays <= 0
					? PolicyUiTexts.Get("AFPOL_UI_Ended", "Ended")
					: includeRemainingDays
						? PolicyUiTexts.Format("AFPOL_UI_RemainingDaysFormat", "Remaining {REMAINING}/{TOTAL} days", "REMAINING", effect.RemainingDays.ToString(CultureInfo.InvariantCulture), "TOTAL", effect.TotalDurationDays.ToString(CultureInfo.InvariantCulture))
						: PolicyUiTexts.Get("AFPOL_UI_InEffect", "In effect");
			if (!string.IsNullOrWhiteSpace(effect.EndReason)
				&& (effect.IsEnded || (!effect.IsPermanentEffect && effect.RemainingDays <= 0)))
			{
				status += "：" + BuildRawPolicyEndReason(effect.EndReason);
			}
			foreach (string visibleLine in BuildRawPolicyEffectLines(
				FirstNonEmpty(effect.TargetLabel, effect.KingdomName),
				effect.ModuleEffects,
				effect.TotalDurationDays))
			{
				lines.Add(visibleLine + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyRecordEffectSummary_081") + status + "。");
			}
		}
		return string.Join("\n", lines);
	}

	private static List<string> BuildRawPolicyEffectLines(AppliedKingdomEffect effect)
	{
		if (effect == null)
		{
			return new List<string> { PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLines_082") };
		}
		return BuildRawPolicyEffectLines(
			FirstNonEmpty(effect.TargetLabel, effect.KingdomName),
			effect.ModuleEffects,
			effect.DurationDays);
	}

	private static List<string> BuildRawPolicyEffectLines(
		string targetName,
		IEnumerable<PolicyEffectInstanceSaveData> moduleEffects,
		int durationDays)
	{
		return BuildRawPolicyEffectLinesForExternal(targetName, moduleEffects, durationDays);
	}

	internal static List<string> BuildRawPolicyEffectLinesForExternal(
		string fallbackTargetName,
		IEnumerable<PolicyEffectInstanceSaveData> moduleEffects,
		int durationDays)
	{
		List<PolicyEffectInstanceSaveData> instances = (moduleEffects ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		Dictionary<PolicyEffectInstanceSaveData, PolicyEffectCanonicalTargetSet> visibleTargetSetByInstance
			= new Dictionary<PolicyEffectInstanceSaveData, PolicyEffectCanonicalTargetSet>();
		foreach (PolicyEffectInstanceSaveData instance in instances)
		{
			if (!visibleTargetSetByInstance.ContainsKey(instance))
			{
				visibleTargetSetByInstance.Add(instance, BuildPlayerVisibleTargetSet(instance));
			}
		}
		instances = instances
			.Where(instance => HasPlayerVisibleEffectTarget(instance, visibleTargetSetByInstance[instance]))
			.ToList();
		string durationText = durationDays < 0
			? string.Empty
			: durationDays == 0
			? PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLinesForExternal_083")
			: PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLinesForExternal_084") + durationDays.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_069");
		string durationSuffix = durationText.Length <= 0 ? string.Empty : PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLinesForExternal_085") + durationText;
		if (instances.Count <= 0)
		{
			return new List<string>
			{
				BuildRawPolicyTargetLabel(null, null, fallbackTargetName) + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLinesForExternal_086") + durationSuffix
			};
		}

		List<string> lines = new List<string>();
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> targetGroup in instances.GroupBy(
			instance => BuildPlayerVisibleTargetGroupingKey(instance, visibleTargetSetByInstance[instance])))
		{
			PolicyEffectInstanceSaveData firstInstance = targetGroup.FirstOrDefault();
			PolicyEffectCanonicalTargetSet targetSet = firstInstance == null
				? null
				: visibleTargetSetByInstance[firstInstance];
			string targetLabel = BuildRawPolicyTargetLabel(firstInstance, targetSet, fallbackTargetName);
			List<string> values = BuildRawPolicyEffectValues(targetGroup);
			if (values.Count <= 0)
			{
				values.Add(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEffectLinesForExternal_087"));
			}
			lines.AddRange(values
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Select(value => targetLabel + "｜" + value.Trim() + durationSuffix));
		}
		return lines;
	}

	private static string BuildRawPolicyTargetLabel(
		PolicyEffectCanonicalTargetSet targetSet,
		string fallbackTargetName)
	{
		return BuildRawPolicyTargetLabel(null, targetSet, fallbackTargetName);
	}

	private static string BuildRawPolicyTargetLabel(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet targetSet,
		string fallbackTargetName)
	{
		if (TryGetPlayerVisibleWholeKingdomLordTargetIds(instance, targetSet, out List<string> wholeKingdomIds))
		{
			return BuildRawPolicyWholeKingdomLordTargetLabel(wholeKingdomIds, fallbackTargetName);
		}
		if (TryGetPlayerVisibleWholeKingdomTargetIds(targetSet, out List<string> semanticKingdomIds))
		{
			return BuildRawPolicyWholeKingdomTargetLabel(semanticKingdomIds, fallbackTargetName);
		}

		List<string> heroIds = DistinctTargetIds(targetSet?.HeroIds);
		if (heroIds.Count > 0)
		{
			List<string> names = heroIds
				.Select(id => TryResolvePlayerVisibleHero(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildRawPolicyNamedOrCountTargetLabel(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_088"), names, heroIds.Count, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_089"));
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
					: clanName + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_090") + leaderName + "）";
			}).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
			return BuildRawPolicyNamedOrCountTargetLabel(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_091"), names, clanIds.Count, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_092"));
		}

		List<string> villageIds = DistinctTargetIds(targetSet?.VillageIds);
		if (villageIds.Count > 0)
		{
			return BuildRawPolicyNamedOrCountTargetLabel(
				PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_051"),
				ResolveSettlementNames(villageIds),
				villageIds.Count,
				PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_093"));
		}

		List<string> townIds = DistinctTargetIds(targetSet?.TownIds);
		if (townIds.Count > 0)
		{
			return BuildRawPolicyNamedOrCountTargetLabel(
				PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"),
				ResolveSettlementNames(townIds),
				townIds.Count,
				PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_094"));
		}

		List<string> settlementIds = DistinctTargetIds(targetSet?.SettlementIds);
		if (settlementIds.Count > 0)
		{
			return BuildRawPolicyNamedOrCountTargetLabel(
				PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_095"),
				ResolveSettlementNames(settlementIds),
				settlementIds.Count,
				PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_096"));
		}

		List<string> kingdomIds = DistinctTargetIds(targetSet?.KingdomIds);
		if (kingdomIds.Count > 0)
		{
			List<string> names = kingdomIds
				.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildRawPolicyNamedOrCountTargetLabel(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_097"), names, kingdomIds.Count, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_098"));
		}

		List<string> parentIds = DistinctTargetIds(targetSet?.ParentSettlementIds);
		if (parentIds.Count > 0)
		{
			List<string> names = ResolveSettlementNames(parentIds)
				.Select(name => name + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_099"))
				.ToList();
			return BuildRawPolicyNamedOrCountTargetLabel(PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_100"), names, parentIds.Count, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_101"));
		}

		string fallback = string.IsNullOrWhiteSpace(fallbackTargetName) ? PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_102") : fallbackTargetName.Trim();
		return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_103") + fallback;
	}

	private static string BuildRawPolicyWholeKingdomTargetLabel(
		IEnumerable<string> kingdomIds,
		string fallbackTargetName)
	{
		List<string> normalizedIds = DistinctTargetIds(kingdomIds);
		List<string> names = normalizedIds
			.Take(3)
			.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (normalizedIds.Count == 1 && names.Count == 0 && !string.IsNullOrWhiteSpace(fallbackTargetName))
		{
			names.Add(fallbackTargetName.Trim());
		}
		if (normalizedIds.Count == 1 && names.Count == 1)
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + names[0];
		}
		if (names.Count <= 0)
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + normalizedIds.Count.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_105");
		}
		string summary = string.Join("、", names.Take(3));
		if (normalizedIds.Count > names.Count)
		{
			summary += PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_106") + normalizedIds.Count.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_105");
		}
		return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + summary;
	}

	private static string BuildRawPolicyWholeKingdomLordTargetLabel(
		IEnumerable<string> kingdomIds,
		string fallbackTargetName)
	{
		List<string> normalizedIds = DistinctTargetIds(kingdomIds);
		List<string> names = normalizedIds
			.Take(3)
			.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (normalizedIds.Count == 1 && names.Count == 0 && !string.IsNullOrWhiteSpace(fallbackTargetName))
		{
			names.Add(fallbackTargetName.Trim());
		}
		if (normalizedIds.Count == 1 && names.Count == 1)
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + names[0] + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_107");
		}
		if (names.Count <= 0)
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + normalizedIds.Count.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_108");
		}
		string summary = string.Join("、", names.Take(3));
		if (normalizedIds.Count > names.Count)
		{
			summary += PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_106") + normalizedIds.Count.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_105");
		}
		return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_104") + summary + PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomLordTargetLabel_109");
	}

	private static string BuildRawPolicyNamedOrCountTargetLabel(
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
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyTargetLabel_103") + totalCount.ToString(CultureInfo.InvariantCulture) + " " + countUnit;
		}
		string summary = string.Join("、", names.Take(3));
		if (totalCount > names.Take(3).Count())
		{
			summary += PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyWholeKingdomTargetLabel_106") + totalCount.ToString(CultureInfo.InvariantCulture) + " " + countUnit;
		}
		return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyNamedOrCountTargetLabel_110") + category + "：" + summary;
	}

	private static List<string> BuildRawPolicyEffectValues(IEnumerable<PolicyEffectInstanceSaveData> moduleEffects)
	{
		return PolicyEffectSaveCodec.DescribeRawPlayerVisibleInstances(moduleEffects);
	}

	private static string BuildRawPolicyEndReason(string reason)
	{
		if ((reason ?? string.Empty).StartsWith(PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal))
		{
			return PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEndReason_111");
		}
		return CleanPlayerVisiblePolicySystemText(reason, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyEndReason_112"));
	}

	private static string BuildPolicyEffectSummary(PolicyApplicationResult application)
	{
		if (application?.KingdomEffects == null || application.KingdomEffects.Count <= 0)
		{
			return PolicyUiTexts.Get("AFPOL_UI_NoApplicableModuleEffects", "No applicable module effects were produced.");
		}
		List<string> lines = new List<string>();
		foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
		{
			foreach (string visibleLine in BuildPlayerVisibleEffectLines(effect))
			{
				string line = visibleLine;
				if (!string.IsNullOrWhiteSpace(effect.Reason))
				{
					string visibleReason = CleanPolicyDisplaySystemText(effect.Reason, string.Empty);
					if (!string.IsNullOrWhiteSpace(visibleReason))
					{
						line += PolicyUiTexts.Format("AFPOL_UI_ReasonSuffix", " | Reason: {REASON}", "REASON", visibleReason);
					}
				}
				lines.Add(line.TrimEnd('。', '.') + PolicyUiTexts.Get("AFPOL_UI_SentencePeriod", "."));
			}
		}
		return string.Join("\n", lines);
	}

	private static string BuildPolicyRecordEffectSummary(PolicyRecordSaveData record, bool includeRemainingDays = true)
	{
		if (record?.Effects == null || record.Effects.Count <= 0)
		{
			return MapPlayerPolicyNarrativeFallbackToDisplay(
				CleanPolicyDisplaySystemText(record?.ImpactSummary, PolicyUiTexts.Get("AFPOL_UI_NoRecordedImpact", "No impact effects recorded.")));
		}
		List<string> lines = new List<string>();
		foreach (PolicyRecordEffectSaveData effect in record.Effects.Where(x => x != null))
		{
			string status = effect.IsPermanentEffect && !effect.IsEnded
				? PolicyUiTexts.Get("AFPOL_UI_PermanentActive", "Permanently active")
				: effect.IsEnded || effect.RemainingDays <= 0
					? PolicyUiTexts.Get("AFPOL_UI_Ended", "Ended")
					: includeRemainingDays
						? PolicyUiTexts.Format("AFPOL_UI_RemainingDaysFormat", "Remaining {REMAINING}/{TOTAL} days", "REMAINING", effect.RemainingDays.ToString(CultureInfo.InvariantCulture), "TOTAL", effect.TotalDurationDays.ToString(CultureInfo.InvariantCulture))
						: PolicyUiTexts.Get("AFPOL_UI_InEffect", "In effect");
			if (!string.IsNullOrWhiteSpace(effect.EndReason)
				&& (effect.IsEnded || (!effect.IsPermanentEffect && effect.RemainingDays <= 0)))
			{
				status += PolicyUiTexts.Get("AFPOL_UI_EndReasonSeparator", ": ") + BuildPlayerVisiblePolicyEndReason(effect.EndReason);
			}
			foreach (string visibleLine in BuildPlayerVisibleEffectLines(
				MapPolicyTargetLabelToDisplay(FirstNonEmpty(effect.TargetLabel, effect.KingdomName)),
				effect.ModuleEffects,
				effect.TotalDurationDays))
			{
				lines.Add(visibleLine + PolicyUiTexts.Format("AFPOL_UI_StatusSuffix", " | Status: {STATUS}", "STATUS", status) + PolicyUiTexts.Get("AFPOL_UI_SentencePeriod", "."));
			}
		}
		return string.Join("\n", lines);
	}

	private static List<string> BuildPlayerVisibleEffectLines(AppliedKingdomEffect effect)
	{
		if (effect == null)
		{
			return new List<string> { PolicyUiTexts.Get("AFPOL_UI_UnknownTargetNoEffects", "Unknown target | No module effects recorded") };
		}
		return BuildPlayerVisibleEffectLines(
			MapPolicyTargetLabelToDisplay(FirstNonEmpty(effect.TargetLabel, effect.KingdomName)),
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
		Dictionary<PolicyEffectInstanceSaveData, PolicyEffectCanonicalTargetSet> visibleTargetSetByInstance
			= new Dictionary<PolicyEffectInstanceSaveData, PolicyEffectCanonicalTargetSet>();
		foreach (PolicyEffectInstanceSaveData instance in instances)
		{
			if (!visibleTargetSetByInstance.ContainsKey(instance))
			{
				visibleTargetSetByInstance.Add(instance, BuildPlayerVisibleTargetSet(instance));
			}
		}
		instances = instances
			.Where(instance => HasPlayerVisibleEffectTarget(instance, visibleTargetSetByInstance[instance]))
			.ToList();
		string durationText = durationDays < 0
			? string.Empty
			: durationDays == 0
			? PolicyUiTexts.Get("AFPOL_UI_DurationPermanent", "Permanent")
			: PolicyUiTexts.Format("AFPOL_UI_DurationDays", "{DAYS} days", "DAYS", durationDays.ToString(CultureInfo.InvariantCulture));
		string durationSuffix = durationText.Length <= 0 ? string.Empty : PolicyUiTexts.Format("AFPOL_UI_DurationSuffix", " | Duration: {DURATION}", "DURATION", durationText);
		if (instances.Count <= 0)
		{
			return new List<string>
			{
				BuildPlayerVisibleTargetLabel(null, null, fallbackTargetName) + PolicyUiTexts.Get("AFPOL_UI_EffectTargetSeparator", " | ") + PolicyUiTexts.Get("AFPOL_UI_NoModuleEffects", "No module effects") + durationSuffix
			};
		}

		List<string> lines = new List<string>();
		foreach (IGrouping<string, PolicyEffectInstanceSaveData> targetGroup in instances.GroupBy(
			instance => BuildPlayerVisibleTargetGroupingKey(instance, visibleTargetSetByInstance[instance])))
		{
			PolicyEffectInstanceSaveData firstInstance = targetGroup.FirstOrDefault();
			PolicyEffectCanonicalTargetSet targetSet = firstInstance == null
				? null
				: visibleTargetSetByInstance[firstInstance];
			string targetLabel = BuildPlayerVisibleTargetLabel(firstInstance, targetSet, fallbackTargetName);
			List<string> values = BuildPlayerVisibleEffectValues(targetGroup);
			if (values.Count <= 0)
			{
				values.Add(PolicyUiTexts.Get("AFPOL_UI_NoModuleEffects", "No module effects"));
			}
			lines.AddRange(values
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Select(value => targetLabel + PolicyUiTexts.Get("AFPOL_UI_EffectTargetSeparator", " | ") + value.Trim() + durationSuffix));
		}
		return lines;
	}

	private static PolicyEffectCanonicalTargetSet BuildPlayerVisibleTargetSet(PolicyEffectInstanceSaveData instance)
	{
		if (instance?.TargetSet == null
			|| !PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
		{
			return instance?.TargetSet;
		}
		string actorClanId = (TryResolvePlayerVisibleHero(instance.ActorHeroId)?.Clan?.StringId ?? string.Empty).Trim();
		return PolicyEffectCompiler.ApplyActorClanTargetExclusion(module, actorClanId, instance.TargetSet);
	}

	private static bool HasPlayerVisibleEffectTarget(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		return !PolicyEffectModuleCatalog.TryGet(instance?.ModuleId, out IPolicyEffectModule module)
			|| module?.Descriptor?.ExcludeActorClanTargets != true
			|| (targetSet?.ClanIds?.Count ?? 0) > 0;
	}

	private static string BuildPlayerVisibleTargetGroupingKey(PolicyEffectCanonicalTargetSet targetSet)
	{
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

	private static string BuildPlayerVisibleTargetGroupingKey(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		if (TryGetPlayerVisibleWholeKingdomLordTargetIds(instance, targetSet, out List<string> wholeKingdomLordIds))
		{
			return "whole-kingdom-lords=" + JoinStableTargetIds(wholeKingdomLordIds);
		}
		if (TryGetPlayerVisibleWholeKingdomTargetIds(targetSet, out List<string> wholeKingdomIds))
		{
			return "whole-kingdom=" + JoinStableTargetIds(wholeKingdomIds);
		}
		return BuildPlayerVisibleTargetGroupingKey(targetSet) + "\u001fconcrete-targets";
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
		return BuildPlayerVisibleTargetLabel(null, targetSet, fallbackTargetName);
	}

	private static string BuildPlayerVisibleTargetLabel(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet targetSet,
		string fallbackTargetName)
	{
		if (TryGetPlayerVisibleWholeKingdomLordTargetIds(instance, targetSet, out List<string> wholeKingdomIds))
		{
			return BuildPlayerVisibleWholeKingdomLordTargetLabel(wholeKingdomIds, fallbackTargetName);
		}
		if (TryGetPlayerVisibleWholeKingdomTargetIds(targetSet, out List<string> semanticKingdomIds))
		{
			return BuildPlayerVisibleWholeKingdomTargetLabel(semanticKingdomIds, fallbackTargetName);
		}

		List<string> heroIds = DistinctTargetIds(targetSet?.HeroIds);
		if (heroIds.Count > 0)
		{
			List<string> names = heroIds
				.Select(id => TryResolvePlayerVisibleHero(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildNamedOrCountTargetLabel(PolicyUiTexts.Get("AFPOL_UI_TargetCategoryHeroes", "characters"), names, heroIds.Count, PolicyUiTexts.Get("AFPOL_UI_TargetUnitHeroes", "characters"));
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
					: PolicyUiTexts.Format("AFPOL_UI_ClanWithLeader", "{CLAN} (leader: {LEADER})", "CLAN", clanName, "LEADER", leaderName);
			}).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
			return BuildNamedOrCountTargetLabel(PolicyUiTexts.Get("AFPOL_UI_TargetCategoryClans", "clans"), names, clanIds.Count, PolicyUiTexts.Get("AFPOL_UI_TargetUnitClans", "clans"));
		}

		List<string> villageIds = DistinctTargetIds(targetSet?.VillageIds);
		if (villageIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				PolicyUiTexts.Get("AFPOL_UI_TargetCategoryVillages", "villages"),
				ResolveSettlementNames(villageIds),
				villageIds.Count,
				PolicyUiTexts.Get("AFPOL_UI_TargetUnitVillages", "villages"));
		}

		List<string> townIds = DistinctTargetIds(targetSet?.TownIds);
		if (townIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				PolicyUiTexts.Get("AFPOL_UI_TargetCategoryTowns", "towns"),
				ResolveSettlementNames(townIds),
				townIds.Count,
				PolicyUiTexts.Get("AFPOL_UI_TargetUnitTowns", "towns"));
		}

		List<string> settlementIds = DistinctTargetIds(targetSet?.SettlementIds);
		if (settlementIds.Count > 0)
		{
			return BuildNamedOrCountTargetLabel(
				PolicyUiTexts.Get("AFPOL_UI_TargetCategorySettlements", "settlements"),
				ResolveSettlementNames(settlementIds),
				settlementIds.Count,
				PolicyUiTexts.Get("AFPOL_UI_TargetUnitSettlements", "settlements"));
		}

		List<string> kingdomIds = DistinctTargetIds(targetSet?.KingdomIds);
		if (kingdomIds.Count > 0)
		{
			List<string> names = kingdomIds
				.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.ToList();
			return BuildNamedOrCountTargetLabel(PolicyUiTexts.Get("AFPOL_UI_TargetCategoryKingdoms", "kingdoms"), names, kingdomIds.Count, PolicyUiTexts.Get("AFPOL_UI_TargetUnitKingdoms", "kingdoms"));
		}

		List<string> parentIds = DistinctTargetIds(targetSet?.ParentSettlementIds);
		if (parentIds.Count > 0)
		{
			List<string> names = ResolveSettlementNames(parentIds)
				.Select(name => PolicyUiTexts.Format("AFPOL_UI_AttachedVillagesOf", "Attached villages of {NAME}", "NAME", name))
				.ToList();
			return BuildNamedOrCountTargetLabel(PolicyUiTexts.Get("AFPOL_UI_TargetCategoryAttachedVillages", "attached villages"), names, parentIds.Count, PolicyUiTexts.Get("AFPOL_UI_TargetUnitAttachedVillages", "attached village groups"));
		}

		string fallback = string.IsNullOrWhiteSpace(fallbackTargetName) ? PolicyUiTexts.Get("AFPOL_UI_UnknownTarget", "Unknown Target") : fallbackTargetName.Trim();
		return PolicyUiTexts.Format("AFPOL_UI_TargetLabel", "Target: {TARGET}", "TARGET", fallback);
	}

	private static bool TryGetPlayerVisibleWholeKingdomLordTargetIds(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectCanonicalTargetSet targetSet,
		out List<string> kingdomIds)
	{
		kingdomIds = new List<string>();
		if (targetSet == null
			|| (targetSet.TargetPlans?.Count ?? 0) > 0
			|| HasPolicyEffectSelectorKind(targetSet.SelectorHandles, 'C')
			|| HasPolicyEffectSelectorKind(targetSet.SelectorHandles, 'R'))
		{
			return false;
		}
		PolicyEffectModuleCatalog.TryGet(instance?.ModuleId, out IPolicyEffectModule module);
		if (module?.Descriptor?.ExcludeActorClanTargets == true)
		{
			return false;
		}

		List<string> selectorIds = DistinctTargetIds(targetSet.SelectorIds);
		if (selectorIds.Count > 0)
		{
			foreach (string selectorId in selectorIds)
			{
				if (!PolicyHeroTargetSelectorResolver.TryDescribeSelector(
					selectorId,
					out string selectorKind,
					out string selectorValue,
					out string selectorKingdomId)
					|| !string.Equals(selectorKind, "role", StringComparison.Ordinal)
					|| !string.Equals(selectorValue, "lords", StringComparison.Ordinal))
				{
					return false;
				}
				if (!string.IsNullOrWhiteSpace(selectorKingdomId))
				{
					kingdomIds.Add(selectorKingdomId.Trim());
				}
			}
			kingdomIds = DistinctTargetIds(kingdomIds);
			return kingdomIds.Count > 0;
		}

		List<string> selectorHandles = DistinctTargetIds(targetSet.SelectorHandles);
		if (selectorHandles.Count <= 0
			|| selectorHandles.Any(handle => !HasPolicyEffectSelectorKind(new[] { handle }, 'K'))
			|| module?.Descriptor?.TargetKinds?.Contains(PolicyEffectTargetKind.Clan) != true)
		{
			return false;
		}

		kingdomIds = DistinctTargetIds(targetSet.KingdomIds);
		return kingdomIds.Count > 0;
	}

	private static bool TryGetPlayerVisibleWholeKingdomTargetIds(
		PolicyEffectCanonicalTargetSet targetSet,
		out List<string> kingdomIds)
	{
		kingdomIds = new List<string>();
		if (targetSet == null
			|| (targetSet.TargetPlans?.Count ?? 0) > 0
			|| (targetSet.SelectorIds?.Count ?? 0) > 0
			|| targetSet.FollowCurrentRulingClan)
		{
			return false;
		}
		List<string> selectorHandles = DistinctTargetIds(targetSet.SelectorHandles);
		if (selectorHandles.Count <= 0
			|| selectorHandles.Any(handle => !HasPolicyEffectSelectorKind(new[] { handle }, 'K')))
		{
			return false;
		}
		kingdomIds = DistinctTargetIds(targetSet.KingdomIds);
		return kingdomIds.Count > 0;
	}

	private static string BuildPlayerVisibleWholeKingdomTargetLabel(
		IEnumerable<string> kingdomIds,
		string fallbackTargetName)
	{
		List<string> normalizedIds = DistinctTargetIds(kingdomIds);
		List<string> names = normalizedIds
			.Take(3)
			.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (normalizedIds.Count == 1 && names.Count == 0 && !string.IsNullOrWhiteSpace(fallbackTargetName))
		{
			names.Add(fallbackTargetName.Trim());
		}
		if (normalizedIds.Count == 1 && names.Count == 1)
		{
			return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomLabel", "Target kingdom: {TARGET}", "TARGET", names[0]);
		}
		if (names.Count <= 0)
		{
			return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomCountKingdoms", "Target kingdom: {COUNT} kingdoms", "COUNT", normalizedIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		string summary = string.Join(PolicyUiTexts.Get("AFPOL_UI_ListItemSeparator", ", "), names.Take(3));
		if (normalizedIds.Count > names.Count)
		{
			summary += PolicyUiTexts.Format("AFPOL_UI_TargetKingdomOverflowKingdoms", " ({COUNT} kingdoms total)", "COUNT", normalizedIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomLabel", "Target kingdom: {TARGET}", "TARGET", summary);
	}

	private static string BuildPlayerVisibleWholeKingdomLordTargetLabel(
		IEnumerable<string> kingdomIds,
		string fallbackTargetName)
	{
		List<string> normalizedIds = DistinctTargetIds(kingdomIds);
		List<string> names = normalizedIds
			.Take(3)
			.Select(id => TryResolvePlayerVisibleKingdom(id)?.Name?.ToString() ?? string.Empty)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Distinct(StringComparer.Ordinal)
			.ToList();
		if (normalizedIds.Count == 1 && names.Count == 0 && !string.IsNullOrWhiteSpace(fallbackTargetName))
		{
			names.Add(fallbackTargetName.Trim());
		}
		if (normalizedIds.Count == 1 && names.Count == 1)
		{
			return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomAllLords", "Target kingdom: all lords of {TARGET}", "TARGET", names[0]);
		}
		if (names.Count <= 0)
		{
			return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomCountKingdomsAllLords", "Target kingdom: all lords of {COUNT} kingdoms", "COUNT", normalizedIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		string summary = string.Join(PolicyUiTexts.Get("AFPOL_UI_ListItemSeparator", ", "), names.Take(3));
		if (normalizedIds.Count > names.Count)
		{
			summary += PolicyUiTexts.Format("AFPOL_UI_TargetKingdomOverflowKingdoms", " ({COUNT} kingdoms total)", "COUNT", normalizedIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		return PolicyUiTexts.Format("AFPOL_UI_TargetKingdomSummaryAllLords", "Target kingdom: {SUMMARY} (all lords)", "SUMMARY", summary);
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
			return PolicyUiTexts.Format("AFPOL_UI_TargetLabelCount", "Target: {COUNT} {UNIT}", "COUNT", totalCount.ToString(CultureInfo.InvariantCulture), "UNIT", countUnit);
		}
		string summary = string.Join(PolicyUiTexts.Get("AFPOL_UI_ListItemSeparator", ", "), names.Take(3));
		if (totalCount > names.Take(3).Count())
		{
			summary += PolicyUiTexts.Format("AFPOL_UI_TargetOverflowWithUnit", " ({COUNT} {UNIT} total)", "COUNT", totalCount.ToString(CultureInfo.InvariantCulture), "UNIT", countUnit);
		}
		return PolicyUiTexts.Format("AFPOL_UI_TargetCategoryLabel", "Target {CATEGORY}: {SUMMARY}", "CATEGORY", category, "SUMMARY", summary);
	}

	private static List<string> BuildPlayerVisibleEffectValues(IEnumerable<PolicyEffectInstanceSaveData> moduleEffects)
	{
		return PolicyEffectSaveCodec.DescribePlayerVisibleInstances(moduleEffects);
	}

	private static string CleanPlayerVisiblePolicySystemText(string text, string fallback)
	{
		return CleanPolicyDisplaySystemText(text, fallback);
	}

	private static string CleanPolicyDisplaySystemText(string text, string fallback)
	{
		string cleaned = CleanPolicyDisplayText(text);
		// Language is not evidence of an internal value. Suppress only explicit
		// action tags and snake_case protocol identifiers, preserving English prose.
		return string.IsNullOrWhiteSpace(cleaned) || Regex.IsMatch(cleaned, @"^(?:\[ACTION:[^\]\r\n]+\]|[a-z][a-z0-9]*(?:_[a-z0-9]+)+)$", RegexOptions.CultureInvariant)
			? (fallback ?? string.Empty)
			: cleaned;
	}

	private static string BuildPlayerVisiblePolicyEndReason(string reason)
	{
		if ((reason ?? string.Empty).StartsWith(PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal))
		{
			return PolicyUiTexts.Get("AFPOL_UI_EndReasonRollbackPending", "Awaiting effect rollback");
		}
		return MapPolicyEndReasonToDisplay(reason, CleanPolicyDisplaySystemText(reason, PolicyUiTexts.Get("AFPOL_UI_EndReasonInternalRecorded", "Internal state recorded")));
	}

	/// <summary>
	/// Persisted marker written by <see cref="BuildLocalPolicyTargetSnapshot"/> when a settlement name
	/// cannot be resolved. It is part of the save payload and must stay byte-identical; the display
	/// boundary maps it on read instead of rewriting the stored value.
	/// </summary>
	private static string LocalPolicyTargetSnapshotUnknownName => PolicyCompatibilityTexts.Get("Legacy_BuildPlayerVisiblePolicyEndReason_113");

	private static string BuildPlayerVisibleOriginalTargetName(LocalPolicyTargetSnapshotSaveData snapshot)
	{
		string name = snapshot?.Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}
		return string.Equals(name, LocalPolicyTargetSnapshotUnknownName, StringComparison.Ordinal)
			? PolicyUiTexts.Get("AFPOL_UI_UnknownFief", "Unknown Fief")
			: name;
	}

	/// <summary>
	/// Display projection for the persisted <c>EndReason</c> payload. The producers keep writing the
	/// original literals so save data, reconciliation and history stay byte-identical; only the text a
	/// player can read is resolved here. Values outside the known set fall back to the caller-supplied
	/// sanitized text so genuinely dynamic reasons are never swallowed.
	/// </summary>
	private static string MapPolicyEndReasonToDisplay(string reason, string fallback)
	{
		switch ((reason ?? string.Empty).Trim())
		{
			case var rawPolicyCase49 when string.Equals(rawPolicyCase49, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_114"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonPlayerStopped", "Stopped by the player");
			case var rawPolicyCase50 when string.Equals(rawPolicyCase50, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_115"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonPlayerAbolished", "Abolished by the player");
			case var rawPolicyCase51 when string.Equals(rawPolicyCase51, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_116"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonReplacedByReReview", "Replaced by the newly re-reviewed policy");
			case var rawPolicyCase52 when string.Equals(rawPolicyCase52, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_117"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonRenewalTargetNotDirectVassal", "At renewal the target was no longer a direct vassal");
			case var rawPolicyCase53 when string.Equals(rawPolicyCase53, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_118"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonRenewalTargetNotDirectVassalConfirmed", "Confirmed at renewal that the target was no longer a direct vassal");
			case var rawPolicyCase54 when string.Equals(rawPolicyCase54, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_119"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonRenewalIndependenceBreakaway", "Renewal independence reached the breakaway threshold");
			case var rawPolicyCase55 when string.Equals(rawPolicyCase55, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_120"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonDurationHasEnded", "Duration has ended");
			case var rawPolicyCase56 when string.Equals(rawPolicyCase56, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_121"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonEffectTermEnded", "Effect term ended");
			case var rawPolicyCase57 when string.Equals(rawPolicyCase57, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_122"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonAllTargetFiefsLost", "All target fiefs have been lost");
			case var rawPolicyCase58 when string.Equals(rawPolicyCase58, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_123"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonTargetKingdomGone", "Target kingdom no longer exists");
			case var rawPolicyCase59 when string.Equals(rawPolicyCase59, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_124"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonDurationEnded", "Duration ended");
			case var rawPolicyCase60 when string.Equals(rawPolicyCase60, PolicyCompatibilityTexts.Get("SaveCodec_EndReason_Upgraded"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_EndReasonLegacyUpgradeStopped", "Stopped after the legacy policy system upgrade; can be re-reviewed");
			case var rawPolicyCase61 when string.Equals(rawPolicyCase61, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_125"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_Ended", "Ended");
			case var rawPolicyCase62 when string.Equals(rawPolicyCase62, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_126"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_StatusVassalageEnded", "Vassalage ended");
			default: return fallback ?? string.Empty;
		}
	}

	/// <summary>
	/// Display projection for policy target labels. The label producers keep writing the original
	/// literals because those strings are persisted into save data and round-trip through
	/// <c>PolicyTargetHandleSaveData.DisplayName</c>. This method maps the closed set of known label
	/// shells to localized display text at the moment the label reaches the player-visible effect
	/// lines. Composed labels are split into shell plus dynamic target names so no information is
	/// dropped; a value that matches no known shell (for example a label restored from an older save
	/// or authored by the model) is returned unchanged, because guessing a translation would be worse
	/// than showing the original.
	/// </summary>
	private static string MapPolicyTargetLabelToDisplay(string label)
	{
		string text = (label ?? string.Empty).Trim();
		if (text.Length <= 0)
		{
			return string.Empty;
		}

		string head = text;
		string targets = null;
		int openIndex = text.IndexOf('（');
		if (openIndex > 0 && text.EndsWith("）", StringComparison.Ordinal))
		{
			head = text.Substring(0, openIndex);
			targets = text.Substring(openIndex + 1, text.Length - openIndex - 2);
		}

		string mappedHead = MapPolicyTargetLabelHeadToDisplay(head);
		if (mappedHead == null)
		{
			return text;
		}
		return targets == null
			? mappedHead
			: mappedHead + PolicyUiTexts.Format("AFPOL_UI_TargetLabelParenthetical", " ({TARGETS})", "TARGETS", MapPolicyTargetLabelTargetsToDisplay(targets));
	}

	/// <summary>
	/// Maps one label head, i.e. the part before an optional target list. Returns null when the head is
	/// not a known shell so the caller can fall back to the untouched original.
	/// </summary>
	private static string MapPolicyTargetLabelHeadToDisplay(string head)
	{
		string text = (head ?? string.Empty).Trim();
		if (text.Length <= 0)
		{
			return null;
		}

		string exact = MapPolicyTargetLabelShellToDisplay(text);
		if (exact != null)
		{
			return exact;
		}

		string designatedSettlements = PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelHeadToDisplay_127");
		if (text.StartsWith(designatedSettlements, StringComparison.Ordinal))
		{
			return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellDesignatedSettlements", "Designated settlements")
				+ PolicyUiTexts.Get("AFPOL_UI_TargetLabelColonSeparator", ": ")
				+ MapPolicyTargetLabelTargetsToDisplay(text.Substring(designatedSettlements.Length));
		}

		string holdingsSuffix = PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelHeadToDisplay_128");
		if (text.EndsWith(holdingsSuffix, StringComparison.Ordinal))
		{
			string owner = text.Substring(0, text.Length - holdingsSuffix.Length).Trim();
			return owner.Length <= 0
				? PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellCurrentHoldings", "Current holdings and attached villages")
				: PolicyUiTexts.Format("AFPOL_UI_TargetLabelOwnerHoldings", "{OWNER} current holdings and attached villages", "OWNER", MapPolicyTargetLabelTargetsToDisplay(owner));
		}

		return null;
	}

	/// <summary>
	/// Maps a whole-string label shell, or null when it is not one of the known shells.
	/// </summary>
	private static string MapPolicyTargetLabelShellToDisplay(string text)
	{
		switch (text)
		{
			case var rawPolicyCase63 when string.Equals(rawPolicyCase63, PolicyCompatibilityTexts.Get("Legacy_BuildRawPolicyNamedOrCountTargetLabel_110"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellTarget", "Target");
			case var rawPolicyCase64 when string.Equals(rawPolicyCase64, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_129"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellPublishFief", "Publishing fief");
			case var rawPolicyCase65 when string.Equals(rawPolicyCase65, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_130"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellPublishFiefScope", "Publishing fief and attached villages");
			case var rawPolicyCase66 when string.Equals(rawPolicyCase66, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_131"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellMentionedTarget", "Mentioned target in the home kingdom");
			case var rawPolicyCase67 when string.Equals(rawPolicyCase67, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_132"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellHomeKingdom", "Home kingdom target");
			case var rawPolicyCase68 when string.Equals(rawPolicyCase68, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_133"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellNoMatching", "No matching target at present");
			case var rawPolicyCase69 when string.Equals(rawPolicyCase69, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_134"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellDesignatedClan", "Designated clan");
			case var rawPolicyCase70 when string.Equals(rawPolicyCase70, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_135"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellRulingClanHoldings", "Current ruling clan's current holdings and attached villages");
			case var rawPolicyCase71 when string.Equals(rawPolicyCase71, PolicyCompatibilityTexts.Get("Legacy_MapPolicyTargetLabelShellToDisplay_136"), StringComparison.Ordinal): return PolicyUiTexts.Get("AFPOL_UI_TargetLabelShellSelectedLocation", "Selected location");
			default: return null;
		}
	}

	/// <summary>
	/// Stored target names are opaque data. Typed UI producers own separators and counts.
	/// </summary>
	private static string MapPolicyTargetLabelTargetsToDisplay(string targets)
	{
		return (targets ?? string.Empty).Trim();
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
		if (string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			&& CanDeletePolicyHistoryState(record.EffectStatus, false, false)
			&& record.RemainingDays == 0
			&& record.Effects.Any(effect => effect.ModuleEffects.Count > 0)
			&& record.Effects.All(effect => effect.IsEnded && effect.RemainingDays == 0))
		{
			record.Status = record.EffectStatus;
		}
		if (CanDeletePolicyHistoryState(record.Status, false, false)) record.EffectStatus = record.Status;
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
			if (IsPolicyHistoryDeleted(LocalPolicyHistorySource(record), record.RecordId)) continue;
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
					PolicyUiTexts.Get("AFPOL_UI_LocalPublishFiefScope", "Publishing fief"),
					Array.Empty<string>(),
					Array.Empty<string>(),
					false,
					sourceSettlements, uiText: true);
			}
			else
			{
				List<string> originalNames = (record.OriginalTargets ?? new List<LocalPolicyTargetSnapshotSaveData>())
					.Where(x => x != null)
					.Select(BuildPlayerVisibleOriginalTargetName)
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				targetText = PolicyUiTexts.Get("AFPOL_UI_LocalPublishFiefPrefix", "Publishing fief: ") + (originalNames.Count <= 0
					? PolicyUiTexts.Get("AFPOL_UI_NoRemainingTargets", "No remaining targets")
					: string.Join(PolicyUiTexts.Get("AFPOL_UI_ListItemSeparator", ", "), originalNames.Take(6))
						+ (originalNames.Count > 6 ? PolicyUiTexts.Format("AFPOL_UI_TargetOverflowSuffix", ", {COUNT} more", "COUNT", (originalNames.Count - 6).ToString(CultureInfo.InvariantCulture)) : ""));
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
				targetText += PolicyUiTexts.Get("AFPOL_UI_LocalSectionSeparator", "; ") + BuildLocalPolicyEffectTargetLabel(
					mentionedEffect.TargetScope,
					mentionedEffect.TargetHandle,
					mentionedEffect.TargetLabel,
					mentionedEffect.TargetClanIds,
					mentionedEffect.DirectTargetSettlementIds,
					mentionedEffect.FollowCurrentRulingClan,
					currentMentioned, uiText: true);
			}
			string statusText = GetLocalPolicyStatusText(record.Status);
			string effectStatusText = !record.Effects.Any(effect => effect.ModuleEffects.Count > 0) ? PolicyUiTexts.Get("AFPOL_UI_NoContinuousNumericEffects", "No continuous numeric effects") : GetLocalPolicyStatusText(record.EffectStatus);
			string renewalHistory = record.Renewals.Count <= 0
				? PolicyUiTexts.Get("AFPOL_UI_LocalRenewalHistoryNone", "Extension history: None")
				: PolicyUiTexts.Get("AFPOL_UI_LocalRenewalHistoryTitle", "Extension history:\n") + string.Join("\n", record.Renewals.Select(x =>
					PolicyUiTexts.Format("AFPOL_UI_LocalRenewalHistoryEntry", "- {DATE}: added {DAYS} days", "DATE", PolicyDateTextProjection.ForUi(x.DateText ?? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date")), "DAYS", x.AddedDays.ToString(CultureInfo.InvariantCulture))));
			data.Records.Add(new LocalPolicyHistoryRecordData
			{
				ScopeKind = PolicyScopeLocal,
				RecordId = record.RecordId,
				DateText = string.IsNullOrWhiteSpace(record.DateText) ? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date") : PolicyDateTextProjection.ForUi(record.DateText),
				PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? PolicyUiTexts.Get("AFPOL_UI_UnnamedLocalPolicy", "Unnamed Local Policy") : record.PolicyName,
				StatusText = statusText,
				TargetText = targetText,
				RemainingText = BuildLocalPolicyRemainingText(record),
				ContentText = record.PolicyContent ?? "",
				FeedbackText = MapPlayerPolicyNarrativeFallbackToDisplay(string.IsNullOrWhiteSpace(record.PublicFeedback)
					? PolicyUiTexts.Get("AFPOL_UI_NoFeedbackRecorded", "No policy feedback recorded.")
					: record.PublicFeedback),
				EffectText = BuildLocalPolicyEffectText(record),
				CostText = PolicyUiTexts.Format("AFPOL_UI_LocalRecordCostLine",
					"Startup required {REQUIRED}; startup paid {PAID}; daily upkeep {DAILY}; cumulative upkeep {TOTAL}; effect scale {SCALE}",
					"REQUIRED", FormatManagedGoldCostText(record.RequiredGoldCost),
					"PAID", FormatManagedGoldCostText(record.InitialActualGoldCost),
					"DAILY", FormatManagedGoldCostText(record.DailyMaintenanceGoldCost),
					"TOTAL", FormatManagedGoldCostText(record.TotalMaintenancePaidGold),
					"SCALE", FormatPercent(record.GoldEffectScale)),
				CycleText = PolicyUiTexts.Get("AFPOL_UI_LocalPolicyStatusPrefix", "Policy status: ") + statusText + PolicyUiTexts.Get("AFPOL_UI_LocalSectionSeparator", "; ") + PolicyUiTexts.Get("AFPOL_UI_LocalEffectStatusPrefix", "Effect status: ") + effectStatusText
					+ (string.IsNullOrWhiteSpace(record.EndReason) ? "" : PolicyUiTexts.Format("AFPOL_UI_EndReasonParenthetical", " ({REASON})", "REASON", BuildPlayerVisiblePolicyEndReason(record.EndReason)))
					+ PolicyUiTexts.Get("AFPOL_UI_LocalSectionSeparator", "; ") + PolicyUiTexts.Get("AFPOL_UI_LocalRenewalCountPrefix", "Renewal count: ") + record.RenewalCount.ToString(CultureInfo.InvariantCulture)
					+ BuildPolicyRuntimeStatusSuffix(record.RecordId, activeEffectsByRecordId),
				RenewalText = renewalHistory,
				CanRenew = true,
				HistoryKey = BuildPolicyHistoryKey(LocalPolicyHistorySource(record), record.RecordId),
				CanDelete = CanDeletePolicyHistoryRecord(BuildPolicyHistoryKey(LocalPolicyHistorySource(record), record.RecordId), out _),
				CanAbolish = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				,
				CanReReview = !string.IsNullOrWhiteSpace(record.PolicyContent)
					&& TryBuildPolicyReReviewContext(record.RecordId, out _, out _)
			});
		}
		return data;
	}

	private LocalPolicyHistoryRecordData BuildVassalPolicyHistoryRecordData(
		LocalPolicyRecordSaveData record,
		IReadOnlyDictionary<string, List<ActivePolicyEffectSaveData>> activeEffectsByRecordId)
	{
		bool relationValid = VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int currentIndependence, out int breakawayThreshold, out int rulerRelation, out string rulerName);
		string targetName = FirstNonEmpty(record.TargetKingdomName, ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName)?.Name?.ToString(), PolicyUiTexts.Get("AFPOL_UI_UnknownVassalKingdom", "Unknown Vassal Kingdom"));
		string issuerName = FirstNonEmpty(record.IssuerKingdomName, PolicyUiTexts.Get("AFPOL_UI_UnknownSuzerain", "Unknown Suzerain"));
		string targetText = PolicyUiTexts.Format("AFPOL_UI_VassalTargetSummary", "Suzerain: {ISSUER}; Target vassal: {TARGET}", "ISSUER", issuerName, "TARGET", targetName);
		if (relationValid)
		{
			targetText += PolicyUiTexts.Format("AFPOL_UI_VassalIndependenceStatus", "; Current independence {CURRENT}/100; Breakaway threshold {THRESHOLD} ({RULER} relation {RELATION})", "CURRENT", currentIndependence.ToString(CultureInfo.InvariantCulture), "THRESHOLD", breakawayThreshold.ToString(CultureInfo.InvariantCulture), "RULER", rulerName, "RELATION", FormatSigned(rulerRelation));
		}
		else
		{
			targetText += PolicyUiTexts.Get("AFPOL_UI_VassalStatusEnded", "; Vassalage relationship terminated");
		}
		string statusText = GetLocalPolicyStatusText(record.Status);
		int initialNetChange = record.InitialIndependenceCost + record.VassalQualityIndependenceDelta;
		string costText = PolicyUiTexts.Format("AFPOL_UI_VassalIndependenceSettlement", "Initial independence settlement: {BEFORE} + random cost {COST} + policy modifier {DELTA} (net change {NET}); Cumulative random cost {TOTAL}", "BEFORE", record.IndependenceBefore.ToString(CultureInfo.InvariantCulture), "COST", record.InitialIndependenceCost.ToString(CultureInfo.InvariantCulture), "DELTA", FormatSigned(record.VassalQualityIndependenceDelta), "NET", FormatSigned(initialNetChange), "TOTAL", record.TotalIndependenceCost.ToString(CultureInfo.InvariantCulture));
		if (!string.IsNullOrWhiteSpace(record.IndependenceReason))
		{
			costText += PolicyUiTexts.Format("AFPOL_UI_VassalIndependenceReason", "; Modifier reason: {REASON}", "REASON", MapPlayerPolicyNarrativeFallbackToDisplay(record.IndependenceReason));
		}
		string renewalHistory = record.Renewals.Count <= 0
			? PolicyUiTexts.Get("AFPOL_UI_VassalRenewalHistoryNone", "Renewal history: None")
			: PolicyUiTexts.Get("AFPOL_UI_VassalRenewalHistoryTitle", "Renewal history:\n") + string.Join("\n", record.Renewals.Select(x =>
				PolicyUiTexts.Format("AFPOL_UI_VassalRenewalHistoryEntry", "- {DATE}: Independence {BEFORE} + {COST} = {AFTER}/100, added {DAYS} days", "DATE", PolicyDateTextProjection.ForUi(x.DateText ?? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date")), "BEFORE", x.IndependenceBefore.ToString(CultureInfo.InvariantCulture), "COST", x.IndependenceCost.ToString(CultureInfo.InvariantCulture), "AFTER", x.IndependenceAfter.ToString(CultureInfo.InvariantCulture), "DAYS", x.AddedDays.ToString(CultureInfo.InvariantCulture))));
		return new LocalPolicyHistoryRecordData
		{
			ScopeKind = PolicyScopeVassal,
			RecordId = record.RecordId,
			DateText = string.IsNullOrWhiteSpace(record.DateText) ? PolicyUiTexts.Get("AFPOL_UI_UnknownDate", "Unknown Date") : PolicyDateTextProjection.ForUi(record.DateText),
			PolicyNameText = string.IsNullOrWhiteSpace(record.PolicyName) ? PolicyUiTexts.Get("AFPOL_UI_UnnamedVassalPolicy", "Unnamed Vassal Policy") : record.PolicyName,
			StatusText = statusText,
			TargetText = targetText,
			RemainingText = BuildLocalPolicyRemainingText(record),
			ContentText = record.PolicyContent ?? "",
			FeedbackText = MapPlayerPolicyNarrativeFallbackToDisplay(string.IsNullOrWhiteSpace(record.PublicFeedback)
				? PolicyUiTexts.Get("AFPOL_UI_NoFeedbackRecorded", "No policy feedback recorded.")
				: record.PublicFeedback),
			EffectText = BuildLocalPolicyEffectText(record),
			CostText = costText,
			CycleText = PolicyUiTexts.Format("AFPOL_UI_StatusPrefix", "Status: {STATUS}", "STATUS", statusText) + (string.IsNullOrWhiteSpace(record.EndReason) ? "" : PolicyUiTexts.Format("AFPOL_UI_EndReasonParenthetical", " ({REASON})", "REASON", BuildPlayerVisiblePolicyEndReason(record.EndReason)))
				+ PolicyUiTexts.Get("AFPOL_UI_LocalSectionSeparator", "; ") + PolicyUiTexts.Format("AFPOL_UI_VassalStatusAndRenewals", "Status: {STATUS}; Renewals {COUNT}", "STATUS", statusText, "COUNT", record.RenewalCount.ToString(CultureInfo.InvariantCulture))
				+ BuildPolicyRuntimeStatusSuffix(record.RecordId, activeEffectsByRecordId),
			RenewalText = renewalHistory,
			CanRenew = true,
			HistoryKey = BuildPolicyHistoryKey("player_vassal", record.RecordId),
			CanDelete = CanDeletePolicyHistoryRecord(BuildPolicyHistoryKey("player_vassal", record.RecordId), out _),
			CanAbolish = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			,
			CanReReview = !string.IsNullOrWhiteSpace(record.PolicyContent)
				&& TryBuildPolicyReReviewContext(record.RecordId, out _, out _)
		};
	}

	private static string BuildLocalPolicyEffectText(LocalPolicyRecordSaveData record)
	{
		record = NormalizeLocalPolicyRecord(record);
		if (record?.Effects == null || record.Effects.Count == 0)
		{
			return PolicyUiTexts.Get("AFPOL_UI_NoModuleEffects", "No module effects");
		}
		bool isVassalPolicy = string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		List<string> lines = new List<string>();
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
		{
			string label = isVassalPolicy
				? FirstNonEmpty(effect.TargetKingdomName, effect.TargetLabel, PolicyUiTexts.Get("AFPOL_UI_UnknownCountry", "Unknown Country"))
				: (!string.IsNullOrWhiteSpace(effect.TargetLabel)
				? effect.TargetLabel
				: (string.Equals(effect.TargetScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase)
					? PolicyUiTexts.Get("AFPOL_UI_LocalMentionedTargetEffect", "Domestic mentioned target effect")
					: PolicyUiTexts.Get("AFPOL_UI_LocalPublishFiefEffect", "Publishing fief effect")));
			foreach (string value in BuildPlayerVisibleEffectLinesForExternal(label, effect.ModuleEffects, -1)
				.Where(value => !string.IsNullOrWhiteSpace(value)))
			{
				string visibleReason = CleanPolicyDisplaySystemText(effect.Reason, string.Empty);
				lines.Add(value.Trim()
					+ (string.IsNullOrWhiteSpace(visibleReason) ? "" : PolicyUiTexts.Format("AFPOL_UI_ReasonSuffix", " | Reason: {REASON}", "REASON", visibleReason)));
			}
		}
		return lines.Count <= 0 ? PolicyUiTexts.Get("AFPOL_UI_NoModuleEffects", "No module effects") : string.Join("\n", lines);
	}

	private static string BuildLocalPolicyRemainingText(LocalPolicyRecordSaveData record)
	{
		if (CanDeletePolicyHistoryState(record.Status, false, false)) return PolicyUiTexts.Get("AFPOL_UI_ContinuousEffectsEnded", "Continuous effects ended");
		if (!record.Effects.Any(effect => effect.ModuleEffects.Count > 0)) return PolicyUiTexts.Get("AFPOL_UI_NoContinuousNumericEffects", "No continuous numeric effects");
		return record.IsPermanentEffect ? PolicyUiTexts.Get("AFPOL_UI_NumericEffectsPermanent", "Numeric effects: Permanent")
			: PolicyUiTexts.Format("AFPOL_UI_EffectRemainingAndOriginalDays", "Effects remaining {REMAINING} days; original period {ORIGINAL} days", "REMAINING", record.RemainingDays.ToString(CultureInfo.InvariantCulture), "ORIGINAL", record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture));
	}

	private static string GetLocalPolicyStatusText(string status)
	{
		if (string.Equals(status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)) return PolicyUiTexts.Get("AFPOL_UI_StatusValid", "Valid");
		if (string.Equals(status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase)) return PolicyUiTexts.Get("AFPOL_UI_StatusExpired", "Expired");
		if (string.Equals(status, LocalPolicyStatusTargetsLost, StringComparison.OrdinalIgnoreCase)) return PolicyUiTexts.Get("AFPOL_UI_StatusTargetsLost", "All fiefs lost");
		if (string.Equals(status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)) return PolicyUiTexts.Get("AFPOL_UI_StatusAbolished", "Abolished by player");
		if (string.Equals(status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase)) return PolicyUiTexts.Get("AFPOL_UI_StatusVassalageEnded", "Vassalage ended");
		return PolicyUiTexts.Get("AFPOL_UI_StatusTerminated", "Ended");
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
		return fief?.IsCastle == true ? PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049") : PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050");
	}

	private static string BuildPolicyReReviewDurationText(PolicyReReviewContext context)
	{
		if (context == null || !context.DurationKnown || context.IsPermanentEffect)
		{
			return string.Empty;
		}
		return Math.Max(1, context.DurationDays).ToString(CultureInfo.InvariantCulture);
	}

	private bool TryGetKingdomPolicyReReviewAvailability(
		string recordId,
		out bool canReReview,
		out string disabledReason)
	{
		canReReview = false;
		disabledReason = string.Empty;
		string id = (recordId ?? string.Empty).Trim();
		if (id.Length == 0
			|| LoadLocalPolicyRecords().Any(record => string.Equals(record?.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}

		DynamicPolicySaveData dynamic = LoadDynamicPolicies()
			.FirstOrDefault(record => string.Equals(record?.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		PolicyRecordSaveData history = LoadPolicyRecordHistory()
			.FirstOrDefault(record => string.Equals(record?.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(id, out NpcRulerPolicyRecord unified);
		bool unifiedKingdomPolicy = unified?.IsPlayerPolicy == true
			&& !string.Equals(unified.PolicyKind ?? string.Empty, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		if (dynamic == null && history == null && !unifiedKingdomPolicy)
		{
			return false;
		}

		if (TryBuildPolicyReReviewContext(id, out PolicyReReviewContext context, out string error)
			&& string.Equals(context?.ScopeKind ?? string.Empty, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase))
		{
			canReReview = true;
			return true;
		}

		disabledReason = FirstNonEmpty(error, PolicyUiTexts.Get("AFPOL_UI_CannotReReviewNow", "This policy cannot be re-reviewed right now."));
		return true;
	}

	private bool TryBuildPolicyReReviewContext(
		string recordId,
		out PolicyReReviewContext context,
		out string error)
	{
		context = null;
		error = string.Empty;
		string id = (recordId ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			error = PolicyUiTexts.Get("AFPOL_UI_ReReviewRecordIdEmpty", "Policy record ID is empty.");
			return false;
		}

		LocalPolicyRecordSaveData local = LoadLocalPolicyRecords()
			.FirstOrDefault(record => string.Equals(record.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		if (local != null)
		{
			if (string.IsNullOrWhiteSpace(local.PolicyContent))
			{
				error = PolicyUiTexts.Get("AFPOL_UI_ReReviewNoFullContent", "The record does not have full policy content saved; cannot re-review.");
				return false;
			}
			string scope = string.Equals(local.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				? PolicyScopeVassal
				: PolicyScopeLocal;
			string root = FirstNonEmpty(local.ReReviewRootRecordId, local.RecordId);
			if (HasPendingPolicyReReview(scope, root))
			{
				error = PolicyUiTexts.Get("AFPOL_UI_ReReviewPendingConfirmation", "This policy lineage already has a pending evaluation awaiting confirmation; please wait for it to complete.");
				return false;
			}
			context = new PolicyReReviewContext
			{
				ScopeKind = scope,
				SourceRecordId = local.RecordId ?? id,
				RootRecordId = root,
				SupersedesRecordId = FindLatestActivePolicyInReReviewLineage(scope, root),
				PolicyName = local.PolicyName ?? string.Empty,
				PolicyContent = local.PolicyContent ?? string.Empty,
				DurationKnown = local.IsPermanentEffect || local.OriginalDurationDays > 0,
				IsPermanentEffect = local.IsPermanentEffect,
				DurationDays = Math.Max(0, local.OriginalDurationDays),
				SelectedTargetId = scope == PolicyScopeVassal ? local.TargetKingdomId ?? string.Empty : string.Empty,
				SelectedFiefIds = NormalizeIdList((local.OriginalTargetFiefIds?.Count ?? 0) > 0
					? local.OriginalTargetFiefIds
					: local.TargetFiefIds)
			};
			return true;
		}

		DynamicPolicySaveData dynamic = LoadDynamicPolicies()
			.FirstOrDefault(record => string.Equals(record.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(id, out NpcRulerPolicyRecord unified);
		string fullContent = FirstNonEmpty(dynamic?.PolicyContent, unified?.PolicyContent);
		if (string.IsNullOrWhiteSpace(fullContent))
		{
			error = PolicyUiTexts.Get("AFPOL_UI_ReReviewOnlySummary", "The record only saved a summary; no reusable full policy content.");
			return false;
		}
		string dynamicRoot = FirstNonEmpty(dynamic?.ReReviewRootRecordId, unified?.ReReviewRootRecordId, id);
		if (HasPendingPolicyReReview(PolicyScopeKingdom, dynamicRoot))
		{
			error = PolicyUiTexts.Get("AFPOL_UI_ReReviewPendingVote", "This policy lineage already has a pending evaluation awaiting a vote; please wait for the agenda to conclude.");
			return false;
		}
		PolicyRecordSaveData history = LoadPolicyRecordHistory()
			.FirstOrDefault(record => string.Equals(record.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		int recordedDuration = history?.Effects?.Where(effect => effect != null)
			.Select(effect => Math.Max(0, effect.TotalDurationDays))
			.DefaultIfEmpty(0)
			.Max() ?? 0;
		bool permanent = history?.IsPermanentEffect == true;
		int unifiedDuration = Math.Max(0, unified?.DurationDays ?? 0);
		context = new PolicyReReviewContext
		{
			ScopeKind = PolicyScopeKingdom,
			SourceRecordId = id,
			RootRecordId = dynamicRoot,
			SupersedesRecordId = FindLatestActivePolicyInReReviewLineage(PolicyScopeKingdom, dynamicRoot),
			PolicyName = FirstNonEmpty(dynamic?.PolicyName, unified?.PolicyName, history?.PolicyName),
			PolicyContent = fullContent,
			DurationKnown = permanent || recordedDuration > 0 || unifiedDuration > 0,
			IsPermanentEffect = permanent,
			DurationDays = Math.Max(recordedDuration, unifiedDuration),
			SelectedTargetId = FirstNonEmpty(dynamic?.OwnerKingdomId, unified?.KingdomId)
		};
		return true;
	}

	private bool ApplyPolicyReReviewLineageToRequest(
		PolicyReReviewContext context,
		PolicyDraftRequest request,
		out string error)
	{
		error = string.Empty;
		if (context == null)
		{
			return true;
		}
		if (request == null || string.IsNullOrWhiteSpace(context.RootRecordId))
		{
			error = PolicyUiTexts.Get("AFPOL_UI_ReReviewLineageInvalid", "Re-review lineage information is invalid.");
			return false;
		}
		if (HasPendingPolicyReReview(context.ScopeKind, context.RootRecordId))
		{
			error = PolicyUiTexts.Get("AFPOL_UI_ReReviewPendingConfirmation", "This policy lineage already has a pending evaluation awaiting confirmation; please wait for it to complete.");
			return false;
		}
		request.ReReviewRootRecordId = context.RootRecordId;
		request.ReReviewSourceRecordId = context.SourceRecordId;
		request.SupersedesRecordId = FindLatestActivePolicyInReReviewLineage(context.ScopeKind, context.RootRecordId);
		request.ReReviewReplacementCommitted = false;
		return true;
	}

	private bool HasPendingPolicyReReview(string scopeKind, string rootRecordId)
	{
		string root = (rootRecordId ?? string.Empty).Trim();
		if (root.Length == 0)
		{
			return false;
		}
		if (string.Equals(scopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase))
		{
			return LoadDynamicPolicies().Any(record => record != null
				&& !string.IsNullOrWhiteSpace(record.ReReviewSourceRecordId)
				&& string.Equals(FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId), root, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(record.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase));
		}
		return LoadLocalPolicyRecords().Any(record => record != null
			&& string.Equals(record.ScopeKind, scopeKind, StringComparison.OrdinalIgnoreCase)
			&& !record.ReReviewReplacementCommitted
			&& !string.IsNullOrWhiteSpace(record.ReReviewSourceRecordId)
			&& string.Equals(FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId), root, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(record.ExternalCommitState, PolicyCommitStateExternalCommitPending, StringComparison.OrdinalIgnoreCase));
	}

	private string FindLatestActivePolicyInReReviewLineage(string scopeKind, string rootRecordId)
	{
		string root = (rootRecordId ?? string.Empty).Trim();
		if (root.Length == 0)
		{
			return string.Empty;
		}
		if (string.Equals(scopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase))
		{
			return LoadDynamicPolicies()
				.Where(record => record != null
					&& string.Equals(FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId), root, StringComparison.OrdinalIgnoreCase)
					&& (string.Equals(record.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(record.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)))
				.OrderByDescending(record => record.CreatedUtcTicks)
				.Select(record => record.RecordId ?? string.Empty)
				.FirstOrDefault() ?? string.Empty;
		}
		return LoadLocalPolicyRecords()
			.Where(record => record != null
				&& string.Equals(record.ScopeKind, scopeKind, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId), root, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(record => record.CreatedUtcTicks)
			.Select(record => record.RecordId ?? string.Empty)
			.FirstOrDefault() ?? string.Empty;
	}

	private void OpenPolicyReReview(string recordId, Action returnToSource)
	{
		if (!TryBuildPolicyReReviewContext(recordId, out PolicyReReviewContext context, out string error))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_CannotReReviewTitle", "Cannot Re-review"), error, true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), string.Empty,
				returnToSource, null), pauseGameActiveState: true);
			return;
		}
		if (string.Equals(context.ScopeKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase))
		{
			OpenLocalPolicyComposePopup(returnToSource, context);
			return;
		}
		OpenComposePopup(context, returnToSource);
	}

	private void OpenPolicyReReviewFromHistory(string recordId, Action onClose)
	{
		bool localSource = LoadLocalPolicyRecords()
			.Any(record => string.Equals(record?.RecordId ?? string.Empty, recordId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		Action returnToSource = localSource
			? (Action)(() => OpenLocalPolicyHistoryPopup(onClose))
			: (Action)(() => OpenRecordHistoryPopup(onClose));
		OpenPolicyReReview(recordId, returnToSource);
	}

	private static LocalPolicyTargetSnapshotSaveData BuildLocalPolicyTargetSnapshot(Settlement fief)
	{
		return new LocalPolicyTargetSnapshotSaveData
		{
			FiefId = fief?.StringId ?? "",
			Name = fief?.Name?.ToString() ?? fief?.StringId ?? PolicyCompatibilityTexts.Get("Legacy_BuildPlayerVisiblePolicyEndReason_113"),
			TypeText = GetLocalPolicyFiefTypeText(fief),
			BoundVillageNames = GetBoundVillageSettlements(fief).Select(x => x.Name?.ToString() ?? x.StringId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
		};
	}

	private void OpenRecordHistoryPopup(Action onClose)
	{
		PolicyHistoryData data = BuildPolicyHistoryData();
		if (!CustomPolicyHistoryPopup.Show(data, recordId => OpenPolicyReReviewFromHistory(recordId, onClose), onClose,
			key => RequestDeletePolicyHistoryRecord(key, () => OpenRecordHistoryPopup(onClose))))
		{
			InformationManager.ShowInquiry(new InquiryData(data.TitleText ?? PolicyUiTexts.Get("AFPOL_UI_PolicyRecordsTitle", "Policy Records"), BuildPolicyHistoryFallbackText(data), true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), "", onClose, null), pauseGameActiveState: true, prioritize: false);
		}
	}

	private void OpenLocalPolicyHistoryPopup(Action onClose)
	{
		TrimLocalPolicyRecords();
		LocalPolicyHistoryData data = BuildLocalPolicyHistoryData();
		if (!LocalPolicyHistoryPopup.Show(data,
			recordId => RequestRenewLocalPolicy(recordId, onClose),
			recordId => RequestAbolishLocalPolicy(recordId, onClose),
			recordId => OpenPolicyReReviewFromHistory(recordId, onClose),
			onClose,
			key => RequestDeletePolicyHistoryRecord(key, () => OpenLocalPolicyHistoryPopup(onClose))))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalRecordsTitle", "Local Policy Records"), PolicyUiTexts.Get("AFPOL_UI_OpenLocalRecordsFailed", "Failed to open local policy records interface."), true, false, PolicyUiTexts.Get("AFPOL_UI_Back", "Back"), "", onClose, null), pauseGameActiveState: true);
		}
	}

	private void RequestRenewLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_LocalRecordNotExist", "Local policy record does not exist."), Colors.Red));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (record.IsPermanentEffect)
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_PermanentEffectNoExtension", "Permanent effects do not need extension."), Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
		{
			RequestRenewVassalPolicy(record, onClose);
			return;
		}
		if (!CanAttemptPolicyRenewal(record, out string renewalError))
		{
			InformationManager.DisplayMessage(new InformationMessage(renewalError, Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		List<Settlement> ownedTargets = ResolveOwnedLocalPolicyFiefs(GetLocalPolicyRenewalTargetIds(record));
		if (ownedTargets.Count <= 0)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_CannotExtendEffectTitle", "Cannot Extend Effect"), PolicyUiTexts.Get("AFPOL_UI_NoOwnedOriginalFiefsForExtension", "None of the original target fiefs are currently owned by the player; cannot renew. Try again after retaking them."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_ExtendLocalPolicyTitle", "Extend Local Policy Effect"), PolicyUiTexts.Format("AFPOL_UI_ExtendLocalPolicyNoStartupFeePrompt", "Extend \"{NAME}\" by one full effect cycle ({DAYS} days)?\n\nExtension does not recharge startup fees, does not recall intelligence service, and will not replay one-shot effects or republish feedback.", "NAME", record.PolicyName, "DAYS", record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture)), true, true, PolicyUiTexts.Get("AFPOL_UI_ConfirmExtend", "Confirm Extension"), PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel"),
			() => ConfirmRenewLocalPolicy(record.RecordId, onClose),
			() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
	}

	private void ConfirmRenewLocalPolicy(string recordId, Action onClose)
	{
		string renewalTransactionId = "renewal:" + Guid.NewGuid().ToString("N");
		PolicySystemLog.Lifecycle("Player", "renewal-start", "started", new PolicyLogContext
		{
			TransactionId = renewalTransactionId,
			RecordId = recordId,
			StateBefore = LocalPolicyStatusActive,
			StateAfter = "renewing"
		});
		bool hadOldRecord = _localPolicyRecords.TryGetValue(recordId ?? string.Empty, out string oldRecordRaw);
		Dictionary<string, string> oldActiveRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		bool renewalCommitted = false;
		try
		{
			LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
			if (record == null) throw new InvalidOperationException("Local policy record does not exist.");
			if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				|| record.IsPermanentEffect
				|| !CanAttemptPolicyRenewal(record, out _))
			{
				throw new InvalidOperationException("This local policy cannot extend its effect at present.");
			}
			oldActiveRaw = SnapshotPolicyEffectRawByRecordId(record.RecordId, PolicyScopeLocal);
			List<Settlement> ownedTargets = ResolveOwnedLocalPolicyFiefs(GetLocalPolicyRenewalTargetIds(record));
			if (ownedTargets.Count <= 0) throw new InvalidOperationException("All original target fiefs have been lost.");
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
						throw new InvalidOperationException("The publishing fief policy effect no longer has executable targets.");
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
				effectRecord.IsEnded = false;
				effectRecord.EndReason = string.Empty;
				renewedShellEffects.Add(active);
				renewedShellRecords.Add(effectRecord);
			}
			if (!renewedShellEffects.Any(active => !IsMentionedLocalPolicyEffect(active)))
			{
				throw new InvalidOperationException("The publishing fief policy effect can no longer be recovered.");
			}
			if (!TryMergeRenewedPolicyEffectShells(renewedShellEffects, string.Empty, out ActivePolicyEffectSaveData renewedActive, out string mergeError))
			{
				throw new InvalidOperationException("Local policy multi-target effects cannot be merged: " + mergeError);
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
				throw new InvalidOperationException("Module lifecycle preparation for extension failed: " + lifecycleError);
			}
			if (!TrySynchronizeRenewalRecordEffects(record, renewedActiveEffects, out string synchronizationError))
			{
				throw new InvalidOperationException("Record shell synchronization for extension failed: " + synchronizationError);
			}
			if (!TrySerializePreparedRenewalState(record, renewedActiveEffects, out string renewedRecordRaw, out Dictionary<string, string> renewedActiveRaw, out string serializationError))
			{
				throw new InvalidOperationException("Extension state validation failed: " + serializationError);
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
			PolicySystemLog.Lifecycle("Player", "renewal-committed", "success", new PolicyLogContext
			{
				TransactionId = renewalTransactionId,
				RecordId = record.RecordId,
				EffectId = renewedActive.EffectId,
				StateBefore = LocalPolicyStatusActive,
				StateAfter = LocalPolicyStatusActive,
				CampaignDay = GetCurrentCampaignDay(),
				Gold = charge,
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["renewalCount"] = record.RenewalCount,
					["addedDays"] = record.OriginalDurationDays,
					["remainingDays"] = record.RemainingDays
				}
			});
			EnqueueRenewedPolicyEffectWork(renewedActiveEffects);
			InvokeLocalPolicyLifecycleMemoryHook("renewed", record.RecordId, record.TargetFiefIds);
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_EffectExtendedTitle", "Effect Extended"), PolicyUiTexts.Format("AFPOL_UI_EffectExtendedRemaining", "Numeric effects for \"{NAME}\" have been extended by {DAYS} days; currently {REMAINING} days remaining.", "NAME", record.PolicyName, "DAYS", record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture), "REMAINING", record.RemainingDays.ToString(CultureInfo.InvariantCulture)), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			if (renewalCommitted)
			{
				PolicySystemLog.Failure("Player", "renewal-post-commit-failed", ex, new PolicyLogContext
				{
					TransactionId = renewalTransactionId,
					RecordId = recordId,
					StateBefore = LocalPolicyStatusActive,
					StateAfter = LocalPolicyStatusActive
				});
				InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyExtendedRefreshFailed", "Local policy effects were extended, but UI or notification refresh failed."), Colors.Yellow));
				return;
			}
			PolicySystemLog.Failure("Player", "renewal-failed", ex, new PolicyLogContext
			{
				TransactionId = renewalTransactionId,
				RecordId = recordId,
				StateBefore = LocalPolicyStatusActive,
				StateAfter = "unchanged"
			});
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_ExtendEffectFailedTitle", "Failed to Extend Effect"), PolicyUiTexts.Get("AFPOL_UI_ExtendEffectFailedMessage", "An exception occurred while extending effects; technical details have been written to the log."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
	}

	private void RequestAbolishLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null || !string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_OnlyActivePolicyCanBeAbolished", "Only currently active local policies can be abolished."), Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		if (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_StopVassalPolicyTitle", "Stop Vassal Policy"), PolicyUiTexts.Format("AFPOL_UI_StopVassalPolicyDetailedPrompt", "Are you sure you want to stop \"{NAME}\" immediately?\n\nContinuous effects across all kingdoms will cease immediately; independence will not change, fees are non-refundable, cooldown is not refreshed, and it cannot be renewed in the future.", "NAME", record.PolicyName), true, true, PolicyUiTexts.Get("AFPOL_UI_ConfirmStop", "Confirm Stop"), PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel"),
				() => ConfirmAbolishLocalPolicy(record.RecordId, onClose),
				() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_AbolishLocalPolicyTitle", "Abolish Local Policy"), PolicyUiTexts.Format("AFPOL_UI_AbolishLocalPolicyDetailedPrompt", "Are you sure you want to abolish \"{NAME}\" immediately?\n\nEffects will cease immediately, paid fees are non-refundable, and this policy cannot be extended in the future.", "NAME", record.PolicyName), true, true, PolicyUiTexts.Get("AFPOL_UI_ConfirmAbolish", "Confirm Abolish"), PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel"),
			() => ConfirmAbolishLocalPolicy(record.RecordId, onClose),
			() => OpenLocalPolicyHistoryPopup(onClose)), pauseGameActiveState: true);
	}

	private void ConfirmAbolishLocalPolicy(string recordId, Action onClose)
	{
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null) { OpenLocalPolicyHistoryPopup(onClose); return; }
		string abolitionTransactionId = "abolition:" + Guid.NewGuid().ToString("N");
		PolicySystemLog.Lifecycle("Player", "abolition-start", "started", new PolicyLogContext
		{
			TransactionId = abolitionTransactionId,
			RecordId = record.RecordId,
			StateBefore = record.Status,
			StateAfter = "abolishing"
		});
		bool isVassalPolicy = string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		if (!TryStopLocalPolicyRecord(record.RecordId, isVassalPolicy ? PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_114") : PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_115"), out string stopError))
		{
			PolicySystemLog.Failure("Player", "abolition-failed",
				"transactionId=" + abolitionTransactionId + " recordId=" + (record.RecordId ?? string.Empty),
				stopError);
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_StopPolicyFailedTitle", "Failed to Stop Policy"), PolicyUiTexts.Get("AFPOL_UI_StopPolicyFailedMessage", "An exception occurred while stopping policy; technical details have been written to the log."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), string.Empty,
				() => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		TrimLocalPolicyRecords();
		PolicySystemLog.Lifecycle("Player", "abolition-complete", "success", new PolicyLogContext
		{
			TransactionId = abolitionTransactionId,
			RecordId = record.RecordId,
			StateBefore = LocalPolicyStatusActive,
			StateAfter = LocalPolicyStatusAbolished
		});
		string title = isVassalPolicy ? PolicyUiTexts.Get("AFPOL_UI_VassalPolicyStoppedTitle", "Vassal Policy Stopped") : PolicyUiTexts.Get("AFPOL_UI_LocalPolicyAbolishedTitle", "Local Policy Abolished");
		string body = isVassalPolicy
			? PolicyUiTexts.Format("AFPOL_UI_VassalPolicyStoppedMessage", "Continuous effects of \"{NAME}\" across all kingdoms have ceased; independence remains unchanged.", "NAME", record.PolicyName)
			: PolicyUiTexts.Format("AFPOL_UI_LocalPolicyAbolishedMessage", "Effects of \"{NAME}\" have ceased; fees are non-refundable.", "NAME", record.PolicyName);
		InformationManager.ShowInquiry(new InquiryData(title, body, true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
	}

	private bool TryStopLocalPolicyRecord(string recordId, string reason, out string error)
	{
		error = string.Empty;
		LocalPolicyRecordSaveData record = LoadLocalPolicyRecords().FirstOrDefault(x => string.Equals(x.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
		if (record == null || !string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		try
		{
			bool isVassalPolicy = string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
		DispatchPolicyEffectRecordAbolishedBeforeRemoval(
			record.RecordId,
			"record:" + record.RecordId + ":abolished:" + NormalizeDynamicPolicyIdPart(reason),
			reason);
		if (isVassalPolicy)
		{
			RemoveVassalPolicyEffectsByRecordId(record.RecordId);
			foreach (LocalPolicyEffectRecordSaveData effect in record.Effects.Where(x => x != null))
			{
				NpcRulerPolicyBehavior.UpdatePolicyEffectStateForExternal(record.RecordId, effect.ActiveEffectId, effect.TargetKingdomId, 0, isEnded: true);
			}
			NpcRulerPolicyBehavior.UpdatePolicyAgendaStatusForExternal(record.RecordId, DynamicPolicyStatusAbolished);
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
			effect.IsEnded = true;
		}
		InvalidatePolicyRecordPresentation();
		record.Status = LocalPolicyStatusAbolished;
		record.EffectStatus = LocalPolicyStatusAbolished;
		record.EndReason = string.IsNullOrWhiteSpace(reason) ? (isVassalPolicy ? PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_114") : PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_115")) : reason;
		record.RemainingDays = 0;
		_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
		_activePolicyEffectModelCache.Clear();
		if (!isVassalPolicy) InvokeLocalPolicyLifecycleMemoryHook("abolished", record.RecordId, record.TargetFiefIds);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private enum PolicyReReviewReplacementAction
	{
		None,
		StopPredecessor,
		Invalid
	}

	private sealed class PolicyReReviewReplacementFacts
	{
		internal string StorageKey;
		internal string RecordId;
		internal string ScopeKind;
		internal string RootRecordId;
		internal string SourceRecordId;
		internal string SupersedesRecordId;
		internal bool PublicationCommitted;
		internal bool CanStop;
	}

	private static PolicyReReviewReplacementFacts DescribePolicyReReviewReplacement(LocalPolicyRecordSaveData record)
		=> record == null ? null : new PolicyReReviewReplacementFacts
		{
			StorageKey = record.RecordId,
			RecordId = record.RecordId,
			ScopeKind = record.ScopeKind,
			RootRecordId = record.ReReviewRootRecordId,
			SourceRecordId = record.ReReviewSourceRecordId,
			SupersedesRecordId = record.SupersedesRecordId,
			PublicationCommitted = record.ReReviewReplacementCommitted
				|| (string.Equals(record.ScopeKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
				|| (string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
					&& (record.ExternalCommitState ?? string.Empty).StartsWith("externalCommitted", StringComparison.OrdinalIgnoreCase)),
			CanStop = string.Equals(record.Status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
		};

	private static PolicyReReviewReplacementFacts DescribePolicyReReviewReplacement(DynamicPolicySaveData record)
		=> record == null ? null : new PolicyReReviewReplacementFacts
		{
			StorageKey = record.PolicyObjectId,
			RecordId = record.RecordId,
			ScopeKind = PolicyScopeKingdom,
			RootRecordId = record.ReReviewRootRecordId,
			SourceRecordId = record.ReReviewSourceRecordId,
			SupersedesRecordId = record.SupersedesRecordId,
			PublicationCommitted = record.ReReviewReplacementCommitted
				|| (string.Equals(record.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(record.CommitState, PolicyCommitStateActive, StringComparison.OrdinalIgnoreCase)),
			CanStop = string.Equals(record.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(record.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
		};

	private static bool CanReconcilePolicyReReviewReplacement(PolicyReReviewReplacementFacts replacement)
		=> replacement != null && replacement.PublicationCommitted
			&& !string.IsNullOrWhiteSpace(replacement.RecordId)
			&& !string.IsNullOrWhiteSpace(replacement.SourceRecordId)
			&& !string.IsNullOrWhiteSpace(replacement.RootRecordId);

	private static PolicyReReviewReplacementAction DecidePolicyReReviewReplacement(
		PolicyReReviewReplacementFacts replacement,
		PolicyReReviewReplacementFacts predecessor)
	{
		if (!CanReconcilePolicyReReviewReplacement(replacement)
			|| string.IsNullOrWhiteSpace(replacement.SupersedesRecordId))
		{
			return PolicyReReviewReplacementAction.None;
		}
		if (string.Equals(replacement.RecordId, replacement.SupersedesRecordId, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyReReviewReplacementAction.Invalid;
		}
		// Missing history is not permission to select another active record from the lineage.
		if (predecessor == null) return PolicyReReviewReplacementAction.None;
		if (!string.Equals(predecessor.RecordId, replacement.SupersedesRecordId, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(predecessor.ScopeKind, replacement.ScopeKind, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(FirstNonEmpty(predecessor.RootRecordId, predecessor.RecordId), replacement.RootRecordId, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyReReviewReplacementAction.Invalid;
		}
		return predecessor.CanStop ? PolicyReReviewReplacementAction.StopPredecessor : PolicyReReviewReplacementAction.None;
	}

	private bool TryReadCurrentPolicyReReviewReplacement(
		PolicyReReviewReplacementFacts identity,
		out PolicyReReviewReplacementFacts current)
	{
		current = null;
		if (identity == null) return true;
		if (string.IsNullOrWhiteSpace(identity.StorageKey)) return false;
		try
		{
			if (string.Equals(identity.ScopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase))
			{
				if (_quarantinedDynamicPolicyIds.Contains(identity.StorageKey)) return false;
				if (!_dynamicPolicyRegistry.TryGetValue(identity.StorageKey, out string rawDynamic)) return true;
				current = DescribePolicyReReviewReplacement(JsonConvert.DeserializeObject<DynamicPolicySaveData>(rawDynamic));
			}
			else
			{
				if (!_localPolicyRecords.TryGetValue(identity.StorageKey, out string rawLocal)) return true;
				current = DescribePolicyReReviewReplacement(NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(rawLocal)));
			}
			return current != null
				&& string.Equals(current.StorageKey, identity.StorageKey, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(current.RecordId, identity.RecordId, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(current.ScopeKind, identity.ScopeKind, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private bool FinalizePolicyReReviewReplacement(string newRecordId)
	{
		string id = (newRecordId ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			return false;
		}
		List<LocalPolicyRecordSaveData> localRecords = LoadLocalPolicyRecords();
		LocalPolicyRecordSaveData local = localRecords
			.FirstOrDefault(record => string.Equals(record.RecordId ?? string.Empty, id, StringComparison.OrdinalIgnoreCase));
		List<DynamicPolicySaveData> dynamicRecords = local == null ? LoadDynamicPolicies() : null;
		DynamicPolicySaveData dynamic = dynamicRecords?.FirstOrDefault(record => string.Equals(record.RecordId, id, StringComparison.OrdinalIgnoreCase));
		PolicyReReviewReplacementFacts replacement = local != null
			? DescribePolicyReReviewReplacement(local) : DescribePolicyReReviewReplacement(dynamic);
		if (replacement == null) return false;
		PolicyReReviewReplacementFacts predecessor = local != null
			? DescribePolicyReReviewReplacement(localRecords.FirstOrDefault(record => string.Equals(record.RecordId, replacement.SupersedesRecordId, StringComparison.OrdinalIgnoreCase)))
			: DescribePolicyReReviewReplacement(dynamicRecords.FirstOrDefault(record => string.Equals(record.RecordId, replacement.SupersedesRecordId, StringComparison.OrdinalIgnoreCase)));
		if (DecidePolicyReReviewReplacement(replacement, predecessor) == PolicyReReviewReplacementAction.Invalid) return false;
		if (!CanReconcilePolicyReReviewReplacement(replacement)) return true;

		if (local != null)
		{
			local.ReReviewReplacementCommitted = true;
			_localPolicyRecords[local.RecordId] = JsonConvert.SerializeObject(local);
		}
		else
		{
			dynamic.ReReviewReplacementCommitted = true;
			StoreDynamicPolicy(dynamic);
		}
		NpcRulerPolicyBehavior.MarkPlayerPolicyReReviewCommittedForExternal(id);

		// Cross-owner publication may invalidate the snapshot; never write it back after stopping.
		if (!TryReadCurrentPolicyReReviewReplacement(replacement, out replacement)
			|| !TryReadCurrentPolicyReReviewReplacement(predecessor, out predecessor)
			|| replacement == null) return false;
		PolicyReReviewReplacementAction action = DecidePolicyReReviewReplacement(replacement, predecessor);
		if (action == PolicyReReviewReplacementAction.Invalid) return false;
		if (action == PolicyReReviewReplacementAction.None) return true;
		bool kingdom = string.Equals(replacement.ScopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase);
		string stopError;
		bool success = kingdom
			? TryStopDynamicPolicyRecordForReReview(predecessor.RecordId, out stopError)
			: TryStopLocalPolicyRecord(predecessor.RecordId, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_116"), out stopError);
		if (!success)
		{
			PolicySystemLog.Failure("ReReview", kingdom ? "kingdom-predecessor-stop-failed" : "local-predecessor-stop-failed", stopError,
				"newRecordId=" + id + " predecessor=" + predecessor.RecordId);
		}
		return success;
	}

	private bool TryStopDynamicPolicyRecordForReReview(string recordId, out string error)
	{
		error = string.Empty;
		DynamicPolicySaveData data = LoadDynamicPolicies()
			.FirstOrDefault(record => string.Equals(record.RecordId ?? string.Empty, recordId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
		if (data == null
			|| (!string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(data.Status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}
		try
		{
			Kingdom owner = ResolveKingdomByIdOrName(data.OwnerKingdomId, string.Empty);
			foreach (KingdomPolicyDecision decision in FindDynamicPolicyDecisions(owner, data.PolicyObjectId).ToList())
			{
				owner?.RemoveDecision(decision);
			}
			PolicyObject policy = owner?.ActivePolicies?.FirstOrDefault(item => item != null
				&& string.Equals(item.StringId ?? string.Empty, data.PolicyObjectId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
				?? EnsureDynamicPolicyObject(data);
			if (owner != null && policy != null && owner.ActivePolicies?.Contains(policy) == true)
			{
				owner.RemovePolicy(policy);
			}
			CompleteDynamicPolicyAbolition(data, policy, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_116"),
				"record:" + data.RecordId + ":abolished:rereview");
			data.CommitState = PolicyCommitStateEnded;
			StoreDynamicPolicy(data);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private void ReconcilePolicyReReviewReplacementsAfterLoad()
	{
		List<string> committedRecordIds = LoadLocalPolicyRecords()
			.Select(record => DescribePolicyReReviewReplacement(record))
			.Concat(LoadDynamicPolicies().Select(record => DescribePolicyReReviewReplacement(record)))
			.Where(CanReconcilePolicyReReviewReplacement)
			.Select(record => record.RecordId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		foreach (string recordId in committedRecordIds)
		{
			if (!FinalizePolicyReReviewReplacement(recordId))
			{
				PolicySystemLog.Failure("ReReview", "reload-reconciliation-incomplete",
					"Replacement reconciliation did not complete; existing records were retained for a later load.",
					"recordId=" + recordId);
			}
		}
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
		if (!CanAttemptPolicyRenewal(record, out string renewalError))
		{
			InformationManager.DisplayMessage(new InformationMessage(renewalError, Colors.Yellow));
			OpenLocalPolicyHistoryPopup(onClose);
			return;
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom targetKingdom = ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName);
		if (!IsPlayerRuler(playerKingdom)
			|| !string.Equals(playerKingdom?.StringId ?? "", record.IssuerKingdomId ?? "", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_CannotRenewTitle", "Cannot Renew"), PolicyUiTexts.Get("AFPOL_UI_NoRulerPermissionToRenew", "You do not have ruler authority of the issuing kingdom; policy unchanged."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		if (targetKingdom == null || targetKingdom.IsEliminated
			|| !VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int currentIndependence, out int breakawayThreshold, out int rulerRelation, out string rulerName))
		{
			OnVassalRelationshipEndedInternal(record.TargetKingdomId, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_117"));
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_CannotRenewTitle", "Cannot Renew"), PolicyUiTexts.Get("AFPOL_UI_TargetNotVassalTerminated", "Target kingdom is no longer a direct vassal of the player kingdom; policy terminated."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
			return;
		}
		int independenceCost = Math.Max(0, record.InitialIndependenceCost);
		InformationManager.ShowInquiry(new InquiryData(
			PolicyUiTexts.Get("AFPOL_UI_RenewVassalPolicyTitle", "Renew Vassal Policy"),
			PolicyUiTexts.Format("AFPOL_UI_RenewVassalPolicyPrompt", "Extend \"{NAME}\" by one full cycle ({DAYS} days)?\n\nThis will re-add the initial random cost of {COST} independence; current independence is {CURRENT}/100, breakaway threshold is {THRESHOLD} ({RULER} relation: {RELATION}). Intelligence service will not be recalled, good/bad modifiers will not repeat, one-shot modules will not replay, and no new world policy events will be created.", "NAME", record.PolicyName, "DAYS", record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture), "COST", independenceCost.ToString(CultureInfo.InvariantCulture), "CURRENT", currentIndependence.ToString(CultureInfo.InvariantCulture), "THRESHOLD", breakawayThreshold.ToString(CultureInfo.InvariantCulture), "RULER", rulerName, "RELATION", FormatSigned(rulerRelation)),
			true,
			true,
			PolicyUiTexts.Get("AFPOL_UI_ConfirmRenewal", "Confirm Renewal"),
			PolicyUiTexts.Get("AFPOL_UI_Cancel", "Cancel"),
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
				throw new InvalidOperationException("Vassal policy record does not exist.");
			}
			if (record.IsPermanentEffect || !CanAttemptPolicyRenewal(record, out _))
			{
				throw new InvalidOperationException("This policy cannot be renewed at present.");
			}
			oldActiveRaw = SnapshotPolicyEffectRawByRecordId(record.RecordId, PolicyScopeVassal);
			Kingdom playerKingdom = GetPlayerKingdom();
			Kingdom targetKingdom = ResolveKingdomByIdOrName(record.TargetKingdomId, record.TargetKingdomName);
			if (!IsPlayerRuler(playerKingdom)
				|| !string.Equals(playerKingdom?.StringId ?? "", record.IssuerKingdomId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("No ruler authority for the original publishing kingdom at present.");
			}
			if (targetKingdom == null || targetKingdom.IsEliminated
				|| !VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(record.TargetKingdomId, out int independenceBefore, out int breakawayThreshold, out int rulerRelation, out string rulerName))
			{
				OnVassalRelationshipEndedInternal(record.TargetKingdomId, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_118"));
				throw new InvalidOperationException("The target kingdom is no longer a direct vassal of the player's kingdom.");
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
				throw new InvalidOperationException("Renewal external submission preparation failed: " + externalPrepareError);
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
				effectRecord.IsEnded = false;
				effectRecord.EndReason = string.Empty;
				renewedShellEffects.Add(active);
				renewedShellRecords.Add(effectRecord);
			}
			if (!renewedShellEffects.Any(active => string.Equals(
				active.TargetKingdomId ?? string.Empty,
				record.TargetKingdomId ?? string.Empty,
				StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidOperationException("The target vassal's policy effect can no longer be recovered.");
			}
			if (!TryMergeRenewedPolicyEffectShells(renewedShellEffects, record.TargetKingdomId, out ActivePolicyEffectSaveData renewedActive, out string mergeError))
			{
				throw new InvalidOperationException("Vassal policy multi-target effects cannot be merged: " + mergeError);
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
			record.EffectStatus = LocalPolicyStatusActive;
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
				throw new InvalidOperationException("Renewal module lifecycle preparation failed: " + lifecycleError);
			}
			if (!TrySynchronizeRenewalRecordEffects(record, renewedActiveEffects, out string synchronizationError))
			{
				throw new InvalidOperationException("Renewal record shell synchronization failed: " + synchronizationError);
			}
			if (!TrySerializePreparedRenewalState(record, renewedActiveEffects, out string continuingRecordRaw, out Dictionary<string, string> renewedActiveRaw, out string serializationError))
			{
				throw new InvalidOperationException("Renewal state validation failed: " + serializationError);
			}
			LocalPolicyRecordSaveData breakawayRecord = CloneRenewalRecord(record);
			MarkPreparedVassalRenewalRelationshipEnded(breakawayRecord, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_119"));
			if (!TrySerializePreparedRenewalState(breakawayRecord, Array.Empty<ActivePolicyEffectSaveData>(), out string breakawayRecordRaw, out Dictionary<string, string> _, out string breakawaySerializationError))
			{
				throw new InvalidOperationException("Renewal state validation after breakaway failed: " + breakawaySerializationError);
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
				throw new InvalidOperationException("Renewal independence submission result is not yet observable; externalCommitPending retained for daily idempotent reconciliation.");
			}
			if ((externalResult.Kind == VassalPolicyExternalCommitResultKind.Unchanged
					|| externalResult.Kind == VassalPolicyExternalCommitResultKind.Conflict)
				&& unchanged)
			{
				TryRestorePolicyRenewalSnapshot(recordId, PolicyScopeVassal, hadOldRecord, oldRecordRaw, oldActiveRaw);
				throw new InvalidOperationException("Renewal independence external submission did not occur; pre-submission AF state restored.");
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
			string resultText = PolicyUiTexts.Format("AFPOL_UI_VassalRenewalSuccessMessage", "\"{NAME}\" extended by {DAYS} days; independence {BEFORE} + {COST} = {AFTER}/100; breakaway threshold {THRESHOLD} ({RULER} relation: {RELATION}).", "NAME", record.PolicyName, "DAYS", record.OriginalDurationDays.ToString(CultureInfo.InvariantCulture), "BEFORE", appliedBefore.ToString(CultureInfo.InvariantCulture), "COST", independenceCost.ToString(CultureInfo.InvariantCulture), "AFTER", independenceAfter.ToString(CultureInfo.InvariantCulture), "THRESHOLD", breakawayThreshold.ToString(CultureInfo.InvariantCulture), "RULER", rulerName, "RELATION", FormatSigned(rulerRelation));
			if (brokeAway) resultText += PolicyUiTexts.Get("AFPOL_UI_VassalBreakawayNotice", "\n\nIndependence reached breakaway threshold; vassalage has ended and all continuous effects have ceased immediately.");
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_RenewalSuccessTitle", "Renewal Succeeded"), resultText, true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			if (renewalCommitted)
			{
				PolicySystemLog.Write("VassalPolicy", "renew-post-commit-failed", ex.ToString());
				InformationManager.DisplayMessage(new InformationMessage(PolicyUiTexts.Get("AFPOL_UI_VassalRenewedRefreshFailed", "Vassal policy renewed, but post-renewal UI or notification refresh failed."), Colors.Yellow));
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
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_RenewalFailedTitle", "Renewal Failed"), PolicyUiTexts.Get("AFPOL_UI_RenewalFailedMessage", "An exception occurred during renewal; technical details have been written to the log."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", () => OpenLocalPolicyHistoryPopup(onClose), null), pauseGameActiveState: true);
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
			error = "No renewal target shell to merge";
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
				error = "Target shell record/scope/duration/progress are inconsistent";
				return false;
			}
			HashSet<string> shellInstanceIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (PolicyEffectInstanceSaveData instance in shell.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				string instanceId = (instance?.InstanceId ?? string.Empty).Trim();
				if (instanceId.Length == 0 || !shellInstanceIds.Add(instanceId))
				{
					error = "Target shell contains empty or duplicate InstanceId";
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
			error = "Merged logical module instance count is invalid";
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
					error = "InstanceId " + instanceId + " target shell is incompatible: " + incompatibility;
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
			error = "Instance is empty";
			return false;
		}
		if (!string.Equals(canonical.ModuleId ?? string.Empty, candidate.ModuleId ?? string.Empty, StringComparison.Ordinal)
			|| !string.Equals(canonical.SourceModuleId ?? string.Empty, candidate.SourceModuleId ?? string.Empty, StringComparison.Ordinal)
			|| canonical.PayloadSchemaVersion != candidate.PayloadSchemaVersion
			|| !PolicyEffectTokensEqual(canonical.Payload, candidate.Payload))
		{
			error = "module/payload are inconsistent";
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
			error = "policy/runtime/lifecycle/timeline are inconsistent";
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
					error = module.Id + " renewal instance is invalid: " + FirstNonEmpty(normalizeError, normalized?.InertReason);
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
					error = module.Id + " renewal Prepare failed: " + (prepare?.Error ?? "No prepared instance returned");
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
					error = module.Id + " OnRenewed failed: " + (renewed?.Error ?? "No successful result returned");
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
						error = module.Id + " OnRenewed returned an invalid receipt";
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
			error = "Active v8 extension structure is incomplete";
			return false;
		}
		if (IsLocalActivePolicyEffect(active)
			&& (active.TargetSettlementIds?.Count ?? 0) == 0
			&& !(active.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Any(instance => HasDynamicPolicyEffectTargetDefinition(instance?.TargetSet)))
		{
			error = "Local renewal Active lacks current settlement target";
			return false;
		}
		if (IsVassalActivePolicyEffect(active))
		{
			Kingdom target = ResolveKingdomByIdOrName(active.TargetKingdomId, active.TargetKingdomName);
			if (target == null || target.IsEliminated)
			{
				error = "Vassal renewal Active has an invalid target kingdom";
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
				error = "Renewal module instance is invalid: " + FirstNonEmpty(instanceId, normalizeError);
				return false;
			}
			totalPayloadBytes += Encoding.UTF8.GetByteCount(instance.Payload.ToString(Formatting.None));
			if (totalPayloadBytes > PolicyEffectSaveCodec.MaxTotalPayloadBytes)
			{
				error = "Renewal module payload total exceeds 32KiB";
				return false;
			}
			moduleIdByInstanceId[instanceId] = instance.ModuleId ?? string.Empty;
			if (!PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module))
			{
				if (!HasAnyPolicyEffectCanonicalTarget(instance.TargetSet))
				{
					error = "Unknown module renewal instance lacks retainable canonical target: " + (instance.ModuleId ?? string.Empty);
					return false;
				}
				continue;
			}
			if (normalized == null || normalized.IsInert || normalized.RuntimeInstance == null)
			{
				error = module.Id + " renewal payload is not executable: " + (normalized?.InertReason ?? string.Empty);
				return false;
			}
			string targetError = string.Empty;
			if (!PolicyEffectModuleCatalog.IsAllowedForScope(module, active.ScopeKind)
				|| (!HasPolicyEffectCanonicalTargetsForModule(module, instance.TargetSet)
					&& !HasDynamicPolicyEffectTargetDefinition(instance.TargetSet))
				|| !AreRenewedModuleTargetsResolvable(module, instance.TargetSet, out targetError))
			{
				error = module.Id + " renewal target or scope is invalid: " + targetError;
				return false;
			}
			if (IsNonRenewableOneTimePolicyEffect(module)
				&& instance.LifecycleState != PolicyEffectLifecycleState.Completed)
			{
				error = module.Id + " renewal must not replay OneShot";
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
				error = "Renewal Active contains invalid, duplicate, or orphaned execution receipts";
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
				error = targetKind + " target is empty";
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
					error = targetKind + " target does not exist: " + id;
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
				error = "Record shell references a non-existent Active: " + effectRecord.ActiveEffectId;
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
					error = "Record shell contains empty or duplicate InstanceId";
					return false;
				}
				if (!activeByInstanceId.TryGetValue(instanceId, out PolicyEffectInstanceSaveData activeInstance))
				{
					error = "Record shell logical instance did not enter Active: " + instanceId;
					return false;
				}
				PolicyEffectCanonicalTargetSet shellTargetSet = NormalizePolicyEffectCanonicalTargetSet(shellInstance.TargetSet);
				if (!HasAnyPolicyEffectCanonicalTarget(shellTargetSet))
				{
					error = "Record shell logical instance lacks target: " + instanceId;
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
				error = "Record shell contains no module instances";
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
					error = FirstNonEmpty(error, "Renewal Active serialization result exceeds limit or cannot be normalized");
					return false;
				}
				if (activeRawById.ContainsKey(active.EffectId))
				{
					error = "Renewal Active effectId is duplicate: " + active.EffectId;
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
				error = "Renewal record v6 structure is incomplete";
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
				error = "Renewal record and Active reference sets are inconsistent";
				return false;
			}
			recordRaw = JsonConvert.SerializeObject(record, Formatting.None);
			if (Encoding.UTF8.GetByteCount(recordRaw) > PolicyEffectSaveCodec.MaxSerializedEnvelopeBytes
				|| !PolicyEffectSaveCodec.TryNormalizeLocalV1ToV6(JObject.Parse(recordRaw), out _, out _, out error))
			{
				error = FirstNonEmpty(error, "Renewal record serialization result exceeds limit or cannot be normalized");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			recordRaw = string.Empty;
			activeRawById.Clear();
			error = "Renewal state serialization failed: " + ex.Message;
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
			throw new InvalidOperationException("Renewal submission data is incomplete.");
		}
		foreach (string newEffectId in newActiveById.Keys)
		{
			if (_activePolicyEffects.ContainsKey(newEffectId)
				&& !(oldActiveRaw?.ContainsKey(newEffectId) ?? false))
			{
				throw new InvalidOperationException("Renewal effectId conflicts with existing Active: " + newEffectId);
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
			InvalidatePolicyRecordPresentation();
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
			throw new InvalidOperationException("Renewal state submission failed; " + (string.IsNullOrWhiteSpace(restoreError) ? "the old snapshot was restored." : "restoring the old snapshot also failed."), commitError);
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
		record.EndReason = string.IsNullOrWhiteSpace(reason) ? PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_126") : reason.Trim();
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
		int renewedTotalDurationDays,
		float renewalDay,
		int renewedRemainingDays)
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
				if (renewedTotalDurationDays > addedDurationDays && instance.EndDay > renewalDay)
				{
					instance.EndDay += addedDays;
				}
				else
				{
					instance.StartDay = renewalDay;
					instance.EndDay = renewalDay + Math.Max(1, renewedRemainingDays);
				}
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
			totalDurationDays,
			GetCurrentCampaignDay(),
			remainingDays);
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
			throw new InvalidOperationException("Vassal policy effect record is invalid.");
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
			totalDurationDays,
			GetCurrentCampaignDay(),
			remainingDays);
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

	private void InvokeLocalPolicyLifecycleMemoryHook(string eventKind, string recordId, IEnumerable<string> targetFiefIds)
	{
		// Only post-commit lifecycle facts. No NPC/AFEF write, forecast or full policy scan.
		try
		{
			if (!_localPolicyRecords.TryGetValue(recordId ?? "", out string raw)) return;
			var record = JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw);
			if (record == null) return;
			string fact = AnimusForge.SiegeAftermathUiText.Policy(eventKind, record.PolicyName);
			if (string.IsNullOrWhiteSpace(fact)) return;
			string eventId = "policy:" + recordId + ":" + eventKind + ":" + (record.Renewals?.Count ?? 0);
			foreach (string id in (targetFiefIds ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
				GcczTownRuleMemoryRuntimeBridge.RecordConfirmedEvent(Settlement.Find(id), eventId, fact);
		}
		catch (Exception ex) { Logger.Log("GcczTownRuleMemory", "Policy fact capture failed: " + ex.Message); }
	}

	private static void ShowPolicyRenewalResultPopup(string policyObjectId, PolicyDraftRequest request, PolicyApplicationResult application)
	{
		string sequencePolicyObjectId = (policyObjectId ?? "").Trim();
		string policyName = string.IsNullOrWhiteSpace(request?.PolicyName) ? PolicyUiTexts.Get("AFPOL_UI_UnnamedPolicy", "Unnamed Policy") : request.PolicyName.Trim();
		int actualGoldCost = Math.Max(0, request?.GoldCost ?? 0);
		int durationDays = application?.KingdomEffects?.Where(effect => effect != null)
			.Select(effect => Math.Max(0, effect.DurationDays))
			.DefaultIfEmpty(0)
			.Max() ?? 0;
		string body = request != null && request.GoldEffectScale < 0.9999f
			? PolicyUiTexts.Format("AFPOL_UI_PolicyRenewedDetailedBody", "\"{NAME}\" extended {DAYS} days.\nRenewal cost: {COST} denars.\nEffects active at {SCALE} this cycle.", "NAME", policyName, "DAYS", durationDays.ToString(CultureInfo.InvariantCulture), "COST", actualGoldCost.ToString(CultureInfo.InvariantCulture), "SCALE", FormatPercent(request.GoldEffectScale))
			: PolicyUiTexts.Format("AFPOL_UI_PolicyRenewedDetailedNoScale", "\"{NAME}\" extended {DAYS} days.\nRenewal cost: {COST} denars.", "NAME", policyName, "DAYS", durationDays.ToString(CultureInfo.InvariantCulture), "COST", actualGoldCost.ToString(CultureInfo.InvariantCulture));
		BeginPolicySuccessResultSequence(sequencePolicyObjectId);
		InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PolicyRenewedTitle", "Policy Extended"), body, true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", delegate
		{
			CompletePolicySuccessResultSequence(sequencePolicyObjectId);
		}, null), pauseGameActiveState: true);
	}

	private static void ShowPolicySuccessResultPopup(string policyObjectId, string impactText)
	{
		string sequencePolicyObjectId = (policyObjectId ?? "").Trim();
		string bodyText = impactText ?? "";
		BeginPolicySuccessResultSequence(sequencePolicyObjectId);
		bool shown = CustomPolicyResultPopup.Show(PolicyUiTexts.Get("AFPOL_UI_PolicyPublished", "Policy Published"), bodyText, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), delegate
		{
			CompletePolicySuccessResultSequence(sequencePolicyObjectId);
		});
		if (!shown)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PolicyPublished", "Policy Published"), bodyText, true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", delegate
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
				PolicyUiTexts.Get("AFPOL_UI_PolicyAdoptionFailedTitle", "Policy Enactment Failed"),
				PolicyUiTexts.Get("AFPOL_UI_PolicyAdoptionFailedMessage", "Agenda vote passed, but atomic commit of policy effects, ledger, or costs failed. AnimusForge has reverted this adoption; the policy did not take effect and will not show as active. Detailed reasons written to PolicySystem.txt."),
				true,
				false,
				PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"),
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
