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
 internal delegate void ShowDevLargeTextOrInquiryCapability(string title, string subtitle, string body, Action onClose, string closeText = "返回");
 internal ShowDevLargeTextOrInquiryCapability ShowDevLargeTextOrInquiry;
 internal delegate void ShowDevLargeSelectionOrInquiryCapability(string title, string subtitle, string body, List<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string affirmativeText = "进入", string cancelText = "返回");
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || lineIndex < 0 || lineIndex >= dialogueDay.Lines.Count)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的对话行。"));
			OpenDevHistoryLineSelection(npc, dayIndex);
			return;
		}
		string currentValue = dialogueDay.Lines[lineIndex] ?? "";
		string displayDate = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text = npc.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑对话行 - " + text, "当前日期: " + displayDate, "下方输入框可直接修改整条内容，留空则删除该行。", currentValue, delegate(string input)
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
		string titleText = "编辑对话行 - " + text;
		string text2 = "当前日期: " + displayDate + "\n原内容已载入下方输入框，可直接编辑。\n留空则删除该行。";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
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
		body.AppendLine("新压缩记忆体系数据：");
		body.AppendLine("今日/待总结原始历史：" + (drafts?.Count ?? 0) + " 天，" + rawLineCount + " 行");
		body.AppendLine("压缩记忆块：" + (blocks?.Count ?? 0) + " 块");
		body.AppendLine("待总结队列：" + queueCount + " 项");
		body.AppendLine("记忆大总结：" + ((overviewState != null && !string.IsNullOrWhiteSpace(overviewState.Summary)) ? ("已生成，纳入 " + (overviewState.IncludedBlockIds?.Count ?? 0) + " 块") : "未生成") + "；待更新队列：" + overviewQueueCount + " 项");
		body.AppendLine();
		body.AppendLine("旧 _dialogueHistory_v2 不再参与主链路记忆注入。");
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("raw", "查看今日/待总结原始历史", null),
			new InquiryElement("edit_raw", "编辑今日/待总结原始历史", null),
			new InquiryElement("blocks", "查看压缩记忆块", null),
			new InquiryElement("edit_blocks", "编辑压缩记忆块", null),
			new InquiryElement("overview", "查看记忆大总结", null),
			new InquiryElement("edit_overview", "编辑记忆大总结", null),
			new InquiryElement("queue", "查看待总结队列", null),
			new InquiryElement("clear", "清空该NPC新记忆数据", null),
			new InquiryElement("process", "手动触发总结队列", null),
			new InquiryElement("back", "返回", null)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("压缩记忆管理 - " + name, body.ToString().TrimEnd(), list, isExitShown: true, 0, 1, "执行", "返回", delegate(List<InquiryElement> selected)
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
			ShowDevCompressedMemoryText(npc, "原始历史", BuildDevCompressedMemoryRawText(npc));
			break;
		case "edit_raw":
			OpenDevDailyMemoryDraftList(npc, 0, DailyQuery);
			break;
		case "blocks":
			ShowDevCompressedMemoryText(npc, "压缩记忆块", BuildDevCompressedMemoryBlockText(npc));
			break;
		case "edit_blocks":
			OpenDevCompressedMemoryBlockList(npc, 0, CompressedQuery);
			break;
		case "overview":
			ShowDevCompressedMemoryText(npc, "记忆大总结", BuildDevMemoryOverviewText(npc));
			break;
		case "edit_overview":
			OpenDevMemoryOverviewEditor(npc);
			break;
		case "queue":
			ShowDevCompressedMemoryText(npc, "待总结队列", BuildDevCompressedMemoryQueueText(npc));
			break;
		case "clear":
			ConfirmDevClearCompressedMemory(npc);
			break;
		case "process":
			_port.TrySealPastDailyMemoryDrafts();
			_port.QueueAllMemoryOverviewCandidatesForDeferredScan();
			_port.ProcessMemoryOverviewCandidateScanBudget(0L, double.MaxValue);
			_port.TryStartMemorySummaryQueue();
			InformationManager.DisplayMessage(new InformationMessage("已触发压缩记忆总结队列检查。"));
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
		_port.ShowDevLargeTextOrInquiry("压缩记忆管理 - " + title + " - " + name, "", string.IsNullOrWhiteSpace(text) ? "（无数据）" : text.Trim(), delegate
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有未压缩原始历史。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<DailyMemoryDraft> view = OpenDailyPage(drafts, page, q, _display);
		List<DailyMemoryDraft> filtered = view.Matches;
		const int pageSize = 40;
		int pageCount = view.PageCount;
		page = view.Page;
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>();
		options.Add(new DevLargeSelectionPopup.Option("__search__", "搜索未压缩记忆", "按日期、场景、说话人、正文或 AFEF 过滤。", isPrimary: true));
		if (!string.IsNullOrWhiteSpace(q))
		{
			options.Add(new DevLargeSelectionPopup.Option("__clear__", "清空搜索", "恢复显示全部未压缩记忆。"));
		}
		if (page > 0)
		{
			options.Add(new DevLargeSelectionPopup.Option("__prev__", "上一页", "查看前一页结果。"));
		}
		if (page + 1 < pageCount)
		{
			options.Add(new DevLargeSelectionPopup.Option("__next__", "下一页", "查看后一页结果。"));
		}
		foreach (DailyMemoryDraft draft in filtered.Skip(page * pageSize).Take(pageSize))
		{
			string date = string.IsNullOrWhiteSpace(draft.GameDate) ? ("第" + draft.GameDayIndex + "日") : draft.GameDate.Trim();
			string detail = _port.BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 260);
			string meta = "行数：" + ((draft.Lines?.Count).GetValueOrDefault()) + "；已入队：" + (draft.QueuedForSummary ? "是" : "否") + (string.IsNullOrWhiteSpace(draft.LastSummaryError) ? "" : "；最近错误：" + BuildDevStoredErrorReference(draft.LastSummaryError));
			options.Add(new DevLargeSelectionPopup.Option("day:" + draft.GameDayIndex, date, detail, meta));
		}
		string name = npc.Name?.ToString() ?? "NPC";
		string descriptionText = "选择一天未压缩原始历史进行编辑。\n原始历史：" + drafts.Count + " 天；当前结果：" + filtered.Count + " 天；第 " + (page + 1) + "/" + pageCount + " 页。";
		if (!string.IsNullOrWhiteSpace(q))
		{
			descriptionText += "\n当前搜索：" + _port.BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += "\n\n没有匹配结果。";
		}
		_port.ShowDevLargeSelectionOrInquiry("编辑未压缩记忆 - " + name, "未压缩原始历史天数列表", descriptionText, options, delegate(string selectedId)
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
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索未压缩记忆", "输入关键词，可匹配日期、场景、说话人、正文或 AFEF。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
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
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆。"));
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(draft);
		string name = npc.Name?.ToString() ?? "NPC";
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("lines", "编辑原始行", "查看并编辑该日每一条未压缩记忆。", isPrimary: true),
			new DevLargeSelectionPopup.Option("add_normal", "新增普通记忆行", "新增一条会参与 LLM 对话记忆的原始行。"),
			new DevLargeSelectionPopup.Option("add_afef", "新增AFEF行", "新增一条 AFEF 机制行，不参与 LLM 对话压缩正文。"),
			new DevLargeSelectionPopup.Option("delete", "删除该日未压缩记忆", "删除该日全部未压缩原始历史，并移除同日待总结队列。", isDanger: true),
			new DevLargeSelectionPopup.Option("back", "返回列表")
		};
		_port.ShowDevLargeSelectionOrInquiry("未压缩记忆 - " + name, BuildDevDailyMemoryDraftSubtitle(draft), BuildDevDailyMemoryDraftEditorDescription(draft), options, delegate(string selectedId)
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
			new DevLargeSelectionPopup.Option("__add_normal__", "新增普通记忆行", "新增一条会参与 LLM 对话记忆的原始行。", isPrimary: true),
			new DevLargeSelectionPopup.Option("__add_afef__", "新增AFEF行", "新增一条 AFEF 机制行。"),
			new DevLargeSelectionPopup.Option("__back__", "返回该日菜单")
		};
		for (int i = 0; i < lines.Count; i++)
		{
			DailyMemoryLine line = lines[i];
			string type = line.IsAfef ? "AFEF" : (line.IsLlmDialogue ? "LLM" : "普通");
			string speaker = string.IsNullOrWhiteSpace(line.Speaker) ? (line.IsAfef ? "AFEF" : "手动") : line.Speaker.Trim();
			string scene = string.IsNullOrWhiteSpace(line.Scene) ? "未知场景" : _port.BuildDevHistoryPreview(line.Scene, 42);
			string title = (i + 1) + ". " + MBMath.ClampInt(line.GameHour, 0, 23) + "时 | " + type + " | " + speaker + " | " + scene;
			string detail = string.IsNullOrWhiteSpace(line.Text) ? "（空）" : _port.BuildDevHistoryPreview(line.Text, 260);
			options.Add(new DevLargeSelectionPopup.Option("line:" + i, title, detail));
		}
		string name = npc?.Name?.ToString() ?? "NPC";
		string body = BuildDevDailyMemoryDraftSubtitle(draft) + "\n\n当前共 " + lines.Count + " 行。选择右侧条目进入行详情。";
		_port.ShowDevLargeSelectionOrInquiry("编辑未压缩记忆行 - " + name, "未压缩原始历史行", body, options, delegate(string selectedId)
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
		}, "编辑", "返回");
	}

	internal void OpenDevDailyMemoryLineEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆行。"));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(line);
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("text", "编辑正文", "打开大文本编辑器修改正文；留空会删除该行。", isPrimary: true),
			new DevLargeSelectionPopup.Option("speaker", "编辑说话人", "修改该行显示和同步使用的说话人。"),
			new DevLargeSelectionPopup.Option("scene", "编辑场景", "修改该行所属场景。"),
			new DevLargeSelectionPopup.Option("hour", "编辑小时", "修改该行游戏时间小时。"),
			new DevLargeSelectionPopup.Option("toggle_afef", line.IsAfef ? "改为普通行" : "改为AFEF行"),
			new DevLargeSelectionPopup.Option("toggle_llm", line.IsLlmDialogue ? "取消LLM对话标记" : "标记为LLM对话"),
			new DevLargeSelectionPopup.Option("delete", "删除该行", "删除后会同步移除对应旧对话历史行。", isDanger: true),
			new DevLargeSelectionPopup.Option("back", "返回行列表")
		};
		string name = npc?.Name?.ToString() ?? "NPC";
		string body = "正文：\n" + (string.IsNullOrWhiteSpace(line.Text) ? "（空）" : line.Text.Trim());
		_port.ShowDevLargeSelectionOrInquiry("未压缩记忆行 - " + name, BuildDevDailyMemoryLineSubtitle(line), body, options, delegate(string selectedId)
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
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆正文 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入新的正文；留空=删除该行。", line.Text ?? "", delegate(string input)
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
			}, "未压缩记忆正文已更新。");
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆说话人 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入说话人；留空=自动使用默认说话人。", line.Speaker ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Speaker = (input ?? "").Trim();
			}, "未压缩记忆说话人已更新。");
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆场景 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入场景；留空=未知场景。", line.Scene ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Scene = (input ?? "").Trim();
			}, "未压缩记忆场景已更新。");
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
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
		InformationManager.ShowTextInquiry(new TextInquiryData("编辑未压缩记忆小时", BuildDevDailyMemoryLineSubtitle(line) + "\n请输入 0~23 的整数。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			if (!int.TryParse((input ?? "").Trim(), out var hour) || hour < 0 || hour > 23)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入 0~23 的整数。"));
				OpenDevDailyMemoryLineHourEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
				return;
			}
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.GameHour = hour;
			}, "未压缩记忆小时已更新。");
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
		}, "未压缩记忆行类型已更新。");
	}

	internal void ToggleDevDailyMemoryLineLlm(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		DailyMemoryLine line = _port.FindDevDailyMemoryLine(_port.LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line != null && line.IsAfef)
		{
			InformationManager.DisplayMessage(new InformationMessage("AFEF 行不能标记为 LLM 对话。"));
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
			return;
		}
		ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
		{
			target.IsLlmDialogue = !target.IsLlmDialogue;
		}, "未压缩记忆 LLM 标记已更新。");
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
		DevTextEditorHelper.ShowLongTextEditor(isAfef ? ("新增AFEF行 - " + name) : ("新增普通记忆行 - " + name), BuildDevDailyMemoryDraftSubtitle(draft), "请输入新增行正文；留空=取消。", "", delegate(string input)
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
			InformationManager.DisplayMessage(new InformationMessage("已新增未压缩记忆行。"));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}, "保存", "返回");
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
		_port.ShowDevLargeConfirmOrInquiry("确认删除未压缩记忆行", BuildDevDailyMemoryLineSubtitle(line), "正文：\n" + (string.IsNullOrWhiteSpace(line.Text) ? "（空）" : line.Text.Trim()) + "\n\n此操作不可撤销，是否继续？", "确认删除", "取消", delegate
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
			}, "delete_line", "已删除未压缩记忆行。", returnPage, returnQuery, returnToDraftEditor: false);
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
		_port.ShowDevLargeConfirmOrInquiry("确认删除未压缩记忆", BuildDevDailyMemoryDraftSubtitle(draft), "将删除该日全部未压缩原始历史，并移除同日待总结队列。\n此操作不可撤销，是否继续？", "确认删除", "取消", delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, _port.FindDevDailyMemoryDraft(_port.LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			if (!_port.DeleteDevDailyMemoryDraftData(npc, dayIndex, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("已删除该日未压缩记忆。"));
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
		string subtitle = "当前压缩记忆块：" + (blocks?.Count ?? 0) + " 块；已纳入：" + (state?.IncludedBlockIds?.Count ?? 0) + " 块。\n保存非空内容后，会把当前所有压缩记忆块标记为已纳入，避免被待更新队列立即覆盖。";
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆大总结 - " + name, subtitle, "请输入新的过往记忆总览；留空=清空并等待重新生成。", state?.Summary ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !string.Equals(heroId, _port.GetMemoryHeroId(npc), StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(new { Overview = _port.GetMemoryOverviewState(heroId), Blocks = _port.LoadCompressedMemoryBlocks(npc) }), StringComparison.Ordinal)) return;
			ApplyDevMemoryOverviewInput(npc, input);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryMenu(npc);
		}, "保存", "返回");
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
			InformationManager.DisplayMessage(new InformationMessage("已清空记忆大总结，并重新检查待整理队列。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		Logger.Log("MemoryOverview", "manual_overview_save hero=" + heroId + " blocks=" + includedCount);
		InformationManager.DisplayMessage(new InformationMessage("记忆大总结已更新。"));
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有压缩记忆块。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<Tuple<int, CompressedMemoryBlock>> view = OpenCompressedPage(blocks, page, q, _display);
		List<Tuple<int, CompressedMemoryBlock>> filtered = view.Matches;
		const int pageSize = 40;
		int pageCount = view.PageCount;
		page = view.Page;
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>();
		options.Add(new DevLargeSelectionPopup.Option("__search__", "搜索压缩记忆块", isPrimary: true));
		if (!string.IsNullOrWhiteSpace(q))
		{
			options.Add(new DevLargeSelectionPopup.Option("__clear__", "清空搜索"));
		}
		if (page > 0)
		{
			options.Add(new DevLargeSelectionPopup.Option("__prev__", "上一页"));
		}
		if (page + 1 < pageCount)
		{
			options.Add(new DevLargeSelectionPopup.Option("__next__", "下一页"));
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
		string descriptionText = "选择一个压缩记忆块进入编辑选项。\n压缩记忆块：" + blocks.Count + " 块；当前结果：" + filtered.Count + " 块；第 " + (page + 1) + "/" + pageCount + " 页。";
		if (!string.IsNullOrWhiteSpace(q))
		{
			descriptionText += "\n当前搜索：" + _port.BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += "\n\n没有匹配结果。";
		}
		_port.ShowDevLargeSelectionOrInquiry("编辑压缩记忆块 - " + name, "压缩记忆块列表", descriptionText, options, delegate(string selectedId)
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
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索压缩记忆块", "输入关键词，可匹配标题、正文、场景、AFEF、日期或记忆块ID。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
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
		}, "进入", "返回");
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
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的压缩记忆块。"));
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = _port.ComputeMemorySummaryFingerprint(block);
		string name = npc.Name?.ToString() ?? "NPC";
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("summary", "编辑正文", "打开大编辑框修改记忆块正文；留空=清空正文。", isPrimary: true),
			new DevLargeSelectionPopup.Option("title", "编辑标题", "打开大编辑框修改富标题；留空=清空标题。"),
			new DevLargeSelectionPopup.Option("scenes", "编辑场景列表", "打开大编辑框逐行修改场景；最多保留16项。"),
			new DevLargeSelectionPopup.Option("afef", "编辑AFEF行", "打开大编辑框逐行修改 AFEF；最多保留80项。"),
			new DevLargeSelectionPopup.Option("delete", "删除该记忆块", "删除后会清空该NPC的记忆大总结并重新排队整理。", isDanger: true),
			new DevLargeSelectionPopup.Option("back", "返回列表")
		};
		_port.ShowDevLargeSelectionOrInquiry("记忆块 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), BuildDevCompressedMemoryBlockEditorBody(block), options, delegate(string selectedId)
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
		}, "进入", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块标题 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "请输入新的富标题；留空=清空标题。", block.RichTitle ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.RichTitle = (input ?? "").Trim();
			}, "记忆块标题已更新。", editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块正文 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "请输入新的记忆正文；留空=清空正文。", block.Summary ?? "", delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Summary = NormalizeDevCompressedMemoryMultilineInput(input);
			}, "记忆块正文已更新。", editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块场景 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "每行一个场景；留空=清空场景列表；最多保留16项。", initial, delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Scenes = ParseDevCompressedMemoryLineList(input, 16, ignoreCase: true);
			}, "记忆块场景列表已更新。", editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
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
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块AFEF - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "每行一条 AFEF；留空=清空；最多保留80项。", initial, delegate(string input)
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(_port.LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, _port.ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.AfefLines = ParseDevCompressedMemoryLineList(input, 80, ignoreCase: false);
			}, "记忆块 AFEF 行已更新。", editorGeneration);
		}, delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
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
		string message = BuildDevCompressedMemoryBlockSubtitle(block) + "\n\n将从 " + name + " 的压缩记忆中删除该块，并使记忆大总结重新整理。\n此操作不可撤销，是否继续？";
		bool completed = false;
		InformationManager.ShowInquiry(new InquiryData("确认删除压缩记忆块", message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认删除", "取消", delegate
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
			string date = string.IsNullOrWhiteSpace(draft.GameDate) ? ("第" + draft.GameDayIndex + "日") : draft.GameDate.Trim();
			sb.AppendLine("【" + date + "】" + (draft.QueuedForSummary ? "（已入总结队列）" : ""));
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
			string date = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
			sb.AppendLine(i + "# " + date + " " + _port.FormatMemoryHourRange(block.StartHour, block.EndHour) + " " + (block.RichTitle ?? ""));
			if (block.Scenes != null && block.Scenes.Count > 0)
			{
				sb.AppendLine("场景：" + string.Join(" / ", block.Scenes));
			}
			sb.AppendLine("内容：" + (block.Summary ?? ""));
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
				sb.AppendLine("周报素材：");
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
			sb.AppendLine("【日结对话压缩】");
			foreach (MemorySummaryJob job in jobs)
			{
				string date = string.IsNullOrWhiteSpace(job.GameDate) ? ("第" + job.GameDayIndex + "日") : job.GameDate.Trim();
				sb.AppendLine(date + " retry=" + job.RetryCount + " error=" + BuildDevStoredErrorReference(job.LastError));
			}
		}
		if (overviewJobs.Count > 0)
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine("【记忆大总结】");
			foreach (MemoryOverviewJob job in overviewJobs)
			{
				string date = string.IsNullOrWhiteSpace(job.TriggerGameDate) ? ("第" + job.TriggerGameDayIndex + "日") : job.TriggerGameDate.Trim();
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
			sb.AppendLine("已纳入记忆块：" + (state.IncludedBlockIds?.Count ?? 0));
			if (state.UpdatedUtcTicks > 0)
			{
				sb.AppendLine("最近更新时间Ticks：" + state.UpdatedUtcTicks);
			}
			if (!string.IsNullOrWhiteSpace(state.LastError))
			{
				sb.AppendLine("最近失败：" + BuildDevStoredErrorReference(state.LastError));
			}
			if (!string.IsNullOrWhiteSpace(state.Summary))
			{
				sb.AppendLine();
				sb.AppendLine("【过往记忆总览】");
				sb.AppendLine(state.Summary.Trim());
			}
			if (state.IncludedBlockIds != null && state.IncludedBlockIds.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("纳入块ID：");
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
			sb.AppendLine("【待更新队列】");
			foreach (MemoryOverviewJob job in jobs)
			{
				string date = string.IsNullOrWhiteSpace(job.TriggerGameDate) ? ("第" + job.TriggerGameDayIndex + "日") : job.TriggerGameDate.Trim();
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
		InformationManager.ShowInquiry(new InquiryData("确认清空压缩记忆", "将删除 " + name + " 的今日历史、待总结队列、压缩记忆块和记忆大总结。\n此操作不可撤销，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认清空", "取消", delegate
		{
			if (!_port.IsMemorySourceEditorCurrent(editorGeneration)) return;
			if (!_port.ClearDevCompressedMemoryData(npc, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的新压缩记忆数据。"));
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
		InformationManager.ShowTextInquiry(new TextInquiryData("搜索对话历史 - " + text, "输入关键词，按内容模糊匹配该 NPC 的全部对话历史。\n留空将不改变当前搜索。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
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
		string text = $"将删除 {arg} 的全部对话历史（共 {num} 条）。\n此操作不可撤销，是否继续？";
		InformationManager.ShowInquiry(new InquiryData("确认删除对话历史", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认删除", "取消", delegate
		{
			if (!_port.ClearDevDialogueHistoryData(npc, editorGeneration)) return;
			HistoryQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage("已删除该NPC的全部对话历史。"));
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			_port.ShowDevEditInquiry(npc);
			return;
		}
		list = list.OrderBy((DialogueDay d) => d.GameDayIndex).ToList();
		string text = (HistoryQuery ?? string.Empty).Trim();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("__search__", "搜索对话历史", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list2.Add(new InquiryElement("__clear_search__", "清空搜索", null));
		}
		list2.Add(new InquiryElement("__clear_all__", "一键删除该NPC全部对话历史", null));
		list2.Add(new InquiryElement("back", "返回", null));
		if (string.IsNullOrWhiteSpace(text))
		{
			foreach (DialogueDay item in list)
			{
				if (item != null && item.Lines != null && item.Lines.Count != 0)
				{
					string text2 = ((!string.IsNullOrEmpty(item.GameDate)) ? item.GameDate : $"第 {item.GameDayIndex} 日");
					string title = text2 + $" (共 {item.Lines.Count} 条)";
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
				string arg = ((!string.IsNullOrEmpty(item2.GameDate)) ? item2.GameDate : $"第 {item2.GameDayIndex} 日");
				for (int num = 0; num < item2.Lines.Count; num++)
				{
					string text3 = item2.Lines[num] ?? string.Empty;
					if (text3.ToLowerInvariant().Contains(value))
					{
						string arg2 = _port.BuildDevHistoryPreview(text3);
						string title2 = $"{arg} / 第{num + 1}条: {arg2}";
						list2.Add(new InquiryElement(new Tuple<int, int>(item2.GameDayIndex, num), title2, null));
					}
				}
			}
		}
		string text4 = npc.Name?.ToString() ?? "NPC";
		string text5 = "请选择要查看/编辑的日期。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			text5 = text5 + "\n当前搜索：" + _port.BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text5 += "\n\n没有匹配结果。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话历史 - " + text4, text5, list2, isExitShown: true, 0, 1, "下一步", "返回", OnDevHistoryDateSelected, delegate
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		DialogueDay dialogueDay = list.FirstOrDefault((DialogueDay d) => d.GameDayIndex == dayIndex);
		if (dialogueDay == null || dialogueDay.Lines == null || dialogueDay.Lines.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该日期下没有对话行。"));
			OpenDevHistoryDateSelection(npc);
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回日期列表", null));
		list2.Add(new InquiryElement("search", "搜索该NPC历史", null));
		for (int num = 0; num < dialogueDay.Lines.Count; num++)
		{
			string line = dialogueDay.Lines[num] ?? "";
			string arg = _port.BuildDevHistoryPreview(line);
			string title = $"{num + 1}. {arg}";
			list2.Add(new InquiryElement(new Tuple<int, int>(dayIndex, num), title, null));
		}
		string text = ((!string.IsNullOrEmpty(dialogueDay.GameDate)) ? dialogueDay.GameDate : $"第 {dialogueDay.GameDayIndex} 日");
		string text2 = npc.Name?.ToString() ?? "NPC";
		string text3 = "请选择要编辑的对话行。";
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话行 - " + text2, text3 + "\n当前日期: " + text, list2, isExitShown: true, 0, 1, "编辑", "返回", OnDevHistoryLineSelected, delegate
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
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆行。"));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(successMessage ?? "未压缩记忆行已更新。"));
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
  InformationManager.DisplayMessage(new InformationMessage("对话行已更新."));
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
			InformationManager.DisplayMessage(new InformationMessage("已删除压缩记忆块。"));
		}
		else
		{
			InformationManager.DisplayMessage(new InformationMessage("未找到要删除的压缩记忆块。"));
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
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的压缩记忆块。"));
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(successMessage ?? "压缩记忆块已更新。"));
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
