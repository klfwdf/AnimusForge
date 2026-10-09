using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using DialogueDay = AnimusForge.MyBehavior.DialogueDay;

namespace AnimusForge;

internal sealed class MemoryEditorPage<T>
{
 internal readonly List<T> Matches;
 internal readonly int Page, PageCount;
 internal MemoryEditorPage(List<T> matches, int page)
 {
  Matches = matches;
  PageCount = Math.Max(1, (matches.Count + 39) / 40);
  Page = Math.Min(Math.Max(0, page), PageCount - 1);
 }
 internal IEnumerable<T> Visible => Matches.Skip(Page * 40).Take(40);
}

// UI-owned navigation only; source records are provided by the Memory domain per opening.

internal sealed class MemoryEditorPort
{
 internal Func<Hero> GetSelectedHero; internal Action<Hero> SetSelectedHero;
 internal Hero SelectedHero { get => GetSelectedHero(); set => SetSelectedHero(value); }
 internal Func<List<MemorySummaryJob>> SummaryJobs;
 internal Func<List<MemoryOverviewJob>> OverviewJobs;
 internal delegate void ShowDevEditInquiryCapability(Hero npc);
 internal ShowDevEditInquiryCapability ShowDevEditInquiry;
 internal delegate string GetMemoryHeroIdCapability(Hero hero);
 internal GetMemoryHeroIdCapability GetMemoryHeroId;
 internal delegate MemoryOverviewState GetMemoryOverviewStateCapability(string heroId);
 internal GetMemoryOverviewStateCapability GetMemoryOverviewState;
 internal delegate List<DailyMemoryDraft> LoadDailyMemoryDraftsCapability(Hero hero);
 internal LoadDailyMemoryDraftsCapability LoadDailyMemoryDrafts;
 internal delegate List<CompressedMemoryBlock> LoadCompressedMemoryBlocksCapability(Hero hero);
 internal LoadCompressedMemoryBlocksCapability LoadCompressedMemoryBlocks;
 internal delegate List<DialogueDay> LoadDialogueHistoryCapability(Hero hero);
 internal LoadDialogueHistoryCapability LoadDialogueHistory;
 internal delegate List<DailyMemoryDraft> SanitizeDailyMemoryDraftsCapability(IEnumerable<DailyMemoryDraft> drafts);
 internal SanitizeDailyMemoryDraftsCapability SanitizeDailyMemoryDrafts;
 internal delegate List<CompressedMemoryBlock> SanitizeCompressedMemoryBlocksCapability(IEnumerable<CompressedMemoryBlock> blocks);
 internal SanitizeCompressedMemoryBlocksCapability SanitizeCompressedMemoryBlocks;
 internal delegate string NormalizeMemoryHeroIdCapability(string heroId);
 internal NormalizeMemoryHeroIdCapability NormalizeMemoryHeroId;
 internal delegate bool TrySealPastDailyMemoryDraftsCapability(long startTimestamp = 0L, double budgetMs = double.MaxValue, bool requirePendingProbe = false);
 internal TrySealPastDailyMemoryDraftsCapability TrySealPastDailyMemoryDrafts;
 internal delegate void QueueAllMemoryOverviewCandidatesForDeferredScanCapability();
 internal QueueAllMemoryOverviewCandidatesForDeferredScanCapability QueueAllMemoryOverviewCandidatesForDeferredScan;
 internal delegate int ProcessMemoryOverviewCandidateScanBudgetCapability(long startTimestamp, double budgetMs);
 internal ProcessMemoryOverviewCandidateScanBudgetCapability ProcessMemoryOverviewCandidateScanBudget;
 internal delegate void TryStartMemorySummaryQueueCapability(bool forceOverviewCandidateScan = false);
 internal TryStartMemorySummaryQueueCapability TryStartMemorySummaryQueue;
 internal delegate DailyMemoryDraft FindDevDailyMemoryDraftCapability(List<DailyMemoryDraft> drafts, int dayIndex);
 internal FindDevDailyMemoryDraftCapability FindDevDailyMemoryDraft;
 internal delegate DailyMemoryLine FindDevDailyMemoryLineCapability(List<DailyMemoryDraft> drafts, int dayIndex, int lineIndex);
 internal FindDevDailyMemoryLineCapability FindDevDailyMemoryLine;
 internal delegate string ComputeMemorySummaryFingerprintCapability(object identity);
 internal ComputeMemorySummaryFingerprintCapability ComputeMemorySummaryFingerprint;
 internal delegate bool TryParseDevSelectionIntCapability(string id, string prefix, out int value);
 internal TryParseDevSelectionIntCapability TryParseDevSelectionInt;
 internal delegate void ShowDevLargeTextOrInquiryCapability(string title, string subtitle, string body, Action onClose, string closeText = "Back");
 internal ShowDevLargeTextOrInquiryCapability ShowDevLargeTextOrInquiry;
 internal delegate void ShowDevLargeSelectionOrInquiryCapability(string title, string subtitle, string body, List<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string affirmativeText = "Enter", string cancelText = "Back");
 internal ShowDevLargeSelectionOrInquiryCapability ShowDevLargeSelectionOrInquiry;
 internal delegate void ShowDevLargeConfirmOrInquiryCapability(string title, string subtitle, string body, string confirmText, string cancelText, Action onConfirm, Action onCancel);
 internal ShowDevLargeConfirmOrInquiryCapability ShowDevLargeConfirmOrInquiry;
 internal Func<Hero,string,Action<CompressedMemoryBlock>,long,bool> TryApplyDevCompressedMemoryBlockDataMutation;
 internal Func<Hero,string,long,bool> DeleteDevCompressedMemoryBlockData;
 internal delegate bool EditLegacyHistoryCapability(Hero npc, int day, int lineIndex, string input, long generation);
 internal EditLegacyHistoryCapability TryApplyDevDialogueHistoryLineDataMutation;
 internal delegate string FormatMemoryHourRangeCapability(int startHour, int endHour);
 internal FormatMemoryHourRangeCapability FormatMemoryHourRange;
 internal delegate string BuildDailyMemoryLineForPromptCapability(DailyMemoryLine line);
 internal BuildDailyMemoryLineForPromptCapability BuildDailyMemoryLineForPrompt;
 internal delegate string BuildDevHistoryPreviewCapability(string line, int maxLen = 56);
 internal BuildDevHistoryPreviewCapability BuildDevHistoryPreview;
 internal delegate bool TryApplyDevDailyMemoryDraftDataMutationCapability(Hero npc, int day, Action<DailyMemoryDraft> mutate,
        string reason, long generation);
 internal TryApplyDevDailyMemoryDraftDataMutationCapability TryApplyDevDailyMemoryDraftDataMutation;
 internal delegate bool TryApplyDevDailyMemoryLineDataMutationCapability(Hero npc, int day, int lineIndex,
        Action<DailyMemoryDraft, DailyMemoryLine> mutate, long generation);
 internal TryApplyDevDailyMemoryLineDataMutationCapability TryApplyDevDailyMemoryLineDataMutation;
 internal delegate bool DeleteDevDailyMemoryDraftDataCapability(Hero npc, int day, long generation);
 internal DeleteDevDailyMemoryDraftDataCapability DeleteDevDailyMemoryDraftData;
 internal delegate bool ClearDevCompressedMemoryDataCapability(Hero npc, long generation);
 internal ClearDevCompressedMemoryDataCapability ClearDevCompressedMemoryData;
 internal delegate bool ClearDevDialogueHistoryDataCapability(Hero npc, long generation);
 internal ClearDevDialogueHistoryDataCapability ClearDevDialogueHistoryData;
 internal delegate bool IsMemorySourceEditorCurrentCapability(long generation);
 internal IsMemorySourceEditorCurrentCapability IsMemorySourceEditorCurrent;
 internal delegate int SaveDevMemoryOverviewCoreCapability(Hero npc, string summary);
 internal SaveDevMemoryOverviewCoreCapability SaveDevMemoryOverviewCore;
}

internal sealed class MemoryEditorController
{
 private readonly MemoryEditorPort _port;
 private readonly MemoryEditorDisplayPort _display;
 internal MemoryEditorController(MemoryEditorPort port = null, MemoryEditorDisplayPort display = null) { _port = port; _display = display; }
 private long _generation = long.MinValue;
 internal string HistoryQuery = string.Empty, DailyQuery = string.Empty, CompressedQuery = string.Empty;
 internal int DailyPage, CompressedPage;
 internal void SynchronizeGeneration(long generation)
 {
  if (_generation == generation) return;
  _generation = generation;
  HistoryQuery = DailyQuery = CompressedQuery = string.Empty;
  DailyPage = CompressedPage = 0;
 }
 internal MemoryEditorPage<DailyMemoryDraft> OpenDailyPage(IEnumerable<DailyMemoryDraft> drafts, int page, string query, MemoryEditorDisplayPort display)
 {
  DailyQuery = (query ?? "").Trim();
  string[] terms = MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(display, DailyQuery);
  var result = new MemoryEditorPage<DailyMemoryDraft>(drafts.Where(x => MemoryEditorProjection.IsDevDailyMemoryDraftMatch(display, x, terms)).ToList(), page);
  DailyPage = result.Page;
  return result;
 }
 internal MemoryEditorPage<Tuple<int, CompressedMemoryBlock>> OpenCompressedPage(IReadOnlyList<CompressedMemoryBlock> blocks, int page, string query, MemoryEditorDisplayPort display)
 {
  CompressedQuery = (query ?? "").Trim();
  string[] terms = MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(display, CompressedQuery);
  var filtered = new List<Tuple<int, CompressedMemoryBlock>>();
  for (int i = 0; i < blocks.Count; i++)
   if (MemoryEditorProjection.IsDevCompressedMemoryBlockMatch(display, blocks[i], terms)) filtered.Add(Tuple.Create(i + 1, blocks[i]));
  var result = new MemoryEditorPage<Tuple<int, CompressedMemoryBlock>>(filtered, page);
  CompressedPage = result.Page;
  return result;
 }

	internal void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = _port.LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoHistory", "This NPC has no dialogue history.")));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay.Lines.Count)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_DialogueLineNotFound", "The dialogue line to edit could not be found.")));
			OpenDevHistoryLineSelection(npc, dayIndex);
			return;
		}
		string currentValue = dialogueDay.Lines[lineIndex] ?? "";
		string displayDate = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", dialogueDay.GameDayIndex.ToString()));
		string text = npc.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueLine", "Edit dialogue line - ") + text, MemoryEditorUiText.T("AF_MEMORY_UI_CurrentDate", "Current date: ") + displayDate, MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueLineHint", "Edit the entire line in the field below; leave it empty to delete the line."), currentValue, delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input, editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevHistoryLineSelection(npc, dayIndex);
		});
	}

	internal void OpenDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string currentValue, string displayDate)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string titleText = MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueLine", "Edit dialogue line - ") + text;
		string text2 = MemoryEditorUiText.T("AF_MEMORY_UI_CurrentDate", "Current date: ") + displayDate + MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueLineLoadedHint", "\nThe original content is loaded below and can be edited.\nLeave it empty to delete the line.");
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input, editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevHistoryLineSelection(npc, dayIndex);
		}, shouldInputBeObfuscated: false, null, "", currentValue ?? ""));
	}

	internal void OpenDevCompressedMemoryMenu(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		_port.SelectedHero = npc;
		List<DailyMemoryDraft> drafts = _port.LoadDailyMemoryDrafts(npc);
		List<CompressedMemoryBlock> blocks = _port.LoadCompressedMemoryBlocks(npc);
		string heroId = _port.GetMemoryHeroId(npc);
		int queueCount = (_port.SummaryJobs() ?? new List<MemorySummaryJob>()).Count((MemorySummaryJob x) => x != null && string.Equals(_port.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
		int overviewQueueCount = (_port.OverviewJobs() ?? new List<MemoryOverviewJob>()).Count((MemoryOverviewJob x) => x != null && string.Equals(_port.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
		MemoryOverviewState overviewState = _port.GetMemoryOverviewState(heroId);
		int rawLineCount = (drafts ?? new List<DailyMemoryDraft>()).Sum((DailyMemoryDraft x) => (x?.Lines?.Count).GetValueOrDefault());
		string name = npc.Name?.ToString() ?? "NPC";
		StringBuilder body = new StringBuilder();
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_SystemData", "New compressed-memory system data:"));
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_RawHistoryCount", "Today/pending raw history:") + (drafts?.Count ?? 0) + MemoryEditorUiText.T("AF_MEMORY_UI_DaysComma", " days, ") + rawLineCount + MemoryEditorUiText.T("AF_MEMORY_UI_Lines", " lines"));
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_BlockCount", "Compressed memory blocks:") + (blocks?.Count ?? 0) + MemoryEditorUiText.T("AF_MEMORY_UI_Blocks", " blocks"));
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_SummaryQueue", "Pending summary queue:") + queueCount + MemoryEditorUiText.T("AF_MEMORY_UI_Items", " items"));
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_Overview", "Memory overview:") + ((overviewState != null && !string.IsNullOrWhiteSpace(overviewState.Summary)) ? (MemoryEditorUiText.T("AF_MEMORY_UI_OverviewGenerated", "generated, includes ") + (overviewState.IncludedBlockIds?.Count ?? 0) + MemoryEditorUiText.T("AF_MEMORY_UI_Blocks", " blocks")) : MemoryEditorUiText.T("AF_MEMORY_UI_NotGenerated", "not generated")) + MemoryEditorUiText.T("AF_MEMORY_UI_OverviewQueue", "; pending update queue: ") + overviewQueueCount + MemoryEditorUiText.T("AF_MEMORY_UI_Items", " items"));
		body.AppendLine();
		body.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_LegacyHistoryNotice", "Legacy _dialogueHistory_v2 is no longer injected into the main memory pipeline."));
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("raw", MemoryEditorUiText.T("AF_MEMORY_UI_ViewRawHistory", "View today/pending raw history"), null),
			new InquiryElement("edit_raw", MemoryEditorUiText.T("AF_MEMORY_UI_EditRawHistory", "Edit today/pending raw history"), null),
			new InquiryElement("blocks", MemoryEditorUiText.T("AF_MEMORY_UI_ViewBlocks", "View compressed memory blocks"), null),
			new InquiryElement("edit_blocks", MemoryEditorUiText.T("AF_MEMORY_UI_EditBlocks", "Edit compressed memory blocks"), null),
			new InquiryElement("overview", MemoryEditorUiText.T("AF_MEMORY_UI_ViewOverview", "View memory overview"), null),
			new InquiryElement("edit_overview", MemoryEditorUiText.T("AF_MEMORY_UI_EditOverview", "Edit memory overview"), null),
			new InquiryElement("queue", MemoryEditorUiText.T("AF_MEMORY_UI_ViewQueue", "View pending summary queue"), null),
			new InquiryElement("clear", MemoryEditorUiText.T("AF_MEMORY_UI_ClearNewData", "Clear this NPC's new memory data"), null),
			new InquiryElement("process", MemoryEditorUiText.T("AF_MEMORY_UI_ProcessQueue", "Process summary queue now"), null),
			new InquiryElement("back", MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), null)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_CompressionMenu", "Compressed memory management - ") + name, body.ToString().TrimEnd(), list, isExitShown: true, 0, 1, MemoryEditorUiText.T("AF_MEMORY_UI_Execute", "Execute"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(List<InquiryElement> selected)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			OnDevCompressedMemoryMenuSelected(selected);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			_port.ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevCompressedMemoryMenuSelected(List<InquiryElement> selected)
	{
		Hero npc = _port.SelectedHero;
		if (npc == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0 || selected[0].Identifier == null)
		{
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "raw":
			ShowDevCompressedMemoryText(npc, MemoryEditorUiText.T("AF_MEMORY_UI_RawHistory", "Raw history"), BuildDevCompressedMemoryRawText(npc));
			break;
		case "edit_raw":
			OpenDevDailyMemoryDraftList(npc, 0, DailyQuery);
			break;
		case "blocks":
			ShowDevCompressedMemoryText(npc, MemoryEditorUiText.T("AF_MEMORY_UI_BlocksTitle", "Compressed memory blocks"), BuildDevCompressedMemoryBlockText(npc));
			break;
		case "edit_blocks":
			OpenDevCompressedMemoryBlockList(npc, 0, CompressedQuery);
			break;
		case "overview":
			ShowDevCompressedMemoryText(npc, MemoryEditorUiText.T("AF_MEMORY_UI_OverviewTitle", "Memory overview"), BuildDevMemoryOverviewText(npc));
			break;
		case "edit_overview":
			OpenDevMemoryOverviewEditor(npc);
			break;
		case "queue":
			ShowDevCompressedMemoryText(npc, MemoryEditorUiText.T("AF_MEMORY_UI_QueueTitle", "Pending summary queue"), BuildDevCompressedMemoryQueueText(npc));
			break;
		case "clear":
			ConfirmDevClearCompressedMemory(npc);
			break;
		case "process":
			_port.TrySealPastDailyMemoryDrafts();
			_port.QueueAllMemoryOverviewCandidatesForDeferredScan();
			_port.ProcessMemoryOverviewCandidateScanBudget(0L, double.MaxValue);
			_port.TryStartMemorySummaryQueue();
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_QueueCheckTriggered", "Compressed-memory summary queue check started.")));
			OpenDevCompressedMemoryMenu(npc);
			break;
		default:
			_port.ShowDevEditInquiry(npc);
			break;
		}
	}

	internal void ShowDevCompressedMemoryText(Hero npc, string title, string text)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		string name = npc?.Name?.ToString() ?? "NPC";
		_port.ShowDevLargeTextOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_CompressionMenu", "Compressed memory management - ") + title + " - " + name, "", string.IsNullOrWhiteSpace(text) ? MemoryEditorUiText.T("AF_MEMORY_UI_NoData", "(No data)") : text.Trim(), delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		});
	}

	internal void OpenDevDailyMemoryDraftList(Hero npc, int page, string query)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		_port.SelectedHero = npc;
		page = Math.Max(0, page);
		string q = (query ?? "").Trim();
		DailyQuery = q;
		DailyPage = page;
		List<DailyMemoryDraft> drafts = _port.SanitizeDailyMemoryDrafts(_port.LoadDailyMemoryDrafts(npc));
		if (drafts == null || drafts.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoRawHistory", "This NPC has no uncompressed raw history.")));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<DailyMemoryDraft> view = OpenDailyPage(drafts, page, q, _display);
		List<DailyMemoryDraft> filtered = view.Matches;
		const int pageSize = 40;
		int pageCount = view.PageCount;
		page = view.Page;
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>();
		options.Add(new DevLargeSelectionPopup.Option("__search__", MemoryEditorUiText.T("AF_MEMORY_UI_SearchRaw", "Search uncompressed memory"), MemoryEditorUiText.T("AF_MEMORY_UI_SearchRawHint", "Filter by date, scene, speaker, body, or AFEF."), isPrimary: true));
		if (!string.IsNullOrWhiteSpace(q))
		{
			options.Add(new DevLargeSelectionPopup.Option("__clear__", MemoryEditorUiText.T("AF_MEMORY_UI_ClearSearch", "Clear search"), MemoryEditorUiText.T("AF_MEMORY_UI_ClearSearchHint", "Show all uncompressed memory again.")));
		}
		if (page > 0)
		{
			options.Add(new DevLargeSelectionPopup.Option("__prev__", MemoryEditorUiText.T("AF_MEMORY_UI_PreviousPage", "Previous page"), MemoryEditorUiText.T("AF_MEMORY_UI_PreviousPageHint", "View the previous page of results.")));
		}
		if (page + 1 < pageCount)
		{
			options.Add(new DevLargeSelectionPopup.Option("__next__", MemoryEditorUiText.T("AF_MEMORY_UI_NextPage", "Next page"), MemoryEditorUiText.T("AF_MEMORY_UI_NextPageHint", "View the next page of results.")));
		}
		foreach (DailyMemoryDraft draft in filtered.Skip(page * pageSize).Take(pageSize))
		{
			string date = string.IsNullOrWhiteSpace(draft.GameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", draft.GameDayIndex.ToString()) : draft.GameDate.Trim();
			string detail = _port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 260);
			string meta = MemoryEditorUiText.T("AF_MEMORY_UI_LineCount", "Lines: ") + ((draft.Lines?.Count).GetValueOrDefault()) + MemoryEditorUiText.T("AF_MEMORY_UI_Queued", "; queued: ") + (draft.QueuedForSummary ? MemoryEditorUiText.T("AF_MEMORY_UI_Yes", "yes") : MemoryEditorUiText.T("AF_MEMORY_UI_No", "no")) + (string.IsNullOrWhiteSpace(draft.LastSummaryError) ? "" : MemoryEditorUiText.T("AF_MEMORY_UI_LatestError", "; latest error: ") + BuildDevStoredErrorReference(draft.LastSummaryError));
			options.Add(new DevLargeSelectionPopup.Option("day:" + draft.GameDayIndex, date, detail, meta));
		}
		string name = npc.Name?.ToString() ?? "NPC";
		string descriptionText = MemoryEditorUiText.T("AF_MEMORY_UI_RawListDescription", "Select a day of uncompressed raw history to edit.\nRaw history: ") + drafts.Count + MemoryEditorUiText.T("AF_MEMORY_UI_RawListResult", " days; current results: ") + filtered.Count + MemoryEditorUiText.T("AF_MEMORY_UI_PagePrefix", " days; page ") + (page + 1) + "/" + pageCount + MemoryEditorUiText.T("AF_MEMORY_UI_PageSuffix", ".");
		if (!string.IsNullOrWhiteSpace(q))
		{
			descriptionText += MemoryEditorUiText.T("AF_MEMORY_UI_CurrentSearch", "\nCurrent search: ") + _port.BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += MemoryEditorUiText.T("AF_MEMORY_UI_NoMatches", "\n\nNo matching results.");
		}
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_EditRawTitle", "Edit uncompressed memory - ") + name, MemoryEditorUiText.T("AF_MEMORY_UI_RawDaysList", "Uncompressed raw-history day list"), descriptionText, options, delegate(string selectedId)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevDailyMemoryDraftList(npc, page, q);
				return;
			}
			switch (selectedId)
			{
			case "__search__":
				InformationManager.ShowTextInquiry(new TextInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_SearchRaw", "Search uncompressed memory"), MemoryEditorUiText.T("AF_MEMORY_UI_SearchRawPrompt", "Enter keywords to match dates, scenes, speakers, body text, or AFEF."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_Search", "Search"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(string input)
				{
					if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
					OpenDevDailyMemoryDraftList(npc, 0, input);
				}, delegate
				{
					if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
					OpenDevDailyMemoryDraftList(npc, page, q);
				}, shouldInputBeObfuscated: false, null, q));
				break;
			case "__clear__":
				OpenDevDailyMemoryDraftList(npc, 0, null);
				break;
			case "__prev__":
				OpenDevDailyMemoryDraftList(npc, page - 1, q);
				break;
			case "__next__":
				OpenDevDailyMemoryDraftList(npc, page + 1, q);
				break;
			default:
				if (_port.TryParseDevSelectionInt(selectedId, "day:", out var dayIndex))
				{
					OpenDevDailyMemoryDraftEditor(npc, dayIndex, page, q);
				}
				else
				{
					OpenDevDailyMemoryDraftList(npc, page, q);
				}
				break;
			}
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		});
	}

	internal void OpenDevDailyMemoryDraftEditor(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		_port.SelectedHero = npc;
		DailyMemoryDraft draft = _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_RawNotFound", "The uncompressed memory to edit could not be found.")));
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(draft);
		string name = npc.Name?.ToString() ?? "NPC";
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("lines", MemoryEditorUiText.T("AF_MEMORY_UI_EditRawLines", "Edit raw lines"), MemoryEditorUiText.T("AF_MEMORY_UI_EditRawLinesHint", "View and edit every uncompressed memory line for this day."), isPrimary: true),
			new DevLargeSelectionPopup.Option("add_normal", MemoryEditorUiText.T("AF_MEMORY_UI_AddNormal", "Add normal memory line"), MemoryEditorUiText.T("AF_MEMORY_UI_AddNormalHint", "Add a raw line included in LLM dialogue memory.")),
			new DevLargeSelectionPopup.Option("add_afef", MemoryEditorUiText.T("AF_MEMORY_UI_AddAfef", "Add AFEF line"), MemoryEditorUiText.T("AF_MEMORY_UI_AddAfefHint", "Add an AFEF mechanism line excluded from LLM dialogue compression text.")),
			new DevLargeSelectionPopup.Option("delete", MemoryEditorUiText.T("AF_MEMORY_UI_DeleteDay", "Delete this day's uncompressed memory"), MemoryEditorUiText.T("AF_MEMORY_UI_DeleteDayHint", "Delete all raw history for this day and remove its pending summary job."), isDanger: true),
			new DevLargeSelectionPopup.Option("back", MemoryEditorUiText.T("AF_MEMORY_UI_BackToList", "Back to list"))
		};
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_RawMemoryTitle", "Uncompressed memory - ") + name, BuildDevDailyMemoryDraftSubtitle(draft), BuildDevDailyMemoryDraftEditorDescription(draft), options, delegate(string selectedId)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
				return;
			}
			switch (selectedId)
			{
			case "lines":
				OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
				break;
			case "add_normal":
				OpenDevAddDailyMemoryLine(npc, dayIndex, isAfef: false, returnPage, returnQuery);
				break;
			case "add_afef":
				OpenDevAddDailyMemoryLine(npc, dayIndex, isAfef: true, returnPage, returnQuery);
				break;
			case "delete":
				ConfirmDevDeleteDailyMemoryDraft(npc, dayIndex, returnPage, returnQuery);
				break;
			case "back":
				OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
				break;
			default:
				OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
				break;
			}
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		});
	}

	internal void OpenDevDailyMemoryLineList(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		DailyMemoryDraft draft = _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		List<DailyMemoryLine> lines = (draft.Lines ?? new List<DailyMemoryLine>()).Where((DailyMemoryLine x) => x != null).ToList();
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("__add_normal__", MemoryEditorUiText.T("AF_MEMORY_UI_AddNormal", "Add normal memory line"), MemoryEditorUiText.T("AF_MEMORY_UI_AddNormalHint", "Add a raw line included in LLM dialogue memory."), isPrimary: true),
			new DevLargeSelectionPopup.Option("__add_afef__", MemoryEditorUiText.T("AF_MEMORY_UI_AddAfef", "Add AFEF line"), MemoryEditorUiText.T("AF_MEMORY_UI_AddAfefShortHint", "Add an AFEF mechanism line.")),
			new DevLargeSelectionPopup.Option("__back__", MemoryEditorUiText.T("AF_MEMORY_UI_BackToDayMenu", "Back to this day's menu"))
		};
		for (int i = 0; i < lines.Count; i++)
		{
			DailyMemoryLine line = lines[i];
			string type = line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? "LLM" : MemoryEditorUiText.T("AF_MEMORY_UI_Normal", "Normal"));
			string speaker = string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "手动") : line.Speaker.Trim();
			string scene = string.IsNullOrWhiteSpace(line.Scene) ? MemoryEditorUiText.T("AF_MEMORY_UI_UnknownScene", "Unknown scene") : _port.BuildDevHistoryPreview(line.Scene, 42);
			string title = (i + 1) + ". " + MBMath.ClampInt(line.GameHour, 0, 23) + MemoryEditorUiText.T("AF_MEMORY_UI_HourSeparator", "h | ") + type + " | " + speaker + " | " + scene;
			string detail = string.IsNullOrWhiteSpace(line.Text) ? MemoryEditorUiText.T("AF_MEMORY_UI_Empty", "(empty)") : _port.BuildDevHistoryPreview(line.Text, 260);
			options.Add(new DevLargeSelectionPopup.Option("line:" + i, title, detail));
		}
		string name = npc?.Name?.ToString() ?? "NPC";
		string body = BuildDevDailyMemoryDraftSubtitle(draft) + MemoryEditorUiText.T("AF_MEMORY_UI_LineListDescription", "\n\nThere are currently ") + lines.Count + MemoryEditorUiText.T("AF_MEMORY_UI_LineListDescriptionSuffix", " lines. Select an item on the right to open line details.");
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_EditRawLineTitle", "Edit uncompressed memory line - ") + name, MemoryEditorUiText.T("AF_MEMORY_UI_RawLineList", "Uncompressed raw-history lines"), body, options, delegate(string selectedId)
		{
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
				return;
			}
			if (!_port.TryParseDevSelectionInt(selectedId, "line:", out var lineIndex))
			{
				switch (selectedId)
				{
				case "__add_normal__":
					OpenDevAddDailyMemoryLine(npc, dayIndex, isAfef: false, returnPage, returnQuery);
					return;
				case "__add_afef__":
					OpenDevAddDailyMemoryLine(npc, dayIndex, isAfef: true, returnPage, returnQuery);
					return;
				case "__back__":
					OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
					return;
				}
			}
			if (_port.TryParseDevSelectionInt(selectedId, "line:", out lineIndex))
			{
				OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
			}
			else
			{
				OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			}
		}, delegate
		{
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Edit", "Edit"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevDailyMemoryLineEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_RawLineNotFound", "The uncompressed memory line to edit could not be found.")));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("text", MemoryEditorUiText.T("AF_MEMORY_UI_EditBody", "Edit body"), MemoryEditorUiText.T("AF_MEMORY_UI_EditBodyHint", "Open the large text editor to change the body; leave it empty to delete the line."), isPrimary: true),
			new DevLargeSelectionPopup.Option("speaker", MemoryEditorUiText.T("AF_MEMORY_UI_EditSpeaker", "Edit speaker"), MemoryEditorUiText.T("AF_MEMORY_UI_EditSpeakerHint", "Change the speaker shown and synchronized for this line.")),
			new DevLargeSelectionPopup.Option("scene", MemoryEditorUiText.T("AF_MEMORY_UI_EditScene", "Edit scene"), MemoryEditorUiText.T("AF_MEMORY_UI_EditSceneHint", "Change the scene assigned to this line.")),
			new DevLargeSelectionPopup.Option("hour", MemoryEditorUiText.T("AF_MEMORY_UI_EditHour", "Edit hour"), MemoryEditorUiText.T("AF_MEMORY_UI_EditHourHint", "Change the in-game hour for this line.")),
			new DevLargeSelectionPopup.Option("toggle_afef", line.IsAfef ? MemoryEditorUiText.T("AF_MEMORY_UI_ToggleNormal", "Change to normal line") : MemoryEditorUiText.T("AF_MEMORY_UI_ToggleAfef", "Change to AFEF line")),
			new DevLargeSelectionPopup.Option("toggle_llm", line.IsLlmDialogue ? MemoryEditorUiText.T("AF_MEMORY_UI_ClearLlm", "Clear LLM dialogue marker") : MemoryEditorUiText.T("AF_MEMORY_UI_SetLlm", "Mark as LLM dialogue")),
			new DevLargeSelectionPopup.Option("delete", MemoryEditorUiText.T("AF_MEMORY_UI_DeleteLine", "Delete line"), MemoryEditorUiText.T("AF_MEMORY_UI_DeleteLineHint", "Deleting also removes the matching legacy dialogue-history line."), isDanger: true),
			new DevLargeSelectionPopup.Option("back", MemoryEditorUiText.T("AF_MEMORY_UI_BackToLines", "Back to line list"))
		};
		string name = npc?.Name?.ToString() ?? "NPC";
		string body = MemoryEditorUiText.T("AF_MEMORY_UI_BodyPrefix", "Body:\n") + (string.IsNullOrWhiteSpace(line.Text) ? MemoryEditorUiText.T("AF_MEMORY_UI_Empty", "(empty)") : line.Text.Trim());
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_RawLineTitle", "Uncompressed memory line - ") + name, BuildDevDailyMemoryLineSubtitle(line), body, options, delegate(string selectedId)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				return;
			}
			switch (selectedId)
			{
			case "text":
				OpenDevDailyMemoryLineTextEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "speaker":
				OpenDevDailyMemoryLineSpeakerEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "scene":
				OpenDevDailyMemoryLineSceneEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "hour":
				OpenDevDailyMemoryLineHourEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "toggle_afef":
				ToggleDevDailyMemoryLineAfef(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "toggle_llm":
				ToggleDevDailyMemoryLineLlm(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "delete":
				ConfirmDevDeleteDailyMemoryLine(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			case "back":
				OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
				break;
			default:
				OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				break;
			}
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		});
	}

	internal void OpenDevDailyMemoryLineTextEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditBodyTitle", "Edit uncompressed memory body - ") + name, BuildDevDailyMemoryLineSubtitle(line), MemoryEditorUiText.T("AF_MEMORY_UI_EditBodyPrompt", "Enter the new body; empty deletes the line."), line.Text ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				string text = NormalizeDevCompressedMemoryMultilineInput(input);
				if (string.IsNullOrWhiteSpace(text))
				{
					draft.Lines?.Remove(target);
				}
				else
				{
					target.Text = text;
				}
			}, MemoryEditorUiText.T("AF_MEMORY_UI_BodyUpdated", "Uncompressed memory body updated."));
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevDailyMemoryLineSpeakerEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditSpeakerTitle", "Edit uncompressed memory speaker - ") + name, BuildDevDailyMemoryLineSubtitle(line), MemoryEditorUiText.T("AF_MEMORY_UI_EditSpeakerPrompt", "Enter a speaker; empty uses the default speaker automatically."), line.Speaker ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Speaker = (input ?? "").Trim();
			}, MemoryEditorUiText.T("AF_MEMORY_UI_SpeakerUpdated", "Uncompressed memory speaker updated."));
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevDailyMemoryLineSceneEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditSceneTitle", "Edit uncompressed memory scene - ") + name, BuildDevDailyMemoryLineSubtitle(line), MemoryEditorUiText.T("AF_MEMORY_UI_EditScenePrompt", "Enter a scene; empty uses unknown scene."), line.Scene ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Scene = (input ?? "").Trim();
			}, MemoryEditorUiText.T("AF_MEMORY_UI_SceneUpdated", "Uncompressed memory scene updated."));
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevDailyMemoryLineHourEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		InformationManager.ShowTextInquiry(new TextInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_EditHourTitle", "Edit uncompressed memory hour"), BuildDevDailyMemoryLineSubtitle(line) + MemoryEditorUiText.T("AF_MEMORY_UI_EditHourPrompt", "\nEnter an integer from 0 to 23."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			if (!int.TryParse((input ?? "").Trim(), out var hour) || hour < 0 || hour > 23)
			{
				InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_InvalidHour", "Enter an integer from 0 to 23.")));
				OpenDevDailyMemoryLineHourEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				return;
			}
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.GameHour = hour;
			}, MemoryEditorUiText.T("AF_MEMORY_UI_HourUpdated", "Uncompressed memory hour updated."));
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, shouldInputBeObfuscated: false, null, line.GameHour.ToString()));
	}

	internal void ToggleDevDailyMemoryLineAfef(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
		{
			target.IsAfef = !target.IsAfef;
			if (target.IsAfef)
			{
				target.IsLlmDialogue = false;
				if (string.IsNullOrWhiteSpace(target.Speaker) || string.Equals(target.Speaker.Trim(), "手动", StringComparison.OrdinalIgnoreCase))
				{
					target.Speaker = "AFEF";
				}
			}
			else if (string.Equals((target.Speaker ?? "").Trim(), "AFEF", StringComparison.OrdinalIgnoreCase))
			{
				target.Speaker = "手动";
			}
		}, MemoryEditorUiText.T("AF_MEMORY_UI_LineTypeUpdated", "Uncompressed memory line type updated."));
	}

	internal void ToggleDevDailyMemoryLineLlm(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line != null && line.IsAfef)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_AfefLlmConflict", "An AFEF line cannot be marked as LLM dialogue.")));
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
			return;
		}
		ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
		{
			target.IsLlmDialogue = !target.IsLlmDialogue;
		}, MemoryEditorUiText.T("AF_MEMORY_UI_LlmMarkerUpdated", "Uncompressed memory LLM marker updated."));
	}

	internal void OpenDevAddDailyMemoryLine(Hero npc, int dayIndex, bool isAfef, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryDraft draft = _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(draft);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(isAfef ? (MemoryEditorUiText.T("AF_MEMORY_UI_AddAfefTitle", "Add AFEF line - ") + name) : (MemoryEditorUiText.T("AF_MEMORY_UI_AddNormalTitle", "Add normal memory line - ") + name), BuildDevDailyMemoryDraftSubtitle(draft), MemoryEditorUiText.T("AF_MEMORY_UI_AddLinePrompt", "Enter the new line body; empty cancels."), "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			string text = NormalizeDevCompressedMemoryMultilineInput(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
				return;
			}
			if (!_port.TryApplyDevDailyMemoryDraftDataMutation(npc, dayIndex, targetDraft =>
			{
				if (targetDraft.Lines == null) targetDraft.Lines = new List<DailyMemoryLine>();
				targetDraft.Lines.Add(new DailyMemoryLine
				{
					GameDayIndex = targetDraft.GameDayIndex,
					GameDate = targetDraft.GameDate ?? "",
					GameHour = GetDefaultDevDailyMemoryLineHour(targetDraft),
					Scene = GetDefaultDevDailyMemoryLineScene(targetDraft),
					Speaker = isAfef ? "AFEF" : "手动",
					Text = text,
					SceneSessionId = -1,
					DialogueSessionId = -1,
					MemorySessionKey = "dev:" + Guid.NewGuid().ToString("N"),
					IsAfef = isAfef,
					IsLlmDialogue = !isAfef
				});
			}, "add_line", editorGeneration))
			{
				OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
				return;
			}
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_LineAdded", "Uncompressed memory line added.")));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void ConfirmDevDeleteDailyMemoryLine(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		_port.ShowDevLargeConfirmOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDeleteLine", "Confirm deleting uncompressed memory line"), BuildDevDailyMemoryLineSubtitle(line), MemoryEditorUiText.T("AF_MEMORY_UI_BodyPrefix", "Body:\n") + (string.IsNullOrWhiteSpace(line.Text) ? MemoryEditorUiText.T("AF_MEMORY_UI_Empty", "(empty)") : line.Text.Trim()) + MemoryEditorUiText.T("AF_MEMORY_UI_IrreversibleConfirm", "\n\nThis action cannot be undone. Continue?"), MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDelete", "Delete"), MemoryEditorUiText.T("AF_MEMORY_UI_Cancel", "Cancel"), delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryDraftMutation(npc, dayIndex, delegate(DailyMemoryDraft draft)
			{
				if (draft.Lines != null && lineIndex >= 0 && lineIndex < draft.Lines.Count)
				{
					draft.Lines.RemoveAt(lineIndex);
				}
			}, "delete_line", MemoryEditorUiText.T("AF_MEMORY_UI_LineDeleted", "Uncompressed memory line deleted."), returnPage, returnQuery, returnToDraftEditor: false);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		});
	}

	internal void ConfirmDevDeleteDailyMemoryDraft(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryDraft draft = _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(draft);
		_port.ShowDevLargeConfirmOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDeleteDay", "Confirm deleting uncompressed memory"), BuildDevDailyMemoryDraftSubtitle(draft), MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDeleteDayBody", "Delete all uncompressed raw history for this day and remove its pending summary job.\nThis action cannot be undone. Continue?"), MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDelete", "Delete"), MemoryEditorUiText.T("AF_MEMORY_UI_Cancel", "Cancel"), delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			if (!_port.DeleteDevDailyMemoryDraftData(npc, dayIndex, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_DayDeleted", "This day's uncompressed memory was deleted.")));
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		});
	}

	internal void OpenDevMemoryOverviewEditor(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		string heroId = _port.GetMemoryHeroId(npc);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		MemoryOverviewState state = _port.GetMemoryOverviewState(heroId);
		List<CompressedMemoryBlock> blocks = _port.LoadCompressedMemoryBlocks(npc);
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(new { Overview = state, Blocks = blocks });
		string name = npc.Name?.ToString() ?? "NPC";
		string subtitle = MemoryEditorUiText.T("AF_MEMORY_UI_CurrentBlocks", "Current compressed memory blocks: ") + (blocks?.Count ?? 0) + MemoryEditorUiText.T("AF_MEMORY_UI_IncludedBlocks", " blocks; included: ") + (state?.IncludedBlockIds?.Count ?? 0) + MemoryEditorUiText.T("AF_MEMORY_UI_OverviewEditHint", " blocks.\nSaving non-empty content marks all current compressed blocks as included so the pending update queue does not overwrite it immediately.");
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditOverviewTitle", "Edit memory overview - ") + name, subtitle, MemoryEditorUiText.T("AF_MEMORY_UI_EditOverviewPrompt", "Enter the new past-memory overview; empty clears it and waits for regeneration."), state?.Summary ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !string.Equals(heroId, _port.GetMemoryHeroId(npc), StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(new { Overview = _port.GetMemoryOverviewState(heroId), Blocks = _port.LoadCompressedMemoryBlocks(npc) }), StringComparison.Ordinal)) return;
			ApplyDevMemoryOverviewInput(npc, input);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryMenu(npc);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void ApplyDevMemoryOverviewInput(Hero npc, string input)
	{
		string heroId = _port.GetMemoryHeroId(npc);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		string summary = NormalizeDevCompressedMemoryMultilineInput(input);
		int includedCount = _port.SaveDevMemoryOverviewCore(npc, summary);
		bool saved = includedCount >= 0;
		if (!saved)
		{
			Logger.Log("MemoryOverview", "manual_overview_clear hero=" + heroId);
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_OverviewCleared", "Memory overview cleared; pending organization queue checked again.")));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		Logger.Log("MemoryOverview", "manual_overview_save hero=" + heroId + " blocks=" + includedCount);
		InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_OverviewUpdated", "Memory overview updated.")));
		OpenDevCompressedMemoryMenu(npc);
	}

	internal void OpenDevCompressedMemoryBlockList(Hero npc, int page, string query)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		_port.SelectedHero = npc;
		page = Math.Max(0, page);
		string q = (query ?? "").Trim();
		CompressedQuery = q;
		CompressedPage = page;
		List<CompressedMemoryBlock> blocks = _port.SanitizeCompressedMemoryBlocks(_port.LoadCompressedMemoryBlocks(npc));
		if (blocks == null || blocks.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoBlocks", "This NPC has no compressed memory blocks.")));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<Tuple<int, CompressedMemoryBlock>> view = OpenCompressedPage(blocks, page, q, _display);
		List<Tuple<int, CompressedMemoryBlock>> filtered = view.Matches;
		const int pageSize = 40;
		int pageCount = view.PageCount;
		page = view.Page;
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>();
		options.Add(new DevLargeSelectionPopup.Option("__search__", MemoryEditorUiText.T("AF_MEMORY_UI_SearchBlocks", "Search compressed memory blocks"), isPrimary: true));
		if (!string.IsNullOrWhiteSpace(q))
		{
			options.Add(new DevLargeSelectionPopup.Option("__clear__", MemoryEditorUiText.T("AF_MEMORY_UI_ClearSearch", "Clear search")));
		}
		if (page > 0)
		{
			options.Add(new DevLargeSelectionPopup.Option("__prev__", MemoryEditorUiText.T("AF_MEMORY_UI_PreviousPage", "Previous page")));
		}
		if (page + 1 < pageCount)
		{
			options.Add(new DevLargeSelectionPopup.Option("__next__", MemoryEditorUiText.T("AF_MEMORY_UI_NextPage", "Next page")));
		}
		foreach (Tuple<int, CompressedMemoryBlock> item in filtered.Skip(page * pageSize).Take(pageSize))
		{
			string blockId = GetDevCompressedMemoryBlockId(item.Item2);
			if (!string.IsNullOrWhiteSpace(blockId))
			{
				options.Add(new DevLargeSelectionPopup.Option(blockId, BuildDevCompressedMemoryBlockListLabel(item.Item2, item.Item1)));
			}
		}
		string name = npc.Name?.ToString() ?? "NPC";
		string descriptionText = MemoryEditorUiText.T("AF_MEMORY_UI_BlockListDescription", "Select a compressed memory block to edit.\nCompressed memory blocks: ") + blocks.Count + MemoryEditorUiText.T("AF_MEMORY_UI_BlockListResult", " blocks; current results: ") + filtered.Count + MemoryEditorUiText.T("AF_MEMORY_UI_BlockPagePrefix", " blocks; page ") + (page + 1) + "/" + pageCount + MemoryEditorUiText.T("AF_MEMORY_UI_PageSuffix", ".");
		if (!string.IsNullOrWhiteSpace(q))
		{
			descriptionText += MemoryEditorUiText.T("AF_MEMORY_UI_CurrentSearch", "\nCurrent search: ") + _port.BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += MemoryEditorUiText.T("AF_MEMORY_UI_NoMatches", "\n\nNo matching results.");
		}
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_EditBlocksTitle", "Edit compressed memory blocks - ") + name, MemoryEditorUiText.T("AF_MEMORY_UI_BlockList", "Compressed memory block list"), descriptionText, options, delegate(string selectedId)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevCompressedMemoryBlockList(npc, page, q);
				return;
			}
			switch (selectedId)
			{
			case "__search__":
				InformationManager.ShowTextInquiry(new TextInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_SearchBlocks", "Search compressed memory blocks"), MemoryEditorUiText.T("AF_MEMORY_UI_SearchBlocksPrompt", "Enter keywords to match title, body, scenes, AFEF, date, or memory-block ID."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_Search", "Search"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(string input)
				{
					if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
					OpenDevCompressedMemoryBlockList(npc, 0, input);
				}, delegate
				{
					if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
					OpenDevCompressedMemoryBlockList(npc, page, q);
				}, shouldInputBeObfuscated: false, null, q));
				break;
			case "__clear__":
				OpenDevCompressedMemoryBlockList(npc, 0, null);
				break;
			case "__prev__":
				OpenDevCompressedMemoryBlockList(npc, page - 1, q);
				break;
			case "__next__":
				OpenDevCompressedMemoryBlockList(npc, page + 1, q);
				break;
			default:
				OpenDevCompressedMemoryBlockEditor(npc, selectedId, page, q);
				break;
			}
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Enter", "Enter"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevCompressedMemoryBlockEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		_port.SelectedHero = npc;
		List<CompressedMemoryBlock> blocks = _port.LoadCompressedMemoryBlocks(npc);
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(blocks, blockId);
		if (block == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_BlockNotFound", "The compressed memory block to edit could not be found.")));
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc.Name?.ToString() ?? "NPC";
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("summary", MemoryEditorUiText.T("AF_MEMORY_UI_EditBody", "Edit body"), MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockBodyHint", "Open the large editor to change block body; empty clears the body."), isPrimary: true),
			new DevLargeSelectionPopup.Option("title", MemoryEditorUiText.T("AF_MEMORY_UI_EditTitle", "Edit title"), MemoryEditorUiText.T("AF_MEMORY_UI_EditTitleHint", "Open the large editor to change the rich title; empty clears the title.")),
			new DevLargeSelectionPopup.Option("scenes", MemoryEditorUiText.T("AF_MEMORY_UI_EditScenes", "Edit scene list"), MemoryEditorUiText.T("AF_MEMORY_UI_EditScenesHint", "Open the large editor to change scenes one per line; keep at most 16 items.")),
			new DevLargeSelectionPopup.Option("afef", MemoryEditorUiText.T("AF_MEMORY_UI_EditAfef", "Edit AFEF lines"), MemoryEditorUiText.T("AF_MEMORY_UI_EditAfefHint", "Open the large editor to change AFEF one per line; keep at most 80 items.")),
			new DevLargeSelectionPopup.Option("delete", MemoryEditorUiText.T("AF_MEMORY_UI_DeleteBlock", "Delete this memory block"), MemoryEditorUiText.T("AF_MEMORY_UI_DeleteBlockHint", "Deleting clears this NPC's overview and queues organization again."), isDanger: true),
			new DevLargeSelectionPopup.Option("back", MemoryEditorUiText.T("AF_MEMORY_UI_BackToList", "Back to list"))
		};
		_port.ShowDevLargeSelectionOrInquiry(MemoryEditorUiText.T("AF_MEMORY_UI_BlockTitle", "Memory block - ") + name, BuildDevCompressedMemoryBlockSubtitle(block), BuildDevCompressedMemoryBlockEditorBody(block), options, delegate(string selectedId)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
				return;
			}
			switch (selectedId)
			{
			case "title":
				OpenDevCompressedMemoryBlockTitleEditor(npc, blockId, returnPage, returnQuery);
				break;
			case "summary":
				OpenDevCompressedMemoryBlockSummaryEditor(npc, blockId, returnPage, returnQuery);
				break;
			case "scenes":
				OpenDevCompressedMemoryBlockScenesEditor(npc, blockId, returnPage, returnQuery);
				break;
			case "afef":
				OpenDevCompressedMemoryBlockAfefEditor(npc, blockId, returnPage, returnQuery);
				break;
			case "delete":
				ConfirmDevDeleteCompressedMemoryBlock(npc, blockId, returnPage, returnQuery);
				break;
			case "back":
				OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
				break;
			default:
				OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
				break;
			}
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_port.SelectedHero, npc)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Enter", "Enter"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevCompressedMemoryBlockTitleEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockTitle", "Edit memory-block title - ") + name, BuildDevCompressedMemoryBlockSubtitle(block), MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockTitlePrompt", "Enter the new rich title; empty clears the title."), block.RichTitle ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.RichTitle = (input ?? "").Trim();
			}, MemoryEditorUiText.T("AF_MEMORY_UI_BlockTitleUpdated", "Memory-block title updated."), editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevCompressedMemoryBlockSummaryEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockBodyTitle", "Edit memory-block body - ") + name, BuildDevCompressedMemoryBlockSubtitle(block), MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockBodyPrompt", "Enter the new memory body; empty clears the body."), block.Summary ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Summary = NormalizeDevCompressedMemoryMultilineInput(input);
			}, MemoryEditorUiText.T("AF_MEMORY_UI_BlockBodyUpdated", "Memory-block body updated."), editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevCompressedMemoryBlockScenesEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string initial = string.Join("\n", block.Scenes ?? new List<string>());
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockScenesTitle", "Edit memory-block scenes - ") + name, BuildDevCompressedMemoryBlockSubtitle(block), MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockScenesPrompt", "One scene per line; empty clears the scene list; keep at most 16 items."), initial, delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Scenes = ParseDevCompressedMemoryLineList(input, 16, ignoreCase: true);
			}, MemoryEditorUiText.T("AF_MEMORY_UI_BlockScenesUpdated", "Memory-block scene list updated."), editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void OpenDevCompressedMemoryBlockAfefEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string initial = string.Join("\n", block.AfefLines ?? new List<string>());
		DevTextEditorHelper.ShowLongTextEditor(MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockAfefTitle", "Edit memory-block AFEF - ") + name, BuildDevCompressedMemoryBlockSubtitle(block), MemoryEditorUiText.T("AF_MEMORY_UI_EditBlockAfefPrompt", "One AFEF per line; empty clears them; keep at most 80 items."), initial, delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.AfefLines = ParseDevCompressedMemoryLineList(input, 80, ignoreCase: false);
			}, MemoryEditorUiText.T("AF_MEMORY_UI_BlockAfefUpdated", "Memory-block AFEF lines updated."), editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, MemoryEditorUiText.T("AF_MEMORY_UI_Save", "Save"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"));
	}

	internal void ConfirmDevDeleteCompressedMemoryBlock(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string message = BuildDevCompressedMemoryBlockSubtitle(block) + MemoryEditorUiText.T("AF_MEMORY_UI_DeleteBlockBodyPrefix", "\n\nDelete this block from ") + name + MemoryEditorUiText.T("AF_MEMORY_UI_DeleteBlockBody", "'s compressed memory and reorganize the overview.\nThis action cannot be undone. Continue?");
		bool completed = false;
		InformationManager.ShowInquiry(new InquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDeleteBlock", "Confirm deleting compressed memory block"), message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDelete", "Delete"), MemoryEditorUiText.T("AF_MEMORY_UI_Cancel", "Cancel"), delegate
		{
			if (completed || !_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			completed = true;
			DeleteDevCompressedMemoryBlock(npc, blockId, returnPage, returnQuery, editorGeneration);
		}, delegate
		{
			if (completed || !_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			completed = true;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}), pauseGameActiveState: true);
	}

	internal string BuildDevCompressedMemoryRawText(Hero npc)
	{
		List<DailyMemoryDraft> drafts = _port.LoadDailyMemoryDrafts(npc);
		if (drafts == null || drafts.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		foreach (DailyMemoryDraft draft in drafts.OrderBy((DailyMemoryDraft x) => x.GameDayIndex))
		{
			string date = string.IsNullOrWhiteSpace(draft.GameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", draft.GameDayIndex.ToString()) : draft.GameDate.Trim();
			sb.AppendLine("【" + date + "】" + (draft.QueuedForSummary ? MemoryEditorUiText.T("AF_MEMORY_UI_QueuedParenthetical", " (queued for summary)") : ""));
			foreach (DailyMemoryLine line in draft.Lines ?? new List<DailyMemoryLine>())
			{
				string rendered = _port.BuildDailyMemoryLineForPrompt(line);
				if (!string.IsNullOrWhiteSpace(rendered))
				{
					sb.AppendLine(rendered);
				}
			}
			sb.AppendLine();
		}
		return sb.ToString().TrimEnd();
	}

	internal string BuildDevCompressedMemoryBlockText(Hero npc)
	{
		List<CompressedMemoryBlock> blocks = _port.LoadCompressedMemoryBlocks(npc);
		if (blocks == null || blocks.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		int i = 1;
		foreach (CompressedMemoryBlock block in blocks.OrderBy((CompressedMemoryBlock x) => x.GameDayIndex).ThenBy((CompressedMemoryBlock x) => x.StartHour))
		{
			string date = string.IsNullOrWhiteSpace(block.GameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", block.GameDayIndex.ToString()) : block.GameDate.Trim();
			sb.AppendLine(i + "# " + date + " " + _port.FormatMemoryHourRange(block.StartHour, block.EndHour) + " " + (block.RichTitle ?? ""));
			if (block.Scenes != null && block.Scenes.Count > 0)
			{
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_SceneLabel", "Scene: ") + string.Join(" / ", block.Scenes));
			}
			sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_ContentLabel", "Content: ") + (block.Summary ?? ""));
			if (block.AfefLines != null && block.AfefLines.Count > 0)
			{
				sb.AppendLine("AFEF：");
				foreach (string line in block.AfefLines)
				{
					sb.AppendLine(line);
				}
			}
			string triggerText = BuildDevWeeklyMemoryMaterialTriggerText(block.WeeklyMaterialTriggers);
			if (!string.IsNullOrWhiteSpace(triggerText))
			{
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_WeeklyMaterialLabel", "Weekly material: "));
				sb.AppendLine(triggerText);
			}
			sb.AppendLine();
			i++;
		}
		return sb.ToString().TrimEnd();
	}

	internal string BuildDevCompressedMemoryQueueText(Hero npc)
	{
		string heroId = _port.GetMemoryHeroId(npc);
		List<MemorySummaryJob> jobs = (_port.SummaryJobs() ?? new List<MemorySummaryJob>()).Where((MemorySummaryJob x) => x != null && string.Equals(_port.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemorySummaryJob x) => x.GameDayIndex).ToList();
		List<MemoryOverviewJob> overviewJobs = (_port.OverviewJobs() ?? new List<MemoryOverviewJob>()).Where((MemoryOverviewJob x) => x != null && string.Equals(_port.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemoryOverviewJob x) => x.TriggerGameDayIndex).ToList();
		if (jobs.Count <= 0 && overviewJobs.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		if (jobs.Count > 0)
		{
			sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_DailyCompressionHeader", "[Daily dialogue compression]"));
			foreach (MemorySummaryJob job in jobs)
			{
				string date = string.IsNullOrWhiteSpace(job.GameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", job.GameDayIndex.ToString()) : job.GameDate.Trim();
				sb.AppendLine(date + " retry=" + job.RetryCount + " error=" + BuildDevStoredErrorReference(job.LastError));
			}
		}
		if (overviewJobs.Count > 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_OverviewHeader", "[Memory overview]"));
			foreach (MemoryOverviewJob job in overviewJobs)
			{
				string date = string.IsNullOrWhiteSpace(job.TriggerGameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", job.TriggerGameDayIndex.ToString()) : job.TriggerGameDate.Trim();
				sb.AppendLine(date + " retry=" + job.RetryCount + " error=" + BuildDevStoredErrorReference(job.LastError));
			}
		}
		return sb.ToString().TrimEnd();
	}

	internal string BuildDevMemoryOverviewText(Hero npc)
	{
		string heroId = _port.GetMemoryHeroId(npc);
		MemoryOverviewState state = _port.GetMemoryOverviewState(heroId);
		List<MemoryOverviewJob> jobs = (_port.OverviewJobs() ?? new List<MemoryOverviewJob>()).Where((MemoryOverviewJob x) => x != null && string.Equals(_port.NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemoryOverviewJob x) => x.TriggerGameDayIndex).ToList();
		if ((state == null || (string.IsNullOrWhiteSpace(state.Summary) && string.IsNullOrWhiteSpace(state.LastError))) && jobs.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		if (state != null)
		{
			sb.AppendLine("HeroId：" + (state.HeroId ?? ""));
			sb.AppendLine("HeroName：" + (state.HeroName ?? ""));
			sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_IncludedBlockLabel", "Included memory blocks: ") + (state.IncludedBlockIds?.Count ?? 0));
			if (state.UpdatedUtcTicks > 0)
			{
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_UpdatedTicks", "Last update Ticks: ") + state.UpdatedUtcTicks);
			}
			if (!string.IsNullOrWhiteSpace(state.LastError))
			{
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_LastFailure", "Last failure: ") + BuildDevStoredErrorReference(state.LastError));
			}
			if (!string.IsNullOrWhiteSpace(state.Summary))
			{
				sb.AppendLine();
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_PastOverviewHeader", "[Past-memory overview]"));
				sb.AppendLine(state.Summary.Trim());
			}
			if (state.IncludedBlockIds != null && state.IncludedBlockIds.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_IncludedBlockIds", "Included block IDs:"));
				foreach (string id in state.IncludedBlockIds)
				{
					if (!string.IsNullOrWhiteSpace(id))
					{
						sb.AppendLine(id.Trim());
					}
				}
			}
		}
		if (jobs.Count > 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(MemoryEditorUiText.T("AF_MEMORY_UI_PendingUpdatesHeader", "[Pending updates]"));
			foreach (MemoryOverviewJob job in jobs)
			{
				string date = string.IsNullOrWhiteSpace(job.TriggerGameDate) ? MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", job.TriggerGameDayIndex.ToString()) : job.TriggerGameDate.Trim();
				sb.AppendLine(date + " retry=" + job.RetryCount + " error=" + BuildDevStoredErrorReference(job.LastError));
			}
		}
		return sb.ToString().TrimEnd();
	}

	internal void ConfirmDevClearCompressedMemory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		string name = npc.Name?.ToString() ?? "NPC";
		InformationManager.ShowInquiry(new InquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmClear", "Confirm clearing compressed memory"), MemoryEditorUiText.T("AF_MEMORY_UI_ClearBodyPrefix", "Delete ") + name + MemoryEditorUiText.T("AF_MEMORY_UI_ClearBodySuffix", "'s today history, pending summary queue, compressed blocks, and memory overview.\nThis action cannot be undone. Continue?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmClearButton", "Clear"), MemoryEditorUiText.T("AF_MEMORY_UI_Cancel", "Cancel"), delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			if (!_port.ClearDevCompressedMemoryData(npc, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_ClearedNewData", "This NPC's new compressed-memory data was cleared.")));
			OpenDevCompressedMemoryMenu(npc);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryMenu(npc);
		}), pauseGameActiveState: true);
	}

	internal void OpenDevHistorySearchInput(Hero npc)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string soundEventPath = HistoryQuery ?? string.Empty;
		InformationManager.ShowTextInquiry(new TextInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_SearchDialogueTitle", "Search dialogue history - ") + text, MemoryEditorUiText.T("AF_MEMORY_UI_SearchDialoguePrompt", "Enter keywords to fuzzy-match this NPC's dialogue history by content.\nLeave empty to keep the current search."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_Search", "Search"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			if (!string.IsNullOrWhiteSpace(input))
			{
				HistoryQuery = input.Trim();
			}
			OpenDevHistoryDateSelection(npc);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevHistoryDateSelection(npc);
		}, shouldInputBeObfuscated: false, null, soundEventPath));
	}

	internal void ConfirmDevClearAllDialogueHistory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		List<DialogueDay> list = _port.LoadDialogueHistory(npc);
		int num = 0;
		if (list != null)
		{
			foreach (DialogueDay item in list)
			{
				num += (item?.Lines?.Count).GetValueOrDefault();
			}
		}
		string arg = npc.Name?.ToString() ?? "NPC";
		string text = MemoryEditorUiText.F("AF_MEMORY_UI_DeleteDialogueBody", "Delete all dialogue history for {HERO} ({COUNT} lines).\nThis action cannot be undone. Continue?", "HERO", arg, "COUNT", num.ToString());
		InformationManager.ShowInquiry(new InquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDeleteDialogue", "Confirm deleting dialogue history"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, MemoryEditorUiText.T("AF_MEMORY_UI_ConfirmDelete", "Delete"), MemoryEditorUiText.T("AF_MEMORY_UI_Cancel", "Cancel"), delegate
		{
			if (!_port.ClearDevDialogueHistoryData(npc, editorGeneration)) return;
			HistoryQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_DialogueDeleted", "All dialogue history for this NPC was deleted.")));
			_port.ShowDevEditInquiry(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}));
	}

	internal void OpenDevHistoryDateSelection(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_port.SelectedHero = npc;
		List<DialogueDay> list = _port.LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoHistory", "This NPC has no dialogue history.")));
			_port.ShowDevEditInquiry(npc);
			return;
		}
		list = list.OrderBy((DialogueDay d) => d.GameDayIndex).ToList();
		string text = (HistoryQuery ?? string.Empty).Trim();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__search__", MemoryEditorUiText.T("AF_MEMORY_UI_SearchDialogue", "Search dialogue history"), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement("__clear_search__", MemoryEditorUiText.T("AF_MEMORY_UI_ClearSearch", "Clear search"), null));
		}
		list2.Add(new InquiryElement("__clear_all__", MemoryEditorUiText.T("AF_MEMORY_UI_DeleteAllDialogue", "Delete all dialogue history for this NPC"), null));
		list2.Add(new InquiryElement("back", MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), null));
		if (string.IsNullOrWhiteSpace(text))
		{
			foreach (DialogueDay item in list)
			{
				if (item != null && item.Lines != null && item.Lines.Count != 0)
				{
					string text2 = ((!string.IsNullOrEmpty(item.GameDate)) ? item.GameDate : MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", item.GameDayIndex.ToString()));
					string title = text2 + MemoryEditorUiText.F("AF_MEMORY_UI_LineCountSuffix", " ({COUNT} lines)", "COUNT", item.Lines.Count.ToString());
					list2.Add(new InquiryElement(item.GameDayIndex, title, null));
				}
			}
		}
		else
		{
			string value = text.ToLowerInvariant();
			foreach (DialogueDay item2 in list)
			{
				if (item2 == null || item2.Lines == null || item2.Lines.Count == 0)
				{
					continue;
				}
				string arg = ((!string.IsNullOrEmpty(item2.GameDate)) ? item2.GameDate : MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", item2.GameDayIndex.ToString()));
				for (int num = 0; num < item2.Lines.Count; num++)
				{
					string text3 = item2.Lines[num] ?? string.Empty;
					if (text3.ToLowerInvariant().Contains(value))
					{
						string arg2 = _port.BuildDevHistoryPreview(text3);
						string title2 = MemoryEditorUiText.F("AF_MEMORY_UI_HistoryLineTitle", "{DATE} / line {LINE}: {PREVIEW}", "DATE", arg, "LINE", (num + 1).ToString(), "PREVIEW", arg2);
						list2.Add(new InquiryElement(new Tuple<int, int>(item2.GameDayIndex, num), title2, null));
					}
				}
			}
		}
		string text4 = npc.Name?.ToString() ?? "NPC";
		string text5 = MemoryEditorUiText.T("AF_MEMORY_UI_SelectDate", "Select a date to view/edit.");
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + MemoryEditorUiText.T("AF_MEMORY_UI_CurrentSearch", "\nCurrent search: ") + _port.BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text5 += MemoryEditorUiText.T("AF_MEMORY_UI_NoMatches", "\n\nNo matching results.");
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueHistory", "Edit dialogue history - ") + text4, text5, list2, isExitShown: true, 0, 1, MemoryEditorUiText.T("AF_MEMORY_UI_NextStep", "Next"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), OnDevHistoryDateSelected, delegate
		{
			_port.ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevHistoryDateSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _port.SelectedHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				_port.ShowDevEditInquiry(devEditingHero);
				return;
			case "__search__":
				OpenDevHistorySearchInput(devEditingHero);
				return;
			case "__clear_search__":
				HistoryQuery = string.Empty;
				OpenDevHistoryDateSelection(devEditingHero);
				return;
			case "__clear_all__":
				ConfirmDevClearAllDialogueHistory(devEditingHero);
				return;
			}
		}
		if (selected[0].Identifier is Tuple<int, int> tuple)
		{
			OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			return;
		}
		if (!(selected[0].Identifier is int))
		{
			OpenDevHistoryDateSelection(devEditingHero);
			return;
		}
		int dayIndex = (int)selected[0].Identifier;
		OpenDevHistoryLineSelection(devEditingHero, dayIndex);
	}

	internal void OpenDevHistoryLineSelection(Hero npc, int dayIndex)
	{
		if (npc == null)
		{
			return;
		}
		_port.SelectedHero = npc;
		List<DialogueDay> list = _port.LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoHistory", "This NPC has no dialogue history.")));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || dialogueDay.Lines.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_NoDialogueLinesForDate", "There are no dialogue lines for this date.")));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", MemoryEditorUiText.T("AF_MEMORY_UI_BackToDates", "Back to date list"), null));
		list2.Add(new InquiryElement("search", MemoryEditorUiText.T("AF_MEMORY_UI_SearchNpcHistory", "Search this NPC's history"), null));
		for (int num = 0; num < dialogueDay.Lines.Count; num++)
		{
			string line = dialogueDay.Lines[num] ?? "";
			string arg = _port.BuildDevHistoryPreview(line);
			string title = $"{num + 1}. {arg}";
			list2.Add(new InquiryElement(new Tuple<int, int>(dayIndex, num), title, null));
		}
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : MemoryEditorUiText.F("AF_MEMORY_UI_Day", "Day {DAY}", "DAY", dialogueDay.GameDayIndex.ToString()));
		string text2 = npc.Name?.ToString() ?? "NPC";
		string text3 = MemoryEditorUiText.T("AF_MEMORY_UI_SelectDialogueLine", "Select a dialogue line to edit.");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(MemoryEditorUiText.T("AF_MEMORY_UI_EditDialogueLine", "Edit dialogue line - ") + text2, text3 + MemoryEditorUiText.T("AF_MEMORY_UI_CurrentDateNewline", "\nCurrent date: ") + text, list2, isExitShown: true, 0, 1, MemoryEditorUiText.T("AF_MEMORY_UI_Edit", "Edit"), MemoryEditorUiText.T("AF_MEMORY_UI_Back", "Back"), OnDevHistoryLineSelected, delegate
		{
			OpenDevHistoryDateSelection(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevHistoryLineSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _port.SelectedHero;
		if (devEditingHero != null)
		{
			if (selected == null || selected.Count == 0)
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else if (selected[0].Identifier is string text2 && text2 == "search")
			{
				OpenDevHistorySearchInput(devEditingHero);
			}
			else if (!(selected[0].Identifier is Tuple<int, int> tuple))
			{
				OpenDevHistoryDateSelection(devEditingHero);
			}
			else
			{
				OpenDevEditLine(devEditingHero, tuple.Item1, tuple.Item2);
			}
		}
	}

	internal void ApplyDevDailyMemoryLineMutation(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery, Action<DailyMemoryDraft, DailyMemoryLine> mutate, string successMessage)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(generation)) return;
		if (!_port.TryApplyDevDailyMemoryLineDataMutation(npc, dayIndex, lineIndex, mutate, generation))
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_RawLineNotFound", "The uncompressed memory line to edit could not be found.")));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(successMessage ?? MemoryEditorUiText.T("AF_MEMORY_UI_RawLineUpdated", "Uncompressed memory line updated.")));
		if (_port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex) == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}
		else if (_port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex) == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}
		else
		{
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}
	}

	internal void ApplyDevDailyMemoryDraftMutation(Hero npc, int dayIndex, Action<DailyMemoryDraft> mutate, string reason, string successMessage, int returnPage, string returnQuery, bool returnToDraftEditor)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(generation)) return;
		if (!_port.TryApplyDevDailyMemoryDraftDataMutation(npc, dayIndex, mutate, reason, generation))
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		if (!string.IsNullOrWhiteSpace(successMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(successMessage));
		}
		if (returnToDraftEditor && _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex) != null)
		{
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}
		else if (_port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex) != null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}
		else
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}
	}

	private bool IsDevDailyMemoryDraftMatch(DailyMemoryDraft draft, string[] terms)
		=> MemoryEditorProjection.IsDevDailyMemoryDraftMatch(_display, draft, terms);

	private string BuildDevDailyMemoryDraftSearchText(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSearchText(_display, draft);

	private string BuildDevDailyMemoryDraftListLabel(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftListLabel(_display, draft);

	private string BuildDevDailyMemoryDraftSubtitle(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSubtitle(_display, draft);

	private string BuildDevStoredErrorReference(string error)
		=> MemoryEditorProjection.BuildDevStoredErrorReference(_display, error);

	private string BuildDevDailyMemoryDraftEditorDescription(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftEditorDescription(_display, draft);

	private string BuildDevWeeklyMemoryMaterialTriggerText(IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
		=> MemoryEditorProjection.BuildDevWeeklyMemoryMaterialTriggerText(_display, triggers);

	private string BuildDevDailyMemoryLineListLabel(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineListLabel(_display, line);

	private string BuildDevDailyMemoryLineSubtitle(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineSubtitle(_display, line);

	private string BuildDevDailyMemoryLineDescription(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineDescription(_display, line);

	private int GetDefaultDevDailyMemoryLineHour(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineHour(_display, draft);

	private string GetDefaultDevDailyMemoryLineScene(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineScene(_display, draft);

	private CompressedMemoryBlock FindDevCompressedMemoryBlock(List<CompressedMemoryBlock> blocks, string blockId)
		=> MemoryEditorProjection.FindDevCompressedMemoryBlock(_display, blocks, blockId);

	private string GetDevCompressedMemoryBlockId(CompressedMemoryBlock block)
		=> MemoryEditorProjection.GetDevCompressedMemoryBlockId(_display, block);

	private string BuildDevCompressedMemoryBlockListLabel(CompressedMemoryBlock block, int displayIndex)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockListLabel(_display, block, displayIndex);

	private string BuildDevCompressedMemoryBlockSubtitle(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockSubtitle(_display, block);

	private string BuildDevCompressedMemoryBlockEditorBody(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockEditorBody(_display, block);

	private string[] SplitDevCompressedMemorySearchTerms(string query)
		=> MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(_display, query);

	private bool IsDevCompressedMemoryBlockMatch(CompressedMemoryBlock block, string[] terms)
		=> MemoryEditorProjection.IsDevCompressedMemoryBlockMatch(_display, block, terms);

	private string NormalizeDevCompressedMemoryMultilineInput(string input)
		=> MemoryEditorProjection.NormalizeDevCompressedMemoryMultilineInput(_display, input);

	private List<string> ParseDevCompressedMemoryLineList(string input, int maxCount, bool ignoreCase)
		=> MemoryEditorProjection.ParseDevCompressedMemoryLineList(_display, input, maxCount, ignoreCase);

 internal void ApplyDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string input, long generation)
 {
  if (!_port.IsMemorySourceEditorCurrent(generation) || npc == null) return;
  if (!_port.TryApplyDevDialogueHistoryLineDataMutation(npc, dayIndex, lineIndex, input, generation))
  {
   OpenDevHistoryLineSelection(npc, dayIndex);
   return;
  }
  InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_DialogueLineUpdated", "Dialogue line updated.")));
  OpenDevHistoryLineSelection(npc, dayIndex);
 }
	internal void DeleteDevCompressedMemoryBlock(Hero npc, string blockId, int returnPage, string returnQuery, long generation)
	{
		if (npc == null || !_port.IsMemorySourceEditorCurrent(generation))
		{
			return;
		}
		bool removed = _port.DeleteDevCompressedMemoryBlockData(npc, blockId, generation);
		if (removed)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_BlockDeleted", "Compressed memory block deleted.")));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_BlockDeleteNotFound", "The compressed memory block to delete could not be found.")));
		}
		OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
	}

	internal void ApplyDevCompressedMemoryBlockMutation(Hero npc, string blockId, int returnPage, string returnQuery, Action<CompressedMemoryBlock> mutate, string successMessage, long generation)
	{
		if (npc == null || !_port.IsMemorySourceEditorCurrent(generation))
		{
			return;
		}
		bool updated = _port.TryApplyDevCompressedMemoryBlockDataMutation(npc, blockId, mutate, generation);
		if (!updated)
		{
			InformationManager.DisplayMessage(new InformationMessage(MemoryEditorUiText.T("AF_MEMORY_UI_BlockNotFound", "The compressed memory block to edit could not be found.")));
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(successMessage ?? MemoryEditorUiText.T("AF_MEMORY_UI_BlockUpdated", "Compressed memory block updated.")));
		if (FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId) == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
		}
		else
		{
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}
	}

}
