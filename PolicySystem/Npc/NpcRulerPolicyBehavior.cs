using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class NpcRulerPolicyBehavior : CampaignBehaviorBase
{
	private const string SaveKeyPolicyRecords = "_afNpcRulerPolicyRecords_v1";
	private const string SaveKeyLastGeneratedDay = "_afNpcRulerPolicyLastGeneratedDay_v1";
	private const string SaveKeyLastGeneratedHour = "_afNpcRulerPolicyLastGeneratedHour_v1";
	private const string SaveKeyLastPolicyCheckDay = "_afNpcRulerPolicyLastCheckDay_v1";
	private const string AgendaStatusPending = "pending";
	private const string AgendaStatusApprovedPendingCommit = "approved_pending_commit";
	private const string AgendaStatusApprovedRenewalPendingCommit = "approved_renewal_pending_commit";
	private const string AgendaStatusCommitSuspended = "commit_suspended";
	private const string AgendaStatusActive = "active";
	private const string AgendaStatusExpiryVotePending = "expiry_vote_pending";
	private const string AgendaStatusAbolished = "abolished";
	private const string AgendaStatusRejected = "rejected";
	private const int AgendaCommitCallbackMaxAttempts = 3;
	private const float InitialGenerationCheckDelaySeconds = 8f;
	private const int MaxPoliciesPerBatch = 1;
	private const int MaxPolicyRecordCount = 180;
	private const int PublicFeedbackNoticeDelayHours = 48;
	private const int MaxNameChars = 90;
	private const int MaxContentChars = 900;
	private const int MaxFeedbackChars = 500;
	private const int MaxImpactChars = 300;
	private const int MaxReasonChars = 120;
	private const int HardContextChars = 96000;
	private const int PolicyKnowledgeTargetChars = 380;
	private const int PolicyKnowledgeMinChars = 220;
	private const int PolicyKnowledgeMaxChars = 450;
	private const int AgendaDialoguePolicyNameChars = 40;
	private const int AgendaDialoguePolicySummaryChars = 80;
	private const int AgendaDialoguePolicyFeedbackChars = 40;
	private const int AgendaDialoguePolicyEffectChars = 90;
	private const int AgendaDialoguePolicyLineChars = 300;
	private const int SuggestedProposalMaxChars = AnimusForgeTextInputSanitizer.MaxPolicyContentChars;
	private const int SuggestedNpcReplyMaxChars = 1200;
	private const int SuggestedHistoryMaxChars = 4800;
	private const int SuggestedChainNameMaxChars = 48;
	private const string PolicyKnowledgeRagFocus = "统治合法性 权力基础 政治目标 制度约束 支持者反对者 社会矛盾";
	private const int FailedGenerationBackoffHours = 6;
	private const int PolicyApiHardTimeoutMilliseconds = 540000;
	private const double PolicyCommitFrameBudgetMs = 1.0;
	private const int NpcPolicyCurrentHistoryLimit = 2;
	private const int NpcPolicyAbolishedHistoryLimit = 1;
	private const string NpcPolicyHistoryStatusActive = "active";
	private const string NpcPolicyHistoryStatusAbolished = "abolished";
	private static readonly string[] PolicyKnowledgeGovernanceTerms =
	{
		"合法", "王权", "统治", "皇帝", "女皇", "大公", "可汗", "至高王", "元老院", "波耶", "那颜", "封臣", "贵族", "氏族", "部落", "酋长",
		"军队", "亲兵", "继承", "自治", "土地", "税", "政策", "法律", "权利", "利益", "支持", "反对", "矛盾", "争议", "评价", "忠诚",
		"民众", "商人", "农户", "宗教", "信仰", "传统", "名望", "威望", "权力", "政治"
	};
	private static readonly string[] PolicyKnowledgeGeographyTerms =
	{
		"位于", "东面", "西面", "南面", "北面", "高原", "山脉", "山岭", "河流", "湖泊", "峡谷", "地形", "地貌", "流入", "发源", "海湾", "气候", "森林", "草原"
	};

	public static NpcRulerPolicyBehavior Instance { get; private set; }

	private readonly Dictionary<string, string> _policyRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentQueue<PendingNpcPolicyCommitContext> _pendingPolicyCommits = new ConcurrentQueue<PendingNpcPolicyCommitContext>();
	private readonly ConcurrentQueue<NpcPolicyGenerationJob> _pendingPolicySnapshotJobs = new ConcurrentQueue<NpcPolicyGenerationJob>();
	private readonly object _generationStateLock = new object();
	private readonly HashSet<string> _policyGenerationInFlightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _policyPresentationLastAttemptDayById
		= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	internal static Func<string, string, long, Task<string>> NpcPolicyApiTextOverrideForTests;
	internal static Func<string, float[]> NpcPolicyQueryEmbeddingOverrideForTests;
	private bool _generationInProgress;
	private string _policyActiveInFlightKey = "";
	private int _lastGeneratedDay = -1;
	private int _lastGeneratedHour = -1;
	private int _lastPolicyCheckDay = -1;
	private int _lastGenerationAttemptHour = -1;
	private int _lastGenerationFailureHour = -1;
	private int _lastGenerationRetryCount;
	private string _lastGenerationError = "";
	private NpcPolicyRetryContext _lastPolicyRetryContext;
	private int _generationVersion;
	private bool _initialGenerationCheckPending;
	private float _initialGenerationCheckElapsed;
	private int _nextPublicFeedbackNoticeDueHour = int.MaxValue;

	public NpcRulerPolicyBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		Log("registered");
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (dataStore.IsSaving)
		{
			TrimPolicyRecords();
			Dictionary<string, string> records = CampaignSaveChunkHelper.FlattenStringDictionary(_policyRecords, SaveKeyPolicyRecords, "NpcRulerPolicyRecords");
			int lastGeneratedDay = _lastGeneratedDay;
			int lastGeneratedHour = _lastGeneratedHour;
			int lastPolicyCheckDay = _lastPolicyCheckDay;
			dataStore.SyncData(SaveKeyPolicyRecords, ref records);
			dataStore.SyncData(SaveKeyLastGeneratedDay, ref lastGeneratedDay);
			dataStore.SyncData(SaveKeyLastGeneratedHour, ref lastGeneratedHour);
			dataStore.SyncData(SaveKeyLastPolicyCheckDay, ref lastPolicyCheckDay);
			Log("save-write records=" + _policyRecords.Count.ToString(CultureInfo.InvariantCulture) + " lastGeneratedDay=" + lastGeneratedDay.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + lastGeneratedHour.ToString(CultureInfo.InvariantCulture) + " lastPolicyCheckDay=" + lastPolicyCheckDay.ToString(CultureInfo.InvariantCulture));
			return;
		}
		ClearPolicyTransientRuntimeForLoadedSave("sync-load", incrementVersion: true);
		_policyRecords.Clear();
		Dictionary<string, string> stored = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData(SaveKeyPolicyRecords, ref stored);
		NpcPolicyLoadNormalizationSummary normalizationSummary = new NpcPolicyLoadNormalizationSummary();
		foreach (KeyValuePair<string, string> item in CampaignSaveChunkHelper.RestoreStringDictionary(stored, "NpcRulerPolicyRecords"))
		{
			string key = (item.Key ?? "").Trim();
			string raw = (item.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(raw))
			{
				continue;
			}
			NpcRulerPolicyRecord record = DeserializeRecordForLoad(raw, normalizationSummary);
			if (record != null && !string.IsNullOrWhiteSpace(record.PolicyId))
			{
				_policyRecords[record.PolicyId] = JsonConvert.SerializeObject(record);
			}
		}
		LogNpcPolicyLoadNormalizationSummary(normalizationSummary);
		int lastDay = -1;
		dataStore.SyncData(SaveKeyLastGeneratedDay, ref lastDay);
		_lastGeneratedDay = lastDay;
		int lastHour = -1;
		dataStore.SyncData(SaveKeyLastGeneratedHour, ref lastHour);
		_lastGeneratedHour = lastHour;
		int loadedLastPolicyCheckDay = -1;
		dataStore.SyncData(SaveKeyLastPolicyCheckDay, ref loadedLastPolicyCheckDay);
		_lastPolicyCheckDay = loadedLastPolicyCheckDay;
		if (_lastGeneratedHour < 0 && _lastGeneratedDay >= 0)
		{
			_lastGeneratedHour = _lastGeneratedDay * 24;
		}
		TrimPolicyRecords();
		Log("save-read records=" + _policyRecords.Count.ToString(CultureInfo.InvariantCulture) + " lastGeneratedDay=" + _lastGeneratedDay.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture) + " lastPolicyCheckDay=" + _lastPolicyCheckDay.ToString(CultureInfo.InvariantCulture));
	}

	public static List<NpcRulerPolicyRecord> GetRecentPolicyRecordsForExternal(string kingdomId = null, int maxCount = 20)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.GetRecentPolicyRecordsInternal(kingdomId, maxCount) ?? new List<NpcRulerPolicyRecord>();
		}
		catch
		{
			return new List<NpcRulerPolicyRecord>();
		}
	}

	public static bool TryStartSuggestedPolicyForExternal(Hero ruler, string proposalText, string npcReplyText, string historyContext, string chainName, out string failureReason)
	{
		failureReason = "";
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "非玩家统治者政策系统尚未初始化。";
				return false;
			}
			return behavior.TryStartSuggestedPolicyInternal(ruler, proposalText, npcReplyText, historyContext, chainName, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = "启动统治者建议政策失败，详细技术信息已写入日志。";
			PolicySystemLog.Failure("Npc", "proposal-generation-start-failed", ex.Message,
				"ruler=" + (ruler?.StringId ?? "") + " chain=" + Limit(chainName ?? "", SuggestedChainNameMaxChars) + " " + ex);
			return false;
		}
	}

	public void OnEngineTick()
	{
		ProcessPendingPolicySnapshotJobs();
		ProcessPendingPolicyCommits();
	}

	private void OnCampaignTick(float dt)
	{
		ProcessInitialGenerationCheck(dt);
	}

	public static bool RegisterPlayerPolicyForExternal(NpcRulerPolicyRecord record)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.RegisterPlayerPolicyInternal(record) == true;
		}
		catch (Exception ex)
		{
			Log("player-policy-register-failed policy=" + (record?.PolicyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static bool UnregisterPlayerPolicyForExternal(string policyId)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.UnregisterPlayerPolicyInternal(policyId) == true;
		}
		catch (Exception ex)
		{
			Log("player-policy-unregister-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static bool TryGetPlayerPolicySnapshotForExternal(string policyId, out NpcRulerPolicyRecord record)
	{
		record = null;
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			return behavior != null && behavior.TryGetPlayerPolicySnapshotInternal(policyId, out record);
		}
		catch (Exception ex)
		{
			record = null;
			Log("player-policy-snapshot-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	public static bool SchedulePublicFeedbackNoticeForExternal(string policyId)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.SchedulePublicFeedbackNoticeInternal(policyId, "player") == true;
		}
		catch (Exception ex)
		{
			Log("policy-feedback-schedule-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	internal static bool TryPublishPlayerPolicyPresentationForExternal(string policyId)
	{
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			return behavior != null && behavior.TryPublishPolicyPresentationInternal(policyId, "player", force: true);
		}
		catch (Exception ex)
		{
			Log("player-policy-presentation-failed policy=" + (policyId ?? string.Empty) + " error=" + ex.Message);
			return false;
		}
	}

	public static void UpdatePolicyEffectStateForExternal(string policyId, string effectId, string targetKingdomId, int remainingDays, bool isEnded)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.UpdatePolicyEffectStateInternal(policyId, effectId, targetKingdomId, remainingDays, isEnded);
		}
		catch (Exception ex)
		{
			Log("policy-effect-state-update-failed policy=" + (policyId ?? "") + " effect=" + (effectId ?? "") + " error=" + ex.Message);
		}
	}

	public static bool OnPolicyAgendaApprovedForExternal(string policyId, bool isRenewal = false)
	{
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			return behavior != null && behavior.OnPolicyAgendaApprovedInternal(policyId, isRenewal, out _);
		}
		catch (Exception ex)
		{
			Log("policy-agenda-approved-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	internal static bool TryGetPendingPolicyAgendaCommitForExternal(string policyId, out bool isRenewal)
	{
		isRenewal = false;
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			return behavior != null && behavior.TryGetPendingPolicyAgendaCommitInternal(policyId, out isRenewal);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsPolicyAgendaCommitSuspendedForExternal(string policyId)
	{
		return TryGetSuspendedPolicyAgendaCommitForExternal(policyId, out _);
	}

	internal static bool TryGetSuspendedPolicyAgendaCommitForExternal(string policyId, out bool isRenewal)
	{
		isRenewal = false;
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			return behavior != null && behavior.TryGetSuspendedPolicyAgendaCommitInternal(policyId, out isRenewal);
		}
		catch
		{
			return false;
		}
	}

	internal static bool CompleteSuspendedPolicyAgendaCommitRollbackForExternal(
		string policyId,
		bool isRenewal,
		out string failureReason)
	{
		failureReason = string.Empty;
		try
		{
			NpcRulerPolicyBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>();
			if (behavior == null)
			{
				failureReason = "NpcRulerPolicyBehavior is not registered";
				return false;
			}
			return behavior.CompleteSuspendedPolicyAgendaCommitRollbackInternal(policyId, isRenewal, out failureReason);
		}
		catch (Exception ex)
		{
			failureReason = ex.Message;
			return false;
		}
	}

	public static void OnPolicyAgendaRejectedForExternal(string policyId, string reason)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.OnPolicyAgendaRejectedInternal(policyId, reason);
		}
		catch (Exception ex)
		{
			Log("policy-agenda-rejected-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
		}
	}

	public static void UpdatePolicyAgendaStatusForExternal(string policyId, string status)
	{
		try
		{
			(Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.UpdatePolicyAgendaStatusInternal(policyId, status);
		}
		catch (Exception ex)
		{
			Log("policy-agenda-status-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
		}
	}

	private bool OnPolicyAgendaApprovedInternal(string policyId, bool isRenewal, out string failureReason)
	{
		failureReason = string.Empty;
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			failureReason = "NPC policy record is missing";
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null)
		{
			failureReason = "NPC policy record is invalid";
			return false;
		}
		string pendingStatus = isRenewal
			? AgendaStatusApprovedRenewalPendingCommit
			: AgendaStatusApprovedPendingCommit;
		string currentStatus = (record.AgendaStatus ?? string.Empty).Trim();
		if (string.Equals(currentStatus, AgendaStatusRejected, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(currentStatus, AgendaStatusAbolished, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(currentStatus, AgendaStatusCommitSuspended, StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "NPC policy is already terminal: " + currentStatus;
			return false;
		}
		if ((string.Equals(currentStatus, AgendaStatusApprovedPendingCommit, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(currentStatus, AgendaStatusApprovedRenewalPendingCommit, StringComparison.OrdinalIgnoreCase))
			&& !string.Equals(currentStatus, pendingStatus, StringComparison.OrdinalIgnoreCase))
		{
			failureReason = "NPC policy has a different pending commit kind: " + currentStatus;
			return false;
		}
		bool preserveCompletedAdoptionProgress = !isRenewal
			&& string.Equals(currentStatus, AgendaStatusActive, StringComparison.OrdinalIgnoreCase);
		if (!string.Equals(currentStatus, pendingStatus, StringComparison.OrdinalIgnoreCase)
			&& !preserveCompletedAdoptionProgress)
		{
			ResetAgendaApprovalCommitProgress(record);
		}
		record.AgendaStatus = pendingStatus;
		record.ApprovalCommitIsRenewal = isRenewal;
		_policyRecords[id] = JsonConvert.SerializeObject(record);
		EnqueueApprovedAgendaCommit(record, isRenewal);
		Log("policy-agenda-approved policy=" + id + " kingdom=" + (record.KingdomId ?? "")
			+ " renewal=" + isRenewal.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private static void ResetAgendaApprovalCommitProgress(NpcRulerPolicyRecord record)
	{
		if (record == null)
		{
			return;
		}
		record.ApprovalAnnouncementPublished = false;
		record.ApprovalPolicyEventPublished = false;
		record.ApprovalPublicFeedbackPublished = false;
		record.ApprovalWeeklyMaterialRecorded = false;
		record.ApprovalCoreCommitFailureCount = 0;
		record.ApprovalFailureCallbackFailureCount = 0;
		record.ApprovalCommitFailureReason = string.Empty;
		record.ApprovalFailureFinalizationPending = false;
		record.EffectBundleRollbackPending = false;
	}

	private bool TryGetPendingPolicyAgendaCommitInternal(string policyId, out bool isRenewal)
	{
		isRenewal = false;
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0 || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		string status = (record?.AgendaStatus ?? string.Empty).Trim();
		if (string.Equals(status, AgendaStatusApprovedRenewalPendingCommit, StringComparison.OrdinalIgnoreCase))
		{
			isRenewal = true;
			return true;
		}
		return string.Equals(status, AgendaStatusApprovedPendingCommit, StringComparison.OrdinalIgnoreCase);
	}

	private bool TryGetSuspendedPolicyAgendaCommitInternal(string policyId, out bool isRenewal)
	{
		isRenewal = false;
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0 || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (!string.Equals(record?.AgendaStatus, AgendaStatusCommitSuspended, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		isRenewal = record.ApprovalCommitIsRenewal;
		return true;
	}

	private bool CompleteSuspendedPolicyAgendaCommitRollbackInternal(
		string policyId,
		bool isRenewal,
		out string failureReason)
	{
		failureReason = string.Empty;
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0 || !_policyRecords.TryGetValue(id, out string raw))
		{
			failureReason = "suspended NPC policy record is missing";
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null)
		{
			failureReason = "suspended NPC policy record is invalid";
			return false;
		}
		if (!string.Equals(record.AgendaStatus, AgendaStatusCommitSuspended, StringComparison.OrdinalIgnoreCase)
			&& !record.EffectBundleRollbackPending)
		{
			return string.Equals(record.AgendaStatus, isRenewal ? AgendaStatusAbolished : AgendaStatusRejected, StringComparison.OrdinalIgnoreCase);
		}
		record.AgendaStatus = isRenewal ? AgendaStatusAbolished : AgendaStatusRejected;
		record.ApprovalCommitIsRenewal = isRenewal;
		record.ApprovalFailureFinalizationPending = false;
		record.EffectBundleRollbackPending = false;
		_policyRecords[id] = JsonConvert.SerializeObject(record);
		PolicySystemLog.Write("Npc", "policy-agenda-suspended-rollback-complete",
			"policyId=" + id + " renewal=" + isRenewal.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	public static bool TouchPlayerPolicyCooldownForExternal(string policyId, int day)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.TouchPlayerPolicyCooldownInternal(policyId, day) == true;
		}
		catch (Exception ex)
		{
			Log("player-policy-cooldown-touch-failed policy=" + (policyId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private void OnPolicyAgendaRejectedInternal(string policyId, string reason)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null)
		{
			return;
		}
		record.AgendaStatus = AgendaStatusRejected;
		foreach (NpcRulerPolicyEffectDto effect in record.Effects ?? new List<NpcRulerPolicyEffectDto>())
		{
			if (effect == null)
			{
				continue;
			}
			foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				if (instance?.LifecycleState == PolicyEffectLifecycleState.Prepared)
				{
					instance.LifecycleState = PolicyEffectLifecycleState.RolledBack;
				}
			}
			effect.RemainingDays = 0;
			effect.IsEnded = true;
		}
		_policyRecords[id] = JsonConvert.SerializeObject(record);
		Log("policy-agenda-rejected policy=" + id + " reason=" + (reason ?? ""));
	}

	private void UpdatePolicyAgendaStatusInternal(string policyId, string status)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null)
		{
			return;
		}
		record.AgendaStatus = string.IsNullOrWhiteSpace(status) ? record.AgendaStatus : status.Trim();
		_policyRecords[id] = JsonConvert.SerializeObject(record);
	}

	private void EnqueueApprovedAgendaCommit(NpcRulerPolicyRecord record, bool isRenewal)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.PolicyId))
		{
			return;
		}
		if (_pendingPolicyCommits.Any(context => context?.IsAgendaApprovalCommit == true
			&& context.GenerationResult?.Records?.Any(x => x != null && string.Equals(x.PolicyId, record.PolicyId, StringComparison.OrdinalIgnoreCase)) == true))
		{
			return;
		}
		NpcPolicyGenerationJob job = new NpcPolicyGenerationJob
		{
			JobId = "agenda-approval:" + record.PolicyId,
			BatchId = record.BatchId ?? "",
			TriggerSource = "agenda-approval",
			Day = Math.Max(0, record.Day),
			Hour = GetCurrentCampaignHour(),
			Version = _generationVersion,
			RuntimeGeneration = SaveRuntimeGuard.CurrentGeneration
		};
		bool resumeFailureFinalization = record.ApprovalFailureFinalizationPending;
		_pendingPolicyCommits.Enqueue(new PendingNpcPolicyCommitContext
		{
			GenerationResult = new NpcPolicyGenerationResult
			{
				Job = job,
				Success = true,
				Records = new List<NpcRulerPolicyRecord> { record },
				ParsedCount = 1
			},
			Stage = resumeFailureFinalization
				? PendingNpcPolicyCommitStage.SerializeRecord
				: PendingNpcPolicyCommitStage.CreateActiveEffect,
			IsAgendaApprovalCommit = true,
			IsRenewalCommit = isRenewal,
			RecordIndex = resumeFailureFinalization ? 1 : 0,
			ApprovalCommitFailed = resumeFailureFinalization,
			ApprovalFailureReason = resumeFailureFinalization
				? FirstNonEmpty(record.ApprovalCommitFailureReason, "NPC policy failure finalization resumed after load")
				: string.Empty
		});
	}

	public static string BuildActivePolicyDialogueContextForExternal(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		return "";
	}

	internal static bool TryBuildPolicyDialogueContextForExternal(
		string inputText,
		MentionedWorldEntities mentionedEntities,
		IEnumerable<string> explicitOwnerKingdomIds,
		Hero targetHero,
		CharacterObject targetCharacter,
		string kingdomIdOverride,
		long runtimeGeneration,
		out PolicyHistoryRetrievalResult result)
	{
		result = new PolicyHistoryRetrievalResult();
		try
		{
			if (mentionedEntities == null || mentionedEntities.IsEmpty)
			{
				result.DialogueFailureCode = "no_mentions";
				return false;
			}
			List<string> ownerKingdomIds = (explicitOwnerKingdomIds ?? Enumerable.Empty<string>())
				.Select(value => (value ?? string.Empty).Trim())
				.Where(value => value.Length > 0)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (ownerKingdomIds.Count == 0)
			{
				string targetKingdomId = ResolveDialogueTargetKingdomId(targetHero, targetCharacter, kingdomIdOverride);
				if (!string.IsNullOrWhiteSpace(targetKingdomId))
				{
					ownerKingdomIds.Add(targetKingdomId);
				}
			}
			result.DialogueOwnerKingdomIds = ownerKingdomIds;
			if (ownerKingdomIds.Count == 0)
			{
				result.DialogueFailureCode = "no_owner_scope";
				return false;
			}
			if (!TryCaptureUnifiedPolicyHistorySnapshotForExternal(
				out List<NpcPolicyHistoryEntry> entries,
				out string snapshotError))
			{
				result.DialogueFailureCode = "snapshot_unavailable";
				Log("dialogue-policy-retrieval-failed code=snapshot_unavailable hash="
					+ ComputeNpcPolicyStableTextHash(snapshotError ?? string.Empty));
				return false;
			}
			return PolicyHistoryRetrievalService.TryRetrieveDialogueByMentions(
				inputText,
				mentionedEntities,
				ownerKingdomIds,
				entries,
				runtimeGeneration,
				out result);
		}
		catch (Exception ex)
		{
			result = new PolicyHistoryRetrievalResult { DialogueFailureCode = "unexpected_failure" };
			Log("dialogue-policy-retrieval-failed code=unexpected_failure hash="
				+ ComputeNpcPolicyStableTextHash(ex.GetType().FullName ?? string.Empty));
			return false;
		}
	}

	private static string ResolveDialogueTargetKingdomId(
		Hero targetHero,
		CharacterObject targetCharacter,
		string kingdomIdOverride)
	{
		string targetKingdomId = (kingdomIdOverride ?? string.Empty).Trim();
		if (targetKingdomId.Length > 0)
		{
			return targetKingdomId;
		}
		return (targetHero?.Clan?.Kingdom?.StringId
			?? targetHero?.MapFaction?.StringId
			?? targetCharacter?.HeroObject?.Clan?.Kingdom?.StringId
			?? targetCharacter?.HeroObject?.MapFaction?.StringId
			?? string.Empty).Trim();
	}

	public static string BuildKingdomAgendaPolicyContextForExternal(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		try
		{
			return (Instance ?? Campaign.Current?.GetCampaignBehavior<NpcRulerPolicyBehavior>())?.BuildActivePolicyDialogueContextInternal(targetHero, targetCharacter, kingdomIdOverride) ?? "";
		}
		catch (Exception ex)
		{
			Log("dialogue-policy-context-failed error=" + ex.Message);
			return "";
		}
	}

	private bool RegisterPlayerPolicyInternal(NpcRulerPolicyRecord record)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.PolicyId) || string.IsNullOrWhiteSpace(record.KingdomId))
		{
			return false;
		}
		record = NormalizePersistedNpcPolicyRecord(record);
		record.Version = Math.Max(6, record.Version);
		record.IsPlayerPolicy = true;
		record.AgendaStatus = AgendaStatusActive;
		record.PolicyObjectId = string.IsNullOrWhiteSpace(record.PolicyObjectId) ? "af_policy:" + NormalizeKeyPart(record.PolicyId) : record.PolicyObjectId;
		record.CreatedUtcTicks = record.CreatedUtcTicks > 0L ? record.CreatedUtcTicks : DateTime.UtcNow.Ticks;
		record.PolicyCooldownDay = Math.Max(Math.Max(0, record.Day), record.PolicyCooldownDay);
		if (_policyRecords.TryGetValue(record.PolicyId, out string existingRaw))
		{
			NpcRulerPolicyRecord existing = DeserializeRecord(existingRaw);
			if (existing != null)
			{
				record.PublicFeedbackNoticeDueHour = existing.PublicFeedbackNoticeDueHour;
				record.PublicFeedbackNoticeShown = existing.PublicFeedbackNoticeShown;
				record.ApprovalAnnouncementPublished |= existing.ApprovalAnnouncementPublished;
				record.ApprovalPolicyEventPublished |= existing.ApprovalPolicyEventPublished;
				record.ApprovalPublicFeedbackPublished |= existing.ApprovalPublicFeedbackPublished;
				record.ApprovalWeeklyMaterialRecorded |= existing.ApprovalWeeklyMaterialRecorded;
			}
		}
		_policyRecords[record.PolicyId] = JsonConvert.SerializeObject(record);
		TrimPolicyRecords();
		Log("player-policy-ledger-registered policy=" + record.PolicyId + " kingdom=" + record.KingdomId);
		return true;
	}

	private bool TryPublishPolicyPresentationInternal(string policyId, string source, bool force)
	{
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0 || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null || !string.Equals(record.AgendaStatus, AgendaStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!HasIncompletePolicyPresentation(record))
		{
			return true;
		}
		int currentDay = GetCurrentCampaignDay();
		if (!force
			&& _policyPresentationLastAttemptDayById.TryGetValue(id, out int attemptedDay)
			&& attemptedDay >= currentDay)
		{
			return false;
		}
		_policyPresentationLastAttemptDayById[id] = currentDay;
		List<string> failures = new List<string>();

		if (!record.ApprovalAnnouncementPublished)
		{
			try
			{
				CustomPolicyBehavior.DisplayPolicyAnnouncementMessage(source, record);
				if (!record.PublicFeedbackNoticeShown && record.PublicFeedbackNoticeDueHour < 0)
				{
					SchedulePublicFeedbackNotice(record, source);
				}
				record.ApprovalAnnouncementPublished = true;
				_policyRecords[id] = JsonConvert.SerializeObject(record);
			}
			catch (Exception ex)
			{
				failures.Add("announcement: " + ex.Message);
			}
		}

		if (!record.ApprovalPolicyEventPublished)
		{
			try
			{
				if (!UpsertPolicyWorldEvent(record))
				{
					throw new InvalidOperationException("policy world-event upsert was not confirmed");
				}
				record.ApprovalPolicyEventPublished = true;
				_policyRecords[id] = JsonConvert.SerializeObject(record);
			}
			catch (Exception ex)
			{
				failures.Add("policy-event: " + ex.Message);
			}
		}

		if (!record.ApprovalPublicFeedbackPublished)
		{
			try
			{
				AnimusForgeWorldEventInboxEntry feedbackEntry = BuildPolicyFeedbackWorldEvent(record);
				if (feedbackEntry != null)
				{
					long inboxVersion = AnimusForgeWorldEventBehavior.GetInboxVersionForExternal();
					AnimusForgeWorldEventBehavior.UpsertWorldEventForExternal(feedbackEntry, markUnread: true);
					if (AnimusForgeWorldEventBehavior.GetInboxVersionForExternal() <= inboxVersion)
					{
						throw new InvalidOperationException("public-feedback upsert was not confirmed");
					}
				}
				record.ApprovalPublicFeedbackPublished = true;
				_policyRecords[id] = JsonConvert.SerializeObject(record);
			}
			catch (Exception ex)
			{
				failures.Add("public-feedback: " + ex.Message);
			}
		}

		if (!record.ApprovalWeeklyMaterialRecorded)
		{
			try
			{
				RecordUnifiedPolicyWeeklyMaterial(record);
				record.ApprovalWeeklyMaterialRecorded = true;
				_policyRecords[id] = JsonConvert.SerializeObject(record);
			}
			catch (Exception ex)
			{
				failures.Add("weekly-material: " + ex.Message);
			}
		}

		bool complete = !HasIncompletePolicyPresentation(record);
		if (!complete || failures.Count > 0)
		{
			PolicySystemLog.Failure("Npc", "policy-presentation-deferred",
				failures.Count == 0 ? "presentation remains incomplete" : string.Join(" | ", failures),
				"policyId=" + id + " source=" + (source ?? string.Empty)
				+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture));
		}
		return complete;
	}

	private void RepairIncompletePolicyPresentations(string source)
	{
		int currentDay = GetCurrentCampaignDay();
		List<NpcRulerPolicyRecord> candidates = _policyRecords.Values
			.Select(DeserializeRecord)
			.Where(record => record != null
				&& string.Equals(record.AgendaStatus, AgendaStatusActive, StringComparison.OrdinalIgnoreCase)
				&& HasIncompletePolicyPresentation(record))
			.OrderBy(record => _policyPresentationLastAttemptDayById.TryGetValue(record.PolicyId ?? string.Empty, out int day) ? day : int.MinValue)
			.ThenBy(record => record.Day)
			.ThenBy(record => record.PolicyId, StringComparer.Ordinal)
			.Take(12)
			.ToList();
		foreach (NpcRulerPolicyRecord record in candidates)
		{
			TryPublishPolicyPresentationInternal(
				record.PolicyId,
				record.IsPlayerPolicy ? "player" : "npc",
				force: false);
		}
		if (candidates.Count > 0)
		{
			Log("policy-presentation-repair source=" + (source ?? string.Empty)
				+ " day=" + currentDay.ToString(CultureInfo.InvariantCulture)
				+ " attempted=" + candidates.Count.ToString(CultureInfo.InvariantCulture));
		}
	}

	private bool UnregisterPlayerPolicyInternal(string policyId)
	{
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			Log("player-policy-unregister-rejected policy= reason=missing-policy-id");
			return false;
		}
		if (!_policyRecords.TryGetValue(id, out string raw))
		{
			Log("player-policy-unregister-rejected policy=" + id + " reason=record-not-found");
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record?.IsPlayerPolicy != true)
		{
			Log("player-policy-unregister-rejected policy=" + id + " reason=not-player-policy");
			return false;
		}
		bool removed = _policyRecords.Remove(id);
		Log("player-policy-unregistered policy=" + id + " removed=" + removed.ToString());
		return removed;
	}

	private bool TryGetPlayerPolicySnapshotInternal(string policyId, out NpcRulerPolicyRecord record)
	{
		record = null;
		string id = (policyId ?? string.Empty).Trim();
		if (id.Length == 0 || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord snapshot = DeserializeRecord(raw);
		if (snapshot?.IsPlayerPolicy != true)
		{
			return false;
		}
		record = snapshot;
		return true;
	}

	private bool TouchPlayerPolicyCooldownInternal(string policyId, int day)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null || !record.IsPlayerPolicy)
		{
			return false;
		}
		record.PolicyCooldownDay = Math.Max(Math.Max(record.Day, record.PolicyCooldownDay), Math.Max(0, day));
		_policyRecords[id] = JsonConvert.SerializeObject(record);
		Log("player-policy-cooldown-touched policy=" + id + " kingdom=" + (record.KingdomId ?? "") + " day=" + record.PolicyCooldownDay.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private void UpdatePolicyEffectStateInternal(string policyId, string effectId, string targetKingdomId, int remainingDays, bool isEnded)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record?.Effects == null)
		{
			return;
		}
		string cleanEffectId = (effectId ?? "").Trim();
		string cleanTargetId = (targetKingdomId ?? "").Trim();
		NpcRulerPolicyEffectDto effect = record.Effects.FirstOrDefault(x => x != null
			&& !string.IsNullOrWhiteSpace(cleanEffectId)
			&& ((x.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>()).Any(instance => instance != null && string.Equals((instance.InstanceId ?? "").Trim(), cleanEffectId, StringComparison.OrdinalIgnoreCase))
				|| string.Equals((x.EffectId ?? "").Trim(), cleanEffectId, StringComparison.OrdinalIgnoreCase)));
		if (effect == null)
		{
			effect = record.Effects.FirstOrDefault(x => x != null && string.Equals((x.TargetKingdomId ?? "").Trim(), cleanTargetId, StringComparison.OrdinalIgnoreCase));
		}
		if (effect == null)
		{
			return;
		}
		string canonicalEffectId = FirstNonEmpty(cleanEffectId, effect.EffectId);
		if (!TrySynchronizeNpcPolicyEffectBundleSnapshot(record, effect, canonicalEffectId, out string failureReason))
		{
			PolicySystemLog.Write("Npc", "effect-state-snapshot-rejected", "policyId=" + id
				+ " effectId=" + canonicalEffectId
				+ " target=" + cleanTargetId
				+ " reportedRemainingDays=" + Math.Max(0, remainingDays).ToString(CultureInfo.InvariantCulture)
				+ " reportedEnded=" + isEnded.ToString()
				+ " reason=" + failureReason);
			return;
		}
		SynchronizeNpcPolicyEffectShell(effect);
		_policyRecords[id] = JsonConvert.SerializeObject(record);
	}

	private void OnDailyTick()
	{
		EnsureSuspendedPolicyAgendaCommitReconciliationScheduled("daily", logDeferredRollbackOwner: false);
		RepairIncompletePolicyPresentations("daily");
		TryStartPolicyGeneration("daily", logSkips: true);
	}

	private void OnHourlyTick()
	{
		int currentHour = GetCurrentCampaignHour();
		if (currentHour < _nextPublicFeedbackNoticeDueHour)
		{
			return;
		}
		PublishDuePublicFeedbackNotices(currentHour);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		EnsureSuspendedPolicyAgendaCommitReconciliationScheduled("session", logDeferredRollbackOwner: true);
		foreach (NpcRulerPolicyRecord record in _policyRecords.Values.Select(DeserializeRecord)
			.Where(x => x != null && (string.Equals(x.AgendaStatus, AgendaStatusApprovedPendingCommit, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.AgendaStatus, AgendaStatusApprovedRenewalPendingCommit, StringComparison.OrdinalIgnoreCase))))
		{
			EnqueueApprovedAgendaCommit(
				record,
				string.Equals(record.AgendaStatus, AgendaStatusApprovedRenewalPendingCommit, StringComparison.OrdinalIgnoreCase));
		}
		RebuildPublicFeedbackNoticeSchedule();
		RepairIncompletePolicyPresentations("session");
		int currentHour = GetCurrentCampaignHour();
		if (currentHour >= _nextPublicFeedbackNoticeDueHour)
		{
			PublishDuePublicFeedbackNotices(currentHour);
		}
		_initialGenerationCheckPending = true;
		_initialGenerationCheckElapsed = 0f;
		Log("session-launched pending-initial-check day=" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + " hour=" + GetCurrentCampaignHour().ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture) + " lastPolicyCheckDay=" + _lastPolicyCheckDay.ToString(CultureInfo.InvariantCulture));
	}

	private void EnsureSuspendedPolicyAgendaCommitReconciliationScheduled(
		string source,
		bool logDeferredRollbackOwner)
	{
		List<NpcRulerPolicyRecord> suspendedRecords = _policyRecords.Values
			.Select(DeserializeRecord)
			.Where(record => record != null
				&& !string.IsNullOrWhiteSpace(record.PolicyId)
				&& string.Equals(record.AgendaStatus, AgendaStatusCommitSuspended, StringComparison.OrdinalIgnoreCase))
			.ToList();
		foreach (NpcRulerPolicyRecord record in suspendedRecords)
		{
			string rollbackEffectId = "npc_ruler_policy_bundle:" + NormalizeKeyPart(record.PolicyId);
			bool activeRollbackPending = record.EffectBundleRollbackPending
				&& CustomPolicyBehavior.HasPersistedPolicyEffectRollbackForExternal(rollbackEffectId);
			if (activeRollbackPending)
			{
				if (logDeferredRollbackOwner)
				{
					PolicySystemLog.Write("Npc", "policy-agenda-suspended-reconciliation-restored",
						"policyId=" + record.PolicyId
						+ " renewal=" + record.ApprovalCommitIsRenewal.ToString(CultureInfo.InvariantCulture)
						+ " owner=core-active-rollback"
						+ " source=" + (source ?? string.Empty));
				}
				continue;
			}

			bool stateChanged = !record.ApprovalFailureFinalizationPending;
			record.ApprovalFailureFinalizationPending = true;
			if (string.IsNullOrWhiteSpace(record.ApprovalCommitFailureReason))
			{
				record.ApprovalCommitFailureReason = record.EffectBundleRollbackPending
					? "suspended NPC rollback active record was missing; terminal reconciliation required"
					: "suspended NPC policy failure finalization requires retry";
				stateChanged = true;
			}
			if (stateChanged)
			{
				_policyRecords[record.PolicyId] = JsonConvert.SerializeObject(record);
			}
			EnqueueApprovedAgendaCommit(record, record.ApprovalCommitIsRenewal);
			if (stateChanged || logDeferredRollbackOwner)
			{
				PolicySystemLog.Write("Npc", "policy-agenda-suspended-reconciliation-restored",
					"policyId=" + record.PolicyId
					+ " renewal=" + record.ApprovalCommitIsRenewal.ToString(CultureInfo.InvariantCulture)
					+ " rollbackFlag=" + record.EffectBundleRollbackPending.ToString(CultureInfo.InvariantCulture)
					+ " owner=npc-failure-finalization"
					+ " source=" + (source ?? string.Empty));
			}
		}
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		ClearPolicyTransientRuntimeForLoadedSave("new_game_created", incrementVersion: true);
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		ClearPolicyTransientRuntimeForLoadedSave("game_loaded", incrementVersion: true);
	}

	private void ClearPolicyTransientRuntimeForLoadedSave(string reason, bool incrementVersion)
	{
		_lastGenerationAttemptHour = -1;
		_lastGenerationFailureHour = -1;
		_lastGenerationRetryCount = 0;
		_lastGenerationError = "";
		_lastPolicyRetryContext = null;
		if (incrementVersion)
		{
			_generationVersion++;
		}
		_initialGenerationCheckPending = false;
		_initialGenerationCheckElapsed = 0f;
		_nextPublicFeedbackNoticeDueHour = int.MaxValue;
		_policyPresentationLastAttemptDayById.Clear();
		while (_pendingPolicyCommits.TryDequeue(out var _))
		{
		}
		while (_pendingPolicySnapshotJobs.TryDequeue(out var _))
		{
		}
		PolicyHistoryRetrievalService.ClearTransientCache();
		ResetPolicyGenerationLifecycleForRuntimeClear();
		Log("transient-cleared reason=" + (reason ?? ""));
	}

	private bool SchedulePublicFeedbackNoticeInternal(string policyId, string source)
	{
		string id = (policyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || !_policyRecords.TryGetValue(id, out string raw))
		{
			return false;
		}
		NpcRulerPolicyRecord record = DeserializeRecord(raw);
		if (record == null || !SchedulePublicFeedbackNotice(record, source))
		{
			return false;
		}
		_policyRecords[id] = JsonConvert.SerializeObject(record);
		return true;
	}

	private bool SchedulePublicFeedbackNotice(NpcRulerPolicyRecord record, string source)
	{
		if (record == null || string.IsNullOrWhiteSpace(record.PolicyId) || record.PublicFeedbackNoticeShown || record.PublicFeedbackNoticeDueHour >= 0)
		{
			return false;
		}
		int currentHour = GetCurrentCampaignHour();
		int dueHour = currentHour > int.MaxValue - PublicFeedbackNoticeDelayHours
			? int.MaxValue
			: currentHour + PublicFeedbackNoticeDelayHours;
		record.PublicFeedbackNoticeDueHour = dueHour;
		record.PublicFeedbackNoticeShown = false;
		_nextPublicFeedbackNoticeDueHour = Math.Min(_nextPublicFeedbackNoticeDueHour, dueHour);
		Log("policy-feedback-scheduled source=" + (source ?? "")
			+ " policy=" + (record.PolicyId ?? "")
			+ " currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture)
			+ " dueHour=" + dueHour.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private void RebuildPublicFeedbackNoticeSchedule()
	{
		int nextDueHour = int.MaxValue;
		foreach (string raw in _policyRecords.Values)
		{
			NpcRulerPolicyRecord record = DeserializeRecord(raw);
			if (record == null || record.PublicFeedbackNoticeShown || record.PublicFeedbackNoticeDueHour < 0)
			{
				continue;
			}
			nextDueHour = Math.Min(nextDueHour, record.PublicFeedbackNoticeDueHour);
		}
		_nextPublicFeedbackNoticeDueHour = nextDueHour;
	}

	private void PublishDuePublicFeedbackNotices(int currentHour)
	{
		int nextDueHour = int.MaxValue;
		int displayedCount = 0;
		foreach (string policyId in _policyRecords.Keys.ToList())
		{
			if (!_policyRecords.TryGetValue(policyId, out string raw))
			{
				continue;
			}
			NpcRulerPolicyRecord record = DeserializeRecord(raw);
			if (record == null || record.PublicFeedbackNoticeShown || record.PublicFeedbackNoticeDueHour < 0)
			{
				continue;
			}
			if (record.PublicFeedbackNoticeDueHour > currentHour)
			{
				nextDueHour = Math.Min(nextDueHour, record.PublicFeedbackNoticeDueHour);
				continue;
			}
			string source = record.IsPlayerPolicy ? "player" : "npc";
			if (CustomPolicyBehavior.DisplayKingdomPolicyFeedbackMessage(
				source,
				record.PolicyId,
				record.KingdomName,
				record.PolicyName,
				record.PublicFeedback))
			{
				record.PublicFeedbackNoticeShown = true;
				record.PublicFeedbackNoticeDueHour = -1;
				_policyRecords[policyId] = JsonConvert.SerializeObject(record);
				displayedCount++;
			}
			else
			{
				nextDueHour = Math.Min(nextDueHour, record.PublicFeedbackNoticeDueHour);
			}
		}
		_nextPublicFeedbackNoticeDueHour = nextDueHour;
		if (displayedCount > 0)
		{
			Log("policy-feedback-release-complete count=" + displayedCount.ToString(CultureInfo.InvariantCulture)
				+ " currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture)
				+ " nextDueHour=" + (nextDueHour == int.MaxValue ? "none" : nextDueHour.ToString(CultureInfo.InvariantCulture)));
		}
	}

	private static string BuildPolicyGenerationInFlightKey(int currentDay, int currentHour, NpcRulerPolicyBatchContext context)
	{
		IEnumerable<string> ids = (context?.Kingdoms ?? new List<NpcRulerPolicyKingdomContext>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
			.Select(x => x.KingdomId.Trim());
		if (!ids.Any())
		{
			ids = (context?.PendingTargets ?? new List<NpcRulerPolicySnapshotTarget>())
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.KingdomId))
				.Select(x => x.KingdomId.Trim());
		}
		string kingdomKey = string.Join(",", ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
		return "npc_policy:" + Math.Max(0, currentHour).ToString(CultureInfo.InvariantCulture)
			+ ":" + NormalizeKeyPart(kingdomKey.Length == 0 ? currentDay.ToString(CultureInfo.InvariantCulture) : kingdomKey);
	}

	// Policy generation lifecycle invariant:
	// - _generationInProgress stays true from reservation until the main-thread pending commit reaches a terminal path.
	// - _policyGenerationInFlightKeys tracks only the API/scheduling slot; background finally may release that slot but must not complete the generation.
	// - _generationVersion is checked before terminal release, so an old commit cannot clear a newer job.
	private bool IsPolicyGenerationBusy(out string activeInFlightKey)
	{
		lock (_generationStateLock)
		{
			activeInFlightKey = _policyActiveInFlightKey ?? "";
			return _generationInProgress || _policyGenerationInFlightKeys.Count > 0;
		}
	}

	private static string NormalizePolicyGenerationInFlightKey(string inFlightKey)
	{
		string key = (inFlightKey ?? "").Trim();
		return string.IsNullOrWhiteSpace(key) ? "npc_policy:unknown" : key;
	}

	private bool TryReservePolicyGenerationLifecycle(string inFlightKey, out string activeInFlightKey)
	{
		string key = NormalizePolicyGenerationInFlightKey(inFlightKey);
		lock (_generationStateLock)
		{
			activeInFlightKey = _policyActiveInFlightKey ?? "";
			if (_generationInProgress || _policyGenerationInFlightKeys.Contains(key))
			{
				return false;
			}
			_policyGenerationInFlightKeys.Add(key);
			_policyActiveInFlightKey = key;
			_generationInProgress = true;
			activeInFlightKey = key;
			return true;
		}
	}

	private void ReleasePolicyGenerationLifecycle(string inFlightKey, bool completeGeneration)
	{
		string key = (inFlightKey ?? "").Trim();
		lock (_generationStateLock)
		{
			if (!string.IsNullOrWhiteSpace(key))
			{
				_policyGenerationInFlightKeys.Remove(key);
			}
			if (completeGeneration)
			{
				_generationInProgress = false;
			}
			if (!_generationInProgress && (string.IsNullOrWhiteSpace(key) || string.Equals(_policyActiveInFlightKey, key, StringComparison.OrdinalIgnoreCase) || _policyGenerationInFlightKeys.Count == 0))
			{
				_policyActiveInFlightKey = _policyGenerationInFlightKeys.FirstOrDefault() ?? "";
			}
		}
	}

	private void ResetPolicyGenerationLifecycleForRuntimeClear()
	{
		lock (_generationStateLock)
		{
			_generationInProgress = false;
			_policyGenerationInFlightKeys.Clear();
			_policyActiveInFlightKey = "";
		}
	}
}
