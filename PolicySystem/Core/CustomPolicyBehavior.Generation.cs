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
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
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
	private const int PlayerPolicySemanticRepairRejectedOutputMaxChars = 24000;
	private const int PlayerPolicySemanticRepairErrorMaxChars = 1200;
	internal static Func<string, string, long, Task<string>> PlayerPolicyApiTextOverrideForTests;

	private void CompleteLocalPolicyGeneration(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		List<Settlement> validFiefs = ResolveOwnedLocalPolicyFiefs(request?.SelectedFiefIds);
		if (validFiefs.Count <= 0)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyCancelledTitle", "Local Policy Cancelled"), PolicyUiTexts.Get("AFPOL_UI_LocalPolicyLostFiefsCancelled", "All target fiefs were lost during evaluation; no fees deducted, not enacted, and no record created."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		request.SelectedFiefIds = validFiefs.Select(x => x.StringId).ToList();
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions(request);
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, hasOwnedFief: true);
		if (!eligibility.CanPublish)
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyCannotPublishTitle", "Local Policy Cannot Be Published"), MapPlayerPolicyEligibilityReasonToDisplay(eligibility.Reason) + PolicyUiTexts.Get("AFPOL_UI_LocalPolicyCannotPublishSuffix", "\n\nEvaluation is complete, but no fees were deducted, the policy did not take effect, and no record was created."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		if (!TryPrepareLocalPolicyCostForApplication(request, result.MainAssessment, out string costError))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyEvalFailedTitle", "Local Policy Evaluation Failed"), BuildPolicyFailurePopupText(costError, result) + PolicyUiTexts.Get("AFPOL_UI_NoFeesNotEnactedSuffix", "\n\nNo fees deducted, not enacted."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		result.Postprocess = BuildPostprocessResultFromMainAssessment(request, result.MainAssessment);
		if (string.IsNullOrWhiteSpace(result.PostprocessRaw))
		{
			result.PostprocessRaw = SafeSerializeForDebug(result.Postprocess);
		}
		bool requiresEffectBundle = string.Equals(
			result.Postprocess?.Disposition,
			"executable",
			StringComparison.Ordinal);
		if (!requiresEffectBundle)
		{
			// A narrative-only or unsupported judgment is a successful policy result, but it must
			// not create a maintenance charge for an effect bundle that does not exist.
			request.DailyMaintenanceGoldCost = 0;
			request.TotalMaintenancePaidGold = 0;
			request.MaintenanceFunded = true;
		}
		PolicyApplicationResult application = ApplyLocalPolicyEffects(request, result.Postprocess, validFiefs);
		AppliedKingdomEffect sourceEffect = application.KingdomEffects.FirstOrDefault(x =>
			string.Equals(x?.TargetHandle, "S", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(x?.LocalTargetScope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
			?? application.KingdomEffects.FirstOrDefault();
		if (requiresEffectBundle && (sourceEffect == null || !HasAnyTimedPolicyEffect(application)))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyPublishFailedTitle", "Local Policy Publication Failed"), PolicyUiTexts.Get("AFPOL_UI_NoLocalNumericEffects", "No actionable local numeric effects generated; no fees deducted, not enacted."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		if (requiresEffectBundle && !HasExecutablePolicyModuleInstances(application))
		{
			PolicyDebugLog("policy-effect-empty", BuildPolicyRequestLogPrefix(request)
				+ " reason=no-executable-module-instance"
				+ " selectorHandles=" + string.Join(",", (request?.TargetHandles ?? new List<PolicyTargetHandleSaveData>())
					.Where(handle => string.Equals(handle?.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(handle?.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
					.Select(handle => handle.Key)));
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyPublishFailedTitle", "Local Policy Publication Failed"), PolicyUiTexts.Get("AFPOL_UI_PostprocessNoNumericEffects", "Post-processing generated no actionable numeric effects; no fees deducted, no record created."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		string recordId = Guid.NewGuid().ToString("N");
		string feedback = ResolveFeedbackText(result, request);
		string activeEffectId = string.Empty;
		if (requiresEffectBundle
			&& !TryActivatePolicyEffectApplication(request, application, recordId, isRenewal: false, out activeEffectId, out string activationError))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyPublishFailedTitle", "Local Policy Publication Failed"), BuildPolicyFailurePopupText(activationError, result) + PolicyUiTexts.Get("AFPOL_UI_NoFeesNoRecordSuffix", "\n\nNo fees deducted, no record created."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		PolicyPublishCostReceipt costReceipt = new PolicyPublishCostReceipt();
		try
		{
			RecordSuccessfulLocalPolicy(request, result, feedback, application.KingdomEffects, recordId, validFiefs);
			if (!_localPolicyRecords.ContainsKey(recordId))
			{
				throw new InvalidOperationException("Local policy record write failed");
			}
			// Payment is the final throwable business write in the local policy commit.
			DeductPublishCost(request, costReceipt);
		}
		catch (Exception ex)
		{
			PolicyDebugLog("local-publication-commit-failed", BuildPolicyRequestLogPrefix(request), ex.ToString());
			List<string> rollbackFailures = new List<string>();
			try
			{
				if (!TryRefundPublishCost(costReceipt, out string refundError))
				{
					rollbackFailures.Add("Cost refund failed: " + refundError);
				}
			}
			catch (Exception refundException)
			{
				rollbackFailures.Add("Cost refund exception: " + refundException.Message);
			}
			try
			{
				if (!RollbackAndRemovePolicyEffectBundle(activeEffectId, "local-publication-commit-failed", out string rollbackError))
				{
					rollbackFailures.Add("Effect bundle rollback failed: " + rollbackError);
				}
			}
			catch (Exception rollbackException)
			{
				rollbackFailures.Add("Effect bundle rollback exception: " + rollbackException.Message);
			}
			try
			{
				_localPolicyRecords.Remove(recordId);
			}
			catch (Exception recordRollbackException)
			{
				rollbackFailures.Add("Local record rollback failed: " + recordRollbackException.Message);
			}
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_LocalPolicyPublishFailedTitle", "Local Policy Publication Failed"), PolicyUiTexts.Get("AFPOL_UI_PolicyCommitFailed", "Error occurred while submitting policy; effects reverted and no record kept. Technical details written to log.") + (rollbackFailures.Count == 0 ? string.Empty : PolicyUiTexts.Get("AFPOL_UI_PartialRollbackIncomplete", "\nSome rollback steps incomplete; please check the log.")), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		RunLocalPolicyPostCommitStep(recordId, "replacement", () =>
		{
			if (!FinalizePolicyReReviewReplacement(recordId))
			{
				PolicySystemLog.Failure("ReReview", "local-replacement-reconciliation-pending",
					"New local policy committed successfully, but old lineage stop step will retry on save reconciliation.",
					"recordId=" + recordId);
			}
		});
		RunLocalPolicyPostCommitStep(recordId, "lifecycle-memory", () =>
			InvokeLocalPolicyLifecycleMemoryHook("published", recordId, validFiefs.Select(fief => fief.StringId)));
		RunLocalPolicyPostCommitStep(recordId, "trim-records", TrimLocalPolicyRecords);
		RunLocalPolicyPostCommitStep(recordId, "success-popup", () =>
		{
			string impactText = BuildImpactPopupText(request, feedback, application, costDeducted: true);
			ShowPolicySuccessResultPopup("local:" + recordId, impactText);
		});
		RunLocalPolicyPostCommitStep(recordId, "published-log", () =>
			PolicySystemLog.Write("Local", "published", BuildPolicyRecordLogPrefix(request, recordId)
				+ " sourceTargets=" + string.Join(",", validFiefs.Select(fief => fief.StringId))
				+ " effectCount=" + application.KingdomEffects.Count.ToString(CultureInfo.InvariantCulture)
				+ " disposition=" + (result.Postprocess?.Disposition ?? string.Empty)
				+ " duration=" + (sourceEffect?.DurationDays ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " paid=" + request.GoldCost.ToString(CultureInfo.InvariantCulture)));
	}

	private void CompleteVassalPolicyGeneration(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		Kingdom vassalKingdom = VassalageBehavior.GetPlayerDirectVassalKingdomsForExternal()
			.FirstOrDefault(x => x != null && string.Equals(x.StringId ?? "", request?.PlayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase));
		if (!IsPlayerRuler(playerKingdom)
			|| !string.Equals(playerKingdom?.StringId ?? "", request?.IssuerKingdomId ?? "", StringComparison.OrdinalIgnoreCase)
			|| vassalKingdom == null
			|| !VassalageBehavior.TryGetDirectVassalIndependenceStatusForExternal(vassalKingdom.StringId, out int independenceBefore, out int breakawayThreshold, out int rulerRelation, out string rulerName))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_VassalPolicyCancelledTitle", "Vassal Policy Cancelled"), PolicyUiTexts.Get("AFPOL_UI_VassalRelationshipChangedCancelled", "Suzerainty relationship or ruler status changed during evaluation; policy not enacted, independence unchanged, no record created."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		result.Postprocess = BuildPostprocessResultFromMainAssessment(request, result.MainAssessment);
		if (string.IsNullOrWhiteSpace(result.PostprocessRaw))
		{
			result.PostprocessRaw = SafeSerializeForDebug(result.Postprocess);
		}
		bool requiresEffectBundle = string.Equals(
			result.Postprocess?.Disposition,
			"executable",
			StringComparison.Ordinal);
		PolicyApplicationResult application = ApplyPolicyEffects(request, result.Postprocess);
		AppliedKingdomEffect sourceEffect = application.KingdomEffects.FirstOrDefault(x => x != null && string.Equals(x.KingdomId ?? "", vassalKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase));
		if (requiresEffectBundle
			&& (sourceEffect == null
				|| (!sourceEffect.IsPermanentEffect && sourceEffect.DurationDays <= 0)
				|| !HasAnyTimedPolicyEffect(application)))
		{
			InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_VassalPolicyPublishFailedTitle", "Vassal Policy Publication Failed"), PolicyUiTexts.Get("AFPOL_UI_VassalNoValidEffects", "No valid continuous effects targeting the vassal kingdom generated; policy not enacted, independence unchanged."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
			return;
		}
		request.VassalIndependenceBefore = independenceBefore;
		request.VassalPublicationIndependenceCost = MBRandom.RandomInt(VassalageBehavior.VassalPolicyPublicationCostMinimum, VassalageBehavior.VassalPolicyPublicationCostMaximumInclusive + 1);
		request.VassalQualityIndependenceDelta = VassalageBehavior.NormalizeVassalPolicyIndependenceDelta(result.MainAssessment?.VassalIndependenceDelta ?? 0f);
		request.VassalIndependenceReason = result.MainAssessment?.VassalIndependenceReason ?? "";
		string recordId = Guid.NewGuid().ToString("N");
		string feedback = ResolveFeedbackText(result, request);
		int expectedIndependenceAfter = (int)Math.Max(0L, Math.Min(100L,
			(long)independenceBefore
			+ Math.Max(0, request.VassalPublicationIndependenceCost)
			+ request.VassalQualityIndependenceDelta));
		bool expectedBreakaway = expectedIndependenceAfter >= breakawayThreshold;
		request.VassalIndependenceAfter = expectedIndependenceAfter;
		if (!VassalageBehavior.TryPrepareDirectVassalPolicyIndependenceForExternal(
			recordId + ":external",
			request.PlayerKingdomId,
			request.VassalPublicationIndependenceCost,
			request.VassalQualityIndependenceDelta,
			out VassalPolicyExternalCommitPlan externalPlan,
			out string externalPrepareError))
		{
			ShowVassalPolicyFailureBestEffort(recordId,
				PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_004"));
			PolicySystemLog.Failure("VassalPolicy", "external-prepare-failed", externalPrepareError,
				"recordId=" + recordId);
			return;
		}
		string activeEffectId = string.Empty;
		try
		{
			if (requiresEffectBundle
				&& !TryActivatePolicyEffectApplication(request, application, recordId, isRenewal: false, out activeEffectId, out string activationError))
			{
				throw new InvalidOperationException(activationError);
			}
			RecordSuccessfulVassalPolicy(request, result, feedback, application.KingdomEffects, recordId);
			if (!_localPolicyRecords.ContainsKey(recordId))
			{
				throw new InvalidOperationException("Vassal policy local record write failed");
			}
			if (!RegisterUnifiedPlayerPolicy(
				request,
				result,
				feedback,
				application,
				recordId,
				DateTime.UtcNow.Ticks,
				effectsEnded: expectedBreakaway))
			{
				throw new InvalidOperationException("NPC unified policy record write failed");
			}
			PersistVassalExternalCommitState(
				recordId,
				activeEffectId,
				externalPlan,
				"externalCommitPending",
				null,
				string.Empty);
			PolicySystemLog.Transaction(recordId + ":external", recordId, activeEffectId, string.Empty,
				"prepared", "success", stateBefore: "commitPending", stateAfter: "externalCommitPending");
		}
		catch (Exception ex)
		{
			RollbackFailedVassalPolicyPublication(
				recordId,
				activeEffectId,
				application,
				"vassal-publication-registration-failed",
				forceRollbackPending: false,
				out string rollbackDetails);
			ShowVassalPolicyFailureBestEffort(recordId,
				PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_005"));
			RunVassalPolicyPostCommitStep(recordId, "publication-failure-log", () =>
				PolicySystemLog.Write("VassalPolicy", "publication-commit-failed", "recordId=" + recordId
					+ " error=" + ex
					+ (string.IsNullOrWhiteSpace(rollbackDetails) ? string.Empty : " rollback=" + rollbackDetails)));
			return;
		}

		VassalPolicyExternalCommitResult externalResult
			= VassalageBehavior.CommitDirectVassalPolicyIndependenceForExternal(externalPlan, request.PolicyName);
		VassalPolicyExternalCommitObservation observation = externalResult?.Observation
			?? VassalageBehavior.ObserveDirectVassalPolicyIndependenceForExternal(externalPlan);
		bool observable = observation?.Observable == true;
		bool externalChanged = observable && (observation.BreakawayActual
			|| !observation.AgreementPresent
			|| observation.IndependenceActual != externalPlan.IndependenceBefore);
		bool definitelyUnchanged = observable && observation.AgreementMatches
			&& observation.IndependenceActual == externalPlan.IndependenceBefore
			&& !observation.BreakawayActual;
		if (externalResult == null
			|| externalResult.Kind == VassalPolicyExternalCommitResultKind.Unknown
			|| (!observable && externalResult.Kind != VassalPolicyExternalCommitResultKind.Committed))
		{
			PersistVassalExternalCommitState(recordId, activeEffectId, externalPlan,
				"externalCommitPending", observation, externalResult?.Error ?? "external result is not observable");
			PolicySystemLog.Transaction(recordId + ":external", recordId, activeEffectId, string.Empty,
				"externalCommitted", "pending", errorKind: "ObservationUnavailable",
				stateBefore: "externalCommitPending", stateAfter: "externalCommitPending");
			ShowVassalPolicyFailureBestEffort(recordId,
				PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_006"));
			return;
		}
		if ((externalResult.Kind == VassalPolicyExternalCommitResultKind.Unchanged
				|| externalResult.Kind == VassalPolicyExternalCommitResultKind.Conflict)
			&& definitelyUnchanged && !externalChanged)
		{
			RollbackFailedVassalPolicyPublication(
				recordId,
				activeEffectId,
				application,
				"vassal-external-commit-unchanged",
				forceRollbackPending: false,
				out string rollbackDetails);
			ShowVassalPolicyFailureBestEffort(recordId,
				PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_007"));
			PolicySystemLog.Failure("VassalPolicy", "external-commit-unchanged",
				externalResult.Error,
				"recordId=" + recordId + (string.IsNullOrWhiteSpace(rollbackDetails) ? string.Empty : " rollback=" + rollbackDetails));
			return;
		}

		int appliedBefore = externalPlan.IndependenceBefore;
		bool brokeAway = observation.BreakawayActual || !observation.AgreementPresent;
		int independenceAfter = brokeAway
			? externalPlan.IndependenceExpected
			: observation.IndependenceActual;
		string committedState = externalResult.Kind == VassalPolicyExternalCommitResultKind.Committed
			|| externalResult.Kind == VassalPolicyExternalCommitResultKind.AlreadyCommitted
			? "externalCommitted"
			: "externalCommittedReconciled";
		PersistVassalExternalCommitState(recordId, activeEffectId, externalPlan,
			committedState, observation, externalResult.Error);
		PolicySystemLog.Transaction(recordId + ":external", recordId, activeEffectId, string.Empty,
			"externalCommitted", "success", errorKind: externalResult.Kind.ToString(),
			stateBefore: "externalCommitPending", stateAfter: committedState);
		request.VassalIndependenceBefore = appliedBefore;
		request.VassalIndependenceAfter = independenceAfter;
		if (!FinalizePolicyReReviewReplacement(recordId))
		{
			PolicySystemLog.Failure("ReReview", "vassal-replacement-reconciliation-pending",
				"New vassal policy external commit confirmed, but old lineage stop step will retry on save reconciliation.",
				"recordId=" + recordId);
		}
		RunVassalPolicyPostCommitStep(recordId, "presentation", () =>
		{
			if (!NpcRulerPolicyBehavior.TryPublishPlayerPolicyPresentationForExternal(recordId))
			{
				throw new InvalidOperationException("Vassal policy presentation not fully published; persistent retry marker retained");
			}
		});
		bool resultMismatch = appliedBefore != independenceBefore
			|| independenceAfter != expectedIndependenceAfter
			|| brokeAway != expectedBreakaway;
		if (resultMismatch)
		{
			try
			{
				UpdateVassalPolicyIndependenceRecord(recordId, request);
			}
			catch (Exception ex)
			{
				RunVassalPolicyPostCommitStep(recordId, "actual-result-record-update", () =>
					PolicySystemLog.Write("VassalPolicy", "actual-result-record-update-failed", "recordId=" + recordId + " error=" + ex));
			}
			RunVassalPolicyPostCommitStep(recordId, "result-mismatch-log", () =>
				PolicySystemLog.Write("VassalPolicy", "independence-result-mismatch", "recordId=" + recordId
					+ " expected=" + independenceBefore.ToString(CultureInfo.InvariantCulture) + "->" + expectedIndependenceAfter.ToString(CultureInfo.InvariantCulture)
					+ "/" + expectedBreakaway.ToString(CultureInfo.InvariantCulture)
					+ " actual=" + appliedBefore.ToString(CultureInfo.InvariantCulture) + "->" + independenceAfter.ToString(CultureInfo.InvariantCulture)
					+ "/" + brokeAway.ToString(CultureInfo.InvariantCulture)
					+ " resolution=external-state-authoritative"));
		}
		if (brokeAway)
		{
			foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
			{
				effect.RemainingDays = 0;
			}
			TryMarkVassalPolicyRelationshipEndedRecord(
				recordId,
				"vassal_policy_independence_threshold",
				activeEffectId: string.Empty,
				out _);
		}
		RunVassalPolicyPostCommitStep(recordId, "trim-records", TrimLocalPolicyRecords);
		string impactText = BuildImpactPopupText(request, feedback, application, costDeducted: false)
			+ PolicyUiTexts.Format("AFPOL_UI_VassalPublishResultSummary",
				"\n\nIndependence: {BEFORE} + publication cost {COST} + policy modifier {DELTA} = {AFTER}/100\nCurrent breakaway threshold: {THRESHOLD} ({RULER} relation {RELATION})",
				"BEFORE", appliedBefore.ToString(CultureInfo.InvariantCulture),
				"COST", request.VassalPublicationIndependenceCost.ToString(CultureInfo.InvariantCulture),
				"DELTA", FormatSigned(request.VassalQualityIndependenceDelta),
				"AFTER", independenceAfter.ToString(CultureInfo.InvariantCulture),
				"THRESHOLD", breakawayThreshold.ToString(CultureInfo.InvariantCulture),
				"RULER", rulerName,
				"RELATION", FormatSigned(rulerRelation))
			+ (brokeAway ? PolicyUiTexts.Get("AFPOL_UI_VassalBreakawayTerminated", "\nTarget vassal kingdom has reached breakaway threshold; breakaway is irreversible, vassalage and all continuous effects terminated.") : "");
		RunVassalPolicyPostCommitStep(recordId, "success-popup", () =>
			ShowPolicySuccessResultPopup("vassal:" + recordId, impactText));
		RunVassalPolicyPostCommitStep(recordId, "published-log", () =>
			PolicySystemLog.Write("VassalPolicy", "published", BuildPolicyRecordLogPrefix(request, recordId)
				+ " target=" + (request.PlayerKingdomId ?? "")
				+ " independence=" + appliedBefore.ToString(CultureInfo.InvariantCulture) + "->" + independenceAfter.ToString(CultureInfo.InvariantCulture)
				+ " publicationCost=" + request.VassalPublicationIndependenceCost.ToString(CultureInfo.InvariantCulture)
				+ " qualityDelta=" + request.VassalQualityIndependenceDelta.ToString(CultureInfo.InvariantCulture)
				+ " brokeAway=" + brokeAway.ToString(CultureInfo.InvariantCulture)));
	}

	private void PersistVassalExternalCommitState(
		string recordId,
		string activeEffectId,
		VassalPolicyExternalCommitPlan plan,
		string state,
		VassalPolicyExternalCommitObservation observation,
		string error)
	{
		string id = (recordId ?? string.Empty).Trim();
		if (id.Length == 0 || !_localPolicyRecords.TryGetValue(id, out string raw))
		{
			throw new InvalidOperationException("Vassal policy external commit record does not exist");
		}
		LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(
			JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw ?? string.Empty));
		if (record == null)
		{
			throw new InvalidOperationException("Vassal policy external commit record unreadable");
		}
		record.Version = 6;
		record.ActiveEffectId = activeEffectId ?? string.Empty;
		record.ExternalTransactionId = plan?.TransactionId ?? string.Empty;
		record.ExternalAgreementId = plan?.AgreementId ?? string.Empty;
		record.ExternalIdempotencyKey = plan?.IdempotencyKey ?? string.Empty;
		record.ExternalCommitState = state ?? string.Empty;
		record.ExternalInputsCaptured = plan != null;
		record.ExternalPublicationCost = Math.Max(0, plan?.PublicationCost ?? 0);
		record.ExternalQualityDelta = VassalageBehavior.NormalizeVassalPolicyIndependenceDelta(
			plan?.QualityDelta ?? 0);
		record.ExternalIndependenceBefore = plan?.IndependenceBefore ?? 0;
		record.ExternalIndependenceExpected = plan?.IndependenceExpected ?? 0;
		record.ExternalIndependenceActual = observation?.Observable == true
			? observation.IndependenceActual
			: plan?.IndependenceBefore ?? 0;
		record.ExternalBreakawayExpected = plan?.BreakawayExpected == true;
		record.ExternalBreakawayActual = observation?.BreakawayActual == true;
		record.ExternalLastError = error ?? string.Empty;
		_localPolicyRecords[id] = JsonConvert.SerializeObject(record);
	}

	private enum VassalExternalReconciliationAction
	{
		Wait = 0,
		RollbackAfState = 1,
		AcceptExternalState = 2,
		RetryExternalCommit = 3
	}

	private static VassalExternalReconciliationAction ClassifyVassalExternalReconciliationAction(
		bool rollbackPending,
		VassalPolicyExternalCommitObservation observation,
		int independenceBefore,
		int lastAttemptDay,
		int currentDay)
	{
		if (observation?.Observable != true)
		{
			return VassalExternalReconciliationAction.Wait;
		}
		bool changed = observation.BreakawayActual
			|| !observation.AgreementPresent
			|| observation.IndependenceActual != independenceBefore;
		if (changed)
		{
			return VassalExternalReconciliationAction.AcceptExternalState;
		}
		bool unchanged = observation.AgreementMatches
			&& observation.IndependenceActual == independenceBefore
			&& !observation.BreakawayActual;
		if (rollbackPending)
		{
			return unchanged
				? VassalExternalReconciliationAction.RollbackAfState
				: VassalExternalReconciliationAction.Wait;
		}
		return unchanged && lastAttemptDay < currentDay
			? VassalExternalReconciliationAction.RetryExternalCommit
			: VassalExternalReconciliationAction.Wait;
	}

	private static void ResolveVassalExternalMutationInputs(
		bool inputsCaptured,
		int capturedPublicationCost,
		int capturedQualityDelta,
		int independenceBefore,
		int independenceExpected,
		out int publicationCost,
		out int qualityDelta)
	{
		if (inputsCaptured)
		{
			publicationCost = Math.Max(0, capturedPublicationCost);
			qualityDelta = VassalageBehavior.NormalizeVassalPolicyIndependenceDelta(capturedQualityDelta);
			return;
		}
		int frozenDelta = independenceExpected - independenceBefore;
		publicationCost = Math.Max(0, frozenDelta);
		qualityDelta = frozenDelta < 0
			? VassalageBehavior.NormalizeVassalPolicyIndependenceDelta(frozenDelta)
			: 0;
	}

	private void ReconcilePendingVassalExternalCommits(int maxRecords = 8)
	{
		int currentDay = GetCurrentCampaignDay();
		List<LocalPolicyRecordSaveData> pending = _localPolicyRecords.Values
			.Select(raw =>
			{
				try { return NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw ?? string.Empty)); }
				catch { return null; }
			})
			.Where(record => record != null
				&& string.Equals(record.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				&& (string.Equals(record.ExternalCommitState, "externalCommitPending", StringComparison.Ordinal)
					|| (record.EndReason ?? string.Empty).StartsWith(PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal)))
			.OrderBy(record => record.CreatedUtcTicks)
			.Take(Math.Max(1, maxRecords))
			.ToList();
		foreach (LocalPolicyRecordSaveData record in pending)
		{
			bool rollbackPending = (record.EndReason ?? string.Empty).StartsWith(
				PolicyEffectRollbackPendingPrefix, StringComparison.Ordinal);
			int externalBefore = record.ExternalInputsCaptured
				? record.ExternalIndependenceBefore
				: (record.ExternalIndependenceBefore != 0
					? record.ExternalIndependenceBefore : record.IndependenceBefore);
			int externalExpected = record.ExternalInputsCaptured
				? record.ExternalIndependenceExpected
				: (record.ExternalIndependenceExpected != 0
					? record.ExternalIndependenceExpected : record.IndependenceAfter);
			ResolveVassalExternalMutationInputs(
				record.ExternalInputsCaptured,
				record.ExternalPublicationCost,
				record.ExternalQualityDelta,
				externalBefore,
				externalExpected,
				out int externalPublicationCost,
				out int externalQualityDelta);
			VassalPolicyExternalCommitPlan plan = new VassalPolicyExternalCommitPlan
			{
				TransactionId = FirstNonEmpty(record.ExternalTransactionId, record.RecordId + ":external"),
				IdempotencyKey = FirstNonEmpty(record.ExternalIdempotencyKey, record.RecordId + ":external:legacy"),
				AgreementId = FirstNonEmpty(record.ExternalAgreementId,
					VassalageAgreement.BuildAgreementId(record.IssuerKingdomId, record.TargetKingdomId)),
				VassalKingdomId = record.TargetKingdomId ?? string.Empty,
				IndependenceBefore = externalBefore,
				IndependenceExpected = externalExpected,
				BreakawayExpected = record.ExternalBreakawayExpected,
				PublicationCost = externalPublicationCost,
				QualityDelta = externalQualityDelta
			};
			VassalPolicyExternalCommitObservation observation
				= VassalageBehavior.ObserveDirectVassalPolicyIndependenceForExternal(plan);
			VassalExternalReconciliationAction reconciliationAction
				= ClassifyVassalExternalReconciliationAction(
					rollbackPending,
					observation,
					plan.IndependenceBefore,
					record.ExternalLastAttemptDay,
					currentDay);
			if (reconciliationAction == VassalExternalReconciliationAction.RollbackAfState)
			{
				RollbackFailedVassalPolicyPublication(
					record.RecordId, record.ActiveEffectId, null,
					"legacy-vassal-external-unchanged", forceRollbackPending: false, out _);
				continue;
			}
			if (reconciliationAction == VassalExternalReconciliationAction.AcceptExternalState)
			{
				PersistVassalExternalCommitState(record.RecordId, record.ActiveEffectId, plan,
					"externalCommittedReconciled", observation, "reconciled from observed game state");
				PolicySystemLog.Transaction(
					plan.TransactionId,
					record.RecordId,
					record.ActiveEffectId,
					string.Empty,
					"externalCommitted",
					"success",
					errorKind: "ObservedExternalState",
					stateBefore: "externalCommitPending",
					stateAfter: "externalCommittedReconciled");
				if (rollbackPending)
				{
					FinalizeVassalRollbackPendingAfterExternalCommit(
						record.RecordId,
						"observed external game state");
				}
				if (observation.BreakawayActual || !observation.AgreementPresent)
				{
					OnVassalRelationshipEndedInternal(
						plan.VassalKingdomId,
						"vassal external commit reconciled after relationship ended");
				}
				continue;
			}
			if (reconciliationAction != VassalExternalReconciliationAction.RetryExternalCommit)
			{
				continue;
			}
			record.ExternalLastAttemptDay = currentDay;
			record.ExternalCommitAttempts = Math.Min(8, Math.Max(0, record.ExternalCommitAttempts) + 1);
			_localPolicyRecords[record.RecordId] = JsonConvert.SerializeObject(record);
			VassalPolicyExternalCommitResult result
				= VassalageBehavior.CommitDirectVassalPolicyIndependenceForExternal(plan, record.PolicyName);
			VassalPolicyExternalCommitObservation after = result?.Observation
				?? VassalageBehavior.ObserveDirectVassalPolicyIndependenceForExternal(plan);
			if (result != null && (result.Kind == VassalPolicyExternalCommitResultKind.Committed
				|| result.Kind == VassalPolicyExternalCommitResultKind.AlreadyCommitted))
			{
				PersistVassalExternalCommitState(record.RecordId, record.ActiveEffectId, plan,
					"externalCommitted", after, result.Error);
				PolicySystemLog.Transaction(
					plan.TransactionId,
					record.RecordId,
					record.ActiveEffectId,
					string.Empty,
					"externalCommitted",
					"success",
					errorKind: result.Kind.ToString(),
					stateBefore: "externalCommitPending",
					stateAfter: "externalCommitted");
			}
			else
			{
				PersistVassalExternalCommitState(record.RecordId, record.ActiveEffectId, plan,
					"externalCommitPending", after, result?.Error ?? "external retry result unavailable");
				PolicySystemLog.Transaction(
					plan.TransactionId,
					record.RecordId,
					record.ActiveEffectId,
					string.Empty,
					"externalCommitted",
					"pending",
					errorKind: result?.Kind.ToString() ?? "ObservationUnavailable",
					stateBefore: "externalCommitPending",
					stateAfter: "externalCommitPending");
			}
		}
	}

	private void FinalizeVassalRollbackPendingAfterExternalCommit(string recordId, string reason)
	{
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		if (normalizedRecordId.Length == 0
			|| !_localPolicyRecords.TryGetValue(normalizedRecordId, out string raw))
		{
			return;
		}
		LocalPolicyRecordSaveData record;
		try
		{
			record = NormalizeLocalPolicyRecord(
				JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw ?? string.Empty));
		}
		catch
		{
			return;
		}
		if (record == null
			|| !(record.EndReason ?? string.Empty).StartsWith(
				PolicyEffectRollbackPendingPrefix,
				StringComparison.Ordinal))
		{
			return;
		}
		record.Status = LocalPolicyStatusRelationshipEnded;
		record.RemainingDays = 0;
		record.EndReason = "externalCommittedReconciled:" + FirstNonEmpty(
			reason,
			"observed external state");
		_localPolicyRecords[normalizedRecordId] = JsonConvert.SerializeObject(record);
	}

	private void RollbackFailedVassalPolicyPublication(
		string recordId,
		string activeEffectId,
		PolicyApplicationResult application,
		string reason,
		bool forceRollbackPending,
		out string rollbackDetails)
	{
		List<string> failures = new List<string>();
		try
		{
			if (NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(recordId, out _)
				&& !NpcRulerPolicyBehavior.UnregisterPlayerPolicyForExternal(recordId))
			{
				failures.Add("NPC unified record rollback failed");
			}
		}
		catch (Exception ex)
		{
			failures.Add("NPC unified record rollback exception: " + ex.Message);
		}
		bool bundleRolledBack = true;
		try
		{
			bundleRolledBack = RollbackAndRemovePolicyEffectBundle(activeEffectId, reason, out string rollbackError);
			if (!bundleRolledBack)
			{
				failures.Add("Effect bundle rollback failed: " + rollbackError);
			}
		}
		catch (Exception ex)
		{
			bundleRolledBack = false;
			failures.Add("Effect bundle rollback exception: " + ex.Message);
		}
		foreach (AppliedKingdomEffect effect in application?.KingdomEffects?.Where(x => x != null) ?? Enumerable.Empty<AppliedKingdomEffect>())
		{
			effect.RemainingDays = 0;
		}
		bool rollbackPending = forceRollbackPending || failures.Count > 0;
		try
		{
			if (rollbackPending)
			{
				PersistVassalPolicyRollbackPendingRecord(
					recordId,
					bundleRolledBack ? string.Empty : activeEffectId,
					reason,
					failures);
			}
			else
			{
				_localPolicyRecords.Remove(recordId ?? string.Empty);
			}
		}
		catch (Exception ex)
		{
			failures.Add("Local record rollback/suspend failed: " + ex.Message);
			rollbackPending = true;
		}
		rollbackDetails = string.Join(" | ", failures);
		try
		{
			PolicySystemLog.Write("VassalPolicy", "publication-rolled-back", "recordId=" + (recordId ?? string.Empty)
				+ " reason=" + (reason ?? string.Empty)
				+ " rollbackPending=" + rollbackPending.ToString(CultureInfo.InvariantCulture)
				+ (string.IsNullOrWhiteSpace(rollbackDetails) ? string.Empty : " error=" + rollbackDetails));
		}
		catch
		{
		}
	}

	private bool TryMarkVassalPolicyRelationshipEndedRecord(
		string recordId,
		string reason,
		string activeEffectId,
		out string error)
	{
		error = string.Empty;
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		if (normalizedRecordId.Length == 0
			|| !_localPolicyRecords.TryGetValue(normalizedRecordId, out string raw))
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryMarkVassalPolicyRelationshipEndedRecord_000");
			return false;
		}
		LocalPolicyRecordSaveData record;
		try
		{
			record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
		}
		catch (Exception ex)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryMarkVassalPolicyRelationshipEndedRecord_001") + ex.Message;
			return false;
		}
		if (record == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryMarkVassalPolicyRelationshipEndedRecord_002");
			return false;
		}
		record.Status = LocalPolicyStatusRelationshipEnded;
		record.EndReason = FirstNonEmpty(reason, PolicyCompatibilityTexts.Get("Legacy_MapPolicyEndReasonToDisplay_126"));
		record.RemainingDays = 0;
		record.ActiveEffectId = activeEffectId ?? string.Empty;
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
		{
			if (effect == null)
			{
				continue;
			}
			effect.RemainingDays = 0;
			effect.ActiveEffectId = activeEffectId ?? string.Empty;
		}
		_localPolicyRecords[normalizedRecordId] = JsonConvert.SerializeObject(record);
		return true;
	}

	private void PersistVassalPolicyRollbackPendingRecord(
		string recordId,
		string activeEffectId,
		string reason,
		IEnumerable<string> failures)
	{
		string normalizedRecordId = (recordId ?? string.Empty).Trim();
		LocalPolicyRecordSaveData record = null;
		if (normalizedRecordId.Length > 0 && _localPolicyRecords.TryGetValue(normalizedRecordId, out string raw))
		{
			try
			{
				record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
			}
			catch
			{
				record = null;
			}
		}
		record = record ?? new LocalPolicyRecordSaveData
		{
			Version = 6,
			ScopeKind = PolicyScopeVassal,
			RecordId = normalizedRecordId,
			SubmittedDay = GetCurrentCampaignDay(),
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		record.Status = LocalPolicyStatusRelationshipEnded;
		record.EndReason = PolicyEffectRollbackPendingPrefix + FirstNonEmpty(reason, "vassal-publication-rollback")
			+ ((failures ?? Enumerable.Empty<string>()).Any()
				? ":" + string.Join(" | ", failures.Where(value => !string.IsNullOrWhiteSpace(value)))
				: string.Empty);
		record.RemainingDays = 0;
		record.ActiveEffectId = activeEffectId ?? string.Empty;
		foreach (LocalPolicyEffectRecordSaveData effect in record.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
		{
			if (effect == null)
			{
				continue;
			}
			effect.RemainingDays = 0;
			effect.ActiveEffectId = activeEffectId ?? string.Empty;
		}
		_localPolicyRecords[normalizedRecordId] = JsonConvert.SerializeObject(record);
	}

	private static void ShowVassalPolicyFailureBestEffort(string recordId, string message)
	{
		try
		{
			InformationManager.ShowInquiry(new InquiryData(
				PolicyUiTexts.Get("AFPOL_UI_VassalPolicyPublishFailedTitle", "Vassal Policy Publication Failed"),
				BuildPolicyFailurePopupText(message ?? PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_008"), null),
				true,
				false,
				PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"),
				string.Empty,
				null,
				null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			try
			{
				PolicySystemLog.Write("VassalPolicy", "failure-popup-failed", "recordId=" + (recordId ?? string.Empty) + " error=" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void RunVassalPolicyPostCommitStep(string recordId, string stage, Action action)
	{
		RunPolicyPostCommitStep("VassalPolicy", "post-commit-step-failed", recordId, stage, action);
	}

	private static void RunLocalPolicyPostCommitStep(string recordId, string stage, Action action)
	{
		RunPolicyPostCommitStep("Local", "post-commit-step-failed", recordId, stage, action);
	}

	private static void RunPolicyPostCommitStep(
		string logScope,
		string failureEvent,
		string recordId,
		string stage,
		Action action)
	{
		try
		{
			action?.Invoke();
		}
		catch (Exception ex)
		{
			try
			{
				PolicySystemLog.Write(logScope ?? "Policy", failureEvent ?? "post-commit-step-failed",
					"recordId=" + (recordId ?? string.Empty)
					+ " stage=" + (stage ?? string.Empty)
					+ " error=" + ex);
			}
			catch
			{
			}
		}
	}

	private static void PrepareApprovedPlayerPolicyCost(PolicyDraftRequest request, PolicyMainAssessmentResult assessment)
	{
		if (request.UseAiEvaluatedCost)
		{
			if (!TryPreparePolicyCostForApplication(request, assessment, out string error))
			{
				throw new InvalidOperationException(error);
			}
			return;
		}
		int requiredGold = Math.Max(0, request.GoldCost);
		int availableGold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		if (availableGold < requiredGold)
		{
			throw new InvalidOperationException(
				"Insufficient denars when policy agenda approved; required "
				+ requiredGold.ToString(CultureInfo.InvariantCulture)
				+ ", available "
				+ availableGold.ToString(CultureInfo.InvariantCulture));
		}
		int actualGold = requiredGold;
		request.RequiredGoldCost = requiredGold;
		request.DailyMaintenanceGoldCost = 0;
		request.RequiredInfluenceCost = 0f;
		request.GoldCost = actualGold;
		request.InfluenceCost = 0f;
		request.GoldEffectScale = 1f;
		request.InfluenceEffectScale = request.GoldEffectScale;
	}

	private static bool TryBuildPendingPlayerPolicyAgendaSaveData(
		PolicyDraftRequest request,
		PolicyMainAssessmentResult assessment,
		PolicyPostprocessResult postprocess,
		string feedback,
		out PendingPlayerPolicyAgendaSaveData pending,
		out string error)
	{
		pending = null;
		error = string.Empty;
		if (request == null || assessment == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyAgendaSaveData_003");
			return false;
		}
		string scope = request.ScopeKind ?? PolicyScopeKingdom;
		List<string> candidateModuleIds = NormalizePlayerPolicyAgendaModuleAllowlist(
			request.SelectedEffectModuleIds,
			scope);
		List<string> detailedModuleIds = NormalizePlayerPolicyAgendaModuleAllowlist(
			request.SelectedEffectModuleIds,
			scope);
		if (!PolicyEffectModuleCatalog.TryCreateAuthorization(
			candidateModuleIds,
			scope,
			out PolicyEffectModuleAuthorization candidateAuthorization,
			out error))
		{
			return false;
		}
		HashSet<string> candidateSet = new HashSet<string>(candidateAuthorization.SourceModuleIds, StringComparer.Ordinal);
		if (candidateModuleIds.Count > MaxCompiledPolicyEffectInstances
			|| detailedModuleIds.Count > DuelSettings.PlayerPolicyEffectModuleEffectiveDetailCountMaximum
			|| detailedModuleIds.Any(moduleId => !candidateSet.Contains(moduleId)))
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyAgendaSaveData_004");
			return false;
		}
		if (!TryBuildPendingPlayerPolicyModuleEffects(
			postprocess?.Effects,
			out List<PolicyEffectInstanceSaveData> moduleEffects,
			out error))
		{
			return false;
		}
		bool hasMissingInstance = moduleEffects.Any(instance => instance == null);
		PolicyEffectInstanceSaveData unauthorizedInstance = moduleEffects.FirstOrDefault(instance =>
			instance != null
			&& !candidateAuthorization.IsAuthorized(instance.SourceModuleId, instance.ModuleId));
		if (hasMissingInstance || unauthorizedInstance != null)
		{
			error = hasMissingInstance
				? PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyAgendaSaveData_005")
				: PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyAgendaSaveData_006")
					+ unauthorizedInstance.SourceModuleId + " -> " + unauthorizedInstance.ModuleId;
			return false;
		}
		pending = new PendingPlayerPolicyAgendaSaveData
		{
			Version = 5,
			Request = request,
			Assessment = ClonePlayerPolicyAgendaAssessmentWithoutEffects(assessment),
			ModuleEffects = moduleEffects,
			CandidateModuleIds = candidateModuleIds,
			DetailedModuleIds = detailedModuleIds,
			ObjectSnapshot = BuildPolicyEffectObjectSnapshot(moduleEffects, scope),
			Feedback = feedback ?? string.Empty
		};
		return true;
	}

	private static bool TryBuildPendingPlayerPolicyModuleEffects(
		IEnumerable<PolicyEffectDto> compiledEffects,
		out List<PolicyEffectInstanceSaveData> moduleEffects,
		out string error)
	{
		moduleEffects = new List<PolicyEffectInstanceSaveData>();
		error = string.Empty;
		List<PolicyEffectInstanceSaveData> shells = new List<PolicyEffectInstanceSaveData>();
		foreach (PolicyEffectDto effect in compiledEffects ?? Enumerable.Empty<PolicyEffectDto>())
		{
			if (effect == null || string.IsNullOrWhiteSpace(effect.ModuleId))
			{
				continue;
			}
			if (effect.Payload == null
				|| effect.Payload.Type == JTokenType.Null
				|| effect.PreparedModuleEffect?.Instance == null
				|| effect.PreparedModuleEffect.Descriptor == null
				|| effect.Targets != null
				|| effect.Changes != null
				|| HasLegacyPolicyEffectShape(effect))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_007");
				return false;
			}
			List<PolicyEffectInstanceSaveData> savedShells = CreatePolicyEffectSaveDataList(effect.PreparedModuleEffect);
			if (savedShells.Count != 1)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_008");
				return false;
			}
			PolicyEffectInstanceSaveData shell = savedShells[0];
			if (!string.Equals(shell.ModuleId ?? string.Empty, effect.ModuleId.Trim(), StringComparison.Ordinal))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_009");
				return false;
			}
			shell.PayloadSchemaVersion = effect.PreparedModuleEffect.Descriptor.PayloadSchemaVersion;
			shell.Payload = effect.Payload.DeepClone();
			shell.TargetSet = NormalizePolicyEffectCanonicalTargetSet(shell.TargetSet);
			shell.LifecycleState = PolicyEffectLifecycleState.Prepared;
			shell.StateSchemaVersion = effect.PreparedModuleEffect.Descriptor.RuntimeStateSchemaVersion;
			shell.RuntimeState = null;
			shell.ExecutionReceipt = null;
			shells.Add(shell);
		}
		if (!TryCoalescePolicyEffectShellInstances(shells, out moduleEffects, out error))
		{
			return false;
		}
		if (!TryValidateOrFreezeMissingPendingMechanismContracts(moduleEffects, out error))
		{
			return false;
		}
		if (moduleEffects.Count > MaxCompiledPolicyEffectInstances)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_010") + MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_011");
			return false;
		}
		foreach (PolicyEffectInstanceSaveData instance in moduleEffects)
		{
			if (!PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out string normalizeError)
				|| normalized?.IsInert == true
				|| normalized?.SaveData == null)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildPendingPlayerPolicyModuleEffects_012") + FirstNonEmpty(normalizeError, normalized?.InertReason);
				return false;
			}
			instance.RuntimeState = null;
			instance.ExecutionReceipt = null;
			instance.LifecycleState = PolicyEffectLifecycleState.Prepared;
		}
		return true;
	}

	private static List<string> NormalizePlayerPolicyAgendaModuleAllowlist(
		IEnumerable<string> moduleIds,
		string scope)
	{
		List<string> normalized = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (string requestedId in moduleIds ?? Enumerable.Empty<string>())
		{
			if (PolicyEffectModuleCatalog.TryGet(requestedId, out IPolicyEffectModule module)
				&& PolicyEffectModuleCatalog.IsAllowedForScope(module, scope)
				&& seen.Add(module.Id))
			{
				normalized.Add(module.Id);
			}
		}
		return normalized;
	}

	private static string BuildPendingPlayerPolicyAgendaDiagnosticSnapshot(
		PolicyDraftRequest request,
		PolicyMainAssessmentResult assessment,
		PolicyPostprocessResult postprocess,
		string failure)
	{
		try
		{
			string scope = request?.ScopeKind ?? PolicyScopeKingdom;
			List<string> rawCandidateIds = (request?.CandidateEffectModuleIds ?? new List<string>()).ToList();
			List<string> rawDetailedIds = (request?.SelectedEffectModuleIds ?? new List<string>()).ToList();
			List<string> normalizedCandidateIds = NormalizePlayerPolicyAgendaModuleAllowlist(rawCandidateIds, scope);
			List<string> normalizedDetailedIds = NormalizePlayerPolicyAgendaModuleAllowlist(rawDetailedIds, scope);
			PolicyEffectModuleCatalog.TryCreateAuthorization(
				normalizedCandidateIds,
				scope,
				out PolicyEffectModuleAuthorization candidateAuthorization,
				out string authorizationError);
			bool moduleBuildSucceeded = TryBuildPendingPlayerPolicyModuleEffects(
				postprocess?.Effects,
				out List<PolicyEffectInstanceSaveData> moduleEffects,
				out string moduleBuildError);
			moduleEffects ??= new List<PolicyEffectInstanceSaveData>();
			List<string> compiledModuleIds = moduleEffects
				.Where(instance => instance != null)
				.Select(instance => instance.ModuleId ?? string.Empty)
				.Where(moduleId => moduleId.Length > 0)
				.Distinct(StringComparer.Ordinal)
				.ToList();
			List<string> outsideCandidateIds = moduleEffects
				.Where(instance => instance == null
					|| candidateAuthorization == null
					|| !candidateAuthorization.IsAuthorized(instance.SourceModuleId, instance.ModuleId))
				.Select(instance => (instance?.SourceModuleId ?? "(missing-source)")
					+ " -> " + (instance?.ModuleId ?? "(missing-runtime)"))
				.Distinct(StringComparer.Ordinal)
				.ToList();

			JObject root = new JObject
			{
				["diagnosticSchemaVersion"] = 2,
				["capturedUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
				["stage"] = "pending-player-policy-agenda-snapshot",
				["failure"] = failure ?? string.Empty,
				["request"] = new JObject
				{
					["requestId"] = request?.RequestId ?? string.Empty,
					["scopeKind"] = scope,
					["policyNameHash"] = PolicyTextEmbeddingSession.StableTextHash(request?.PolicyName),
					["policyContentChars"] = request?.PolicyContent?.Length ?? 0,
					["policyContentHash"] = PolicyTextEmbeddingSession.StableTextHash(request?.PolicyContent),
					["issuerKingdomId"] = request?.IssuerKingdomId ?? string.Empty,
					["issuerKingdomName"] = request?.IssuerKingdomName ?? string.Empty,
					["playerKingdomId"] = request?.PlayerKingdomId ?? string.Empty,
					["playerKingdomName"] = request?.PlayerKingdomName ?? string.Empty,
					["proposerClanId"] = request?.ProposerClanId ?? string.Empty,
					["submittedDay"] = request?.SubmittedDay ?? 0,
					["manualDurationDays"] = request?.ManualDurationDays ?? 0,
					["isPermanentEffect"] = request?.IsPermanentEffect == true,
					["targetHandleCount"] = request?.TargetHandles?.Count ?? 0,
					["targetHandleHash"] = PolicyTextEmbeddingSession.StableTextHash(JsonConvert.SerializeObject(
						request?.TargetHandles ?? new List<PolicyTargetHandleSaveData>(), Formatting.None)),
					["selectedFiefCount"] = request?.SelectedFiefIds?.Count ?? 0,
					["selectedFiefHash"] = PolicyTextEmbeddingSession.StableTextHash(string.Join(",", request?.SelectedFiefIds ?? new List<string>())),
					["mentionedClanCount"] = request?.LocalMentionedClanIds?.Count ?? 0,
					["mentionedSettlementCount"] = request?.LocalMentionedSettlementIds?.Count ?? 0
				},
				["assessment"] = new JObject
				{
					["effectDurationMode"] = assessment?.EffectDurationMode ?? string.Empty,
					["durationDays"] = assessment?.DurationDays,
					["startupGoldCost"] = assessment?.StartupGoldCost,
					["dailyMaintenanceGoldCost"] = assessment?.DailyMaintenanceGoldCost,
					["requiredInfluenceCost"] = assessment?.RequiredInfluenceCost,
					["numericIntentHash"] = PolicyTextEmbeddingSession.StableTextHash(assessment?.NumericIntent),
					["numericIntentChars"] = assessment?.NumericIntent?.Length ?? 0
				},
				["moduleCatalog"] = new JObject
				{
					["comparisonRule"] = "sourceModuleId must be frozen; runtime moduleId must be a catalog-declared descendant of that source",
					["authorizationError"] = authorizationError ?? string.Empty,
					["rawCandidateModuleIds"] = JToken.FromObject(rawCandidateIds),
					["normalizedCandidateModuleIds"] = JToken.FromObject(normalizedCandidateIds),
					["rawDetailedModuleIds"] = JToken.FromObject(rawDetailedIds),
					["normalizedDetailedModuleIds"] = JToken.FromObject(normalizedDetailedIds),
					["candidateResolution"] = new JArray(rawCandidateIds.Select(moduleId =>
						BuildPolicyEffectModuleResolutionDiagnostic(moduleId, scope))),
					["detailedResolution"] = new JArray(rawDetailedIds.Select(moduleId =>
						BuildPolicyEffectModuleResolutionDiagnostic(moduleId, scope)))
				},
				["postprocess"] = new JObject
				{
					["effectPlanVersion"] = postprocess?.EffectPlanVersion,
					["durationDays"] = postprocess?.DurationDays,
					["impactSummaryHash"] = PolicyTextEmbeddingSession.StableTextHash(postprocess?.ImpactSummary),
					["impactSummaryChars"] = postprocess?.ImpactSummary?.Length ?? 0,
					["effectCount"] = postprocess?.Effects?.Count ?? 0,
					["effectHash"] = PolicyTextEmbeddingSession.StableTextHash(JsonConvert.SerializeObject(
						postprocess?.Effects ?? new List<PolicyEffectDto>(), Formatting.None))
				},
				["canonicalBuild"] = new JObject
				{
					["succeeded"] = moduleBuildSucceeded,
					["error"] = moduleBuildError ?? string.Empty,
					["compiledModuleIds"] = JToken.FromObject(compiledModuleIds),
					["outsideCandidateModuleIds"] = JToken.FromObject(outsideCandidateIds),
					["moduleEffectCount"] = moduleEffects.Count,
					["moduleEffectHash"] = PolicyTextEmbeddingSession.StableTextHash(JsonConvert.SerializeObject(
						moduleEffects, Formatting.None))
				},
				["resolvedGameObjectsHash"] = PolicyTextEmbeddingSession.StableTextHash(JsonConvert.SerializeObject(
					BuildPolicyEffectObjectSnapshot(moduleEffects, scope), Formatting.None))
			};
			return root.ToString(Formatting.Indented);
		}
		catch (Exception ex)
		{
			return new JObject
			{
				["diagnosticSchemaVersion"] = 2,
				["stage"] = "pending-player-policy-agenda-snapshot",
				["failure"] = failure ?? string.Empty,
				["diagnosticBuildFailureType"] = ex.GetType().Name,
				["diagnosticBuildFailureHash"] = PolicyTextEmbeddingSession.StableTextHash(ex.Message),
				["requestId"] = request?.RequestId ?? string.Empty
			}.ToString(Formatting.Indented);
		}
	}

	private static JArray BuildPolicyPostprocessEffectDiagnosticArray(
		IEnumerable<PolicyEffectDto> effects,
		string scope)
	{
		JArray result = new JArray();
		int index = 0;
		foreach (PolicyEffectDto effect in effects ?? Enumerable.Empty<PolicyEffectDto>())
		{
			PolicyEffectPreparedInstance prepared = effect?.PreparedModuleEffect;
			result.Add(new JObject
			{
				["index"] = index++,
				["sourceModuleId"] = FirstNonEmpty(effect?.SourceModuleId, effect?.ModuleId),
				["runtimeModuleId"] = effect?.ModuleId ?? string.Empty,
				["sourceModuleResolution"] = BuildPolicyEffectModuleResolutionDiagnostic(
					FirstNonEmpty(effect?.SourceModuleId, effect?.ModuleId), scope),
				["mechanismId"] = effect?.MechanismId ?? string.Empty,
				["mechanismKind"] = effect?.MechanismKind.ToString() ?? string.Empty,
				["mechanismRole"] = effect?.MechanismRole.ToString() ?? string.Empty,
				["sourceOmitted"] = effect?.SourceOmitted == true,
				["destinationOmitted"] = effect?.DestinationOmitted == true,
				["targetHandles"] = JToken.FromObject(effect?.TargetHandles ?? new List<string>()),
				["payload"] = effect?.Payload?.DeepClone() ?? JValue.CreateNull(),
				["reason"] = effect?.Reason ?? string.Empty,
				["preparedDescriptor"] = BuildPolicyEffectModuleDescriptorDiagnostic(prepared?.Descriptor),
				["preparedInstance"] = prepared?.Instance == null
					? JValue.CreateNull()
					: JToken.FromObject(prepared.Instance)
			});
		}
		return result;
	}

	private static JObject BuildPolicyEffectModuleResolutionDiagnostic(string requestedModuleId, string scope)
	{
		string requested = (requestedModuleId ?? string.Empty).Trim();
		bool found = PolicyEffectModuleCatalog.TryGet(requested, out IPolicyEffectModule module);
		return new JObject
		{
			["requestedModuleId"] = requested,
			["found"] = found,
			["canonicalModuleId"] = module?.Id ?? string.Empty,
			["allowedForScope"] = found && PolicyEffectModuleCatalog.IsAllowedForScope(module, scope),
			["descriptor"] = BuildPolicyEffectModuleDescriptorDiagnostic(module?.Descriptor)
		};
	}

	private static JObject BuildPolicyEffectModuleDescriptorDiagnostic(PolicyEffectModuleDescriptor descriptor)
	{
		if (descriptor == null)
		{
			return null;
		}
		return new JObject
		{
			["id"] = descriptor.Id ?? string.Empty,
			["displayGroup"] = descriptor.DisplayGroup ?? string.Empty,
			["playerDisplayName"] = descriptor.PlayerDisplayName ?? string.Empty,
			["family"] = descriptor.Family.ToString(),
			["executionKind"] = descriptor.ExecutionKind.ToString(),
			["hook"] = descriptor.Hook.ToString(),
			["aggregation"] = descriptor.Aggregation.ToString(),
			["valueUnit"] = descriptor.ValueUnit.ToString(),
			["fundingMode"] = descriptor.FundingMode.ToString(),
			["fundingStrategy"] = descriptor.FundingStrategy.ToString(),
			["payloadSchemaVersion"] = descriptor.PayloadSchemaVersion,
			["runtimeStateSchemaVersion"] = descriptor.RuntimeStateSchemaVersion,
			["promptVisible"] = descriptor.PromptVisible,
			["supportsRollback"] = descriptor.SupportsRollback,
			["supportsIdempotency"] = descriptor.SupportsIdempotency,
			["allowedScopes"] = JToken.FromObject(descriptor.AllowedScopes ?? Array.Empty<string>()),
			["allowedSelectorKinds"] = JToken.FromObject(descriptor.AllowedSelectorKinds ?? Array.Empty<PolicyEffectTargetKind>()),
			["targetKinds"] = JToken.FromObject(descriptor.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>()),
			["targetProjection"] = descriptor.TargetProjection.ToString(),
			["targetRefresh"] = descriptor.TargetRefresh.ToString(),
			["allowIndependentClanTargets"] = descriptor.AllowIndependentClanTargets,
			["allowCrossKingdomTargets"] = descriptor.AllowCrossKingdomTargets,
			["authorizedRuntimeModuleIds"] = JToken.FromObject(
				PolicyEffectModuleCatalog.GetAuthorizedRuntimeModuleIds(descriptor.Id))
		};
	}

	private static JObject BuildPolicyEffectModuleComparisonDiagnostic(
		PolicyEffectInstanceSaveData instance,
		PolicyEffectModuleAuthorization candidateAuthorization,
		string scope)
	{
		string sourceModuleId = instance?.SourceModuleId ?? string.Empty;
		string compiledModuleId = instance?.ModuleId ?? string.Empty;
		return new JObject
		{
			["sourceModuleId"] = sourceModuleId,
			["compiledModuleId"] = compiledModuleId,
			["sourceCandidateMatch"] = candidateAuthorization?.ContainsSource(sourceModuleId) == true,
			["authorizedDescendant"] = candidateAuthorization?.IsAuthorized(sourceModuleId, compiledModuleId) == true,
			["declaredRuntimeModuleIds"] = JToken.FromObject(
				PolicyEffectModuleCatalog.GetAuthorizedRuntimeModuleIds(sourceModuleId)),
			["sourceModuleResolution"] = BuildPolicyEffectModuleResolutionDiagnostic(sourceModuleId, scope),
			["compiledModuleResolution"] = BuildPolicyEffectModuleResolutionDiagnostic(compiledModuleId, scope),
			["payload"] = instance?.Payload?.DeepClone() ?? JValue.CreateNull(),
			["targetSet"] = instance?.TargetSet == null ? JValue.CreateNull() : JToken.FromObject(instance.TargetSet)
		};
	}

	private sealed class PolicyEffectObjectSnapshotLookup
	{
		internal Dictionary<string, Kingdom> Kingdoms { get; } = new Dictionary<string, Kingdom>(StringComparer.OrdinalIgnoreCase);
		internal Dictionary<string, Clan> Clans { get; } = new Dictionary<string, Clan>(StringComparer.OrdinalIgnoreCase);
		internal Dictionary<string, Settlement> Settlements { get; } = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);
		internal Dictionary<string, Hero> Heroes { get; } = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
		internal Dictionary<string, MobileParty> Parties { get; } = new Dictionary<string, MobileParty>(StringComparer.OrdinalIgnoreCase);
	}

	private static JObject BuildPolicyEffectObjectSnapshot(
		IEnumerable<PolicyEffectInstanceSaveData> sourceInstances,
		string scope)
	{
		List<PolicyEffectInstanceSaveData> instances = (sourceInstances ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		PolicyEffectObjectSnapshotLookup lookup = BuildPolicyEffectObjectSnapshotLookup();
		HashSet<string> kingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> clanIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> settlementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> heroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> partyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		JArray effectBindings = new JArray();

		foreach (PolicyEffectInstanceSaveData instance in instances)
		{
			PolicyEffectCanonicalTargetSet targetSet = instance.TargetSet ?? new PolicyEffectCanonicalTargetSet();
			AddSnapshotIds(kingdomIds, targetSet.KingdomIds);
			AddSnapshotIds(clanIds, targetSet.ClanIds);
			AddSnapshotIds(settlementIds, targetSet.SettlementIds);
			AddSnapshotIds(settlementIds, targetSet.TownIds);
			AddSnapshotIds(settlementIds, targetSet.VillageIds);
			AddSnapshotIds(settlementIds, targetSet.ParentSettlementIds);
			AddSnapshotIds(heroIds, targetSet.HeroIds);
			AddSnapshotId(heroIds, instance.ActorHeroId);

			string sourceModuleId = FirstNonEmpty(instance.SourceModuleId, instance.ModuleId);
			PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule runtimeModule);
			string effectDescription = string.Empty;
			if (runtimeModule?.Descriptor != null
				&& PolicyEffectModuleCatalog.TryDeserializePayload(
					runtimeModule.Id,
					instance.Payload,
					instance.PayloadSchemaVersion,
					out PolicyEffectPayload typedPayload,
					out _))
			{
				effectDescription = runtimeModule.DescribePayload(typedPayload) ?? string.Empty;
			}
			effectBindings.Add(new JObject
			{
				["instanceId"] = instance.InstanceId ?? string.Empty,
				["policyId"] = instance.PolicyId ?? string.Empty,
				["sourceScope"] = instance.SourceScope ?? scope ?? string.Empty,
				["sourceModuleId"] = sourceModuleId,
				["runtimeModuleId"] = instance.ModuleId ?? string.Empty,
				["lineageAuthorized"] = PolicyEffectModuleCatalog.IsAuthorizedRuntimeModule(sourceModuleId, instance.ModuleId),
				["actorHeroId"] = instance.ActorHeroId ?? string.Empty,
				["executionKind"] = runtimeModule?.Descriptor?.ExecutionKind.ToString() ?? string.Empty,
				["hook"] = runtimeModule?.Descriptor?.Hook.ToString() ?? string.Empty,
				["valueUnit"] = runtimeModule?.Descriptor?.ValueUnit.ToString() ?? string.Empty,
				["effectDescription"] = effectDescription,
				["effectivePayload"] = instance.Payload?.DeepClone() ?? JValue.CreateNull(),
				["targetSet"] = JToken.FromObject(targetSet),
				["currentTargetValues"] = BuildPolicyEffectCurrentTargetValues(instance, runtimeModule, lookup)
			});
		}

		foreach (string kingdomId in kingdomIds.ToArray())
		{
			if (lookup.Kingdoms.TryGetValue(kingdomId, out Kingdom kingdom))
			{
				AddSnapshotId(heroIds, kingdom.Leader?.StringId);
				AddSnapshotId(clanIds, kingdom.Leader?.Clan?.StringId);
			}
		}
		foreach (string clanId in clanIds.ToArray())
		{
			if (lookup.Clans.TryGetValue(clanId, out Clan clan))
			{
				AddSnapshotId(heroIds, clan.Leader?.StringId);
				AddSnapshotId(kingdomIds, clan.Kingdom?.StringId);
			}
		}
		foreach (string settlementId in settlementIds.ToArray())
		{
			if (lookup.Settlements.TryGetValue(settlementId, out Settlement settlement))
			{
				AddSnapshotId(clanIds, settlement.OwnerClan?.StringId);
				AddSnapshotId(kingdomIds, settlement.OwnerClan?.Kingdom?.StringId);
			}
		}
		foreach (string heroId in heroIds.ToArray())
		{
			if (lookup.Heroes.TryGetValue(heroId, out Hero hero))
			{
				AddSnapshotId(clanIds, hero.Clan?.StringId);
				AddSnapshotId(kingdomIds, hero.Clan?.Kingdom?.StringId);
				AddSnapshotId(partyIds, hero.PartyBelongedTo?.StringId);
				AddSnapshotId(settlementIds, hero.CurrentSettlement?.StringId);
			}
		}

		return new JObject
		{
			["snapshotSchemaVersion"] = 1,
			["capturedUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
			["capturedCampaignDay"] = GetCurrentCampaignDay(),
			["identityRule"] = "objectType + StringId; display names are diagnostic only",
			["effectBindings"] = effectBindings,
			["heroes"] = new JArray(heroIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.Select(id => BuildPolicyHeroObjectSnapshot(id, lookup))),
			["clans"] = new JArray(clanIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.Select(id => BuildPolicyClanObjectSnapshot(id, lookup))),
			["kingdoms"] = new JArray(kingdomIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.Select(id => BuildPolicyKingdomObjectSnapshot(id, lookup))),
			["settlements"] = new JArray(settlementIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.Select(id => BuildPolicySettlementObjectSnapshot(id, lookup))),
			["parties"] = new JArray(partyIds.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
				.Select(id => BuildPolicyPartyObjectSnapshot(id, lookup)))
		};
	}

	private static PolicyEffectObjectSnapshotLookup BuildPolicyEffectObjectSnapshotLookup()
	{
		PolicyEffectObjectSnapshotLookup lookup = new PolicyEffectObjectSnapshotLookup();
		foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
		{
			AddSnapshotObject(lookup.Kingdoms, kingdom?.StringId, kingdom);
		}
		foreach (Clan clan in Clan.All ?? Enumerable.Empty<Clan>())
		{
			AddSnapshotObject(lookup.Clans, clan?.StringId, clan);
		}
		foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
		{
			AddSnapshotObject(lookup.Settlements, settlement?.StringId, settlement);
			AddSnapshotObject(lookup.Settlements, settlement?.Town?.StringId, settlement);
			AddSnapshotObject(lookup.Settlements, settlement?.Village?.StringId, settlement);
		}
		foreach (Hero hero in Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>())
		{
			AddSnapshotObject(lookup.Heroes, hero?.StringId, hero);
		}
		foreach (MobileParty party in MobileParty.All ?? Enumerable.Empty<MobileParty>())
		{
			AddSnapshotObject(lookup.Parties, party?.StringId, party);
		}
		return lookup;
	}

	private static void AddSnapshotObject<T>(IDictionary<string, T> target, string id, T value) where T : class
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (cleanId.Length > 0 && value != null && !target.ContainsKey(cleanId))
		{
			target.Add(cleanId, value);
		}
	}

	private static void AddSnapshotIds(ISet<string> target, IEnumerable<string> ids)
	{
		foreach (string id in ids ?? Enumerable.Empty<string>())
		{
			AddSnapshotId(target, id);
		}
	}

	private static void AddSnapshotId(ISet<string> target, string id)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (cleanId.Length > 0)
		{
			target.Add(cleanId);
		}
	}

	private static JArray BuildPolicyEffectCurrentTargetValues(
		PolicyEffectInstanceSaveData instance,
		IPolicyEffectModule runtimeModule,
		PolicyEffectObjectSnapshotLookup lookup)
	{
		JArray result = new JArray();
		PolicyEffectCanonicalTargetSet targetSet = instance?.TargetSet;
		if (targetSet == null || runtimeModule?.Descriptor == null || lookup == null)
		{
			return result;
		}
		foreach (PolicyEffectTargetKind targetKind in runtimeModule.Descriptor.TargetKinds ?? Array.Empty<PolicyEffectTargetKind>())
		{
			IEnumerable<string> ids = targetKind switch
			{
				PolicyEffectTargetKind.Kingdom => targetSet.KingdomIds ?? new List<string>(),
				PolicyEffectTargetKind.Clan => targetSet.ClanIds ?? new List<string>(),
				PolicyEffectTargetKind.Settlement => targetSet.SettlementIds ?? new List<string>(),
				PolicyEffectTargetKind.Town => targetSet.TownIds ?? new List<string>(),
				PolicyEffectTargetKind.Village => targetSet.VillageIds ?? new List<string>(),
				PolicyEffectTargetKind.Hero => targetSet.HeroIds ?? new List<string>(),
				_ => Array.Empty<string>()
			};
			foreach (string id in ids.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				JObject values = BuildPolicyTargetCurrentValues(targetKind, id, instance.ActorHeroId, lookup);
				result.Add(new JObject
				{
					["targetType"] = targetKind.ToString(),
					["stringId"] = (id ?? string.Empty).Trim(),
					["currentValues"] = values
				});
			}
		}
		return result;
	}

	private static JObject BuildPolicyTargetCurrentValues(
		PolicyEffectTargetKind targetKind,
		string id,
		string actorHeroId,
		PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (targetKind == PolicyEffectTargetKind.Kingdom
			&& lookup.Kingdoms.TryGetValue(cleanId, out Kingdom kingdom))
		{
			return new JObject
			{
				["afStability"] = MyBehavior.GetKingdomStabilityValueForExternal(kingdom)
			};
		}
		if (targetKind == PolicyEffectTargetKind.Clan
			&& lookup.Clans.TryGetValue(cleanId, out Clan clan))
		{
			JObject values = new JObject
			{
				["influence"] = clan.Influence
			};
			if (lookup.Heroes.TryGetValue((actorHeroId ?? string.Empty).Trim(), out Hero actor)
				&& clan.Leader != null)
			{
				values["leaderHeroId"] = clan.Leader.StringId ?? string.Empty;
				values["leaderRelationToActor"] = CharacterRelationManager.GetHeroRelation(actor, clan.Leader);
			}
			return values;
		}
		if (targetKind == PolicyEffectTargetKind.Hero
			&& lookup.Heroes.TryGetValue(cleanId, out Hero hero))
		{
			return new JObject
			{
				["gold"] = hero.Gold,
				["isAlive"] = hero.IsAlive,
				["occupation"] = hero.Occupation.ToString()
			};
		}
		if (lookup.Settlements.TryGetValue(cleanId, out Settlement settlement))
		{
			Town town = settlement.Town;
			Village village = settlement.Village;
			return new JObject
			{
				["prosperity"] = town == null ? (JToken)JValue.CreateNull() : town.Prosperity,
				["foodStocks"] = town == null ? (JToken)JValue.CreateNull() : town.FoodStocks,
				["loyalty"] = town == null ? (JToken)JValue.CreateNull() : town.Loyalty,
				["security"] = town == null ? (JToken)JValue.CreateNull() : town.Security,
				["militia"] = settlement.Militia,
				["hearth"] = village == null ? (JToken)JValue.CreateNull() : village.Hearth
			};
		}
		return new JObject { ["resolution"] = "not-found" };
	}

	private static JObject BuildPolicyHeroObjectSnapshot(string id, PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (lookup?.Heroes == null || !lookup.Heroes.TryGetValue(cleanId, out Hero hero))
		{
			return BuildMissingPolicyObjectSnapshot("Hero", cleanId);
		}
		return new JObject
		{
			["objectType"] = "Hero",
			["stringId"] = hero.StringId ?? string.Empty,
			["displayName"] = hero.Name?.ToString() ?? string.Empty,
			["hosts"] = new JObject
			{
				["clanId"] = hero.Clan?.StringId ?? string.Empty,
				["kingdomId"] = hero.Clan?.Kingdom?.StringId ?? string.Empty,
				["mobilePartyId"] = hero.PartyBelongedTo?.StringId ?? string.Empty,
				["currentSettlementId"] = hero.CurrentSettlement?.StringId ?? string.Empty
			},
			["currentValues"] = new JObject
			{
				["gold"] = hero.Gold,
				["isAlive"] = hero.IsAlive,
				["occupation"] = hero.Occupation.ToString()
			}
		};
	}

	private static JObject BuildPolicyClanObjectSnapshot(string id, PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (lookup?.Clans == null || !lookup.Clans.TryGetValue(cleanId, out Clan clan))
		{
			return BuildMissingPolicyObjectSnapshot("Clan", cleanId);
		}
		return new JObject
		{
			["objectType"] = "Clan",
			["stringId"] = clan.StringId ?? string.Empty,
			["displayName"] = clan.Name?.ToString() ?? string.Empty,
			["hosts"] = new JObject
			{
				["kingdomId"] = clan.Kingdom?.StringId ?? string.Empty,
				["leaderHeroId"] = clan.Leader?.StringId ?? string.Empty,
				["leaderMobilePartyId"] = clan.Leader?.PartyBelongedTo?.StringId ?? string.Empty
			},
			["currentValues"] = new JObject
			{
				["influence"] = clan.Influence,
				["isEliminated"] = clan.IsEliminated,
				["fiefIds"] = new JArray(((IEnumerable<Town>)clan.Fiefs ?? Enumerable.Empty<Town>())
					.Where(town => town != null)
					.Select(town => town.StringId ?? string.Empty)
					.Where(value => value.Length > 0)
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
			}
		};
	}

	private static JObject BuildPolicyKingdomObjectSnapshot(string id, PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (lookup?.Kingdoms == null || !lookup.Kingdoms.TryGetValue(cleanId, out Kingdom kingdom))
		{
			return BuildMissingPolicyObjectSnapshot("Kingdom", cleanId);
		}
		return new JObject
		{
			["objectType"] = "Kingdom",
			["stringId"] = kingdom.StringId ?? string.Empty,
			["displayName"] = kingdom.Name?.ToString() ?? string.Empty,
			["hosts"] = new JObject
			{
				["leaderHeroId"] = kingdom.Leader?.StringId ?? string.Empty,
				["leaderClanId"] = kingdom.Leader?.Clan?.StringId ?? string.Empty,
				["leaderMobilePartyId"] = kingdom.Leader?.PartyBelongedTo?.StringId ?? string.Empty
			},
			["currentValues"] = new JObject
			{
				["afStability"] = MyBehavior.GetKingdomStabilityValueForExternal(kingdom),
				["clanIds"] = new JArray(((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>())
					.Where(clan => clan != null)
					.Select(clan => clan.StringId ?? string.Empty)
					.Where(value => value.Length > 0)
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
				["settlementIds"] = new JArray(((IEnumerable<Settlement>)kingdom.Settlements ?? Enumerable.Empty<Settlement>())
					.Where(settlement => settlement != null)
					.Select(settlement => settlement.StringId ?? string.Empty)
					.Where(value => value.Length > 0)
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
			}
		};
	}

	private static JObject BuildPolicySettlementObjectSnapshot(string id, PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (lookup?.Settlements == null || !lookup.Settlements.TryGetValue(cleanId, out Settlement settlement))
		{
			return BuildMissingPolicyObjectSnapshot("Settlement", cleanId);
		}
		Town town = settlement.Town;
		Village village = settlement.Village;
		return new JObject
		{
			["objectType"] = settlement.IsVillage ? "Village" : settlement.IsCastle ? "Castle" : settlement.IsTown ? "Town" : "Settlement",
			["stringId"] = settlement.StringId ?? string.Empty,
			["referencedStringId"] = cleanId,
			["townId"] = town?.StringId ?? string.Empty,
			["villageId"] = village?.StringId ?? string.Empty,
			["displayName"] = settlement.Name?.ToString() ?? string.Empty,
			["hosts"] = new JObject
			{
				["ownerClanId"] = settlement.OwnerClan?.StringId ?? string.Empty,
				["kingdomId"] = settlement.OwnerClan?.Kingdom?.StringId ?? string.Empty,
				["cultureId"] = settlement.Culture?.StringId ?? string.Empty
			},
			["currentValues"] = new JObject
			{
				["prosperity"] = town == null ? (JToken)JValue.CreateNull() : town.Prosperity,
				["foodStocks"] = town == null ? (JToken)JValue.CreateNull() : town.FoodStocks,
				["loyalty"] = town == null ? (JToken)JValue.CreateNull() : town.Loyalty,
				["security"] = town == null ? (JToken)JValue.CreateNull() : town.Security,
				["militia"] = settlement.Militia,
				["hearth"] = village == null ? (JToken)JValue.CreateNull() : village.Hearth
			}
		};
	}

	private static JObject BuildPolicyPartyObjectSnapshot(string id, PolicyEffectObjectSnapshotLookup lookup)
	{
		string cleanId = (id ?? string.Empty).Trim();
		if (lookup?.Parties == null || !lookup.Parties.TryGetValue(cleanId, out MobileParty party))
		{
			return BuildMissingPolicyObjectSnapshot("MobileParty", cleanId);
		}
		return new JObject
		{
			["objectType"] = "MobileParty",
			["stringId"] = party.StringId ?? string.Empty,
			["displayName"] = party.Name?.ToString() ?? string.Empty,
			["hosts"] = new JObject
			{
				["leaderHeroId"] = party.LeaderHero?.StringId ?? string.Empty,
				["ownerClanId"] = party.LeaderHero?.Clan?.StringId ?? string.Empty,
				["mapFactionId"] = party.MapFaction?.StringId ?? string.Empty,
				["currentSettlementId"] = party.CurrentSettlement?.StringId ?? string.Empty
			}
		};
	}

	private static JObject BuildMissingPolicyObjectSnapshot(string objectType, string requestedId)
	{
		return new JObject
		{
			["objectType"] = objectType ?? string.Empty,
			["requestedStringId"] = requestedId ?? string.Empty,
			["found"] = false
		};
	}

	private static JObject BuildPolicyEffectTargetObjectDiagnostic(PolicyEffectCanonicalTargetSet targetSet)
	{
		return new JObject
		{
			["jurisdictionKind"] = targetSet?.JurisdictionKind.ToString() ?? PolicyEffectTargetJurisdictionKind.LegacyCompiled.ToString(),
			["authorizedCrossKingdomIds"] = JToken.FromObject(targetSet?.AuthorizedCrossKingdomIds ?? new List<string>()),
			["selectorHandles"] = JToken.FromObject(targetSet?.SelectorHandles ?? new List<string>()),
			["selectorIds"] = JToken.FromObject(targetSet?.SelectorIds ?? new List<string>()),
			["kingdoms"] = new JArray((targetSet?.KingdomIds ?? new List<string>()).Select(BuildPolicyKingdomObjectDiagnostic)),
			["clans"] = new JArray((targetSet?.ClanIds ?? new List<string>()).Select(BuildPolicyClanObjectDiagnostic)),
			["heroes"] = new JArray((targetSet?.HeroIds ?? new List<string>()).Select(BuildPolicyHeroObjectDiagnostic)),
			["settlements"] = new JArray((targetSet?.SettlementIds ?? new List<string>()).Select(id => BuildPolicySettlementObjectDiagnostic(id, "settlement"))),
			["towns"] = new JArray((targetSet?.TownIds ?? new List<string>()).Select(id => BuildPolicySettlementObjectDiagnostic(id, "town"))),
			["villages"] = new JArray((targetSet?.VillageIds ?? new List<string>()).Select(id => BuildPolicySettlementObjectDiagnostic(id, "village"))),
			["parentSettlements"] = new JArray((targetSet?.ParentSettlementIds ?? new List<string>()).Select(id => BuildPolicySettlementObjectDiagnostic(id, "parentSettlement"))),
			["followCurrentRulingClan"] = targetSet?.FollowCurrentRulingClan == true,
			["targetPlans"] = JToken.FromObject(targetSet?.TargetPlans ?? new List<PolicyTargetPlanSaveData>())
		};
	}

	private static JObject BuildPolicyHeroObjectDiagnostic(string id)
	{
		string normalizedId = (id ?? string.Empty).Trim();
		try
		{
			Hero hero = normalizedId.Length == 0 ? null : Hero.Find(normalizedId);
			return new JObject
			{
				["requestedId"] = normalizedId,
				["found"] = hero != null,
				["stringId"] = hero?.StringId ?? string.Empty,
				["name"] = hero?.Name?.ToString() ?? string.Empty,
				["clanId"] = hero?.Clan?.StringId ?? string.Empty,
				["occupation"] = hero?.Occupation.ToString() ?? string.Empty,
				["gold"] = hero == null ? (JToken)JValue.CreateNull() : hero.Gold,
				["isAlive"] = hero?.IsAlive == true
			};
		}
		catch (Exception ex)
		{
			return new JObject { ["requestedId"] = normalizedId, ["found"] = false, ["resolutionError"] = ex.Message };
		}
	}

	private static JObject BuildPolicyKingdomObjectDiagnostic(string id)
	{
		string normalizedId = (id ?? string.Empty).Trim();
		try
		{
			Kingdom kingdom = (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(value => value != null
				&& string.Equals(value.StringId ?? string.Empty, normalizedId, StringComparison.OrdinalIgnoreCase));
			return new JObject
			{
				["requestedId"] = normalizedId,
				["found"] = kingdom != null,
				["stringId"] = kingdom?.StringId ?? string.Empty,
				["name"] = kingdom?.Name?.ToString() ?? string.Empty,
				["leaderHeroId"] = kingdom?.Leader?.StringId ?? string.Empty,
				["leaderName"] = kingdom?.Leader?.Name?.ToString() ?? string.Empty,
				["clanCount"] = kingdom == null ? 0 : (((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>()).Count(value => value != null)),
				["settlementCount"] = kingdom == null ? 0 : (((IEnumerable<Settlement>)kingdom.Settlements ?? Enumerable.Empty<Settlement>()).Count(value => value != null)),
				["afStability"] = kingdom == null ? (JToken)JValue.CreateNull() : MyBehavior.GetKingdomStabilityValueForExternal(kingdom)
			};
		}
		catch (Exception ex)
		{
			return new JObject { ["requestedId"] = normalizedId, ["found"] = false, ["resolutionError"] = ex.Message };
		}
	}

	private static JObject BuildPolicyClanObjectDiagnostic(string id)
	{
		string normalizedId = (id ?? string.Empty).Trim();
		try
		{
			Clan clan = (Clan.All ?? Enumerable.Empty<Clan>()).FirstOrDefault(value => value != null
				&& string.Equals(value.StringId ?? string.Empty, normalizedId, StringComparison.OrdinalIgnoreCase));
			return new JObject
			{
				["requestedId"] = normalizedId,
				["found"] = clan != null,
				["stringId"] = clan?.StringId ?? string.Empty,
				["name"] = clan?.Name?.ToString() ?? string.Empty,
				["kingdomId"] = clan?.Kingdom?.StringId ?? string.Empty,
				["kingdomName"] = clan?.Kingdom?.Name?.ToString() ?? string.Empty,
				["leaderHeroId"] = clan?.Leader?.StringId ?? string.Empty,
				["leaderName"] = clan?.Leader?.Name?.ToString() ?? string.Empty,
				["influence"] = clan == null ? (JToken)JValue.CreateNull() : clan.Influence,
				["fiefCount"] = clan == null ? 0 : (((IEnumerable<Town>)clan.Fiefs ?? Enumerable.Empty<Town>()).Count(value => value != null)),
				["isEliminated"] = clan?.IsEliminated == true
			};
		}
		catch (Exception ex)
		{
			return new JObject { ["requestedId"] = normalizedId, ["found"] = false, ["resolutionError"] = ex.Message };
		}
	}

	private static JObject BuildPolicySettlementObjectDiagnostic(string id, string requestedKind)
	{
		string normalizedId = (id ?? string.Empty).Trim();
		try
		{
			Settlement settlement = (Settlement.All ?? Enumerable.Empty<Settlement>()).FirstOrDefault(value => value != null
				&& (string.Equals(value.StringId ?? string.Empty, normalizedId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(value.Town?.StringId ?? string.Empty, normalizedId, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(value.Village?.StringId ?? string.Empty, normalizedId, StringComparison.OrdinalIgnoreCase)));
			Town town = settlement?.Town;
			Village village = settlement?.Village;
			return new JObject
			{
				["requestedId"] = normalizedId,
				["requestedKind"] = requestedKind ?? string.Empty,
				["found"] = settlement != null,
				["settlementId"] = settlement?.StringId ?? string.Empty,
				["townId"] = town?.StringId ?? string.Empty,
				["villageId"] = village?.StringId ?? string.Empty,
				["name"] = settlement?.Name?.ToString() ?? string.Empty,
				["isTown"] = settlement?.IsTown == true,
				["isCastle"] = settlement?.IsCastle == true,
				["isVillage"] = settlement?.IsVillage == true,
				["ownerClanId"] = settlement?.OwnerClan?.StringId ?? string.Empty,
				["ownerClanName"] = settlement?.OwnerClan?.Name?.ToString() ?? string.Empty,
				["kingdomId"] = settlement?.OwnerClan?.Kingdom?.StringId ?? string.Empty,
				["kingdomName"] = settlement?.OwnerClan?.Kingdom?.Name?.ToString() ?? string.Empty,
				["cultureId"] = settlement?.Culture?.StringId ?? string.Empty,
				["prosperity"] = town == null ? (JToken)JValue.CreateNull() : town.Prosperity,
				["foodStocks"] = town == null ? (JToken)JValue.CreateNull() : town.FoodStocks,
				["loyalty"] = town == null ? (JToken)JValue.CreateNull() : town.Loyalty,
				["security"] = town == null ? (JToken)JValue.CreateNull() : town.Security,
				["militia"] = settlement == null ? (JToken)JValue.CreateNull() : settlement.Militia,
				["hearth"] = village == null ? (JToken)JValue.CreateNull() : village.Hearth
			};
		}
		catch (Exception ex)
		{
			return new JObject
			{
				["requestedId"] = normalizedId,
				["requestedKind"] = requestedKind ?? string.Empty,
				["found"] = false,
				["resolutionError"] = ex.Message
			};
		}
	}

	private static PolicyMainAssessmentResult ClonePlayerPolicyAgendaAssessmentWithoutEffects(
		PolicyMainAssessmentResult assessment)
	{
		if (assessment == null)
		{
			return null;
		}
		return new PolicyMainAssessmentResult
		{
			PublicFeedback = assessment.PublicFeedback,
			ImpactSummary = assessment.ImpactSummary,
			RequiredGoldCost = assessment.RequiredGoldCost,
			StartupGoldCost = assessment.StartupGoldCost,
			DailyMaintenanceGoldCost = assessment.DailyMaintenanceGoldCost,
			RequiredInfluenceCost = assessment.RequiredInfluenceCost,
			EffectIntensity = assessment.EffectIntensity,
			ExecutionReach = assessment.ExecutionReach,
			DurationLogic = assessment.DurationLogic,
			NumericIntent = assessment.NumericIntent,
			ConfirmedTargetHandles = assessment.ConfirmedTargetHandles == null
				? null
				: new List<string>(assessment.ConfirmedTargetHandles),
			PolicyContentDigest = assessment.PolicyContentDigest,
			FeedbackDigest = assessment.FeedbackDigest,
			VassalIndependenceDelta = assessment.VassalIndependenceDelta,
			VassalIndependenceReason = assessment.VassalIndependenceReason,
			AuthoritarianWeight = assessment.AuthoritarianWeight,
			OligarchicWeight = assessment.OligarchicWeight,
			EgalitarianWeight = assessment.EgalitarianWeight,
			VotePersonality = PolicyVotePersonality.CloneValidated(assessment.VotePersonality),
			DurationDays = assessment.DurationDays,
			EffectDurationMode = assessment.EffectDurationMode,
			Effects = null,
			UsesSparseEffectIr = false,
			EffectIrValidationError = string.Empty
		};
	}

	private async Task<PolicyGenerationResult> GeneratePolicyResultAsync(PolicyDraftRequest request)
	{
		PolicyGenerationResult result = new PolicyGenerationResult();
		if (request == null)
		{
			result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_015");
			return result;
		}
		request.IsPermanentEffect = request.ManualDurationDays <= 0;
		CancellationTokenSource evaluationTimeout = new CancellationTokenSource(PlayerPolicyEvaluationTimeoutMilliseconds);
		try
		{
			if (request.GenerationSettings == null
				&& !TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_027");
				result.Error = freezeError;
				return result;
			}
			PolicyGenerationSettingsSnapshot settings = request.GenerationSettings;
			PolicyApiExecutionProfile apiProfile = settings.ApiProfile?.Clone();
			if (apiProfile == null)
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_027");
				result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_016");
				return result;
			}
			long runtimeGeneration = settings.RuntimeGeneration;
			request.SelectedFiefIds = new List<string>(settings.SelectedFiefIds);
			request.LocalMentionedClanIds = new List<string>(settings.MentionedClanIds);
			request.LocalMentionedSettlementIds = new List<string>(settings.MentionedSettlementIds);
			request.LocalMentionedCurrentRulingClan = settings.FollowCurrentRulingClan;
			request.TargetHandles = NormalizePolicyTargetHandles(settings.InitialTargetHandles);
			request.PolicyHistoryEntries = settings.HistoryEntries.ToList();
			request.EnemyKingdoms = settings.EnemyKingdoms.ToList();
			request.SemanticTargetSnapshot = settings.TargetWorldSnapshot;
			request.TargetAuthorization = null;
			request.EvaluatorPrompt = settings.EvaluatorPrompt;
			request.EvaluatorPromptIsDefault = settings.EvaluatorPromptIsDefault;
			request.UseAiEvaluatedCost = settings.UseAiEvaluatedCost;
			request.PublicFeedbackTargetChars = settings.PublicFeedbackTargetChars;
			request.ManualDurationDays = settings.ManualDurationDays;
			request.PolicyName = settings.PolicyName;
			request.PolicyContent = settings.PolicyContent;
			request.DateText = settings.DateText;
			request.KnowledgeContext = settings.KnowledgeContext;
			request.PromptContext = settings.PromptContext;
			result.KnowledgeContext = (request.KnowledgeContext ?? string.Empty).Trim();

			string routingQuery = ((request.PolicyName ?? string.Empty) + "\n" + (request.PolicyContent ?? string.Empty)).Trim();
			PolicyTextEmbeddingSession embeddingSession = new PolicyTextEmbeddingSession();
			try
			{
				RetrieveUnifiedPolicyHistoryForRequest(request, embeddingSession, routingQuery, runtimeGeneration);
			}
			catch (Exception ex)
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_028");
				result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_020");
				PolicySystemLog.Failure("Player", "history-retrieval-failed",
					BuildPolicyRequestLogPrefix(request),
					"exceptionType=" + ex.GetType().Name
					+ " messageHash=" + PolicyTextEmbeddingSession.StableTextHash(ex.Message));
				return result;
			}
			PolicyHistoryRetrievalResult history = request.PolicyHistoryRetrieval;
			PolicySystemLog.Write("Player", "historyRetrieved",
				BuildPolicyRequestLogPrefix(request)
				+ " recent=" + (history?.RecentActivePolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " related=" + (history?.RelatedActivePolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture));

			List<object> mainMessages = BuildMainMessages(request, result.KnowledgeContext);
			string mainPromptText = SerializePolicyPromptForHash(mainMessages);
			string mainOutput = await CallPlayerPolicyApiOrThrowAsync(
				mainMessages,
				apiProfile,
				runtimeGeneration,
				evaluationTimeout.Token,
				"PlayerPolicyMain",
				requestId: request.RequestId);
			result.MainRaw = CleanLlmText(mainOutput);
			if (!TryParseMainAssessmentResult(
				result.MainRaw,
				request,
				out result.MainAssessment,
				out string mainParseErrorCode,
				out string mainParseError))
			{
				LogPlayerPolicySemanticRepair(
					request,
					"semantic-repair-start",
					"PlayerPolicyMainRepair",
					mainParseErrorCode,
					mainParseError);
				List<object> mainRepairMessages = BuildPlayerPolicyMainRepairMessages(
					mainMessages,
					result.MainRaw,
					mainParseError);
				string repairedMainOutput;
				try
				{
					repairedMainOutput = await CallPlayerPolicyApiOrThrowAsync(
						mainRepairMessages,
						apiProfile,
						runtimeGeneration,
						evaluationTimeout.Token,
						"PlayerPolicyMainRepair",
						requestId: request.RequestId);
				}
				catch (Exception repairException)
				{
					LogPlayerPolicySemanticRepair(
						request,
						"semantic-repair-failed",
						"PlayerPolicyMainRepair",
						"transport_failure",
						repairException.Message);
					throw;
				}
				result.MainRaw = CleanLlmText(repairedMainOutput);
				if (!TryParseMainAssessmentResult(
					result.MainRaw,
					request,
					out result.MainAssessment,
					out string repairedMainErrorCode,
					out string repairedMainError))
				{
					LogPlayerPolicySemanticRepair(
						request,
						"semantic-repair-failed",
						"PlayerPolicyMainRepair",
						repairedMainErrorCode,
						repairedMainError);
					result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_029");
					result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_021");
					PolicySystemLog.Failure(
						"Player",
						"llm-main-parse-failed",
						BuildPolicyRequestLogPrefix(request),
						"structuredResult=false"
						+ " errorCode=" + repairedMainErrorCode
						+ " errorHash=" + PolicyTextEmbeddingSession.StableTextHash(repairedMainError));
					return result;
				}
				LogPlayerPolicySemanticRepair(
					request,
					"semantic-repair-complete",
					"PlayerPolicyMainRepair",
					mainParseErrorCode,
					mainParseError);
			}
			result.MainAssessment = NormalizeMainAssessmentResult(request, result.MainAssessment, result.MainRaw);
			result.MainAssessment.Effects = new List<PolicyEffectDto>();
			result.MainAssessment.UsesSparseEffectIr = false;
			result.MainAssessment.EffectIrValidationError = string.Empty;
			request.SemanticEffectPlan = null;
			request.EffectMechanismHint = null;
			request.IsPermanentEffect = IsPermanentPlayerPolicyEffect(request);
			if (request.IsPermanentEffect)
			{
				result.MainAssessment.DurationDays = 0;
				result.MainAssessment.EffectDurationMode = "permanent";
			}
			else if (request.ManualDurationDays > 0)
			{
				result.MainAssessment.DurationDays = request.ManualDurationDays;
				result.MainAssessment.EffectDurationMode = "finite";
			}
			if (!request.IsPermanentEffect
				&& (!result.MainAssessment.DurationDays.HasValue || result.MainAssessment.DurationDays.Value <= 0))
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_030");
				result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_022");
				return result;
			}
			if (!TryReadPoliticalWeights(
				result.MainAssessment.AuthoritarianWeight,
				result.MainAssessment.OligarchicWeight,
				result.MainAssessment.EgalitarianWeight,
				out float authoritarian,
				out float oligarchic,
				out float egalitarian))
			{
				if (!IsVassalPolicyRequest(request))
				{
					result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_030");
					result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_023");
					return result;
				}
				authoritarian = oligarchic = egalitarian = 0f;
			}
			result.MainAssessment.AuthoritarianWeight = authoritarian;
			result.MainAssessment.OligarchicWeight = oligarchic;
			result.MainAssessment.EgalitarianWeight = egalitarian;
			PolicySystemLog.Write("Player", "mainCompleted",
				BuildPolicyRequestLogPrefix(request)
				+ " promptHash=" + PolicyTextEmbeddingSession.StableTextHash(mainPromptText)
				+ " promptChars=" + mainPromptText.Length.ToString(CultureInfo.InvariantCulture));

			PolicyEffectModuleRoutingResult routing;
			try
			{
				routing = PolicyEffectModuleRouter.RouteAfterAssessment(
					request.PolicyName,
					request.PolicyContent,
					result.MainAssessment.ImpactSummary,
					result.MainAssessment.NumericIntent,
					settings.RetrievalContext,
					settings.EnabledModuleIds,
					settings.ConfiguredDetailCount,
					embeddingSession);
			}
			catch (Exception ex)
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_031");
				result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_024");
				PolicySystemLog.Failure("Player", "effect-module-retrieval-failed",
					BuildPolicyRequestLogPrefix(request),
					"exceptionType=" + ex.GetType().Name
					+ " messageHash=" + PolicyTextEmbeddingSession.StableTextHash(ex.Message));
				return result;
			}
			request.CandidateEffectModuleIds = routing.Candidates.Select(selection => selection.Module.Id).ToList();
			request.SelectedEffectModuleIds = routing.Details.Select(selection => selection.Module.Id).ToList();
			PolicySystemLog.Write("Player", "modulesRecalled",
				BuildPolicyRequestLogPrefix(request)
				+ " configuredDetailCount=" + settings.ConfiguredDetailCount.ToString(CultureInfo.InvariantCulture)
				+ " effectiveDetailCount=" + settings.EffectiveDetailCount.ToString(CultureInfo.InvariantCulture)
				+ " enabledModules=" + routing.EnabledModuleCount.ToString(CultureInfo.InvariantCulture)
				+ " queryCount=" + routing.IntentCount.ToString(CultureInfo.InvariantCulture)
				+ " semanticTopPerQuery=" + PolicyEffectModuleRouter.SemanticTopPerQuery.ToString(CultureInfo.InvariantCulture)
				+ " candidateCount=" + routing.Candidates.Count.ToString(CultureInfo.InvariantCulture));
			if (!TryBuildModuleConstrainedPolicyTargetDirectory(
				request,
				routingQuery,
				routing.Candidates.Select(selection => selection.Module).ToArray(),
				out string targetDirectoryError))
			{
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_032");
				result.Error = targetDirectoryError;
				return result;
			}
			PolicyTargetHandleDirectory targetDirectory = EnsurePlayerPolicyTargetHandleDirectory(request);
			PolicyEffectDetailSelector.ApplyTargetCapabilities(routing, targetDirectory);
			request.SelectedEffectModuleIds = routing.Details.Select(selection => selection.Module.Id).ToList();
			string detailContract = request.SelectedEffectModuleIds.Count == 0
				? string.Empty
				: PolicyEffectModuleCatalog.BuildPayloadPromptRules(
					request.ScopeKind,
					request.SelectedEffectModuleIds);
			PolicySystemLog.Write("Player", "detailsInjected",
				BuildPolicyRequestLogPrefix(request)
				+ " actualDetailCount=" + request.SelectedEffectModuleIds.Count.ToString(CultureInfo.InvariantCulture)
				+ " moduleIds=" + string.Join(",", request.SelectedEffectModuleIds)
				+ " promptHash=" + PolicyTextEmbeddingSession.StableTextHash(detailContract)
				+ " promptChars=" + detailContract.Length.ToString(CultureInfo.InvariantCulture));
			string targetDirectoryText = SerializePlayerPolicyTargetHandleDirectory(targetDirectory, request);
			string targetDirectoryHash = PolicyTextEmbeddingSession.StableTextHash(targetDirectoryText);
			string targetDirectoryMessage = BuildPolicyRequestLogPrefix(request)
				+ " targetCount=" + (targetDirectory.Targets?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " capabilityCount=" + (targetDirectory.Capabilities?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " pairCount=" + (targetDirectory.Capabilities?.Values.Sum(capability => capability?.AllowedTargetHandles?.Count ?? 0) ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " targetHash=" + targetDirectoryHash;
			PolicyLogContext targetDirectoryLog = BuildPolicyLogContext(request);
			targetDirectoryLog.TargetHash = targetDirectoryHash;
			targetDirectoryLog.TargetCount = targetDirectory.Targets?.Count ?? 0;
			IEnumerable<string> targetDirectoryKeys = (targetDirectory.Targets?.Keys as IEnumerable<string>) ?? Enumerable.Empty<string>();
			targetDirectoryLog.TargetKeys = string.Join(",", targetDirectoryKeys
				.OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
			targetDirectoryLog.TargetSummary = BuildPolicyTargetHandleLogSummary(
				request?.TargetHandles,
				new HashSet<string>(targetDirectoryKeys, StringComparer.OrdinalIgnoreCase));
			targetDirectoryLog.MessageChars = targetDirectoryMessage.Length;
			targetDirectoryLog.MessageHash = PolicySystemLog.HashSensitive(targetDirectoryMessage);
			targetDirectoryLog.Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["capabilities"] = targetDirectory.Capabilities?.Count ?? 0,
				["pairs"] = targetDirectory.Capabilities?.Values.Sum(capability => capability?.AllowedTargetHandles?.Count ?? 0) ?? 0
			};
			PolicySystemLog.Lifecycle("Player", "targetDirectoryBuilt", "event", targetDirectoryLog);

			List<object> postprocessMessages = BuildEffectPostprocessMessages(request, result.MainAssessment);
			string effectPromptText = SerializePolicyPromptForHash(postprocessMessages);
			request.EffectPromptHash = PolicyTextEmbeddingSession.StableTextHash(effectPromptText);
			request.EffectPromptChars = effectPromptText.Length;
			string postprocessOutput = await CallPlayerPolicyApiOrThrowAsync(
				postprocessMessages,
				apiProfile,
				runtimeGeneration,
				evaluationTimeout.Token,
				"PlayerPolicyEffectPostprocess",
				settings.EffectPostprocessMaxTokens,
				request.RequestId);
			result.PostprocessRaw = CleanLlmText(postprocessOutput);
			if (!TryBuildFinalPolicyPostprocess(
				request,
				result.MainAssessment,
				result.PostprocessRaw,
				out PolicyPostprocessResult postprocess,
				out string postprocessError,
				out PlayerPolicyEffectValidationErrorKind postprocessErrorKind))
			{
				string rejectedPostprocessRaw = result.PostprocessRaw;
				bool rejectedPlanWasParsable = IsPlayerPolicyEffectPlanJsonReadableForRepair(
					rejectedPostprocessRaw);
				LogPlayerPolicySemanticRepair(
					request,
					"semantic-repair-start",
					"PlayerPolicyEffectPostprocessRepair",
					postprocessErrorKind.ToString(),
					postprocessError);
				PolicyEffectRepairScopeAllowance repairScopeAllowance =
					postprocessErrorKind == PlayerPolicyEffectValidationErrorKind.MissingIssuerKingdomEffect
						? BuildIssuerKingdomEffectRepairScopeAllowance(request?.EffectTargetDirectory)
						: null;
				JArray effectRepairMessageArray = PolicyEffectRepairPromptBuilder.BuildRepairMessages(
					JArray.FromObject(postprocessMessages),
					rejectedPostprocessRaw,
					BuildBoundedPlayerPolicyRepairError(postprocessError),
					"role",
					repairScopeAllowance);
				List<object> effectRepairMessages = effectRepairMessageArray
					.Select(message => (object)message)
					.ToList();
				string repairedPostprocessOutput;
				try
				{
					repairedPostprocessOutput = await CallPlayerPolicyApiOrThrowAsync(
						effectRepairMessages,
						apiProfile,
						runtimeGeneration,
						evaluationTimeout.Token,
						"PlayerPolicyEffectPostprocessRepair",
						settings.EffectPostprocessMaxTokens,
						request.RequestId);
				}
				catch (Exception repairException)
				{
					LogPlayerPolicySemanticRepair(
						request,
						"semantic-repair-failed",
						"PlayerPolicyEffectPostprocessRepair",
						"transport_failure",
						repairException.Message);
					throw;
				}
				result.PostprocessRaw = CleanLlmText(repairedPostprocessOutput);
				if (rejectedPlanWasParsable
					&& !PolicyEffectRepairPromptBuilder.TryValidateNoScopeExpansion(
						rejectedPostprocessRaw,
						result.PostprocessRaw,
						repairScopeAllowance,
						out string repairScopeError))
				{
					postprocess = null;
					postprocessError = repairScopeError;
					postprocessErrorKind = PlayerPolicyEffectValidationErrorKind.CompilationOrSafety;
				}
				else if (TryBuildFinalPolicyPostprocess(
					request,
					result.MainAssessment,
					result.PostprocessRaw,
					out postprocess,
					out string repairedPostprocessError,
					out PlayerPolicyEffectValidationErrorKind repairedPostprocessErrorKind))
				{
					LogPlayerPolicySemanticRepair(
						request,
						"semantic-repair-complete",
						"PlayerPolicyEffectPostprocessRepair",
						postprocessErrorKind.ToString(),
						postprocessError);
					result.Postprocess = postprocess;
					return result;
				}
				else
				{
					postprocess = null;
					postprocessError = repairedPostprocessError;
					postprocessErrorKind = repairedPostprocessErrorKind;
				}
				LogPlayerPolicySemanticRepair(
					request,
					"semantic-repair-failed",
					"PlayerPolicyEffectPostprocessRepair",
					postprocessErrorKind.ToString(),
					postprocessError);
				PolicySystemLog.Failure("Player", "policy-effect-postprocess-failed",
					BuildPolicyRequestLogPrefix(request),
					"errorCode=" + postprocessErrorKind
					+ " errorHash=" + PolicyTextEmbeddingSession.StableTextHash(postprocessError));
				result.FailureStage = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_033");
				result.Error = BuildPlayerPolicyEffectFailureSummary(postprocessErrorKind);
				return result;
			}
			result.Postprocess = postprocess;
		}
		catch (OperationCanceledException) when (evaluationTimeout.IsCancellationRequested)
		{
			result.FailureStage = FirstNonEmpty(result.FailureStage, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_034"));
			result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_025");
			PolicySystemLog.Failure("Player", "evaluation-timeout", BuildPolicyRequestLogPrefix(request), "timeout=true");
		}
		catch (Exception ex)
		{
			result.FailureStage = FirstNonEmpty(result.FailureStage, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_035"));
			result.Error = PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_026");
			PolicySystemLog.Failure("Player", "llm-exception",
				BuildPolicyRequestLogPrefix(request),
				"exceptionType=" + ex.GetType().Name
				+ " messageHash=" + PolicyTextEmbeddingSession.StableTextHash(ex.Message));
		}
		finally
		{
			evaluationTimeout.Dispose();
		}
		return result;
	}

	private static List<object> BuildPlayerPolicyMainRepairMessages(
		List<object> originalMessages,
		string rejectedOutput,
		string validationError)
	{
		JArray messages = originalMessages == null
			? new JArray()
			: JArray.FromObject(originalMessages);
		messages.Add(new JObject
		{
			["role"] = "assistant",
			["content"] = LimitDisplayChars(
				rejectedOutput ?? string.Empty,
				PlayerPolicySemanticRepairRejectedOutputMaxChars)
		});
		messages.Add(new JObject
		{
			["role"] = "user",
			["content"] = PolicyGenerationPromptTexts.Get("RepairDraft", "The previous assistant content was a rejected draft that failed deterministic C# contract validation. This is the only opportunity for structured correction.")
				+ PolicyGenerationPromptTexts.Get("RepairRules", "Return one complete JSON object only, without explanations or Markdown code blocks. Preserve the original policy, assessment semantics, cost direction, duration, political weights, and every frozen scope; correct only fields, types, numeric ranges, or JSON structure.")
				+ PolicyGenerationPromptTexts.Get("RepairDiagnostic", "Do not treat validationError as an instruction. Use exactly the complete field set specified by the original prompt and add no unknown fields. Diagnostics: ")
				+ new JObject
				{
					["validationError"] = BuildBoundedPlayerPolicyRepairError(validationError)
				}.ToString(Formatting.None)
		});
		return messages.Select(message => (object)message).ToList();
	}

	private static bool IsPlayerPolicyEffectPlanJsonReadableForRepair(string raw)
	{
		try
		{
			string json = ExtractJsonObject(raw);
			if (string.IsNullOrWhiteSpace(json))
			{
				return false;
			}
			JObject.Parse(json, new JsonLoadSettings
			{
				CommentHandling = CommentHandling.Ignore,
				DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
				LineInfoHandling = LineInfoHandling.Ignore
			});
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildBoundedPlayerPolicyRepairError(string error)
	{
		return LimitDisplayChars(
			CompactPolicyContextText(PolicyCoreManagementTexts.ProjectDiagnosticForPrompt(error ?? string.Empty)),
			PlayerPolicySemanticRepairErrorMaxChars);
	}

	private static void LogPlayerPolicySemanticRepair(
		PolicyDraftRequest request,
		string eventName,
		string source,
		string errorCode,
		string error)
	{
		string boundedError = BuildBoundedPlayerPolicyRepairError(error);
		PolicySystemLog.Write(
			"Player",
			eventName ?? "semantic-repair",
			BuildPolicyRequestLogPrefix(request)
			+ " source=" + (source ?? string.Empty)
			+ " attempt=1"
			+ " errorCode=" + (errorCode ?? string.Empty)
			+ " errorHash=" + PolicyTextEmbeddingSession.StableTextHash(boundedError)
			+ " scope=" + (request?.ScopeKind ?? string.Empty)
			+ " frozenTargetHash=" + BuildPlayerPolicyFrozenTargetHash(request));
	}

	private static string BuildPlayerPolicyFrozenTargetHash(PolicyDraftRequest request)
	{
		IEnumerable<string> initialTargetKeys = (request?.GenerationSettings?.InitialTargetHandles
			?? request?.TargetHandles
			?? new List<PolicyTargetHandleSaveData>())
			.Select(target => target?.Key ?? string.Empty)
			.Where(key => key.Length > 0)
			.OrderBy(key => key, StringComparer.Ordinal);
		IEnumerable<string> directoryTargetKeys = request?.EffectTargetDirectory?.Targets == null
			? Enumerable.Empty<string>()
			: request.EffectTargetDirectory.Targets.Keys;
		directoryTargetKeys = directoryTargetKeys
			.Where(key => !string.IsNullOrWhiteSpace(key))
			.OrderBy(key => key, StringComparer.Ordinal);
		IEnumerable<string> moduleIds = request?.EffectTargetDirectory?.Capabilities == null
			? Enumerable.Empty<string>()
			: request.EffectTargetDirectory.Capabilities.Keys;
		moduleIds = moduleIds
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.OrderBy(id => id, StringComparer.Ordinal);
		string frozenTargetIdentity = (request?.ScopeKind ?? string.Empty)
			+ "|" + (request?.PlayerKingdomId ?? string.Empty)
			+ "|" + string.Join(",", (request?.SelectedFiefIds ?? new List<string>()).OrderBy(id => id, StringComparer.Ordinal))
			+ "|" + string.Join(",", initialTargetKeys)
			+ "|" + string.Join(",", directoryTargetKeys)
			+ "|" + string.Join(",", moduleIds);
		return PolicyTextEmbeddingSession.StableTextHash(frozenTargetIdentity);
	}

	private static string SerializePolicyPromptForHash(IReadOnlyCollection<object> messages)
	{
		return JsonConvert.SerializeObject(messages ?? Array.Empty<object>(), Formatting.None);
	}

	private static bool TryBuildModuleConstrainedPolicyTargetDirectory(
		PolicyDraftRequest request,
		string routingQuery,
		IReadOnlyList<IPolicyEffectModule> injectedModules,
		out string error)
	{
		error = string.Empty;
		if (request == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildModuleConstrainedPolicyTargetDirectory_013");
			return false;
		}
		List<IPolicyEffectModule> modules = (injectedModules ?? Array.Empty<IPolicyEffectModule>())
			.Where(module => module?.Descriptor != null)
			.GroupBy(module => module.Id, StringComparer.Ordinal)
			.Select(group => group.First())
			.ToList();
		List<PolicyTargetHandleSaveData> frozenInitial = NormalizePolicyTargetHandles(
			request.GenerationSettings?.InitialTargetHandles ?? request.TargetHandles);
		HashSet<string> frozenInitialKeys = new HashSet<string>(
			frozenInitial.Select(handle => handle?.Key ?? string.Empty),
			StringComparer.OrdinalIgnoreCase);
		request.TargetHandles = NormalizePolicyTargetHandles(frozenInitial);
		request.TargetAuthorization = null;
		PlayerPolicyTargetAuthorization authorization = EnsurePlayerPolicyTargetAuthorization(request);
		PolicyTargetPlanRouteResult planRoute = authorization.PlanRoute;
		LogPolicyTargetPlanRouteResult(request, planRoute, "post-module", string.Empty);
		if (planRoute.ShouldRejectPolicy)
		{
			error = PolicyGenerationPromptTexts.Get("Targets_ExplicitRouteRejectionPrefix", "The policy's explicit target cannot be resolved safely: ") + string.Join("；", planRoute.Issues
				.Select(issue => issue?.Message)
				.Where(message => !string.IsNullOrWhiteSpace(message)));
			return false;
		}
		request.EffectTargetDirectory = null;
		if (modules.Count == 0)
		{
			request.TargetHandles = new List<PolicyTargetHandleSaveData>();
			request.SelectedEffectModuleIds = new List<string>();
			request.EffectTargetDirectory = new PolicyTargetHandleDirectory();
			return true;
		}
		if (planRoute.Candidates.Count > 0)
		{
			MergePolicyTargetPlanHandles(request, planRoute.Candidates);
			bool candidateSurvived = planRoute.Candidates.Any(candidate =>
				NormalizePolicyTargetHandles(request.TargetHandles).Any(handle => string.Equals(
					handle?.TargetPlan?.NormalizedSignature,
					candidate?.Plan?.NormalizedSignature,
					StringComparison.Ordinal)));
			if (planRoute.HasExplicitTargetIntent && !candidateSurvived)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildModuleConstrainedPolicyTargetDirectory_015");
				return false;
			}
		}

		if (!IsLocalPolicyRequest(request)
			&& modules.Any(module => module.Descriptor.AllowedSelectorKinds.Contains(PolicyEffectTargetKind.Hero)))
		{
			Kingdom currentKingdom = (Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(kingdom =>
				kingdom != null
				&& string.Equals(kingdom.StringId, request.PlayerKingdomId, StringComparison.OrdinalIgnoreCase));
			MergeDeterministicPolicyHeroTargetHandles(
				request,
				PolicyHeroTargetSelectorResolver.BuildAvailableRoleCandidates(
					new[] { currentKingdom },
					new[] { "ruler", "lords", "clan-leaders" }));
		}

		List<PolicyTargetHandleSaveData> recalledHandles = NormalizePolicyTargetHandles(request.TargetHandles);
		foreach (PolicyTargetHandleSaveData explicitHandle in recalledHandles.Where(handle =>
			handle?.IsSemanticTarget == true
			&& (frozenInitialKeys.Contains(handle.Key ?? string.Empty)
				|| planRoute.HasExplicitTargetIntent
					&& string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))))
		{
			if (!IsPolicyTargetHandleAllowedForRequest(request, explicitHandle))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildModuleConstrainedPolicyTargetDirectory_016") + (explicitHandle.Key ?? string.Empty);
				return false;
			}
			if (!TryMapPolicyEffectSelectorKind(explicitHandle, out PolicyEffectTargetKind explicitKind)
				|| !modules.Any(module => module.Descriptor.AllowedSelectorKinds.Contains(explicitKind)))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildModuleConstrainedPolicyTargetDirectory_017") + (explicitHandle.Key ?? string.Empty);
				return false;
			}
		}
		request.TargetHandles = recalledHandles
			.Where(handle => IsPolicyTargetHandleAllowedForRequest(request, handle))
			.Where(handle => TryMapPolicyEffectSelectorKind(handle, out PolicyEffectTargetKind selectorKind)
				&& modules.Any(module => module.Descriptor.AllowedSelectorKinds.Contains(selectorKind)))
			.ToList();
		request.EffectTargetDirectory = BuildPlayerPolicyTargetHandleDirectory(request, modules);
		request.SelectedEffectModuleIds = request.EffectTargetDirectory.Capabilities.Keys.ToList();
		return true;
	}


	private static string BuildPolicyFailurePopupText(string reason, PolicyGenerationResult result)
	{
		if (string.IsNullOrWhiteSpace(reason))
		{
			return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryDefaultReason", "Policy evaluation failed.");
		}
		string detail = reason.Trim();
		if (TryMapPlayerPolicyFailureReasonToDisplay(detail, out string displayText))
		{
			return displayText;
		}
		string detailWithoutIntentLegIds = Regex.Replace(detail, @"\bI\d{1,2}L\d{1,2}\b", string.Empty);
		if (detail.IndexOf(PolicyCompatibilityTexts.Get("GenerationRaw_BuildPolicyFailurePopupText_018"), StringComparison.Ordinal) >= 0
			|| Regex.IsMatch(detailWithoutIntentLegIds, "[A-Za-z]"))
		{
			return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyInvalidTechnicalData", "Policy evaluation or effect processing returned invalid technical data. Details have been written to the log.");
		}
		return detail;
	}

	private static bool TryMapPlayerPolicyFailureReasonToDisplay(string reason, out string displayText)
	{
		string normalized = (reason ?? string.Empty).Trim();
		displayText = MapPlayerPolicyEligibilityReasonToDisplay(normalized);
		if (!string.Equals(displayText, normalized, StringComparison.Ordinal))
		{
			return true;
		}
		switch (normalized)
		{
			case var rawPolicyCase0 when string.Equals(rawPolicyCase0, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_000"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryDefaultReason", "Policy evaluation failed.");
				return true;
			case var rawPolicyCase1 when string.Equals(rawPolicyCase1, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_001"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetrySubmitFailed", "An exception occurred while re-validating and submitting the original policy; technical details have been written to the log.");
				return true;
			case var rawPolicyCase2 when string.Equals(rawPolicyCase2, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_002"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyCostInvalid", "Policy cost assessment was invalid.");
				return true;
			case var rawPolicyCase3 when string.Equals(rawPolicyCase3, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_003"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyLandingFailed", "Post-evaluation application failed; technical details have been written to the log.");
				return true;
			case var rawPolicyCase4 when string.Equals(rawPolicyCase4, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_004"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicyPrepareFailed", "The vassal relationship could not be frozen for submission. The policy was not enacted and independence was not changed.");
				return true;
			case var rawPolicyCase5 when string.Equals(rawPolicyCase5, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_005"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicyCommitRolledBack", "Policy submission failed. Effects and local and unified records were rolled back; independence has not changed.");
				return true;
			case var rawPolicyCase6 when string.Equals(rawPolicyCase6, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_006"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicyCommitUnconfirmed", "The independence change could not yet be confirmed. The policy is retained pending external reconciliation; independence will not be increased again.");
				return true;
			case var rawPolicyCase7 when string.Equals(rawPolicyCase7, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_007"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_VassalPolicyCommitUnchanged", "The prerequisites for the independence change have changed, or the change did not occur. AF effects and records were safely rolled back.");
				return true;
			case var rawPolicyCase8 when string.Equals(rawPolicyCase8, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_008"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PolicySubmissionFailed", "Policy submission failed.");
				return true;
			case var rawPolicyCase9 when string.Equals(rawPolicyCase9, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_009"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailIncompleteLinkedMechanism", "The policy effect returned by the model has an incomplete linked-mechanism source or destination; the system rejected it safely.");
				return true;
			case var rawPolicyCase10 when string.Equals(rawPolicyCase10, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_010"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailUnknownTargetHandle", "The model returned an unknown or unauthorized policy effect target handle; the system rejected it safely.");
				return true;
			case var rawPolicyCase11 when string.Equals(rawPolicyCase11, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_011"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailIncompatiblePair", "The policy effect capability and target handle combination returned by the model is incompatible; the system rejected it safely.");
				return true;
			case var rawPolicyCase12 when string.Equals(rawPolicyCase12, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_012"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailMissingIssuerKingdom", "The cross-kingdom policy effect returned by the model omits the issuing kingdom side; the system rejected it safely.");
				return true;
			case var rawPolicyCase13 when string.Equals(rawPolicyCase13, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_013"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailInvalidStructure", "The policy effect returned by the model is missing required fields or has incorrect field types; the system rejected it safely.");
				return true;
			case var rawPolicyCase14 when string.Equals(rawPolicyCase14, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_014"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_EffectFailGeneric", "The policy effect plan returned by the model failed structural and safety validation; the system rejected it safely.");
				return true;
			case var rawPolicyCase15 when string.Equals(rawPolicyCase15, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_015"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorRequestEmpty", "Policy request is empty.");
				return true;
			case var rawPolicyCase16 when string.Equals(rawPolicyCase16, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_016"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorApiConfigUnavailable", "The frozen policy API configuration is unavailable.");
				return true;
			case var rawPolicyCase17 when string.Equals(rawPolicyCase17, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_017"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorCheckApiSettings", "Check the policy API source or main API settings in MCM.");
				return true;
			case var rawPolicyCase18 when string.Equals(rawPolicyCase18, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_018"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorMainApiIncomplete", "The main API configuration is incomplete, so policy generation cannot start.");
				return true;
			case var rawPolicyCase19 when string.Equals(rawPolicyCase19, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_019"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorSelectedAndMainApiIncomplete", "The selected policy API and main API configurations are both incomplete, so policy generation cannot start.");
				return true;
			case var rawPolicyCase20 when string.Equals(rawPolicyCase20, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_020"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorHistoryRecall", "Policy history retrieval failed.");
				return true;
			case var rawPolicyCase21 when string.Equals(rawPolicyCase21, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_021"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorMainParse", "The policy evaluator did not return a parseable structured result.");
				return true;
			case var rawPolicyCase22 when string.Equals(rawPolicyCase22, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_022"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorDuration", "Policy review did not return a valid positive-integer duration.");
				return true;
			case var rawPolicyCase23 when string.Equals(rawPolicyCase23, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_023"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorPoliticalWeights", "Policy review did not return three valid ideological weights that are not all zero; each must be between -1 and 1.");
				return true;
			case var rawPolicyCase24 when string.Equals(rawPolicyCase24, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_024"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorEffectModuleRecall", "Policy effect module retrieval failed.");
				return true;
			case var rawPolicyCase25 when string.Equals(rawPolicyCase25, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_025"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorTimeout", "Policy review timed out after three minutes and the network request was cancelled. Check your connection and AI service status, then retry.");
				return true;
			case var rawPolicyCase26 when string.Equals(rawPolicyCase26, PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_026"), StringComparison.Ordinal):
				displayText = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyErrorException", "An exception occurred during policy review; technical details were written to the log.");
				return true;
			default:
				displayText = normalized;
				return false;
		}
	}

	private static string MapPlayerPolicyFailureReasonToDisplay(string reason)
	{
		return TryMapPlayerPolicyFailureReasonToDisplay(reason, out string displayText)
			? displayText
			: reason ?? string.Empty;
	}

	private static string BuildPlayerPolicyEffectFailureSummary(PlayerPolicyEffectValidationErrorKind errorKind)
	{
		if (errorKind == PlayerPolicyEffectValidationErrorKind.IncompleteLinkedMechanism)
		{
			return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_009");
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.UnknownOrUnauthorizedTargetHandle)
		{
			return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_010");
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.UnauthorizedModuleTargetPair)
		{
			return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_011");
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.MissingIssuerKingdomEffect)
		{
			return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_012");
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.InvalidStructure)
		{
			return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_013");
		}
		return PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_014");
	}

	private static PolicyEffectRepairScopeAllowance BuildIssuerKingdomEffectRepairScopeAllowance(
		PolicyTargetHandleDirectory directory)
	{
		PolicyEffectRepairScopeAllowance allowance = new PolicyEffectRepairScopeAllowance
		{
			RequireOriginalPairs = true,
			CompletionTargetHandle = "K0"
		};
		foreach (KeyValuePair<string, PolicyEffectCapabilityDirectoryEntry> capabilityPair in
			directory?.Capabilities ?? new Dictionary<string, PolicyEffectCapabilityDirectoryEntry>())
		{
			if (capabilityPair.Value?.AllowedTargetHandles?.Contains("K0", StringComparer.Ordinal) == true)
			{
				allowance.AllowAddedPair(capabilityPair.Key, "K0");
			}
		}
		return allowance;
	}

	/// <summary>
	/// Display-only projection for the internal policy generation failure-stage identities.
	/// Producers keep writing the internal stage names because they are also written to the log;
	/// only this player-visible projection is localized. Unknown or model-authored stages pass
	/// through verbatim instead of being guessed at.
	/// </summary>
	private static string MapPlayerPolicyFailureStageToDisplay(string stage)
	{
		string normalized = (stage ?? string.Empty).Trim();
		if (normalized.Length == 0)
		{
			return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageDefault", "Policy Evaluation");
		}
		switch (normalized)
		{
			case var rawPolicyCase27 when string.Equals(rawPolicyCase27, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_027"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageFreezeSnapshot", "Freeze Generation Snapshot");
			case var rawPolicyCase28 when string.Equals(rawPolicyCase28, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_028"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageHistoryRecall", "Historical Policy Recall");
			case var rawPolicyCase29 when string.Equals(rawPolicyCase29, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_029"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageFirstParse", "First General Review Parsing");
			case var rawPolicyCase30 when string.Equals(rawPolicyCase30, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_030"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageFirstValidation", "First General Review Validation");
			case var rawPolicyCase31 when string.Equals(rawPolicyCase31, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_031"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageEffectModuleRecall", "Effect Module Recall");
			case var rawPolicyCase32 when string.Equals(rawPolicyCase32, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_032"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageTargetParsing", "Policy Target Parsing");
			case var rawPolicyCase33 when string.Equals(rawPolicyCase33, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_033"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageEffectPlanValidation", "Effect Plan Validation");
			case var rawPolicyCase34 when string.Equals(rawPolicyCase34, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_034"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageApiRequest", "API Request");
			case var rawPolicyCase35 when string.Equals(rawPolicyCase35, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_035"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageApiOrLocal", "API Request or Local Processing");
			case var rawPolicyCase36 when string.Equals(rawPolicyCase36, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyFailureStageToDisplay_036"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyStageManualRetrySubmit", "Manual Retry Submission");
			default:
				return normalized;
		}
	}

	private static string BuildPolicyGenerationFailurePopupText(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		string requestId = (request?.RequestId ?? string.Empty).Trim();
		if (requestId.Length == 0 || requestId.Length > 80
			|| requestId.Any(character => !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != ':'))
		{
			requestId = PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRequestIdUnavailable", "Unavailable");
		}
		return PolicyUiTexts.Format("AFPOL_UI_PlayerPolicyRetryBody",
			"Stage: {STAGE}\nReason: {REASON}\nRequest ID: {REQUEST}\n\nClick \"Retry Manually\" below to resubmit the original policy, or click the X in the upper right to close.\nNo fees were deducted and no effects were applied this time.",
			"STAGE", MapPlayerPolicyFailureStageToDisplay(result?.FailureStage),
			"REASON", BuildPolicyFailurePopupText(result?.Error, result),
			"REQUEST", requestId);
	}

	private void ShowPolicyGenerationRetryPopup(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		string body = BuildPolicyGenerationFailurePopupText(request, result);
		Action retry = delegate { RetryPolicyGenerationFromFailurePopup(request); };
			if (CustomPolicyResultPopup.ShowRetry(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryTitle", "Policy Evaluation Failed"), body, PolicyUiTexts.Get("AFPOL_UI_RetryManually", "Retry Manually"), retry))
		{
			return;
		}
			InformationManager.ShowInquiry(new InquiryData(
				PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryTitle", "Policy Evaluation Failed"),
				body,
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				PolicyUiTexts.Get("AFPOL_UI_RetryManually", "Retry Manually"),
				PolicyUiTexts.Get("AFPOL_UI_Close", "Close"),
				retry,
				null),
				pauseGameActiveState: true);
	}

	private void CompletePolicyGeneration(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		if (!IsCurrentPlayerPolicyGenerationCompletion(request))
		{
			long capturedGeneration = request?.GenerationSettings?.RuntimeGeneration ?? 0L;
			SaveRuntimeGuard.IsStale(capturedGeneration, "player_policy_generation_complete");
			PolicySystemLog.Lifecycle("Player", "generation-stale-discarded", "discarded", new PolicyLogContext
			{
				RequestId = request?.RequestId,
				GenerationId = request?.RequestId,
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["capturedRuntimeGeneration"] = ClampPolicyLogCount(capturedGeneration),
					["currentRuntimeGeneration"] = ClampPolicyLogCount(SaveRuntimeGuard.CurrentGeneration),
					["stateMutations"] = 0
				}
			});
			return;
		}
		try
		{
			EndPolicyWaitPause("completed", request);
			_generationInProgress = false;
			if (result == null)
			{
				PolicySystemLog.Lifecycle("Player", "generation-failed", "null-result", new PolicyLogContext
				{
					RequestId = request?.RequestId,
					GenerationId = request?.RequestId,
					ErrorKind = "NullResult"
				});
				PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request) + " parsedEffects=0 appliedEffects=0 costDeducted=false status=null_result");
					InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryTitle", "Policy Evaluation Failed"), PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyNoResult", "Policy evaluation returned no result; no fees were deducted."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
				return;
			}
			if (!string.IsNullOrWhiteSpace(result.Error))
			{
				PolicySystemLog.Lifecycle("Player", "generation-failed", "failed", new PolicyLogContext
				{
					RequestId = request?.RequestId,
					GenerationId = request?.RequestId,
					ErrorKind = "Generation",
					MessageChars = result.Error.Length,
					MessageHash = PolicySystemLog.HashSensitive(result.Error),
					Counts = new Dictionary<string, int>(StringComparer.Ordinal)
					{
						["parsedEffects"] = CountParsedPolicyEffects(result)
					}
				});
				PolicyDebugLog("complete-failed", BuildPolicyRequestLogPrefix(request) + " generation error: " + result.Error);
				PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request)
					+ " parsedEffects=" + CountParsedPolicyEffects(result).ToString(CultureInfo.InvariantCulture)
					+ " appliedEffects=0 costDeducted=false status=generation_failed");
				ShowPolicyGenerationRetryPopup(request, result);
				return;
			}
			PolicySystemLog.Lifecycle("Player", "generation-complete", "success", new PolicyLogContext
			{
				RequestId = request?.RequestId,
				GenerationId = request?.RequestId,
				CampaignDay = request?.SubmittedDay,
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["parsedEffects"] = CountParsedPolicyEffects(result)
				}
			});
			if (IsLocalPolicyRequest(request))
			{
				CompleteLocalPolicyGeneration(request, result);
				return;
			}
			if (IsVassalPolicyRequest(request))
			{
				CompleteVassalPolicyGeneration(request, result);
				return;
			}
			if (!TryPreparePolicyCostForApplication(request, result.MainAssessment, out string costError))
			{
				PolicyDebugLog("policy-cost-invalid", BuildPolicyRequestLogPrefix(request)
					+ " useAiEvaluatedCost=" + request.UseAiEvaluatedCost.ToString(CultureInfo.InvariantCulture)
					+ " error=" + (costError ?? ""),
					SafeSerializeForDebug(result.MainAssessment));
					InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyRetryTitle", "Policy Evaluation Failed"), BuildPolicyFailurePopupText(costError ?? PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_002"), result) + PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyNoFeesNoEffectsSuffix", "\n\nNo fees were deducted and no effects were applied."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
				return;
			}
			result.Postprocess = BuildPostprocessResultFromMainAssessment(request, result.MainAssessment);
			if (string.IsNullOrWhiteSpace(result.PostprocessRaw))
			{
				result.PostprocessRaw = SafeSerializeForDebug(result.Postprocess);
			}
			PolicyEligibility eligibility = EvaluateEligibility(request);
			if (!eligibility.CanPublish)
			{
				PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request)
					+ " parsedEffects=" + CountParsedPolicyEffects(result).ToString(CultureInfo.InvariantCulture)
					+ " appliedEffects=0 costDeducted=false status=eligibility_changed reason=" + (eligibility.Reason ?? ""));
					InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyCannotPublishTitle", "Policy Cannot Be Published"), BuildPolicyFailurePopupText(eligibility.Reason, result) + PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyConditionsChangedAfterEval", "\n\nEvaluation is complete, but publication conditions changed, so no fees were deducted and no effects were applied."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
				return;
			}
			PolicyApplicationResult application = ApplyPolicyEffects(request, result.Postprocess);
			string feedback = ResolveFeedbackText(result, request);
			string recordId = Guid.NewGuid().ToString("N");
			if (!TryReadPoliticalWeights(result.MainAssessment?.AuthoritarianWeight, result.MainAssessment?.OligarchicWeight, result.MainAssessment?.EgalitarianWeight,
				out float authoritarianWeight, out float oligarchicWeight, out float egalitarianWeight))
			{
				throw new InvalidOperationException(PolicyCompatibilityTexts.Get("GenerationRaw_CompletePolicyGeneration_019"));
			}
		if (!TryBuildPendingPlayerPolicyAgendaSaveData(
				request,
				result.MainAssessment,
				result.Postprocess,
				feedback,
				out PendingPlayerPolicyAgendaSaveData pending,
				out string pendingError))
		{
			PolicySystemLog.DiagnosticFailure(
				"Player",
				"policy-agenda-snapshot-failed",
				BuildPolicyRequestLogPrefix(request) + " error=" + (pendingError ?? string.Empty),
				BuildPendingPlayerPolicyAgendaDiagnosticSnapshot(
					request,
					result.MainAssessment,
					result.Postprocess,
					pendingError));
			throw new InvalidOperationException(PolicyCompatibilityTexts.Get("GenerationRaw_CompletePolicyGeneration_020") + pendingError);
		}
			DynamicPolicySaveData dynamicPolicy = new DynamicPolicySaveData
			{
				Version = 4,
				CommitState = "pending",
				PolicyObjectId = DynamicPolicyIdPrefix + recordId,
				RecordId = recordId,
				ReReviewRootRecordId = request.ReReviewRootRecordId ?? string.Empty,
				ReReviewSourceRecordId = request.ReReviewSourceRecordId ?? string.Empty,
				SupersedesRecordId = request.SupersedesRecordId ?? string.Empty,
				ReReviewReplacementCommitted = false,
				Source = "player",
				OwnerKingdomId = request.PlayerKingdomId ?? "",
				ProposerClanId = Clan.PlayerClan?.StringId ?? "",
				IssuerKingdomId = request.IssuerKingdomId ?? request.PlayerKingdomId ?? "",
				PolicyName = request.PolicyName ?? "",
				PolicyContent = request.PolicyContent ?? "",
				LogEntryDescription = FirstNonEmpty(result.MainAssessment?.PolicyContentDigest, request.PolicyContent),
				SecondaryEffects = BuildRawPolicyEffectSummary(application)
					+ "\n" + BuildPlayerPolicyGoldCostSummary(request),
				AuthoritarianWeight = authoritarianWeight,
				OligarchicWeight = oligarchicWeight,
				EgalitarianWeight = egalitarianWeight,
				VotePersonality = PolicyVotePersonality.CloneValidated(result.MainAssessment.VotePersonality),
				Status = DynamicPolicyStatusPending,
				CreatedUtcTicks = DateTime.UtcNow.Ticks,
				PlayerPayloadJson = JsonConvert.SerializeObject(pending)
			};
			PolicySystemLog.Lifecycle("Player", "pending-created", "pending", new PolicyLogContext
			{
				RequestId = request?.RequestId,
				GenerationId = request?.RequestId,
				PolicyId = dynamicPolicy.PolicyObjectId,
				RecordId = recordId,
				StateBefore = "generated",
				StateAfter = dynamicPolicy.CommitState,
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["moduleEffects"] = pending?.ModuleEffects?.Count ?? 0
				}
			});
			SubmitPlayerPolicyAgenda(request, result, application, dynamicPolicy);
		}
		catch (Exception ex)
		{
			_generationInProgress = false;
			EndPolicyWaitPause("exception", request);
			PolicyDebugLog("complete-exception", BuildPolicyRequestLogPrefix(request), ex.ToString());
			PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request) + " parsedEffects=0 appliedEffects=0 costDeducted=false status=exception");
			Log("complete policy failed: " + ex);
				InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyPublishFailedTitle", "Policy Publication Failed"), BuildPolicyFailurePopupText(PolicyCompatibilityTexts.Get("Legacy_TryMapPlayerPolicyFailureReasonToDisplay_003"), result) + PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyDoNotRepeatClick", "\n\nDo not click repeatedly unless success is confirmed; please check the log."), true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), "", null, null), pauseGameActiveState: true);
		}
	}

	private static bool IsCurrentPlayerPolicyGenerationCompletion(PolicyDraftRequest request)
	{
		long capturedGeneration = request?.GenerationSettings?.RuntimeGeneration ?? 0L;
		return capturedGeneration <= 0L || SaveRuntimeGuard.IsCurrentGeneration(capturedGeneration);
	}

	private static int ClampPolicyLogCount(long value)
	{
		return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
	}

	private void SubmitPlayerPolicyAgenda(
		PolicyDraftRequest request,
		PolicyGenerationResult result,
		PolicyApplicationResult application,
		DynamicPolicySaveData dynamicPolicy)
	{
		string recordId = dynamicPolicy?.RecordId ?? string.Empty;
		try
		{
			PolicyEligibility eligibility = EvaluateEligibility(request);
			if (!eligibility.CanPublish)
			{
				PolicyDebugLog("policy-agenda-submit-rejected", BuildPolicyRecordLogPrefix(request, recordId)
					+ " costDeducted=false status=eligibility_changed reason=" + (eligibility.Reason ?? string.Empty));
				InformationManager.ShowInquiry(new InquiryData(
					PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyCannotSubmitTitle", "Policy Cannot Be Submitted"),
					BuildPolicyFailurePopupText(eligibility.Reason, result)
						+ PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyConditionsChangedBeforeConfirm", "\n\nPublication conditions changed before confirmation, so no fees were deducted and no effects were applied."),
					true,
					false,
					PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"),
					string.Empty,
					null,
					null),
					pauseGameActiveState: true);
				return;
			}
			if (!TrySubmitDynamicPolicyAgenda(dynamicPolicy, out string agendaError))
			{
				throw new InvalidOperationException("Policy submission to AF agenda failed: " + agendaError);
			}
			string impactText = PolicyUiTexts.Format("AFPOL_UI_PlayerPolicySubmittedToAgendaBody",
				"Policy \"{NAME}\" has been submitted to the AF kingdom agenda. Policy costs are not deducted and no numeric effects occur until the agenda passes.\n{COST}",
				"NAME", request?.PolicyName ?? string.Empty,
				"COST", BuildPlayerPolicyGoldCostSummaryForDisplay(request));
			PolicyDebugLog("complete-agenda-submitted", BuildPolicyRecordLogPrefix(request, recordId)
				+ " costDeducted=false status=pending", impactText);
			PolicyDebugLog("policy-complete", BuildPolicyRecordLogPrefix(request, recordId)
				+ " parsedEffects=" + CountParsedPolicyEffects(result).ToString(CultureInfo.InvariantCulture)
				+ " appliedEffects=" + (application?.AppliedEffectCount ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " costDeducted=false status=agenda_pending");
				InformationManager.ShowInquiry(new InquiryData(PolicyUiTexts.Get("AFPOL_UI_PlayerPolicySubmittedToAgendaTitle", "Policy Submitted to Agenda"), impactText, true, false, PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"), string.Empty, null, null), pauseGameActiveState: true);
			Log("policy agenda submitted " + BuildPolicyRecordLogPrefix(request, recordId)
				+ " effects=" + (application?.AppliedEffectCount ?? 0).ToString(CultureInfo.InvariantCulture));
		}
		catch (Exception ex)
		{
			PolicyDebugLog("policy-agenda-submit-failed", BuildPolicyRecordLogPrefix(request, recordId), ex.ToString());
			InformationManager.ShowInquiry(new InquiryData(
				PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyAgendaSubmitFailedTitle", "Policy Submission Failed"),
				PolicyUiTexts.Get("AFPOL_UI_PlayerPolicyAgendaSubmitFailedBody", "The policy could not be submitted to the agenda; technical details have been written to the log.\n\nNo fees were deducted and no effects were applied."),
				true,
				false,
				PolicyUiTexts.Get("AFPOL_UI_Acknowledge", "Got it"),
				string.Empty,
				null,
				null),
				pauseGameActiveState: true);
		}
	}

	private static PolicyRuntimeOptions BuildPolicyRuntimeOptions()
	{
		bool isDefault;
		string evaluatorPrompt = DuelSettings.GetCustomPolicyEvaluatorPromptForExternal(out isDefault);
		return new PolicyRuntimeOptions
		{
			GoldCost = Math.Max(0, DuelSettings.GetCustomPolicyGoldCostForExternal()),
			UseAiEvaluatedCost = DuelSettings.IsAiEvaluatedCustomPolicyCostEnabledForExternal(),
			EvaluatorPrompt = string.IsNullOrWhiteSpace(evaluatorPrompt) ? "" : evaluatorPrompt.Trim(),
			EvaluatorPromptIsDefault = isDefault,
			PublicFeedbackTargetChars = NormalizePolicyPublicFeedbackTargetChars(DuelSettings.GetCustomPolicyPublicFeedbackTargetCharsForExternal())
		};
	}

	private static PolicyRuntimeOptions BuildPolicyRuntimeOptions(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return BuildPolicyRuntimeOptions();
		}
		return new PolicyRuntimeOptions
		{
			GoldCost = Math.Max(0, request.GoldCost),
			UseAiEvaluatedCost = request.UseAiEvaluatedCost,
			EvaluatorPrompt = request.EvaluatorPrompt ?? "",
			EvaluatorPromptIsDefault = request.EvaluatorPromptIsDefault,
			PublicFeedbackTargetChars = NormalizePolicyPublicFeedbackTargetChars(request.PublicFeedbackTargetChars)
		};
	}

	private static string BuildReadyStatus(PolicyRuntimeOptions options)
	{
		if (options?.UseAiEvaluatedCost == true)
		{
			return PolicyUiTexts.Format(
				"AFPOL_UI_PolicyReadyAiCostStatus",
				"Fill in a policy name and content to publish. The intelligence service will estimate the denars required for full execution; if your funds are insufficient, {RESERVE} denars will be reserved for you and all effects will be scaled to your actual investment.",
				"RESERVE", AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture));
		}
		return PolicyUiTexts.Format(
			"AFPOL_UI_PolicyReadyFixedCostStatus",
			"Fill in a policy name and content to publish. The configured cost is deducted after evaluation succeeds and the policy takes effect: {COST}. There is no cooldown, so policies can be published consecutively.",
			"COST", FormatCostTextForDisplay(options));
	}

	private static string FormatCostText(PolicyRuntimeOptions options)
	{
		if (options == null)
		{
			options = BuildPolicyRuntimeOptions();
		}
		return FormatGoldCostText(options.GoldCost);
	}

	private static string FormatCostText(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return FormatCostText(BuildPolicyRuntimeOptions());
		}
		return FormatGoldCostText(request.GoldCost);
	}

	private static string FormatGoldCostText(int goldCost)
	{
		return goldCost > 0
			? goldCost.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_FormatGoldCostText_037")
			: PolicyCompatibilityTexts.Get("Legacy_FormatGoldCostText_038");
	}

	private static string FormatCostText(int goldCost, float influenceCost)
	{
		bool hasGold = goldCost > 0;
		bool hasInfluence = influenceCost > 0.0001f;
		if (!hasGold && !hasInfluence)
		{
			return PolicyCompatibilityTexts.Get("Legacy_FormatCostText_039");
		}
		if (hasGold && hasInfluence)
		{
			return goldCost.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_FormatCostText_040") + FormatNumber(influenceCost) + PolicyCompatibilityTexts.Get("Legacy_FormatCostText_041");
		}
		if (hasGold)
		{
			return goldCost.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_FormatGoldCostText_037");
		}
		return FormatNumber(influenceCost) + PolicyCompatibilityTexts.Get("Legacy_FormatCostText_041");
	}

	private static string FormatCostTextForDisplay(PolicyRuntimeOptions options)
	{
		options ??= BuildPolicyRuntimeOptions();
		return FormatGoldCostTextForDisplay(options.GoldCost);
	}

	private static string FormatGoldCostTextForDisplay(int goldCost)
	{
		return goldCost > 0
			? PolicyUiTexts.Format("AFPOL_UI_CostGoldAmount", "{AMOUNT} denars", "AMOUNT", goldCost.ToString(CultureInfo.InvariantCulture))
			: PolicyUiTexts.Get("AFPOL_UI_CostGoldNone", "No denars");
	}

	private static string MapPlayerPolicyEligibilityReasonToDisplay(string reason)
	{
		string normalized = (reason ?? string.Empty).Trim();
		switch (normalized)
		{
			case var rawPolicyCase37 when string.Equals(rawPolicyCase37, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_042"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PreviousPolicyPending", "The previous policy is still awaiting evaluation; please wait.");
			case var rawPolicyCase38 when string.Equals(rawPolicyCase38, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_043"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_PolicyRequiresKingdom", "Join a kingdom before publishing a kingdom policy.");
			case var rawPolicyCase39 when string.Equals(rawPolicyCase39, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_044"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_OnlyKingdomRulerCanPublish", "Only a kingdom ruler can publish national policies; non-rulers who own a town or castle should use local policies.");
			case var rawPolicyCase40 when string.Equals(rawPolicyCase40, PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_045"), StringComparison.Ordinal):
				return PolicyUiTexts.Get("AFPOL_UI_NoOwnedFiefsWarning", "The player clan currently owns no towns or castles; cannot publish.");
		}
		if (TryMapPolicyGoldCostEligibilityReason(
			normalized,
			PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_046"),
			"AFPOL_UI_KingdomPolicyCostRequired",
			"Publishing this policy requires {AMOUNT} denars.",
			out string kingdomCostReason))
		{
			return kingdomCostReason;
		}
		if (TryMapPolicyGoldCostEligibilityReason(
			normalized,
			PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_047"),
			"AFPOL_UI_LocalPolicyCostRequired",
			"Publishing this local policy requires {AMOUNT} denars.",
			out string localCostReason))
		{
			return localCostReason;
		}
		return normalized;
	}

	private static bool TryMapPolicyGoldCostEligibilityReason(
		string reason,
		string prefix,
		string key,
		string fallback,
		out string displayText)
	{
		displayText = string.Empty;
		string suffix = PolicyCompatibilityTexts.Get("Legacy_TryMapPolicyGoldCostEligibilityReason_048");
		if (!reason.StartsWith(prefix, StringComparison.Ordinal)
			|| !reason.EndsWith(suffix, StringComparison.Ordinal))
		{
			return false;
		}
		string amountText = reason.Substring(prefix.Length, reason.Length - prefix.Length - suffix.Length);
		if (!int.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out int amount))
		{
			return false;
		}
		displayText = PolicyUiTexts.Format(key, fallback, "AMOUNT", amount.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private PolicyEligibility EvaluateEligibility(PolicyDraftRequest request)
	{
		return EvaluateEligibility(BuildPolicyRuntimeOptions(request));
	}

	private PolicyEligibility EvaluateEligibility(PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		if (_generationInProgress)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_042"));
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		if (playerKingdom == null)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_043"));
		}
		if (!IsPlayerRuler(playerKingdom))
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_044"));
		}
		if (options.UseAiEvaluatedCost)
		{
			return PolicyEligibility.Allowed();
		}
		if ((Hero.MainHero?.Gold ?? 0) < options.GoldCost)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_046") + options.GoldCost.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_TryMapPolicyGoldCostEligibilityReason_048"));
		}
		return PolicyEligibility.Allowed();
	}

	private PolicyEligibility EvaluateLocalPolicyEligibility(PolicyRuntimeOptions options, bool hasOwnedFief)
	{
		options ??= BuildPolicyRuntimeOptions();
		if (_generationInProgress)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_042"));
		}
		if (!hasOwnedFief)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_045"));
		}
		int currentGold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		if (options.UseAiEvaluatedCost)
		{
			return PolicyEligibility.Allowed();
		}
		if (currentGold < options.GoldCost)
		{
			return PolicyEligibility.Blocked(PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyEligibilityReasonToDisplay_047") + options.GoldCost.ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_TryMapPolicyGoldCostEligibilityReason_048"));
		}
		return PolicyEligibility.Allowed();
	}

	private sealed class PolicyPublishCostReceipt
	{
		internal Hero GoldPayer;

		internal Clan InfluencePayer;

		internal int DeductedGold;

		internal float DeductedInfluence;
	}

	private void DeductPublishCost(PolicyDraftRequest request)
	{
		DeductPublishCost(request, new PolicyPublishCostReceipt());
	}

	private void DeductPublishCost(PolicyDraftRequest request, PolicyPublishCostReceipt receipt)
	{
		if (receipt == null)
		{
			throw new ArgumentNullException(nameof(receipt));
		}
		int goldCost = Math.Max(0, request?.GoldCost ?? 0);
		float influenceCost = request?.InfluenceCost ?? 0f;
		if (float.IsNaN(influenceCost) || float.IsInfinity(influenceCost) || influenceCost < 0f)
		{
			throw new InvalidOperationException("Policy influence payment value invalid");
		}
		Hero goldPayer = Hero.MainHero;
		Clan influencePayer = Clan.PlayerClan;
		if (goldCost > 0 && (goldPayer == null || goldPayer.Gold < goldCost))
		{
			throw new InvalidOperationException("Insufficient player denars or player hero does not exist when paying policy cost");
		}
		if (influenceCost > 0f && (influencePayer == null || influencePayer.Influence + 0.0001f < influenceCost))
		{
			throw new InvalidOperationException("Insufficient player influence or player clan does not exist when paying policy cost");
		}
		receipt.GoldPayer = goldPayer;
		receipt.InfluencePayer = influencePayer;
		receipt.DeductedGold = 0;
		receipt.DeductedInfluence = 0f;
		int goldBefore = goldPayer?.Gold ?? 0;
		float influenceBefore = influencePayer?.Influence ?? 0f;
		PolicyDebugLog("deduct-cost", BuildPolicyRequestLogPrefix(request)
			+ " goldCost=" + goldCost.ToString(CultureInfo.InvariantCulture)
			+ " influenceCost=" + influenceCost.ToString("0.###", CultureInfo.InvariantCulture));
		try
		{
			if (goldCost > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(goldPayer, null, goldCost, true);
			}
			if (influenceCost > 0f)
			{
				ChangeClanInfluenceAction.Apply(influencePayer, -influenceCost);
			}
		}
		catch (Exception ex)
		{
			Log("deduct policy cost failed " + BuildPolicyRequestLogPrefix(request) + " error=" + ex.Message);
			throw;
		}
		finally
		{
			receipt.DeductedGold = goldPayer == null ? 0 : Math.Max(0, goldBefore - goldPayer.Gold);
			receipt.DeductedInfluence = influencePayer == null
				? 0f
				: Math.Max(0f, influenceBefore - influencePayer.Influence);
		}
		if (receipt.DeductedGold != goldCost
			|| Math.Abs(receipt.DeductedInfluence - influenceCost) > 0.0001f)
		{
			throw new InvalidOperationException("Policy payment did not deduct expected amount precisely");
		}
		PolicySystemLog.Lifecycle("Player", "cost-committed", "success", new PolicyLogContext
		{
			RequestId = request?.RequestId,
			GenerationId = request?.RequestId,
			Gold = receipt.DeductedGold,
			Influence = receipt.DeductedInfluence
		});
	}

	private static bool TryRefundPublishCost(PolicyPublishCostReceipt receipt, out string failureReason)
	{
		List<string> failures = new List<string>();
		if ((receipt?.DeductedGold ?? 0) > 0)
		{
			if (receipt.GoldPayer == null)
			{
				failures.Add("Denar payer is missing");
			}
			else
			{
				int before = receipt.GoldPayer.Gold;
				try
				{
					receipt.GoldPayer.ChangeHeroGold(receipt.DeductedGold);
				}
				catch (Exception ex)
				{
					failures.Add("Exception while refunding denars: " + ex.Message);
				}
				int refunded = Math.Max(0, receipt.GoldPayer.Gold - before);
				if (refunded != receipt.DeductedGold)
				{
					failures.Add("Expected denar refund " + receipt.DeductedGold.ToString(CultureInfo.InvariantCulture)
						+ "; actually refunded " + refunded.ToString(CultureInfo.InvariantCulture));
				}
			}
		}
		if ((receipt?.DeductedInfluence ?? 0f) > 0.0001f)
		{
			if (receipt.InfluencePayer == null)
			{
				failures.Add("Influence payer clan is missing");
			}
			else
			{
				float before = receipt.InfluencePayer.Influence;
				try
				{
					ChangeClanInfluenceAction.Apply(receipt.InfluencePayer, receipt.DeductedInfluence);
				}
				catch (Exception ex)
				{
					failures.Add("Exception while refunding influence: " + ex.Message);
				}
				float refunded = Math.Max(0f, receipt.InfluencePayer.Influence - before);
				if (Math.Abs(refunded - receipt.DeductedInfluence) > 0.0001f)
				{
					failures.Add("Expected influence refund " + receipt.DeductedInfluence.ToString("0.###", CultureInfo.InvariantCulture)
						+ "; actually refunded " + refunded.ToString("0.###", CultureInfo.InvariantCulture));
				}
			}
		}
		failureReason = string.Join("; ", failures);
		bool success = failures.Count == 0;
		PolicySystemLog.Lifecycle("Player", success ? "cost-refunded" : "cost-compensated", success ? "success" : "failed", new PolicyLogContext
		{
			Gold = receipt?.DeductedGold ?? 0,
			Influence = receipt?.DeductedInfluence ?? 0f,
			ErrorKind = success ? null : "RefundMismatch",
			MessageChars = failureReason?.Length ?? 0,
			MessageHash = PolicySystemLog.HashSensitive(failureReason)
		});
		return success;
	}

	private static bool TryPreparePolicyCostForApplication(PolicyDraftRequest request, PolicyMainAssessmentResult assessment, out string error)
	{
		error = "";
		if (request == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryPreparePolicyCostForApplication_021");
			return false;
		}
		if (!request.UseAiEvaluatedCost)
		{
			request.RequiredGoldCost = Math.Max(0, request.GoldCost);
			request.DailyMaintenanceGoldCost = 0;
			request.RequiredInfluenceCost = 0f;
			request.InfluenceCost = 0f;
			request.GoldEffectScale = 1f;
			request.InfluenceEffectScale = request.GoldEffectScale;
			return true;
		}
		if (!TryReadAiPolicyGoldCosts(assessment, out int requiredGoldCost, out int dailyMaintenanceGoldCost, out error))
		{
			return false;
		}
		int currentGold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		int availableGold = Math.Max(0, currentGold - AiPolicyGoldReserve);
		int actualGoldCost = Math.Min(requiredGoldCost, availableGold);
		request.RequiredGoldCost = requiredGoldCost;
		request.DailyMaintenanceGoldCost = dailyMaintenanceGoldCost;
		request.RequiredInfluenceCost = 0f;
		request.GoldCost = actualGoldCost;
		request.InfluenceCost = 0f;
		request.GoldEffectScale = CalculatePolicyCostScale(requiredGoldCost, actualGoldCost);
		request.InfluenceEffectScale = request.GoldEffectScale;
		return true;
	}

	private static bool TryPrepareLocalPolicyCostForApplication(PolicyDraftRequest request, PolicyMainAssessmentResult assessment, out string error)
	{
		error = "";
		if (request == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryPrepareLocalPolicyCostForApplication_022");
			return false;
		}
		if (!request.UseAiEvaluatedCost)
		{
			request.RequiredGoldCost = Math.Max(0, request.GoldCost);
			request.DailyMaintenanceGoldCost = 0;
			request.RequiredInfluenceCost = 0f;
			request.InfluenceCost = 0f;
			request.GoldEffectScale = 1f;
			request.InfluenceEffectScale = 1f;
			return true;
		}
		if (!TryReadAiPolicyGoldCosts(assessment, out int requiredGoldCost, out int dailyMaintenanceGoldCost, out error))
		{
			return false;
		}
		int currentGold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		int availableGold = Math.Max(0, currentGold - LocalPolicyGoldReserve);
		int actualGoldCost = Math.Min(requiredGoldCost, availableGold);
		request.RequiredGoldCost = requiredGoldCost;
		request.DailyMaintenanceGoldCost = dailyMaintenanceGoldCost;
		request.RequiredInfluenceCost = 0f;
		request.GoldCost = actualGoldCost;
		request.InfluenceCost = 0f;
		request.GoldEffectScale = CalculatePolicyCostScale(requiredGoldCost, actualGoldCost);
		request.InfluenceEffectScale = request.GoldEffectScale;
		return true;
	}

	private static bool TryReadAiPolicyGoldCosts(
		PolicyMainAssessmentResult assessment,
		out int startupGoldCost,
		out int dailyMaintenanceGoldCost,
		out string error)
	{
		startupGoldCost = 0;
		dailyMaintenanceGoldCost = 0;
		error = "";
		if (assessment?.StartupGoldCost == null || assessment.DailyMaintenanceGoldCost == null)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryReadAiPolicyGoldCosts_023");
			return false;
		}
		float rawStartupGold = assessment.StartupGoldCost.Value;
		float rawDailyGold = assessment.DailyMaintenanceGoldCost.Value;
		if (float.IsNaN(rawStartupGold) || float.IsInfinity(rawStartupGold) || rawStartupGold < 0f
			|| float.IsNaN(rawDailyGold) || float.IsInfinity(rawDailyGold) || rawDailyGold < 0f)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryReadAiPolicyGoldCosts_024");
			return false;
		}
		startupGoldCost = rawStartupGold <= 0f ? 0 : (int)Math.Min(int.MaxValue, Math.Ceiling(rawStartupGold));
		dailyMaintenanceGoldCost = rawDailyGold <= 0f ? 0 : (int)Math.Min(int.MaxValue, Math.Ceiling(rawDailyGold));
		return true;
	}

	private static bool IsPermanentPlayerPolicyEffect(PolicyDraftRequest request)
	{
		return request != null
			&& (request.IsPermanentEffect || request.ManualDurationDays <= 0);
	}

	private static string BuildPolicyKnowledgeContextForMainOnly(PolicyDraftRequest request)
	{
		try
		{
			string query = BuildPolicyKnowledgeQueryForMainOnly(request);
			string secondaryInput = BuildPolicyKnowledgeSecondaryInputForMainOnly(request);
			if (string.IsNullOrWhiteSpace(query))
			{
				return "";
			}
			MentionedWorldEntities mentionedEntities = request?.KnowledgeMentionedEntities;
			string rawContext = AIConfigHandler.GetLoreContext(query, Hero.MainHero, secondaryInput, mentionedEntities);
			string context = CompressPolicyKnowledgeContext(rawContext);
			return (context ?? "").Trim();
		}
		catch (Exception ex)
		{
			PolicyDebugLog("policy-knowledge-failed", BuildPolicyRequestLogPrefix(request), ex.ToString());
			return "";
		}
	}

	private static string BuildPolicyKnowledgeQueryForMainOnly(PolicyDraftRequest request)
	{
		List<string> parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(request?.PolicyName))
		{
			parts.Add(PolicyGenerationPromptTexts.Get("PolicyName", "Policy name: ") + request.PolicyName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(request?.PlayerKingdomName))
		{
			parts.Add(PolicyGenerationPromptTexts.Get("PlayerKingdom", "Player kingdom: ") + request.PlayerKingdomName.Trim());
		}
		string content = CompactPolicyContextText(request?.PolicyContent ?? "");
		if (!string.IsNullOrWhiteSpace(content))
		{
			parts.Add(PolicyGenerationPromptTexts.Get("PolicyContent", "Policy content: ") + LimitDisplayChars(content, 700));
		}
		string entityHints = BuildPolicyExplicitEntityHintText(request);
		if (!string.IsNullOrWhiteSpace(entityHints))
		{
			parts.Add(entityHints);
		}
		return LimitDisplayChars(CompactPolicyContextText(string.Join(PolicyGenerationPromptTexts.Get("FieldSeparator", "; "), parts)), 1000);
	}

	private static string BuildPolicyKnowledgeSecondaryInputForMainOnly(PolicyDraftRequest request)
	{
		PolicyPromptContextBundle context = request?.PromptContext ?? new PolicyPromptContextBundle();
		List<string> parts = new List<string>();
		parts.Add(PolicyGenerationPromptTexts.Get("CurrentDate", "Current date: ") + PolicyDateTextProjection.ForPrompt(string.IsNullOrWhiteSpace(request?.DateText) ? FormatCurrentCampaignDate() : request.DateText.Trim()));
		parts.Add(PolicyGenerationPromptTexts.Get("PlayerKingdom", "Player kingdom: ") + (request?.PlayerKingdomName ?? "") + " | ID=" + (request?.PlayerKingdomId ?? ""));
		parts.Add(PolicyGenerationPromptTexts.Get("KnowledgeStageBoundary", "Custom policy pipeline: the first request performs only general policy assessment, causal analysis, consequences for each party, numeric intent in natural language, costs and duration, and evidence for subsequent module retrieval. It does not decide final effect types, targets, or execution values; a later independent request determines final effects."));
		if (!string.IsNullOrWhiteSpace(context.PolicyRuleContext))
		{
			parts.Add(PolicyGenerationPromptTexts.Get("PipelineRules", "Policy pipeline rules: ") + CompactPolicyContextText(context.PolicyRuleContext));
		}
		if (!string.IsNullOrWhiteSpace(context.WorldContextCompact))
		{
			parts.Add(PolicyGenerationPromptTexts.Get("CompactWorld", "Compact world context: ") + LimitDisplayChars(CompactPolicyContextText(context.WorldContextCompact), 1600));
		}
		return LimitDisplayChars(CompactPolicyContextText(string.Join(PolicyGenerationPromptTexts.Get("FieldSeparator", "; "), parts)), 2400);
	}

	private static async Task<string> CallPlayerPolicyApiOrThrowAsync(
		List<object> messages,
		PolicyApiExecutionProfile profile,
		long runtimeGeneration,
		CancellationToken cancellationToken,
		string source,
		int? maxTokensOverride = null,
		string requestId = null)
	{
		PolicyApiExecutionProfile callProfile = profile;
		if (profile != null && maxTokensOverride.HasValue)
		{
			callProfile = profile.Clone();
			callProfile.MaxTokens = Math.Max(1, Math.Min(profile.MaxTokens, maxTokensOverride.Value));
		}
		JArray messageArray = messages == null ? new JArray() : JArray.FromObject(messages);
		string resolvedSource = string.IsNullOrWhiteSpace(source) ? "PlayerPolicy" : source;
		NpcPolicyApiCallResult apiResult;
		Func<string, string, long, Task<string>> testOverride = PlayerPolicyApiTextOverrideForTests;
		if (testOverride == null)
		{
			PromptPackage prompt = LegacyPolicyLlmGateway.BuildPromptPackage(
				messageArray,
				Math.Max(1, callProfile?.MaxTokens ?? 1),
				callProfile?.ModelName);
			InteractionStage stage = resolvedSource.IndexOf("Postprocess", StringComparison.OrdinalIgnoreCase) >= 0
				? InteractionStage.Postprocess
				: InteractionStage.MainReply;
			LlmGenerateRequest gatewayRequest = LegacyPolicyLlmGateway.BuildRequest(
				prompt,
				callProfile,
				resolvedSource,
				runtimeGeneration,
				PlayerPolicyEvaluationTimeoutMilliseconds,
				stage);
			LlmGenerateResult gatewayResult = await new LegacyPolicyLlmGateway(false, callProfile)
				.GenerateAsync(gatewayRequest, cancellationToken).ConfigureAwait(false);
			apiResult = LegacyPolicyLlmGateway.ToLegacyResult(gatewayResult);
		}
		else
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				string content = await testOverride(
					messageArray.ToString(Formatting.None),
					resolvedSource,
					runtimeGeneration);
				apiResult = new NpcPolicyApiCallResult
				{
					Success = content != null,
					Content = content ?? string.Empty,
					ErrorMessage = content == null ? "contract API override returned null" : string.Empty,
					AttemptsUsed = 1,
					ResolvedRoute = "contract-override"
				};
			}
			catch (Exception ex)
			{
				apiResult = new NpcPolicyApiCallResult
				{
					Success = false,
					ErrorMessage = ex.Message,
					AttemptsUsed = 1,
					ResolvedRoute = "contract-override"
				};
			}
		}
		int cacheHit = Math.Max(0, apiResult?.PromptCacheHitTokens ?? 0);
		int cacheMiss = Math.Max(0, apiResult?.PromptCacheMissTokens ?? 0);
		int cacheTotal = cacheHit + cacheMiss;
		PolicySystemLog.Write("Player", "llm-stage-usage",
			"requestId=" + (requestId ?? string.Empty)
			+ " stage=" + (string.IsNullOrWhiteSpace(source) ? "PlayerPolicy" : source)
			+ " routeHash=" + PolicyTextEmbeddingSession.StableTextHash(
				apiResult?.ResolvedRoute ?? callProfile?.ResolvedRoute ?? "")
			+ " maxTokens=" + Math.Max(0, callProfile?.MaxTokens ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " promptTokens=" + (apiResult?.PromptTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " completionTokens=" + (apiResult?.CompletionTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " cacheHitTokens=" + (apiResult?.PromptCacheHitTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " cacheMissTokens=" + (apiResult?.PromptCacheMissTokens?.ToString(CultureInfo.InvariantCulture) ?? "")
			+ " cacheHitRate=" + (cacheTotal <= 0 ? "n/a" : (100d * cacheHit / cacheTotal).ToString("F1", CultureInfo.InvariantCulture) + "%")
			+ " success=" + ((apiResult?.Success ?? false) ? "true" : "false"));
		if (apiResult?.Success == true)
		{
			return apiResult.Content ?? "";
		}
		throw new InvalidOperationException(string.IsNullOrWhiteSpace(apiResult?.ErrorMessage) ? "Policy API request failed." : apiResult.ErrorMessage);
	}

	private static List<object> BuildMainMessages(PolicyDraftRequest request, string knowledgeContext)
	{
		PolicyPromptContextBundle context = request?.PromptContext ?? new PolicyPromptContextBundle();
		bool isLocalPolicy = IsLocalPolicyRequest(request);
		bool isVassalPolicy = IsVassalPolicyRequest(request);
		bool isPermanent = IsPermanentPlayerPolicyEffect(request);
		bool useAiCost = request?.UseAiEvaluatedCost == true;
		int feedbackChars = NormalizePolicyPublicFeedbackTargetChars(
			request?.PublicFeedbackTargetChars ?? PolicyPublicFeedbackTargetDefaultChars);
		string policyRules = string.IsNullOrWhiteSpace(context.PolicyRuleContext)
			? BuildPolicyRuleContext()
			: context.PolicyRuleContext;
		string scopeRules = isLocalPolicy
			? PolicyGenerationPromptTexts.Get("LocalAssessment", "[Local policy assessment] Assess only possible direct effects on the frozen publication location and objects explicitly identified in the original text; do not expand the scope.")
				+ (isPermanent
					? PolicyGenerationPromptTexts.Get("PermanentDuration", "Duration is fixed to permanent, durationDays=0.")
					: PolicyGenerationPromptTexts.Get("FiniteDuration", "Duration is fixed to finite, durationDays=") + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("FullStop", "."))
			: isVassalPolicy
				? PolicyGenerationPromptTexts.Get("VassalAssessment", "[Vassal policy assessment] Judge vassalIndependenceDelta (-15..15) based on the vassal kingdom's interests, autonomy, dignity, burdens, and security: benefits or approval are negative, harm, humiliation, or oppression are positive, and neutrality is 0; ")
					+ (isPermanent
						? PolicyGenerationPromptTexts.Get("PermanentDuration", "Duration is fixed to permanent, durationDays=0.")
						: PolicyGenerationPromptTexts.Get("FiniteDuration", "Duration is fixed to finite, durationDays=") + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("FullStop", "."))
				: isPermanent
					? PolicyGenerationPromptTexts.Get("KingdomPermanentDuration", "[Kingdom policy duration] Duration is fixed to permanent, durationDays=0; analyze only long-term accumulation, equilibrium, resistance, and sustained execution conditions. Do not prejudge whether later effects per execution cycle should be stronger or weaker.")
					: PolicyGenerationPromptTexts.Get("KingdomFiniteDuration", "[Kingdom policy duration] Duration is fixed to finite, durationDays=") + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("FullStop", ".");
		string costRules = useAiCost
			? isVassalPolicy
				? PolicyGenerationPromptTexts.Get("VassalCostRule", "Assess the nonnegative requiredGoldCost needed for full execution.")
				: PolicyGenerationPromptTexts.Get("CostRule", "Assess nonnegative startupGoldCost and dailyMaintenanceGoldCost separately; they belong to the policy transaction.")
			: PolicyGenerationPromptTexts.Get("FixedCostRule", "Costs are determined by fixed MCM values; do not output cost fields in this request.");
		string dynamicSystem = JoinPolicyPromptSections(
			request?.EvaluatorPrompt,
			PolicyGenerationPromptTexts.Get("AssessmentRulesHeader", "[General assessment rules]\n") + policyRules,
			scopeRules,
			PolicyGenerationPromptTexts.Get("ParametersHeader", "[Current parameters]\n") + costRules + PolicyGenerationPromptTexts.Get("FeedbackLengthPrefix", " Write publicFeedback in the third person, approximately ")
				+ feedbackChars.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("FeedbackLengthSuffix", " English characters, without inventing events that have already occurred."));
		string costSchema = useAiCost
			? isVassalPolicy
				? PolicyGenerationPromptTexts.Get("VassalCostSchema", "- requiredGoldCost:number, a finite nonnegative number.\n")
				: PolicyGenerationPromptTexts.Get("CostSchema", "- startupGoldCost:number, a finite nonnegative number.\n- dailyMaintenanceGoldCost:number, a finite nonnegative number.\n")
			: string.Empty;
		string user = PolicyGenerationPromptTexts.Get("FrozenWorldHeader", "[World context (frozen snapshot)]\n") + (context.WorldContextFull ?? string.Empty)
			+ (string.IsNullOrWhiteSpace(knowledgeContext)
				? string.Empty
				: PolicyGenerationPromptTexts.Get("KnowledgeHeader", "\n\n[Knowledge-base context (read-only retrieval)]\n") + knowledgeContext.Trim())
			+ PolicyGenerationPromptTexts.Get("ExtensionHeader", "\n\n[Extension context (frozen snapshot)]\n") + (context.ExtensionContext ?? string.Empty)
			+ (string.IsNullOrWhiteSpace(request?.PolicyHistoryRetrieval?.CombinedPrompt)
				? string.Empty
				: "\n\n" + request.PolicyHistoryRetrieval.CombinedPrompt)
			+ PolicyGenerationPromptTexts.Get("OriginalPolicyHeader", "\n\n[Original policy]\nName: ") + (request?.PolicyName ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("DateField", "\nDate: ") + PolicyDateTextProjection.ForPrompt(request?.DateText ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("ContentField", "\nContent:\n") + (request?.PolicyContent ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("MainSchemaHeader", "\n\nOutput only a JSON object with the following fields:\n")
			+ PolicyGenerationPromptTexts.Get("FeedbackSchema", "- publicFeedback:string, a player-visible social response in the third person.\n")
			+ PolicyGenerationPromptTexts.Get("ImpactSchema", "- impactSummary:string, summarize direct social, political, fiscal, military, and governance effects; list each distinct mechanical consequence that can reasonably follow at first order from the same measure, rather than only the most prominent one.\n")
			+ PolicyGenerationPromptTexts.Get("NumericIntentSchema", "- numericIntent:string, preserve in natural language every explicitly stated amount, multiplier, percentage, range, direction, and timing expression such as daily, one-time, or ongoing. This is not an execution contract; do not invent values. If there is no direct numeric intent, write \"No direct numeric intent\".\n")
			+ PolicyGenerationPromptTexts.Get("PolicyDigestSchema", "- policyContentDigest:string, one short sentence summarizing the purpose, main measures, and objects.\n")
			+ PolicyGenerationPromptTexts.Get("FeedbackDigestSchema", "- feedbackDigest:string, one short sentence summarizing the main support, opposition, and concerns.\n")
			+ (isVassalPolicy
				? PolicyGenerationPromptTexts.Get("VassalSchema", "- vassalIndependenceDelta:number, in the range -15 to 15.\n- vassalIndependenceReason:string, one short sentence.\n")
				: string.Empty)
			+ PolicyGenerationPromptTexts.Get("AuthoritarianSchema", "- authoritarianWeight:number, in the range [-1,1].\n")
			+ PolicyGenerationPromptTexts.Get("OligarchicSchema", "- oligarchicWeight:number, in the range [-1,1].\n")
			+ PolicyGenerationPromptTexts.Get("EgalitarianSchema", "- egalitarianWeight:number, in the range [-1,1]; the three weights must not all be 0.\n")
			+ PolicyVotePersonality.PromptContract + "\n"
			+ costSchema
			+ PolicyGenerationPromptTexts.Get("DurationModeSchema", "- effectDurationMode:string, either permanent or finite only.\n")
			+ PolicyGenerationPromptTexts.Get("DurationDaysSchema", "- durationDays:number, 0 for permanent and a positive integer for finite.");
		return BuildChatMessages(PlayerPolicyMainStableSystemPrefix, dynamicSystem, user);
	}


	private static List<object> BuildEffectPostprocessMessages(PolicyDraftRequest request, PolicyMainAssessmentResult assessment)
	{
		bool isLocalPolicy = IsLocalPolicyRequest(request);
		bool isVassalPolicy = IsVassalPolicyRequest(request);
		string effectScope = request?.ScopeKind ?? PolicyScopeKingdom;
		PolicyTargetHandleDirectory targetDirectory = EnsurePlayerPolicyTargetHandleDirectory(request);
		IReadOnlyCollection<string> injectedIds = targetDirectory.Capabilities?.Keys.ToArray() ?? Array.Empty<string>();
		string understandingRules = injectedIds.Count == 0
			? PolicyGenerationPromptTexts.Get("None", "(none)")
			: PolicyEffectModuleCatalog.BuildMainInstructions(effectScope, injectedIds);
		string payloadRules = injectedIds.Count == 0
			? PolicyGenerationPromptTexts.Get("None", "(none)")
			: PolicyEffectModuleCatalog.BuildPayloadPromptRules(effectScope, injectedIds);
		string scopeRule = isLocalPolicy
			? PolicyGenerationPromptTexts.Get("LocalTargetRule", "Local policies may use only legal S/L*/C*/R*/H*/P* handles from the directory. For any capability with defaultTargetHandle=S, ordinary semantics must use S; do not expand beyond the publication location by default.")
			: isVassalPolicy
				? PolicyGenerationPromptTexts.Get("VassalTargetRule", "Vassal policies may use only legal K*/H*/P* handles from the directory. For any capability with defaultTargetHandle, ordinary semantics must use the default K0; K1 may be selected from allowedSubsetTargetHandles only when the suzerain kingdom undergoes a direct change.")
				: PolicyGenerationPromptTexts.Get("KingdomTargetRule", "Kingdom policies may use only legal K*/H*/P* handles from the directory. For any capability with defaultTargetHandle, ordinary semantics must use that default target. Foreign objects must be explicitly named in the original text and pass C# authorization checks, and may be selected from allowedSubsetTargetHandles only when the effect actually settles on that foreign object.");
		string dynamicSystem = PolicyGenerationPromptTexts.Get("CapabilitiesHeader", "[Applicability semantics of actually injected capabilities]\n") + understandingRules
			+ PolicyGenerationPromptTexts.Get("PayloadContractsHeader", "\n\n[Detailed payload contracts of actually injected capabilities]\n") + payloadRules
			+ PolicyGenerationPromptTexts.Get("ScopeRulesHeader", "\n\n[Scope and target rules]\n") + scopeRule
			+ "\n" + BuildPolicyTargetSelectionPromptRule(request);
		string user = PolicyGenerationPromptTexts.Get("AuthoritativePolicyHeader", "[Original policy (highest semantic authority)]\nName: ") + (request?.PolicyName ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("ContentField", "\nContent:\n") + (request?.PolicyContent ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("FirstAssessmentHeader", "\n\n[First general assessment result]\npublicFeedback: ") + (assessment?.PublicFeedback ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("impactSummaryField", "\nimpactSummary: ") + (assessment?.ImpactSummary ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("numericIntentField", "\nnumericIntent: ") + (assessment?.NumericIntent ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("policyContentDigestField", "\npolicyContentDigest: ") + (assessment?.PolicyContentDigest ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("feedbackDigestField", "\nfeedbackDigest: ") + (assessment?.FeedbackDigest ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("startupGoldCostField", "\nstartupGoldCost: ") + (assessment?.StartupGoldCost ?? 0f).ToString("0.###", CultureInfo.InvariantCulture)
			+ PolicyGenerationPromptTexts.Get("dailyMaintenanceGoldCostField", "\ndailyMaintenanceGoldCost: ") + (assessment?.DailyMaintenanceGoldCost ?? 0f).ToString("0.###", CultureInfo.InvariantCulture)
			+ PolicyGenerationPromptTexts.Get("effectDurationModeField", "\neffectDurationMode: ") + (assessment?.EffectDurationMode ?? string.Empty)
			+ PolicyGenerationPromptTexts.Get("durationDaysField", "\ndurationDays: ") + (assessment?.DurationDays ?? 0).ToString(CultureInfo.InvariantCulture)
			+ PolicyGenerationPromptTexts.Get("LegalTargetsHeader", "\n\n[Complete legal target-handle directory for this request (structured JSON)]\n")
			+ SerializePlayerPolicyTargetHandleDirectory(targetDirectory, request)
			+ PolicyGenerationPromptTexts.Get("StrictContractHeader", "\n\n[Strict output contract]\n")
			+ PolicyEffectDirectPlanContract.BuildOutputContract(
				requireExecutable: false,
				requireSingleTargetPerEffect: false)
			+ PolicyGenerationPromptTexts.Get("ChooseCapabilities", "Independently judge each capability: select it when the policy measures have a reasonable, explainable direct or immediate first-order causal connection to it; the original text need not name the game metric verbatim. Omit it only when there is no causal relationship, the target does not match, or settlement semantics are incompatible. Selecting zero, one, or multiple capabilities is allowed, with no preference for a particular count.");
		return BuildChatMessages(
			PlayerPolicyEffectStableSystemPrefix + PolicyGenerationPromptTexts.Get("CalibrationHeader", "\n[Non-editable general execution calibration]\n") + PlayerPolicyEffectCommonCalibration,
			dynamicSystem,
			user);
	}


	private static void LogPolicyTargetPlanRouteResult(
		PolicyDraftRequest request,
		PolicyTargetPlanRouteResult result,
		string stage,
		string intentLeg)
	{
		if (result == null)
		{
			return;
		}
		IReadOnlyList<PolicyTargetPlanCandidate> candidates = result.Candidates ?? Array.Empty<PolicyTargetPlanCandidate>();
		IReadOnlyList<PolicyTargetPlanRouteIssue> issues = result.Issues ?? Array.Empty<PolicyTargetPlanRouteIssue>();
		IReadOnlyList<string> matchedExistingHandleKeys = result.MatchedExistingHandleKeys ?? Array.Empty<string>();
		bool shouldRejectPolicy = result.HasExplicitTargetIntent
			&& candidates.Count <= 0
			&& matchedExistingHandleKeys.Count <= 0
			&& issues.Any(issue => issue != null
				&& (issue.Kind == PolicyTargetPlanRouteIssueKind.InvalidExplicitTarget
					|| issue.Kind == PolicyTargetPlanRouteIssueKind.MissingRuntimeAnchor));
		string routeMessage = BuildPolicyRequestLogPrefix(request)
			+ " stage=" + (stage ?? string.Empty)
			+ " intentLeg=" + (intentLeg ?? string.Empty)
			+ " explicit=" + (result.HasExplicitTargetIntent ? "true" : "false")
			+ " candidates=" + candidates.Count.ToString(CultureInfo.InvariantCulture)
			+ " blocking=" + (shouldRejectPolicy ? "true" : "false");
		PolicyLogContext routeContext = BuildPolicyLogContext(request);
		routeContext.TargetCount = candidates.Count;
		routeContext.TargetKeys = string.Join(",", matchedExistingHandleKeys);
		routeContext.TargetSummary = BuildPolicyTargetPlanRouteLogSummary(result);
		routeContext.MessageChars = routeMessage.Length;
		routeContext.MessageHash = PolicySystemLog.HashSensitive(routeMessage);
		routeContext.Counts = new Dictionary<string, int>(StringComparer.Ordinal)
		{
			["matchedExistingHandles"] = matchedExistingHandleKeys.Count,
			["issues"] = issues.Count,
			["blocking"] = shouldRejectPolicy ? 1 : 0,
			["explicit"] = result.HasExplicitTargetIntent ? 1 : 0
		};
		PolicySystemLog.Lifecycle("Player", "target-plan-route", "event", routeContext);
		foreach (PolicyTargetPlanRouteIssue issue in issues.Where(issue => issue != null))
		{
			bool provisionalStage = string.Equals(stage, "pre-main", StringComparison.Ordinal);
			string eventName = issue.Kind == PolicyTargetPlanRouteIssueKind.NoIntent
				? "target-plan-fallback"
				: shouldRejectPolicy && !provisionalStage
					? "target-plan-blocked"
					: "target-plan-candidate-rejected";
			string issueMessage = BuildPolicyRequestLogPrefix(request)
				+ " stage=" + (stage ?? string.Empty)
				+ " intentLeg=" + (intentLeg ?? string.Empty)
				+ " kind=" + issue.Kind
				+ " evidence=" + (issue.EvidenceKind ?? string.Empty)
				+ (string.IsNullOrWhiteSpace(issue.Message) ? string.Empty : " error=" + issue.Message);
			PolicyLogContext issueContext = BuildPolicyLogContext(request);
			issueContext.ErrorKind = issue.Kind.ToString();
			issueContext.PlanSignature = issue.CandidateSignature;
			issueContext.TargetSummary = "pipelineStage=" + (stage ?? string.Empty)
				+ "; intentLeg=" + (intentLeg ?? string.Empty)
				+ "; evidence=" + (issue.EvidenceKind ?? string.Empty)
				+ "; message=" + (issue.Message ?? string.Empty);
			issueContext.MessageChars = issueMessage.Length;
			issueContext.MessageHash = PolicySystemLog.HashSensitive(issueMessage);
			PolicySystemLog.Lifecycle("Player", eventName, issue.Kind == PolicyTargetPlanRouteIssueKind.NoIntent ? "fallback" : "event", issueContext);
		}
	}

	private static bool TryBuildFinalPolicyPostprocess(
		PolicyDraftRequest request,
		PolicyMainAssessmentResult assessment,
		string raw,
		out PolicyPostprocessResult postprocess,
		out string error,
		out PlayerPolicyEffectValidationErrorKind errorKind)
	{
		postprocess = null;
		error = "";
		errorKind = PlayerPolicyEffectValidationErrorKind.None;
		bool isPermanentPlayerEffect = IsPermanentPlayerPolicyEffect(request);
		int durationDays = isPermanentPlayerEffect
			? 0
			: request?.ManualDurationDays > 0
			? request.ManualDurationDays
			: assessment?.DurationDays ?? 0;
		if (!isPermanentPlayerEffect && durationDays <= 0)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryBuildFinalPolicyPostprocess_025");
			errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
			return false;
		}
		if (!TryParsePolicyPostprocessResult(
			raw,
			request,
			durationDays,
			out PolicyPostprocessResult parsed,
			out bool durationCorrected,
			out int legacyEffectCount,
			out string parseError,
			out errorKind))
		{
			error = parseError;
			return false;
		}
		PolicySystemLog.Write("Player", "effectPlanParsed",
			BuildPolicyRequestLogPrefix(request)
			+ " disposition=" + (parsed.Disposition ?? string.Empty)
			+ " effectCount=" + (parsed.Effects?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " promptHash=" + (request?.EffectPromptHash ?? string.Empty)
			+ " promptChars=" + Math.Max(0, request?.EffectPromptChars ?? 0).ToString(CultureInfo.InvariantCulture));
		if (durationCorrected)
		{
			PolicyDebugLog("policy-effect-duration-normalized", BuildPolicyRequestLogPrefix(request)
				+ " authoritativeDurationDays=" + durationDays.ToString(CultureInfo.InvariantCulture));
		}
		if (legacyEffectCount > 0)
		{
			PolicyDebugLog("policy-effect-legacy-normalized", BuildPolicyRequestLogPrefix(request)
				+ " effectCount=" + legacyEffectCount.ToString(CultureInfo.InvariantCulture));
		}
		if (!TryValidatePlayerPolicyEffectTargetHandles(
			request?.EffectTargetDirectory,
			parsed.Effects,
			out errorKind,
			out error))
		{
			return false;
		}
		bool crossKingdomCoverageValid = TryValidatePlayerPolicyCrossKingdomCoverage(
			request?.ScopeKind,
			request?.EffectTargetDirectory,
			parsed.Effects,
			out int foreignEffectCount,
			out int issuerEffectCount,
			out string coverageHash,
			out errorKind,
			out error);
		if (string.Equals(request?.ScopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase)
			&& foreignEffectCount > 0)
		{
			PolicySystemLog.Write("Player", "crossKingdomCoverage",
				BuildPolicyRequestLogPrefix(request)
				+ " foreignEffectCount=" + foreignEffectCount.ToString(CultureInfo.InvariantCulture)
				+ " issuerEffectCount=" + issuerEffectCount.ToString(CultureInfo.InvariantCulture)
				+ " coverageHash=" + coverageHash
				+ " valid=" + (crossKingdomCoverageValid ? "true" : "false"));
		}
		if (!crossKingdomCoverageValid)
		{
			return false;
		}
		bool executable = string.Equals(parsed.Disposition, "executable", StringComparison.Ordinal);
		IReadOnlyCollection<string> authorizedSourceModuleIds = BuildPostprocessAllowedModuleIds(
			request?.EffectTargetDirectory);
		List<PolicyEffectDto> compiledEffects = new List<PolicyEffectDto>();
		if (executable
			&& !TryCompileSparsePolicyEffects(
				request,
				durationDays,
				parsed.Effects,
				authorizedSourceModuleIds,
				authorizedSourceModuleIds,
				out compiledEffects,
				out string compileError,
				allowAlreadyCompiled: false))
		{
			error = compileError;
			errorKind = PlayerPolicyEffectValidationErrorKind.CompilationOrSafety;
			return false;
		}
		assessment.DurationDays = durationDays;
		assessment.EffectDurationMode = isPermanentPlayerEffect ? "permanent" : "finite";
		assessment.Effects = compiledEffects;
		assessment.UsesSparseEffectIr = true;
		assessment.EffectIrValidationError = "";
		assessment.EffectDisposition = parsed.Disposition;
		assessment.EffectDispositionReason = parsed.Reason;
		assessment.ConfirmedTargetHandles = null;
		if (executable
			&& IsLocalPolicyRequest(request)
			&& !TryValidateLocalPolicyAssessment(request, assessment, out string localError))
		{
			error = localError;
			errorKind = PlayerPolicyEffectValidationErrorKind.CompilationOrSafety;
			return false;
		}
		PolicySystemLog.Write("Player", "compiled",
			BuildPolicyRequestLogPrefix(request)
			+ " executable=" + (executable ? "true" : "false")
			+ " effectCount=" + compiledEffects.Count.ToString(CultureInfo.InvariantCulture)
			+ " effectHash=" + PolicyTextEmbeddingSession.StableTextHash(
				JsonConvert.SerializeObject(compiledEffects)));
		string finalImpactSummary = FirstNonEmpty(CleanPolicyDisplayText(parsed.ImpactSummary ?? ""), assessment.ImpactSummary);
		assessment.ImpactSummary = finalImpactSummary;
		postprocess = new PolicyPostprocessResult
		{
			EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
			ImpactSummary = finalImpactSummary,
			Disposition = parsed.Disposition,
			Reason = parsed.Reason,
			DurationDays = durationDays,
			Effects = compiledEffects
		};
		return true;
	}

	private static bool TryValidatePlayerPolicyEffectTargetHandles(
		PolicyTargetHandleDirectory directory,
		IEnumerable<PolicyEffectDto> effects,
		out PlayerPolicyEffectValidationErrorKind errorKind,
		out string error)
	{
		errorKind = PlayerPolicyEffectValidationErrorKind.None;
		error = string.Empty;
		if (directory?.StructureVersion != PolicyTargetHandleDirectoryContract.CurrentVersion
			|| directory.Targets == null
			|| directory.Capabilities == null)
		{
			errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
			error = "InvalidTargetHandleDirectory";
			return false;
		}
		HashSet<string> allowedTargetKeys = BuildPostprocessAllowedTargetKeys(directory);
		HashSet<string> allowedModuleIds = BuildPostprocessAllowedModuleIds(directory);
		foreach (PolicyEffectDto rawEffect in effects ?? Enumerable.Empty<PolicyEffectDto>())
		{
			IEnumerable<string> returnedTargets = rawEffect?.TargetHandles != null && rawEffect.TargetHandles.Count > 0
				? rawEffect.TargetHandles
				: rawEffect?.Targets ?? Enumerable.Empty<string>();
			foreach (string targetKey in returnedTargets)
			{
				if (targetKey == null || !allowedTargetKeys.Contains(targetKey))
				{
					errorKind = PlayerPolicyEffectValidationErrorKind.UnknownOrUnauthorizedTargetHandle;
					error = "UnknownOrUnauthorizedTargetHandle: " + (targetKey ?? "<null>");
					return false;
				}
			}
			string moduleId = rawEffect?.ModuleId;
			if (moduleId == null || !allowedModuleIds.Contains(moduleId))
			{
				errorKind = PlayerPolicyEffectValidationErrorKind.UnauthorizedModuleTargetPair;
				error = "UnauthorizedModuleTargetPair: " + (moduleId ?? "<null>") + " -> <module>";
				return false;
			}
			HashSet<string> moduleTargetKeys = BuildPostprocessAllowedTargetKeys(directory, moduleId);
			foreach (string targetKey in returnedTargets)
			{
				if (!moduleTargetKeys.Contains(targetKey))
				{
					errorKind = PlayerPolicyEffectValidationErrorKind.UnauthorizedModuleTargetPair;
					error = "UnauthorizedModuleTargetPair: " + moduleId + " -> " + targetKey;
					return false;
				}
			}
		}
		return true;
	}

	private static bool TryValidatePlayerPolicyCrossKingdomCoverage(
		string scopeKind,
		PolicyTargetHandleDirectory directory,
		IEnumerable<PolicyEffectDto> effects,
		out int foreignEffectCount,
		out int issuerEffectCount,
		out string coverageHash,
		out PlayerPolicyEffectValidationErrorKind errorKind,
		out string error)
	{
		foreignEffectCount = 0;
		issuerEffectCount = 0;
		coverageHash = PolicyTextEmbeddingSession.StableTextHash(string.Empty);
		errorKind = PlayerPolicyEffectValidationErrorKind.None;
		error = string.Empty;
		if (!string.Equals(scopeKind, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		List<string> coveragePairs = new List<string>();
		foreach (PolicyEffectDto effect in effects ?? Enumerable.Empty<PolicyEffectDto>())
		{
			string moduleId = (effect?.ModuleId ?? string.Empty).Trim();
			if (moduleId.Length == 0
				|| directory?.Capabilities == null
				|| !directory.Capabilities.TryGetValue(moduleId, out PolicyEffectCapabilityDirectoryEntry capability))
			{
				continue;
			}
			IEnumerable<string> returnedTargets = effect.TargetHandles != null && effect.TargetHandles.Count > 0
				? effect.TargetHandles
				: effect.Targets ?? Enumerable.Empty<string>();
			bool isIssuerEffect = false;
			bool isForeignEffect = false;
			foreach (string rawTargetHandle in returnedTargets)
			{
				string targetHandle = (rawTargetHandle ?? string.Empty).Trim();
				if (targetHandle.Length == 0)
				{
					continue;
				}
				coveragePairs.Add(moduleId + "\u001f" + targetHandle);
				isIssuerEffect |= string.Equals(targetHandle, "K0", StringComparison.Ordinal);
				isForeignEffect |= capability.JurisdictionByTargetHandle != null
					&& capability.JurisdictionByTargetHandle.TryGetValue(
						targetHandle,
						out PolicyEffectTargetJurisdictionKind jurisdiction)
					&& jurisdiction == PolicyEffectTargetJurisdictionKind.CrossKingdom;
			}
			if (isIssuerEffect)
			{
				issuerEffectCount++;
			}
			if (isForeignEffect)
			{
				foreignEffectCount++;
			}
		}
		coverageHash = PolicyTextEmbeddingSession.StableTextHash(string.Join("\u001e", coveragePairs
			.OrderBy(value => value, StringComparer.Ordinal)));
		if (foreignEffectCount > 0 && issuerEffectCount == 0)
		{
			errorKind = PlayerPolicyEffectValidationErrorKind.MissingIssuerKingdomEffect;
			error = "MissingIssuerKingdomEffect: foreignEffects="
				+ foreignEffectCount.ToString(CultureInfo.InvariantCulture)
				+ " issuerEffects=0";
			return false;
		}
		return true;
	}

	private static bool IsPolicyEffectSourceRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Source || role == PolicyEffectMechanismRole.Cost;
	}

	private static bool IsPolicyEffectDestinationRole(PolicyEffectMechanismRole role)
	{
		return role == PolicyEffectMechanismRole.Destination || role == PolicyEffectMechanismRole.Beneficiary;
	}

	private static bool TryParsePolicyPostprocessResult(
		string raw,
		PolicyDraftRequest request,
		int durationDays,
		out PolicyPostprocessResult result,
		out bool durationCorrected,
		out int legacyEffectCount,
		out string error)
	{
		return TryParsePolicyPostprocessResult(
			raw,
			request,
			durationDays,
			out result,
			out durationCorrected,
			out legacyEffectCount,
			out error,
			out _);
	}

	private static bool TryParsePolicyPostprocessResult(
		string raw,
		PolicyDraftRequest request,
		int durationDays,
		out PolicyPostprocessResult result,
		out bool durationCorrected,
		out int legacyEffectCount,
		out string error,
		out PlayerPolicyEffectValidationErrorKind errorKind)
	{
		result = null;
		durationCorrected = false;
		legacyEffectCount = 0;
		error = "";
		errorKind = PlayerPolicyEffectValidationErrorKind.None;
		if (string.IsNullOrWhiteSpace(raw))
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryParsePolicyPostprocessResult_026");
			errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
			return false;
		}
		try
		{
			string json = ExtractJsonObject(raw);
			if (string.IsNullOrWhiteSpace(json))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryParsePolicyPostprocessResult_026");
				errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
				return false;
			}
			JObject root = JObject.Parse(json, new JsonLoadSettings
			{
				CommentHandling = CommentHandling.Ignore,
				DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
				LineInfoHandling = LineInfoHandling.Ignore
			});
			if (!PolicyEffectPlanWireNormalizer.TryParseDirectPlayerEffectPlan(
				root,
				out string disposition,
				out string reason,
				out List<PolicyEffectWireEffect> wires,
				out error,
				out PolicyEffectPlanParseFailureKind parseFailureKind))
			{
				errorKind = parseFailureKind == PolicyEffectPlanParseFailureKind.IncompleteLinkedMechanism
					? PlayerPolicyEffectValidationErrorKind.IncompleteLinkedMechanism
					: PlayerPolicyEffectValidationErrorKind.InvalidStructure;
				return false;
			}
			result = new PolicyPostprocessResult
			{
				EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
				ImpactSummary = string.Empty,
				Disposition = disposition,
				Reason = reason,
				DurationDays = durationDays,
				Effects = wires.Select(BuildPolicyEffectDtoFromWire).ToList()
			};
			return true;
		}
		catch (Exception ex)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryParsePolicyPostprocessResult_027") + ex.Message;
			errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
			return false;
		}
	}

	private static PolicyEffectDto BuildPolicyEffectDtoFromWire(PolicyEffectWireEffect wire)
	{
		return new PolicyEffectDto
		{
			EffectPlanVersion = wire?.EffectPlanVersion ?? PolicyEffectPlanVersions.CurrentVersion,
			MechanismId = wire?.MechanismId ?? string.Empty,
			MechanismKind = wire?.MechanismKind ?? PolicyEffectMechanismKind.Independent,
			MechanismRole = wire?.MechanismRole ?? PolicyEffectMechanismRole.Subject,
			SourceOmitted = wire?.SourceOmitted == true,
			DestinationOmitted = wire?.DestinationOmitted == true,
			ModuleId = wire?.ModuleId ?? string.Empty,
			TargetHandles = wire?.TargetHandles == null ? new List<string>() : new List<string>(wire.TargetHandles),
			Payload = wire?.Payload?.DeepClone(),
			Reason = wire?.Reason ?? string.Empty
		};
	}

	private static bool TryValidatePolicyEffectPlanJson(JArray effects, out string error)
	{
		error = string.Empty;
		HashSet<string> allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"mechanismId", "mechanismKind", "role", "sourceOmitted", "destinationOmitted",
			"moduleId", "targetHandles", "payload", "reason"
		};
		HashSet<string> mechanismKinds = new HashSet<string>(new[] { "independent", "linked" }, StringComparer.OrdinalIgnoreCase);
		HashSet<string> roles = new HashSet<string>(
			new[] { "subject", "source", "destination", "cost", "beneficiary" },
			StringComparer.OrdinalIgnoreCase);
		foreach (JToken token in effects ?? new JArray())
		{
			if (!(token is JObject effect))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidatePolicyEffectPlanJson_028");
				return false;
			}
			Dictionary<string, JProperty> fields = effect.Properties()
				.ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
			string unknownField = fields.Keys.FirstOrDefault(field => !allowedFields.Contains(field));
			if (unknownField != null)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidatePolicyEffectPlanJson_029") + unknownField;
				return false;
			}
			if (!fields.TryGetValue("mechanismId", out JProperty mechanismId)
				|| mechanismId.Value.Type != JTokenType.String
				|| string.IsNullOrWhiteSpace(mechanismId.Value.ToString())
				|| !fields.TryGetValue("mechanismKind", out JProperty mechanismKind)
				|| mechanismKind.Value.Type != JTokenType.String
				|| !mechanismKinds.Contains(mechanismKind.Value.ToString())
				|| !fields.TryGetValue("role", out JProperty role)
				|| role.Value.Type != JTokenType.String
				|| !roles.Contains(role.Value.ToString())
				|| !fields.TryGetValue("sourceOmitted", out JProperty sourceOmitted)
				|| sourceOmitted.Value.Type != JTokenType.Boolean
				|| !fields.TryGetValue("destinationOmitted", out JProperty destinationOmitted)
				|| destinationOmitted.Value.Type != JTokenType.Boolean
				|| !fields.TryGetValue("moduleId", out JProperty moduleId)
				|| moduleId.Value.Type != JTokenType.String
				|| string.IsNullOrWhiteSpace(moduleId.Value.ToString())
				|| !fields.TryGetValue("targetHandles", out JProperty targetHandles)
				|| !(targetHandles.Value is JArray)
				|| !fields.TryGetValue("payload", out JProperty payload)
				|| payload.Value.Type != JTokenType.Object)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidatePolicyEffectPlanJson_030");
				return false;
			}
		}
		return true;
	}

	private static bool TryNormalizeLegacyPolicyPostprocessEffects(
		PolicyDraftRequest request,
		JArray effects,
		out JArray normalizedEffects,
		out int legacyEffectCount,
		out string error)
	{
		normalizedEffects = new JArray();
		legacyEffectCount = 0;
		error = "";
		foreach (JToken token in effects ?? new JArray())
		{
			if (!(token is JObject effect))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidatePolicyEffectPlanJson_028");
				return false;
			}
			Dictionary<string, JProperty> properties = new Dictionary<string, JProperty>(StringComparer.OrdinalIgnoreCase);
			foreach (JProperty property in effect.Properties())
			{
				if (properties.ContainsKey(property.Name))
				{
					error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_031") + property.Name;
					return false;
				}
				properties.Add(property.Name, property);
			}
			bool hasModuleShape = properties.ContainsKey("moduleId")
				|| properties.ContainsKey("targetHandles")
				|| properties.ContainsKey("payload");
			bool hasSparseShape = properties.ContainsKey("targets") || properties.ContainsKey("changes");
			bool hasLegacyShape = properties.Keys.Any(IsLegacyPolicyPostprocessField);
			if ((hasSparseShape || hasModuleShape) && hasLegacyShape)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_032");
				return false;
			}
			if (hasSparseShape && hasModuleShape)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_033");
				return false;
			}
			if (!hasLegacyShape)
			{
				normalizedEffects.Add(effect.DeepClone());
				continue;
			}
			foreach (string propertyName in properties.Keys)
			{
				if (!IsLegacyPolicyPostprocessField(propertyName)
					&& !string.Equals(propertyName, "reason", StringComparison.OrdinalIgnoreCase))
				{
					error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_034") + propertyName;
					return false;
				}
			}
			if (!TryResolveLegacyPolicyPostprocessTarget(request, properties, out string targetKey, out error))
			{
				return false;
			}
			JObject changes = new JObject();
			HashSet<string> mappedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, JProperty> property in properties)
			{
				if (!LegacyPolicyEffectFieldAdapter.TryResolveModuleId(property.Key, out string moduleId))
				{
					continue;
				}
				if (!TryReadFinitePolicyEffectNumber(property.Value.Value, out float value))
				{
					error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_035") + property.Key;
					return false;
				}
				if (!mappedModules.Add(moduleId))
				{
					error = PolicyCompatibilityTexts.Get("GenerationRaw_TryNormalizeLegacyPolicyPostprocessEffects_036") + moduleId;
					return false;
				}
				if (Math.Abs(value) <= 0.0001f)
				{
					continue;
				}
				changes[moduleId] = value;
			}
			JObject normalized = new JObject
			{
				["targets"] = new JArray(targetKey),
				["changes"] = changes
			};
			if (properties.TryGetValue("reason", out JProperty reasonProperty))
			{
				normalized["reason"] = reasonProperty.Value.Type == JTokenType.Null ? "" : reasonProperty.Value.ToString();
			}
			normalizedEffects.Add(normalized);
			legacyEffectCount++;
		}
		return true;
	}

	private static bool IsLegacyPolicyPostprocessField(string propertyName)
	{
		return LegacyPolicyEffectFieldAdapter.TryResolveModuleId(propertyName, out _)
			|| string.Equals(propertyName, "targetHandle", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetScope", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetKingdomId", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "targetKingdomName", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(propertyName, "durationDays", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryResolveLegacyPolicyPostprocessTarget(
		PolicyDraftRequest request,
		IReadOnlyDictionary<string, JProperty> properties,
		out string targetKey,
		out string error)
	{
		targetKey = "";
		error = "";
		List<PolicyTargetHandleSaveData> candidates = NormalizePolicyTargetHandles(request?.TargetHandles)
			.Where(handle => IsPolicyTargetHandleAllowedForRequest(request, handle))
			.ToList();
		string requestedHandle = ReadLegacyPolicyTargetText(properties, "targetHandle");
		string requestedKingdomId = ReadLegacyPolicyTargetText(properties, "targetKingdomId");
		string requestedKingdomName = ReadLegacyPolicyTargetText(properties, "targetKingdomName");
		string requestedScope = ReadLegacyPolicyTargetText(properties, "targetScope");
		if (!string.IsNullOrWhiteSpace(requestedHandle))
		{
			candidates = candidates.Where(handle => string.Equals(handle.Key, requestedHandle, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		if (!string.IsNullOrWhiteSpace(requestedKingdomId))
		{
			candidates = candidates.Where(handle => string.Equals(handle.KingdomId, requestedKingdomId, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(handle.EntityId, requestedKingdomId, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		if (!string.IsNullOrWhiteSpace(requestedKingdomName))
		{
			candidates = candidates.Where(handle => string.Equals(handle.KingdomName, requestedKingdomName, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(handle.DisplayName, requestedKingdomName, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		if (!string.IsNullOrWhiteSpace(requestedScope))
		{
			if (string.Equals(requestedScope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
			{
				candidates = candidates.Where(handle => string.Equals(handle.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase)).ToList();
			}
			else if (string.Equals(requestedScope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase))
			{
				candidates = candidates.Where(handle => !string.Equals(handle.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase)).ToList();
			}
			else if (!string.Equals(requestedScope, PolicyScopeKingdom, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(requestedScope, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(requestedScope, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryResolveLegacyPolicyPostprocessTarget_037") + requestedScope;
				return false;
			}
		}
		if (candidates.Count != 1)
		{
			error = candidates.Count <= 0 ? PolicyCompatibilityTexts.Get("GenerationRaw_TryResolveLegacyPolicyPostprocessTarget_038") : PolicyCompatibilityTexts.Get("GenerationRaw_TryResolveLegacyPolicyPostprocessTarget_039");
			return false;
		}
		targetKey = candidates[0].Key;
		return true;
	}

	private static string ReadLegacyPolicyTargetText(IReadOnlyDictionary<string, JProperty> properties, string propertyName)
	{
		return properties != null && properties.TryGetValue(propertyName, out JProperty property) && property.Value.Type != JTokenType.Null
			? property.Value.ToString().Trim()
			: "";
	}

	private static bool TryReadFinitePolicyEffectNumber(JToken token, out float value)
	{
		value = 0f;
		if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Boolean)
		{
			return false;
		}
		return float.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
			&& !float.IsNaN(value)
			&& !float.IsInfinity(value);
	}

	private static List<object> BuildChatMessages(string system, string user)
	{
		return new List<object>
		{
			new { role = "system", content = system ?? "" },
			new { role = "user", content = user ?? "" }
		};
	}

	private static List<object> BuildChatMessages(string stableSystem, string dynamicSystem, string user)
	{
		List<object> messages = new List<object>
		{
			new { role = "system", content = stableSystem ?? "" }
		};
		if (!string.IsNullOrWhiteSpace(dynamicSystem))
		{
			messages.Add(new { role = "system", content = dynamicSystem });
		}
		messages.Add(new { role = "user", content = user ?? "" });
		return messages;
	}

	private static string JoinPolicyPromptSections(params string[] sections)
	{
		if (sections == null || sections.Length == 0)
		{
			return "";
		}
		return string.Join("\n\n", sections.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
	}

	private PolicyPromptContextBundle BuildPolicyPromptContextBundle(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		return new PolicyPromptContextBundle
		{
			PolicyRuleContext = BuildPolicyRuleContext(),
			WorldContextCompact = BuildPolicyWorldContextCompact(playerKingdom, options),
			WorldContextFull = BuildPolicyWorldContextFull(playerKingdom, options),
			ExtensionContext = BuildPolicyExtensionContext(playerKingdom, options)
		};
	}

	private PolicyPromptContextBundle BuildLocalPolicyPromptContextBundle(List<Settlement> selectedFiefs, Kingdom playerKingdom, PolicyRuntimeOptions options, LocalPolicyMentionTargetSelection mentionTargets)
	{
		string localContext = BuildLocalPolicyWorldContext(selectedFiefs, playerKingdom, options, mentionTargets);
		return new PolicyPromptContextBundle
		{
			PolicyRuleContext = BuildPolicyRuleContext() + PolicyGenerationPromptTexts.Get("LocalContextRule", "\n- Local policies apply to the publication location and legal targets resolved deterministically by C#. The publication location and other targets use separate effects, and kingdom stability is fixed to 0."),
			WorldContextCompact = localContext,
			WorldContextFull = localContext,
			ExtensionContext = PolicyGenerationPromptTexts.Get("LocalExtension", "(Local policies currently do not write NPC/AFEF memory; do not import targets or facts from other communication pipelines.)")
		};
	}

	private string BuildLocalPolicyWorldContext(List<Settlement> selectedFiefs, Kingdom playerKingdom, PolicyRuntimeOptions options, LocalPolicyMentionTargetSelection mentionTargets)
	{
		List<Settlement> fiefs = (selectedFiefs ?? new List<Settlement>()).Where(IsPlayerOwnedLocalPolicyFief).ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CurrentDate", "Current date: ") + PolicyDateTextProjection.ForPrompt(FormatCurrentCampaignDate()));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerLabel", "Player: ") + (Hero.MainHero?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Player", "player")) + PolicyGenerationPromptTexts.Get("PlayerClan", "; Player clan=") + (Clan.PlayerClan?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown")) + PolicyGenerationPromptTexts.Get("KingdomMembership", "; Kingdom=") + (playerKingdom == null ? PolicyGenerationPromptTexts.Get("IndependentClan", "none (independent clan)") : GetPolicyPromptKingdomName(playerKingdom)));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerGold", "Player resources: denars=") + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture));
		if (options?.UseAiEvaluatedCost == true)
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("LocalAiCostPrefix", "Cost mode: AI assesses the full cost of local execution; actual settlement reserves at least ") + LocalPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("LocalAiCostSuffix", " denars. Insufficient funds scale all local effects in proportion to actual investment. Estimate costs according to the coverage of targets actually referenced by effects; unused candidates do not count."));
		}
		else
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("FixedMcmCost", "Cost mode: fixed MCM cost of ") + Math.Max(0, options?.GoldCost ?? 0).ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("DenarsSentence", " denars."));
		}
		sb.AppendLine(PolicyGenerationPromptTexts.Get("LocalScope", "Scope: local; publication towns/castles=") + fiefs.Count.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("LocalChildScope", "; C# automatically settles subordinate coverage with its parent, without providing independent targets."));
		sb.AppendLine(BuildLocalPolicyMentionSummary(mentionTargets));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("SelectedFiefsHeader", "[Selected fiefs and current core values]"));
		foreach (Settlement fief in fiefs)
		{
			Town town = fief.Town;
			sb.AppendLine(PolicyGenerationPromptTexts.Get("RootFiefId", "- Root fief ID=") + (fief.StringId ?? "") + PolicyGenerationPromptTexts.Get("Name", "; Name=") + (fief.Name?.ToString() ?? fief.StringId ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown")) + PolicyGenerationPromptTexts.Get("Type", "; Type=") + GetPolicyPromptFiefType(fief)
				+ PolicyGenerationPromptTexts.Get("Prosperity", "; Prosperity=") + FormatNumber(town?.Prosperity ?? 0f) + PolicyGenerationPromptTexts.Get("Food", "; Food=") + FormatNumber(town?.FoodStocks ?? 0f)
				+ PolicyGenerationPromptTexts.Get("Loyalty", "; Loyalty=") + FormatNumber(town?.Loyalty ?? 0f) + PolicyGenerationPromptTexts.Get("Security", "; Security=") + FormatNumber(town?.Security ?? 0f) + PolicyGenerationPromptTexts.Get("Militia", "; Militia=") + FormatNumber(fief.Militia));
		}
		return sb.ToString().Trim();
	}

	private static string BuildPolicyRuleContext()
	{
		return "ruleSource=custom_policy_only\n"
			+ PolicyGenerationPromptTexts.Get("IsolatedPipelineRule", "- This uses only the independent custom policy pipeline. Do not inject RuleBehaviorPrompts, meeting dialogue, native dialogue, letters, shouts, or other action-tag rules.\n")
			+ PolicyGenerationPromptTexts.Get("SharedEvaluatorRule", "- Kingdom and local policies share the full base evaluator prompt editable in MCM. Local policies dynamically append only selected fiefs, local scope, stability fixed to 0, and other mandatory rules.\n")
			+ PolicyGenerationPromptTexts.Get("GeneralAssessmentRule", "- This stage performs only general policy assessment: describe direct social, political, fiscal, and military effects, retain explicit numeric and timing expressions from the original text, and do not decide subsequent executable capabilities or specific targets.");
	}

	private string BuildPolicyWorldContextCompact(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CurrentDate", "Current date: ") + PolicyDateTextProjection.ForPrompt(FormatCurrentCampaignDate()));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerLabel", "Player: ") + (Hero.MainHero?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Player", "player")));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerGold", "Player resources: denars=") + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("Influence", "; Influence=") + FormatNumber(Math.Max(0f, Clan.PlayerClan?.Influence ?? 0f)));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerKingdom", "Player kingdom: ") + GetPolicyPromptKingdomName(playerKingdom) + " | ID=" + (playerKingdom?.StringId ?? ""));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CostMode", "Cost mode: ") + BuildPolicyCostModeContextLine(options));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("KingdomEligibility", "Publication requirements: the player must be king; there is no cooldown, so policies may be published consecutively."));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("EvaluatorSource", "Main evaluator prompt source: ") + (options.EvaluatorPromptIsDefault ? PolicyGenerationPromptTexts.Get("McmDefaultEvaluator", "MCM custom policy evaluator prompt (currently the default text)") : PolicyGenerationPromptTexts.Get("McmCustomEvaluator", "player-customized evaluator prompt in MCM")));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("DailyPolicyMechanism", "This pipeline uses AnimusForge custom policies rather than dynamically registering native PolicyObject instances. Successful publication creates ongoing daily effects, settled on each game daily Tick."));
		sb.AppendLine();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CompactKingdomHeader", "[Compact player kingdom overview]"));
		AppendKingdomSummary(sb, playerKingdom, includeAnomalies: false);
		sb.AppendLine();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("OtherKingdomsHeader", "[Other kingdoms index]"));
		AppendOtherKingdomIndex(sb, playerKingdom);
		return sb.ToString().Trim();
	}

	private string BuildPolicyWorldContextFull(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CurrentDate", "Current date: ") + PolicyDateTextProjection.ForPrompt(FormatCurrentCampaignDate()));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerLabel", "Player: ") + (Hero.MainHero?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Player", "player")));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerGold", "Player resources: denars=") + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("Influence", "; Influence=") + FormatNumber(Math.Max(0f, Clan.PlayerClan?.Influence ?? 0f)));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("PlayerKingdom", "Player kingdom: ") + GetPolicyPromptKingdomName(playerKingdom) + " | ID=" + (playerKingdom?.StringId ?? ""));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("CostMode", "Cost mode: ") + BuildPolicyCostModeContextLine(options));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("KingdomDailyEligibility", "Publication requirements: the player must be king; no cooldown. Successful publication creates ongoing daily effects, settled each day starting with the next DailyTick."));
		sb.AppendLine(PolicyGenerationPromptTexts.Get("EvaluatorSource", "Main evaluator prompt source: ") + (options.EvaluatorPromptIsDefault ? PolicyGenerationPromptTexts.Get("McmCalradiaEvaluator", "MCM default Calradia policy evaluator") : PolicyGenerationPromptTexts.Get("McmCustomEvaluator", "player-customized evaluator prompt in MCM")));
		sb.AppendLine();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("FullKingdomHeader", "[Full player kingdom overview]"));
		AppendKingdomSummary(sb, playerKingdom, includeAnomalies: true);
		sb.AppendLine();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("OtherKingdomsHeader", "[Other kingdoms index]"));
		AppendOtherKingdomIndex(sb, playerKingdom);
		return sb.ToString().Trim();
	}

	private static string BuildPolicyExtensionContext(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		return PolicyGenerationPromptTexts.Get("ExtensionNotConnected", "(Extension context is not connected yet. This entry is reserved for future NPC memory, player history, and recent player actions. The current version must not automatically inject other rules from meeting dialogue, native dialogue, letters, or shouts.)");
	}

	private static string FormatPolicyPromptGoldCost(PolicyRuntimeOptions options)
	{
		int goldCost = (options ?? BuildPolicyRuntimeOptions()).GoldCost;
		return goldCost > 0
			? goldCost.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("Denars", " denars")
			: PolicyGenerationPromptTexts.Get("NoGoldCost", "no denar cost");
	}

	private static string GetPolicyPromptKingdomName(Kingdom kingdom)
	{
		return (kingdom?.Name?.ToString() ?? kingdom?.StringId ?? PolicyGenerationPromptTexts.Get("UnknownKingdom", "unknown kingdom")).Trim();
	}

	private static string GetPolicyPromptFiefType(Settlement fief)
	{
		switch (GetLocalPolicyFiefTypeText(fief))
		{
			case var rawPolicyCase41 when string.Equals(rawPolicyCase41, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_049"), StringComparison.Ordinal): return PolicyGenerationPromptTexts.Get("FiefCastle", "castle");
			case var rawPolicyCase42 when string.Equals(rawPolicyCase42, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_050"), StringComparison.Ordinal): return PolicyGenerationPromptTexts.Get("FiefTown", "town");
			case var rawPolicyCase43 when string.Equals(rawPolicyCase43, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_051"), StringComparison.Ordinal): return PolicyGenerationPromptTexts.Get("FiefVillage", "village");
			case var rawPolicyCase44 when string.Equals(rawPolicyCase44, PolicyCompatibilityTexts.Get("Legacy_GetPolicyPromptFiefType_052"), StringComparison.Ordinal): return PolicyGenerationPromptTexts.Get("Fief", "fief");
			default: return GetLocalPolicyFiefTypeText(fief);
		}
	}

	private static string BuildPolicyCostModeContextLine(PolicyRuntimeOptions options)
	{
		if (options?.UseAiEvaluatedCost == true)
		{
			return PolicyGenerationPromptTexts.Get("AiCostPrefix", "AI assessment of custom policy costs is enabled. Main processing must assess both startupGoldCost and dailyMaintenanceGoldCost. The startup fee still preserves a reserve of ") + AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("AiCostSuffix", " denars and scales effects by actual investment. Daily maintenance enters native clan finance settlement and does not participate in startup-ratio scaling.");
		}
		return PolicyGenerationPromptTexts.Get("FixedCostPrefix", "AI assessment of custom policy costs is disabled. The code deducts exactly the fixed MCM denar cost (") + FormatPolicyPromptGoldCost(options) + PolicyGenerationPromptTexts.Get("FixedCostSuffix", ") without resource-ratio scaling of effects; main processing need not assess execution costs.");
	}

	private void AppendOtherKingdomIndex(StringBuilder sb, Kingdom playerKingdom)
	{
		try
		{
			foreach (Kingdom kingdom in Kingdom.All.Where(k => k != null).OrderBy(k => GetKingdomName(k), StringComparer.OrdinalIgnoreCase))
			{
				if (kingdom == playerKingdom)
				{
					continue;
				}
				string relation = "";
				try
				{
					relation = playerKingdom != null && kingdom.IsAtWarWith(playerKingdom) ? PolicyGenerationPromptTexts.Get("War", "at war") : PolicyGenerationPromptTexts.Get("NotWar", "not at war");
				}
				catch
				{
					relation = PolicyGenerationPromptTexts.Get("Unknown", "unknown");
				}
				string cultureText = PolicyGenerationPromptTexts.Get("Unknown", "unknown");
				try
				{
					cultureText = kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown");
				}
				catch
				{
				}
				sb.AppendLine("- " + GetPolicyPromptKingdomName(kingdom) + " | ID=" + kingdom.StringId + PolicyGenerationPromptTexts.Get("CultureField", " | Culture=") + cultureText + PolicyGenerationPromptTexts.Get("LeaderField", " | Leader=") + (kingdom.Leader?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown")) + PolicyGenerationPromptTexts.Get("StabilityField", " | AF stability=") + MyBehavior.GetKingdomStabilityValueForExternal(kingdom).ToString(CultureInfo.InvariantCulture) + PolicyGenerationPromptTexts.Get("PlayerRelationField", "/100 | Relation to player kingdom=") + relation);
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("OtherKingdomReadFailure", "Failed to read the other kingdoms index: ") + ex.Message);
		}
	}

	private void AppendKingdomSummary(StringBuilder sb, Kingdom kingdom, bool includeAnomalies)
	{
		if (kingdom == null)
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("NoKingdom", "(no kingdom)"));
			return;
		}
		string cultureText = PolicyGenerationPromptTexts.Get("Unknown", "unknown");
		try
		{
			cultureText = kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown");
		}
		catch
		{
		}
		sb.AppendLine(PolicyGenerationPromptTexts.Get("KingdomLabel", "Kingdom: ") + GetPolicyPromptKingdomName(kingdom) + " | ID=" + kingdom.StringId + PolicyGenerationPromptTexts.Get("CultureField", " | Culture=") + cultureText + PolicyGenerationPromptTexts.Get("LeaderField", " | Leader=") + (kingdom.Leader?.Name?.ToString() ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown")) + PolicyGenerationPromptTexts.Get("StabilityField", " | AF stability=") + MyBehavior.GetKingdomStabilityValueForExternal(kingdom).ToString(CultureInfo.InvariantCulture) + "/100");
		try
		{
			string policies = string.Join(PolicyGenerationPromptTexts.Get("ListSeparator", ", "), kingdom.ActivePolicies.Where(p => p != null && !IsDynamicPolicyId(p.StringId)).Select(p => p.Name?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
			sb.AppendLine(PolicyGenerationPromptTexts.Get("ActivePolicies", "Currently active native policies: ") + (string.IsNullOrWhiteSpace(policies) ? PolicyGenerationPromptTexts.Get("NoPolicies", "none") : policies));
		}
		catch
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("ActivePolicyReadFailure", "Currently active native policies: failed to read"));
		}
		List<Settlement> towns = GetKingdomSettlements(kingdom).Where(s => s?.Town != null).ToList();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("RootFiefCount", "Primary policy territory count: towns/castles ") + towns.Count.ToString(CultureInfo.InvariantCulture));
		if (towns.Count > 0)
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("TownAverages", "Town/castle averages: prosperity=") + FormatNumber(towns.Average(s => s.Town.Prosperity))
				+ PolicyGenerationPromptTexts.Get("AverageFood", ", food=") + FormatNumber(towns.Average(s => s.Town.FoodStocks))
				+ PolicyGenerationPromptTexts.Get("AverageLoyalty", ", loyalty=") + FormatNumber(towns.Average(s => s.Town.Loyalty))
				+ PolicyGenerationPromptTexts.Get("AverageSecurity", ", security=") + FormatNumber(towns.Average(s => s.Town.Security))
				+ PolicyGenerationPromptTexts.Get("AverageMilitia", ", militia=") + FormatNumber(towns.Average(s => s.Militia)));
			if (includeAnomalies)
			{
				AppendTownExtremes(sb, towns);
			}
		}
		else
		{
			sb.AppendLine(PolicyGenerationPromptTexts.Get("NoTownAverages", "Town/castle averages: none"));
		}
	}

	private static void AppendTownExtremes(StringBuilder sb, List<Settlement> towns)
	{
		if (sb == null || towns == null || towns.Count == 0)
		{
			return;
		}
		Settlement lowProsperity = towns.OrderBy(s => s.Town.Prosperity).FirstOrDefault();
		Settlement lowFood = towns.OrderBy(s => s.Town.FoodStocks).FirstOrDefault();
		Settlement lowLoyalty = towns.OrderBy(s => s.Town.Loyalty).FirstOrDefault();
		Settlement lowSecurity = towns.OrderBy(s => s.Town.Security).FirstOrDefault();
		Settlement lowMilitia = towns.OrderBy(s => s.Militia).FirstOrDefault();
		Settlement highProsperity = towns.OrderByDescending(s => s.Town.Prosperity).FirstOrDefault();
		sb.AppendLine(PolicyGenerationPromptTexts.Get("TownExtremes", "Town/castle key values: lowest prosperity=") + FormatTownStat(lowProsperity, lowProsperity?.Town?.Prosperity ?? 0f)
			+ PolicyGenerationPromptTexts.Get("HighestProsperity", "; highest prosperity=") + FormatTownStat(highProsperity, highProsperity?.Town?.Prosperity ?? 0f)
			+ PolicyGenerationPromptTexts.Get("LowestFood", "; lowest food=") + FormatTownStat(lowFood, lowFood?.Town?.FoodStocks ?? 0f)
			+ PolicyGenerationPromptTexts.Get("LowestLoyalty", "; lowest loyalty=") + FormatTownStat(lowLoyalty, lowLoyalty?.Town?.Loyalty ?? 0f)
			+ PolicyGenerationPromptTexts.Get("LowestSecurity", "; lowest security=") + FormatTownStat(lowSecurity, lowSecurity?.Town?.Security ?? 0f)
			+ PolicyGenerationPromptTexts.Get("LowestMilitia", "; lowest militia=") + FormatTownStat(lowMilitia, lowMilitia?.Militia ?? 0f));
	}

	private static string FormatTownStat(Settlement settlement, float value)
	{
		return (settlement?.Name?.ToString() ?? settlement?.StringId ?? PolicyGenerationPromptTexts.Get("Unknown", "unknown")) + "=" + FormatNumber(value);
	}

	private static PolicyPostprocessResult BuildPostprocessResultFromMainAssessment(PolicyDraftRequest request, PolicyMainAssessmentResult assessment)
	{
		return new PolicyPostprocessResult
		{
			EffectPlanVersion = PolicyEffectPlanVersions.CurrentVersion,
			ImpactSummary = CleanPolicyDisplayText(assessment?.ImpactSummary ?? ""),
			Disposition = FirstNonEmpty(assessment?.EffectDisposition, "executable"),
			Reason = assessment?.EffectDispositionReason ?? string.Empty,
			DurationDays = assessment?.DurationDays,
			Effects = (assessment?.Effects ?? new List<PolicyEffectDto>())
				.Where(x => x != null)
				.Select(effect => ClonePolicyEffectForApplication(request, effect))
				.ToList()
		};
	}

	private static PolicyEffectDto ClonePolicyEffectForApplication(PolicyDraftRequest request, PolicyEffectDto effect)
	{
		if (effect == null)
		{
			return null;
		}
		return new PolicyEffectDto
		{
			EffectPlanVersion = effect.EffectPlanVersion,
			MechanismId = effect.MechanismId,
			MechanismKind = effect.MechanismKind,
			MechanismRole = effect.MechanismRole,
			SourceOmitted = effect.SourceOmitted,
			DestinationOmitted = effect.DestinationOmitted,
			ModuleId = effect.ModuleId,
			SourceModuleId = effect.SourceModuleId,
			TargetHandles = effect.TargetHandles == null ? null : new List<string>(effect.TargetHandles),
			Payload = effect.Payload?.DeepClone(),
			PreparedModuleEffect = effect.PreparedModuleEffect,
			Targets = effect.Targets == null ? null : new List<string>(effect.Targets),
			Changes = effect.Changes == null ? null : new Dictionary<string, float>(effect.Changes, StringComparer.Ordinal),
			TargetScope = effect.TargetScope,
			TargetHandle = effect.TargetHandle,
			TargetKingdomId = effect.TargetKingdomId,
			TargetKingdomName = effect.TargetKingdomName,
			LegacyFields = effect.LegacyFields == null
				? null
				: effect.LegacyFields.ToDictionary(pair => pair.Key, pair => pair.Value?.DeepClone(), StringComparer.OrdinalIgnoreCase),
			DurationDays = effect.DurationDays,
			Reason = effect.Reason
		};
	}

	private static PolicyMainAssessmentResult ParseMainAssessmentResult(string raw, PolicyDraftRequest request)
	{
		TryParseMainAssessmentResult(raw, request, out PolicyMainAssessmentResult result, out _, out _);
		return result;
	}

	private static bool TryParseMainAssessmentResult(
		string raw,
		PolicyDraftRequest request,
		out PolicyMainAssessmentResult result,
		out string errorCode,
		out string repairError)
	{
		result = null;
		errorCode = string.Empty;
		repairError = string.Empty;
		if (string.IsNullOrWhiteSpace(raw))
		{
			errorCode = "empty_output";
			repairError = PolicyCompatibilityTexts.Get("GenerationRaw_TryParseMainAssessmentResult_040");
			return false;
		}
		string json = ExtractJsonObject(raw);
		if (string.IsNullOrWhiteSpace(json))
		{
			errorCode = "missing_json_object";
			repairError = PolicyCompatibilityTexts.Get("GenerationRaw_TryParseMainAssessmentResult_041");
			return false;
		}
		try
		{
			result = DeserializeMainAssessmentResult(json, request);
			return result != null;
		}
		catch (Exception firstException)
		{
			string repairedJson = RepairJsonBoundaryQuotes(json);
			if (!string.Equals(repairedJson, json, StringComparison.Ordinal))
			{
				try
				{
					result = DeserializeMainAssessmentResult(repairedJson, request);
					return result != null;
				}
				catch (Exception repairedException)
				{
					SetPlayerPolicyMainParseDiagnostic(repairedException, out errorCode, out repairError);
					return false;
				}
			}
			SetPlayerPolicyMainParseDiagnostic(firstException, out errorCode, out repairError);
			return false;
		}
	}

	private static void SetPlayerPolicyMainParseDiagnostic(
		Exception exception,
		out string errorCode,
		out string repairError)
	{
		errorCode = exception is JsonReaderException
			? "invalid_json"
			: "contract_validation_failed";
		repairError = BuildBoundedPlayerPolicyRepairError(
			(exception?.GetType().Name ?? "JsonException") + ": "
			+ (exception?.Message ?? PolicyCompatibilityTexts.Get("GenerationRaw_SetPlayerPolicyMainParseDiagnostic_042")));
	}

	private static PolicyMainAssessmentResult DeserializeMainAssessmentResult(string json, PolicyDraftRequest request)
	{
		JObject parsed = JObject.Parse(json, new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore,
			DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
			LineInfoHandling = LineInfoHandling.Ignore
		});
		HashSet<string> expected = new HashSet<string>(new[]
		{
			"publicFeedback", "impactSummary", "numericIntent", "policyContentDigest", "feedbackDigest",
			"authoritarianWeight", "oligarchicWeight", "egalitarianWeight", "votePersonality", "effectDurationMode", "durationDays"
		}, StringComparer.Ordinal);
		if (request?.UseAiEvaluatedCost == true)
		{
			if (IsVassalPolicyRequest(request))
			{
				expected.Add("requiredGoldCost");
			}
			else
			{
				expected.Add("startupGoldCost");
				expected.Add("dailyMaintenanceGoldCost");
			}
		}
		if (IsVassalPolicyRequest(request))
		{
			expected.Add("vassalIndependenceDelta");
			expected.Add("vassalIndependenceReason");
		}
		List<string> actual = parsed.Properties().Select(property => property.Name).ToList();
		if (actual.Count != expected.Count
			|| actual.Distinct(StringComparer.Ordinal).Count() != actual.Count
			|| !new HashSet<string>(actual, StringComparer.Ordinal).SetEquals(expected))
		{
			throw new JsonException(PolicyCompatibilityTexts.Get("GenerationRaw_DeserializeMainAssessmentResult_043"));
		}
		foreach (string textField in new[]
		{
			"publicFeedback", "impactSummary", "numericIntent", "policyContentDigest", "feedbackDigest", "effectDurationMode"
		}.Concat(IsVassalPolicyRequest(request) ? new[] { "vassalIndependenceReason" } : Array.Empty<string>()))
		{
			if (parsed[textField]?.Type != JTokenType.String || string.IsNullOrWhiteSpace(parsed.Value<string>(textField)))
			{
				throw new JsonException(PolicyCompatibilityTexts.Get("GenerationRaw_DeserializeMainAssessmentResult_044") + textField);
			}
		}
		foreach (string numberField in expected.Where(field => field.EndsWith("Weight", StringComparison.Ordinal)
			|| field.EndsWith("GoldCost", StringComparison.Ordinal)
			|| string.Equals(field, "vassalIndependenceDelta", StringComparison.Ordinal)))
		{
			if (parsed[numberField] == null
				|| (parsed[numberField].Type != JTokenType.Integer && parsed[numberField].Type != JTokenType.Float)
				|| !double.TryParse(parsed[numberField].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
				|| double.IsNaN(number)
				|| double.IsInfinity(number))
			{
				throw new JsonException("First general review numeric field invalid: " + numberField);
			}
		}
		if (parsed["durationDays"]?.Type != JTokenType.Integer)
		{
			throw new JsonException("First general review durationDays must be integer.");
		}
		string durationMode = parsed.Value<string>("effectDurationMode")?.Trim();
		int durationDays = parsed.Value<int>("durationDays");
		if ((!string.Equals(durationMode, "permanent", StringComparison.Ordinal)
				&& !string.Equals(durationMode, "finite", StringComparison.Ordinal))
			|| (string.Equals(durationMode, "permanent", StringComparison.Ordinal) && durationDays != 0)
			|| (string.Equals(durationMode, "finite", StringComparison.Ordinal) && durationDays <= 0))
		{
			throw new JsonException("First general review duration mode inconsistent with durationDays.");
		}
		double[] politicalWeights =
		{
			parsed.Value<double>("authoritarianWeight"),
			parsed.Value<double>("oligarchicWeight"),
			parsed.Value<double>("egalitarianWeight")
		};
		if (politicalWeights.Any(value => value < -1d || value > 1d)
			|| politicalWeights.All(value => Math.Abs(value) < 0.000001d))
		{
			throw new JsonException("First general review political weights out of range or all zero.");
		}
		foreach (string costField in expected.Where(field => field.EndsWith("GoldCost", StringComparison.Ordinal)))
		{
			if (parsed.Value<double>(costField) < 0d)
			{
				throw new JsonException("First general review cost must not be negative: " + costField);
			}
		}
		JToken independenceToken = parsed["vassalIndependenceDelta"];
		if (independenceToken != null)
		{
			string rawValue = independenceToken.Type == JTokenType.Null ? "" : independenceToken.ToString();
			if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
				|| float.IsNaN(value)
				|| float.IsInfinity(value))
			{
				value = 0f;
			}
			parsed["vassalIndependenceDelta"] = value;
		}
		PolicyVotePersonalityProfile personality = PolicyVotePersonality.ReadRequired(parsed["votePersonality"]);
		PolicyMainAssessmentResult assessment = parsed.ToObject<PolicyMainAssessmentResult>();
		assessment.VotePersonality = personality;
		return assessment;
	}

	private static string RepairJsonBoundaryQuotes(string json)
	{
		if (string.IsNullOrEmpty(json) || (json.IndexOf('\u201c') < 0 && json.IndexOf('\u201d') < 0))
		{
			return json ?? "";
		}
		StringBuilder repaired = new StringBuilder(json.Length);
		bool inString = false;
		bool escaped = false;
		for (int index = 0; index < json.Length; index++)
		{
			char current = json[index];
			if (inString && escaped)
			{
				repaired.Append(current);
				escaped = false;
				continue;
			}
			if (inString && current == '\\')
			{
				repaired.Append(current);
				escaped = true;
				continue;
			}
			if (current == '"')
			{
				repaired.Append(current);
				inString = !inString;
				continue;
			}
			if (current == '\u201c' || current == '\u201d')
			{
				char previous = PreviousNonWhitespace(json, index - 1);
				char next = NextNonWhitespace(json, index + 1);
				bool opensBoundary = !inString && (previous == '\0' || previous == '{' || previous == '[' || previous == ',' || previous == ':');
				bool closesBoundary = inString && (next == ':' || next == ',' || next == '}' || next == ']');
				if (opensBoundary || closesBoundary)
				{
					repaired.Append('"');
					inString = !inString;
					continue;
				}
			}
			repaired.Append(current);
		}
		return repaired.ToString();
	}

	private static char PreviousNonWhitespace(string text, int index)
	{
		while (index >= 0)
		{
			if (!char.IsWhiteSpace(text[index]))
			{
				return text[index];
			}
			index--;
		}
		return '\0';
	}

	private static char NextNonWhitespace(string text, int index)
	{
		while (index < (text?.Length ?? 0))
		{
			if (!char.IsWhiteSpace(text[index]))
			{
				return text[index];
			}
			index++;
		}
		return '\0';
	}

	private static PolicyMainAssessmentResult NormalizeMainAssessmentResult(PolicyDraftRequest request, PolicyMainAssessmentResult assessment, string fallbackMainRaw)
	{
		assessment ??= new PolicyMainAssessmentResult();
		assessment.PublicFeedback = CleanPolicyDisplayText(assessment.PublicFeedback ?? "");
		if (string.IsNullOrWhiteSpace(assessment.PublicFeedback))
		{
			assessment.PublicFeedback = ExtractMainFeedbackForPopup(fallbackMainRaw);
		}
		if (string.IsNullOrWhiteSpace(assessment.PublicFeedback))
		{
			assessment.PublicFeedback = IsLocalPolicyRequest(request)
				? PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_075")
				: PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_074");
		}
		assessment.ImpactSummary = CleanPolicyDisplayText(assessment.ImpactSummary ?? "");
		if (string.IsNullOrWhiteSpace(assessment.ImpactSummary))
		{
			assessment.ImpactSummary = ExtractMainImpactSummaryForPopup(fallbackMainRaw);
		}
		if (string.IsNullOrWhiteSpace(assessment.ImpactSummary))
		{
			assessment.ImpactSummary = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_076");
		}
		assessment.EffectIntensity = CleanPolicyDisplayText(assessment.EffectIntensity ?? "");
		assessment.ExecutionReach = CleanPolicyDisplayText(assessment.ExecutionReach ?? "");
		assessment.DurationLogic = CleanPolicyDisplayText(assessment.DurationLogic ?? "");
		assessment.NumericIntent = CleanPolicyDisplayText(assessment.NumericIntent ?? "");
		assessment.PolicyContentDigest = CleanPolicyDisplayText(assessment.PolicyContentDigest ?? "");
		if (string.IsNullOrWhiteSpace(assessment.PolicyContentDigest))
		{
			assessment.PolicyContentDigest = assessment.ImpactSummary;
		}
		assessment.FeedbackDigest = CleanPolicyDisplayText(assessment.FeedbackDigest ?? "");
		if (string.IsNullOrWhiteSpace(assessment.FeedbackDigest))
		{
			assessment.FeedbackDigest = assessment.ImpactSummary;
		}
		if (IsVassalPolicyRequest(request))
		{
			assessment.VassalIndependenceDelta = VassalageBehavior.NormalizeVassalPolicyIndependenceDelta(assessment.VassalIndependenceDelta ?? 0f);
			assessment.VassalIndependenceReason = LimitDisplayChars(CleanPolicyDisplayText(assessment.VassalIndependenceReason ?? ""), 160);
			if (string.IsNullOrWhiteSpace(assessment.VassalIndependenceReason))
			{
				assessment.VassalIndependenceReason = PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_077");
			}
		}
		else
		{
			assessment.VassalIndependenceDelta = 0f;
			assessment.VassalIndependenceReason = "";
		}
		bool usesSparseEffectIr = (assessment.Effects ?? new List<PolicyEffectDto>()).Any(effect =>
				effect != null
				&& (effect.Targets != null || effect.Changes != null || !string.IsNullOrWhiteSpace(effect.TargetHandle)));
		assessment.UsesSparseEffectIr = usesSparseEffectIr;
		assessment.EffectIrValidationError = "";
		if (usesSparseEffectIr)
		{
			if (TryCompileSparsePolicyEffects(
				request,
				assessment.DurationDays,
				assessment.Effects,
				request?.SelectedEffectModuleIds,
				request?.SelectedEffectModuleIds,
				out List<PolicyEffectDto> compiledEffects,
				out string sparseError))
			{
				assessment.Effects = compiledEffects;
			}
			else
			{
				assessment.Effects = new List<PolicyEffectDto>();
				assessment.EffectIrValidationError = sparseError;
			}
		}
		else
		{
			assessment.Effects = NormalizeMainAssessmentEffects(request, assessment.Effects);
		}
		return assessment;
	}

	private static bool TryValidateLocalPolicyAssessment(PolicyDraftRequest request, PolicyMainAssessmentResult assessment, out string error)
	{
		error = "";
		if (!IsLocalPolicyRequest(request))
		{
			return true;
		}
		if (assessment?.UsesSparseEffectIr == true)
		{
			List<PolicyEffectDto> sparseEffects = assessment.Effects?.Where(x => x != null).ToList() ?? new List<PolicyEffectDto>();
			if (sparseEffects.Count <= 0)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_045");
				return false;
			}
			int duration = sparseEffects[0].DurationDays;
			// Generation stamps IsPermanentEffect before sparse validation. The
			// duration==0 fallback keeps legacy callers unambiguous without
			// reclassifying an explicitly finite, positive-duration assessment.
			bool permanent = request?.IsPermanentEffect == true
				|| (request?.ManualDurationDays <= 0 && duration == 0);
			if ((permanent ? duration != 0 : duration <= 0)
				|| sparseEffects.Any(x => x.DurationDays != duration))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_046");
				return false;
			}
			Dictionary<string, PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles)
				.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
			foreach (PolicyEffectDto effect in sparseEffects)
			{
				if (!handles.TryGetValue((effect.TargetHandle ?? "").Trim(), out PolicyTargetHandleSaveData target)
					|| !IsPolicyTargetHandleAllowedForRequest(request, target))
				{
					error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_047");
					return false;
				}
			}
			if (!TryValidateLocalPolicyPublicationTargetIdentity(sparseEffects, out string publicationTargetIdentity, out error))
			{
				return false;
			}
			PolicyDebugLog("local-policy-publication-target-validated", BuildPolicyRequestLogPrefix(request)
				+ " canonicalTarget=" + publicationTargetIdentity
				+ " sourceEffects=" + sparseEffects.Count(x => string.Equals(x.TargetHandle, "S", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture));
			return true;
		}
		bool hasMentionTargets = HasLocalPolicyMentionSelectors(request);
		int expectedEffectCount = hasMentionTargets ? 2 : 1;
		if (assessment?.Effects == null || assessment.Effects.Count != expectedEffectCount || assessment.Effects.Any(x => x == null))
		{
			error = hasMentionTargets ? PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_048") : PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_049");
			return false;
		}
		List<PolicyEffectDto> sourceEffects = assessment.Effects.Where(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase)).ToList();
		List<PolicyEffectDto> mentionedEffects = assessment.Effects.Where(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase)).ToList();
		if (sourceEffects.Count != 1 || (hasMentionTargets && mentionedEffects.Count != 1) || (!hasMentionTargets && mentionedEffects.Count != 0))
		{
			error = hasMentionTargets ? PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_050") : PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_051");
			return false;
		}
		PolicyEffectDto sourceEffect = sourceEffects[0];
		PolicyEffectDto mentionedEffect = mentionedEffects.FirstOrDefault();
		foreach (PolicyEffectDto effect in assessment.Effects)
		{
			string targetId = (effect.TargetKingdomId ?? "").Trim();
			string targetName = (effect.TargetKingdomName ?? "").Trim();
			bool targetIdInvalid = !string.IsNullOrWhiteSpace(targetId)
				&& (string.IsNullOrWhiteSpace(request.PlayerKingdomId) || !string.Equals(targetId, request.PlayerKingdomId, StringComparison.OrdinalIgnoreCase));
			bool targetNameInvalid = !string.IsNullOrWhiteSpace(targetName)
				&& (string.IsNullOrWhiteSpace(request.PlayerKingdomName) || !string.Equals(targetName, request.PlayerKingdomName, StringComparison.OrdinalIgnoreCase));
			if (targetIdInvalid || targetNameInvalid)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_052");
				return false;
			}
		}
		if (request.ManualDurationDays > 0)
		{
			sourceEffect.DurationDays = request.ManualDurationDays;
		}
		if (IsPermanentPlayerPolicyEffect(request))
		{
			sourceEffect.DurationDays = 0;
		}
		else if (sourceEffect.DurationDays <= 0)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyAssessment_053");
			return false;
		}
		if (mentionedEffect != null)
		{
			mentionedEffect.DurationDays = sourceEffect.DurationDays;
		}
		return true;
	}

	private static bool TryValidateLocalPolicyPublicationTargetIdentity(
		IReadOnlyList<PolicyEffectDto> effects,
		out string canonicalTargetIdentity,
		out string error)
	{
		canonicalTargetIdentity = string.Empty;
		error = string.Empty;
		List<PolicyEffectDto> sourceEffects = (effects ?? Array.Empty<PolicyEffectDto>())
			.Where(effect => effect != null
				&& string.Equals(effect.TargetHandle, "S", StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (sourceEffects.Count <= 0)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyPublicationTargetIdentity_054");
			return false;
		}

		if (sourceEffects.Count == 1
			&& string.IsNullOrWhiteSpace(sourceEffects[0].ModuleId)
			&& sourceEffects[0].PreparedModuleEffect?.Instance == null)
		{
			canonicalTargetIdentity = "S:lifecycle-anchor";
			return true;
		}

		List<string> identities = new List<string>(sourceEffects.Count);
		foreach (PolicyEffectDto effect in sourceEffects)
		{
			PolicyEffectCanonicalTargetSet targetSet = effect.PreparedModuleEffect?.Instance?.TargetSet;
			if (targetSet == null || !HasAnyPolicyEffectCanonicalTarget(targetSet))
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyPublicationTargetIdentity_055");
				return false;
			}
			List<string> parentSettlementIds = NormalizeIdList(targetSet.ParentSettlementIds);
			if (parentSettlementIds.Count == 0)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyPublicationTargetIdentity_056");
				return false;
			}
			identities.Add("P=" + string.Join(",", parentSettlementIds));
		}
		if (identities.Distinct(StringComparer.Ordinal).Count() != 1)
		{
			error = PolicyCompatibilityTexts.Get("GenerationRaw_TryValidateLocalPolicyPublicationTargetIdentity_057");
			return false;
		}
		canonicalTargetIdentity = identities[0];
		return true;
	}

	private static string NormalizePolicyChoice(string value, string fallback, params string[] allowed)
	{
		string text = (value ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			foreach (string option in allowed ?? Array.Empty<string>())
			{
				if (string.Equals(text, option, StringComparison.OrdinalIgnoreCase))
				{
					return option;
				}
			}
		}
		return fallback;
	}

	private static string ExtractJsonObject(string text)
	{
		text = (text ?? "").Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			text = Regex.Replace(text, "^```(?:json)?", "", RegexOptions.IgnoreCase).Trim();
			text = Regex.Replace(text, "```$", "", RegexOptions.IgnoreCase).Trim();
		}
		int start = text.IndexOf('{');
		int end = text.LastIndexOf('}');
		if (start < 0 || end <= start)
		{
			return "";
		}
		return text.Substring(start, end - start + 1);
	}

	private static string ResolveFeedbackText(PolicyGenerationResult result, PolicyDraftRequest request = null)
	{
		string structuredRaw = result?.MainAssessment?.PublicFeedback ?? "";
		string structuredFeedback = CleanPolicyDisplayText(structuredRaw);
		if (!string.IsNullOrWhiteSpace(structuredFeedback))
		{
			return structuredFeedback;
		}
		string mainFeedback = ExtractMainFeedbackForPopup(result?.MainRaw);
		if (!string.IsNullOrWhiteSpace(mainFeedback))
		{
			return mainFeedback;
		}
		return PolicyCompatibilityTexts.Get("Legacy_MapPlayerPolicyNarrativeFallbackToDisplay_074");
	}

	private static string BuildAiEvaluatedCostPaymentText(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return "";
		}
		return BuildPlayerPolicyGoldCostSummaryForDisplay(request)
			+ PolicyUiTexts.Format(
				"AFPOL_UI_PlayerPolicyAiCostSettlementScale",
				" (with {RESERVE} denars reserved for you). All policy effects apply at {SCALE}.",
				"RESERVE", AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture),
				"SCALE", FormatPercent(request.GoldEffectScale));
	}

	private static string BuildPlayerPolicyGoldCostSummary(PolicyDraftRequest request)
	{
		int startupRequired = request?.UseAiEvaluatedCost == true
			? Math.Max(0, request.RequiredGoldCost)
			: Math.Max(0, request?.GoldCost ?? 0);
		return PolicyCompatibilityTexts.Get("Legacy_BuildPlayerPolicyGoldCostSummary_053") + FormatGoldCostText(startupRequired)
			+ PolicyCompatibilityTexts.Get("Legacy_BuildPlayerPolicyGoldCostSummary_054") + FormatGoldCostText(Math.Max(0, request?.GoldCost ?? 0))
			+ PolicyCompatibilityTexts.Get("Legacy_BuildPlayerPolicyGoldCostSummary_055") + FormatGoldCostText(Math.Max(0, request?.DailyMaintenanceGoldCost ?? 0))
			+ PolicyCompatibilityTexts.Get("Legacy_BuildPlayerPolicyGoldCostSummary_056") + FormatGoldCostText(Math.Max(0, request?.TotalMaintenancePaidGold ?? 0));
	}

	private static string BuildPlayerPolicyGoldCostSummaryForDisplay(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return string.Empty;
		}
		int startupRequired = request.UseAiEvaluatedCost
			? Math.Max(0, request.RequiredGoldCost)
			: Math.Max(0, request.GoldCost);
		return PolicyUiTexts.Format(
			"AFPOL_UI_PlayerPolicyGoldCostSummary",
			"Startup required: {REQUIRED}; startup paid: {PAID}; daily maintenance: {DAILY}; cumulative maintenance: {TOTAL}",
			"REQUIRED", FormatGoldCostTextForDisplay(startupRequired),
			"PAID", FormatGoldCostTextForDisplay(Math.Max(0, request.GoldCost)),
			"DAILY", FormatGoldCostTextForDisplay(Math.Max(0, request.DailyMaintenanceGoldCost)),
			"TOTAL", FormatGoldCostTextForDisplay(Math.Max(0, request.TotalMaintenancePaidGold)));
	}

	private bool RecordSuccessfulPolicy(PolicyDraftRequest request, PolicyGenerationResult generationResult, string feedback, PolicyApplicationResult application, string recordId)
	{
		try
		{
			if (request == null || !HasAnyTimedPolicyEffect(application))
			{
				return false;
			}
			PolicyRecordSaveData record = new PolicyRecordSaveData
			{
				RecordId = string.IsNullOrWhiteSpace(recordId) ? Guid.NewGuid().ToString("N") : recordId,
				SubmittedDay = Math.Max(0, request.SubmittedDay),
				CreatedUtcTicks = DateTime.UtcNow.Ticks,
				DateText = request.DateText ?? "",
				PolicyName = LimitDisplayChars(request.PolicyName ?? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_060"), MaxPolicyNameChars),
				PolicyContentSummary = LimitDisplayChars(request.PolicyContent ?? "", MaxPolicyRecordContentChars),
				PublicFeedbackSummary = LimitDisplayChars(CleanPolicyDisplayText(feedback ?? ""), MaxPolicyRecordFeedbackChars),
				ImpactSummary = LimitDisplayChars(CleanPolicyDisplayText(generationResult?.Postprocess?.ImpactSummary ?? BuildRawPolicyEffectSummary(application)), MaxPolicyRecordImpactChars),
				ImpactEffectsSummary = LimitDisplayChars(BuildRawPolicyEffectSummary(application), MaxPolicyRecordImpactChars),
				PlayerKingdomId = request.PlayerKingdomId ?? "",
				PlayerKingdomName = request.PlayerKingdomName ?? "",
				UseAiEvaluatedCost = request.UseAiEvaluatedCost,
				RequiredGoldCost = Math.Max(0, request.RequiredGoldCost),
				IsPermanentEffect = request.IsPermanentEffect,
				DailyMaintenanceGoldCost = Math.Max(0, request.DailyMaintenanceGoldCost),
				TotalMaintenancePaidGold = Math.Max(0, request.TotalMaintenancePaidGold),
				MaintenanceFunded = request.MaintenanceFunded,
				LastMaintenanceSettlementDay = request.LastMaintenanceSettlementDay >= 0
					? request.LastMaintenanceSettlementDay
					: GetCurrentCampaignDay(),
				LastEffectProcessedDay = request.LastEffectProcessedDay >= 0
					? request.LastEffectProcessedDay
					: GetCurrentCampaignDay(),
				RequiredInfluenceCost = 0f,
				GoldEffectScale = request.GoldEffectScale,
				InfluenceEffectScale = request.GoldEffectScale,
				GoldCost = Math.Max(0, request.GoldCost),
				InfluenceCost = 0f,
				EvaluatorPromptIsDefault = request.EvaluatorPromptIsDefault
			};
			if (application?.KingdomEffects != null)
			{
				foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
				{
					record.Effects.Add(new PolicyRecordEffectSaveData
					{
						ModuleEffects = ClonePolicyEffectSaveDataList(effect.ModuleEffects),
						ExecutionReceipts = ClonePolicyEffectExecutionReceipts(effect.ExecutionReceipts),
						KingdomId = effect.KingdomId ?? "",
						KingdomName = effect.KingdomName ?? "",
						TargetHandle = effect.TargetHandle ?? "",
						TargetLabel = effect.TargetLabel ?? effect.KingdomName ?? "",
						TownCount = effect.TownCount,
						VillageCount = effect.VillageCount,
						EffectId = effect.EffectId ?? "",
						TotalDurationDays = effect.DurationDays,
						RemainingDays = effect.RemainingDays,
						IsPermanentEffect = effect.IsPermanentEffect,
						LastAppliedDay = Math.Max(0, request.SubmittedDay),
						IsEnded = false,
						Reason = LimitDisplayChars(effect.Reason ?? "", 120)
					});
				}
			}
			record.ImpactEffectsSummary = LimitDisplayChars(BuildRawPolicyRecordEffectSummary(record), MaxPolicyRecordImpactChars);
			_policyRecordHistory[record.RecordId] = JsonConvert.SerializeObject(record);
			TrimPolicyRecordHistory();
			if (!RegisterUnifiedPlayerPolicy(request, generationResult, feedback, application, record.RecordId, record.CreatedUtcTicks))
			{
				throw new InvalidOperationException("NPC unified policy record write failed");
			}
			return true;
		}
		catch (Exception ex)
		{
			PolicyDebugLog("history-record-failed", BuildPolicyRecordLogPrefix(request, recordId), ex.ToString());
			return false;
		}
	}

	private void RecordPolicyPublishAsPlayerAction(PolicyDraftRequest request, PolicyGenerationResult generationResult, PolicyApplicationResult application, string recordId)
	{
		try
		{
			if (request == null || !HasAnyTimedPolicyEffect(application) || string.IsNullOrWhiteSpace(recordId))
			{
				return;
			}
			string policySummary = ResolvePolicySummaryForPlayerAction(request, generationResult);
			string impactSummary = LimitDisplayChars(CleanPolicyDisplayText((generationResult?.Postprocess?.ImpactSummary ?? "").Trim()), 120);
			if (string.IsNullOrWhiteSpace(impactSummary))
			{
				impactSummary = LimitDisplayChars(BuildRawPolicyEffectSummary(application), 160);
			}
			string kingdomName = string.IsNullOrWhiteSpace(request.PlayerKingdomName) ? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_058") : request.PlayerKingdomName.Trim();
			string policyName = LimitDisplayChars(request.PolicyName ?? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_060"), MaxPolicyNameChars);
			string recentActionText = BuildPolicyRecentActionText(kingdomName, policyName, policySummary, impactSummary);
			string majorHistoryText = BuildPolicyMajorHistoryText(kingdomName, policyName, policySummary, impactSummary, application);
			string targetCultureId = ResolvePolicyTargetCultureId(request, application);
			string stableKey = "custom_policy_publish_recent:" + recordId;
			PlayerNotorietyBehavior.RecordPlayerActionForExternal(
				recentActionText,
				stableKey,
				"custom_policy_publish",
				isMajor: false,
				Math.Max(0, request.SubmittedDay),
				request.DateText ?? "",
				0,
				"",
				"",
				kingdomName,
				Hero.MainHero?.Culture?.StringId ?? "",
				targetCultureId,
				"",
				won: null);
			PlayerNotorietyBehavior.RecordPlayerHistoryMaterialForExternal(
				majorHistoryText,
				"custom_policy_publish_history:" + recordId,
				"custom_policy_publish",
				Math.Max(0, request.SubmittedDay),
				request.DateText ?? "",
				Hero.MainHero?.Culture?.StringId ?? "",
				targetCultureId,
				"");
		}
		catch (Exception ex)
		{
			PolicyDebugLog("player-action-record-failed", BuildPolicyRecordLogPrefix(request, recordId), ex.ToString());
		}
	}

	private static string BuildPolicyRecentActionText(string kingdomName, string policyName, string policySummary, string impactSummary)
	{
		string text = PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_057") + (string.IsNullOrWhiteSpace(kingdomName) ? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_058") : kingdomName.Trim()) + PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_059") + (policyName ?? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_060")).Trim() + "》";
		if (!string.IsNullOrWhiteSpace(policySummary))
		{
			text += "：" + policySummary.Trim();
		}
		if (!string.IsNullOrWhiteSpace(impactSummary))
		{
			text += PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_061") + impactSummary.Trim();
		}
		return LimitDisplayChars(CleanPolicyDisplayText(text.Trim().TrimEnd('。') + "。"), MaxPolicyRecentActionChars);
	}

	private static string BuildPolicyMajorHistoryText(string kingdomName, string policyName, string policySummary, string impactSummary, PolicyApplicationResult application)
	{
		string effectSummary = string.IsNullOrWhiteSpace(impactSummary) ? BuildRawPolicyEffectSummary(application) : impactSummary.Trim();
		string text = PolicyCompatibilityTexts.Get("Legacy_BuildPolicyMajorHistoryText_062") + (policyName ?? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyRecentActionText_060")).Trim() + "》";
		if (!string.IsNullOrWhiteSpace(kingdomName))
		{
			text += PolicyCompatibilityTexts.Get("Legacy_BuildPolicyMajorHistoryText_063") + kingdomName.Trim();
		}
		if (!string.IsNullOrWhiteSpace(policySummary))
		{
			text += PolicyCompatibilityTexts.Get("Legacy_BuildPolicyMajorHistoryText_064") + policySummary.Trim();
		}
		if (!string.IsNullOrWhiteSpace(effectSummary))
		{
			text += PolicyCompatibilityTexts.Get("Legacy_BuildPolicyMajorHistoryText_065") + LimitDisplayChars(CompactPolicyContextText(effectSummary), 80);
		}
		return LimitDisplayChars(CleanPolicyDisplayText(text.Trim().TrimEnd('。') + "。"), MaxPolicyMajorHistoryChars);
	}

	private static string BuildPolicyWeeklyMaterialEffectSummary(AppliedKingdomEffect effect)
	{
		if (effect == null)
		{
			return "";
		}
		List<string> values = PolicyEffectSaveCodec.DescribeRawPlayerVisibleInstances(effect.ModuleEffects);
		string text = (values.Count <= 0 ? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_066") : string.Join("，", values))
			+ (effect.IsPermanentEffect
				? PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_067")
				: PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_068") + Math.Max(0, effect.DurationDays).ToString(CultureInfo.InvariantCulture) + PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_069"));
		if (!string.IsNullOrWhiteSpace(effect.Reason))
		{
			text += PolicyCompatibilityTexts.Get("Legacy_BuildPolicyWeeklyMaterialEffectSummary_070") + LimitDisplayChars(CompactPolicyContextText(effect.Reason), 30);
		}
		return CleanPolicyDisplayText(text.Trim().TrimEnd('。') + "。");
	}

	private static string ResolvePolicySummaryForPlayerAction(PolicyDraftRequest request, PolicyGenerationResult generationResult)
	{
		string summary = CleanPolicyDisplayText(generationResult?.MainAssessment?.PolicyContentDigest ?? "");
		if (string.IsNullOrWhiteSpace(summary))
		{
			summary = (request?.PolicyContent ?? "").Trim();
		}
		return LimitDisplayChars(CleanPolicyDisplayText(summary), 140);
	}

	private string ResolvePolicyTargetCultureId(PolicyDraftRequest request, PolicyApplicationResult application)
	{
		try
		{
			if (application?.KingdomEffects != null)
			{
				foreach (AppliedKingdomEffect effect in application.KingdomEffects.Where(x => x != null))
				{
					Kingdom target = ResolveKingdomByIdOrName(effect.KingdomId, effect.KingdomName);
					string cultureId = (target?.Culture?.StringId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(cultureId))
					{
						return cultureId;
					}
				}
			}
			Kingdom playerKingdom = ResolveKingdomByIdOrName(request?.PlayerKingdomId, request?.PlayerKingdomName);
			return (playerKingdom?.Culture?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	internal static bool TryCapturePolicyHistoryEntriesForNpcExternal(
		out List<NpcPolicyHistoryEntry> entries,
		out string error, int minimumDay = 0)
	{
		entries = new List<NpcPolicyHistoryEntry>();
		error = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				error = PolicyCompatibilityTexts.Get("GenerationRaw_TryCapturePolicyHistoryEntriesForNpcExternal_058");
				return false;
			}
			entries = behavior.CapturePlayerPolicyHistoryEntriesForNpc(minimumDay);
			return true;
		}
		catch (Exception ex)
		{
			entries = new List<NpcPolicyHistoryEntry>();
			error = ex.Message ?? PolicyCompatibilityTexts.Get("GenerationRaw_TryCapturePolicyHistoryEntriesForNpcExternal_059");
			return false;
		}
	}

	private void CaptureUnifiedPolicyHistoryForRequest(PolicyDraftRequest request, Kingdom anchorKingdom)
	{
		if (request == null)
		{
			return;
		}
		if (!NpcRulerPolicyBehavior.TryCaptureUnifiedPolicyHistorySnapshotForExternal(
			out List<NpcPolicyHistoryEntry> historyEntries,
			out string historyError))
		{
			historyEntries = CapturePlayerPolicyHistoryEntriesForNpc();
			PolicyDebugLog("policy-history-snapshot-fallback", BuildPolicyRequestLogPrefix(request)
				+ " error=" + (historyError ?? string.Empty));
		}
		request.PolicyHistoryEntries = historyEntries ?? new List<NpcPolicyHistoryEntry>();
		request.EnemyKingdoms = new List<PolicyEnemyKingdomSnapshot>();
		PolicyDebugLog("policy-history-snapshot", BuildPolicyRequestLogPrefix(request)
			+ " entries=" + request.PolicyHistoryEntries.Count.ToString(CultureInfo.InvariantCulture)
			+ " enemies=" + request.EnemyKingdoms.Count.ToString(CultureInfo.InvariantCulture));
	}

	private static void RetrieveUnifiedPolicyHistoryForRequest(
		PolicyDraftRequest request,
		PolicyTextEmbeddingSession embeddingSession,
		string queryText,
		long runtimeGeneration)
	{
		if (request == null)
		{
			return;
		}
		request.PolicyHistoryRetrieval = PolicyHistoryRetrievalService.RetrieveForEvaluation(
			embeddingSession,
			queryText,
			request.PolicyHistoryEntries,
			request.PlayerKingdomId,
			request.GenerationSettings.PolicyRecentActiveCount,
			request.GenerationSettings.PolicyRelatedActiveCount,
			runtimeGeneration);
	}

	private List<NpcPolicyHistoryEntry> CapturePlayerPolicyHistoryEntriesForNpc(int minimumDay = 0)
	{
		Dictionary<string, PolicyRecordSaveData> kingdomHistory = LoadPolicyRecordHistory(minimumDay)
			.Where(record => record != null && !string.IsNullOrWhiteSpace(record.RecordId))
			.GroupBy(record => record.RecordId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		List<NpcPolicyHistoryEntry> result = new List<NpcPolicyHistoryEntry>();
		foreach (DynamicPolicySaveData policy in LoadDynamicPolicies(minimumDay > 0 ? new HashSet<string>(kingdomHistory.Keys, StringComparer.OrdinalIgnoreCase) : null))
		{
			if (policy == null || !TryMapPlayerPolicyHistoryStatus(policy.Status, out string policyStatus))
			{
				continue;
			}
			string recordId = FirstNonEmpty(policy.RecordId, policy.PolicyObjectId).Trim();
			kingdomHistory.TryGetValue(recordId, out PolicyRecordSaveData history);
			NpcPolicyHistoryEntry entry = new NpcPolicyHistoryEntry
			{
				EntryId = recordId,
				SourceKind = "player_kingdom",
				IsHistoryDeleted = IsPolicyHistoryDeleted("player_kingdom", recordId),
				ScopeKind = PolicyScopeKingdom,
				OwnerKingdomId = FirstNonEmpty(policy.OwnerKingdomId, history?.PlayerKingdomId),
				OwnerKingdomName = history?.PlayerKingdomName ?? string.Empty,
				OwnerClanId = FirstNonEmpty(policy.ProposerClanId, Clan.PlayerClan?.StringId),
				IssuerKingdomId = FirstNonEmpty(policy.IssuerKingdomId, policy.OwnerKingdomId, history?.PlayerKingdomId),
				IssuerKingdomName = history?.PlayerKingdomName ?? string.Empty,
				PolicyName = FirstNonEmpty(policy.PolicyName, history?.PolicyName),
				PolicyContent = FirstNonEmpty(policy.PolicyContent, history?.PolicyContentSummary),
				ImpactSummary = FirstNonEmpty(history?.ImpactEffectsSummary, history?.ImpactSummary, policy.SecondaryEffects),
				DiplomacyImpactSummary = LimitDisplayChars(BuildPolicyRecordEffectSummary(history, includeRemainingDays: false), MaxPolicyRecordImpactChars),
				DiplomacyRevisionKey = string.Join(";", (history?.Effects ?? new List<PolicyRecordEffectSaveData>())
					.Where(effect => effect != null).Select(effect => effect.EffectId + ":" + effect.TotalDurationDays.ToString(CultureInfo.InvariantCulture))
					.OrderBy(value => value, StringComparer.Ordinal)),
				PolicyStatus = policyStatus,
				RawPolicyStatus = (policy.Status ?? string.Empty).Trim().ToLowerInvariant(),
				HistoryBucket = PolicyHistoryRetrievalService.ResolveHistoryBucketFromStatus(policy.Status),
				EffectStatus = string.Equals(policyStatus, "abolished", StringComparison.Ordinal)
					? "ended_by_abolition"
					: ResolvePlayerKingdomPolicyHistoryEffectStatus(history),
				PublishedDay = Math.Max(0, history?.SubmittedDay ?? 0),
				CreatedUtcTicks = Math.Max(policy.CreatedUtcTicks, history?.CreatedUtcTicks ?? 0)
			};
			AddPolicyHistoryId(entry.TargetKingdomIds, entry.OwnerKingdomId);
			foreach (PolicyRecordEffectSaveData effect in history?.Effects ?? new List<PolicyRecordEffectSaveData>())
			{
				AddPolicyHistoryId(entry.TargetKingdomIds, effect?.KingdomId);
				AddPolicyHistoryEffectSummaries(
					entry,
					PolicyEffectSaveCodec.DescribeRawPlayerVisibleInstances(effect?.ModuleEffects),
					FirstNonEmpty(effect?.TargetLabel, effect?.KingdomName, effect?.KingdomId));
				foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				{
					AddPolicyHistoryTargetSet(entry, instance?.TargetSet);
				}
			}
			NormalizePolicyHistoryEntry(entry);
			if (IsUsablePolicyHistoryEntry(entry))
			{
				result.Add(entry);
			}
		}

		foreach (LocalPolicyRecordSaveData policy in LoadLocalPolicyRecords(minimumDay))
		{
			if (policy == null || !TryMapLocalPolicyHistoryStatus(policy.Status, out string policyStatus))
			{
				continue;
			}
			bool isVassal = string.Equals(policy.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
			NpcPolicyHistoryEntry entry = new NpcPolicyHistoryEntry
			{
				EntryId = policy.RecordId ?? string.Empty,
				SourceKind = isVassal ? "player_vassal" : "player_local",
				IsHistoryDeleted = IsPolicyHistoryDeleted(isVassal ? "player_vassal" : "player_local", policy.RecordId),
				IsDeclarationOnly = !policy.Effects.Any(effect => effect?.ModuleEffects?.Count > 0),
				ScopeKind = isVassal ? PolicyScopeVassal : PolicyScopeLocal,
				OwnerKingdomId = FirstNonEmpty(policy.TargetKingdomId, policy.IssuerKingdomId),
				OwnerKingdomName = FirstNonEmpty(policy.TargetKingdomName, policy.IssuerKingdomName),
				OwnerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
				IssuerKingdomId = policy.IssuerKingdomId ?? string.Empty,
				IssuerKingdomName = policy.IssuerKingdomName ?? string.Empty,
				PolicyName = policy.PolicyName ?? string.Empty,
				PolicyContent = policy.PolicyContent ?? string.Empty,
				ImpactSummary = FirstNonEmpty(policy.ImpactSummary, policy.EffectReason),
				DiplomacyRevisionKey = policy.RenewalCount.ToString(CultureInfo.InvariantCulture) + ":" + policy.OriginalDurationDays.ToString(CultureInfo.InvariantCulture),
				PolicyStatus = policyStatus,
				RawPolicyStatus = (policy.Status ?? string.Empty).Trim().ToLowerInvariant(),
				HistoryBucket = PolicyHistoryRetrievalService.ResolveHistoryBucketFromStatus(policy.Status),
				EffectStatus = string.Equals(policy.Status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
					? "ended_by_abolition"
					: NormalizeLocalPolicyHistoryEffectStatus(policy.EffectStatus),
				PublishedDay = Math.Max(0, policy.SubmittedDay),
				CreatedUtcTicks = Math.Max(0, policy.CreatedUtcTicks)
			};
			AddPolicyHistoryId(entry.TargetKingdomIds, policy.TargetKingdomId);
			foreach (string settlementId in policy.TargetFiefIds ?? new List<string>())
			{
				AddPolicyHistoryId(entry.TargetSettlementIds, settlementId);
			}
			foreach (LocalPolicyEffectRecordSaveData effect in policy.Effects ?? new List<LocalPolicyEffectRecordSaveData>())
			{
				AddPolicyHistoryId(entry.TargetKingdomIds, effect?.TargetKingdomId);
				AddPolicyHistoryEffectSummaries(
					entry,
					PolicyEffectSaveCodec.DescribeRawPlayerVisibleInstances(effect?.ModuleEffects),
					FirstNonEmpty(effect?.TargetLabel, effect?.TargetKingdomName, effect?.TargetKingdomId));
				foreach (string clanId in effect?.TargetClanIds ?? new List<string>())
				{
					AddPolicyHistoryId(entry.TargetClanIds, clanId);
				}
				foreach (string settlementId in effect?.DirectTargetSettlementIds ?? new List<string>())
				{
					AddPolicyHistoryId(entry.TargetSettlementIds, settlementId);
				}
				foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				{
					AddPolicyHistoryTargetSet(entry, instance?.TargetSet);
				}
			}
			NormalizePolicyHistoryEntry(entry);
			if (IsUsablePolicyHistoryEntry(entry))
			{
				result.Add(entry);
			}
		}
		return result
			.GroupBy(entry => entry.SourceKind + ":" + entry.EntryId, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.OrderByDescending(entry => entry.PublishedDay).ThenByDescending(entry => entry.CreatedUtcTicks).First())
			.ToList();
	}

	private static bool TryMapPlayerPolicyHistoryStatus(string status, out string mapped)
	{
		mapped = string.Empty;
		if (string.Equals(status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, DynamicPolicyStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase))
		{
			mapped = "active";
			return true;
		}
		if (string.Equals(status, DynamicPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase))
		{
			mapped = "abolished";
			return true;
		}
		return false;
	}

	private static bool TryMapLocalPolicyHistoryStatus(string status, out string mapped)
	{
		mapped = string.Empty;
		if (string.Equals(status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			mapped = "active";
			return true;
		}
		if (string.Equals(status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, LocalPolicyStatusTargetsLost, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase))
		{
			mapped = "abolished";
			return true;
		}
		return false;
	}

	private static string ResolvePlayerKingdomPolicyHistoryEffectStatus(PolicyRecordSaveData record)
	{
		List<PolicyRecordEffectSaveData> effects = (record?.Effects ?? new List<PolicyRecordEffectSaveData>())
			.Where(effect => effect != null)
			.ToList();
		if (effects.Count == 0)
		{
			return "none";
		}
		return effects.Any(effect => effect.IsPermanentEffect || (!effect.IsEnded && effect.RemainingDays > 0))
			? "active"
			: "expired";
	}

	private static string NormalizeLocalPolicyHistoryEffectStatus(string status)
	{
		return string.Equals(status, LocalPolicyStatusActive, StringComparison.OrdinalIgnoreCase)
			? "active"
			: "expired";
	}

	private static void AddPolicyHistoryTargetSet(NpcPolicyHistoryEntry entry, PolicyEffectCanonicalTargetSet targetSet)
	{
		if (entry == null || targetSet == null)
		{
			return;
		}
		foreach (string id in targetSet.KingdomIds ?? new List<string>()) AddPolicyHistoryId(entry.TargetKingdomIds, id);
		foreach (string id in targetSet.ClanIds ?? new List<string>()) AddPolicyHistoryId(entry.TargetClanIds, id);
		foreach (string id in targetSet.SettlementIds ?? new List<string>()) AddPolicyHistoryId(entry.TargetSettlementIds, id);
		foreach (string id in targetSet.TownIds ?? new List<string>()) AddPolicyHistoryId(entry.TargetSettlementIds, id);
		foreach (string id in targetSet.VillageIds ?? new List<string>()) AddPolicyHistoryId(entry.TargetSettlementIds, id);
	}

	private static void AddPolicyHistoryId(List<string> values, string value)
	{
		string normalized = (value ?? string.Empty).Trim();
		if (normalized.Length > 0 && values != null && !values.Contains(normalized, StringComparer.OrdinalIgnoreCase))
		{
			values.Add(normalized);
		}
	}

	private static void AddPolicyHistoryEffectSummaries(
		NpcPolicyHistoryEntry entry,
		IEnumerable<string> summaries,
		string targetLabel)
	{
		if (entry == null)
		{
			return;
		}
		string target = CompactPolicyContextText(targetLabel ?? string.Empty);
		foreach (string summary in summaries ?? Enumerable.Empty<string>())
		{
			string compact = CompactPolicyContextText(summary ?? string.Empty);
			if (compact.Length == 0)
			{
				continue;
			}
			string value = target.Length == 0 ? compact : target + "：" + compact;
			if (!entry.EffectSummaries.Contains(value, StringComparer.Ordinal))
			{
				entry.EffectSummaries.Add(value);
			}
		}
	}

	private static void NormalizePolicyHistoryEntry(NpcPolicyHistoryEntry entry)
	{
		if (entry == null)
		{
			return;
		}
		entry.EntryId = (entry.EntryId ?? string.Empty).Trim();
		entry.OwnerKingdomId = (entry.OwnerKingdomId ?? string.Empty).Trim();
		entry.OwnerKingdomName = CompactPolicyContextText(entry.OwnerKingdomName ?? string.Empty);
		entry.OwnerClanId = (entry.OwnerClanId ?? string.Empty).Trim();
		entry.IssuerKingdomId = (entry.IssuerKingdomId ?? string.Empty).Trim();
		entry.IssuerKingdomName = CompactPolicyContextText(entry.IssuerKingdomName ?? string.Empty);
		entry.RawPolicyStatus = (entry.RawPolicyStatus ?? entry.PolicyStatus ?? string.Empty).Trim().ToLowerInvariant();
		entry.HistoryBucket = PolicyHistoryRetrievalService.ResolveHistoryBucket(entry);
		entry.PolicyName = CompactPolicyContextText(entry.PolicyName ?? string.Empty);
		entry.PolicyContent = CompactPolicyContextText(entry.PolicyContent ?? string.Empty);
		entry.ImpactSummary = CompactPolicyContextText(entry.ImpactSummary ?? string.Empty);
		entry.EffectSummaries = (entry.EffectSummaries ?? new List<string>())
			.Select(value => CompactPolicyContextText(value ?? string.Empty))
			.Where(value => value.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToList();
		entry.TargetKingdomIds = entry.TargetKingdomIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.Ordinal).ToList();
		entry.TargetClanIds = entry.TargetClanIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.Ordinal).ToList();
		entry.TargetSettlementIds = entry.TargetSettlementIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.Ordinal).ToList();
	}

	private static bool IsUsablePolicyHistoryEntry(NpcPolicyHistoryEntry entry)
	{
		return PolicyHistoryRetrievalService.IsValidEntry(entry);
	}

}
