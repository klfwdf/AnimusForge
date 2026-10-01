using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class EventEditorController
{
 private long _confirmationTicket;
 private readonly EventEditorPort _port;
 internal EventEditorController(EventEditorPort port) { _port = port; }
 private long _generation=long.MinValue; internal EventRecordEntry RecordSelection; internal int RecordPage, MaterialPage;
 internal void SynchronizeGeneration(long generation) { if (_generation==generation) return; _generation=generation; RecordSelection=null; RecordPage=MaterialPage=0; }
	internal void OpenDevEventEditorMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.EnsureWeekZeroOpeningSummaryEvents();
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit_world_summary", "编辑世界开局概要", null));
		list.Add(new InquiryElement("edit_kingdom_summary", "编辑王国开局概要", null));
		list.Add(new InquiryElement("preview_weekly_materials", "查看本周事件素材预览", null));
		list.Add(new InquiryElement("preview_weekly_prompts", "预览本周周报 Prompt", null));
		list.Add(new InquiryElement("generate_weekly_reports", "生成本周周报草案", null));
		list.Add(new InquiryElement("kingdom_stability_lab", "王国稳定度与叛乱实验", null));
		list.Add(new InquiryElement("view_events", "查看事件与素材", null));
		list.Add(new InquiryElement("export_event_data", "全量导出（事件编辑，选文件夹）", null));
		list.Add(new InquiryElement("import_event_data", "全量导入（事件编辑，选文件夹）", null));
		list.Add(new InquiryElement("clear_all", "清空全部事件概要", null));
		list.Add(new InquiryElement("back", "返回", null));
		string text = BuildDevEventEditorMenuDescription();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件编辑", text, list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(generation)) OnDevEventEditorMenuSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevEventEditorMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "edit_world_summary":
			OpenDevEditWorldOpeningSummary();
			break;
		case "edit_kingdom_summary":
			OpenDevKingdomOpeningSummaryMenu();
			break;
		case "preview_weekly_materials":
			_port.OpenDevWeeklyEventMaterialPreviewMenu();
			break;
		case "preview_weekly_prompts":
			_port.OpenDevWeeklyReportPromptPreviewMenu();
			break;
		case "generate_weekly_reports":
			_port.ConfirmGenerateDevWeeklyReports();
			break;
		case "kingdom_stability_lab":
			_port.OpenDevKingdomStabilityLabMenu();
			break;
		case "view_events":
			OpenDevEventViewerMenu(0);
			break;
		case "export_event_data":
			_port.OpenExportFolderPicker("全量导出（事件编辑）- 选择文件夹", ExportImportScope.EventData, OpenDevEventEditorMenu);
			break;
		case "import_event_data":
			_port.OpenImportFolderPicker("全量导入（事件编辑）- 选择文件夹", ExportImportScope.EventData, OpenDevEventEditorMenu);
			break;
		case "clear_all":
			ConfirmClearAllEventOpeningSummaries();
			break;
		}
	}

	internal string BuildDevEventEditorMenuDescription()
	{
		int num = _port.CountConfiguredOpeningSummaries();
		int num2 = 0;
		try
		{
			num2 = Kingdom.All.Count((Kingdom x) => x != null && !string.IsNullOrWhiteSpace(x.StringId));
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("世界开局概要：" + (string.IsNullOrWhiteSpace(_port.WorldOpeningSummary()) ? "未设置" : "已设置"));
		stringBuilder.AppendLine("王国开局概要：" + num + "/" + num2 + " 已设置");
		stringBuilder.AppendLine("事件记录：" + ((_port.EventRecords() != null) ? _port.SanitizeEventRecordEntries(_port.EventRecords()).Count : 0) + " 条");
		stringBuilder.AppendLine("周报篇幅档位：" + _port.PromptProfileLabel());
		List<Kingdom> editableKingdoms = EventEditorProjection.GetDevEditableKingdoms();
		int num3 = editableKingdoms.Count;
		int num4 = editableKingdoms.Count((Kingdom x) => _port.GetKingdomStabilityValue(x) != _port.StabilityDefault);
		stringBuilder.AppendLine("王国稳定度：" + num4 + "/" + num3 + " 已偏离默认值");
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEditWorldOpeningSummary()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		DevTextEditorHelper.ShowLongTextEditor("编辑世界开局概要", "这段文本会作为世界事件系统的初始背景底稿。", "请输入世界开局概要（留空=清空）。", _port.WorldOpeningSummary() ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			if (!_port.SetDeveloperWorldOpeningSummary(input, generation)) return;
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(_port.WorldOpeningSummary()) ? "已清空世界开局概要。" : "世界开局概要已更新。"));
			OpenDevEventEditorMenu();
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventEditorMenu();
		});
	}

	internal void OpenDevKingdomOpeningSummaryMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<Kingdom> list = EventEditorProjection.GetDevEditableKingdoms();
		if (list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的王国。"));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (Kingdom item in list)
		{
			string devKingdomSummaryLabel = BuildDevKingdomSummaryLabel(item);
			list2.Add(new InquiryElement(new DevKingdomSummaryMenuItem
			{
				KingdomId = item.StringId ?? "",
				DisplayName = item.Name?.ToString() ?? (item.StringId ?? "王国")
			}, devKingdomSummaryLabel, null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("请选择要编辑的王国开局概要。");
		stringBuilder.AppendLine("这些文本会作为该王国后续每周事件生成的基础背景。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑王国开局概要", stringBuilder.ToString().TrimEnd(), list2, isExitShown: true, 0, 1, "编辑", "返回", selected => { if (_port.IsCurrent(generation)) OnDevKingdomOpeningSummaryMenuSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevKingdomOpeningSummaryMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (selected == null || selected.Count == 0)
		{
			OpenDevEventEditorMenu();
			return;
		}
		if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevEventEditorMenu();
			return;
		}
		if (selected[0].Identifier is DevKingdomSummaryMenuItem devKingdomSummaryMenuItem)
		{
			Kingdom kingdom = _port.FindKingdomById(devKingdomSummaryMenuItem.KingdomId);
			if (kingdom == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("找不到对应的王国。"));
				OpenDevKingdomOpeningSummaryMenu();
			}
			else
			{
				OpenDevEditKingdomOpeningSummary(kingdom);
			}
		}
		else
		{
			OpenDevKingdomOpeningSummaryMenu();
		}
	}

	internal void OpenDevEditKingdomOpeningSummary(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomOpeningSummaryMenu();
			return;
		}
		string text = _port.GetKingdomOpeningSummary(kingdom);
		string text2 = kingdom.Name?.ToString() ?? (kingdom.StringId ?? "王国");
		string subtitleText = "这段文本会作为该王国的开局底稿，供后续事件系统生成该王国的周事件时参考。";
		DevTextEditorHelper.ShowLongTextEditor("编辑王国开局概要 - " + text2, subtitleText, "请输入该王国的开局概要（留空=清空）。", text, delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			_port.SaveKingdomOpeningSummary(kingdom, input);
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(input) ? ("已清空 " + text2 + " 的开局概要。") : (text2 + " 的开局概要已更新。")));
			OpenDevKingdomOpeningSummaryMenu();
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomOpeningSummaryMenu();
		});
	}

	internal void ConfirmClearAllEventOpeningSummaries()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		long ticket = ++_confirmationTicket;
		bool completed = false;
		InformationManager.ShowInquiry(new InquiryData("确认清空事件概要", "这会清空世界开局概要，以及所有王国的开局概要。\n此操作不可撤销，是否继续？", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认清空", "取消", delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			if (!_port.ClearDeveloperOpeningSummaries(generation)) return;
			InformationManager.DisplayMessage(new InformationMessage("已清空全部事件概要。"));
			OpenDevEventEditorMenu();
		}, delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			OpenDevEventEditorMenu();
		}));
	}

	internal string BuildDevKingdomSummaryLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "无效王国";
		}
		string text = kingdom.Name?.ToString() ?? (kingdom.StringId ?? "王国");
		string kingdomOpeningSummary = _port.GetKingdomOpeningSummary(kingdom);
		string text2 = string.IsNullOrWhiteSpace(kingdomOpeningSummary) ? "未设置" : "已设置";
		string devSummaryPreview = EventEditorProjection.BuildDevSummaryPreview(kingdomOpeningSummary, 72);
		if (string.IsNullOrWhiteSpace(devSummaryPreview))
		{
			return text + " [" + text2 + "]";
		}
		return text + " [" + text2 + "] " + devSummaryPreview;
	}

	internal void OpenDevEventViewerMenu(int page)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<EventRecordEntry> list = _port.SanitizeEventRecordEntries(_port.EventRecords());
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 14;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		RecordPage = page;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", "下一页", null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventRecordEntry item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, EventEditorProjection.BuildDevEventRecordMenuLabel(_port, item), null));
		}
		string text = BuildDevEventViewerDescription(list, page, num);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件查看器", text, list2, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text2)
			{
				switch (text2)
				{
				case "back":
					OpenDevEventEditorMenu();
					break;
				case "prev_page":
					OpenDevEventViewerMenu(page - 1);
					break;
				case "next_page":
					OpenDevEventViewerMenu(page + 1);
					break;
				default:
					OpenDevEventViewerMenu(page);
					break;
				}
			}
			else if (selected[0].Identifier is EventRecordEntry eventRecordEntry)
			{
				OpenDevEventRecordDetail(eventRecordEntry, page);
			}
			else
			{
				OpenDevEventViewerMenu(page);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevEventViewerDescription(List<EventRecordEntry> entries, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里用于查看事件系统已经记录下来的事件，以及每条事件引用了哪些素材。");
		stringBuilder.AppendLine("素材可能包括：世界开局概要、王国开局概要、NPC近期行动、NPC重大行动，以及未来接入的其他摘要。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("事件总数：" + ((entries != null) ? entries.Count : 0));
		stringBuilder.AppendLine("页码：" + (page + 1) + "/" + Math.Max(1, totalPages));
		if (entries == null || entries.Count == 0)
		{
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("当前还没有事件记录。");
			stringBuilder.AppendLine("后续周事件系统接入后，每条事件都会显示在这里，并能展开查看引用素材。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEventRecordDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		RecordSelection = entry;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回事件列表", null));
		if (!string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			list2.Add(new InquiryElement("view_short", "查看短摘要", null));
		}
		list2.Add(new InquiryElement("view_report", "查看周报正文", null));
		list2.Add(new InquiryElement("edit_title", "编辑标题", null));
		list2.Add(new InquiryElement("edit_report", "编辑周报正文", null));
		if (!string.IsNullOrWhiteSpace(entry.TagText))
		{
			list2.Add(new InquiryElement("view_tags", "查看标签层", null));
		}
		if (!string.IsNullOrWhiteSpace(entry.PromptText))
		{
			list2.Add(new InquiryElement("view_prompt", "查看请求 Prompt", null));
		}
		if ((entry.Materials?.Count).GetValueOrDefault() > 0)
		{
			list2.Add(new InquiryElement("view_materials", "查看素材列表", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件详情", BuildDevEventRecordCompactDetailText(entry), list2, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventViewerMenu(returnPage);
			}
			else if (selected[0].Identifier is string text)
			{
				if (text == "back")
				{
					OpenDevEventViewerMenu(returnPage);
				}
				else if (text == "edit_title")
				{
					OpenDevEditEventRecordTitle(entry, returnPage);
				}
				else if (text == "view_report")
				{
					OpenDevEventReportDetail(entry, returnPage);
				}
				else if (text == "view_short")
				{
					OpenDevEventShortSummaryDetail(entry, returnPage);
				}
				else if (text == "edit_report")
				{
					OpenDevEditEventRecordReport(entry, returnPage);
				}
				else if (text == "view_tags")
				{
					OpenDevEventTagDetail(entry, returnPage);
				}
				else if (text == "view_prompt")
				{
					OpenDevEventPromptDetail(entry, returnPage);
				}
				else if (text == "view_materials")
				{
					OpenDevEventMaterialList(entry, returnPage, 0);
				}
				else
				{
					OpenDevEventRecordDetail(entry, returnPage);
				}
			}
			else
			{
				OpenDevEventRecordDetail(entry, returnPage);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventViewerMenu(returnPage);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevEventRecordDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, "事件标题", (entry.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, "事件类型", _port.TranslateEventKindForDev(entry.EventKind));
		_port.AppendDevNpcActionField(stringBuilder, "周数", "第 " + Math.Max(0, entry.WeekIndex) + " 周");
		_port.AppendDevNpcActionField(stringBuilder, "生成日期", !string.IsNullOrWhiteSpace(entry.CreatedDate) ? entry.CreatedDate.Trim() : ("第 " + Math.Max(0, entry.CreatedDay) + " 日"));
		_port.AppendDevNpcActionField(stringBuilder, "归属王国", _port.ResolveKingdomDisplay(entry.ScopeKingdomId));
		_port.AppendDevNpcActionField(stringBuilder, "素材数量", ((entry.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, "短摘要", string.IsNullOrWhiteSpace(entry.ShortSummary) ? "未保存" : EventEditorProjection.BuildDevSummaryPreview(entry.ShortSummary, 48));
		_port.AppendDevNpcActionField(stringBuilder, "标签层", string.IsNullOrWhiteSpace(entry.TagText) ? "未保存" : "已保存");
		_port.AppendDevNpcActionField(stringBuilder, "Prompt", string.IsNullOrWhiteSpace(entry.PromptText) ? "未保存" : "已保存");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【操作说明】");
		stringBuilder.AppendLine("短摘要、周报正文、标签层、Prompt 和素材列表都已拆分为单独入口，不再直接铺在这一页。");
		if (string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			stringBuilder.AppendLine("当前这条事件还没有短摘要。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有短摘要，可通过“查看短摘要”进入。");
		}
		if (string.IsNullOrWhiteSpace(entry.Summary))
		{
			stringBuilder.AppendLine("当前这条事件还没有正文。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有正文，可通过“查看周报正文”或“编辑周报正文”进入。");
		}
		if (string.IsNullOrWhiteSpace(entry.TagText))
		{
			stringBuilder.AppendLine("当前这条事件还没有标签层。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已有标签层，可通过“查看标签层”进入。");
		}
		if (entry.Materials == null || entry.Materials.Count == 0)
		{
			stringBuilder.AppendLine("当前这条事件还没有挂载任何素材引用。");
		}
		else
		{
			stringBuilder.AppendLine("当前这条事件已挂载素材，可通过“查看素材列表”进入。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal string BuildDevEventRecordCompactDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, "事件标题", (entry?.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, "事件类型", _port.TranslateEventKindForDev(entry?.EventKind));
		_port.AppendDevNpcActionField(stringBuilder, "周数", "第" + Math.Max(0, entry?.WeekIndex ?? 0) + " 周");
		_port.AppendDevNpcActionField(stringBuilder, "生成日期", !string.IsNullOrWhiteSpace(entry?.CreatedDate) ? entry.CreatedDate.Trim() : ("第" + Math.Max(0, entry?.CreatedDay ?? 0) + " 日"));
		_port.AppendDevNpcActionField(stringBuilder, "归属王国", _port.ResolveKingdomDisplay(entry?.ScopeKingdomId));
		_port.AppendDevNpcActionField(stringBuilder, "素材数量", ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, "短摘要", string.IsNullOrWhiteSpace(entry?.ShortSummary) ? "未保存" : EventEditorProjection.BuildDevSummaryPreview(entry.ShortSummary, 48));
		_port.AppendDevNpcActionField(stringBuilder, "标签层", string.IsNullOrWhiteSpace(entry?.TagText) ? "未保存" : "已保存");
		_port.AppendDevNpcActionField(stringBuilder, "Prompt", string.IsNullOrWhiteSpace(entry?.PromptText) ? "未保存" : "已保存");
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEventReportDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry("周报正文 - " + ((entry?.Title ?? "").Trim()), "", string.IsNullOrWhiteSpace(entry?.Summary) ? "当前这条事件还没有正文。" : entry.Summary.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, "返回事件详情");
	}

	internal void OpenDevEventShortSummaryDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry("短摘要 - " + ((entry?.Title ?? "").Trim()), "", string.IsNullOrWhiteSpace(entry?.ShortSummary) ? "当前这条事件还没有短摘要。" : entry.ShortSummary.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, "返回事件详情");
	}

	internal void OpenDevEventTagDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry("标签层 - " + ((entry?.Title ?? "").Trim()), "", string.IsNullOrWhiteSpace(entry?.TagText) ? "当前这条事件还没有标签层。" : entry.TagText.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, "返回事件详情");
	}

	internal void OpenDevEventMaterialList(EventRecordEntry entry, int returnPage, int page)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		List<EventMaterialReference> list = entry.Materials ?? new List<EventMaterialReference>();
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 14;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		MaterialPage = page;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回事件详情", null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", "下一页", null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventMaterialReference item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, EventEditorProjection.BuildDevEventMaterialItemLabel(_port, item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("事件素材列表", BuildDevEventMaterialListText(entry, page, num), list2, isExitShown: true, 0, 1, "查看素材", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevEventRecordDetail(entry, returnPage);
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "back":
					OpenDevEventRecordDetail(entry, returnPage);
					break;
				case "prev_page":
					OpenDevEventMaterialList(entry, returnPage, page - 1);
					break;
				case "next_page":
					OpenDevEventMaterialList(entry, returnPage, page + 1);
					break;
				default:
					OpenDevEventMaterialList(entry, returnPage, page);
					break;
				}
			}
			else if (selected[0].Identifier is EventMaterialReference eventMaterialReference)
			{
				OpenDevEventMaterialDetail(entry, eventMaterialReference, returnPage, page);
			}
			else
			{
				OpenDevEventMaterialList(entry, returnPage, page);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevEventMaterialListText(EventRecordEntry entry, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, "事件标题", (entry?.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, "素材数量", ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, "页码", (page + 1) + "/" + Math.Max(1, totalPages));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("下方列表中的每一项，都是这条周报在生成时引用过的素材。");
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEditEventRecordTitle(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑周报标题", "修改当前这条周报记录的标题。", "请输入新的标题（留空将回退为默认标题）。", entry.Title ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			EventRecordEntry updatedEntry = _port.ApplyDeveloperEventTitle(entry, input, generation);
			if (updatedEntry == null) return;
			InformationManager.DisplayMessage(new InformationMessage("周报标题已更新。"));
			OpenDevEventRecordDetail(updatedEntry ?? entry, returnPage);
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		});
	}

	internal void OpenDevEditEventRecordReport(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (entry == null)
		{
			OpenDevEventViewerMenu(returnPage);
			return;
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑周报正文", "这里修改的是最终展示出来的周报正文，不会改动原始素材。", "请输入新的周报正文。", entry.Summary ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			EventRecordEntry updatedEntry = _port.ApplyDeveloperEventReport(entry, input, generation);
			if (updatedEntry == null) return;
			InformationManager.DisplayMessage(new InformationMessage("周报正文已更新。"));
			OpenDevEventRecordDetail(updatedEntry ?? entry, returnPage);
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		});
	}

	internal void OpenDevEventPromptDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry("请求 Prompt - " + ((entry?.Title ?? "").Trim()), "", string.IsNullOrWhiteSpace(entry?.PromptText) ? "当前没有保存该条周报的请求 Prompt。" : entry.PromptText.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, "返回事件详情");
	}

	internal void OpenDevEventMaterialDetail(EventRecordEntry entry, EventMaterialReference material, int returnPage, int returnMaterialPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		string text = _port.BuildDevEventMaterialDetailText(material);
		string text2 = EventEditorProjection.BuildDevEventMaterialItemLabel(_port, material);
		_port.ShowDevLargeTextOrInquiry("素材详情 - " + text2, "", text, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventMaterialList(entry, returnPage, returnMaterialPage);
		}, "返回素材列表");
	}
}
