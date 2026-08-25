using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

// This lightweight special quest exposes one persisted debt promise in Bannerlord's quest journal.
public sealed class DebtPromiseQuest : QuestBase
{
	// These fields duplicate only display and lookup data; RewardSystemBehavior remains the debt source of truth.
	[SaveableField(1)]
	private string _debtId;

	[SaveableField(2)]
	private string _ownerKey;

	[SaveableField(3)]
	private string _debtorName;

	[SaveableField(4)]
	private string _debtSummary;

	[SaveableField(5)]
	private string _deadlineText;

	[SaveableField(6)]
	private string _debtNote;

	// Version 7 rebuilds saved active promise journals once, removing unsafe non-hero troop hyperlinks from pre-existing entries.
	[SaveableField(7)]
	private int _journalFormatVersion;

	// Persist the ledger deadline so native quest UI can show time remaining without a separate polling loop.
	[SaveableField(8)]
	private float _dueDay;

	[SaveableField(9)]
	private bool _isDueUnlimited;

	// An expired promise remains ongoing and can still be completed by a later agreement.
	[SaveableField(10)]
	private bool _isExpired;

	private const int CurrentJournalFormatVersion = 7;

	public string DebtId => (_debtId ?? string.Empty).Trim();

	public string OwnerKey => (_ownerKey ?? string.Empty).Trim();

	public override TextObject Title
	{
		get
		{
			// Keep the counterparty as a compact list identity without exposing ledger terminology.
			TextObject text = new TextObject(_isExpired ? "{=AF_DEBT_PROMISE_EXPIRED_TITLE}承诺已过期：{DEBTOR}" : "{=AF_DEBT_PROMISE_TITLE}承诺：{DEBTOR}");
			text.SetTextVariable("DEBTOR", GetDisplayText(_debtorName, "对方"));
			return text;
		}
	}

	// Finite promises use QuestDueTime for the native countdown; unlimited and expired phases deliberately hide it.
	public override bool IsRemainingTimeHidden => _isDueUnlimited || _isExpired || _dueDay <= 0f;

	// QuestManager only preserves issue quests or special quests on load; this prevents orphaned debt entries from being cancelled.
	public override string SpecialQuestType => "animusforge_debt_promise";

	public DebtPromiseQuest(string debtId, string ownerKey, string debtorName, string debtSummary, string deadlineText, string debtNote, float dueDay, bool isDueUnlimited)
		// A null QuestGiver avoids blocking the counterparty's vanilla issue availability or adding an unwanted map tracker.
		: base(BuildQuestId(debtId), null, BuildQuestDueTime(dueDay, isDueUnlimited), 0)
	{
		_debtId = (debtId ?? string.Empty).Trim();
		_ownerKey = (ownerKey ?? string.Empty).Trim();
		_debtorName = GetDisplayText(debtorName, "对方");
		_debtSummary = GetDisplayText(debtSummary, "未说明");
		_deadlineText = GetDisplayText(deadlineText, "未设期限");
		_debtNote = GetDisplayText(debtNote, "无");
		_journalFormatVersion = CurrentJournalFormatVersion;
		_dueDay = NormalizeDueDay(dueDay, isDueUnlimited);
		_isDueUnlimited = isDueUnlimited;
		_isExpired = false;
		SetDialogs();
		InitializeQuestOnCreation();
	}

	public bool Matches(string ownerKey, string debtId)
	{
		return string.Equals(OwnerKey, (ownerKey ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
			&& string.Equals(DebtId, (debtId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
	}

	// RewardSystemBehavior invokes this only after the matching debt line has been released successfully.
	public void CompleteByAgreement()
	{
		if (!IsOngoing)
		{
			return;
		}
		// Keep the completion detail in the journal without adding a second lower-left log-update notification.
		AddLog(BuildCompletionLog(), hideInformation: true);
		CompleteQuestWithSuccess();
	}

	protected override void SetDialogs()
	{
		// This bookkeeping quest deliberately adds no dialogue, menu, or mission hooks.
	}

	protected override void InitializeQuestOnGameLoad()
	{
		// Journal logs are saved as text, so migrate legacy active entries once instead of scanning during campaign ticks.
		UpgradeJournalPresentationIfNeeded();
		// Restore a finite native countdown or its expired phase from persisted deadline data.
		RestoreDeadlineStageOnLoad();
	}

	protected override void OnStartQuest()
	{
		// The task page remains informative while avoiding the recurring notification style this feature replaces.
		AddLog(BuildStartLog(), hideInformation: true);
		// Imported or delayed task creation may begin after its deadline, so enter the next phase immediately.
		AdvanceToExpiredStageIfNeeded();
	}

	protected override void OnBeforeTimedOut(ref bool completeWithSuccess, ref bool doNotResolveTheQuest)
	{
		// Native expiry normally finalizes a quest; intercept it to keep this promise available for later completion.
		doNotResolveTheQuest = true;
		if (!_isDueUnlimited && _dueDay > 0f)
		{
			EnterExpiredStage();
			return;
		}
		// Defensive fallback prevents a malformed legacy deadline from retriggering every quest-manager hour.
		ChangeQuestDueTime(CampaignTime.Never);
	}

	// RewardSystemBehavior calls this on creation and load reconciliation; it never needs a daily task scan.
	public void SynchronizeDeadline(float dueDay, bool isDueUnlimited)
	{
		if (!IsOngoing)
		{
			return;
		}
		_dueDay = NormalizeDueDay(dueDay, isDueUnlimited);
		_isDueUnlimited = isDueUnlimited;
		if (_isExpired || _isDueUnlimited || _dueDay <= 0f)
		{
			ChangeQuestDueTime(CampaignTime.Never);
			return;
		}
		if (AdvanceToExpiredStageIfNeeded())
		{
			return;
		}
		ChangeQuestDueTime(BuildQuestDueTime(_dueDay, isDueUnlimited: false));
	}

	private TextObject BuildStartLog()
	{
		// Keep the original remark concise, then append the fixed completion condition with known participant tokens clickable.
		Hero counterparty = ResolveCounterpartyHero();
		Hero player = ResolvePlayerHero();
		bool hasCounterpartyLink = counterparty?.CharacterObject != null;
		bool hasPlayerLink = player?.CharacterObject != null;
		string note = GetDisplayText(_debtNote, "无");
		// This is the sole task-completion instruction: the agreement must be released through a conversation with the counterpart.
		note += "\n\n与NPC聊天完成或解除该承诺即可结束此任务。";
		if (hasCounterpartyLink)
		{
			note = note.Replace("NPC", "{NPC.LINK}");
		}
		else
		{
			// Merchant promises have no individual hero target, so retain a truthful non-clickable counterpart name.
			note = note.Replace("NPC", GetDisplayText(_debtorName, "NPC"));
		}
		if (hasPlayerLink)
		{
			note = note.Replace("玩家", "{PLAYER.LINK}");
		}
		else
		{
			note = note.Replace("玩家", GetDisplayText(player?.Name?.ToString(), "玩家"));
		}
		// Entity matching runs only while a task log is created or migrated, never in a campaign tick.
		List<KeyValuePair<string, TextObject>> entityLinks = ReplaceKnownEntityNamesWithLinkTags(ref note);
		TextObject text = new TextObject("{=AF_DEBT_PROMISE_START}" + note);
		if (hasCounterpartyLink)
		{
			StringHelpers.SetCharacterProperties("NPC", counterparty.CharacterObject, text, includeDetails: false);
		}
		if (hasPlayerLink)
		{
			StringHelpers.SetCharacterProperties("PLAYER", player.CharacterObject, text, includeDetails: false);
		}
		for (int index = 0; index < entityLinks.Count; index++)
		{
			text.SetTextVariable(entityLinks[index].Key, entityLinks[index].Value);
		}
		return text;
	}

	private TextObject BuildCompletionLog()
	{
		// Completed tasks retain a neutral outcome line and never reintroduce the removed debt label.
		return new TextObject("{=AF_DEBT_PROMISE_COMPLETE}此承诺已完成或解除。");
	}

	private TextObject BuildExpiredLog()
	{
		// Expiry is a visible next stage rather than a task failure, so the agreement may still be fulfilled later.
		return new TextObject("{=AF_DEBT_PROMISE_EXPIRED}承诺已过期，但仍可在之后解除或完成。");
	}

	private void UpgradeJournalPresentationIfNeeded()
	{
		if (!IsOngoing || _journalFormatVersion >= CurrentJournalFormatVersion)
		{
			return;
		}

		// Remove persisted legacy lines backwards, then rebuild link-aware active stages without a UI notification.
		for (int index = JournalEntries.Count - 1; index >= 0; index--)
		{
			RemoveLog(JournalEntries[index]);
		}

		AddLog(BuildStartLog(), hideInformation: true);
		if (_isExpired)
		{
			// Preserve the active expired stage while upgrading only its earlier note presentation.
			AddLog(BuildExpiredLog(), hideInformation: true);
		}
		_journalFormatVersion = CurrentJournalFormatVersion;
	}

	private void RestoreDeadlineStageOnLoad()
	{
		if (_isExpired || _isDueUnlimited || _dueDay <= 0f)
		{
			ChangeQuestDueTime(CampaignTime.Never);
			return;
		}
		if (AdvanceToExpiredStageIfNeeded())
		{
			return;
		}
		ChangeQuestDueTime(BuildQuestDueTime(_dueDay, isDueUnlimited: false));
	}

	private bool AdvanceToExpiredStageIfNeeded()
	{
		if (_isExpired || _isDueUnlimited || _dueDay <= 0f || !IsDeadlineReached())
		{
			return false;
		}
		EnterExpiredStage();
		return true;
	}

	private void EnterExpiredStage()
	{
		if (_isExpired)
		{
			ChangeQuestDueTime(CampaignTime.Never);
			return;
		}
		// Stop the native timeout loop before appending the one-time stage log without a lower-left notification.
		_isExpired = true;
		ChangeQuestDueTime(CampaignTime.Never);
		AddLog(BuildExpiredLog(), hideInformation: true);
	}

	private bool IsDeadlineReached()
	{
		if (_isDueUnlimited || _dueDay <= 0f)
		{
			return false;
		}
		try
		{
			return CampaignTime.Now.ToDays >= _dueDay;
		}
		catch
		{
			return false;
		}
	}

	private static CampaignTime BuildQuestDueTime(float dueDay, bool isDueUnlimited)
	{
		if (isDueUnlimited || dueDay <= 0f)
		{
			return CampaignTime.Never;
		}
		try
		{
			// Reconstruct the absolute ledger day as a native relative due time, preserving the existing deadline.
			float remainingDays = Math.Max(0f, dueDay - (float)CampaignTime.Now.ToDays);
			return CampaignTime.DaysFromNow(remainingDays);
		}
		catch
		{
			return CampaignTime.Never;
		}
	}

	private static float NormalizeDueDay(float dueDay, bool isDueUnlimited)
	{
		// A nonpositive finite day is legacy/malformed data and must not create an immediate recurring timeout.
		return isDueUnlimited || dueDay <= 0f ? 0f : dueDay;
	}

	private Hero ResolveCounterpartyHero()
	{
		string ownerKey = OwnerKey;
		if (string.IsNullOrWhiteSpace(ownerKey))
		{
			return null;
		}
		try
		{
			// Hero debt keys use the hero StringId; non-hero merchant keys intentionally resolve to no link.
			return Hero.Find(ownerKey);
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolvePlayerHero()
	{
		try
		{
			// MainHero supplies the native encyclopedia-link character properties for the 玩家 token.
			return Hero.MainHero;
		}
		catch
		{
			return null;
		}
	}

	private static List<KeyValuePair<string, TextObject>> ReplaceKnownEntityNamesWithLinkTags(ref string note)
	{
		List<KeyValuePair<string, TextObject>> replacements = new List<KeyValuePair<string, TextObject>>();
		if (string.IsNullOrWhiteSpace(note))
		{
			return replacements;
		}
		List<KeyValuePair<string, TextObject>> candidates = new List<KeyValuePair<string, TextObject>>();
		HashSet<string> seenNames = new HashSet<string>(StringComparer.Ordinal);
		// These small one-off lists are deliberately not cached: names and encyclopedia entries can change during a campaign.
		// They run only when this quest is created or migrated, never on a campaign tick or while rendering the quest list.
		AddSettlementLinkCandidates(note, candidates, seenNames);
		AddClanLinkCandidates(note, candidates, seenNames);
		AddKingdomLinkCandidates(note, candidates, seenNames);
		// Troop templates deliberately stay plain because opening their native encyclopedia pages can crash the game.

		// Replace longer names first so a nested entity label cannot consume part of a more specific one.
		candidates.Sort((left, right) => right.Key.Length.CompareTo(left.Key.Length));
		for (int index = 0; index < candidates.Count; index++)
		{
			KeyValuePair<string, TextObject> candidate = candidates[index];
			if (note.IndexOf(candidate.Key, StringComparison.Ordinal) < 0)
			{
				continue;
			}
			string tag = "ENTITY_" + replacements.Count;
			note = note.Replace(candidate.Key, "{" + tag + "}");
			replacements.Add(new KeyValuePair<string, TextObject>(tag, candidate.Value));
		}
		return replacements;
	}

	private static void AddSettlementLinkCandidates(string note, List<KeyValuePair<string, TextObject>> candidates, HashSet<string> seenNames)
	{
		try
		{
			for (int index = 0; index < Settlement.All.Count; index++)
			{
				Settlement settlement = Settlement.All[index];
				AddEntityLinkCandidate(note, settlement?.Name?.ToString(), settlement?.EncyclopediaLinkWithName, candidates, seenNames);
			}
		}
		catch
		{
			// A malformed or not-yet-ready settlement list must not prevent the promise task from being shown.
		}
	}

	private static void AddClanLinkCandidates(string note, List<KeyValuePair<string, TextObject>> candidates, HashSet<string> seenNames)
	{
		try
		{
			for (int index = 0; index < Clan.All.Count; index++)
			{
				Clan clan = Clan.All[index];
				AddEntityLinkCandidate(note, clan?.Name?.ToString(), clan?.EncyclopediaLinkWithName, candidates, seenNames);
			}
		}
		catch
		{
			// Keep the note readable if a campaign object is unavailable during an unusual save/load transition.
		}
	}

	private static void AddKingdomLinkCandidates(string note, List<KeyValuePair<string, TextObject>> candidates, HashSet<string> seenNames)
	{
		try
		{
			for (int index = 0; index < Kingdom.All.Count; index++)
			{
				Kingdom kingdom = Kingdom.All[index];
				TextObject link = kingdom?.EncyclopediaLinkWithName;
				AddEntityLinkCandidate(note, kingdom?.Name?.ToString(), link, candidates, seenNames);
				// Kingdom notes may use either the formal or informal name; both target the same native encyclopedia entry.
				AddEntityLinkCandidate(note, kingdom?.InformalName?.ToString(), link, candidates, seenNames);
			}
		}
		catch
		{
			// Keep the task journal usable even if a kingdom object cannot build its encyclopedia label.
		}
	}

	private static void AddEntityLinkCandidate(string note, string rawName, TextObject link, List<KeyValuePair<string, TextObject>> candidates, HashSet<string> seenNames)
	{
		string name = (rawName ?? string.Empty).Trim();
		// One-character Chinese labels are too ambiguous; exact multi-character matches avoid prose being linked by accident.
		if (name.Length < 2 || link == null || note.IndexOf(name, StringComparison.Ordinal) < 0 || !seenNames.Add(name))
		{
			return;
		}
		candidates.Add(new KeyValuePair<string, TextObject>(name, link));
	}

	private static string BuildQuestId(string debtId)
	{
		// Debt IDs are generated as compact alphanumeric tokens, making the quest ID stable across save/load.
		string normalizedDebtId = (debtId ?? string.Empty).Trim();
		return "animusforge_debt_promise_" + (string.IsNullOrWhiteSpace(normalizedDebtId) ? "unknown" : normalizedDebtId.ToLowerInvariant());
	}

	private static string GetDisplayText(string value, string fallback)
	{
		string text = (value ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = fallback;
		}
		// Protect TextObject parsing from braces supplied by an LLM note or imported save data.
		return text.Replace("{", "(").Replace("}", ")");
	}
}

// Registers the custom QuestBase subtype so its fields can round-trip through Bannerlord save files.
public sealed class DebtPromiseQuestSaveableTypeDefiner : SaveableTypeDefiner
{
	public DebtPromiseQuestSaveableTypeDefiner()
		: base(711110)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(DebtPromiseQuest), 1);
	}
}
