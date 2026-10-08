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

	private bool DeleteDialogueHistoryLine(string memoryId, int gameDayIndex, int lineOrdinal, string expectedText, out string status) => _memoryHistoryCommit.DeleteDialogueHistoryLine(memoryId, gameDayIndex, lineOrdinal, expectedText, out status);

	private static bool DisplayTextMatches(string rawLine, string expectedText) => MemoryHistoryCommitBannerlordAdapter.DisplayTextMatches(rawLine, expectedText);

	// Removes one draft line with the same normalized text (the same key the draft-edit sync uses),
	// then reuses that sync so the native conversation session cache drops the entry too.
	private bool RemoveMatchingDraftLine(string memoryId, int gameDayIndex, string syncKey) => _memoryHistoryCommit.RemoveMatchingDraftLine(memoryId, gameDayIndex, syncKey);
}
