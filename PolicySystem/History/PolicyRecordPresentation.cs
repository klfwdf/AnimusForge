using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using AnimusForge.PolicyEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// A current UI projection, deliberately separate from the immutable diplomatic artifact text.
internal sealed class PolicyRecordPresentationData
{
	internal string HistoryKey, RecordId, SourceKind, ScopeKind, KingdomId, KingdomName;
	internal string TitleText, DateText, StatusText, BodyText, ImpactText;
	internal int Day;
	internal long CreatedUtcTicks;
	internal bool CanDelete, CanReReview;
}

public sealed partial class CustomPolicyBehavior
{
	private const string SaveKeyDeletedPolicyHistory = "_afDeletedPolicyHistory_v1";
	private readonly Dictionary<string, string> _deletedPolicyHistory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private long _policyRecordPresentationRevision = 1;
	private long _cachedPolicyRecordPresentationRevision = -1;
	private IReadOnlyList<PolicyRecordPresentationData> _policyRecordPresentationCache;
	private long _policyHistoryGuardRevision = -1;
	private readonly HashSet<string> _policyHistoryLiveRecords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _policyHistoryPendingRecords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _policyHistoryPendingRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, DynamicPolicySaveData> _policyHistoryDynamicRecords;
	private bool _policyHistoryUnknownEffect;

	internal static string BuildPolicyHistoryKey(string sourceKind, string recordId)
		=> (sourceKind ?? string.Empty).Trim().ToLowerInvariant() + ":" + (recordId ?? string.Empty).Trim();

	private static string LocalPolicyHistorySource(LocalPolicyRecordSaveData record)
		=> string.Equals(record?.ScopeKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase) ? "player_vassal" : "player_local";

	internal static bool IsPolicyHistoryDeleted(string sourceKind, string recordId)
		=> Instance?._deletedPolicyHistory.ContainsKey(BuildPolicyHistoryKey(sourceKind, recordId)) == true;

	internal static void InvalidatePolicyRecordPresentation()
	{
		if (Instance != null) Instance._policyRecordPresentationRevision++;
	}

	internal static long GetPolicyRecordPresentationRevision()
	{
		unchecked
		{
			return (((Instance?._policyRecordPresentationRevision ?? 0) * 397 + SaveRuntimeGuard.CurrentGeneration) * 397
				+ GetCurrentCampaignDay()) * 397 + AnimusForgeWorldEventBehavior.GetInboxVersionForExternal();
		}
	}

	internal static bool CanDeletePolicyHistoryState(string status, bool hasLiveEffects, bool hasPendingWork)
		=> !hasLiveEffects && !hasPendingWork
			&& (string.Equals(status, LocalPolicyStatusAbolished, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(status, LocalPolicyStatusExpired, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(status, LocalPolicyStatusTargetsLost, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(status, LocalPolicyStatusRelationshipEnded, StringComparison.OrdinalIgnoreCase));

	private static bool IsPendingPolicyHistoryCommit(string state)
		=> string.Equals(state, PolicyCommitStateCommitPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(state, PolicyCommitStateExternalCommitPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(state, PolicyCommitStateCompensationPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(state, PolicyCommitStateQuarantinedBlocked, StringComparison.OrdinalIgnoreCase);

	private bool HasUnfinishedPolicyRecordEffects(string recordId, bool includeLiveEffects)
	{
		EnsurePolicyHistoryGuardIndex();
		if (HasAmbiguousQuarantinedActiveEffectForRecord(recordId)) return true;
		return _policyHistoryUnknownEffect || _policyHistoryPendingRecords.Contains(recordId ?? string.Empty)
			|| (includeLiveEffects && _policyHistoryLiveRecords.Contains(recordId ?? string.Empty));
	}

	// Rebuild once per owner mutation, never once per displayed record or per frame.
	private void EnsurePolicyHistoryGuardIndex()
	{
		if (_policyHistoryGuardRevision == _policyRecordPresentationRevision) return;
		_policyHistoryLiveRecords.Clear();
		_policyHistoryPendingRecords.Clear();
		_policyHistoryPendingRoots.Clear();
		_policyHistoryUnknownEffect = false;
		foreach (KeyValuePair<string, string> item in _activePolicyEffects)
		{
			ActivePolicyEffectSaveData active = GetActivePolicyEffectForWork(item.Key, item.Value);
			if (active == null) { _policyHistoryUnknownEffect = true; continue; }
			if (IsPolicyEffectWithinDuration(active)) _policyHistoryLiveRecords.Add(active.RecordId ?? string.Empty);
			if (HasPendingExceptionalPolicyEffectState(active)) _policyHistoryPendingRecords.Add(active.RecordId ?? string.Empty);
		}
		_policyHistoryDynamicRecords = LoadDynamicPolicies().Where(record => !string.IsNullOrWhiteSpace(record.RecordId))
			.GroupBy(record => record.RecordId, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		foreach (DynamicPolicySaveData record in _policyHistoryDynamicRecords.Values)
			if (!string.IsNullOrWhiteSpace(record.ReReviewSourceRecordId) && string.Equals(record.Status, DynamicPolicyStatusPending, StringComparison.OrdinalIgnoreCase))
				_policyHistoryPendingRoots.Add(BuildPolicyHistoryKey(PolicyScopeKingdom, FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId)));
		foreach (LocalPolicyRecordSaveData record in LoadLocalPolicyRecords())
			if (!record.ReReviewReplacementCommitted && !string.IsNullOrWhiteSpace(record.ReReviewSourceRecordId)
				&& IsPendingPolicyHistoryCommit(record.ExternalCommitState))
				_policyHistoryPendingRoots.Add(BuildPolicyHistoryKey(record.ScopeKind, FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId)));
		_policyHistoryGuardRevision = _policyRecordPresentationRevision;
	}

	private bool CanAttemptPolicyRenewal(LocalPolicyRecordSaveData record, out string error)
	{
		error = "政策仍有待完成事务，或记录不可用，暂时不能续期。";
		if (!ReferenceEquals(this, Instance)) return false;
		InvalidatePolicyRecordPresentation();
		if (record == null || IsPolicyHistoryDeleted(LocalPolicyHistorySource(record), record.RecordId)
			|| HasUnfinishedPolicyRecordEffects(record.RecordId, false)
			|| IsPendingPolicyHistoryCommit(record.ExternalCommitState)
			|| HasPendingPolicyReReview(record.ScopeKind, FirstNonEmpty(record.ReReviewRootRecordId, record.RecordId))) return false;
		error = string.Empty;
		return true;
	}

	private static List<string> GetLocalPolicyRenewalTargetIds(LocalPolicyRecordSaveData record)
		=> NormalizeIdList(record?.OriginalTargetFiefIds?.Count > 0 ? record.OriginalTargetFiefIds : record?.TargetFiefIds);

	private bool CanDeletePolicyHistoryRecord(string historyKey, out string error)
	{
		try { return CanDeletePolicyHistoryRecordCore(historyKey, out error); }
		catch { error = "政策记录状态无法确认，暂时不能删除。"; return false; }
	}

	private bool CanDeletePolicyHistoryRecordCore(string historyKey, out string error)
	{
		EnsurePolicyHistoryGuardIndex();
		error = "只能删除已经废除、自然到期、全部失地或臣属关系终止，且没有待完成事务的政策记录。";
		if (string.IsNullOrWhiteSpace(historyKey) || _deletedPolicyHistory.ContainsKey(historyKey)) return false;
		int separator = historyKey.IndexOf(':');
		if (separator <= 0 || separator == historyKey.Length - 1) return false;
		string source = historyKey.Substring(0, separator);
		string id = historyKey.Substring(separator + 1);
		string status;
		if (source == "player_local" || source == "player_vassal")
		{
			if (!_localPolicyRecords.TryGetValue(id, out string raw)) return false;
			LocalPolicyRecordSaveData record = NormalizeLocalPolicyRecord(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(raw));
			if (record == null || source != LocalPolicyHistorySource(record)
				|| IsPendingPolicyHistoryCommit(record.ExternalCommitState)
				|| _policyHistoryPendingRoots.Contains(BuildPolicyHistoryKey(record.ScopeKind, FirstNonEmpty(record.ReReviewRootRecordId, id)))) return false;
			status = record.Status;
		}
		else if (source == "player_kingdom")
		{
			_policyHistoryDynamicRecords.TryGetValue(id, out DynamicPolicySaveData record);
			if (record == null || !string.Equals(record.Source, "player", StringComparison.OrdinalIgnoreCase)
				|| IsPendingPolicyHistoryCommit(record.CommitState)
				|| _policyHistoryPendingRoots.Contains(BuildPolicyHistoryKey(PolicyScopeKingdom, FirstNonEmpty(record.ReReviewRootRecordId, id)))) return false;
			status = record.Status;
		}
		else if (source == "npc")
		{
			if (!NpcRulerPolicyBehavior.TryGetPolicyRecordForPresentation(id, out NpcRulerPolicyRecord record)
				|| record.IsPlayerPolicy || record.EffectBundleRollbackPending || record.ApprovalFailureFinalizationPending) return false;
			status = record.AgendaStatus;
		}
		else return false;
		if (!CanDeletePolicyHistoryState(status, false, false) || HasUnfinishedPolicyRecordEffects(id, true)) return false;
		error = string.Empty;
		return true;
	}

	private bool TryDeletePolicyHistoryRecord(string historyKey, out string error)
	{
		try
		{
			InvalidatePolicyRecordPresentation();
			if (!CanDeletePolicyHistoryRecord(historyKey, out error)) return false;
			_deletedPolicyHistory.Add(historyKey, "deleted");
			InvalidatePolicyRecordPresentation();
		}
		catch (Exception ex)
		{
			error = "政策记录状态无法确认，未删除。";
			PolicySystemLog.Failure("History", "record-delete-failed", error, ex.ToString());
			return false;
		}
		// The tombstone is committed. Notification failures must not report a failed deletion.
		try
		{
			foreach (AnimusForgeWorldEventInboxEntry entry in AnimusForgeWorldEventBehavior.GetInboxSnapshotForExternal(240))
				if (string.Equals(GetPolicyAnnouncementHistoryKey(entry), historyKey, StringComparison.OrdinalIgnoreCase))
					AnimusForgeWorldEventBehavior.MarkEventReadForExternal(entry.EventId);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("History", "record-delete-notification-failed", "政策记录已删除，通知刷新失败。", ex.ToString());
		}
		return true;
	}

	internal static void RequestDeletePolicyHistoryRecord(string historyKey, Action onReturn)
	{
		CustomPolicyBehavior owner = Instance;
		InvalidatePolicyRecordPresentation();
		string error = "政策系统未初始化。";
		if (owner == null || !owner.CanDeletePolicyHistoryRecord(historyKey, out error))
		{
			InformationManager.DisplayMessage(new InformationMessage(error, Colors.Yellow));
			onReturn?.Invoke();
			return;
		}
		InformationManager.ShowInquiry(new InquiryData("删除政策记录",
			"删除后，该记录不再出现在政策记录、传闻或政策检索中，也不能从记录续期或重新评议。不会回滚历史效果或删除已有对话记忆。是否删除？",
			true, true, "删除记录", "取消", () =>
			{
				if (!ReferenceEquals(owner, Instance))
				{
					InformationManager.DisplayMessage(new InformationMessage("存档已切换，未删除政策记录。", Colors.Yellow));
					return;
				}
				if (!owner.TryDeletePolicyHistoryRecord(historyKey, out string failure))
					InformationManager.DisplayMessage(new InformationMessage(failure, Colors.Yellow));
				onReturn?.Invoke();
			}, onReturn), pauseGameActiveState: true);
	}

	internal static string GetPolicyAnnouncementHistoryKey(AnimusForgeWorldEventInboxEntry entry)
	{
		if (entry == null) return string.Empty;
		if (!(entry.EventId ?? string.Empty).StartsWith("npc_ruler_policy:", StringComparison.OrdinalIgnoreCase)) return string.Empty;
		string id = entry.PolicyRecordId;
		if (string.IsNullOrWhiteSpace(id) && (entry.EventId ?? string.Empty).StartsWith("npc_ruler_policy:", StringComparison.OrdinalIgnoreCase))
			id = entry.EventId.Substring("npc_ruler_policy:".Length);
		if (string.IsNullOrWhiteSpace(id)) return string.Empty;
		if (Instance != null && Instance._localPolicyRecords.TryGetValue(id, out string localRaw))
		{
			try { return BuildPolicyHistoryKey(LocalPolicyHistorySource(JsonConvert.DeserializeObject<LocalPolicyRecordSaveData>(localRaw)), id); }
			catch { return string.Empty; }
		}
		if (NpcRulerPolicyBehavior.TryGetPolicyRecordForPresentation(id, out NpcRulerPolicyRecord record))
		{
			string source = !record.IsPlayerPolicy ? "npc"
				: string.Equals(record.PolicyKind, PolicyScopeVassal, StringComparison.OrdinalIgnoreCase) ? "player_vassal"
				: string.Equals(record.PolicyKind, PolicyScopeLocal, StringComparison.OrdinalIgnoreCase) ? "player_local" : "player_kingdom";
			return BuildPolicyHistoryKey(source, id);
		}
		return BuildPolicyHistoryKey(entry.IsPlayerPolicy ? "player_kingdom" : "npc", id);
	}

	internal static bool IsPolicyAnnouncementDeleted(AnimusForgeWorldEventInboxEntry entry)
		=> Instance?._deletedPolicyHistory.ContainsKey(GetPolicyAnnouncementHistoryKey(entry)) == true;

	internal static IReadOnlyList<PolicyRecordPresentationData> GetPolicyRecordPresentationSnapshot()
	{
		CustomPolicyBehavior owner = Instance;
		if (owner == null) return Array.Empty<PolicyRecordPresentationData>();
		long revision = GetPolicyRecordPresentationRevision();
		if (owner._policyRecordPresentationCache == null || owner._cachedPolicyRecordPresentationRevision != revision)
		{
			owner._policyRecordPresentationCache = owner.BuildPolicyRecordPresentationSnapshot();
			owner._cachedPolicyRecordPresentationRevision = revision;
		}
		return owner._policyRecordPresentationCache;
	}

	private IReadOnlyList<PolicyRecordPresentationData> BuildPolicyRecordPresentationSnapshot()
	{
		List<PolicyRecordPresentationData> result = new List<PolicyRecordPresentationData>();
		Dictionary<string, PolicyRecordSaveData> histories = LoadPolicyRecordHistory().Where(x => !string.IsNullOrWhiteSpace(x.RecordId))
			.GroupBy(x => x.RecordId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		Dictionary<string, DynamicPolicySaveData> dynamics = LoadDynamicPolicies().Where(x => !string.IsNullOrWhiteSpace(x.RecordId))
			.GroupBy(x => x.RecordId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		foreach (PolicyHistoryRecordData row in BuildPolicyHistoryData().Records)
		{
			histories.TryGetValue(row.RecordId, out PolicyRecordSaveData record);
			dynamics.TryGetValue(row.RecordId, out DynamicPolicySaveData dynamic);
			NpcRulerPolicyBehavior.TryGetPlayerPolicySnapshotForExternal(row.RecordId, out NpcRulerPolicyRecord unified);
			string state = GetPolicyHistoryStatusText(FirstNonEmpty(dynamic?.Status, unified?.AgendaStatus));
			result.Add(new PolicyRecordPresentationData
			{
				HistoryKey = row.HistoryKey, RecordId = row.RecordId, SourceKind = "player_kingdom", ScopeKind = PolicyScopeKingdom,
				KingdomId = FirstNonEmpty(record?.PlayerKingdomId, dynamic?.OwnerKingdomId), KingdomName = FirstNonEmpty(record?.PlayerKingdomName, unified?.KingdomName),
				TitleText = row.PolicyNameText, DateText = row.DateText, StatusText = state, Day = record?.SubmittedDay ?? unified?.Day ?? 0,
				CreatedUtcTicks = record?.CreatedUtcTicks ?? unified?.CreatedUtcTicks ?? dynamic?.CreatedUtcTicks ?? 0,
				BodyText = BuildPolicyPresentationBody(state, FirstNonEmpty(record?.PlayerKingdomName, unified?.KingdomName), row.CostText, row.ContentSummaryText, row.FeedbackSummaryText),
				ImpactText = row.ImpactSummaryText, CanDelete = row.CanDelete, CanReReview = row.CanReReview
			});
		}
		Dictionary<string, LocalPolicyRecordSaveData> locals = LoadLocalPolicyRecords().GroupBy(x => x.RecordId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
		foreach (LocalPolicyHistoryRecordData row in BuildLocalPolicyHistoryData().Records)
		{
			LocalPolicyRecordSaveData record = locals[row.RecordId];
			LocalPolicyEffectRecordSaveData sourceEffect = record.Effects.FirstOrDefault(effect => effect.TargetScope == LocalPolicyTargetScopeSource);
			result.Add(new PolicyRecordPresentationData
			{
				HistoryKey = row.HistoryKey, RecordId = row.RecordId, SourceKind = LocalPolicyHistorySource(record), ScopeKind = row.ScopeKind,
				KingdomId = FirstNonEmpty(record.TargetKingdomId, record.IssuerKingdomId, sourceEffect?.TargetKingdomId),
				KingdomName = FirstNonEmpty(record.TargetKingdomName, record.IssuerKingdomName, sourceEffect?.TargetKingdomName),
				TitleText = "《" + row.PolicyNameText + "》", DateText = row.DateText, StatusText = row.StatusText,
				Day = record.SubmittedDay, CreatedUtcTicks = record.CreatedUtcTicks,
				BodyText = BuildPolicyPresentationBody(row.StatusText, row.TargetText, row.CostText, row.ContentText, row.FeedbackText),
				ImpactText = row.EffectText + "\n" + row.RemainingText + "\n" + row.CycleText + "\n" + row.RenewalText,
				CanDelete = row.CanDelete, CanReReview = row.CanReReview
			});
		}
		foreach (NpcRulerPolicyRecord record in NpcRulerPolicyBehavior.GetPolicyRecordsForPresentation())
		{
			if (record.IsPlayerPolicy || IsPolicyHistoryDeleted("npc", record.PolicyId)) continue;
			string key = BuildPolicyHistoryKey("npc", record.PolicyId);
			string state = GetPolicyHistoryStatusText(record.AgendaStatus);
			result.Add(new PolicyRecordPresentationData
			{
				HistoryKey = key, RecordId = record.PolicyId, SourceKind = "npc", ScopeKind = PolicyScopeKingdom,
				KingdomId = record.KingdomId, KingdomName = record.KingdomName,
				TitleText = "《" + record.PolicyName + "》", DateText = FirstNonEmpty(record.GameDate, "旧存档日期未记录"),
				StatusText = state, Day = record.Day, CreatedUtcTicks = record.CreatedUtcTicks,
				BodyText = BuildPolicyPresentationBody(state, record.KingdomName, string.Empty,
					FirstNonEmpty(record.PolicyContent, record.PolicyDigest), FirstNonEmpty(record.PublicFeedback, "民众反馈未记录。")),
				ImpactText = NpcRulerPolicyBehavior.BuildPolicyEffectSummaryForPresentation(record), CanDelete = CanDeletePolicyHistoryRecord(key, out _)
			});
		}
		return result.OrderByDescending(x => x.Day).ThenByDescending(x => x.CreatedUtcTicks).Take(480).ToArray();
	}

	private static string GetPolicyHistoryStatusText(string status)
	{
		switch ((status ?? string.Empty).Trim().ToLowerInvariant())
		{
			case "expiry_vote_pending": return "到期废除表决中";
			case "abolished": return "已废除";
			case "pending": return "待表决";
			case "approved_pending_commit": return "政策提交中";
			case "approved_renewal_pending_commit": return "续期提交中";
			case "commit_suspended": return "事务待处理";
			case "rejected": return "未通过";
			case "active": case "expired": case "targets_lost": case "relationship_ended":
				return GetLocalPolicyStatusText(status);
			default: return "状态未确认";
		}
	}

	private static string BuildPolicyPresentationBody(string status, string target, string cost, string content, string feedback)
		=> "政策状态：" + status + (string.IsNullOrWhiteSpace(target) ? "" : "\n" + target)
			+ (string.IsNullOrWhiteSpace(cost) ? "" : "\n" + cost)
			+ "\n\n【政策正文】\n" + content + "\n\n【民众反馈】\n" + feedback;
}
