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

internal sealed class PolicyEffectBundleRegistration
{
	internal string ScopeKind { get; set; } = string.Empty;

	internal string EffectId { get; set; } = string.Empty;

	internal string RecordId { get; set; } = string.Empty;

	internal string ProposerClanId { get; set; } = string.Empty;

	// Frozen at policy adoption. Deferred one-time effects must never resolve the
	// actor from the kingdom's current ruler on their execution day.
	internal string ActorHeroId { get; set; } = string.Empty;

	internal string IssuerKingdomId { get; set; } = string.Empty;

	internal string PolicyName { get; set; } = string.Empty;

	internal string DateText { get; set; } = string.Empty;

	internal int SubmittedDay { get; set; }

	internal string TargetKingdomId { get; set; } = string.Empty;

	internal string TargetKingdomName { get; set; } = string.Empty;

	internal string TargetHandle { get; set; } = string.Empty;

	internal string TargetLabel { get; set; } = string.Empty;

	internal string LocalTargetScope { get; set; } = string.Empty;

	internal List<string> TargetFiefIds { get; set; } = new List<string>();

	internal List<string> TargetSettlementIds { get; set; } = new List<string>();

	internal List<string> TargetClanIds { get; set; } = new List<string>();

	internal List<string> DirectTargetSettlementIds { get; set; } = new List<string>();

	internal bool FollowCurrentRulingClan { get; set; }

	internal int DurationDays { get; set; }

	internal bool IsPermanentEffect { get; set; }

	internal int DailyMaintenanceGoldCost { get; set; }

	internal int TotalMaintenancePaidGold { get; set; }

	internal bool MaintenanceChargeEnabled { get; set; }

	internal bool MaintenanceFunded { get; set; } = true;

	internal int LastMaintenanceSettlementDay { get; set; } = -1;

	internal int LastEffectProcessedDay { get; set; } = -1;

	internal string Reason { get; set; } = string.Empty;

	internal List<PolicyEffectInstanceSaveData> ModuleEffects { get; set; } = new List<PolicyEffectInstanceSaveData>();

	internal List<PolicyEffectExecutionReceipt> ExecutionReceipts { get; set; } = new List<PolicyEffectExecutionReceipt>();
}

public sealed partial class CustomPolicyBehavior : CampaignBehaviorBase, INonReadyObjectHandler
{
	private const int MaxPolicyNameChars = 100;

	private const int MaxPolicyContentChars = 6000;

	private const int PolicyPublicFeedbackTargetMinChars = 100;

	private const int PolicyPublicFeedbackTargetMaxChars = 1800;

	private const int PolicyPublicFeedbackTargetStepChars = 100;

	private const int PolicyPublicFeedbackTargetDefaultChars = 900;

	private const int PlayerPolicyEvaluationTimeoutMilliseconds = 180000;

	private const int PolicyKnowledgeTargetChars = 580;

	private const int PolicyKnowledgeMinChars = 380;

	private const int PolicyKnowledgeMaxChars = 650;

	private const int AiPolicyGoldReserve = 1000;

	private const int LocalPolicyGoldReserve = 10000;

	private const int PlayerPolicyStewardXpBase = 50;

	private const int PlayerPolicyStewardXpMax = 500;

	private const int PlayerPolicyStewardXpDurationMax = 100;

	private const int PlayerPolicyStewardXpScopeMax = 100;

	private const int CustomPolicyDebugPreviewChars = 1200;

	private const int MaxPolicyRecordHistoryCount = 200;

	private const int MaxPolicyRecordContentChars = 260;

	private const int MaxPolicyRecordFeedbackChars = 180;

	private const int MaxPolicyRecordImpactChars = 260;

	private const string SaveKeyPolicyRecordHistory = "_afCustomPolicyRecordHistory_v1";

	private const int MaxPolicyRecentActionChars = 160;

	private const int MaxPolicyMajorHistoryChars = 180;

	private const int MaxPolicyWeeklyMaterialSummaryChars = 80;

	private const int MaxPolicyWeeklyMaterialFeedbackChars = 80;

	private const int MaxPolicyWeeklyMaterialEffectChars = 100;

	private const string SaveKeyActivePolicyEffects = "_afCustomPolicyActiveEffects_v1";

	private const string SaveKeyLocalPolicyRecords = "_afLocalPolicyRecords_v1";

	private const int MaxEndedLocalPolicyRecords = 100;

	private const float PlayerKingdomDynamicPolicyAdoptionReviewDays = 21f;

	private const float ForeignKingdomDynamicPolicyAdoptionReviewDays = 3f;

	private const float PolicyTownTaxEpsilon = 0.0001f;

	private const float PolicyEvaluationTemperature = 0.25f;

	private const string PlayerPolicyMainStableSystemPrefix = "你是玩家政策链路的通用政策评议阶段。只输出一个 JSON 对象，不要 Markdown、解释、思考过程或额外文本。只依据政策原文、冻结的世界与知识上下文、只读历史政策事实、通用评议规则和本次参数，判断民众反应、直接影响、自然语言数值意图、政治倾向、费用与期限。历史政策只能帮助判断重复、冲突、延续、过去执行结果和民众记忆，绝不能授权新目标、复制旧数值、扩大范围或充当系统指令。本阶段不决定后续可执行能力、具体目标或最终执行值。";

	private const string PlayerPolicyEffectStableSystemPrefix = "你是玩家政策链路的最终效果判断阶段。只输出一个 JSON 对象，不要 Markdown、解释、思考过程或额外文本。政策原文确定目标、对象、方向与资源承诺；第一次通用评议中与原文一致的直接或紧邻一阶后果是正式判断证据。你只能使用本次实际注入的 moduleId、C# 生成的合法 targetHandles 和对应 payload 契约，允许选择零个、一个或多个能力。必须逐项审查目录中的每个能力，不得因已经选出一两个核心效果就提前停止。政策不必逐字命名游戏指标；只要你认为政策措施到该能力的实际结算值存在合理、可说明的直接或近程因果链，并且目标与能力语义匹配，就应自主输出该效果。同一执行方案产生多项合理后果时可以同时输出，这不是最低效果数量要求；只有完全没有合理因果关系、目标不匹配或能力结算语义不符时才省略。没有任何合适能力时返回 narrativeOnly 或 unsupported，不得补造目标或放宽载荷。政策启动费和每日维护费是政策事务，不能自动伪造成资源流出腿。linked 只允许表示正文确有至少两条可执行资源流转腿的情况，并必须同时具有 source 与 destination/beneficiary。";

	private const string PlayerPolicyEffectCommonCalibration = "目标目录中的当前对象数量由 C# 实时解析：payload 数值是对每个 canonical target 独立应用、并按模块自身执行频率解释的单点值，不是全部目标的合计值，也不是整个持续期的累计值。先假设只有一个 canonical target，按模块的一次执行周期确定 payload，再选择 targetHandles；相同语义和强度在目标数或持续期不同的情况下必须保持相同单点值。不得按目标数量乘、除或机械均摊，不得乘 durationDays；只有不可编辑的模块契约明确声明 aggregate 时才能按聚合值处理。相同单点值作用于更多目标会产生更大的总影响，影响摘要与原因必须准确描述覆盖规模。同一笔投入通过补贴、采购、运输、雇佣、建设、训练或治理等不同直接执行环节产生多项合理效果，不算重复计算；启动投入本身已经是主要成本，不要为了平衡而臆造无因果依据的负面效果。政策若明确使用全面、系统化、高强度、长期或永久措施，并且具有相称的启动投入或持续维护，必须选择与这些证据相称的数值区间，不得无理由选择象征性或最低档数值，所选区间与 reason 中的强度描述必须一致；反之也不得脱离投入与执行力度虚高。每个效果仍必须服从候选目录、合法目标、载荷结构和模块执行频率；不得扩大候选、改写期限、补造目标或绕过校验。";

	private const string PolicyScopeKingdom = "kingdom";

	private const string PolicyScopeLocal = "local";

	private const string PolicyScopeVassal = "vassal";

	private const string LocalPolicyTargetScopeSource = "source";

	private const string LocalPolicyTargetScopeMentioned = "mentioned";

	private const int LocalPolicyMentionSummaryMaxChars = 120;

	private const string PolicyTargetKindSource = "source";

	private const string PolicyTargetKindSettlement = "settlement";

	private const string PolicyTargetKindClan = "clan";

	private const string PolicyTargetKindRuler = "ruler";

	private const string PolicyTargetKindKingdom = "kingdom";

	private const string PolicyTargetKindHero = "hero";

	private const string PolicyTargetKindSelector = "selector";

	private const string PolicyTargetKindPlan = "plan";

	private const string LocalPolicyStatusActive = "active";

	private const string LocalPolicyStatusExpired = "expired";

	private const string LocalPolicyStatusTargetsLost = "targets_lost";

	private const string LocalPolicyStatusAbolished = "abolished";

	private const string LocalPolicyStatusRelationshipEnded = "relationship_ended";

	private const int KingdomAgendaPolicyContextMaxChars = 2400;

	private const int KingdomAgendaLocalPolicyMaxCount = 3;

	private const int KingdomAgendaLocalPolicyNameChars = 40;

	private const int KingdomAgendaLocalPolicySummaryChars = 80;

	private const int KingdomAgendaLocalPolicyScopeChars = 120;

	private const int KingdomAgendaLocalPolicyEffectChars = 180;

	private const int KingdomAgendaLocalPolicyFeedbackChars = 40;

	private const int KingdomAgendaLocalPolicyLineChars = 420;

	private const string SaveKeyDynamicPolicyRegistry = "_afDynamicPolicyRegistry_v1";

	private const string DynamicPolicyIdPrefix = "af_policy:";

	private const string DynamicPolicyStatusPending = "pending";

	private const string DynamicPolicyStatusActive = "active";

	private const string DynamicPolicyStatusExpiryVotePending = "expiry_vote_pending";

	private const string DynamicPolicyStatusAbolished = "abolished";

	private const string DynamicPolicyStatusRejected = "rejected";

	private const string PolicyCommitStatePending = "pending";
	private const string PolicyCommitStateCommitPending = "commitPending";
	private const string PolicyCommitStateActive = "active";
	private const string PolicyCommitStateExternalCommitPending = "externalCommitPending";
	private const string PolicyCommitStateExternalCommitted = "externalCommitted";
	private const string PolicyCommitStateCompensationPending = "compensationPending";
	private const string PolicyCommitStateQuarantinedBlocked = "quarantinedBlocked";
	private const string PolicyCommitStateFailed = "failed";
	private const string PolicyCommitStateEnded = "ended";

	private const double ActivePolicyMaintenanceDefaultFrameBudgetMs = 3.0;

	// Settlement application only updates the active-effect progress ledger. Batch it to avoid serializing the full effect after every settlement.
	private const int ActivePolicySettlementBatchSize = 12;

	private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();

	private readonly Dictionary<string, string> _policyRecordHistory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _activePolicyEffects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _localPolicyRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, ActivePolicyEffectSaveData> _activePolicyEffectModelCache = new Dictionary<string, ActivePolicyEffectSaveData>(StringComparer.Ordinal);

	private readonly Dictionary<string, ActivePolicyEffectRuntimeEntry> _activePolicyEffectRuntimeCache = new Dictionary<string, ActivePolicyEffectRuntimeEntry>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Settlement> _settlementByIdRuntimeCache = new Dictionary<string, Settlement>(StringComparer.OrdinalIgnoreCase);

	private Campaign _settlementByIdRuntimeCacheCampaign;

	private readonly Dictionary<string, string> _dynamicPolicyRegistry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	// Future-schema and corrupt rows remain byte-for-byte saveable but are excluded
	// from every runtime/membership path until an explicit compatible migration exists.
	private readonly HashSet<string> _quarantinedDynamicPolicyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly Queue<PendingActivePolicyEffectWork> _pendingActivePolicyEffectWork = new Queue<PendingActivePolicyEffectWork>();

	private readonly HashSet<string> _queuedActivePolicyEffectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private int _activePolicyRuntimeGeneration;

	private int _lastActivePolicyScheduledDay = -1;

	private bool _generationInProgress;

	private CampaignTimeControlMode _previousTimeControlMode = CampaignTimeControlMode.Stop;

	private bool _previousTimeControlLock;

	private bool _waitTimeLocked;

	private bool _policyWaitPopupShown;

	private static bool _dynamicPolicyPatchesApplied;

	private static readonly System.Reflection.FieldInfo DynamicPolicyDecisionPolicyField = AccessTools.Field(typeof(KingdomPolicyDecision), nameof(KingdomPolicyDecision.Policy));

	private static readonly System.Reflection.FieldInfo DynamicPolicyDecisionInvertedField = AccessTools.Field(typeof(KingdomPolicyDecision), "_isInvertedDecision");

	private static readonly System.Reflection.FieldInfo DynamicPolicyDecisionSnapshotField = AccessTools.Field(typeof(KingdomPolicyDecision), "_kingdomPolicies");

	// DecisionItemBaseVM normally invokes this only after its native result inquiry is closed.
	// Custom policy result popups replace that inquiry, so retain the cleanup callback without
	// re-opening the native popup after the custom result has been acknowledged.
	private static readonly System.Reflection.FieldInfo DecisionItemOnDecisionOverField = AccessTools.Field(typeof(DecisionItemBaseVM), "_onDecisionOver");

	private static bool _policySettlementModelPatchesApplied;

	private static bool _policyFinanceModelPatchesApplied;

	private static bool _policyClanPoliticsModelPatchesApplied;

	private static bool _policySuccessResultVisible;

	private static string _policySuccessResultPolicyObjectId = "";

	private static readonly Dictionary<string, Action> DeferredOriginalPolicyResults = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

	public static CustomPolicyBehavior Instance { get; private set; }

	internal static void OnVassalRelationshipEndedForExternal(string vassalKingdomId, string reason)
	{
		try
		{
			Instance?.OnVassalRelationshipEndedInternal(vassalKingdomId, reason);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("VassalPolicy", "relationship-end-failed", ex.Message, ex.ToString());
		}
	}

	internal static bool TryCancelActiveKingdomPolicyForExternal(
		string policyId,
		string ownerKingdomId,
		string reason,
		out string policyName,
		out string result)
	{
		policyName = string.Empty;
		result = "error";
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				PolicySystemLog.Write("Agenda", "external-cancel-unavailable",
					"policyId=" + (policyId ?? string.Empty) + " owner=" + (ownerKingdomId ?? string.Empty));
				return false;
			}
			bool succeeded = behavior.TryCancelActiveKingdomPolicyInternal(
				policyId,
				ownerKingdomId,
				reason,
				out policyName,
				out result);
			if (succeeded)
			{
				WorldDiplomacyPolicyContext.Clear();
			}
			return succeeded;
		}
		catch (Exception ex)
		{
			result = "error";
			PolicySystemLog.Write("Agenda", "external-cancel-exception",
				"policyId=" + (policyId ?? string.Empty)
				+ " owner=" + (ownerKingdomId ?? string.Empty)
				+ " error=" + ex);
			return false;
		}
	}

	public CustomPolicyBehavior()
	{
		Instance = this;
	}

	internal static bool TryRegisterPolicyEffectBundleForExternal(PolicyEffectBundleRegistration registration, out string effectId, out string failureReason)
	{
		effectId = string.Empty;
		failureReason = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			return behavior.TryRegisterPolicyEffectBundleInternal(registration, out effectId, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Effect", "register-bundle-exception", ex.ToString());
			return false;
		}
	}

	internal static bool TryCompleteNpcPolicyEffectBundleCommitForExternal(
		string recordId,
		string activeEffectId,
		bool isRenewal,
		out string failureReason)
	{
		failureReason = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			return behavior.TryCompleteNpcPolicyEffectBundleCommit(recordId, activeEffectId, isRenewal, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Agenda", "npc-bundle-commit-callback-failed", "recordId=" + (recordId ?? string.Empty) + " " + ex);
			return false;
		}
	}

	internal static bool TryFailNpcPolicyEffectBundleCommitForExternal(
		string recordId,
		bool isRenewal,
		string reason,
		out string failureReason)
	{
		failureReason = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			return behavior.TryFailNpcPolicyEffectBundleCommit(recordId, isRenewal, reason, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Agenda", "npc-bundle-failure-callback-failed", "recordId=" + (recordId ?? string.Empty) + " " + ex);
			return false;
		}
	}

	internal static bool TryRollbackNpcPolicyEffectBundleForExternal(
		string effectId,
		string reason,
		out string failureReason)
	{
		failureReason = string.Empty;
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			return behavior.RollbackAndRemovePolicyEffectBundle(effectId, reason, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Effect", "npc-bundle-rollback-callback-failed", "effectId=" + (effectId ?? string.Empty) + " " + ex);
			return false;
		}
	}

	internal static bool TryGetPolicyEffectBundleSnapshotForExternal(
		string effectId,
		out List<PolicyEffectInstanceSaveData> moduleEffects,
		out List<PolicyEffectExecutionReceipt> receipts)
	{
		moduleEffects = new List<PolicyEffectInstanceSaveData>();
		receipts = new List<PolicyEffectExecutionReceipt>();
		string normalizedEffectId = (effectId ?? string.Empty).Trim();
		if (normalizedEffectId.Length == 0)
		{
			return false;
		}

		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null
				|| !behavior._activePolicyEffects.TryGetValue(normalizedEffectId, out string raw)
				|| string.IsNullOrWhiteSpace(raw))
			{
				return false;
			}

			JObject persisted = JObject.Parse(raw);
			if (!PolicyEffectSaveCodec.TryNormalizeActiveV4ToV8(
				persisted,
				out JObject normalizedPersisted,
				out _,
				out _))
			{
				return false;
			}

			ActivePolicyEffectSaveData active = normalizedPersisted.ToObject<ActivePolicyEffectSaveData>();
			if (active == null
				|| active.Version != 8
				|| !string.Equals((active.EffectId ?? string.Empty).Trim(), normalizedEffectId, StringComparison.OrdinalIgnoreCase)
				|| active.ModuleEffects == null
				|| active.ModuleEffects.Count == 0
				|| active.ModuleEffects.Count > PolicyEffectSaveCodec.MaxInstancesPerPolicy)
			{
				return false;
			}

			List<PolicyEffectInstanceSaveData> instanceSnapshots = new List<PolicyEffectInstanceSaveData>(active.ModuleEffects.Count);
			Dictionary<string, string> moduleIdByInstanceId = new Dictionary<string, string>(StringComparer.Ordinal);
			int totalPayloadBytes = 0;
			foreach (PolicyEffectInstanceSaveData instance in active.ModuleEffects)
			{
				string instanceId = (instance?.InstanceId ?? string.Empty).Trim();
				if (instanceId.Length == 0
					|| moduleIdByInstanceId.ContainsKey(instanceId)
					|| !PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out _)
					|| normalized == null
					|| (instance.ExecutionReceipt != null
						&& !IsValidPolicyEffectReceiptSnapshotSource(instance.ExecutionReceipt, instanceId, instance.ModuleId)))
				{
					return false;
				}
				totalPayloadBytes += Encoding.UTF8.GetByteCount(instance.Payload.ToString(Formatting.None));
				if (totalPayloadBytes > PolicyEffectSaveCodec.MaxTotalPayloadBytes)
				{
					return false;
				}
				moduleIdByInstanceId.Add(instanceId, instance.ModuleId);
				instanceSnapshots.Add(ClonePolicyEffectInstanceSnapshot(instance));
			}

			List<PolicyEffectExecutionReceipt> receiptSnapshots = new List<PolicyEffectExecutionReceipt>(active.ExecutionReceipts?.Count ?? 0);
			HashSet<string> receiptIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (PolicyEffectExecutionReceipt receipt in active.ExecutionReceipts ?? new List<PolicyEffectExecutionReceipt>())
			{
				string receiptId = (receipt?.ReceiptId ?? string.Empty).Trim();
				if (receipt == null
					|| receiptId.Length == 0
					|| !receiptIds.Add(receiptId)
					|| !moduleIdByInstanceId.TryGetValue((receipt.InstanceId ?? string.Empty).Trim(), out string moduleId)
					|| !IsValidPolicyEffectReceiptSnapshotSource(receipt, receipt.InstanceId, moduleId))
				{
					return false;
				}
				receiptSnapshots.Add(ClonePolicyEffectReceiptSnapshot(receipt));
			}

			moduleEffects = instanceSnapshots;
			receipts = receiptSnapshots;
			return true;
		}
		catch
		{
			moduleEffects = new List<PolicyEffectInstanceSaveData>();
			receipts = new List<PolicyEffectExecutionReceipt>();
			return false;
		}
	}

	private static bool IsValidPolicyEffectReceiptSnapshotSource(
		PolicyEffectExecutionReceipt source,
		string expectedInstanceId,
		string expectedModuleId)
	{
		return source != null
			&& !string.IsNullOrWhiteSpace(source.ReceiptId)
			&& string.Equals((source.InstanceId ?? string.Empty).Trim(), (expectedInstanceId ?? string.Empty).Trim(), StringComparison.Ordinal)
			&& string.Equals((source.ModuleId ?? string.Empty).Trim(), (expectedModuleId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
			&& source.TargetSet != null;
	}

	private static PolicyEffectInstanceSaveData ClonePolicyEffectInstanceSnapshot(PolicyEffectInstanceSaveData source)
	{
		return new PolicyEffectInstanceSaveData
		{
			MechanismContractVersion = source.MechanismContractVersion,
			MechanismContractHash = source.MechanismContractHash ?? string.Empty,
			ExpectedMechanismLegIds = new List<string>(source.ExpectedMechanismLegIds ?? new List<string>()),
			EffectPlanVersion = source.EffectPlanVersion,
			MechanismId = source.MechanismId ?? string.Empty,
			MechanismKind = source.MechanismKind,
			MechanismRole = source.MechanismRole,
			SourceOmitted = source.SourceOmitted,
			DestinationOmitted = source.DestinationOmitted,
			InstanceId = source.InstanceId ?? string.Empty,
			PolicyId = source.PolicyId ?? string.Empty,
			ActorHeroId = source.ActorHeroId ?? string.Empty,
			ModuleId = source.ModuleId ?? string.Empty,
			SourceModuleId = source.SourceModuleId ?? string.Empty,
			PayloadSchemaVersion = source.PayloadSchemaVersion,
			Payload = source.Payload?.DeepClone(),
			TargetSet = ClonePolicyEffectTargetSetSnapshot(source.TargetSet),
			LifecycleState = source.LifecycleState,
			StateSchemaVersion = source.StateSchemaVersion,
			RuntimeState = source.RuntimeState?.DeepClone(),
			ExecutionReceipt = source.ExecutionReceipt == null ? null : ClonePolicyEffectReceiptSnapshot(source.ExecutionReceipt),
			StartDay = source.StartDay,
			EndDay = source.EndDay,
			SourceScope = source.SourceScope ?? string.Empty,
			Reason = source.Reason ?? string.Empty
		};
	}

	private static PolicyEffectExecutionReceipt ClonePolicyEffectReceiptSnapshot(PolicyEffectExecutionReceipt source)
	{
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = source.ReceiptId ?? string.Empty,
			InstanceId = source.InstanceId ?? string.Empty,
			PolicyId = source.PolicyId ?? string.Empty,
			ModuleId = source.ModuleId ?? string.Empty,
			TargetSet = ClonePolicyEffectTargetSetSnapshot(source.TargetSet),
			Status = source.Status,
			RequestedValue = source.RequestedValue,
			AppliedValue = source.AppliedValue,
			RequestedPayload = source.RequestedPayload?.DeepClone(),
			AppliedPayload = source.AppliedPayload?.DeepClone(),
			CampaignDay = source.CampaignDay,
			Message = source.Message ?? string.Empty
		};
	}

	private static PolicyEffectCanonicalTargetSet ClonePolicyEffectTargetSetSnapshot(PolicyEffectCanonicalTargetSet source)
	{
		if (source == null)
		{
			return null;
		}
		return new PolicyEffectCanonicalTargetSet
		{
			StructureVersion = source.StructureVersion,
			SelectorHandles = new List<string>(source.SelectorHandles ?? new List<string>()),
			SelectorIds = new List<string>(source.SelectorIds ?? new List<string>()),
			TargetPlans = PolicyTargetPlanResolver.NormalizePlans(source.TargetPlans),
			SettlementIds = new List<string>(source.SettlementIds ?? new List<string>()),
			TownIds = new List<string>(source.TownIds ?? new List<string>()),
			VillageIds = new List<string>(source.VillageIds ?? new List<string>()),
			ClanIds = new List<string>(source.ClanIds ?? new List<string>()),
			KingdomIds = new List<string>(source.KingdomIds ?? new List<string>()),
			HeroIds = new List<string>(source.HeroIds ?? new List<string>()),
			ParentSettlementIds = new List<string>(source.ParentSettlementIds ?? new List<string>()),
			FollowCurrentRulingClan = source.FollowCurrentRulingClan
		};
	}



	public override void RegisterEvents()
	{
		ApplyDynamicPolicyPatchesOnce();
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		CampaignEvents.KingdomDecisionCancelled.AddNonSerializedListener(this, OnKingdomDecisionCancelled);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnPolicyTargetClanChangedKingdom);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnPolicyTargetSettlementOwnerChanged);
		CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnPolicyTargetClanLeaderChanged);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnPolicyTargetClanDestroyed);
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnPolicyTargetRulingClanChanged);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnPolicyTargetWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPolicyTargetPeaceMade);
		CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnPolicyTargetAllianceChanged);
		CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnPolicyTargetAllianceChanged);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnPolicyHeroCreated);
		CampaignEvents.HeroOccupationChangedEvent.AddNonSerializedListener(this, OnPolicyHeroOccupationChanged);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnPolicyHeroChangedClan);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnPolicyHeroKilled);
	}

	private void OnPolicyHeroCreated(Hero hero, bool isBornNaturally)
	{
		_ = isBornNaturally;
		PolicyHeroTargetSelectorResolver.OnHeroChanged(hero);
		BannerlordPolicyEffectGameBridge.Instance.InvalidateTargetCaches();
	}

	private void OnPolicyHeroOccupationChanged(Hero hero, Occupation previousOccupation)
	{
		_ = previousOccupation;
		PolicyHeroTargetSelectorResolver.OnHeroChanged(hero);
	}

	private void OnPolicyHeroChangedClan(Hero hero, Clan previousClan)
	{
		_ = previousClan;
		PolicyHeroTargetSelectorResolver.OnHeroChanged(hero);
		BannerlordPolicyEffectGameBridge.Instance.InvalidateTargetCaches();
	}

	private void OnPolicyHeroKilled(
		Hero victim,
		Hero killer,
		KillCharacterAction.KillCharacterActionDetail detail,
		bool showNotification)
	{
		_ = killer;
		_ = detail;
		_ = showNotification;
		PolicyHeroTargetSelectorResolver.OnHeroRemoved(victim);
		BannerlordPolicyEffectGameBridge.Instance.InvalidateTargetCaches();
	}

	void INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted()
	{
		InitializeLoadedDynamicPoliciesBeforeNonReadyCleanup();
	}

	public static bool TrySubmitNpcPolicyAgendaForExternal(NpcRulerPolicyRecord record, out string failureReason)
	{
		failureReason = "";
		try
		{
			CustomPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "CustomPolicyBehavior 未注册";
				return false;
			}
			if (record == null || string.IsNullOrWhiteSpace(record.PolicyId))
			{
				failureReason = "NPC 政策记录无效";
				return false;
			}
			if (!TryReadPoliticalWeights(record.AuthoritarianWeight, record.OligarchicWeight, record.EgalitarianWeight, out float authoritarian, out float oligarchic, out float egalitarian))
			{
				failureReason = "NPC 政策政治权重缺失或无效";
				return false;
			}
			DynamicPolicySaveData data = new DynamicPolicySaveData
			{
				PolicyObjectId = FirstNonEmpty(record.PolicyObjectId, DynamicPolicyIdPrefix + NormalizeDynamicPolicyIdPart(record.PolicyId)),
				RecordId = record.PolicyId ?? "",
				Source = "npc",
				OwnerKingdomId = record.KingdomId ?? "",
				ProposerClanId = ResolveKingdomStatic(record.KingdomId)?.RulingClan?.StringId ?? "",
				PolicyName = record.PolicyName ?? "",
				PolicyContent = record.PolicyContent ?? "",
				LogEntryDescription = FirstNonEmpty(record.PolicyDigest, record.PolicyContent),
				SecondaryEffects = record.ImpactSummary ?? "",
				AuthoritarianWeight = authoritarian,
				OligarchicWeight = oligarchic,
				EgalitarianWeight = egalitarian,
				Status = DynamicPolicyStatusPending,
				CreatedUtcTicks = record.CreatedUtcTicks > 0L ? record.CreatedUtcTicks : DateTime.UtcNow.Ticks
			};
			record.PolicyObjectId = data.PolicyObjectId;
			return behavior.TrySubmitDynamicPolicyAgenda(data, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			PolicySystemLog.Write("Agenda", "npc-submit-exception", ex.ToString());
			return false;
		}
	}

	public static void TryQueuePolicyExpiryAgendaForExternal(string recordId)
	{
		try
		{
			(CustomPolicyBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<CustomPolicyBehavior>())
				?.TryQueueNaturalExpiryAbolition(recordId, "");
		}
		catch (Exception ex)
		{
			PolicySystemLog.Write("Agenda", "expiry-submit-bridge-failed", "recordId=" + (recordId ?? "") + " " + ex);
		}
	}

	private static float CalculatePolicyCostScale(float requiredCost, float actualCost)
	{
		if (requiredCost <= 0.0001f)
		{
			return 1f;
		}
		if (float.IsNaN(actualCost) || float.IsInfinity(actualCost) || actualCost <= 0f)
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(1f, actualCost / requiredCost));
	}

	private static bool HasAnyDailyDelta(AppliedKingdomEffect effect)
	{
		return effect?.ModuleEffects?.Any(instance => instance != null) == true;
	}

	private static int NormalizePolicyPublicFeedbackTargetChars(int value)
	{
		if (value <= 0)
		{
			value = PolicyPublicFeedbackTargetDefaultChars;
		}
		int clamped = Math.Max(PolicyPublicFeedbackTargetMinChars, Math.Min(PolicyPublicFeedbackTargetMaxChars, value));
		int rounded = ((clamped + (PolicyPublicFeedbackTargetStepChars / 2)) / PolicyPublicFeedbackTargetStepChars) * PolicyPublicFeedbackTargetStepChars;
		return Math.Max(PolicyPublicFeedbackTargetMinChars, Math.Min(PolicyPublicFeedbackTargetMaxChars, rounded));
	}

	private static Kingdom GetPlayerKingdom()
	{
		return Clan.PlayerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
	}

	private static bool IsPlayerRuler(Kingdom kingdom)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			return kingdom != null && mainHero != null && Clan.PlayerClan != null && (kingdom.RulingClan == Clan.PlayerClan || kingdom.Leader == mainHero || mainHero.IsFactionLeader && mainHero.MapFaction == kingdom);
		}
		catch
		{
			return false;
		}
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static string FormatCurrentCampaignDate()
	{
		try
		{
			int day = GetCurrentCampaignDay();
			int daysInSeason = GetDaysInSeasonSafe();
			int daysInYear = GetDaysInYearSafe(daysInSeason);
			int year = day / Math.Max(1, daysInYear);
			int dayOfYear = day % Math.Max(1, daysInYear);
			int season = dayOfYear / Math.Max(1, daysInSeason);
			int dayOfSeason = dayOfYear % Math.Max(1, daysInSeason) + 1;
			return year.ToString(CultureInfo.InvariantCulture) + "年" + GetSeasonTextZh(season) + "季" + dayOfSeason.ToString(CultureInfo.InvariantCulture) + "日";
		}
		catch
		{
			try
			{
				return CampaignTime.Now.ToString();
			}
			catch
			{
				return "未知日期";
			}
		}
	}

	private static int GetDaysInSeasonSafe()
	{
		try
		{
			int days = CampaignTime.DaysInSeason;
			if (days > 0)
			{
				return days;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafe(int daysInSeason)
	{
		try
		{
			int days = CampaignTime.DaysInYear;
			if (days > 0)
			{
				return days;
			}
		}
		catch
		{
		}
		return Math.Max(1, daysInSeason) * 4;
	}

	private static string GetSeasonTextZh(int seasonIndexZeroBased)
	{
		int value = seasonIndexZeroBased % 4;
		if (value < 0)
		{
			value += 4;
		}
		return value switch
		{
			0 => "春",
			1 => "夏",
			2 => "秋",
			_ => "冬"
		};
	}

	private static string NormalizePolicyName(string value)
	{
		value = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (value.Length > MaxPolicyNameChars)
		{
			value = value.Substring(0, MaxPolicyNameChars);
		}
		return value;
	}

	private static string NormalizePolicyContent(string value)
	{
		value = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (value.Length > MaxPolicyContentChars)
		{
			value = value.Substring(0, MaxPolicyContentChars);
		}
		return value;
	}

	private static string CleanLlmText(string text)
	{
		return (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
	}

	private static string BuildPolicyRequestLogPrefix(PolicyDraftRequest request)
	{
		string requestId = (request?.RequestId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(requestId))
		{
			requestId = "(none)";
		}
		return "requestId=" + requestId;
	}

	private static string BuildPolicyRecordLogPrefix(PolicyDraftRequest request, string recordId)
	{
		return BuildPolicyRequestLogPrefix(request) + " recordId=" + ((recordId ?? "").Trim());
	}

	private static int CountParsedPolicyEffects(PolicyGenerationResult result)
	{
		try
		{
			return result?.Postprocess?.Effects?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private static string PreviewForPolicyDebugLog(string text, int maxChars = CustomPolicyDebugPreviewChars)
	{
		return LimitDisplayChars(text ?? "", maxChars);
	}

	private static void PolicyDebugLog(string stage, string message)
	{
		PolicyDebugLog(stage, message, null);
	}

	private static string BuildPolicyEffectLedgerLine(string recordId, string effectId, AppliedKingdomEffect effect, int day, int remainingDays)
	{
		if (effect == null)
		{
			return "recordId=" + (recordId ?? "") + " effectId=" + (effectId ?? "") + " effect=null";
		}
		string modules = string.Join(",", PolicyEffectSaveCodec.DescribeInstances(effect.ModuleEffects));
		return "recordId=" + (recordId ?? "")
			+ " effectId=" + (effectId ?? effect.EffectId ?? "")
			+ " day=" + day.ToString(CultureInfo.InvariantCulture)
			+ " targetKingdomId=" + (effect.KingdomId ?? "")
			+ " targetKingdomName=" + (effect.KingdomName ?? "")
			+ " towns=" + effect.TownCount.ToString(CultureInfo.InvariantCulture)
			+ " villages=" + effect.VillageCount.ToString(CultureInfo.InvariantCulture)
			+ " remaining=" + remainingDays.ToString(CultureInfo.InvariantCulture)
			+ " duration=" + effect.DurationDays.ToString(CultureInfo.InvariantCulture)
			+ " modules=" + modules;
	}

	private static void PolicyEffectLedgerLog(string stage, string message)
	{
		PolicySystemLog.Write("Effect", stage, message);
	}

	private static void PolicyDebugLog(string stage, string message, string detail)
	{
		PolicySystemLog.Write("Player", stage, message, detail);
	}

	private static string SafeSerializeForDebug(object value)
	{
		try
		{
			return JsonConvert.SerializeObject(value, Formatting.Indented) ?? "";
		}
		catch (Exception ex)
		{
			return "[serialize failed] " + ex.Message;
		}
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		return (kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国").Trim();
	}

	private static string FormatNumber(float value)
	{
		return value.ToString("0.#", CultureInfo.InvariantCulture);
	}

	private static string FormatPercent(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = 0f;
		}
		value = Math.Max(0f, Math.Min(1f, value));
		return (value * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
	}

	private static string FormatSigned(float value)
	{
		if (Math.Abs(value) < 0.0001f)
		{
			return "±0";
		}
		return (value > 0f ? "+" : "") + value.ToString("0.#", CultureInfo.InvariantCulture);
	}

	private static void Log(string message)
	{
		PolicySystemLog.WriteRuntime("Player", message);
	}

}
