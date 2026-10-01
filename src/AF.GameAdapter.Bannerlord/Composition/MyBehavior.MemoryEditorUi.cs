using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
 private MemoryEditorController _memoryEditor;
 private MemoryEditorController MemoryEditor
 {
  get
  {
   var owner = _memoryEditor ?? (_memoryEditor = new MemoryEditorController());
   owner.SynchronizeGeneration(SaveRuntimeGuard.CaptureGeneration());
   return owner;
  }
 }

 private static readonly MemoryEditorDisplayPort MemoryEditorDisplay = new MemoryEditorDisplayPort
 {
  BuildDailyMemoryLineForPrompt = BuildDailyMemoryLineForPrompt,
  SanitizeWeeklyMemoryMaterialTriggers = SanitizeWeeklyMemoryMaterialTriggers,
  BuildWeeklyMemoryMaterialTagLabel = BuildWeeklyMemoryMaterialTagLabel,
  BuildCompressedMemoryBlockId = BuildCompressedMemoryBlockId,
  FormatMemoryHourRange = FormatMemoryHourRange,
  BuildDevHistoryPreview = BuildDevHistoryPreview,
  GetCurrentGameDayIndexSafe = GetCurrentGameDayIndexSafe,
  GetCurrentHourOfDaySafeForPrompt = GetCurrentHourOfDaySafeForPrompt,
  ResolveCurrentMemorySceneLabel = ResolveCurrentMemorySceneLabel
 };

	private string _devHistorySearchQuery { get => MemoryEditor.HistoryQuery; set => MemoryEditor.HistoryQuery = value; }

	private string _devDailyMemorySearchQuery { get => MemoryEditor.DailyQuery; set => MemoryEditor.DailyQuery = value; }

	private string _devCompressedMemorySearchQuery { get => MemoryEditor.CompressedQuery; set => MemoryEditor.CompressedQuery = value; }

	private int _devDailyMemoryDraftPage { get => MemoryEditor.DailyPage; set => MemoryEditor.DailyPage = value; }

	private int _devCompressedMemoryBlockPage { get => MemoryEditor.CompressedPage; set => MemoryEditor.CompressedPage = value; }

	private void OpenDevEditLine(Hero npc, int dayIndex, int lineIndex)
	{
		if (npc == null)
		{
			return;
		}
		List<DialogueDay> list = LoadDialogueHistory(npc);
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
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		});
	}

	private void OpenDevEditLineInput(Hero npc, int dayIndex, int lineIndex, string currentValue, string displayDate)
	{
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string titleText = "编辑对话行 - " + text;
		string text2 = "当前日期: " + displayDate + "\n原内容已载入下方输入框，可直接编辑。\n留空则删除该行。";
		InformationManager.ShowTextInquiry(new TextInquiryData(titleText, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			ApplyDevEditLineInput(npc, dayIndex, lineIndex, input);
		}, delegate
		{
			OpenDevHistoryLineSelection(npc, dayIndex);
		}, shouldInputBeObfuscated: false, null, "", currentValue ?? ""));
	}

	private void OpenDevCompressedMemoryMenu(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		_devEditingHero = npc;
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDrafts(npc);
		List<CompressedMemoryBlock> blocks = LoadCompressedMemoryBlocks(npc);
		string heroId = GetMemoryHeroId(npc);
		int queueCount = (_memorySummaryQueue ?? new List<MemorySummaryJob>()).Count((MemorySummaryJob x) => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
		int overviewQueueCount = (_memoryOverviewQueue ?? new List<MemoryOverviewJob>()).Count((MemoryOverviewJob x) => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase));
		MemoryOverviewState overviewState = GetMemoryOverviewState(heroId);
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
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
			OnDevCompressedMemoryMenuSelected(selected);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevCompressedMemoryMenuSelected(List<InquiryElement> selected)
	{
		Hero npc = _devEditingHero;
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
			OpenDevDailyMemoryDraftList(npc, 0, _devDailyMemorySearchQuery);
			break;
		case "blocks":
			ShowDevCompressedMemoryText(npc, "压缩记忆块", BuildDevCompressedMemoryBlockText(npc));
			break;
		case "edit_blocks":
			OpenDevCompressedMemoryBlockList(npc, 0, _devCompressedMemorySearchQuery);
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
			TrySealPastDailyMemoryDrafts();
			QueueAllMemoryOverviewCandidatesForDeferredScan();
			ProcessMemoryOverviewCandidateScanBudget(0L, double.MaxValue);
			TryStartMemorySummaryQueue();
			InformationManager.DisplayMessage(new InformationMessage("已触发压缩记忆总结队列检查。"));
			OpenDevCompressedMemoryMenu(npc);
			break;
		default:
			ShowDevEditInquiry(npc);
			break;
		}
	}

	private void ShowDevCompressedMemoryText(Hero npc, string title, string text)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		string name = npc?.Name?.ToString() ?? "NPC";
		ShowDevLargeTextOrInquiry("压缩记忆管理 - " + title + " - " + name, "", string.IsNullOrWhiteSpace(text) ? "（无数据）" : text.Trim(), delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		});
	}

	private void OpenDevDailyMemoryDraftList(Hero npc, int page, string query)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		_devEditingHero = npc;
		page = Math.Max(0, page);
		string q = (query ?? "").Trim();
		_devDailyMemorySearchQuery = q;
		_devDailyMemoryDraftPage = page;
		List<DailyMemoryDraft> drafts = SanitizeDailyMemoryDrafts(LoadDailyMemoryDrafts(npc));
		if (drafts == null || drafts.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有未压缩原始历史。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<DailyMemoryDraft> view = MemoryEditor.OpenDailyPage(drafts, page, q, MemoryEditorDisplay);
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
			string detail = BuildDevHistoryPreview((draft.Lines ?? new List<DailyMemoryLine>()).Select((DailyMemoryLine x) => x?.Text).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)), 260);
			string meta = "行数：" + ((draft.Lines?.Count).GetValueOrDefault()) + "；已入队：" + (draft.QueuedForSummary ? "是" : "否") + (string.IsNullOrWhiteSpace(draft.LastSummaryError) ? "" : "；最近错误：" + BuildDevStoredErrorReference(draft.LastSummaryError));
			options.Add(new DevLargeSelectionPopup.Option("day:" + draft.GameDayIndex, date, detail, meta));
		}
		string name = npc.Name?.ToString() ?? "NPC";
		string descriptionText = "选择一天未压缩原始历史进行编辑。\n原始历史：" + drafts.Count + " 天；当前结果：" + filtered.Count + " 天；第 " + (page + 1) + "/" + pageCount + " 页。";
		if (!string.IsNullOrWhiteSpace(q))
		{
			descriptionText += "\n当前搜索：" + BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += "\n\n没有匹配结果。";
		}
		ShowDevLargeSelectionOrInquiry("编辑未压缩记忆 - " + name, "未压缩原始历史天数列表", descriptionText, options, delegate(string selectedId)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
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
					if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
					OpenDevDailyMemoryDraftList(npc, 0, input);
				}, delegate
				{
					if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
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
				if (TryParseDevSelectionInt(selectedId, "day:", out var dayIndex))
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
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		});
	}

	private void OpenDevDailyMemoryDraftEditor(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		_devEditingHero = npc;
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆。"));
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(draft);
		string name = npc.Name?.ToString() ?? "NPC";
		List<DevLargeSelectionPopup.Option> options = new List<DevLargeSelectionPopup.Option>
		{
			new DevLargeSelectionPopup.Option("lines", "编辑原始行", "查看并编辑该日每一条未压缩记忆。", isPrimary: true),
			new DevLargeSelectionPopup.Option("add_normal", "新增普通记忆行", "新增一条会参与 LLM 对话记忆的原始行。"),
			new DevLargeSelectionPopup.Option("add_afef", "新增AFEF行", "新增一条 AFEF 机制行，不参与 LLM 对话压缩正文。"),
			new DevLargeSelectionPopup.Option("delete", "删除该日未压缩记忆", "删除该日全部未压缩原始历史，并移除同日待总结队列。", isDanger: true),
			new DevLargeSelectionPopup.Option("back", "返回列表")
		};
		ShowDevLargeSelectionOrInquiry("未压缩记忆 - " + name, BuildDevDailyMemoryDraftSubtitle(draft), BuildDevDailyMemoryDraftEditorDescription(draft), options, delegate(string selectedId)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		});
	}

	private void OpenDevDailyMemoryLineList(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex);
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
			string scene = string.IsNullOrWhiteSpace(line.Scene) ? "未知场景" : BuildDevHistoryPreview(line.Scene, 42);
			string title = (i + 1) + ". " + MBMath.ClampInt(line.GameHour, 0, 23) + "时 | " + type + " | " + speaker + " | " + scene;
			string detail = string.IsNullOrWhiteSpace(line.Text) ? "（空）" : BuildDevHistoryPreview(line.Text, 260);
			options.Add(new DevLargeSelectionPopup.Option("line:" + i, title, detail));
		}
		string name = npc?.Name?.ToString() ?? "NPC";
		string body = BuildDevDailyMemoryDraftSubtitle(draft) + "\n\n当前共 " + lines.Count + " 行。选择右侧条目进入行详情。";
		ShowDevLargeSelectionOrInquiry("编辑未压缩记忆行 - " + name, "未压缩原始历史行", body, options, delegate(string selectedId)
		{
			if (string.IsNullOrWhiteSpace(selectedId))
			{
				OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
				return;
			}
			if (!TryParseDevSelectionInt(selectedId, "line:", out var lineIndex))
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
			if (TryParseDevSelectionInt(selectedId, "line:", out lineIndex))
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

	private void OpenDevDailyMemoryLineEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆行。"));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
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
		ShowDevLargeSelectionOrInquiry("未压缩记忆行 - " + name, BuildDevDailyMemoryLineSubtitle(line), body, options, delegate(string selectedId)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		});
	}

	private void OpenDevDailyMemoryLineTextEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆正文 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入新的正文；留空=删除该行。", line.Text ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevDailyMemoryLineSpeakerEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆说话人 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入说话人；留空=自动使用默认说话人。", line.Speaker ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Speaker = (input ?? "").Trim();
			}, "未压缩记忆说话人已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevDailyMemoryLineSceneEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑未压缩记忆场景 - " + name, BuildDevDailyMemoryLineSubtitle(line), "请输入场景；留空=未知场景。", line.Scene ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryLineMutation(npc, dayIndex, lineIndex, returnPage, returnQuery, delegate(DailyMemoryDraft draft, DailyMemoryLine target)
			{
				target.Scene = (input ?? "").Trim();
			}, "未压缩记忆场景已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevDailyMemoryLineHourEditor(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
		InformationManager.ShowTextInquiry(new TextInquiryData("编辑未压缩记忆小时", BuildDevDailyMemoryLineSubtitle(line) + "\n请输入 0~23 的整数。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}, shouldInputBeObfuscated: false, null, line.GameHour.ToString()));
	}

	private void ToggleDevDailyMemoryLineAfef(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
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

	private void ToggleDevDailyMemoryLineLlm(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
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

	private void OpenDevAddDailyMemoryLine(Hero npc, int dayIndex, bool isAfef, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(draft);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor(isAfef ? ("新增AFEF行 - " + name) : ("新增普通记忆行 - " + name), BuildDevDailyMemoryDraftSubtitle(draft), "请输入新增行正文；留空=取消。", "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			string text = NormalizeDevCompressedMemoryMultilineInput(input);
			if (string.IsNullOrWhiteSpace(text))
			{
				OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
				return;
			}
			if (!TryApplyDevDailyMemoryDraftDataMutation(npc, dayIndex, targetDraft =>
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
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void ConfirmDevDeleteDailyMemoryLine(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryLine line = FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex);
		if (line == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(line);
		ShowDevLargeConfirmOrInquiry("确认删除未压缩记忆行", BuildDevDailyMemoryLineSubtitle(line), "正文：\n" + (string.IsNullOrWhiteSpace(line.Text) ? "（空）" : line.Text.Trim()) + "\n\n此操作不可撤销，是否继续？", "确认删除", "取消", delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			ApplyDevDailyMemoryDraftMutation(npc, dayIndex, delegate(DailyMemoryDraft draft)
			{
				if (draft.Lines != null && lineIndex >= 0 && lineIndex < draft.Lines.Count)
				{
					draft.Lines.RemoveAt(lineIndex);
				}
			}, "delete_line", "已删除未压缩记忆行。", returnPage, returnQuery, returnToDraftEditor: false);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(line, FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(line), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		});
	}

	private void ConfirmDevDeleteDailyMemoryDraft(Hero npc, int dayIndex, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		DailyMemoryDraft draft = FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex);
		if (draft == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(draft);
		ShowDevLargeConfirmOrInquiry("确认删除未压缩记忆", BuildDevDailyMemoryDraftSubtitle(draft), "将删除该日全部未压缩原始历史，并移除同日待总结队列。\n此操作不可撤销，是否继续？", "确认删除", "取消", delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			if (!DeleteDevDailyMemoryDraftData(npc, dayIndex, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("已删除该日未压缩记忆。"));
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(draft, FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(draft), StringComparison.Ordinal)) return;
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		});
	}

	private static bool IsDevDailyMemoryDraftMatch(DailyMemoryDraft draft, string[] terms)
		=> MemoryEditorProjection.IsDevDailyMemoryDraftMatch(MemoryEditorDisplay, draft, terms);

	private static string BuildDevDailyMemoryDraftSearchText(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSearchText(MemoryEditorDisplay, draft);

	private static string BuildDevDailyMemoryDraftListLabel(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftListLabel(MemoryEditorDisplay, draft);

	private static string BuildDevDailyMemoryDraftSubtitle(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftSubtitle(MemoryEditorDisplay, draft);

	private static string BuildDevStoredErrorReference(string error)
		=> MemoryEditorProjection.BuildDevStoredErrorReference(MemoryEditorDisplay, error);

	private static string BuildDevDailyMemoryDraftEditorDescription(DailyMemoryDraft draft)
		=> MemoryEditorProjection.BuildDevDailyMemoryDraftEditorDescription(MemoryEditorDisplay, draft);

	private static string BuildDevWeeklyMemoryMaterialTriggerText(IEnumerable<WeeklyMemoryMaterialTrigger> triggers)
		=> MemoryEditorProjection.BuildDevWeeklyMemoryMaterialTriggerText(MemoryEditorDisplay, triggers);

	private static string BuildDevDailyMemoryLineListLabel(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineListLabel(MemoryEditorDisplay, line);

	private static string BuildDevDailyMemoryLineSubtitle(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineSubtitle(MemoryEditorDisplay, line);

	private static string BuildDevDailyMemoryLineDescription(DailyMemoryLine line)
		=> MemoryEditorProjection.BuildDevDailyMemoryLineDescription(MemoryEditorDisplay, line);

	private static int GetDefaultDevDailyMemoryLineHour(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineHour(MemoryEditorDisplay, draft);

	private static string GetDefaultDevDailyMemoryLineScene(DailyMemoryDraft draft)
		=> MemoryEditorProjection.GetDefaultDevDailyMemoryLineScene(MemoryEditorDisplay, draft);

	private void OpenDevMemoryOverviewEditor(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		string heroId = GetMemoryHeroId(npc);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		MemoryOverviewState state = GetMemoryOverviewState(heroId);
		List<CompressedMemoryBlock> blocks = LoadCompressedMemoryBlocks(npc);
		string editorFingerprint = ComputeMemorySummaryFingerprint(new { Overview = state, Blocks = blocks });
		string name = npc.Name?.ToString() ?? "NPC";
		string subtitle = "当前压缩记忆块：" + (blocks?.Count ?? 0) + " 块；已纳入：" + (state?.IncludedBlockIds?.Count ?? 0) + " 块。\n保存非空内容后，会把当前所有压缩记忆块标记为已纳入，避免被待更新队列立即覆盖。";
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆大总结 - " + name, subtitle, "请输入新的过往记忆总览；留空=清空并等待重新生成。", state?.Summary ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !string.Equals(heroId, GetMemoryHeroId(npc), StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(new { Overview = GetMemoryOverviewState(heroId), Blocks = LoadCompressedMemoryBlocks(npc) }), StringComparison.Ordinal)) return;
			ApplyDevMemoryOverviewInput(npc, input);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryMenu(npc);
		}, "保存", "返回");
	}

	private void ApplyDevMemoryOverviewInput(Hero npc, string input)
	{
		string heroId = GetMemoryHeroId(npc);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		string summary = NormalizeDevCompressedMemoryMultilineInput(input);
		List<CompressedMemoryBlock> blocks = string.IsNullOrWhiteSpace(summary) ? null : LoadCompressedMemoryBlocks(npc);
		MemoryImportExportState state = CaptureMemoryImportExportState();
		bool saved = MemoryDeveloperEditOwner.SaveOverview(heroId, npc.Name?.ToString(), summary, blocks, DateTime.UtcNow.Ticks, state);
		_memoryOverviewStates = state.Overviews;
		_memoryOverviewQueue = state.OverviewQueue;
		if (!saved)
		{
			TryEnqueueMemoryOverviewForHero(npc, LoadCompressedMemoryBlocks(npc));
			Logger.Log("MemoryOverview", "manual_overview_clear hero=" + heroId);
			InformationManager.DisplayMessage(new InformationMessage("已清空记忆大总结，并重新检查待整理队列。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		int includedCount = state.Overviews[heroId].IncludedBlockIds?.Count ?? 0;
		Logger.Log("MemoryOverview", "manual_overview_save hero=" + heroId + " blocks=" + includedCount);
		InformationManager.DisplayMessage(new InformationMessage("记忆大总结已更新。"));
		OpenDevCompressedMemoryMenu(npc);
	}

	private void OpenDevCompressedMemoryBlockList(Hero npc, int page, string query)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		_devEditingHero = npc;
		page = Math.Max(0, page);
		string q = (query ?? "").Trim();
		_devCompressedMemorySearchQuery = q;
		_devCompressedMemoryBlockPage = page;
		List<CompressedMemoryBlock> blocks = SanitizeCompressedMemoryBlocks(LoadCompressedMemoryBlocks(npc));
		if (blocks == null || blocks.Count <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有压缩记忆块。"));
			OpenDevCompressedMemoryMenu(npc);
			return;
		}
		MemoryEditorPage<Tuple<int, CompressedMemoryBlock>> view = MemoryEditor.OpenCompressedPage(blocks, page, q, MemoryEditorDisplay);
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
			descriptionText += "\n当前搜索：" + BuildDevHistoryPreview(q, 80);
		}
		if (filtered.Count <= 0)
		{
			descriptionText += "\n\n没有匹配结果。";
		}
		ShowDevLargeSelectionOrInquiry("编辑压缩记忆块 - " + name, "压缩记忆块列表", descriptionText, options, delegate(string selectedId)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
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
					if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
					OpenDevCompressedMemoryBlockList(npc, 0, input);
				}, delegate
				{
					if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)) return;
			OpenDevCompressedMemoryMenu(npc);
		}, "进入", "返回");
	}

	private void OpenDevCompressedMemoryBlockEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		_devEditingHero = npc;
		List<CompressedMemoryBlock> blocks = LoadCompressedMemoryBlocks(npc);
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(blocks, blockId);
		if (block == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的压缩记忆块。"));
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
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
		ShowDevLargeSelectionOrInquiry("记忆块 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), BuildDevCompressedMemoryBlockEditorBody(block), options, delegate(string selectedId)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
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
			if (!IsMemorySourceEditorCurrent(editorGeneration) || !ReferenceEquals(_devEditingHero, npc)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
		}, "进入", "返回");
	}

	private void OpenDevCompressedMemoryBlockTitleEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块标题 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "请输入新的富标题；留空=清空标题。", block.RichTitle ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.RichTitle = (input ?? "").Trim();
			}, "记忆块标题已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevCompressedMemoryBlockSummaryEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块正文 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "请输入新的记忆正文；留空=清空正文。", block.Summary ?? "", delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Summary = NormalizeDevCompressedMemoryMultilineInput(input);
			}, "记忆块正文已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevCompressedMemoryBlockScenesEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string initial = string.Join("\n", block.Scenes ?? new List<string>());
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块场景 - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "每行一个场景；留空=清空场景列表；最多保留16项。", initial, delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.Scenes = ParseDevCompressedMemoryLineList(input, 16, ignoreCase: true);
			}, "记忆块场景列表已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void OpenDevCompressedMemoryBlockAfefEditor(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string initial = string.Join("\n", block.AfefLines ?? new List<string>());
		DevTextEditorHelper.ShowLongTextEditor("编辑记忆块AFEF - " + name, BuildDevCompressedMemoryBlockSubtitle(block), "每行一条 AFEF；留空=清空；最多保留80项。", initial, delegate(string input)
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			ApplyDevCompressedMemoryBlockMutation(npc, blockId, returnPage, returnQuery, delegate(CompressedMemoryBlock target)
			{
				target.AfefLines = ParseDevCompressedMemoryLineList(input, 80, ignoreCase: false);
			}, "记忆块 AFEF 行已更新。");
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}, "保存", "返回");
	}

	private void ConfirmDevDeleteCompressedMemoryBlock(Hero npc, string blockId, int returnPage, string returnQuery)
	{
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		CompressedMemoryBlock block = FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId);
		if (block == null)
		{
			OpenDevCompressedMemoryBlockList(npc, returnPage, returnQuery);
			return;
		}
		string editorFingerprint = ComputeMemorySummaryFingerprint(block);
		string name = npc?.Name?.ToString() ?? "NPC";
		string message = BuildDevCompressedMemoryBlockSubtitle(block) + "\n\n将从 " + name + " 的压缩记忆中删除该块，并使记忆大总结重新整理。\n此操作不可撤销，是否继续？";
		InformationManager.ShowInquiry(new InquiryData("确认删除压缩记忆块", message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认删除", "取消", delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)
				|| !ReferenceEquals(block, FindDevCompressedMemoryBlock(LoadCompressedMemoryBlocks(npc), blockId))
				|| !string.Equals(editorFingerprint, ComputeMemorySummaryFingerprint(block), StringComparison.Ordinal)) return;
			DeleteDevCompressedMemoryBlock(npc, blockId, returnPage, returnQuery);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryBlockEditor(npc, blockId, returnPage, returnQuery);
		}), pauseGameActiveState: true);
	}

	private static CompressedMemoryBlock FindDevCompressedMemoryBlock(List<CompressedMemoryBlock> blocks, string blockId)
		=> MemoryEditorProjection.FindDevCompressedMemoryBlock(MemoryEditorDisplay, blocks, blockId);

	private static string GetDevCompressedMemoryBlockId(CompressedMemoryBlock block)
		=> MemoryEditorProjection.GetDevCompressedMemoryBlockId(MemoryEditorDisplay, block);

	private static string BuildDevCompressedMemoryBlockListLabel(CompressedMemoryBlock block, int displayIndex)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockListLabel(MemoryEditorDisplay, block, displayIndex);

	private static string BuildDevCompressedMemoryBlockSubtitle(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockSubtitle(MemoryEditorDisplay, block);

	private static string BuildDevCompressedMemoryBlockEditorBody(CompressedMemoryBlock block)
		=> MemoryEditorProjection.BuildDevCompressedMemoryBlockEditorBody(MemoryEditorDisplay, block);

	private static string[] SplitDevCompressedMemorySearchTerms(string query)
		=> MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(MemoryEditorDisplay, query);

	private static bool IsDevCompressedMemoryBlockMatch(CompressedMemoryBlock block, string[] terms)
		=> MemoryEditorProjection.IsDevCompressedMemoryBlockMatch(MemoryEditorDisplay, block, terms);

	private static string NormalizeDevCompressedMemoryMultilineInput(string input)
		=> MemoryEditorProjection.NormalizeDevCompressedMemoryMultilineInput(MemoryEditorDisplay, input);

	private static List<string> ParseDevCompressedMemoryLineList(string input, int maxCount, bool ignoreCase)
		=> MemoryEditorProjection.ParseDevCompressedMemoryLineList(MemoryEditorDisplay, input, maxCount, ignoreCase);

	private string BuildDevCompressedMemoryRawText(Hero npc)
	{
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDrafts(npc);
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
				string rendered = BuildDailyMemoryLineForPrompt(line);
				if (!string.IsNullOrWhiteSpace(rendered))
				{
					sb.AppendLine(rendered);
				}
			}
			sb.AppendLine();
		}
		return sb.ToString().TrimEnd();
	}

	private string BuildDevCompressedMemoryBlockText(Hero npc)
	{
		List<CompressedMemoryBlock> blocks = LoadCompressedMemoryBlocks(npc);
		if (blocks == null || blocks.Count <= 0)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		int i = 1;
		foreach (CompressedMemoryBlock block in blocks.OrderBy((CompressedMemoryBlock x) => x.GameDayIndex).ThenBy((CompressedMemoryBlock x) => x.StartHour))
		{
			string date = string.IsNullOrWhiteSpace(block.GameDate) ? ("第" + block.GameDayIndex + "日") : block.GameDate.Trim();
			sb.AppendLine(i + "# " + date + " " + FormatMemoryHourRange(block.StartHour, block.EndHour) + " " + (block.RichTitle ?? ""));
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

	private string BuildDevCompressedMemoryQueueText(Hero npc)
	{
		string heroId = GetMemoryHeroId(npc);
		List<MemorySummaryJob> jobs = (_memorySummaryQueue ?? new List<MemorySummaryJob>()).Where((MemorySummaryJob x) => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemorySummaryJob x) => x.GameDayIndex).ToList();
		List<MemoryOverviewJob> overviewJobs = (_memoryOverviewQueue ?? new List<MemoryOverviewJob>()).Where((MemoryOverviewJob x) => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemoryOverviewJob x) => x.TriggerGameDayIndex).ToList();
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

	private string BuildDevMemoryOverviewText(Hero npc)
	{
		string heroId = GetMemoryHeroId(npc);
		MemoryOverviewState state = GetMemoryOverviewState(heroId);
		List<MemoryOverviewJob> jobs = (_memoryOverviewQueue ?? new List<MemoryOverviewJob>()).Where((MemoryOverviewJob x) => x != null && string.Equals(NormalizeMemoryHeroId(x.HeroId), heroId, StringComparison.OrdinalIgnoreCase)).OrderBy((MemoryOverviewJob x) => x.TriggerGameDayIndex).ToList();
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

	private void ConfirmDevClearCompressedMemory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		string name = npc.Name?.ToString() ?? "NPC";
		InformationManager.ShowInquiry(new InquiryData("确认清空压缩记忆", "将删除 " + name + " 的今日历史、待总结队列、压缩记忆块和记忆大总结。\n此操作不可撤销，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认清空", "取消", delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			if (!ClearDevCompressedMemoryData(npc, editorGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的新压缩记忆数据。"));
			OpenDevCompressedMemoryMenu(npc);
		}, delegate
		{
			if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
			OpenDevCompressedMemoryMenu(npc);
		}), pauseGameActiveState: true);
	}

	private void OpenDevHistorySearchInput(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string soundEventPath = _devHistorySearchQuery ?? string.Empty;
		InformationManager.ShowTextInquiry(new TextInquiryData("搜索对话历史 - " + text, "输入关键词，按内容模糊匹配该 NPC 的全部对话历史。\n留空将不改变当前搜索。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
		{
			if (!string.IsNullOrWhiteSpace(input))
			{
				_devHistorySearchQuery = input.Trim();
			}
			OpenDevHistoryDateSelection(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}, shouldInputBeObfuscated: false, null, soundEventPath));
	}

	private void ConfirmDevClearAllDialogueHistory(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		long editorGeneration = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(editorGeneration)) return;
		List<DialogueDay> list = LoadDialogueHistory(npc);
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
			if (!ClearDevDialogueHistoryData(npc, editorGeneration)) return;
			_devHistorySearchQuery = string.Empty;
			InformationManager.DisplayMessage(new InformationMessage("已删除该NPC的全部对话历史。"));
			ShowDevEditInquiry(npc);
		}, delegate
		{
			OpenDevHistoryDateSelection(npc);
		}));
	}

	private void OpenDevHistoryDateSelection(Hero npc)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC当前没有任何对话历史。"));
			ShowDevEditInquiry(npc);
			return;
		}
		list = list.OrderBy((DialogueDay d) => d.GameDayIndex).ToList();
		string text = (_devHistorySearchQuery ?? string.Empty).Trim();
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
						string arg2 = BuildDevHistoryPreview(text3);
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
			text5 = text5 + "\n当前搜索：" + BuildDevHistoryPreview(text, 40);
		}
		if (list2.Count <= 4)
		{
			text5 += "\n\n没有匹配结果。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑对话历史 - " + text4, text5, list2, isExitShown: true, 0, 1, "下一步", "返回", OnDevHistoryDateSelected, delegate
		{
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	private void OnDevHistoryDateSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
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
				ShowDevEditInquiry(devEditingHero);
				return;
			case "__search__":
				OpenDevHistorySearchInput(devEditingHero);
				return;
			case "__clear_search__":
				_devHistorySearchQuery = string.Empty;
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

	private void OpenDevHistoryLineSelection(Hero npc, int dayIndex)
	{
		if (npc == null)
		{
			return;
		}
		_devEditingHero = npc;
		List<DialogueDay> list = LoadDialogueHistory(npc);
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
			string arg = BuildDevHistoryPreview(line);
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

	private void OnDevHistoryLineSelected(List<InquiryElement> selected)
	{
		Hero devEditingHero = _devEditingHero;
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

	private void ApplyDevDailyMemoryLineMutation(Hero npc, int dayIndex, int lineIndex, int returnPage, string returnQuery, Action<DailyMemoryDraft, DailyMemoryLine> mutate, string successMessage)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(generation)) return;
		if (!TryApplyDevDailyMemoryLineDataMutation(npc, dayIndex, lineIndex, mutate, generation))
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到要编辑的未压缩记忆行。"));
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
			return;
		}
		InformationManager.DisplayMessage(new InformationMessage(successMessage ?? "未压缩记忆行已更新。"));
		if (FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex) == null)
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}
		else if (FindDevDailyMemoryLine(LoadDailyMemoryDrafts(npc), dayIndex, lineIndex) == null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}
		else
		{
			OpenDevDailyMemoryLineEditor(npc, dayIndex, lineIndex, returnPage, returnQuery);
		}
	}

	private void ApplyDevDailyMemoryDraftMutation(Hero npc, int dayIndex, Action<DailyMemoryDraft> mutate, string reason, string successMessage, int returnPage, string returnQuery, bool returnToDraftEditor)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		if (!IsMemorySourceEditorCurrent(generation)) return;
		if (!TryApplyDevDailyMemoryDraftDataMutation(npc, dayIndex, mutate, reason, generation))
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
			return;
		}
		if (!string.IsNullOrWhiteSpace(successMessage))
		{
			InformationManager.DisplayMessage(new InformationMessage(successMessage));
		}
		if (returnToDraftEditor && FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex) != null)
		{
			OpenDevDailyMemoryDraftEditor(npc, dayIndex, returnPage, returnQuery);
		}
		else if (FindDevDailyMemoryDraft(LoadDailyMemoryDrafts(npc), dayIndex) != null)
		{
			OpenDevDailyMemoryLineList(npc, dayIndex, returnPage, returnQuery);
		}
		else
		{
			OpenDevDailyMemoryDraftList(npc, returnPage, returnQuery);
		}
	}

}
