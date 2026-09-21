using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	private HashSet<string> _pendingDebtPromiseQuestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static string BuildDebtId()
	{
		try
		{
			return "D" + Guid.NewGuid().ToString("N").Substring(0, 8)
				.ToUpperInvariant();
		}
		catch
		{
			return "D" + DateTime.UtcNow.Ticks;
		}
	}

	private void QueueDebtPromiseQuest(string ownerKey, string debtId)
	{
		string text = (ownerKey ?? "").Trim();
		string text2 = (debtId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		if (_pendingDebtPromiseQuestKeys == null)
		{
			_pendingDebtPromiseQuestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		// The separator cannot occur in generated IDs and avoids allocating a request object for each promise.
		_pendingDebtPromiseQuestKeys.Add(text + "\u001f" + text2);
	}

	private void QueueDebtPromiseQuestsForActiveDebts()
	{
		if (_debts == null || _debts.Count == 0)
		{
			return;
		}
		// This migration/reconciliation is called only after load or import, never from the daily debt-maintenance loop.
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			if (string.IsNullOrWhiteSpace(debt.Key) || debt.Value == null)
			{
				continue;
			}
			NormalizeDebtRecord(debt.Value);
			if (debt.Value.DebtLines == null)
			{
				continue;
			}
			for (int i = 0; i < debt.Value.DebtLines.Count; i++)
			{
				DebtRecord.DebtLine debtLine = debt.Value.DebtLines[i];
				if (debtLine != null && debtLine.RemainingAmount > 0)
				{
					QueueDebtPromiseQuest(debt.Key, debtLine.DebtId);
				}
			}
		}
	}

	private void DrainPendingDebtPromiseQuestCreations()
	{
		if (_pendingDebtPromiseQuestKeys == null || _pendingDebtPromiseQuestKeys.Count == 0 || !CanStartDebtPromiseQuest())
		{
			return;
		}
		// Copy then clear so a task created by this pass can safely enqueue a later promise without being lost.
		List<string> list = _pendingDebtPromiseQuestKeys.ToList();
		_pendingDebtPromiseQuestKeys.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			if (!TryParseDebtPromiseQuestKey(list[i], out var ownerKey, out var debtId)
				|| !TryGetActiveDebtPromiseQuestData(ownerKey, debtId, out var debtorName, out var debtSummary, out var deadlineText, out var debtNote, out var dueDay, out var isDueUnlimited))
			{
				// A same-conversation ADP can clear the debt before this deferred task creation runs.
				continue;
			}
			EnsureDebtPromiseQuest(ownerKey, debtId, debtorName, debtSummary, deadlineText, debtNote, dueDay, isDueUnlimited);
		}
	}

	private static bool CanStartDebtPromiseQuest()
	{
		try
		{
			return Campaign.Current != null && Campaign.Current.QuestManager != null && (Campaign.Current.ConversationManager == null || !Campaign.Current.ConversationManager.IsConversationInProgress);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryParseDebtPromiseQuestKey(string value, out string ownerKey, out string debtId)
	{
		ownerKey = "";
		debtId = "";
		string text = value ?? "";
		int num = text.IndexOf('\u001f');
		if (num <= 0 || num >= text.Length - 1)
		{
			return false;
		}
		ownerKey = text.Substring(0, num).Trim();
		debtId = text.Substring(num + 1).Trim();
		return !string.IsNullOrWhiteSpace(ownerKey) && !string.IsNullOrWhiteSpace(debtId);
	}

	private bool TryGetActiveDebtPromiseQuestData(string ownerKey, string debtId, out string debtorName, out string debtSummary, out string deadlineText, out string debtNote, out float dueDay, out bool isDueUnlimited)
	{
		debtorName = "";
		debtSummary = "";
		deadlineText = "";
		debtNote = "";
		dueDay = 0f;
		isDueUnlimited = false;
		DebtRecord debtRecord = GetDebtRecordByKey(ownerKey);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		DebtRecord.DebtLine debtLine = debtRecord.DebtLines?.FirstOrDefault((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0 && string.Equals(x.DebtId ?? "", debtId, StringComparison.OrdinalIgnoreCase));
		if (debtLine == null)
		{
			return false;
		}
		Hero hero = null;
		try
		{
			hero = Hero.Find(ownerKey);
		}
		catch
		{
			hero = null;
		}
		if (hero != null)
		{
			debtorName = hero.Name?.ToString() ?? ownerKey;
		}
		else if (TryParseSettlementMerchantDebtKey(ownerKey, out var settlementId, out var kind))
		{
			Settlement settlement = ResolveSettlementById(settlementId);
			debtorName = BuildSettlementMerchantDebtLabel(settlement, kind);
		}
		else
		{
			debtorName = ownerKey;
		}
		debtSummary = BuildDebtPromiseSummary(debtLine);
		deadlineText = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
		debtNote = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
		// Pass raw deadline state so the task can use the native countdown instead of parsing display text.
		dueDay = debtLine.DueDay;
		isDueUnlimited = debtLine.IsDueUnlimited;
		return true;
	}

	private static string BuildDebtPromiseSummary(DebtRecord.DebtLine debtLine)
	{
		if (debtLine == null)
		{
			return "未说明";
		}
		int num = Math.Max(0, debtLine.RemainingAmount);
		if (debtLine.IsGold)
		{
			return num + " 第纳尔";
		}
		ItemObject itemObject = ResolveItemById(debtLine.ItemId);
		string text = itemObject?.Name?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = string.IsNullOrWhiteSpace(debtLine.ItemId) ? "物品" : debtLine.ItemId;
		}
		return text + " ×" + num;
	}

	private void EnsureDebtPromiseQuest(string ownerKey, string debtId, string debtorName, string debtSummary, string deadlineText, string debtNote, float dueDay, bool isDueUnlimited)
	{
		try
		{
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				DebtPromiseQuest debtPromiseQuest = quest as DebtPromiseQuest;
				if (debtPromiseQuest != null && debtPromiseQuest.IsOngoing && debtPromiseQuest.Matches(ownerKey, debtId))
				{
					// Existing saves receive the exact ledger deadline during one-time load reconciliation.
					debtPromiseQuest.SynchronizeDeadline(dueDay, isDueUnlimited);
					return;
				}
			}
			// The task deliberately has no QuestGiver so it cannot reserve a hero's vanilla issue slot or force a map marker.
			DebtPromiseQuest debtPromiseQuest2 = new DebtPromiseQuest(debtId, ownerKey, debtorName, debtSummary, deadlineText, debtNote, dueDay, isDueUnlimited);
			debtPromiseQuest2.StartQuest();
			Logger.Log("Trust", "[DebtPromiseQuest] created debtId=" + debtId + " owner=" + ownerKey);
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] Debt promise quest creation failed debtId=" + debtId + " owner=" + ownerKey + " error=" + ex.Message);
		}
	}

	private void CompleteDebtPromiseQuest(string ownerKey, string debtId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(ownerKey) || string.IsNullOrWhiteSpace(debtId) || Campaign.Current?.QuestManager == null)
			{
				return;
			}
			List<DebtPromiseQuest> list = new List<DebtPromiseQuest>();
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				DebtPromiseQuest debtPromiseQuest = quest as DebtPromiseQuest;
				if (debtPromiseQuest != null && debtPromiseQuest.IsOngoing && debtPromiseQuest.Matches(ownerKey, debtId))
				{
					list.Add(debtPromiseQuest);
				}
			}
			// Complete every duplicate defensively; only one is normally created per debt ID.
			for (int i = 0; i < list.Count; i++)
			{
				list[i].CompleteByAgreement();
			}
		}
		catch (Exception ex)
		{
			// Quest UI/save failures must never roll back a debt release that was already applied to the ledger.
			Logger.Log("Trust", "[WARN] Debt promise quest completion failed debtId=" + debtId + " owner=" + ownerKey + " error=" + ex.Message);
		}
	}
}
