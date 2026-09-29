using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// Player-initiated deletion of one persisted dialogue history line (DialogueUI history panel).
// Runs only on an explicit click, never on a tick: one read-modify-write of one NPC's history
// and, when the line is still uncompressed, the same day's memory draft. Compressed memory
// blocks are intentionally left untouched; the shared history store keeps all three channels
// (courier, free conversation, scene shout) consistent because they read the same records.
public partial class MyBehavior
{
	public static bool DeleteDialogueHistoryLineForExternal(string memoryId, int gameDayIndex, int lineOrdinal, string expectedText, out string status)
	{
		status = "";
		if (!TWParallel.IsMainThread())
		{
			status = "只能在游戏主线程删除记录。";
			return false;
		}
		MyBehavior owner = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
		if (owner == null)
		{
			status = "当前没有可用的记录存储。";
			return false;
		}
		try
		{
			return owner.DeleteDialogueHistoryLine(memoryId, gameDayIndex, lineOrdinal, expectedText, out status);
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[ERROR] delete line failed memoryId=" + (memoryId ?? "") + " day=" + gameDayIndex + " ordinal=" + lineOrdinal + " error=" + ex.GetType().Name + ": " + ex.Message);
			status = "删除失败，记录未改动。";
			return false;
		}
	}

	private bool DeleteDialogueHistoryLine(string memoryId, int gameDayIndex, int lineOrdinal, string expectedText, out string status)
	{
		string id = NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(id) || gameDayIndex < 0 || lineOrdinal < 0)
		{
			status = "这条记录不能删除。";
			return false;
		}
		List<DialogueDay> records = LoadDialogueHistoryById(id);
		DialogueDay day = records?.FirstOrDefault(x => x != null && x.GameDayIndex == gameDayIndex);
		// The ordinal comes from the panel's snapshot; re-check the text so a stale list can never delete a different line.
		if (day?.Lines == null || lineOrdinal >= day.Lines.Count || !DisplayTextMatches(day.Lines[lineOrdinal], expectedText))
		{
			status = "记录已变化，请重新打开历史后再删除。";
			return false;
		}
		string rawLine = day.Lines[lineOrdinal];
		string syncKey = NormalizeDialogueHistoryLineForDailyMemorySync(rawLine);
		day.Lines.RemoveAt(lineOrdinal);
		if (day.Lines.Count == 0 && (day.MemoryCommitMarkers == null || day.MemoryCommitMarkers.Count == 0))
		{
			records.Remove(day);
		}
		SaveDialogueHistoryById(id, records);
		bool draftRemoved = RemoveMatchingDraftLine(id, gameDayIndex, syncKey);
		Logger.Log("DialogueHistory", "deleted line memoryId=" + id + " day=" + gameDayIndex + " ordinal=" + lineOrdinal + " draftRemoved=" + draftRemoved);
		status = draftRemoved ? "已删除该条记录。" : "已删除该条记录（该日记忆已压缩，压缩内容不受影响）。";
		return true;
	}

	private static bool DisplayTextMatches(string rawLine, string expectedText)
	{
		string line = (rawLine ?? "").Trim();
		DialogueHistoryLedger.TryStripSceneSessionMarker(line, out line, out var _);
		ClassifyDialogueHistoryLine(line, out var _, out var text, out var _);
		return string.Equals((text ?? "").Trim(), (expectedText ?? "").Trim(), StringComparison.Ordinal);
	}

	// Removes one draft line with the same normalized text (the same key the draft-edit sync uses),
	// then reuses that sync so the native conversation session cache drops the entry too.
	private bool RemoveMatchingDraftLine(string memoryId, int gameDayIndex, string syncKey)
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
		DailyMemoryLine removed = draft.Lines[index];
		draft.Lines.RemoveAt(index);
		// Sanitize keeps a stale HasLlmDialogue; recompute from what is left.
		draft.HasLlmDialogue = draft.Lines.Any(x => x != null && x.IsLlmDialogue && !x.IsAfef && !string.IsNullOrWhiteSpace(x.Text));
		SaveDailyMemoryDraftsById(memoryId, drafts);
		if (FindDevDailyMemoryDraft(LoadDailyMemoryDraftsById(memoryId), gameDayIndex) == null)
		{
			_memorySummaryQueue?.RemoveAll(x => x != null && x.GameDayIndex == gameDayIndex
				&& string.Equals(NormalizeMemoryHeroId(x.HeroId), memoryId, StringComparison.OrdinalIgnoreCase));
		}
		// Same lookup as the host's other memory-id resolution: indexed Find first, full scan only on a case mismatch.
		Hero hero = IsNonHeroMemoryId(memoryId) ? null
			: Hero.Find(memoryId) ?? Hero.FindFirst(x => x != null && string.Equals(GetMemoryHeroId(x), memoryId, StringComparison.OrdinalIgnoreCase));
		if (hero != null)
		{
			ShoutBehavior.SyncNativeConversationSessionHistoryForDailyMemoryEditExternal(hero, hero.CharacterObject, hero.Name?.ToString(), gameDayIndex,
				BuildNativeConversationHistoryEntriesForDailyMemoryEdit(hero, new[] { removed }),
				new List<AnimusForgeDialogueHistoryEntry>(), "dialogueui_delete");
		}
		return true;
	}
}
