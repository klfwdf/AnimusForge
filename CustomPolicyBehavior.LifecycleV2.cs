using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private const string SaveKeyPolicyLifecycleStates = "_afPolicyLifecycleStates_v1";

	private const string PolicyLifecycleKindConditional = "conditional";
	private const string PolicyLifecyclePhaseGrace = "grace";
	private const string PolicyLifecyclePhaseMaintained = "maintained";
	private const string PolicyLifecyclePhaseBreached = "breached";
	private const string PolicyLifecyclePhaseCompleted = "completed";

	private const string PolicyRenewalModeNone = "none";
	private const string PolicyRecoveryModeAutomatic = "automatic";

	private const string PolicySubjectRulerClan = "rulerClan";
	private const string PolicySubjectVassalClans = "vassalClans";
	private const string PolicySubjectAllMemberClans = "allMemberClans";
	private const string PolicySubjectRulerFiefs = "rulerFiefs";
	private const string PolicySubjectVassalFiefs = "vassalFiefs";
	private const string PolicySubjectAllKingdomFiefs = "allKingdomFiefs";

	private const string PolicyConditionWarDeclaredAfterEnactment = "warDeclaredAfterEnactment";
	private const string PolicyConditionIsAtWarWithAny = "isAtWarWithAny";
	private const string PolicyConditionIsAtWarWithTarget = "isAtWarWithTarget";
	private const string PolicyConditionIsAtWarWithRecordedEnemy = "isAtWarWithRecordedEnemy";
	private const string PolicyConditionActiveWarCountAtLeast = "activeWarCountAtLeast";
	private const string PolicyConditionRulingClanTierAtLeast = "rulingClanTierAtLeast";
	private const string PolicyConditionSettlementCountAtLeast = "settlementCountAtLeast";
	private const string PolicyConditionKingdomStabilityAtLeast = "kingdomStabilityAtLeast";

	private readonly Dictionary<string, string> _policyLifecycleStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private int _lastPolicyLifecycleAdvancedDay = -1;

	private sealed class PolicySubjectSaveData
	{
		[JsonProperty("kind")]
		public string Kind { get; set; }

		[JsonProperty("minClanTier", NullValueHandling = NullValueHandling.Ignore)]
		public int? MinClanTier { get; set; }

		[JsonProperty("maxClanTier", NullValueHandling = NullValueHandling.Ignore)]
		public int? MaxClanTier { get; set; }
	}

	private sealed class PolicyLifecycleDefinitionSaveData
	{
		[JsonProperty("kind")]
		public string Kind { get; set; }

		[JsonProperty("initialPhase")]
		public string InitialPhase { get; set; }

		[JsonProperty("graceDays")]
		public int GraceDays { get; set; }

		[JsonProperty("fulfillmentCondition", NullValueHandling = NullValueHandling.Ignore)]
		public PolicyConditionSaveData FulfillmentCondition { get; set; }

		[JsonProperty("maintenanceCondition", NullValueHandling = NullValueHandling.Ignore)]
		public PolicyConditionSaveData MaintenanceCondition { get; set; }

		[JsonProperty("failureToleranceDays")]
		public int FailureToleranceDays { get; set; }

		[JsonProperty("recoveryMode")]
		public string RecoveryMode { get; set; }

		[JsonProperty("penaltyDurationDays")]
		public int PenaltyDurationDays { get; set; }

		[JsonProperty("breachOnAbolition")]
		public bool BreachOnAbolition { get; set; }

		[JsonProperty("renewalMode")]
		public string RenewalMode { get; set; }
	}

	private sealed class PolicyConditionSaveData
	{
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
		public string Target { get; set; }

		[JsonProperty("targetKingdomId", NullValueHandling = NullValueHandling.Ignore)]
		public string TargetKingdomId { get; set; }

		[JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
		public float? Value { get; set; }
	}

	private sealed class PolicyPhaseSaveData
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("effects")]
		public List<PolicyEffectDto> Effects { get; set; } = new List<PolicyEffectDto>();
	}

	private sealed class PolicyLifecycleStateSaveData
	{
		public int Version { get; set; } = 1;
		public string RecordId { get; set; }
		public string OwnerKingdomId { get; set; }
		public string PolicyName { get; set; }
		public PolicyLifecycleDefinitionSaveData Definition { get; set; }
		public string CurrentPhase { get; set; }
		public int EnactedDay { get; set; }
		public int LastAdvancedDay { get; set; }
		public int RemainingDays { get; set; }
		public int ConditionDeadlineDay { get; set; }
		public bool ConditionEverSatisfied { get; set; }
		public int SatisfiedDay { get; set; }
		public string RecordedEnemyKingdomId { get; set; }
		public int ConsecutiveFailureDays { get; set; }
		public int BreachDay { get; set; }
		public int PenaltyRemainingDays { get; set; }
		public int BenefitRemainingDaysBeforeBreach { get; set; }
		public bool ForcePenaltyToCompletion { get; set; }
		public List<string> AppliedOneShotPhaseIds { get; set; } = new List<string>();
		public bool Completed { get; set; }
		public string CompletionReason { get; set; }
	}

	private static PolicySubjectSaveData NormalizePolicySubject(PolicySubjectSaveData subject)
	{
		if (subject == null || string.IsNullOrWhiteSpace(subject.Kind))
		{
			return null;
		}
		string kind = subject.Kind.Trim();
		if (!IsSupportedPolicySubjectKind(kind))
		{
			return null;
		}
		int? minTier = subject.MinClanTier.HasValue ? Math.Max(0, Math.Min(6, subject.MinClanTier.Value)) : null;
		int? maxTier = subject.MaxClanTier.HasValue ? Math.Max(0, Math.Min(6, subject.MaxClanTier.Value)) : null;
		if (minTier.HasValue && maxTier.HasValue && minTier.Value > maxTier.Value)
		{
			int swap = minTier.Value;
			minTier = maxTier;
			maxTier = swap;
		}
		return new PolicySubjectSaveData
		{
			Kind = kind,
			MinClanTier = minTier,
			MaxClanTier = maxTier
		};
	}

	private static bool IsSupportedPolicySubjectKind(string kind)
	{
		return string.Equals(kind, PolicySubjectRulerClan, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectVassalClans, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectAllMemberClans, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectRulerFiefs, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectVassalFiefs, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectAllKingdomFiefs, StringComparison.Ordinal);
	}

	private static bool IsClanPolicySubject(string kind)
	{
		return string.Equals(kind, PolicySubjectRulerClan, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectVassalClans, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectAllMemberClans, StringComparison.Ordinal);
	}

	private static bool IsFiefPolicySubject(string kind)
	{
		return string.Equals(kind, PolicySubjectRulerFiefs, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectVassalFiefs, StringComparison.Ordinal)
			|| string.Equals(kind, PolicySubjectAllKingdomFiefs, StringComparison.Ordinal);
	}

	private static string BuildPolicySubjectKey(PolicySubjectSaveData subject)
	{
		PolicySubjectSaveData normalized = NormalizePolicySubject(subject);
		return normalized == null
			? ""
			: (normalized.Kind ?? "") + ":" + (normalized.MinClanTier?.ToString(CultureInfo.InvariantCulture) ?? "") + ":" + (normalized.MaxClanTier?.ToString(CultureInfo.InvariantCulture) ?? "");
	}

	private static string BuildPolicySubjectDisplayText(PolicySubjectSaveData subject)
	{
		PolicySubjectSaveData normalized = NormalizePolicySubject(subject);
		if (normalized == null)
		{
			return "";
		}
		string text = normalized.Kind switch
		{
			PolicySubjectRulerClan => "统治氏族",
			PolicySubjectVassalClans => "封臣氏族",
			PolicySubjectAllMemberClans => "全部成员氏族",
			PolicySubjectRulerFiefs => "统治氏族领地",
			PolicySubjectVassalFiefs => "封臣领地",
			PolicySubjectAllKingdomFiefs => "王国全部领地",
			_ => normalized.Kind
		};
		if (normalized.MinClanTier.HasValue || normalized.MaxClanTier.HasValue)
		{
			text += "（家族等级 " + (normalized.MinClanTier?.ToString(CultureInfo.InvariantCulture) ?? "0") + "-" + (normalized.MaxClanTier?.ToString(CultureInfo.InvariantCulture) ?? "6") + "）";
		}
		return text;
	}

	private static bool IsClanTierMatched(Clan clan, PolicySubjectSaveData subject)
	{
		if (clan == null)
		{
			return false;
		}
		PolicySubjectSaveData normalized = NormalizePolicySubject(subject);
		return normalized == null
			|| ((!normalized.MinClanTier.HasValue || clan.Tier >= normalized.MinClanTier.Value)
				&& (!normalized.MaxClanTier.HasValue || clan.Tier <= normalized.MaxClanTier.Value));
	}

	private static bool IsPolicySubjectMatchedForClan(PolicySubjectSaveData subject, Clan clan, Kingdom targetKingdom)
	{
		if (clan == null || targetKingdom == null || clan.Kingdom != targetKingdom || clan.IsUnderMercenaryService || !IsClanTierMatched(clan, subject))
		{
			return false;
		}
		string kind = NormalizePolicySubject(subject)?.Kind;
		if (string.IsNullOrWhiteSpace(kind) || string.Equals(kind, PolicySubjectAllMemberClans, StringComparison.Ordinal))
		{
			return true;
		}
		if (string.Equals(kind, PolicySubjectRulerClan, StringComparison.Ordinal))
		{
			return clan == targetKingdom.RulingClan;
		}
		if (string.Equals(kind, PolicySubjectVassalClans, StringComparison.Ordinal))
		{
			return clan != targetKingdom.RulingClan;
		}
		return false;
	}

	private static bool IsPolicySubjectMatchedForSettlement(PolicySubjectSaveData subject, Settlement settlement, Kingdom targetKingdom)
	{
		if (settlement == null || targetKingdom == null)
		{
			return false;
		}
		Clan ownerClan = settlement.OwnerClan ?? settlement.Village?.Bound?.OwnerClan;
		if (ownerClan == null || ownerClan.Kingdom != targetKingdom || !IsClanTierMatched(ownerClan, subject))
		{
			return false;
		}
		string kind = NormalizePolicySubject(subject)?.Kind;
		if (string.IsNullOrWhiteSpace(kind) || string.Equals(kind, PolicySubjectAllKingdomFiefs, StringComparison.Ordinal))
		{
			return true;
		}
		if (string.Equals(kind, PolicySubjectRulerFiefs, StringComparison.Ordinal))
		{
			return ownerClan == targetKingdom.RulingClan;
		}
		if (string.Equals(kind, PolicySubjectVassalFiefs, StringComparison.Ordinal))
		{
			return ownerClan != targetKingdom.RulingClan && !ownerClan.IsUnderMercenaryService;
		}
		return false;
	}

	private bool IsEffectInCurrentLifecyclePhase(ActivePolicyEffectSaveData effect)
	{
		if (effect == null || string.IsNullOrWhiteSpace(effect.PhaseId))
		{
			return true;
		}
		return TryLoadPolicyLifecycleState(effect.RecordId, out PolicyLifecycleStateSaveData state)
			&& !state.Completed
			&& string.Equals(state.CurrentPhase ?? "", effect.PhaseId ?? "", StringComparison.OrdinalIgnoreCase);
	}

	private bool TryLoadPolicyLifecycleState(string recordId, out PolicyLifecycleStateSaveData state)
	{
		state = null;
		string id = (recordId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyLifecycleStates.TryGetValue(id, out string raw) || string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		try
		{
			state = JsonConvert.DeserializeObject<PolicyLifecycleStateSaveData>(raw);
			if (state == null || string.IsNullOrWhiteSpace(state.RecordId))
			{
				return false;
			}
			state.AppliedOneShotPhaseIds ??= new List<string>();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void StorePolicyLifecycleState(PolicyLifecycleStateSaveData state)
	{
		if (state == null || string.IsNullOrWhiteSpace(state.RecordId))
		{
			return;
		}
		_policyLifecycleStates[state.RecordId.Trim()] = JsonConvert.SerializeObject(state);
		_activePolicyPercentEffectCacheDirty = true;
		_activePolicyEffectModelCache.Clear();
	}

	private void RegisterPolicyLifecycle(PolicyDraftRequest request, PolicyApplicationResult application, string recordId)
	{
		PolicyLifecycleDefinitionSaveData definition = NormalizePolicyLifecycleDefinition(application?.Lifecycle, request?.TargetHandles, out _);
		if (definition == null || !string.Equals(definition.Kind, PolicyLifecycleKindConditional, StringComparison.Ordinal))
		{
			return;
		}
		int enactedDay = Math.Max(0, request?.SubmittedDay ?? GetCurrentCampaignDay());
		int durationDays = application?.KingdomEffects?.Where(x => x != null).Select(x => x.DurationDays).DefaultIfEmpty(1).Max() ?? 1;
		PolicyLifecycleStateSaveData state = new PolicyLifecycleStateSaveData
		{
			RecordId = recordId ?? "",
			OwnerKingdomId = request?.PlayerKingdomId ?? "",
			PolicyName = request?.PolicyName ?? "",
			Definition = definition,
			CurrentPhase = definition.InitialPhase,
			EnactedDay = enactedDay,
			LastAdvancedDay = enactedDay,
			RemainingDays = Math.Max(1, durationDays),
			ConditionDeadlineDay = enactedDay + Math.Max(0, definition.GraceDays),
			PenaltyRemainingDays = Math.Max(1, definition.PenaltyDurationDays)
		};
		StorePolicyLifecycleState(state);
		if (string.Equals(state.CurrentPhase, PolicyLifecyclePhaseGrace, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(state.Definition?.FulfillmentCondition?.Type, PolicyConditionWarDeclaredAfterEnactment, StringComparison.Ordinal)
			&& EvaluatePolicyCondition(state, state.Definition?.FulfillmentCondition))
		{
			state.ConditionEverSatisfied = true;
			state.SatisfiedDay = enactedDay;
			TransitionPolicyLifecycle(state, PolicyLifecyclePhaseMaintained, "政策生效时履约条件已满足");
			return;
		}
		SynchronizeLifecycleEffectDurations(state);
		ApplyLifecyclePhaseOneTimeEffects(state);
		UpdatePolicyRecordLifecycleState(state);
		PolicySystemLog.Write("Lifecycle", "registered", BuildLifecycleLog(state, "registered"));
	}

	private static PolicyLifecycleDefinitionSaveData NormalizePolicyLifecycleDefinition(
		PolicyLifecycleDefinitionSaveData definition,
		IEnumerable<PolicyTargetHandleSaveData> handles,
		out string error)
	{
		error = "";
		if (definition == null || !string.Equals((definition.Kind ?? "").Trim(), PolicyLifecycleKindConditional, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		PolicyLifecycleDefinitionSaveData normalized = new PolicyLifecycleDefinitionSaveData
		{
			Kind = PolicyLifecycleKindConditional,
			InitialPhase = string.Equals(definition.InitialPhase, PolicyLifecyclePhaseMaintained, StringComparison.OrdinalIgnoreCase)
				? PolicyLifecyclePhaseMaintained
				: PolicyLifecyclePhaseGrace,
			GraceDays = Math.Max(0, definition.GraceDays),
			FailureToleranceDays = Math.Max(0, definition.FailureToleranceDays),
			RecoveryMode = string.Equals(definition.RecoveryMode, PolicyRecoveryModeAutomatic, StringComparison.OrdinalIgnoreCase)
				? PolicyRecoveryModeAutomatic
				: "none",
			PenaltyDurationDays = Math.Max(1, definition.PenaltyDurationDays),
			// Conditional benefits must not be escapable by repealing the policy before its failure phase.
			BreachOnAbolition = true,
			RenewalMode = PolicyRenewalModeNone,
			FulfillmentCondition = NormalizePolicyCondition(definition.FulfillmentCondition, handles, out error),
			MaintenanceCondition = NormalizePolicyCondition(definition.MaintenanceCondition, handles, out string maintenanceError)
		};
		if (string.IsNullOrWhiteSpace(error))
		{
			error = maintenanceError;
		}
		if (normalized.InitialPhase == PolicyLifecyclePhaseGrace && normalized.GraceDays <= 0)
		{
			error = "conditional lifecycle 的 graceDays 必须为正整数";
		}
		if (normalized.InitialPhase == PolicyLifecyclePhaseGrace && normalized.FulfillmentCondition == null)
		{
			error = "conditional lifecycle 缺少 fulfillmentCondition";
		}
		return string.IsNullOrWhiteSpace(error) ? normalized : null;
	}

	private static PolicyConditionSaveData NormalizePolicyCondition(
		PolicyConditionSaveData condition,
		IEnumerable<PolicyTargetHandleSaveData> handles,
		out string error)
	{
		error = "";
		if (condition == null || string.IsNullOrWhiteSpace(condition.Type))
		{
			return null;
		}
		string type = condition.Type.Trim();
		if (!IsSupportedPolicyConditionType(type))
		{
			error = "不支持的政策条件：" + type;
			return null;
		}
		PolicyConditionSaveData normalized = new PolicyConditionSaveData
		{
			Type = type,
			Target = (condition.Target ?? "").Trim(),
			Value = condition.Value
		};
		if (normalized.Value.HasValue && (float.IsNaN(normalized.Value.Value) || float.IsInfinity(normalized.Value.Value)))
		{
			error = "政策条件 value 必须是有限数字";
			return null;
		}
		if (!string.IsNullOrWhiteSpace(normalized.Target) && !string.Equals(normalized.Target, "ANY_FOREIGN", StringComparison.OrdinalIgnoreCase))
		{
			PolicyTargetHandleSaveData handle = NormalizePolicyTargetHandles(handles)
				.FirstOrDefault(x => string.Equals(x.Key, normalized.Target, StringComparison.OrdinalIgnoreCase));
			if (handle == null || !string.Equals(handle.Kind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase))
			{
				error = "条件引用了非法王国句柄：" + normalized.Target;
				return null;
			}
			normalized.TargetKingdomId = handle.KingdomId;
		}
		if (string.Equals(type, PolicyConditionIsAtWarWithTarget, StringComparison.Ordinal)
			&& string.IsNullOrWhiteSpace(normalized.TargetKingdomId))
		{
			error = "isAtWarWithTarget 必须引用合法的 K* 王国句柄";
			return null;
		}
		return normalized;
	}

	private static bool IsSupportedPolicyConditionType(string type)
	{
		return string.Equals(type, PolicyConditionWarDeclaredAfterEnactment, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionIsAtWarWithAny, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionIsAtWarWithTarget, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionIsAtWarWithRecordedEnemy, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionActiveWarCountAtLeast, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionRulingClanTierAtLeast, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionSettlementCountAtLeast, StringComparison.Ordinal)
			|| string.Equals(type, PolicyConditionKingdomStabilityAtLeast, StringComparison.Ordinal);
	}

	private static bool TryCompileConditionalPolicy(
		PolicyDraftRequest request,
		PolicyMainAssessmentResult assessment,
		out List<PolicyEffectDto> compiledEffects,
		out string error)
	{
		compiledEffects = new List<PolicyEffectDto>();
		error = "";
		if (assessment?.Lifecycle == null || !string.Equals(assessment.Lifecycle.Kind, PolicyLifecycleKindConditional, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (IsLocalPolicyRequest(request))
		{
			error = "地方政策暂不支持 conditional lifecycle";
			return false;
		}
		if ((assessment.Effects ?? new List<PolicyEffectDto>()).Any(x => x != null))
		{
			error = "conditional lifecycle 的顶层 effects 必须为空，效果只能写入 phase.effects";
			return false;
		}
		PolicyLifecycleDefinitionSaveData normalized = NormalizePolicyLifecycleDefinition(assessment.Lifecycle, request?.TargetHandles, out error);
		if (normalized == null)
		{
			return false;
		}
		int effectiveDurationDays = request?.ManualDurationDays > 0
			? request.ManualDurationDays
			: Math.Max(0, assessment.DurationDays ?? 0);
		if (string.Equals(normalized.InitialPhase, PolicyLifecyclePhaseGrace, StringComparison.OrdinalIgnoreCase)
			&& normalized.GraceDays >= effectiveDurationDays)
		{
			error = "conditional lifecycle 的 graceDays 必须小于 durationDays，避免政策在履约截止前直接自然结束";
			return false;
		}
		assessment.Lifecycle = normalized;
		List<PolicyPhaseSaveData> phases = (assessment.Phases ?? new List<PolicyPhaseSaveData>()).Where(x => x != null).ToList();
		HashSet<string> phaseIds = new HashSet<string>(phases.Select(x => (x.Id ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
		if (phases.Count != 3
			|| phaseIds.Count != 3
			|| !phaseIds.Contains(PolicyLifecyclePhaseGrace)
			|| !phaseIds.Contains(PolicyLifecyclePhaseMaintained)
			|| !phaseIds.Contains(PolicyLifecyclePhaseBreached))
		{
			error = "conditional lifecycle 必须包含 grace、maintained、breached 三个阶段";
			return false;
		}
		foreach (PolicyPhaseSaveData phase in phases)
		{
			string phaseId = (phase.Id ?? "").Trim().ToLowerInvariant();
			if (!TryCompileSparsePolicyEffects(request, assessment.DurationDays, phase.Effects, out List<PolicyEffectDto> phaseEffects, out error))
			{
				error = "阶段 " + phaseId + "：" + error;
				return false;
			}
			foreach (PolicyEffectDto effect in phaseEffects)
			{
				effect.PhaseId = phaseId;
			}
			phase.Id = phaseId;
			phase.Effects = phaseEffects;
			compiledEffects.AddRange(phaseEffects);
		}
		assessment.Phases = phases;
		return true;
	}

	private void AdvancePolicyLifecycles(int currentDay)
	{
		if (_lastPolicyLifecycleAdvancedDay == currentDay)
		{
			return;
		}
		_lastPolicyLifecycleAdvancedDay = currentDay;
		foreach (string recordId in _policyLifecycleStates.Keys.ToList())
		{
			if (!TryLoadPolicyLifecycleState(recordId, out PolicyLifecycleStateSaveData state) || state.Completed)
			{
				continue;
			}
			int elapsedDays = Math.Max(0, currentDay - Math.Max(state.EnactedDay, state.LastAdvancedDay));
			if (elapsedDays <= 0)
			{
				continue;
			}
			state.LastAdvancedDay = currentDay;
			if (string.Equals(state.CurrentPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase))
			{
				if (!state.ForcePenaltyToCompletion
					&& string.Equals(state.Definition?.RecoveryMode, PolicyRecoveryModeAutomatic, StringComparison.OrdinalIgnoreCase)
					&& state.Definition?.MaintenanceCondition != null
					&& EvaluatePolicyCondition(state, state.Definition.MaintenanceCondition))
				{
					state.RemainingDays = Math.Max(1, state.BenefitRemainingDaysBeforeBreach);
					TransitionPolicyLifecycle(state, PolicyLifecyclePhaseMaintained, "维持条件恢复，自动结束违约阶段");
					continue;
				}
				state.PenaltyRemainingDays = Math.Max(0, state.PenaltyRemainingDays - elapsedDays);
				state.RemainingDays = state.PenaltyRemainingDays;
				if (state.PenaltyRemainingDays <= 0)
				{
					CompletePolicyLifecycle(state, "违约后果持续时间结束");
					continue;
				}
			}
			else
			{
				state.RemainingDays = Math.Max(0, state.RemainingDays - elapsedDays);
				if (string.Equals(state.CurrentPhase, PolicyLifecyclePhaseGrace, StringComparison.OrdinalIgnoreCase))
				{
					if (EvaluatePolicyCondition(state, state.Definition?.FulfillmentCondition))
					{
						state.ConditionEverSatisfied = true;
						state.SatisfiedDay = currentDay;
						TransitionPolicyLifecycle(state, PolicyLifecyclePhaseMaintained, "履约条件已满足");
					}
					else if (currentDay > state.ConditionDeadlineDay)
					{
						TransitionPolicyLifecycle(state, PolicyLifecyclePhaseBreached, "未在期限内满足政策条件");
					}
				}
				else if (string.Equals(state.CurrentPhase, PolicyLifecyclePhaseMaintained, StringComparison.OrdinalIgnoreCase)
					&& state.Definition?.MaintenanceCondition != null)
				{
					if (EvaluatePolicyCondition(state, state.Definition.MaintenanceCondition))
					{
						state.ConsecutiveFailureDays = 0;
					}
					else
					{
						state.ConsecutiveFailureDays += elapsedDays;
						if (state.ConsecutiveFailureDays > Math.Max(0, state.Definition.FailureToleranceDays))
						{
							TransitionPolicyLifecycle(state, PolicyLifecyclePhaseBreached, "政策维持条件失效");
						}
					}
				}
				if (!string.Equals(state.CurrentPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase)
					&& state.RemainingDays <= 0)
				{
					CompletePolicyLifecycle(state, "政策期限结束");
					continue;
				}
			}
			StorePolicyLifecycleState(state);
			SynchronizeLifecycleEffectDurations(state);
			UpdatePolicyRecordLifecycleState(state);
		}
	}

	private bool EvaluatePolicyCondition(PolicyLifecycleStateSaveData state, PolicyConditionSaveData condition)
	{
		if (condition == null)
		{
			return true;
		}
		Kingdom owner = ResolveKingdomByIdOrName(state?.OwnerKingdomId, "");
		if (owner == null || owner.IsEliminated)
		{
			return false;
		}
		string type = condition.Type ?? "";
		if (string.Equals(type, PolicyConditionWarDeclaredAfterEnactment, StringComparison.Ordinal))
		{
			return state.ConditionEverSatisfied && IsRecordedEnemyAllowed(condition, state.RecordedEnemyKingdomId);
		}
		if (string.Equals(type, PolicyConditionIsAtWarWithAny, StringComparison.Ordinal))
		{
			return GetActiveEnemyKingdoms(owner).Count > 0;
		}
		if (string.Equals(type, PolicyConditionIsAtWarWithTarget, StringComparison.Ordinal))
		{
			Kingdom target = ResolveKingdomByIdOrName(condition.TargetKingdomId, "");
			return target != null && owner.IsAtWarWith(target);
		}
		if (string.Equals(type, PolicyConditionIsAtWarWithRecordedEnemy, StringComparison.Ordinal))
		{
			Kingdom target = ResolveKingdomByIdOrName(state.RecordedEnemyKingdomId, "");
			return target != null && owner.IsAtWarWith(target);
		}
		if (string.Equals(type, PolicyConditionActiveWarCountAtLeast, StringComparison.Ordinal))
		{
			return GetActiveEnemyKingdoms(owner).Count >= Math.Max(1, (int)Math.Round(condition.Value ?? 1f));
		}
		if (string.Equals(type, PolicyConditionRulingClanTierAtLeast, StringComparison.Ordinal))
		{
			return owner.RulingClan != null && owner.RulingClan.Tier >= Math.Max(0, (int)Math.Round(condition.Value ?? 1f));
		}
		if (string.Equals(type, PolicyConditionSettlementCountAtLeast, StringComparison.Ordinal))
		{
			return GetKingdomSettlements(owner).Count >= Math.Max(0, (int)Math.Round(condition.Value ?? 1f));
		}
		if (string.Equals(type, PolicyConditionKingdomStabilityAtLeast, StringComparison.Ordinal))
		{
			return MyBehavior.GetKingdomStabilityValueForExternal(owner) >= (int)Math.Round(condition.Value ?? 0f);
		}
		return false;
	}

	private static List<Kingdom> GetActiveEnemyKingdoms(Kingdom owner)
	{
		if (owner == null)
		{
			return new List<Kingdom>();
		}
		return Kingdom.All.Where(x => x != null && x != owner && !x.IsEliminated && owner.IsAtWarWith(x)).ToList();
	}

	private static bool IsRecordedEnemyAllowed(PolicyConditionSaveData condition, string enemyKingdomId)
	{
		if (string.IsNullOrWhiteSpace(enemyKingdomId))
		{
			return false;
		}
		return string.IsNullOrWhiteSpace(condition?.TargetKingdomId)
			|| string.Equals(condition.TargetKingdomId, enemyKingdomId, StringComparison.OrdinalIgnoreCase);
	}

	private void OnPolicyWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		Kingdom kingdom1 = ResolveKingdomFromFaction(faction1);
		Kingdom kingdom2 = ResolveKingdomFromFaction(faction2);
		if (kingdom1 == null || kingdom2 == null)
		{
			return;
		}
		int currentDay = GetCurrentCampaignDay();
		foreach (string recordId in _policyLifecycleStates.Keys.ToList())
		{
			if (!TryLoadPolicyLifecycleState(recordId, out PolicyLifecycleStateSaveData state)
				|| state.Completed
				|| !string.Equals(state.CurrentPhase, PolicyLifecyclePhaseGrace, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			// DeclareWarAction passes the initiating faction as faction1 in both supported API lines.
			Kingdom enemy = string.Equals(state.OwnerKingdomId, kingdom1.StringId, StringComparison.OrdinalIgnoreCase)
				? kingdom2
				: null;
			if (enemy == null || currentDay < state.EnactedDay)
			{
				continue;
			}
			PolicyConditionSaveData fulfillment = state.Definition?.FulfillmentCondition;
			if (!string.Equals(fulfillment?.Type, PolicyConditionWarDeclaredAfterEnactment, StringComparison.Ordinal)
				|| (!string.IsNullOrWhiteSpace(fulfillment.TargetKingdomId)
					&& !string.Equals(fulfillment.TargetKingdomId, enemy.StringId, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			state.ConditionEverSatisfied = true;
			state.SatisfiedDay = currentDay;
			state.RecordedEnemyKingdomId = enemy.StringId ?? "";
			TransitionPolicyLifecycle(state, PolicyLifecyclePhaseMaintained, "政策生效后已发动战争");
			StorePolicyLifecycleState(state);
			SynchronizeLifecycleEffectDurations(state);
			UpdatePolicyRecordLifecycleState(state);
		}
	}

	private void OnPolicyPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		Kingdom kingdom1 = ResolveKingdomFromFaction(faction1);
		Kingdom kingdom2 = ResolveKingdomFromFaction(faction2);
		if (kingdom1 == null || kingdom2 == null)
		{
			return;
		}
		PolicySystemLog.Write("Lifecycle", "peace-observed", "kingdom1=" + (kingdom1.StringId ?? "") + " kingdom2=" + (kingdom2.StringId ?? ""));
	}

	private static Kingdom ResolveKingdomFromFaction(IFaction faction)
	{
		return faction as Kingdom ?? (faction as Clan)?.Kingdom;
	}

	private void TransitionPolicyLifecycle(PolicyLifecycleStateSaveData state, string nextPhase, string reason)
	{
		if (state == null || state.Completed || string.Equals(state.CurrentPhase, nextPhase, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string oldPhase = state.CurrentPhase ?? "";
		state.CurrentPhase = nextPhase;
		state.ConsecutiveFailureDays = 0;
		if (string.Equals(nextPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase))
		{
			state.BenefitRemainingDaysBeforeBreach = Math.Max(1, state.RemainingDays);
			state.BreachDay = GetCurrentCampaignDay();
			state.PenaltyRemainingDays = Math.Max(1, state.Definition?.PenaltyDurationDays ?? 1);
			state.RemainingDays = state.PenaltyRemainingDays;
		}
		else if (string.Equals(nextPhase, PolicyLifecyclePhaseMaintained, StringComparison.OrdinalIgnoreCase))
		{
			state.PenaltyRemainingDays = 0;
		}
		StorePolicyLifecycleState(state);
		SynchronizeLifecycleEffectDurations(state);
		ApplyLifecyclePhaseOneTimeEffects(state);
		UpdatePolicyRecordLifecycleState(state);
		PolicySystemLog.Write("Lifecycle", "transition", BuildLifecycleLog(state, oldPhase + "->" + nextPhase + " reason=" + (reason ?? "")));
		ShowPlayerLifecycleMessage(state, "《" + FirstNonEmpty(state.PolicyName, "未命名政策") + "》阶段变更：" + oldPhase + " → " + nextPhase + "（" + (reason ?? "") + "）", Colors.Yellow);
	}

	private void SynchronizeLifecycleEffectDurations(PolicyLifecycleStateSaveData state)
	{
		if (state == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			ActivePolicyEffectSaveData effect;
			try
			{
				effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(item.Value ?? "");
			}
			catch
			{
				continue;
			}
			if (effect == null || !string.Equals(effect.RecordId, state.RecordId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			effect.RemainingDays = Math.Max(0, state.RemainingDays);
			effect.Ended = state.Completed;
			effect.EndReason = state.Completed ? state.CompletionReason ?? "生命周期结束" : "";
			if (!state.Completed && !string.Equals(effect.PhaseId ?? "", state.CurrentPhase ?? "", StringComparison.OrdinalIgnoreCase))
			{
				effect.PendingApplication = null;
			}
			PersistActivePolicyEffect(item.Key, effect);
		}
	}

	private void ApplyLifecyclePhaseOneTimeEffects(PolicyLifecycleStateSaveData state)
	{
		if (state == null || state.Completed || state.AppliedOneShotPhaseIds.Contains(state.CurrentPhase ?? "", StringComparer.OrdinalIgnoreCase))
		{
			return;
		}
		foreach (string raw in _activePolicyEffects.Values.ToList())
		{
			try
			{
				ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(raw ?? "");
				if (effect != null
					&& string.Equals(effect.RecordId, state.RecordId, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(effect.PhaseId, state.CurrentPhase, StringComparison.OrdinalIgnoreCase)
					&& effect.KingdomStabilityDailyDelta != 0)
				{
					ApplyKingdomStabilityOneTime(effect, effect.KingdomStabilityDailyDelta);
				}
			}
			catch
			{
			}
		}
		state.AppliedOneShotPhaseIds.Add(state.CurrentPhase ?? "");
		StorePolicyLifecycleState(state);
	}

	private void CompletePolicyLifecycle(PolicyLifecycleStateSaveData state, string reason)
	{
		if (state == null || state.Completed)
		{
			return;
		}
		state.Completed = true;
		state.CurrentPhase = PolicyLifecyclePhaseCompleted;
		state.RemainingDays = 0;
		state.PenaltyRemainingDays = 0;
		state.CompletionReason = reason ?? "生命周期结束";
		StorePolicyLifecycleState(state);
		SynchronizeLifecycleEffectDurations(state);
		foreach (KeyValuePair<string, string> item in _activePolicyEffects.ToList())
		{
			try
			{
				ActivePolicyEffectSaveData effect = JsonConvert.DeserializeObject<ActivePolicyEffectSaveData>(item.Value ?? "");
				if (effect != null && string.Equals(effect.RecordId, state.RecordId, StringComparison.OrdinalIgnoreCase))
				{
					MarkPolicyRecordEffectEnded(effect, state.CompletionReason, queueNaturalExpiry: false);
					RemoveActivePolicyEffect(item.Key);
				}
			}
			catch
			{
			}
		}
		UpdatePolicyRecordLifecycleState(state);
		if (LoadDynamicPolicies().FirstOrDefault(x => x != null && string.Equals(x.RecordId, state.RecordId, StringComparison.OrdinalIgnoreCase)) is DynamicPolicySaveData data
			&& string.Equals(data.Status, DynamicPolicyStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			ExpireDynamicPolicyWithoutRenewal(data, EnsureDynamicPolicyObject(data), state.CompletionReason);
		}
		_policyLifecycleStates.Remove(state.RecordId ?? "");
		PolicySystemLog.Write("Lifecycle", "completed", BuildLifecycleLog(state, state.CompletionReason));
		ShowPlayerLifecycleMessage(state, "《" + FirstNonEmpty(state.PolicyName, "未命名政策") + "》条件生命周期结束：" + state.CompletionReason, Colors.Green);
	}

	private static void ShowPlayerLifecycleMessage(PolicyLifecycleStateSaveData state, string text, Color color)
	{
		try
		{
			if (state != null
				&& string.Equals(state.OwnerKingdomId ?? "", Clan.PlayerClan?.Kingdom?.StringId ?? "", StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage(text ?? "", color));
			}
		}
		catch
		{
		}
	}

	private bool TryHandleConditionalPolicyAbolition(string recordId, string reason)
	{
		if (!TryLoadPolicyLifecycleState(recordId, out PolicyLifecycleStateSaveData state) || state.Completed)
		{
			return false;
		}
		if (state.Definition?.BreachOnAbolition == true
			&& !string.Equals(state.CurrentPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase))
		{
			state.ForcePenaltyToCompletion = true;
			TransitionPolicyLifecycle(state, PolicyLifecyclePhaseBreached, "政策提前废止：" + (reason ?? ""));
		}
		else if (string.Equals(state.CurrentPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase))
		{
			state.ForcePenaltyToCompletion = true;
			StorePolicyLifecycleState(state);
		}
		return string.Equals(state.CurrentPhase, PolicyLifecyclePhaseBreached, StringComparison.OrdinalIgnoreCase);
	}

	private void UpdatePolicyRecordLifecycleState(PolicyLifecycleStateSaveData state)
	{
		if (state == null || !_policyRecordHistory.TryGetValue(state.RecordId ?? "", out string raw))
		{
			return;
		}
		try
		{
			PolicyRecordSaveData record = JsonConvert.DeserializeObject<PolicyRecordSaveData>(raw ?? "");
			if (record == null)
			{
				return;
			}
			record.LifecycleKind = PolicyLifecycleKindConditional;
			record.CurrentLifecyclePhase = state.CurrentPhase ?? "";
			record.LifecycleSummary = BuildLifecycleSummary(state);
			foreach (PolicyRecordEffectSaveData effect in record.Effects ?? new List<PolicyRecordEffectSaveData>())
			{
				if (effect == null)
				{
					continue;
				}
				effect.RemainingDays = Math.Max(0, state.RemainingDays);
				if (state.Completed)
				{
					effect.IsEnded = true;
					effect.EndReason = state.CompletionReason ?? "生命周期结束";
				}
			}
			record.ImpactEffectsSummary = LimitDisplayChars(BuildPolicyRecordEffectSummary(record), MaxPolicyRecordImpactChars);
			_policyRecordHistory[state.RecordId] = JsonConvert.SerializeObject(record);
		}
		catch
		{
		}
	}

	private static string BuildLifecycleSummary(PolicyLifecycleStateSaveData state)
	{
		if (state == null)
		{
			return "";
		}
		return "阶段=" + (state.CurrentPhase ?? "")
			+ "；剩余=" + Math.Max(0, state.RemainingDays).ToString(CultureInfo.InvariantCulture) + "天"
			+ (state.ConditionDeadlineDay > 0 ? "；条件截止日=" + state.ConditionDeadlineDay.ToString(CultureInfo.InvariantCulture) : "")
			+ (!string.IsNullOrWhiteSpace(state.RecordedEnemyKingdomId) ? "；记录敌国=" + state.RecordedEnemyKingdomId : "");
	}

	private static string BuildLifecycleDefinitionDisplayText(PolicyLifecycleDefinitionSaveData definition)
	{
		if (definition == null || !string.Equals(definition.Kind, PolicyLifecycleKindConditional, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return "初始阶段=" + (definition.InitialPhase ?? PolicyLifecyclePhaseGrace)
			+ "；宽限=" + Math.Max(0, definition.GraceDays).ToString(CultureInfo.InvariantCulture) + "天"
			+ "；履约条件=" + BuildPolicyConditionDisplayText(definition.FulfillmentCondition)
			+ "；维持条件=" + BuildPolicyConditionDisplayText(definition.MaintenanceCondition)
			+ "；失效容忍=" + Math.Max(0, definition.FailureToleranceDays).ToString(CultureInfo.InvariantCulture) + "天"
			+ "；违约后果=" + Math.Max(1, definition.PenaltyDurationDays).ToString(CultureInfo.InvariantCulture) + "天；提前废止会进入违约阶段";
	}

	private static string BuildPolicyConditionDisplayText(PolicyConditionSaveData condition)
	{
		if (condition == null)
		{
			return "无";
		}
		string target = string.IsNullOrWhiteSpace(condition.Target) ? "" : "(" + condition.Target.Trim() + ")";
		string value = condition.Value.HasValue ? "≥" + FormatNumber(condition.Value.Value) : "";
		return (condition.Type ?? "未知") + target + value;
	}

	private static string BuildLifecycleLog(PolicyLifecycleStateSaveData state, string detail)
	{
		return "recordId=" + (state?.RecordId ?? "")
			+ " phase=" + (state?.CurrentPhase ?? "")
			+ " remaining=" + Math.Max(0, state?.RemainingDays ?? 0).ToString(CultureInfo.InvariantCulture)
			+ " enemy=" + (state?.RecordedEnemyKingdomId ?? "")
			+ " detail=" + (detail ?? "");
	}
}
