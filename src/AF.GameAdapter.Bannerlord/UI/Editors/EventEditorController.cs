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
		list.Add(new InquiryElement("edit_world_summary", EventEditorUiText.T("AF_UI_Event_Menu_EditWorld", "Edit world opening summary"), null));
		list.Add(new InquiryElement("edit_kingdom_summary", EventEditorUiText.T("AF_UI_Event_Menu_EditKingdom", "Edit kingdom opening summaries"), null));
		list.Add(new InquiryElement("preview_weekly_materials", EventEditorUiText.T("AF_UI_Event_Menu_PreviewMaterials", "Preview this week's event materials"), null));
		list.Add(new InquiryElement("preview_weekly_prompts", EventEditorUiText.T("AF_UI_Event_Menu_PreviewPrompts", "Preview weekly report prompts"), null));
		list.Add(new InquiryElement("generate_weekly_reports", EventEditorUiText.T("AF_UI_Event_Menu_GenerateReports", "Generate weekly report drafts"), null));
		list.Add(new InquiryElement("kingdom_stability_lab", EventEditorUiText.T("AF_UI_Event_Menu_StabilityLab", "Kingdom stability and rebellion lab"), null));
		list.Add(new InquiryElement("view_events", EventEditorUiText.T("AF_UI_Event_Menu_ViewEvents", "View events and materials"), null));
		list.Add(new InquiryElement("export_event_data", EventEditorUiText.T("AF_UI_Event_Menu_Export", "Export all (event editor - choose a folder)"), null));
		list.Add(new InquiryElement("import_event_data", EventEditorUiText.T("AF_UI_Event_Menu_Import", "Import all (event editor - choose a folder)"), null));
		list.Add(new InquiryElement("clear_all", EventEditorUiText.T("AF_UI_Event_Menu_Clear", "Clear all event summaries"), null));
		list.Add(new InquiryElement("back", EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), null));
		string text = BuildDevEventEditorMenuDescription();
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(EventEditorUiText.T("AF_UI_Event_Title_Editor", "Event editor"), text, list, isExitShown: true, 0, 1, EventEditorUiText.T("AF_UI_Event_Action_Enter", "Enter"), EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), selected => { if (_port.IsCurrent(generation)) OnDevEventEditorMenuSelected(selected); }, delegate
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
			_port.OpenExportFolderPicker(EventEditorUiText.T("AF_UI_Event_Menu_ExportPicker", "Export all (event editor) - choose a folder)"), ExportImportScope.EventData, OpenDevEventEditorMenu);
			break;
		case "import_event_data":
			_port.OpenImportFolderPicker(EventEditorUiText.T("AF_UI_Event_Menu_ImportPicker", "Import all (event editor) - choose a folder)"), ExportImportScope.EventData, OpenDevEventEditorMenu);
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
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Field_WorldSummary", "World opening summary: ") + (string.IsNullOrWhiteSpace(_port.WorldOpeningSummary()) ? EventEditorUiText.T("AF_UI_Event_Status_NotSet", "not set") : EventEditorUiText.T("AF_UI_Event_Status_Set", "set")));
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Status_KingdomSummary", "Kingdom opening summaries: {SET}/{TOTAL} set", ("SET", num.ToString()), ("TOTAL", num2.ToString())));
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Status_EventRecords", "Event records: {COUNT}", ("COUNT", ((_port.EventRecords() != null) ? _port.SanitizeEventRecordEntries(_port.EventRecords()).Count : 0).ToString())));
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Field_ReportProfile", "Weekly report profile: ") + _port.PromptProfileLabel());
		List<Kingdom> editableKingdoms = EventEditorProjection.GetDevEditableKingdoms();
		int num3 = editableKingdoms.Count;
		int num4 = editableKingdoms.Count((Kingdom x) => _port.GetKingdomStabilityValue(x) != _port.StabilityDefault);
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Status_Stability", "Kingdom stability: {CHANGED}/{TOTAL} values differ from default", ("CHANGED", num4.ToString()), ("TOTAL", num3.ToString())));
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEditWorldOpeningSummary()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		DevTextEditorHelper.ShowLongTextEditor(EventEditorUiText.T("AF_UI_Event_Menu_EditWorld", "Edit world opening summary"), EventEditorUiText.T("AF_UI_Event_EditWorld_Subtitle", "This text provides the initial background for the world event system."), EventEditorUiText.T("AF_UI_Event_EditWorld_Hint", "Enter the world opening summary (leave blank to clear)."), _port.WorldOpeningSummary() ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			if (!_port.SetDeveloperWorldOpeningSummary(input, generation)) return;
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(_port.WorldOpeningSummary()) ? EventEditorUiText.T("AF_UI_Event_EditWorld_Cleared", "World opening summary cleared.") : EventEditorUiText.T("AF_UI_Event_EditWorld_Updated", "World opening summary updated.")));
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
			InformationManager.DisplayMessage(new InformationMessage(EventEditorUiText.T("AF_UI_Event_Kingdom_None", "There are no editable kingdoms.")));
			OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), null));
		foreach (Kingdom item in list)
		{
			string devKingdomSummaryLabel = BuildDevKingdomSummaryLabel(item);
			list2.Add(new InquiryElement(new DevKingdomSummaryMenuItem
			{
				KingdomId = item.StringId ?? "",
				DisplayName = item.Name?.ToString() ?? (item.StringId ?? EventEditorUiText.T("AF_UI_Event_Kingdom_Fallback", "kingdom"))
			}, devKingdomSummaryLabel, null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Kingdom_ChooseBody", "Choose a kingdom opening summary to edit.\nThese texts provide the background for that kingdom's future weekly events."));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(EventEditorUiText.T("AF_UI_Event_Menu_EditKingdom", "Edit kingdom opening summaries"), stringBuilder.ToString().TrimEnd(), list2, isExitShown: true, 0, 1, EventEditorUiText.T("AF_UI_Event_Action_Edit", "Edit"), EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), selected => { if (_port.IsCurrent(generation)) OnDevKingdomOpeningSummaryMenuSelected(selected); }, delegate
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
				InformationManager.DisplayMessage(new InformationMessage(EventEditorUiText.T("AF_UI_Event_Kingdom_NotFound", "The selected kingdom could not be found.")));
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
		string text2 = kingdom.Name?.ToString() ?? (kingdom.StringId ?? EventEditorUiText.T("AF_UI_Event_Kingdom_Fallback", "kingdom"));
		string subtitleText = EventEditorUiText.T("AF_UI_Event_Kingdom_EditSubtitle", "This text provides the kingdom's opening background for future weekly event generation.");
		DevTextEditorHelper.ShowLongTextEditor(EventEditorUiText.T("AF_UI_Event_Kingdom_EditTitle", "Edit kingdom opening summary - {KINGDOM}", ("KINGDOM", text2)), subtitleText, EventEditorUiText.T("AF_UI_Event_Kingdom_EditHint", "Enter this kingdom's opening summary (leave blank to clear)."), text, delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			_port.SaveKingdomOpeningSummary(kingdom, input);
			InformationManager.DisplayMessage(new InformationMessage(string.IsNullOrWhiteSpace(input) ? EventEditorUiText.T("AF_UI_Event_Kingdom_Cleared", "{KINGDOM}'s opening summary was cleared.", ("KINGDOM", text2)) : EventEditorUiText.T("AF_UI_Event_Kingdom_Updated", "{KINGDOM}'s opening summary was updated.", ("KINGDOM", text2))));
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
		InformationManager.ShowInquiry(new InquiryData(EventEditorUiText.T("AF_UI_Event_Clear_Title", "Confirm clearing event summaries"), EventEditorUiText.T("AF_UI_Event_Clear_Body", "This clears the world opening summary and all kingdom opening summaries.\nThis action cannot be undone. Continue?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, EventEditorUiText.T("AF_UI_Event_Clear_Confirm", "Clear"), EventEditorUiText.T("AF_UI_Event_Clear_Cancel", "Cancel"), delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			if (!_port.ClearDeveloperOpeningSummaries(generation)) return;
			InformationManager.DisplayMessage(new InformationMessage(EventEditorUiText.T("AF_UI_Event_Clear_Done", "All event summaries cleared.")));
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
			return EventEditorUiText.T("AF_UI_Event_Kingdom_Invalid", "Invalid kingdom");
		}
		string text = kingdom.Name?.ToString() ?? (kingdom.StringId ?? EventEditorUiText.T("AF_UI_Event_Kingdom_Fallback", "kingdom"));
		string kingdomOpeningSummary = _port.GetKingdomOpeningSummary(kingdom);
		string text2 = string.IsNullOrWhiteSpace(kingdomOpeningSummary) ? EventEditorUiText.T("AF_UI_Event_Status_NotSet", "not set") : EventEditorUiText.T("AF_UI_Event_Status_Set", "set");
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
		list2.Add(new InquiryElement("back", EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", EventEditorUiText.T("AF_UI_Event_Action_PreviousPage", "Previous page"), null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", EventEditorUiText.T("AF_UI_Event_Action_NextPage", "Next page"), null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventRecordEntry item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, EventEditorProjection.BuildDevEventRecordMenuLabel(_port, item), null));
		}
		string text = BuildDevEventViewerDescription(list, page, num);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(EventEditorUiText.T("AF_UI_Event_Viewer_Title", "Event viewer"), text, list2, isExitShown: true, 0, 1, EventEditorUiText.T("AF_UI_Event_Action_View", "View"), EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), delegate(List<InquiryElement> selected)
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
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Viewer_Body", "Review events recorded by the event system and the materials referenced by each event.\nMaterials may include world and kingdom opening summaries, recent NPC actions, major NPC actions, and future summary sources.\n\nTotal events: {COUNT}\nPage: {PAGE}/{PAGES}{EMPTY}", ("COUNT", ((entries != null) ? entries.Count : 0).ToString()), ("PAGE", (page + 1).ToString()), ("PAGES", Math.Max(1, totalPages).ToString()), ("EMPTY", entries == null || entries.Count == 0 ? EventEditorUiText.T("AF_UI_Event_Viewer_Empty", "\n\nThere are no event records yet.\nFuture weekly events will appear here with their referenced materials.") : string.Empty)));
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
		list2.Add(new InquiryElement("back", EventEditorUiText.T("AF_UI_Event_Action_BackToList", "Back to event list"), null));
		if (!string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			list2.Add(new InquiryElement("view_short", EventEditorUiText.T("AF_UI_Event_Detail_Short", "View short summary"), null));
		}
		list2.Add(new InquiryElement("view_report", EventEditorUiText.T("AF_UI_Event_Detail_Report", "View weekly report"), null));
		list2.Add(new InquiryElement("edit_title", EventEditorUiText.T("AF_UI_Event_Detail_EditTitle", "Edit title"), null));
		list2.Add(new InquiryElement("edit_report", EventEditorUiText.T("AF_UI_Event_EditReport_Title", "Edit weekly report"), null));
		if (!string.IsNullOrWhiteSpace(entry.TagText))
		{
			list2.Add(new InquiryElement("view_tags", EventEditorUiText.T("AF_UI_Event_Detail_Tags", "View tag layer"), null));
		}
		if (!string.IsNullOrWhiteSpace(entry.PromptText))
		{
			list2.Add(new InquiryElement("view_prompt", EventEditorUiText.T("AF_UI_Event_Detail_Prompt", "View request Prompt"), null));
		}
		if ((entry.Materials?.Count).GetValueOrDefault() > 0)
		{
			list2.Add(new InquiryElement("view_materials", EventEditorUiText.T("AF_UI_Event_Detail_Materials", "View materials list"), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(EventEditorUiText.T("AF_UI_Event_Detail_Title", "Event details"), BuildDevEventRecordCompactDetailText(entry), list2, isExitShown: true, 0, 1, EventEditorUiText.T("AF_UI_Event_Action_Enter", "Enter"), EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), delegate(List<InquiryElement> selected)
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
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Title", "Event title"), (entry.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Type", "Event type"), _port.TranslateEventKindForDev(entry.EventKind));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_WeekLabel", "Week"), EventEditorUiText.T("AF_UI_Event_Field_Week", "Week {WEEK}", ("WEEK", Math.Max(0, entry.WeekIndex).ToString())));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_DateLabel", "Created date"), !string.IsNullOrWhiteSpace(entry.CreatedDate) ? entry.CreatedDate.Trim() : EventEditorUiText.T("AF_UI_Event_Field_Date", "Day {DAY}", ("DAY", Math.Max(0, entry.CreatedDay).ToString())));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Kingdom", "Kingdom"), _port.ResolveKingdomDisplay(entry.ScopeKingdomId));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_MaterialCount", "Material count"), ((entry.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Short", "Short summary"), string.IsNullOrWhiteSpace(entry.ShortSummary) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorProjection.BuildDevSummaryPreview(entry.ShortSummary, 48));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Tags", "Tag layer"), string.IsNullOrWhiteSpace(entry.TagText) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorUiText.T("AF_UI_Event_Field_Saved", "saved"));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Prompt", "Prompt"), string.IsNullOrWhiteSpace(entry.PromptText) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorUiText.T("AF_UI_Event_Field_Saved", "saved"));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_Operations", "Operations"));
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_OperationsBody", "The short summary, weekly report, tag layer, Prompt, and materials list each have a separate entry point."));
		if (string.IsNullOrWhiteSpace(entry.ShortSummary))
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_NoShort", "This event has no short summary."));
		}
		else
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_HasShort", "This event has a short summary; use View short summary to open it."));
		}
		if (string.IsNullOrWhiteSpace(entry.Summary))
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_NoReport", "This event has no report body."));
		}
		else
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_HasReport", "This event has a report body; use View weekly report or Edit weekly report to open it."));
		}
		if (string.IsNullOrWhiteSpace(entry.TagText))
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_NoTags", "This event has no tag layer."));
		}
		else
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_HasTags", "This event has a tag layer; use View tag layer to open it."));
		}
		if (entry.Materials == null || entry.Materials.Count == 0)
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_NoMaterials", "This event has no attached material references."));
		}
		else
		{
			stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Detail_HasMaterials", "This event has attached materials; use View materials list to open them."));
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal string BuildDevEventRecordCompactDetailText(EventRecordEntry entry)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Title", "Event title"), (entry?.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Type", "Event type"), _port.TranslateEventKindForDev(entry?.EventKind));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_WeekLabel", "Week"), EventEditorUiText.T("AF_UI_Event_Field_Week", "Week {WEEK}", ("WEEK", Math.Max(0, entry?.WeekIndex ?? 0).ToString())));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_DateLabel", "Created date"), !string.IsNullOrWhiteSpace(entry?.CreatedDate) ? entry.CreatedDate.Trim() : EventEditorUiText.T("AF_UI_Event_Field_Date", "Day {DAY}", ("DAY", Math.Max(0, entry?.CreatedDay ?? 0).ToString())));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Kingdom", "Kingdom"), _port.ResolveKingdomDisplay(entry?.ScopeKingdomId));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_MaterialCount", "Material count"), ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Short", "Short summary"), string.IsNullOrWhiteSpace(entry?.ShortSummary) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorProjection.BuildDevSummaryPreview(entry.ShortSummary, 48));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Tags", "Tag layer"), string.IsNullOrWhiteSpace(entry?.TagText) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorUiText.T("AF_UI_Event_Field_Saved", "saved"));
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Prompt", "Prompt"), string.IsNullOrWhiteSpace(entry?.PromptText) ? EventEditorUiText.T("AF_UI_Event_Field_NotSaved", "not saved") : EventEditorUiText.T("AF_UI_Event_Field_Saved", "saved"));
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevEventReportDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry(EventEditorUiText.T("AF_UI_Event_Report_Title", "Weekly report - {TITLE}", ("TITLE", (entry?.Title ?? "").Trim())), "", string.IsNullOrWhiteSpace(entry?.Summary) ? EventEditorUiText.T("AF_UI_Event_Empty_Report", "This event has no report body.") : entry.Summary.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, EventEditorUiText.T("AF_UI_Event_Action_BackToDetails", "Back to event details"));
	}

	internal void OpenDevEventShortSummaryDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry(EventEditorUiText.T("AF_UI_Event_Short_Title", "Short summary - {TITLE}", ("TITLE", (entry?.Title ?? "").Trim())), "", string.IsNullOrWhiteSpace(entry?.ShortSummary) ? EventEditorUiText.T("AF_UI_Event_Empty_Short", "This event has no short summary.") : entry.ShortSummary.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, EventEditorUiText.T("AF_UI_Event_Action_BackToDetails", "Back to event details"));
	}

	internal void OpenDevEventTagDetail(EventRecordEntry entry, int returnPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		_port.ShowDevLargeTextOrInquiry(EventEditorUiText.T("AF_UI_Event_Tags_Title", "Tag layer - {TITLE}", ("TITLE", (entry?.Title ?? "").Trim())), "", string.IsNullOrWhiteSpace(entry?.TagText) ? EventEditorUiText.T("AF_UI_Event_Empty_Tags", "This event has no tag layer.") : entry.TagText.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, EventEditorUiText.T("AF_UI_Event_Action_BackToDetails", "Back to event details"));
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
		list2.Add(new InquiryElement("back", EventEditorUiText.T("AF_UI_Event_Action_BackToDetails", "Back to event details"), null));
		if (page > 0)
		{
			list2.Add(new InquiryElement("prev_page", EventEditorUiText.T("AF_UI_Event_Action_PreviousPage", "Previous page"), null));
		}
		if (page + 1 < num)
		{
			list2.Add(new InquiryElement("next_page", EventEditorUiText.T("AF_UI_Event_Action_NextPage", "Next page"), null));
		}
		list2.Add(new InquiryElement("__sep__", "----------------", null));
		foreach (EventMaterialReference item in list.Skip(page * pageSize).Take(pageSize))
		{
			list2.Add(new InquiryElement(item, EventEditorProjection.BuildDevEventMaterialItemLabel(_port, item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(EventEditorUiText.T("AF_UI_Event_Materials_Title", "Event materials"), BuildDevEventMaterialListText(entry, page, num), list2, isExitShown: true, 0, 1, EventEditorUiText.T("AF_UI_Event_Action_ViewMaterial", "View material"), EventEditorUiText.T("AF_UI_Event_Action_Back", "Back"), delegate(List<InquiryElement> selected)
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
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Title", "Event title"), (entry?.Title ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_MaterialCount", "Material count"), ((entry?.Materials != null) ? entry.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, EventEditorUiText.T("AF_UI_Event_Field_Page", "Page"), EventEditorUiText.T("AF_UI_Event_Materials_Page", "Page {PAGE}/{PAGES}", ("PAGE", (page + 1).ToString()), ("PAGES", Math.Max(1, totalPages).ToString())));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(EventEditorUiText.T("AF_UI_Event_Materials_Body", "Each item below is a material referenced while this weekly report was generated."));
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
		DevTextEditorHelper.ShowLongTextEditor(EventEditorUiText.T("AF_UI_Event_EditTitle_Title", "Edit weekly report title"), EventEditorUiText.T("AF_UI_Event_EditTitle_Subtitle", "Edit the title saved for this event record."), EventEditorUiText.T("AF_UI_Event_EditTitle_Hint", "Enter a new title (leave blank to use the default title)."), entry.Title ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			EventRecordEntry updatedEntry = _port.ApplyDeveloperEventTitle(entry, input, generation);
			if (updatedEntry == null) return;
			InformationManager.DisplayMessage(new InformationMessage(EventEditorUiText.T("AF_UI_Event_EditTitle_Updated", "Weekly report title updated.")));
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
		DevTextEditorHelper.ShowLongTextEditor(EventEditorUiText.T("AF_UI_Event_EditReport_Title", "Edit weekly report"), EventEditorUiText.T("AF_UI_Event_EditReport_Subtitle", "This changes the final report text shown to players; the original materials are unchanged."), EventEditorUiText.T("AF_UI_Event_EditReport_Hint", "Enter the new weekly report body."), entry.Summary ?? "", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			EventRecordEntry updatedEntry = _port.ApplyDeveloperEventReport(entry, input, generation);
			if (updatedEntry == null) return;
			InformationManager.DisplayMessage(new InformationMessage(EventEditorUiText.T("AF_UI_Event_EditReport_Updated", "Weekly report body updated.")));
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
		_port.ShowDevLargeTextOrInquiry(EventEditorUiText.T("AF_UI_Event_Prompt_Title", "Request Prompt - {TITLE}", ("TITLE", (entry?.Title ?? "").Trim())), "", string.IsNullOrWhiteSpace(entry?.PromptText) ? EventEditorUiText.T("AF_UI_Event_Empty_Prompt", "No request Prompt was saved for this weekly report.") : entry.PromptText.Trim(), delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventRecordDetail(entry, returnPage);
		}, EventEditorUiText.T("AF_UI_Event_Action_BackToDetails", "Back to event details"));
	}

	internal void OpenDevEventMaterialDetail(EventRecordEntry entry, EventMaterialReference material, int returnPage, int returnMaterialPage)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		string text = _port.BuildDevEventMaterialDetailText(material);
		string text2 = EventEditorProjection.BuildDevEventMaterialItemLabel(_port, material);
		_port.ShowDevLargeTextOrInquiry(EventEditorUiText.T("AF_UI_Event_Material_Title", "Material details - {MATERIAL}", ("MATERIAL", text2)), "", text, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevEventMaterialList(entry, returnPage, returnMaterialPage);
		}, EventEditorUiText.T("AF_UI_Event_Action_BackToMaterials", "Back to materials list"));
	}
}
