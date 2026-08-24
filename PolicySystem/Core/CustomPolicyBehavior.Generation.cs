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
	private void CompleteLocalPolicyGeneration(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		List<Settlement> validFiefs = ResolveOwnedLocalPolicyFiefs(request?.SelectedFiefIds);
		if (validFiefs.Count <= 0)
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策已取消", "评议期间已失去全部目标封地，因此未扣费、未生效、未写入成功记录。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
			return;
		}
		request.SelectedFiefIds = validFiefs.Select(x => x.StringId).ToList();
		PolicyRuntimeOptions options = BuildPolicyRuntimeOptions(request);
		PolicyEligibility eligibility = EvaluateLocalPolicyEligibility(options, hasOwnedFief: true);
		if (!eligibility.CanPublish)
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策无法发布", eligibility.Reason + "\n\n评议已经完成，但未扣费、未生效、未写入成功记录。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
			return;
		}
		if (!TryPrepareLocalPolicyCostForApplication(request, result.MainAssessment, out string costError))
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策评议失败", BuildPolicyFailurePopupText(costError, result) + "\n\n未扣费、未生效。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
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
			InformationManager.ShowInquiry(new InquiryData("地方政策发布失败", "没有生成可执行的地方数值效果，未扣费、未生效。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
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
			InformationManager.ShowInquiry(new InquiryData("地方政策发布失败", "后处理没有生成可执行数值效果，未扣费、未写入记录。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
			return;
		}
		string recordId = Guid.NewGuid().ToString("N");
		string feedback = ResolveFeedbackText(result, request);
		string activeEffectId = string.Empty;
		if (requiresEffectBundle
			&& !TryActivatePolicyEffectApplication(request, application, recordId, isRenewal: false, out activeEffectId, out string activationError))
		{
			InformationManager.ShowInquiry(new InquiryData("地方政策发布失败", BuildPolicyFailurePopupText(activationError, result) + "\n\n未扣费、未写入成功记录。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
			return;
		}
		PolicyPublishCostReceipt costReceipt = new PolicyPublishCostReceipt();
		try
		{
			RecordSuccessfulLocalPolicy(request, result, feedback, application.KingdomEffects, recordId, validFiefs);
			if (!_localPolicyRecords.ContainsKey(recordId))
			{
				throw new InvalidOperationException("地方政策成功记录写入失败");
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
					rollbackFailures.Add("付款退款失败：" + refundError);
				}
			}
			catch (Exception refundException)
			{
				rollbackFailures.Add("付款退款异常：" + refundException.Message);
			}
			try
			{
				if (!RollbackAndRemovePolicyEffectBundle(activeEffectId, "local-publication-commit-failed", out string rollbackError))
				{
					rollbackFailures.Add("效果 bundle 回滚失败：" + rollbackError);
				}
			}
			catch (Exception rollbackException)
			{
				rollbackFailures.Add("效果 bundle 回滚异常：" + rollbackException.Message);
			}
			try
			{
				_localPolicyRecords.Remove(recordId);
			}
			catch (Exception recordRollbackException)
			{
				rollbackFailures.Add("本地记录回滚失败：" + recordRollbackException.Message);
			}
			InformationManager.ShowInquiry(new InquiryData("地方政策发布失败", "提交政策时发生错误，已撤销效果且未保留成功记录。详细技术信息已写入日志。"
				+ (rollbackFailures.Count == 0 ? string.Empty : "\n部分回滚步骤未完成，请查看日志。"), true, false, "知道了", "", null, null), pauseGameActiveState: true);
			return;
		}
		InvokeLocalPolicyLifecycleMemoryHook("published", recordId, validFiefs.Select(fief => fief.StringId));
		TrimLocalPolicyRecords();
		string impactText = BuildImpactPopupText(request, feedback, application, costDeducted: true);
		ShowPolicySuccessResultPopup("local:" + recordId, impactText);
		PolicySystemLog.Write("Local", "published", BuildPolicyRecordLogPrefix(request, recordId)
			+ " sourceTargets=" + string.Join(",", validFiefs.Select(fief => fief.StringId))
			+ " effectCount=" + application.KingdomEffects.Count.ToString(CultureInfo.InvariantCulture)
			+ " disposition=" + (result.Postprocess?.Disposition ?? string.Empty)
			+ " duration=" + (sourceEffect?.DurationDays ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " paid=" + request.GoldCost.ToString(CultureInfo.InvariantCulture));
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
			InformationManager.ShowInquiry(new InquiryData("附庸国政策已取消", "评议期间宗主关系或统治者身份已经变化，因此政策未生效、未增加独立度、未写入成功记录。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
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
			InformationManager.ShowInquiry(new InquiryData("附庸国政策发布失败", "没有生成作用于目标附庸国的有效持续效果，政策未生效、未增加独立度。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
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
				"附庸关系无法冻结为可提交状态，政策未生效、独立度未变更。");
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
				throw new InvalidOperationException("附庸国政策本地记录写入失败");
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
				throw new InvalidOperationException("NPC 统一政策记录写入失败");
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
				"政策提交失败，已撤销效果与本地/统一记录；独立度尚未变更。");
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
				"独立度提交结果暂时无法确认；政策已保留为外部提交待对账状态，不会重复增加独立度。");
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
				"独立度提交前置状态已变化或提交未发生；AF 效果与记录已安全撤销。");
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
		RunVassalPolicyPostCommitStep(recordId, "presentation", () =>
		{
			if (!NpcRulerPolicyBehavior.TryPublishPlayerPolicyPresentationForExternal(recordId))
			{
				throw new InvalidOperationException("附庸国政策展示未全部发布，已保留持久化待重试标记");
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
			+ "\n\n独立度：" + appliedBefore.ToString(CultureInfo.InvariantCulture)
			+ " + 发布费用 " + request.VassalPublicationIndependenceCost.ToString(CultureInfo.InvariantCulture)
			+ " + 政策修正 " + FormatSigned(request.VassalQualityIndependenceDelta)
			+ " = " + independenceAfter.ToString(CultureInfo.InvariantCulture) + "/100"
			+ "\n当前脱离阈值：" + breakawayThreshold.ToString(CultureInfo.InvariantCulture) + "（" + rulerName + "关系 " + FormatSigned(rulerRelation) + "）"
			+ (brokeAway ? "\n目标附庸国已达到脱离阈值；脱离是不可逆最终提交，臣属关系和全部持续效果已经终止。" : "");
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
			throw new InvalidOperationException("附庸政策外部提交记录不存在");
		}
		LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(
			JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw ?? string.Empty));
		if (record == null)
		{
			throw new InvalidOperationException("附庸政策外部提交记录不可读");
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
				failures.Add("NPC 统一记录撤销失败");
			}
		}
		catch (Exception ex)
		{
			failures.Add("NPC 统一记录撤销异常：" + ex.Message);
		}
		bool bundleRolledBack = true;
		try
		{
			bundleRolledBack = RollbackAndRemovePolicyEffectBundle(activeEffectId, reason, out string rollbackError);
			if (!bundleRolledBack)
			{
				failures.Add("效果 bundle 回滚失败：" + rollbackError);
			}
		}
		catch (Exception ex)
		{
			bundleRolledBack = false;
			failures.Add("效果 bundle 回滚异常：" + ex.Message);
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
			failures.Add("本地记录回滚/挂起失败：" + ex.Message);
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
			error = "附庸国政策本地记录不存在";
			return false;
		}
		LocalPolicyRecordSaveData record;
		try
		{
			record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
		}
		catch (Exception ex)
		{
			error = "附庸国政策本地记录解析失败：" + ex.Message;
			return false;
		}
		if (record == null)
		{
			error = "附庸国政策本地记录为空";
			return false;
		}
		record.Status = LocalPolicyStatusRelationshipEnded;
		record.EndReason = FirstNonEmpty(reason, "臣属关系终止");
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
				"附庸国政策发布失败",
				BuildPolicyFailurePopupText(message ?? "政策提交失败。", null),
				true,
				false,
				"知道了",
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
		try
		{
			action?.Invoke();
		}
		catch (Exception ex)
		{
			try
			{
				PolicySystemLog.Write("VassalPolicy", "post-commit-step-failed", "recordId=" + (recordId ?? string.Empty)
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
				"政策议程批准时第纳尔不足，需要 "
				+ requiredGold.ToString(CultureInfo.InvariantCulture)
				+ "，当前仅有 "
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
			error = "政策请求或主评议为空";
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
			error = "候选/详规模块快照无效";
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
				? "规范效果包含空实例"
				: "规范效果超出冻结的源模块授权："
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
				error = "已编译效果壳缺少 canonical module/payload/target 数据或仍含旧字段";
				return false;
			}
			List<PolicyEffectInstanceSaveData> savedShells = CreatePolicyEffectSaveDataList(effect.PreparedModuleEffect);
			if (savedShells.Count != 1)
			{
				error = "已编译效果壳没有唯一 module instance";
				return false;
			}
			PolicyEffectInstanceSaveData shell = savedShells[0];
			if (!string.Equals(shell.ModuleId ?? string.Empty, effect.ModuleId.Trim(), StringComparison.Ordinal))
			{
				error = "已编译效果壳的 moduleId 与 prepared instance 不一致";
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
			error = "规范效果实例超过 " + MaxCompiledPolicyEffectInstances.ToString(CultureInfo.InvariantCulture) + " 个";
			return false;
		}
		foreach (PolicyEffectInstanceSaveData instance in moduleEffects)
		{
			if (!PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out string normalizeError)
				|| normalized?.IsInert == true
				|| normalized?.SaveData == null)
			{
				error = "规范效果实例校验失败：" + FirstNonEmpty(normalizeError, normalized?.InertReason);
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
			result.Error = "政策请求为空。";
			return result;
		}
		request.IsPermanentEffect = request.ManualDurationDays <= 0;
		CancellationTokenSource evaluationTimeout = new CancellationTokenSource(PlayerPolicyEvaluationTimeoutMilliseconds);
		try
		{
			if (request.GenerationSettings == null
				&& !TryFreezePlayerPolicyGenerationSettings(request, out string freezeError))
			{
				result.FailureStage = "冻结生成快照";
				result.Error = freezeError;
				return result;
			}
			PolicyGenerationSettingsSnapshot settings = request.GenerationSettings;
			PolicyApiExecutionProfile apiProfile = settings.ApiProfile?.Clone();
			if (apiProfile == null)
			{
				result.FailureStage = "冻结生成快照";
				result.Error = "冻结的政策 API 配置不可用。";
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
				result.FailureStage = "历史政策召回";
				result.Error = "历史政策语义召回失败。";
				PolicySystemLog.Failure("Player", "history-retrieval-failed",
					BuildPolicyRequestLogPrefix(request),
					"exceptionType=" + ex.GetType().Name
					+ " messageHash=" + PolicyTextEmbeddingSession.StableTextHash(ex.Message));
				return result;
			}
			PolicyHistoryRetrievalResult history = request.PolicyHistoryRetrieval;
			PolicySystemLog.Write("Player", "historyRetrieved",
				BuildPolicyRequestLogPrefix(request)
				+ " current=" + (history?.RelatedCurrentPolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " historical=" + (history?.RelatedHistoricalPolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " enemies=" + (history?.EnemyWithPolicyCount ?? 0).ToString(CultureInfo.InvariantCulture));

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
			result.MainAssessment = ParseMainAssessmentResult(result.MainRaw, request);
			if (result.MainAssessment == null)
			{
				result.FailureStage = "第一次通用评议解析";
				result.Error = "政策主评判未返回可解析的结构化结果。";
				PolicySystemLog.Failure("Player", "llm-main-parse-failed", BuildPolicyRequestLogPrefix(request), "structuredResult=false");
				return result;
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
				result.FailureStage = "第一次通用评议校验";
				result.Error = "政策主评议没有返回有效的正整数持续天数。";
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
					result.FailureStage = "第一次通用评议校验";
					result.Error = "政策主评议没有返回三个合法且不全为零的意识形态权重，允许范围为 -1 到 1。";
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
				result.FailureStage = "效果模块召回";
				result.Error = "效果模块语义召回失败。";
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
				routing.Details.Select(selection => selection.Module).ToArray(),
				out string targetDirectoryError))
			{
				result.FailureStage = "政策目标解析";
				result.Error = targetDirectoryError;
				return result;
			}
			PolicyTargetHandleDirectory targetDirectory = EnsurePlayerPolicyTargetHandleDirectory(request);
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
			string targetDirectoryText = SerializePlayerPolicyTargetHandleDirectory(targetDirectory);
			PolicySystemLog.Write("Player", "targetDirectoryBuilt",
				BuildPolicyRequestLogPrefix(request)
				+ " targetCount=" + (targetDirectory.Targets?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " capabilityCount=" + (targetDirectory.Capabilities?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " pairCount=" + (targetDirectory.Capabilities?.Values.Sum(capability => capability?.AllowedTargetHandles?.Count ?? 0) ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " targetHash=" + PolicyTextEmbeddingSession.StableTextHash(targetDirectoryText));

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
				PolicySystemLog.Failure("Player", "policy-effect-postprocess-failed",
					BuildPolicyRequestLogPrefix(request), postprocessError);
				result.FailureStage = "效果方案校验";
				result.Error = BuildPlayerPolicyEffectFailureSummary(postprocessErrorKind);
				return result;
			}
			result.Postprocess = postprocess;
		}
		catch (OperationCanceledException) when (evaluationTimeout.IsCancellationRequested)
		{
			result.FailureStage = FirstNonEmpty(result.FailureStage, "API 请求");
			result.Error = "政策评议超过 3 分钟，网络请求已取消。请检查网络与智能服务状态后重试。";
			PolicySystemLog.Failure("Player", "evaluation-timeout", BuildPolicyRequestLogPrefix(request), "timeout=true");
		}
		catch (Exception ex)
		{
			result.FailureStage = FirstNonEmpty(result.FailureStage, "API 请求或本地处理");
			result.Error = "政策评议请求发生异常，详细技术信息已写入日志。";
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
			error = "政策目标目录上下文无效。";
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
			error = "政策明确目标无法安全解析：" + string.Join("；", planRoute.Issues
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
				error = "政策明确目标计划包含缺锚、非法引用或不可解析的对象。";
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
				error = "政策明确目标越权或引用无效：" + (explicitHandle.Key ?? string.Empty);
				return false;
			}
			if (!TryMapPolicyEffectSelectorKind(explicitHandle, out PolicyEffectTargetKind explicitKind)
				|| !modules.Any(module => module.Descriptor.AllowedSelectorKinds.Contains(explicitKind)))
			{
				error = "政策明确目标种类与实际注入模块不兼容：" + (explicitHandle.Key ?? string.Empty);
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
		string detail = string.IsNullOrWhiteSpace(reason) ? "政策评议失败。" : reason.Trim();
		string detailWithoutIntentLegIds = Regex.Replace(detail, @"\bI\d{1,2}L\d{1,2}\b", string.Empty);
		if (detail.IndexOf("【模型回复（完整）】", StringComparison.Ordinal) >= 0
			|| Regex.IsMatch(detailWithoutIntentLegIds, "[A-Za-z]"))
		{
			return "政策评议或效果处理返回了无效的技术数据。详细信息已写入日志。";
		}
		return detail;
	}

	private static string BuildPlayerPolicyEffectFailureSummary(PlayerPolicyEffectValidationErrorKind errorKind)
	{
		if (errorKind == PlayerPolicyEffectValidationErrorKind.IncompleteLinkedMechanism)
		{
			return "模型返回的政策效果联动来源或去向不完整，系统已安全拒绝。";
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.UnknownOrUnauthorizedTargetHandle)
		{
			return "模型返回了未知或未授权的政策效果目标句柄，系统已安全拒绝。";
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.UnauthorizedModuleTargetPair)
		{
			return "模型返回的政策效果能力与目标句柄组合不兼容，系统已安全拒绝。";
		}
		if (errorKind == PlayerPolicyEffectValidationErrorKind.InvalidStructure)
		{
			return "模型返回的政策效果缺少必需字段或字段类型不正确，系统已安全拒绝。";
		}
		return "模型返回的政策效果方案未通过结构与安全校验，系统已安全拒绝。";
	}

	private static string BuildPolicyGenerationFailurePopupText(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		string requestId = (request?.RequestId ?? string.Empty).Trim();
		if (requestId.Length == 0 || requestId.Length > 80
			|| requestId.Any(character => !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != ':'))
		{
			requestId = "不可用";
		}
		return "阶段：" + FirstNonEmpty(result?.FailureStage, "政策评议")
			+ "\n原因：" + BuildPolicyFailurePopupText(result?.Error, result)
			+ "\n请求编号：" + requestId
			+ "\n\n可点击下方“手动重试”重新提交原政策，或点击右上角 X 关闭。"
			+ "\n未扣除费用，也未应用任何效果。";
	}

	private void ShowPolicyGenerationRetryPopup(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		string body = BuildPolicyGenerationFailurePopupText(request, result);
		Action retry = delegate { RetryPolicyGenerationFromFailurePopup(request); };
		if (CustomPolicyResultPopup.ShowRetry("政策评议失败", body, "手动重试", retry))
		{
			return;
		}
		InformationManager.ShowInquiry(new InquiryData(
			"政策评议失败",
			body,
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"手动重试",
			"关闭",
			retry,
			null),
			pauseGameActiveState: true);
	}

	private void CompletePolicyGeneration(PolicyDraftRequest request, PolicyGenerationResult result)
	{
		try
		{
			EndPolicyWaitPause("completed", request);
			_generationInProgress = false;
			if (result == null)
			{
				PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request) + " parsedEffects=0 appliedEffects=0 costDeducted=false status=null_result");
				InformationManager.ShowInquiry(new InquiryData("政策评议失败", "政策评议没有返回结果，未扣除费用。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
				return;
			}
			if (!string.IsNullOrWhiteSpace(result.Error))
			{
				PolicyDebugLog("complete-failed", BuildPolicyRequestLogPrefix(request) + " generation error: " + result.Error);
				PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request)
					+ " parsedEffects=" + CountParsedPolicyEffects(result).ToString(CultureInfo.InvariantCulture)
					+ " appliedEffects=0 costDeducted=false status=generation_failed");
				ShowPolicyGenerationRetryPopup(request, result);
				return;
			}
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
				InformationManager.ShowInquiry(new InquiryData("政策评议失败", BuildPolicyFailurePopupText(costError ?? "政策消耗评估无效。", result) + "\n\n未扣除费用，也未应用效果。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
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
				InformationManager.ShowInquiry(new InquiryData("政策无法发布", BuildPolicyFailurePopupText(eligibility.Reason, result) + "\n\n政策评议已经完成，但发布条件已变化，因此未扣除费用，也未应用效果。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
				return;
			}
			PolicyApplicationResult application = ApplyPolicyEffects(request, result.Postprocess);
			string feedback = ResolveFeedbackText(result, request);
			string recordId = Guid.NewGuid().ToString("N");
			if (!TryReadPoliticalWeights(result.MainAssessment?.AuthoritarianWeight, result.MainAssessment?.OligarchicWeight, result.MainAssessment?.EgalitarianWeight,
				out float authoritarianWeight, out float oligarchicWeight, out float egalitarianWeight))
			{
				throw new InvalidOperationException("政策政治权重缺失或无效");
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
			throw new InvalidOperationException("政策议程规范效果快照失败：" + pendingError);
		}
			DynamicPolicySaveData dynamicPolicy = new DynamicPolicySaveData
			{
				Version = 4,
				CommitState = "pending",
				PolicyObjectId = DynamicPolicyIdPrefix + recordId,
				RecordId = recordId,
				Source = "player",
				OwnerKingdomId = request.PlayerKingdomId ?? "",
				ProposerClanId = Clan.PlayerClan?.StringId ?? "",
				IssuerKingdomId = request.IssuerKingdomId ?? request.PlayerKingdomId ?? "",
				PolicyName = request.PolicyName ?? "",
				PolicyContent = request.PolicyContent ?? "",
				LogEntryDescription = FirstNonEmpty(result.MainAssessment?.PolicyContentDigest, request.PolicyContent),
				SecondaryEffects = BuildPolicyEffectSummary(application)
					+ "\n" + BuildPlayerPolicyGoldCostSummary(request),
				AuthoritarianWeight = authoritarianWeight,
				OligarchicWeight = oligarchicWeight,
				EgalitarianWeight = egalitarianWeight,
				Status = DynamicPolicyStatusPending,
				CreatedUtcTicks = DateTime.UtcNow.Ticks,
				PlayerPayloadJson = JsonConvert.SerializeObject(pending)
			};
			SubmitPlayerPolicyAgenda(request, result, application, dynamicPolicy);
		}
		catch (Exception ex)
		{
			_generationInProgress = false;
			EndPolicyWaitPause("exception", request);
			PolicyDebugLog("complete-exception", BuildPolicyRequestLogPrefix(request), ex.ToString());
			PolicyDebugLog("policy-complete", BuildPolicyRequestLogPrefix(request) + " parsedEffects=0 appliedEffects=0 costDeducted=false status=exception");
			Log("complete policy failed: " + ex);
			InformationManager.ShowInquiry(new InquiryData("政策发布失败", BuildPolicyFailurePopupText("政策评议完成后的落地处理失败，详细技术信息已写入日志。", result) + "\n\n未确认成功时不应重复点击；请查看日志。", true, false, "知道了", "", null, null), pauseGameActiveState: true);
		}
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
					"政策无法提交",
					BuildPolicyFailurePopupText(eligibility.Reason, result)
						+ "\n\n确认前发布条件已变化，因此未扣除费用，也未应用效果。",
					true,
					false,
					"知道了",
					string.Empty,
					null,
					null),
					pauseGameActiveState: true);
				return;
			}
			if (!TrySubmitDynamicPolicyAgenda(dynamicPolicy, out string agendaError))
			{
				throw new InvalidOperationException("政策提交 AF 议程失败：" + agendaError);
			}
			string impactText = "政策《" + (request?.PolicyName ?? string.Empty) + "》已提交 AF 王国议程。议程通过前不会扣除政策成本，也不会产生数值效果。\n"
				+ BuildPlayerPolicyGoldCostSummary(request);
			PolicyDebugLog("complete-agenda-submitted", BuildPolicyRecordLogPrefix(request, recordId)
				+ " costDeducted=false status=pending", impactText);
			PolicyDebugLog("policy-complete", BuildPolicyRecordLogPrefix(request, recordId)
				+ " parsedEffects=" + CountParsedPolicyEffects(result).ToString(CultureInfo.InvariantCulture)
				+ " appliedEffects=" + (application?.AppliedEffectCount ?? 0).ToString(CultureInfo.InvariantCulture)
				+ " costDeducted=false status=agenda_pending");
			InformationManager.ShowInquiry(new InquiryData("政策已提交议程", impactText, true, false, "知道了", string.Empty, null, null), pauseGameActiveState: true);
			Log("policy agenda submitted " + BuildPolicyRecordLogPrefix(request, recordId)
				+ " effects=" + (application?.AppliedEffectCount ?? 0).ToString(CultureInfo.InvariantCulture));
		}
		catch (Exception ex)
		{
			PolicyDebugLog("policy-agenda-submit-failed", BuildPolicyRecordLogPrefix(request, recordId), ex.ToString());
			InformationManager.ShowInquiry(new InquiryData(
				"政策提交失败",
				"政策未能提交议程，详细技术信息已写入日志。\n\n未扣除费用，也未应用效果。",
				true,
				false,
				"知道了",
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
			return "填写政策名和政策内容后即可发布。智能服务会评估完整执行所需第纳尔；若第纳尔不足，将为你保留 " + AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + " 第纳尔，并按实际投入比例折算全部效果。";
		}
		return "填写政策名和政策内容后即可发布。智能服务完成评议且成功落地时扣除：" + FormatCostText(options) + "。无冷却限制，可连续发布。";
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
			? goldCost.ToString(CultureInfo.InvariantCulture) + " 第纳尔"
			: "不消耗第纳尔";
	}

	private static string FormatCostText(int goldCost, float influenceCost)
	{
		bool hasGold = goldCost > 0;
		bool hasInfluence = influenceCost > 0.0001f;
		if (!hasGold && !hasInfluence)
		{
			return "不消耗第纳尔或影响力";
		}
		if (hasGold && hasInfluence)
		{
			return goldCost.ToString(CultureInfo.InvariantCulture) + " 第纳尔、" + FormatNumber(influenceCost) + " 影响力";
		}
		if (hasGold)
		{
			return goldCost.ToString(CultureInfo.InvariantCulture) + " 第纳尔";
		}
		return FormatNumber(influenceCost) + " 影响力";
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
			return PolicyEligibility.Blocked("上一份政策仍在等待评议。");
		}
		Kingdom playerKingdom = GetPlayerKingdom();
		if (playerKingdom == null)
		{
			return PolicyEligibility.Blocked("你尚未加入任何王国，不能提交全国政策。");
		}
		if (!IsPlayerRuler(playerKingdom))
		{
			return PolicyEligibility.Blocked("只有王国统治者才能提交全国政策；拥有城镇或城堡的非统治者请使用地方政策。");
		}
		if (options.UseAiEvaluatedCost)
		{
			return PolicyEligibility.Allowed();
		}
		if ((Hero.MainHero?.Gold ?? 0) < options.GoldCost)
		{
			return PolicyEligibility.Blocked("发布政策需要 " + options.GoldCost.ToString(CultureInfo.InvariantCulture) + " 第纳尔。");
		}
		return PolicyEligibility.Allowed();
	}

	private PolicyEligibility EvaluateLocalPolicyEligibility(PolicyRuntimeOptions options, bool hasOwnedFief)
	{
		options ??= BuildPolicyRuntimeOptions();
		if (_generationInProgress)
		{
			return PolicyEligibility.Blocked("上一份政策仍在等待评议。");
		}
		if (!hasOwnedFief)
		{
			return PolicyEligibility.Blocked("玩家家族当前没有城镇或城堡，不能发布地方政策。");
		}
		int currentGold = Math.Max(0, Hero.MainHero?.Gold ?? 0);
		if (options.UseAiEvaluatedCost)
		{
			return PolicyEligibility.Allowed();
		}
		if (currentGold < options.GoldCost)
		{
			return PolicyEligibility.Blocked("发布地方政策需要 " + options.GoldCost.ToString(CultureInfo.InvariantCulture) + " 第纳尔。");
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
			throw new InvalidOperationException("政策影响力支付数值无效");
		}
		Hero goldPayer = Hero.MainHero;
		Clan influencePayer = Clan.PlayerClan;
		if (goldCost > 0 && (goldPayer == null || goldPayer.Gold < goldCost))
		{
			throw new InvalidOperationException("政策支付时玩家第纳尔不足或玩家角色不存在");
		}
		if (influenceCost > 0f && (influencePayer == null || influencePayer.Influence + 0.0001f < influenceCost))
		{
			throw new InvalidOperationException("政策支付时玩家影响力不足或玩家氏族不存在");
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
			throw new InvalidOperationException("政策支付未精确扣除预期金额");
		}
	}

	private static bool TryRefundPublishCost(PolicyPublishCostReceipt receipt, out string failureReason)
	{
		List<string> failures = new List<string>();
		if ((receipt?.DeductedGold ?? 0) > 0)
		{
			if (receipt.GoldPayer == null)
			{
				failures.Add("第纳尔付款人丢失");
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
					failures.Add("第纳尔退款异常：" + ex.Message);
				}
				int refunded = Math.Max(0, receipt.GoldPayer.Gold - before);
				if (refunded != receipt.DeductedGold)
				{
					failures.Add("第纳尔应退 " + receipt.DeductedGold.ToString(CultureInfo.InvariantCulture)
						+ " 实退 " + refunded.ToString(CultureInfo.InvariantCulture));
				}
			}
		}
		if ((receipt?.DeductedInfluence ?? 0f) > 0.0001f)
		{
			if (receipt.InfluencePayer == null)
			{
				failures.Add("影响力付款氏族丢失");
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
					failures.Add("影响力退款异常：" + ex.Message);
				}
				float refunded = Math.Max(0f, receipt.InfluencePayer.Influence - before);
				if (Math.Abs(refunded - receipt.DeductedInfluence) > 0.0001f)
				{
					failures.Add("影响力应退 " + receipt.DeductedInfluence.ToString("0.###", CultureInfo.InvariantCulture)
						+ " 实退 " + refunded.ToString("0.###", CultureInfo.InvariantCulture));
				}
			}
		}
		failureReason = string.Join("; ", failures);
		return failures.Count == 0;
	}

	private static bool TryPreparePolicyCostForApplication(PolicyDraftRequest request, PolicyMainAssessmentResult assessment, out string error)
	{
		error = "";
		if (request == null)
		{
			error = "政策请求丢失。";
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
			error = "地方政策请求丢失。";
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
			error = "AI 消耗模式要求主评判返回 startupGoldCost 与 dailyMaintenanceGoldCost。";
			return false;
		}
		float rawStartupGold = assessment.StartupGoldCost.Value;
		float rawDailyGold = assessment.DailyMaintenanceGoldCost.Value;
		if (float.IsNaN(rawStartupGold) || float.IsInfinity(rawStartupGold) || rawStartupGold < 0f
			|| float.IsNaN(rawDailyGold) || float.IsInfinity(rawDailyGold) || rawDailyGold < 0f)
		{
			error = "AI 返回的政策消耗不合法：两个第纳尔字段都必须是有限非负数字。";
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
			parts.Add("政策名：" + request.PolicyName.Trim());
		}
		if (!string.IsNullOrWhiteSpace(request?.PlayerKingdomName))
		{
			parts.Add("玩家王国：" + request.PlayerKingdomName.Trim());
		}
		string content = CompactPolicyContextText(request?.PolicyContent ?? "");
		if (!string.IsNullOrWhiteSpace(content))
		{
			parts.Add("政策内容：" + LimitDisplayChars(content, 700));
		}
		string entityHints = BuildPolicyExplicitEntityHintText(request);
		if (!string.IsNullOrWhiteSpace(entityHints))
		{
			parts.Add(entityHints);
		}
		return LimitDisplayChars(CompactPolicyContextText(string.Join("；", parts)), 1000);
	}

	private static string BuildPolicyKnowledgeSecondaryInputForMainOnly(PolicyDraftRequest request)
	{
		PolicyPromptContextBundle context = request?.PromptContext ?? new PolicyPromptContextBundle();
		List<string> parts = new List<string>();
		parts.Add("当前日期：" + (string.IsNullOrWhiteSpace(request?.DateText) ? FormatCurrentCampaignDate() : request.DateText.Trim()));
		parts.Add("玩家王国：" + (request?.PlayerKingdomName ?? "") + " | ID=" + (request?.PlayerKingdomId ?? ""));
		parts.Add("自定义政策链路：主处理一次性完成政策摘要、目标识别、知识库上下文使用、民众反馈、每日数值、持续天数和最终 JSON；effects 是最终落地数据。");
		if (!string.IsNullOrWhiteSpace(context.PolicyRuleContext))
		{
			parts.Add("政策链路规则：" + CompactPolicyContextText(context.PolicyRuleContext));
		}
		if (!string.IsNullOrWhiteSpace(context.WorldContextCompact))
		{
			parts.Add("世界上下文精简：" + LimitDisplayChars(CompactPolicyContextText(context.WorldContextCompact), 1600));
		}
		return LimitDisplayChars(CompactPolicyContextText(string.Join("；", parts)), 2400);
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
		NpcPolicyApiCallResult apiResult = await PolicyLlmClient.CallPolicyApiWithRetriesAsync(
			messageArray,
			callProfile,
			PlayerPolicyEvaluationTimeoutMilliseconds,
			string.IsNullOrWhiteSpace(source) ? "PlayerPolicy" : source,
			runtimeGeneration,
			1,
			cancellationToken);
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
		throw new InvalidOperationException(string.IsNullOrWhiteSpace(apiResult?.ErrorMessage) ? "政策 API 请求失败。" : apiResult.ErrorMessage);
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
			? "【地方政策评议】只评价冻结发布地与原文明示对象可能受到的直接影响；不得扩大范围。"
				+ (isPermanent
					? "期限固定为 permanent，durationDays=0。"
					: "期限固定为 finite，durationDays=" + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + "。")
			: isVassalPolicy
				? "【附庸政策评议】从附庸国利益、自治、尊严、负担和安全判断 vassalIndependenceDelta（-15..15）：受益或认可为负，受损、受辱或压迫为正，中性为 0；"
					+ (isPermanent
						? "期限固定为 permanent，durationDays=0。"
						: "期限固定为 finite，durationDays=" + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + "。")
				: isPermanent
					? "【全国政策期限】期限固定为 permanent，durationDays=0；永久影响应明显弱于同类有限影响。"
					: "【全国政策期限】期限固定为 finite，durationDays=" + request.ManualDurationDays.ToString(CultureInfo.InvariantCulture) + "。";
		string costRules = useAiCost
			? isVassalPolicy
				? "评估完整执行所需的非负 requiredGoldCost。"
				: "分别评估非负 startupGoldCost 与 dailyMaintenanceGoldCost；它们属于政策事务。"
			: "费用由 MCM 固定值决定，本次不输出费用字段。";
		string dynamicSystem = JoinPolicyPromptSections(
			request?.EvaluatorPrompt,
			"【通用评议规则】\n" + policyRules,
			scopeRules,
			"【本次参数】\n" + costRules + " publicFeedback 使用第三人称，约 "
				+ feedbackChars.ToString(CultureInfo.InvariantCulture) + " 个中文字符，不得伪造已经发生的事实。");
		string costSchema = useAiCost
			? isVassalPolicy
				? "- requiredGoldCost:number，有限非负数字。\n"
				: "- startupGoldCost:number，有限非负数字。\n- dailyMaintenanceGoldCost:number，有限非负数字。\n"
			: string.Empty;
		string user = "【世界上下文（冻结快照）】\n" + (context.WorldContextFull ?? string.Empty)
			+ (string.IsNullOrWhiteSpace(knowledgeContext)
				? string.Empty
				: "\n\n【知识库上下文（只读召回）】\n" + knowledgeContext.Trim())
			+ "\n\n【扩展上下文（冻结快照）】\n" + (context.ExtensionContext ?? string.Empty)
			+ (string.IsNullOrWhiteSpace(request?.PolicyHistoryRetrieval?.CombinedPrompt)
				? string.Empty
				: "\n\n" + request.PolicyHistoryRetrieval.CombinedPrompt)
			+ "\n\n【政策原文】\n名称：" + (request?.PolicyName ?? string.Empty)
			+ "\n日期：" + (request?.DateText ?? string.Empty)
			+ "\n内容：\n" + (request?.PolicyContent ?? string.Empty)
			+ "\n\n只输出包含以下字段的 JSON 对象：\n"
			+ "- publicFeedback:string，玩家可见的第三人称社会反应。\n"
			+ "- impactSummary:string，概括直接社会、政治、财政、军事与治理影响；同一措施可合理一阶推出的不同机械后果应逐项列出，不要只写最显眼的一项。\n"
			+ "- numericIntent:string，只用自然语言逐项保留正文明确的金额、倍率、百分比、范围、方向，以及每日/一次性/持续等时间表达；它不是执行合同，不得补造数值。没有直接数值意图时写“无直接数值意图”。\n"
			+ "- policyContentDigest:string，一句短句概括目的、主要措施和对象。\n"
			+ "- feedbackDigest:string，一句短句概括主要支持、反对和担忧。\n"
			+ (isVassalPolicy
				? "- vassalIndependenceDelta:number，范围 -15 到 15。\n- vassalIndependenceReason:string，一句短句。\n"
				: string.Empty)
			+ "- authoritarianWeight:number，范围 [-1,1]。\n"
			+ "- oligarchicWeight:number，范围 [-1,1]。\n"
			+ "- egalitarianWeight:number，范围 [-1,1]；三项不得全为 0。\n"
			+ costSchema
			+ "- effectDurationMode:string，只能是 permanent 或 finite。\n"
			+ "- durationDays:number，permanent 时为 0，finite 时为正整数。";
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
			? "（无）"
			: PolicyEffectModuleCatalog.BuildMainInstructions(effectScope, injectedIds);
		string payloadRules = injectedIds.Count == 0
			? "（无）"
			: PolicyEffectModuleCatalog.BuildPayloadPromptRules(effectScope, injectedIds);
		string scopeRule = isLocalPolicy
			? "地方政策只能使用目录中的合法 S/L*/C*/R*/H*/P* 句柄，不得扩大到未冻结对象。"
			: isVassalPolicy
				? "附庸政策只能使用目录中的合法 K*/H*/P* 句柄；K0 为附庸国，K1 仅在宗主国确有直接变化时可用。"
				: "全国政策只能使用目录中的合法 K*/H*/P* 句柄；外国对象必须由原文明确点名并通过 C# 权限校验。";
		string dynamicSystem = "【实际注入能力的适用语义】\n" + understandingRules
			+ "\n\n【实际注入能力的详细载荷契约】\n" + payloadRules
			+ "\n\n【作用域与目标规则】\n" + scopeRule
			+ "\n" + BuildPolicyTargetSelectionPromptRule(request);
		string user = "【政策原文（语义最高权威）】\n名称：" + (request?.PolicyName ?? string.Empty)
			+ "\n内容：\n" + (request?.PolicyContent ?? string.Empty)
			+ "\n\n【第一次通用评议结果】\npublicFeedback：" + (assessment?.PublicFeedback ?? string.Empty)
			+ "\nimpactSummary：" + (assessment?.ImpactSummary ?? string.Empty)
			+ "\nnumericIntent：" + (assessment?.NumericIntent ?? string.Empty)
			+ "\npolicyContentDigest：" + (assessment?.PolicyContentDigest ?? string.Empty)
			+ "\nfeedbackDigest：" + (assessment?.FeedbackDigest ?? string.Empty)
			+ "\nstartupGoldCost：" + (assessment?.StartupGoldCost ?? 0f).ToString("0.###", CultureInfo.InvariantCulture)
			+ "\ndailyMaintenanceGoldCost：" + (assessment?.DailyMaintenanceGoldCost ?? 0f).ToString("0.###", CultureInfo.InvariantCulture)
			+ "\neffectDurationMode：" + (assessment?.EffectDurationMode ?? string.Empty)
			+ "\ndurationDays：" + (assessment?.DurationDays ?? 0).ToString(CultureInfo.InvariantCulture)
			+ "\n\n【本次全部合法目标句柄目录（结构化 JSON）】\n"
			+ SerializePlayerPolicyTargetHandleDirectory(targetDirectory)
			+ "\n\n【严格输出合同】\n"
			+ PolicyEffectDirectPlanContract.BuildOutputContract(
				requireExecutable: false,
				requireSingleTargetPerEffect: false)
			+ "由你逐项自主判断是否选择能力：只要政策措施到该能力存在合理、可说明的直接或紧邻一阶因果链即可，不要求原文逐字命名游戏指标；完全无因果关系、目标不匹配或结算语义不符时才省略。允许选择零个、一个或多个，不设数量偏好。";
		return BuildChatMessages(
			PlayerPolicyEffectStableSystemPrefix + "\n【不可编辑的通用执行定标】\n" + PlayerPolicyEffectCommonCalibration,
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
		PolicyDebugLog("target-plan-route", BuildPolicyRequestLogPrefix(request)
			+ " stage=" + (stage ?? string.Empty)
			+ " intentLeg=" + (intentLeg ?? string.Empty)
			+ " explicit=" + (result.HasExplicitTargetIntent ? "true" : "false")
			+ " candidates=" + result.Candidates.Count.ToString(CultureInfo.InvariantCulture)
			+ " blocking=" + (result.ShouldRejectPolicy ? "true" : "false"));
		foreach (PolicyTargetPlanRouteIssue issue in result.Issues.Where(issue => issue != null))
		{
			bool provisionalStage = string.Equals(stage, "pre-main", StringComparison.Ordinal);
			string eventName = issue.Kind == PolicyTargetPlanRouteIssueKind.NoIntent
				? "target-plan-fallback"
				: result.ShouldRejectPolicy && !provisionalStage
					? "target-plan-blocked"
					: "target-plan-candidate-rejected";
			PolicyDebugLog(eventName, BuildPolicyRequestLogPrefix(request)
				+ " stage=" + (stage ?? string.Empty)
				+ " intentLeg=" + (intentLeg ?? string.Empty)
				+ " kind=" + issue.Kind
				+ " evidence=" + (issue.EvidenceKind ?? string.Empty)
				+ (string.IsNullOrWhiteSpace(issue.Message) ? string.Empty : " error=" + issue.Message));
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
			error = "主评议缺少正整数 durationDays";
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
			error = "未返回可解析的 JSON";
			errorKind = PlayerPolicyEffectValidationErrorKind.InvalidStructure;
			return false;
		}
		try
		{
			string json = ExtractJsonObject(raw);
			if (string.IsNullOrWhiteSpace(json))
			{
				error = "未返回可解析的 JSON";
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
			error = "后处理 JSON 解析失败：" + ex.Message;
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
				error = "effects 每一项都必须是对象";
				return false;
			}
			Dictionary<string, JProperty> fields = effect.Properties()
				.ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
			string unknownField = fields.Keys.FirstOrDefault(field => !allowedFields.Contains(field));
			if (unknownField != null)
			{
				error = "EffectPlan effect 包含未知字段：" + unknownField;
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
				error = "EffectPlan effect 缺少必需字段或字段类型无效";
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
				error = "effects 每一项都必须是对象";
				return false;
			}
			Dictionary<string, JProperty> properties = new Dictionary<string, JProperty>(StringComparer.OrdinalIgnoreCase);
			foreach (JProperty property in effect.Properties())
			{
				if (properties.ContainsKey(property.Name))
				{
					error = "effect 字段重复：" + property.Name;
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
				error = "effects 不得混用稀疏结构和旧版扁平字段";
				return false;
			}
			if (hasSparseShape && hasModuleShape)
			{
				error = "effects 不得混用 module payload 结构和旧 changes 结构";
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
					error = "旧版扁平 effect 包含未知字段：" + propertyName;
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
					error = "旧版扁平 effect 数值无效：" + property.Key;
					return false;
				}
				if (!mappedModules.Add(moduleId))
				{
					error = "旧版扁平 effect 同时提供了重复量纲：" + moduleId;
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
				error = "旧版扁平 effect targetScope 无效：" + requestedScope;
				return false;
			}
		}
		if (candidates.Count != 1)
		{
			error = candidates.Count <= 0 ? "旧版扁平 effect 目标不合法" : "旧版扁平 effect 目标不唯一";
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
			PolicyRuleContext = BuildPolicyRuleContext() + "\n- 地方政策作用于发布地及 C# 确定性解析的合法目标；发布地与其他目标使用独立效果，王国稳定度固定为 0。",
			WorldContextCompact = localContext,
			WorldContextFull = localContext,
			ExtensionContext = "（地方政策当前不写入 NPC/AFEF 记忆；不要从其他交流链路引入目标或事实。）"
		};
	}

	private string BuildLocalPolicyWorldContext(List<Settlement> selectedFiefs, Kingdom playerKingdom, PolicyRuntimeOptions options, LocalPolicyMentionTargetSelection mentionTargets)
	{
		List<Settlement> fiefs = (selectedFiefs ?? new List<Settlement>()).Where(IsPlayerOwnedLocalPolicyFief).ToList();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("当前日期：" + FormatCurrentCampaignDate());
		sb.AppendLine("玩家：" + (Hero.MainHero?.Name?.ToString() ?? "玩家") + "；玩家家族=" + (Clan.PlayerClan?.Name?.ToString() ?? "未知") + "；所属王国=" + (playerKingdom == null ? "无（独立家族）" : GetKingdomName(playerKingdom)));
		sb.AppendLine("玩家资源：第纳尔=" + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture));
		if (options?.UseAiEvaluatedCost == true)
		{
			sb.AppendLine("费用模式：AI 评估完整地方执行成本；实际结算至少保留 " + LocalPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + " 第纳尔，资金不足会按实际投入比例缩放全部地方效果。费用按 effects 实际引用目标的覆盖规模评估，未引用候选不计入。");
		}
		else
		{
			sb.AppendLine("费用模式：MCM 固定费用 " + Math.Max(0, options?.GoldCost ?? 0).ToString(CultureInfo.InvariantCulture) + " 第纳尔。");
		}
		sb.AppendLine("作用域：地方；发布地城镇/城堡=" + fiefs.Count.ToString(CultureInfo.InvariantCulture) + "；附属范围由 C# 自动随父级结算，不提供独立目标。");
		sb.AppendLine(BuildLocalPolicyMentionSummary(mentionTargets));
		sb.AppendLine("【已选封地及实时核心数值】");
		foreach (Settlement fief in fiefs)
		{
			Town town = fief.Town;
			sb.AppendLine("- 根封地 ID=" + (fief.StringId ?? "") + "；名称=" + (fief.Name?.ToString() ?? fief.StringId ?? "未知") + "；类型=" + GetLocalPolicyFiefTypeText(fief)
				+ "；繁荣=" + FormatNumber(town?.Prosperity ?? 0f) + "；粮食=" + FormatNumber(town?.FoodStocks ?? 0f)
				+ "；忠诚=" + FormatNumber(town?.Loyalty ?? 0f) + "；治安=" + FormatNumber(town?.Security ?? 0f) + "；民兵=" + FormatNumber(fief.Militia));
		}
		return sb.ToString().Trim();
	}

	private static string BuildPolicyRuleContext()
	{
		return "ruleSource=custom_policy_only\n"
			+ "- 本链路只使用自定义政策独立链路，不注入 RuleBehaviorPrompts、会面对话、原版对话、写信、喊话或其他动作标签规则。\n"
			+ "- 全国政策与地方政策共用 MCM 可编辑的完整基础评判提示词；地方政策只动态追加所选封地、地方作用域和稳定度为 0 等强制规则。\n"
			+ "- 本阶段只做通用政策评议：描述直接社会、政治、财政与军事影响，保留正文明确数值和时间表达，不决定后续执行能力或具体目标。";
	}

	private string BuildPolicyWorldContextCompact(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("当前日期：" + FormatCurrentCampaignDate());
		sb.AppendLine("玩家：" + (Hero.MainHero?.Name?.ToString() ?? "玩家"));
		sb.AppendLine("玩家资源：第纳尔=" + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture) + "；影响力=" + FormatNumber(Math.Max(0f, Clan.PlayerClan?.Influence ?? 0f)));
		sb.AppendLine("玩家王国：" + GetKingdomName(playerKingdom) + " | ID=" + (playerKingdom?.StringId ?? ""));
		sb.AppendLine("消耗模式：" + BuildPolicyCostModeContextLine(options));
		sb.AppendLine("发布条件：玩家必须为国王；无冷却限制，可连续发布。");
		sb.AppendLine("主评判提示词来源：" + (options.EvaluatorPromptIsDefault ? "MCM 自定义政策评判器提示词（当前为默认文本）" : "玩家在 MCM 中自定义的评判器提示词"));
		sb.AppendLine("本链路不是原版 PolicyObject 动态注册，而是 AnimusForge 自定义政策；成功发布后创建每日持续效果，由游戏每日 Tick 逐日结算。");
		sb.AppendLine();
		sb.AppendLine("【玩家王国精简概况】");
		AppendKingdomSummary(sb, playerKingdom, includeAnomalies: false);
		sb.AppendLine();
		sb.AppendLine("【其他王国索引】");
		AppendOtherKingdomIndex(sb, playerKingdom);
		return sb.ToString().Trim();
	}

	private string BuildPolicyWorldContextFull(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		options = options ?? BuildPolicyRuntimeOptions();
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("当前日期：" + FormatCurrentCampaignDate());
		sb.AppendLine("玩家：" + (Hero.MainHero?.Name?.ToString() ?? "玩家"));
		sb.AppendLine("玩家资源：第纳尔=" + Math.Max(0, Hero.MainHero?.Gold ?? 0).ToString(CultureInfo.InvariantCulture) + "；影响力=" + FormatNumber(Math.Max(0f, Clan.PlayerClan?.Influence ?? 0f)));
		sb.AppendLine("玩家王国：" + GetKingdomName(playerKingdom) + " | ID=" + (playerKingdom?.StringId ?? ""));
		sb.AppendLine("消耗模式：" + BuildPolicyCostModeContextLine(options));
		sb.AppendLine("发布条件：玩家必须为国王；无冷却；成功后创建每日持续效果，从下一次 DailyTick 起逐日结算。");
		sb.AppendLine("主评判提示词来源：" + (options.EvaluatorPromptIsDefault ? "MCM 默认卡拉迪亚政策评判器" : "玩家在 MCM 中自定义的评判器提示词"));
		sb.AppendLine();
		sb.AppendLine("【玩家王国完整概况】");
		AppendKingdomSummary(sb, playerKingdom, includeAnomalies: true);
		sb.AppendLine();
		sb.AppendLine("【其他王国索引】");
		AppendOtherKingdomIndex(sb, playerKingdom);
		return sb.ToString().Trim();
	}

	private static string BuildPolicyExtensionContext(Kingdom playerKingdom, PolicyRuntimeOptions options)
	{
		return "（扩展上下文暂未接入。本入口预留给之后的 NPC 记忆、玩家履历、玩家近期行动；当前版本不得从会面对话、原版对话、写信或喊话链路自动注入其他规则。）";
	}

	private static string BuildPolicyCostModeContextLine(PolicyRuntimeOptions options)
	{
		if (options?.UseAiEvaluatedCost == true)
		{
			return "AI 判断自定义政策消耗已开启。主处理必须同时评估 startupGoldCost 与 dailyMaintenanceGoldCost；启动费仍保留 " + AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + " 第纳尔底线并按实际投入折算效果，每日维护费进入原版家族财政结算且不参与启动比例缩放。";
		}
		return "AI 判断自定义政策消耗已关闭。代码完全按 MCM 固定第纳尔消耗（" + FormatCostText(options) + "）扣费，效果不按资源比例折算；主处理不需要评估执行成本。";
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
					relation = playerKingdom != null && kingdom.IsAtWarWith(playerKingdom) ? "战争" : "非战争";
				}
				catch
				{
					relation = "未知";
				}
				string cultureText = "未知";
				try
				{
					cultureText = kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? "未知";
				}
				catch
				{
				}
				sb.AppendLine("- " + GetKingdomName(kingdom) + " | ID=" + kingdom.StringId + " | 文化=" + cultureText + " | 领袖=" + (kingdom.Leader?.Name?.ToString() ?? "未知") + " | AF稳定度=" + MyBehavior.GetKingdomStabilityValueForExternal(kingdom).ToString(CultureInfo.InvariantCulture) + "/100 | 与玩家王国关系=" + relation);
			}
		}
		catch (Exception ex)
		{
			sb.AppendLine("其他王国索引读取失败：" + ex.Message);
		}
	}

	private void AppendKingdomSummary(StringBuilder sb, Kingdom kingdom, bool includeAnomalies)
	{
		if (kingdom == null)
		{
			sb.AppendLine("（无王国）");
			return;
		}
		string cultureText = "未知";
		try
		{
			cultureText = kingdom.Culture?.Name?.ToString() ?? kingdom.Culture?.StringId ?? "未知";
		}
		catch
		{
		}
		sb.AppendLine("王国：" + GetKingdomName(kingdom) + " | ID=" + kingdom.StringId + " | 文化=" + cultureText + " | 领袖=" + (kingdom.Leader?.Name?.ToString() ?? "未知") + " | AF稳定度=" + MyBehavior.GetKingdomStabilityValueForExternal(kingdom).ToString(CultureInfo.InvariantCulture) + "/100");
		try
		{
			string policies = string.Join("、", kingdom.ActivePolicies.Where(p => p != null).Select(p => p.Name?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
			sb.AppendLine("当前原版生效政策：" + (string.IsNullOrWhiteSpace(policies) ? "无" : policies));
		}
		catch
		{
			sb.AppendLine("当前原版生效政策：读取失败");
		}
		List<Settlement> towns = GetKingdomSettlements(kingdom).Where(s => s?.Town != null).ToList();
		sb.AppendLine("一级政策领地数量：城镇/城堡 " + towns.Count.ToString(CultureInfo.InvariantCulture));
		if (towns.Count > 0)
		{
			sb.AppendLine("城镇/城堡均值：繁荣=" + FormatNumber(towns.Average(s => s.Town.Prosperity))
				+ "，粮食=" + FormatNumber(towns.Average(s => s.Town.FoodStocks))
				+ "，忠诚=" + FormatNumber(towns.Average(s => s.Town.Loyalty))
				+ "，治安=" + FormatNumber(towns.Average(s => s.Town.Security))
				+ "，民兵=" + FormatNumber(towns.Average(s => s.Militia)));
			if (includeAnomalies)
			{
				AppendTownExtremes(sb, towns);
			}
		}
		else
		{
			sb.AppendLine("城镇/城堡均值：无");
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
		sb.AppendLine("城镇/城堡关键项：繁荣最低=" + FormatTownStat(lowProsperity, lowProsperity?.Town?.Prosperity ?? 0f)
			+ "；繁荣最高=" + FormatTownStat(highProsperity, highProsperity?.Town?.Prosperity ?? 0f)
			+ "；粮食最低=" + FormatTownStat(lowFood, lowFood?.Town?.FoodStocks ?? 0f)
			+ "；忠诚最低=" + FormatTownStat(lowLoyalty, lowLoyalty?.Town?.Loyalty ?? 0f)
			+ "；治安最低=" + FormatTownStat(lowSecurity, lowSecurity?.Town?.Security ?? 0f)
			+ "；民兵最低=" + FormatTownStat(lowMilitia, lowMilitia?.Militia ?? 0f));
	}

	private static string FormatTownStat(Settlement settlement, float value)
	{
		return (settlement?.Name?.ToString() ?? settlement?.StringId ?? "未知") + "=" + FormatNumber(value);
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
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		try
		{
			string json = ExtractJsonObject(raw);
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}
			try
			{
				return DeserializeMainAssessmentResult(json, request);
			}
			catch
			{
				string repairedJson = RepairJsonBoundaryQuotes(json);
				if (string.Equals(repairedJson, json, StringComparison.Ordinal))
				{
					return null;
				}
				return DeserializeMainAssessmentResult(repairedJson, request);
			}
		}
		catch
		{
			return null;
		}
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
			"authoritarianWeight", "oligarchicWeight", "egalitarianWeight", "effectDurationMode", "durationDays"
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
			throw new JsonException("第一次通用评议字段缺失、重复或包含未知字段。");
		}
		foreach (string textField in new[]
		{
			"publicFeedback", "impactSummary", "numericIntent", "policyContentDigest", "feedbackDigest", "effectDurationMode"
		}.Concat(IsVassalPolicyRequest(request) ? new[] { "vassalIndependenceReason" } : Array.Empty<string>()))
		{
			if (parsed[textField]?.Type != JTokenType.String || string.IsNullOrWhiteSpace(parsed.Value<string>(textField)))
			{
				throw new JsonException("第一次通用评议文本字段无效：" + textField);
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
				throw new JsonException("第一次通用评议数值字段无效：" + numberField);
			}
		}
		if (parsed["durationDays"]?.Type != JTokenType.Integer)
		{
			throw new JsonException("第一次通用评议 durationDays 必须是整数。");
		}
		string durationMode = parsed.Value<string>("effectDurationMode")?.Trim();
		int durationDays = parsed.Value<int>("durationDays");
		if ((!string.Equals(durationMode, "permanent", StringComparison.Ordinal)
				&& !string.Equals(durationMode, "finite", StringComparison.Ordinal))
			|| (string.Equals(durationMode, "permanent", StringComparison.Ordinal) && durationDays != 0)
			|| (string.Equals(durationMode, "finite", StringComparison.Ordinal) && durationDays <= 0))
		{
			throw new JsonException("第一次通用评议期限模式与 durationDays 不一致。");
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
			throw new JsonException("第一次通用评议政治权重超出范围或全部为 0。");
		}
		foreach (string costField in expected.Where(field => field.EndsWith("GoldCost", StringComparison.Ordinal)))
		{
			if (parsed.Value<double>(costField) < 0d)
			{
				throw new JsonException("第一次通用评议费用不得为负数：" + costField);
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
		return parsed.ToObject<PolicyMainAssessmentResult>();
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
				? "所选封地及其附属村庄的民众已经听闻这项地方政策，但反馈尚不明朗。"
				: "各地民众已经听闻这项新政策，但反馈尚不明朗。";
		}
		assessment.ImpactSummary = CleanPolicyDisplayText(assessment.ImpactSummary ?? "");
		if (string.IsNullOrWhiteSpace(assessment.ImpactSummary))
		{
			assessment.ImpactSummary = ExtractMainImpactSummaryForPopup(fallbackMainRaw);
		}
		if (string.IsNullOrWhiteSpace(assessment.ImpactSummary))
		{
			assessment.ImpactSummary = "政策影响需按评判器与世界状态判断。";
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
				assessment.VassalIndependenceReason = "政策对附庸国独立倾向的影响被评为中性。";
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
				error = "稀疏效果缺少可计时的发布地生命周期";
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
				error = "所有地方效果必须使用同一个正 durationDays";
				return false;
			}
			Dictionary<string, PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles)
				.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
			foreach (PolicyEffectDto effect in sparseEffects)
			{
				if (!handles.TryGetValue((effect.TargetHandle ?? "").Trim(), out PolicyTargetHandleSaveData target)
					|| !IsPolicyTargetHandleAllowedForRequest(request, target))
				{
					error = "返回了玩家地方作用域之外的目标句柄";
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
			error = hasMentionTargets ? "effects 必须包含 source 与 mentioned 各一组效果" : "effects 必须且只能包含一组 source 效果";
			return false;
		}
		List<PolicyEffectDto> sourceEffects = assessment.Effects.Where(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase)).ToList();
		List<PolicyEffectDto> mentionedEffects = assessment.Effects.Where(x => string.Equals(NormalizeLocalPolicyTargetScope(x.TargetScope), LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase)).ToList();
		if (sourceEffects.Count != 1 || (hasMentionTargets && mentionedEffects.Count != 1) || (!hasMentionTargets && mentionedEffects.Count != 0))
		{
			error = hasMentionTargets ? "targetScope 必须分别为 source 和 mentioned" : "targetScope 必须为 source";
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
				error = "返回了玩家地方作用域之外的目标";
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
			error = "持续天数必须为正整数";
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
			error = "地方政策必须至少保留一个 S 发布地效果（可为全零计时效果）";
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
				error = "地方政策 S 发布地模块效果缺少 canonical 目标";
				return false;
			}
			List<string> parentSettlementIds = NormalizeIdList(targetSet.ParentSettlementIds);
			if (parentSettlementIds.Count == 0)
			{
				error = "地方政策 S 发布地模块效果缺少 canonical 父级发布地目标";
				return false;
			}
			identities.Add("P=" + string.Join(",", parentSettlementIds));
		}
		if (identities.Distinct(StringComparer.Ordinal).Count() != 1)
		{
			error = "地方政策 S 发布地模块效果必须作用于同一个 canonical 发布地目标";
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
		return "各地民众已经听闻这项新政策，但反馈尚不明朗。";
	}

	private static string BuildAiEvaluatedCostPaymentText(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return "";
		}
		return BuildPlayerPolicyGoldCostSummary(request)
			+ "（已为你保留 " + AiPolicyGoldReserve.ToString(CultureInfo.InvariantCulture) + " 第纳尔）。"
			+ "全部政策效果按 " + FormatPercent(request.GoldEffectScale) + " 生效。";
	}

	private static string BuildPlayerPolicyGoldCostSummary(PolicyDraftRequest request)
	{
		int startupRequired = request?.UseAiEvaluatedCost == true
			? Math.Max(0, request.RequiredGoldCost)
			: Math.Max(0, request?.GoldCost ?? 0);
		return "启动所需：" + FormatGoldCostText(startupRequired)
			+ "；启动实付：" + FormatGoldCostText(Math.Max(0, request?.GoldCost ?? 0))
			+ "；每日维护：" + FormatGoldCostText(Math.Max(0, request?.DailyMaintenanceGoldCost ?? 0))
			+ "；累计维护：" + FormatGoldCostText(Math.Max(0, request?.TotalMaintenancePaidGold ?? 0));
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
				PolicyName = LimitDisplayChars(request.PolicyName ?? "未命名政策", MaxPolicyNameChars),
				PolicyContentSummary = LimitDisplayChars(request.PolicyContent ?? "", MaxPolicyRecordContentChars),
				PublicFeedbackSummary = LimitDisplayChars(CleanPolicyDisplayText(feedback ?? ""), MaxPolicyRecordFeedbackChars),
				ImpactSummary = LimitDisplayChars(CleanPolicyDisplayText(generationResult?.Postprocess?.ImpactSummary ?? BuildPolicyEffectSummary(application)), MaxPolicyRecordImpactChars),
				ImpactEffectsSummary = LimitDisplayChars(BuildPolicyEffectSummary(application), MaxPolicyRecordImpactChars),
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
			record.ImpactEffectsSummary = LimitDisplayChars(BuildPolicyRecordEffectSummary(record), MaxPolicyRecordImpactChars);
			_policyRecordHistory[record.RecordId] = JsonConvert.SerializeObject(record);
			TrimPolicyRecordHistory();
			if (!RegisterUnifiedPlayerPolicy(request, generationResult, feedback, application, record.RecordId, record.CreatedUtcTicks))
			{
				throw new InvalidOperationException("NPC 统一政策记录写入失败");
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
				impactSummary = LimitDisplayChars(BuildPolicyEffectSummary(application), 160);
			}
			string kingdomName = string.IsNullOrWhiteSpace(request.PlayerKingdomName) ? "玩家王国" : request.PlayerKingdomName.Trim();
			string policyName = LimitDisplayChars(request.PolicyName ?? "未命名政策", MaxPolicyNameChars);
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
		string text = "以" + (string.IsNullOrWhiteSpace(kingdomName) ? "玩家王国" : kingdomName.Trim()) + "国王身份发布《" + (policyName ?? "未命名政策").Trim() + "》";
		if (!string.IsNullOrWhiteSpace(policySummary))
		{
			text += "：" + policySummary.Trim();
		}
		if (!string.IsNullOrWhiteSpace(impactSummary))
		{
			text += "；影响：" + impactSummary.Trim();
		}
		return LimitDisplayChars(CleanPolicyDisplayText(text.Trim().TrimEnd('。') + "。"), MaxPolicyRecentActionChars);
	}

	private static string BuildPolicyMajorHistoryText(string kingdomName, string policyName, string policySummary, string impactSummary, PolicyApplicationResult application)
	{
		string effectSummary = string.IsNullOrWhiteSpace(impactSummary) ? BuildPolicyEffectSummary(application) : impactSummary.Trim();
		string text = "发布自定义政策《" + (policyName ?? "未命名政策").Trim() + "》";
		if (!string.IsNullOrWhiteSpace(kingdomName))
		{
			text += "，适用于" + kingdomName.Trim();
		}
		if (!string.IsNullOrWhiteSpace(policySummary))
		{
			text += "；内容：" + policySummary.Trim();
		}
		if (!string.IsNullOrWhiteSpace(effectSummary))
		{
			text += "；评判影响：" + LimitDisplayChars(CompactPolicyContextText(effectSummary), 80);
		}
		return LimitDisplayChars(CleanPolicyDisplayText(text.Trim().TrimEnd('。') + "。"), MaxPolicyMajorHistoryChars);
	}

	private static string BuildPolicyWeeklyMaterialEffectSummary(AppliedKingdomEffect effect)
	{
		if (effect == null)
		{
			return "";
		}
		List<string> values = PolicyEffectSaveCodec.DescribePlayerVisibleInstances(effect.ModuleEffects);
		string text = (values.Count <= 0 ? "无持续数值变化" : string.Join("，", values))
			+ (effect.IsPermanentEffect
				? "；永久生效"
				: "；效果持续 " + Math.Max(0, effect.DurationDays).ToString(CultureInfo.InvariantCulture) + " 天");
		if (!string.IsNullOrWhiteSpace(effect.Reason))
		{
			text += "；原因：" + LimitDisplayChars(CompactPolicyContextText(effect.Reason), 30);
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
		out string error)
	{
		entries = new List<NpcPolicyHistoryEntry>();
		error = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				error = "玩家政策行为尚未初始化";
				return false;
			}
			entries = behavior.CapturePlayerPolicyHistoryEntriesForNpc();
			return true;
		}
		catch (Exception ex)
		{
			entries = new List<NpcPolicyHistoryEntry>();
			error = ex.Message ?? "玩家政策历史快照失败";
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
		request.EnemyKingdoms = PolicyHistoryRetrievalService.CaptureEnemyKingdoms(anchorKingdom);
		PolicyDebugLog("policy-history-snapshot", BuildPolicyRequestLogPrefix(request)
			+ " entries=" + request.PolicyHistoryEntries.Count.ToString(CultureInfo.InvariantCulture)
			+ " enemies=" + request.EnemyKingdoms.Count.ToString(CultureInfo.InvariantCulture));
	}

	private static void RetrieveUnifiedPolicyHistoryForRequest(
		PolicyDraftRequest request,
		float[] queryVector,
		string queryText,
		long runtimeGeneration)
	{
		if (request == null)
		{
			return;
		}
		request.PolicyHistoryRetrieval = PolicyHistoryRetrievalService.Retrieve(
			queryVector,
			queryText,
			request.PolicyHistoryEntries,
			request.EnemyKingdoms,
			request.PlayerKingdomId,
			runtimeGeneration);
		PolicyHistoryRetrievalResult retrieval = request.PolicyHistoryRetrieval;
		PolicyDebugLog("policy-history-retrieved", BuildPolicyRequestLogPrefix(request)
			+ " enemyCount=" + (retrieval?.EnemyCount ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " enemyWithPolicy=" + (retrieval?.EnemyWithPolicyCount ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " current=" + (retrieval?.RelatedCurrentPolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " historical=" + (retrieval?.RelatedHistoricalPolicies?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " cacheHits=" + (retrieval?.DocumentVectorCacheHits ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " cacheMisses=" + (retrieval?.DocumentVectorCacheMisses ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " promptChars=" + (retrieval?.CombinedPrompt?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
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
		request.PolicyHistoryRetrieval = PolicyHistoryRetrievalService.Retrieve(
			embeddingSession,
			queryText,
			request.PolicyHistoryEntries,
			request.EnemyKingdoms,
			request.PlayerKingdomId,
			runtimeGeneration);
	}

	private List<NpcPolicyHistoryEntry> CapturePlayerPolicyHistoryEntriesForNpc()
	{
		Dictionary<string, PolicyRecordSaveData> kingdomHistory = LoadPolicyRecordHistory()
			.Where(record => record != null && !string.IsNullOrWhiteSpace(record.RecordId))
			.GroupBy(record => record.RecordId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		List<NpcPolicyHistoryEntry> result = new List<NpcPolicyHistoryEntry>();
		foreach (DynamicPolicySaveData policy in LoadDynamicPolicies())
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
				ScopeKind = PolicyScopeKingdom,
				OwnerKingdomId = FirstNonEmpty(policy.OwnerKingdomId, history?.PlayerKingdomId),
				OwnerKingdomName = history?.PlayerKingdomName ?? string.Empty,
				OwnerClanId = FirstNonEmpty(policy.ProposerClanId, Clan.PlayerClan?.StringId),
				IssuerKingdomId = FirstNonEmpty(policy.IssuerKingdomId, policy.OwnerKingdomId, history?.PlayerKingdomId),
				IssuerKingdomName = history?.PlayerKingdomName ?? string.Empty,
				PolicyName = FirstNonEmpty(policy.PolicyName, history?.PolicyName),
				PolicyContent = FirstNonEmpty(policy.PolicyContent, history?.PolicyContentSummary),
				ImpactSummary = FirstNonEmpty(history?.ImpactEffectsSummary, history?.ImpactSummary, policy.SecondaryEffects),
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
					PolicyEffectSaveCodec.DescribePlayerVisibleInstances(effect?.ModuleEffects),
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

		foreach (LocalPolicyRecordSaveData policy in LoadLocalPolicyRecords())
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
				ScopeKind = isVassal ? PolicyScopeVassal : PolicyScopeLocal,
				OwnerKingdomId = FirstNonEmpty(policy.TargetKingdomId, policy.IssuerKingdomId),
				OwnerKingdomName = FirstNonEmpty(policy.TargetKingdomName, policy.IssuerKingdomName),
				OwnerClanId = Clan.PlayerClan?.StringId ?? string.Empty,
				IssuerKingdomId = policy.IssuerKingdomId ?? string.Empty,
				IssuerKingdomName = policy.IssuerKingdomName ?? string.Empty,
				PolicyName = policy.PolicyName ?? string.Empty,
				PolicyContent = policy.PolicyContent ?? string.Empty,
				ImpactSummary = FirstNonEmpty(policy.ImpactSummary, policy.EffectReason),
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
					PolicyEffectSaveCodec.DescribePlayerVisibleInstances(effect?.ModuleEffects),
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
		return PolicyHistoryRetrievalService.IsUsableEntry(entry);
	}

}
