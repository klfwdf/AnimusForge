using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using TaleWorlds.Library;

using TaleWorlds.Localization;



namespace AnimusForge;



internal sealed class MemoryEditorDisplayPort

{

	internal Func<DailyMemoryLine, string> BuildDailyMemoryLineForPrompt;

	internal Func<IEnumerable<WeeklyMemoryMaterialTrigger>, List<WeeklyMemoryMaterialTrigger>> SanitizeWeeklyMemoryMaterialTriggers;

	internal Func<string, string> BuildWeeklyMemoryMaterialTagLabel;

	internal Func<string, int, string> BuildCompressedMemoryBlockId;

	internal Func<int, int, string> FormatMemoryHourRange;

	internal Func<string, int, string> BuildDevHistoryPreview;

	internal Func<int> GetCurrentGameDayIndexSafe, GetCurrentHourOfDaySafeForPrompt;

	internal Func<string> ResolveCurrentMemorySceneLabel;

}



internal static class MemoryEditorProjection

{

	internal static string HourRangeForDisplay(int startHour, int endHour)

	{

		startHour = Math.Max(0, Math.Min(23, startHour));

		endHour = Math.Max(0, Math.Min(23, endHour));

		return startHour == endHour

			? MemoryEditorUiText.F("AF_MEMORY_UI_HourSingle", "{HOUR}h", "HOUR", startHour.ToString())

			: MemoryEditorUiText.F("AF_MEMORY_UI_HourRange", "{START}-{END}h", "START", startHour.ToString(), "END", endHour.ToString());

	}



	internal static string SpeakerForDisplay(DailyMemoryLine line)

	{

		string speaker = (line.Speaker ?? "").Trim();

		if (speaker.Length == 0) return line.IsAfef ? "AFEF" : new TextObject("{=AF_MEMORY_UI_Manual}Manual").ToString();

		return string.Equals(speaker, MemoryTextResources.Raw("MEM_Raw_007"), StringComparison.OrdinalIgnoreCase)

			? new TextObject("{=AF_MEMORY_UI_Manual}Manual").ToString() : speaker;

	}



	internal static string WeeklyMaterialTagForDisplay(MemoryEditorDisplayPort port, string tag)

	{

		string text = (tag ?? "").Trim();

		switch (text.ToUpperInvariant())

		{

			case "[WEEKLY:ECONOMY_GIVE_GOLD]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagConfirmedGold", "Confirmed gold payment");

			case "[WEEKLY:ECONOMY_GIVE_ASSET]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagConfirmedAsset", "Confirmed asset transfer");

			case "[WEEKLY:ECONOMY_DEBT_CREATE]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagConfirmedDebtCreate", "Confirmed debt creation");

			case "[WEEKLY:ECONOMY_DEBT_RESOLVE]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagConfirmedDebtResolve", "Confirmed debt repayment");

			case "[WEEKLY:ECONOMY_SETTLEMENT_TRANSFER]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagConfirmedSettlement", "Confirmed settlement asset transfer");

			case "[ACTION:DUEL]": return MemoryEditorUiText.T("AF_MEMORY_UI_TagDuel", "Duel");

		}

		if (text.StartsWith("[ACTION:GIVE_ASSET:GOLD:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagGold", "Gold payment");

		if (text.StartsWith("[AD:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagDebtCreate", "New debt");

		if (text.StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagDebtResolve", "Debt repayment");

		if (text.StartsWith("[ATT:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagTroops", "Troop transfer");

		if (text.StartsWith("[ATP:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagPrisoners", "Prisoner transfer");

		if (text.StartsWith("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagAsset", "Asset transfer");

		if (text.StartsWith("[ACTION:KINGDOM_SERVICE:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagService", "Kingdom service change");

		if (text.StartsWith("[ACTION:MARRIAGE_FORMAL:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagMarriage", "Formal marriage");

		if (text.StartsWith("[ACTION:MARRIAGE_ELOPE:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagElopement", "Marriage by elopement");

		if (text.StartsWith("[ACTION:DIVORCE:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagDivorce", "Divorce");

		if (text.StartsWith("[ACTION:AGENDA:", StringComparison.OrdinalIgnoreCase)) return MemoryEditorUiText.T("AF_MEMORY_UI_TagAgenda", "Kingdom agenda commitment");

		if (text.StartsWith("[ACTION:SIEGE_", StringComparison.OrdinalIgnoreCase)

			&& !string.IsNullOrWhiteSpace(port.BuildWeeklyMemoryMaterialTagLabel(text)))

			return MemoryEditorUiText.F("AF_MEMORY_UI_TagSiege", "Siege aftermath: {ACTION}", "ACTION", text.Substring("[ACTION:SIEGE_".Length).TrimEnd(']'));

		return "";

	}



	private static string FormatDay(string gameDate, int gameDayIndex)

	{

		if (!string.IsNullOrWhiteSpace(gameDate))

		{

			return gameDate.Trim();

		}

		var text = new TextObject("{=AF_MEMORY_UI_Day}Day {DAY}");

		text.SetTextVariable("DAY", gameDayIndex.ToString());

		return text.ToString();

	}



	internal static string BuildDevHistoryPreview(string line, int maxLen = 56)

	{

		if (string.IsNullOrWhiteSpace(line))

		{

			return new TextObject("{=AF_MEMORY_UI_Empty}(empty)").ToString();

		}

		StringBuilder stringBuilder = new StringBuilder(line.Length);

		bool flag = false;

		foreach (char c in line)

		{

			if (char.IsWhiteSpace(c))

			{

				if (!flag)

				{

					stringBuilder.Append(' ');

					flag = true;

				}

			}

			else

			{

				stringBuilder.Append(c);

				flag = false;

			}

		}

		string text = stringBuilder.ToString().Trim();

		if (text.Length <= maxLen)

		{

			return text;

		}

		return text.Substring(0, Math.Max(1, maxLen)) + "...";

	}



	internal static bool IsDevDailyMemoryDraftMatch(MemoryEditorDisplayPort port, DailyMemoryDraft draft, string[] terms)

	{

		if (draft == null)

		{

			return false;

		}

		if (terms == null || terms.Length <= 0)

		{

			return true;

		}

		string haystack = BuildDevDailyMemoryDraftSearchText(port, draft).ToLowerInvariant();

		foreach (string term in terms)

		{

			string text = (term ?? "").Trim().ToLowerInvariant();

			if (!string.IsNullOrWhiteSpace(text) && !haystack.Contains(text))

			{

				return false;

			}

		}

		return true;

	}



	internal static string BuildDevDailyMemoryDraftSearchText(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{

		StringBuilder sb = new StringBuilder();

		if (draft == null)

		{

			return "";

		}

		sb.AppendLine(draft.HeroName ?? "");

		sb.AppendLine(draft.GameDate ?? "");

		sb.AppendLine(draft.GameDayIndex.ToString());

		foreach (DailyMemoryLine line in draft.Lines ?? new List<DailyMemoryLine>())

		{

			sb.AppendLine(port.BuildDailyMemoryLineForPrompt(line));

		}

		sb.AppendLine(BuildDevWeeklyMemoryMaterialTriggerTextCore(port, draft.WeeklyMaterialTriggers, forDisplay: false));

		return sb.ToString();

	}



	internal static string BuildDevDailyMemoryDraftListLabel(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{

		if (draft == null)

		{

			return new TextObject("{=AF_MEMORY_UI_Empty}(empty)").ToString();

		}

		string date = FormatDay(draft.GameDate, draft.GameDayIndex);

		string queuedFlag = draft.QueuedForSummary ? (" " + new TextObject("{=AF_MEMORY_UI_FlagQueued}Queued").ToString()) : "";

		string errorFlag = string.IsNullOrWhiteSpace(draft.LastSummaryError) ? "" : (" " + new TextObject("{=AF_MEMORY_UI_FlagHasError}Has error").ToString());

		string weeklyFlag = "";

		if ((draft.WeeklyMaterialTriggers?.Count).GetValueOrDefault() > 0)

		{

			var triggerFlagText = new TextObject("{=AF_MEMORY_UI_FlagWeeklyMaterialCount}Weekly material x{COUNT}");

			triggerFlagText.SetTextVariable("COUNT", draft.WeeklyMaterialTriggers.Count.ToString());

			weeklyFlag = " " + triggerFlagText.ToString();

		}

		string flags = queuedFlag + errorFlag + weeklyFlag;

		return date + " | " + ((draft.Lines?.Count).GetValueOrDefault()) + new TextObject("{=AF_MEMORY_UI_Lines} lines").ToString() + flags + " | " + port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 64);

	}



	internal static string BuildDevDailyMemoryDraftSubtitle(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{

		if (draft == null)

		{

			return "";

		}

		string date = FormatDay(draft.GameDate, draft.GameDayIndex);

		string queuedText = draft.QueuedForSummary ? new TextObject("{=AF_MEMORY_UI_Yes}yes").ToString() : new TextObject("{=AF_MEMORY_UI_No}no").ToString();

		return new TextObject("{=AF_MEMORY_UI_DateLabel}Date: ").ToString() + date + "\n" +

			new TextObject("{=AF_MEMORY_UI_LineCount}Lines: ").ToString() + ((draft.Lines?.Count).GetValueOrDefault()) + "\n" +

			new TextObject("{=AF_MEMORY_UI_WeeklyMaterialTriggersLabel}Weekly material triggers: ").ToString() + ((draft.WeeklyMaterialTriggers?.Count).GetValueOrDefault()) + "\n" +

			new TextObject("{=AF_MEMORY_UI_QueuedForSummaryLabel}Queued for summary: ").ToString() + queuedText + "\n" +

			new TextObject("{=AF_MEMORY_UI_LatestSummaryErrorLabel}Latest summary error: ").ToString() + BuildDevStoredErrorReference(port, draft.LastSummaryError);

	}



	internal static string BuildDevStoredErrorReference(MemoryEditorDisplayPort port, string error)

	{

		// 开发菜单会在返回和翻页时反复重建；这里只放固定短提示，避免历史 API 原文再次挤占操作项。

		return string.IsNullOrWhiteSpace(error)

			? new TextObject("{=AF_MEMORY_UI_None}None").ToString()

			: new TextObject("{=AF_MEMORY_UI_ErrorRecordedNotice}Recorded (details shown in bottom-left when triggered)").ToString();

	}



	internal static string BuildDevDailyMemoryDraftEditorDescription(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{

		if (draft == null)

		{

			return new TextObject("{=AF_MEMORY_UI_RawMemoryNotFound}Uncompressed memory could not be found.").ToString();

		}

		StringBuilder sb = new StringBuilder();

		sb.AppendLine(BuildDevDailyMemoryDraftSubtitle(port, draft));

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_NormalLinesLabel}Normal lines: ").ToString() + (draft.Lines ?? new List<DailyMemoryLine>()).Count((DailyMemoryLine x) => x != null && !x.IsAfef));

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_AfefLinesLabel}AFEF lines: ").ToString() + (draft.Lines ?? new List<DailyMemoryLine>()).Count((DailyMemoryLine x) => x != null && x.IsAfef));

		string triggerText = BuildDevWeeklyMemoryMaterialTriggerText(port, draft.WeeklyMaterialTriggers);

		if (!string.IsNullOrWhiteSpace(triggerText))

		{

			sb.AppendLine(new TextObject("{=AF_MEMORY_UI_WeeklyMaterialLabel}Weekly material: ").ToString().TrimEnd());

			sb.AppendLine(triggerText);

		}

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_PreviewLabel}Preview: ").ToString() + port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 180));

		return sb.ToString().TrimEnd();

	}



	internal static string BuildDevWeeklyMemoryMaterialTriggerText(MemoryEditorDisplayPort port, IEnumerable<WeeklyMemoryMaterialTrigger> triggers)

		=> BuildDevWeeklyMemoryMaterialTriggerTextCore(port, triggers, forDisplay: true);



	private static string BuildDevWeeklyMemoryMaterialTriggerTextCore(MemoryEditorDisplayPort port, IEnumerable<WeeklyMemoryMaterialTrigger> triggers, bool forDisplay)

	{

		List<WeeklyMemoryMaterialTrigger> list = port.SanitizeWeeklyMemoryMaterialTriggers(triggers);

		if (list.Count == 0)

		{

			return "";

		}

		string listSeparator = new TextObject("{=AF_MEMORY_UI_ListSeparator}, ").ToString();

		StringBuilder sb = new StringBuilder();

		Func<string, string> tagLabel = forDisplay ? tag => WeeklyMaterialTagForDisplay(port, tag) : port.BuildWeeklyMemoryMaterialTagLabel;

		foreach (WeeklyMemoryMaterialTrigger trigger in list.Take(8))

		{

			List<string> labels = (trigger.Tags ?? new List<string>()).Select(tagLabel).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

			sb.Append(new TextObject("{=AF_MEMORY_UI_TriggerKingdomItem}- kingdom=").ToString()).Append(trigger.FootholdKingdomId ?? "")

				.Append(" session=").Append(trigger.SceneSessionId >= 0 ? ("scene:" + trigger.SceneSessionId) : (trigger.DialogueSessionId >= 0 ? ("dialogue:" + trigger.DialogueSessionId) : "none"))

				.Append(" value=").Append(Math.Max(0L, trigger.EstimatedValueDenars))

				.Append(" tags=").Append(labels.Count > 0 ? string.Join(listSeparator, labels) : string.Join(listSeparator, trigger.Tags ?? new List<string>()));

			if (!string.IsNullOrWhiteSpace(trigger.TriggerReason))

			{

				sb.Append(" reason=").Append(trigger.TriggerReason.Trim());

			}

			sb.AppendLine();

		}

		if (list.Count > 8)

		{

			var moreText = new TextObject("{=AF_MEMORY_UI_TriggerMoreCount}- ...and {COUNT} more");

			moreText.SetTextVariable("COUNT", (list.Count - 8).ToString());

			sb.AppendLine(moreText.ToString());

		}

		return sb.ToString().TrimEnd();

	}



	internal static string BuildDevDailyMemoryLineListLabel(MemoryEditorDisplayPort port, DailyMemoryLine line)

	{

		if (line == null)

		{

			return new TextObject("{=AF_MEMORY_UI_Empty}(empty)").ToString();

		}

		string type = line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? "LLM" : new TextObject("{=AF_MEMORY_UI_Normal}Normal").ToString());

		string speaker = SpeakerForDisplay(line);

		return MBMath.ClampInt(line.GameHour, 0, 23) + new TextObject("{=AF_MEMORY_UI_HourSeparator}h | ").ToString() + type + " | " + speaker + " | " + port.BuildDevHistoryPreview(line.Text, 90);

	}



	internal static string BuildDevDailyMemoryLineSubtitle(MemoryEditorDisplayPort port, DailyMemoryLine line)

	{

		if (line == null)

		{

			return "";

		}

		string date = FormatDay(line.GameDate, line.GameDayIndex);

		string scene = string.IsNullOrWhiteSpace(line.Scene) ? new TextObject("{=AF_MEMORY_UI_UnknownScene}Unknown scene").ToString() : line.Scene.Trim();

		string speaker = SpeakerForDisplay(line);

		string type = line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? new TextObject("{=AF_MEMORY_UI_LlmDialogue}LLM dialogue").ToString() : new TextObject("{=AF_MEMORY_UI_Normal}Normal").ToString());

		return new TextObject("{=AF_MEMORY_UI_DateLabel}Date: ").ToString() + date + " " + MBMath.ClampInt(line.GameHour, 0, 23) + new TextObject("{=AF_MEMORY_UI_HourSuffix}h").ToString() + "\n" +

			new TextObject("{=AF_MEMORY_UI_SceneLabel}Scene: ").ToString() + scene + "\n" +

			new TextObject("{=AF_MEMORY_UI_SpeakerLabel}Speaker: ").ToString() + speaker + "\n" +

			new TextObject("{=AF_MEMORY_UI_TypeLabel}Type: ").ToString() + type;

	}



	internal static string BuildDevDailyMemoryLineDescription(MemoryEditorDisplayPort port, DailyMemoryLine line)

	{
		if (line == null)
		{
			return new TextObject("{=AF_MEMORY_UI_UncompressedMemoryLineNotFound}Uncompressed memory line could not be found.").ToString();
		}
		string bodyText = string.IsNullOrWhiteSpace(line.Text) ? new TextObject("{=AF_MEMORY_UI_Empty}(empty)").ToString() : line.Text.Trim();
		return BuildDevDailyMemoryLineSubtitle(port, line) + "\n\n" + new TextObject("{=AF_MEMORY_UI_BodyPrefix}Body:\n").ToString() + bodyText;
	}



	internal static int GetDefaultDevDailyMemoryLineHour(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{

		DailyMemoryLine latest = (draft?.Lines ?? new List<DailyMemoryLine>()).LastOrDefault((DailyMemoryLine x) => x != null);

		if (latest != null)

		{

			return MBMath.ClampInt(latest.GameHour, 0, 23);

		}

		if (draft != null && draft.GameDayIndex == port.GetCurrentGameDayIndexSafe())

		{

			return port.GetCurrentHourOfDaySafeForPrompt();

		}

		return 12;

	}



	internal static string GetDefaultDevDailyMemoryLineScene(MemoryEditorDisplayPort port, DailyMemoryDraft draft)

	{
		string text = (draft?.Lines ?? new List<DailyMemoryLine>()).LastOrDefault((DailyMemoryLine x) => x != null && !string.IsNullOrWhiteSpace(x.Scene))?.Scene;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return port.ResolveCurrentMemorySceneLabel();
	}



	internal static CompressedMemoryBlock FindDevCompressedMemoryBlock(MemoryEditorDisplayPort port, List<CompressedMemoryBlock> blocks, string blockId)

	{
		string text = (blockId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		foreach (CompressedMemoryBlock block in blocks ?? new List<CompressedMemoryBlock>())
		{
			if (string.Equals(GetDevCompressedMemoryBlockId(port, block), text, StringComparison.OrdinalIgnoreCase))
			{
				return block;
			}
		}
		return null;
	}



	internal static string GetDevCompressedMemoryBlockId(MemoryEditorDisplayPort port, CompressedMemoryBlock block)

	{
		if (block == null)
		{
			return "";
		}
		string text = (block.Id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = port.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex);
		}
		return (text ?? "").Trim();
	}



	internal static string BuildDevCompressedMemoryBlockListLabel(MemoryEditorDisplayPort port, CompressedMemoryBlock block, int displayIndex)

	{

		if (block == null)

		{

			return displayIndex + "# " + new TextObject("{=AF_MEMORY_UI_Empty}(empty)").ToString();

		}

		string date = FormatDay(block.GameDate, block.GameDayIndex);

		string title = string.IsNullOrWhiteSpace(block.RichTitle) ? new TextObject("{=AF_MEMORY_UI_Untitled}(untitled)").ToString() : block.RichTitle.Trim();

		return displayIndex + "# " + date + " " + HourRangeForDisplay(block.StartHour, block.EndHour) + " | " + port.BuildDevHistoryPreview(title, 44);

	}



	internal static string BuildDevCompressedMemoryBlockSubtitle(MemoryEditorDisplayPort port, CompressedMemoryBlock block)

	{

		if (block == null)

		{

			return "";

		}

		string date = FormatDay(block.GameDate, block.GameDayIndex);

		string title = string.IsNullOrWhiteSpace(block.RichTitle) ? new TextObject("{=AF_MEMORY_UI_Untitled}(untitled)").ToString() : block.RichTitle.Trim();

		return new TextObject("{=AF_MEMORY_UI_IdLabel}ID: ").ToString() + GetDevCompressedMemoryBlockId(port, block) + "\n" +

			new TextObject("{=AF_MEMORY_UI_DateLabel}Date: ").ToString() + date + " " + HourRangeForDisplay(block.StartHour, block.EndHour) + "\n" +

			new TextObject("{=AF_MEMORY_UI_TitleLabel}Title: ").ToString() + title + "\n" +

			new TextObject("{=AF_MEMORY_UI_WeeklyMaterialTriggersLabel}Weekly material triggers: ").ToString() + ((block.WeeklyMaterialTriggers?.Count).GetValueOrDefault());

	}



	internal static string BuildDevCompressedMemoryBlockEditorBody(MemoryEditorDisplayPort port, CompressedMemoryBlock block)

	{

		if (block == null)

		{

			return new TextObject("{=AF_MEMORY_UI_CompressedMemoryBlockNotFound}Compressed memory block could not be found.").ToString();

		}

		StringBuilder sb = new StringBuilder();

		if (block.Scenes != null && block.Scenes.Count > 0)

		{

			sb.AppendLine(new TextObject("{=AF_MEMORY_UI_SceneLabel}Scene: ").ToString() + string.Join(" / ", block.Scenes));

		}

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_BodyPreviewLabel}Body preview: ").ToString() + port.BuildDevHistoryPreview(block.Summary, 220));

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_AfefLabel}AFEF: ").ToString() + (block.AfefLines?.Count ?? 0) + new TextObject("{=AF_MEMORY_UI_Lines} lines").ToString());

		string triggerText = BuildDevWeeklyMemoryMaterialTriggerText(port, block.WeeklyMaterialTriggers);

		if (!string.IsNullOrWhiteSpace(triggerText))

		{

			sb.AppendLine(new TextObject("{=AF_MEMORY_UI_WeeklyMaterialLabel}Weekly material: ").ToString().TrimEnd());

			sb.AppendLine(triggerText);

		}

		if (block.AfefLines != null && block.AfefLines.Count > 0)

		{

			sb.AppendLine(new TextObject("{=AF_MEMORY_UI_AfefPreviewLabel}AFEF preview: ").ToString() + port.BuildDevHistoryPreview(string.Join(" / ", block.AfefLines.Take(4)), 180));

		}

		sb.AppendLine();

		sb.AppendLine(new TextObject("{=AF_MEMORY_UI_BlockEditOverviewClearNotice}Editing or deleting clears this NPC's memory overview and queues organization again.").ToString());

		return sb.ToString().TrimEnd();

	}



	internal static string[] SplitDevCompressedMemorySearchTerms(MemoryEditorDisplayPort port, string query)

	{

		try

		{

			return (query ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

		}

		catch

		{

			return new string[0];

		}

	}



	internal static bool IsDevCompressedMemoryBlockMatch(MemoryEditorDisplayPort port, CompressedMemoryBlock block, string[] terms)

	{

		if (block == null)

		{

			return false;

		}

		if (terms == null || terms.Length <= 0)

		{

			return true;

		}

		string haystack = (GetDevCompressedMemoryBlockId(port, block) + "\n" + (block.HeroName ?? "") + "\n" + (block.GameDate ?? "") + "\n" + (block.RichTitle ?? "") + "\n" + (block.Summary ?? "") + "\n" + string.Join("\n", block.Scenes ?? new List<string>()) + "\n" + string.Join("\n", block.AfefLines ?? new List<string>()) + "\n" + BuildDevWeeklyMemoryMaterialTriggerTextCore(port, block.WeeklyMaterialTriggers, forDisplay: false)).ToLowerInvariant();

		foreach (string term in terms)

		{

			string text = (term ?? "").Trim().ToLowerInvariant();

			if (!string.IsNullOrWhiteSpace(text) && !haystack.Contains(text))

			{

				return false;

			}

		}

		return true;

	}



	internal static string NormalizeDevCompressedMemoryMultilineInput(MemoryEditorDisplayPort port, string input)

	{
		return MemoryDeveloperEditOwner.NormalizeMultiline(input);
	}



	internal static List<string> ParseDevCompressedMemoryLineList(MemoryEditorDisplayPort port, string input, int maxCount, bool ignoreCase)

	{
		return MemoryDeveloperEditOwner.ParseLineList(input, maxCount, ignoreCase);
	}



	internal static DailyMemoryLine FindDevDailyMemoryLine(DailyMemoryDraft draft, int lineIndex)

	{

		if (draft?.Lines == null || lineIndex < 0 || lineIndex >= draft.Lines.Count)

		{

			return null;

		}

		return draft.Lines[lineIndex];

	}

}

