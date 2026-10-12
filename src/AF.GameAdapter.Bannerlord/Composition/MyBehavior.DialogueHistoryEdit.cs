using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// Player-initiated rewrite of one persisted dialogue history line (DialogueUI history panel).
// Same contract as the delete path: click-driven only, one read-modify-write of one NPC's history
// plus the same day's uncompressed memory draft line. The speaker/AFEF prefix and scene-session
// marker are preserved so only the spoken text changes; compressed memory blocks stay untouched.
public partial class MyBehavior
{
	// On success the entry's Text/Speaker/Kind are updated to what the panel would read back.
	public static bool EditDialogueHistoryLineForExternal(AnimusForgeDialogueHistoryEntry entry, string newText, out string status)
	{
		status = "";
		if (entry == null)
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditUnavailable", "This record cannot be edited.");
			return false;
		}
		if (!TWParallel.IsMainThread())
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditMainThread", "Records can only be edited on the game main thread.");
			return false;
		}
		MyBehavior owner = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
		if (owner == null)
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryStoreUnavailable", "No record storage is currently available.");
			return false;
		}
		try
		{
			return owner.EditDialogueHistoryLine(entry, newText, out status);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[ERROR] edit line failed memoryId=" + (entry.MemoryId ?? "") + " day=" + entry.GameDayIndex + " ordinal=" + entry.LineOrdinal + " error=" + ex.GetType().Name + ": " + ex.Message);
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditFailed", "Editing failed; the record was not changed.");
			return false;
		}
	}

	private bool EditDialogueHistoryLine(AnimusForgeDialogueHistoryEntry entry, string newText, out string status)
	{
		string id = NormalizeMemoryHeroId(entry.MemoryId);
		int gameDayIndex = entry.GameDayIndex;
		int lineOrdinal = entry.LineOrdinal;
		string text = (newText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) || gameDayIndex < 0 || lineOrdinal < 0)
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditUnavailable", "This record cannot be edited.");
			return false;
		}
		if (text.Length == 0)
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditEmpty", "Content cannot be empty; use Delete to remove the record.");
			return false;
		}
		List<DialogueDay> records = LoadDialogueHistoryById(id);
		DialogueDay day = records?.FirstOrDefault(x => x != null && x.GameDayIndex == gameDayIndex);
		// The ordinal comes from the panel's snapshot; re-check the text so a stale list can never rewrite a different line.
		if (day?.Lines == null || lineOrdinal >= day.Lines.Count || !DisplayTextMatches(day.Lines[lineOrdinal], entry.Text))
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditChanged", "The record changed. Reopen the history before editing it.");
			return false;
		}
		string rawLine = day.Lines[lineOrdinal];
		bool tagged = DialogueHistoryLedger.TryStripSceneSessionMarker(rawLine, out string line, out int sceneSessionId);
		line = (line ?? "").Trim();
		ClassifyDialogueHistoryLine(line, out var _, out var oldText, out var _);
		oldText = (oldText ?? "").Trim();
		// The displayed text is always the trimmed tail of the line; everything before it is the speaker/AFEF prefix.
		if (!line.EndsWith(oldText, StringComparison.Ordinal))
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditFormat", "This record's format cannot be edited safely.");
			return false;
		}
		string newLine = line.Substring(0, line.Length - oldText.Length) + text;
		if (string.Equals(newLine, line, StringComparison.Ordinal))
		{
			status = MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditUnchanged", "The content is unchanged.");
			return false;
		}
		string syncKey = NormalizeDialogueHistoryLineForDailyMemorySync(rawLine);
		day.Lines[lineOrdinal] = tagged ? DialogueHistoryLedger.TagSceneSession(newLine, sceneSessionId) : newLine;
		SaveDialogueHistoryById(id, records);
		bool draftUpdated = UpdateMatchingDraftLine(id, gameDayIndex, syncKey, newLine);
		ClassifyDialogueHistoryLine(newLine, out var speaker, out var displayText, out var kind);
		entry.Speaker = speaker;
		entry.Text = displayText;
		entry.Kind = kind;
		Logger.Log("DialogueHistory", "edited line memoryId=" + id + " day=" + gameDayIndex + " ordinal=" + lineOrdinal + " draftUpdated=" + draftUpdated);
		status = draftUpdated ? MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditDone", "The record was edited.") : MemoryEditorUiText.T("AF_MEMORY_UI_HistoryEditDoneCompressed", "The record was edited. That day was already compressed; compressed memory is unchanged.");
		return true;
	}

	// Rewrites one draft line with the same normalized text (the key the delete path also uses),
	// then lets the native conversation session cache swap the old entry for the new one.
	private bool UpdateMatchingDraftLine(string memoryId, int gameDayIndex, string syncKey, string newLine)
	{
		if (string.IsNullOrWhiteSpace(syncKey))
		{
			return false;
		}
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDraftsById(memoryId);
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(drafts, gameDayIndex);
		int index = draft?.Lines == null ? -1 : draft.Lines.FindIndex(x => x != null
			&& string.Equals(NormalizeDialogueHistoryLineForDailyMemorySync(x.Text), syncKey, StringComparison.Ordinal));
		if (index < 0)
		{
			return false;
		}
		DailyMemoryLine target = draft.Lines[index];
		List<DailyMemoryLine> previous = CloneDevDailyMemoryLines(new[] { target });
		target.Text = newLine;
		SaveDailyMemoryDraftsById(memoryId, drafts);
		Hero hero = ResolveDialogueHistoryEditHero(memoryId);
		if (hero != null)
		{
			ShoutBehavior.SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(hero, hero.CharacterObject, hero.Name?.ToString(), gameDayIndex,
				BuildNativeConversationHistoryEntriesForDailyMemoryEdit(hero, previous),
				BuildNativeConversationHistoryEntriesForDailyMemoryEdit(hero, new[] { target }), "dialogueui_edit", completeDaySnapshot: false);
		}
		return true;
	}

	// Same lookup as the host's other memory-id resolution: indexed Find first, full scan only on a case mismatch.
	private static Hero ResolveDialogueHistoryEditHero(string memoryId) => CampaignCharacterRecordCaptureAdapter.ResolveMemoryHero(memoryId);
}
