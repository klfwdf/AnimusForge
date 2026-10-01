using static AnimusForge.MyBehavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;
using DevWeeklyReportBatchPreviewEntry = AnimusForge.MyBehavior.DevWeeklyReportBatchPreviewEntry;

namespace AnimusForge;

internal sealed class WeeklyEditorPort : WeeklyEditorDisplayPort
{
 internal Func<long> CaptureGeneration; internal Func<long, bool> IsCurrent;
 internal Func<List<EventRecordEntry>> EventRecords;
 internal Func<string> WorldOpeningSummary;
 internal Func<EventRecordEntry, string, WorldBulletinPanelData> BuildBulletinPanel;
 internal Action<string> AwardReadingXp;
 internal delegate List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroupsCapability();
 internal BuildWeeklyEventMaterialPreviewGroupsCapability BuildWeeklyEventMaterialPreviewGroups;
 internal delegate List<WeeklyEventMaterialPreviewGroup> OrderWeeklyReportGenerationGroupsCapability(List<WeeklyEventMaterialPreviewGroup> groups);
 internal OrderWeeklyReportGenerationGroupsCapability OrderWeeklyReportGenerationGroups;
 internal delegate List<WeeklyReportBatchRequest> BuildWeeklyReportBatchRequestsCapability(List<WeeklyEventMaterialPreviewGroup> groups, int weekIndex, int startDay, int endDay);
 internal BuildWeeklyReportBatchRequestsCapability BuildWeeklyReportBatchRequests;
 internal delegate DevWeeklyReportBatchPreviewEntry FindLatestWeeklyReportBatchDevPreviewCapability(WeeklyReportBatchRequest batch);
 internal FindLatestWeeklyReportBatchDevPreviewCapability FindLatestWeeklyReportBatchDevPreview;
 internal delegate string BuildWeeklyReportSystemPromptCapability(WeeklyEventMaterialPreviewGroup group);
 internal BuildWeeklyReportSystemPromptCapability BuildWeeklyReportSystemPrompt;
 internal delegate string BuildWeeklyReportUserPromptCapability(WeeklyEventMaterialPreviewGroup group, int weekIndex, int startDay, int endDay);
 internal BuildWeeklyReportUserPromptCapability BuildWeeklyReportUserPrompt;
 internal delegate string BuildWeeklyBatchReportSystemPromptCapability(WeeklyReportBatchRequest batch);
 internal BuildWeeklyBatchReportSystemPromptCapability BuildWeeklyBatchReportSystemPrompt;
 internal delegate string BuildWeeklyBatchReportUserPromptCapability(WeeklyReportBatchRequest batch);
 internal BuildWeeklyBatchReportUserPromptCapability BuildWeeklyBatchReportUserPrompt;
 internal delegate IEnumerable<EventMaterialReference> OrderWeeklyPreviewMaterialsCapability(List<EventMaterialReference> materials);
 internal OrderWeeklyPreviewMaterialsCapability OrderWeeklyPreviewMaterials;
 internal delegate NpcActionEntry ResolveEventMaterialNpcActionCapability(EventMaterialReference material);
 internal ResolveEventMaterialNpcActionCapability ResolveEventMaterialNpcAction;
 internal delegate List<string> GetKingdomIdsByPlayerProximityCapability(IEnumerable<string> kingdomIds);
 internal GetKingdomIdsByPlayerProximityCapability GetKingdomIdsByPlayerProximity;
 internal delegate List<Kingdom> GetDevEditableKingdomsCapability();
 internal GetDevEditableKingdomsCapability GetDevEditableKingdoms;
 internal delegate List<EventRecordEntry> SanitizeEventRecordEntriesCapability(List<EventRecordEntry> source);
 internal SanitizeEventRecordEntriesCapability SanitizeEventRecordEntries;
 internal delegate int GetWeeklyReportBatchSizeCapability();
 internal GetWeeklyReportBatchSizeCapability GetWeeklyReportBatchSize;
 internal delegate int GetWeeklyReportRequestsPerMinuteCapability();
 internal GetWeeklyReportRequestsPerMinuteCapability GetWeeklyReportRequestsPerMinute;
 internal delegate Task GenerateDevWeeklyReportsAsyncCapability();
 internal GenerateDevWeeklyReportsAsyncCapability GenerateDevWeeklyReportsAsync;
 internal delegate void OpenDevEventEditorMenuCapability();
 internal OpenDevEventEditorMenuCapability OpenDevEventEditorMenu;
 internal delegate string ResolveKingdomOpeningSummaryByIdCapability(string kingdomId);
 internal ResolveKingdomOpeningSummaryByIdCapability ResolveKingdomOpeningSummaryById;
 internal delegate string BuildDevNpcActionDetailTextCapability(NpcActionEntry entry);
 internal BuildDevNpcActionDetailTextCapability BuildDevNpcActionDetailText;
 internal delegate string GetDevNpcActionKindDisplayCapability(string actionKind);
 internal GetDevNpcActionKindDisplayCapability GetDevNpcActionKindDisplay;
 internal delegate List<string> ResolveHeroNamesCapability(IEnumerable<string> heroIds);
 internal ResolveHeroNamesCapability ResolveHeroNames;
 internal delegate List<string> ResolveClanNamesCapability(IEnumerable<string> clanIds);
 internal ResolveClanNamesCapability ResolveClanNames;
 internal delegate List<string> ResolveKingdomNamesCapability(IEnumerable<string> kingdomIds);
 internal ResolveKingdomNamesCapability ResolveKingdomNames;
 internal delegate string TranslateEventKindForDevCapability(string eventKind);
 internal TranslateEventKindForDevCapability TranslateEventKindForDev;
 internal delegate int GetCurrentGameDayIndexSafeCapability();
 internal GetCurrentGameDayIndexSafeCapability GetCurrentGameDayIndexSafe;
 internal delegate void EnsureWeekZeroOpeningSummaryEventsCapability(bool sanitizeAfter = true);
 internal EnsureWeekZeroOpeningSummaryEventsCapability EnsureWeekZeroOpeningSummaryEvents;
 internal delegate string BuildWeeklyReportBatchDisplayLabelCapability(WeeklyReportBatchRequest batch);
 internal BuildWeeklyReportBatchDisplayLabelCapability BuildWeeklyReportBatchDisplayLabel;
}

internal sealed class WeeklyReportEditorController
{
 private readonly WeeklyEditorPort _port;
 private long _confirmationTicket;
 private long _generation = long.MinValue;
 internal WeeklyEventMaterialPreviewGroup MaterialSelection, PromptSelection;
 internal EventMaterialReference MaterialDetail;
 internal WeeklyReportBatchRequest BatchSelection;
 internal int MaterialPage;
 internal WeeklyReportEditorController(WeeklyEditorPort port) { _port = port; }
 internal void SynchronizeGeneration(long generation)
 {
  if (_generation == generation) return;
  _generation = generation;
  _confirmationTicket++;
  MaterialSelection = PromptSelection = null; MaterialDetail = null; BatchSelection = null; MaterialPage = 0;
 }
 internal bool TryShowWorldBulletinPanel(EventRecordEntry entry, string eventId)
 {
  WorldBulletinPanelData data;
  try { data = _port.BuildBulletinPanel(entry, eventId); }
  catch (Exception ex) { Logger.Log("WorldBulletinPanel", "[WARN] panel data build failed, using legacy popup: " + ex.Message); return false; }
  long generation = _port.CaptureGeneration();
  return DevWeeklyReportPopup.ShowWorldBulletin(data, 10.0, () => { if (_port.IsCurrent(generation)) _port.AwardReadingXp(eventId); });
 }
	internal string BuildDevEventMaterialDetailText(EventMaterialReference material)
	{
		if (material == null)
		{
			return "无效素材。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, "素材类型", _port.TranslateEventMaterialTypeForDev(material.MaterialType));
		_port.AppendDevNpcActionField(stringBuilder, "素材标签", (material.Label ?? "").Trim());
		_port.AppendDevNpcActionField(stringBuilder, "人物", _port.ResolveHeroDisplay(material.HeroId));
		_port.AppendDevNpcActionField(stringBuilder, "王国", _port.ResolveKingdomDisplay(material.KingdomId));
		_port.AppendDevNpcActionField(stringBuilder, "定居点", _port.ResolveSettlementDisplay(material.SettlementId));
		_port.AppendDevNpcActionField(stringBuilder, "行动类型", _port.GetDevNpcActionKindDisplay(material.ActionKind));
		_port.AppendDevNpcActionField(stringBuilder, "相关人物", string.Join("、", _port.ResolveHeroNames(material.RelatedHeroIds)));
		_port.AppendDevNpcActionField(stringBuilder, "相关家族", string.Join("、", _port.ResolveClanNames(material.RelatedClanIds)));
		_port.AppendDevNpcActionField(stringBuilder, "相关王国", string.Join("、", _port.ResolveKingdomNames(material.RelatedKingdomIds)));
		_port.AppendDevNpcActionField(stringBuilder, "原始素材数", Math.Max(0, material.SourceMaterialCount).ToString());
		_port.AppendDevNpcActionField(stringBuilder, "来源StableKey", string.Join(" | ", (material.SourceStableKeys ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())));
		_port.AppendDevNpcActionField(stringBuilder, "来源ActionKind", string.Join(" | ", (material.SourceActionKinds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())));
		switch ((material.MaterialType ?? "").Trim().ToLowerInvariant())
		{
		case "world_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(!string.IsNullOrWhiteSpace(material.SnapshotText) ? material.SnapshotText.Trim() : ((_port.WorldOpeningSummary() ?? "").Trim()));
			break;
		case "kingdom_opening_summary":
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(!string.IsNullOrWhiteSpace(material.SnapshotText) ? material.SnapshotText.Trim() : _port.ResolveKingdomOpeningSummaryById(material.KingdomId));
			break;
		case "npc_recent_action":
		case "npc_major_action":
			NpcActionEntry npcActionEntry = _port.ResolveEventMaterialNpcAction(material);
			if (npcActionEntry != null)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(_port.BuildDevNpcActionDetailText(npcActionEntry));
			}
			else
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("【素材正文】");
				if (!string.IsNullOrWhiteSpace(material.SnapshotText))
				{
					stringBuilder.AppendLine(material.SnapshotText.Trim());
				}
				else
				{
					stringBuilder.AppendLine("未能在当前行动记录中定位到这条 NPC 行为，可能是旧记录被裁剪掉了。");
				}
			}
			break;
		default:
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【素材正文】");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(material.SnapshotText) ? "这条素材当前没有额外快照文本。" : material.SnapshotText.Trim());
			break;
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevWeeklyEventMaterialPreviewMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<WeeklyEventMaterialPreviewGroup> list = _port.BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement(item, BuildWeeklyEventMaterialPreviewGroupLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周事件素材预览", BuildWeeklyEventMaterialPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup)
			{
				OpenDevWeeklyEventMaterialPreviewGroupDetail(weeklyEventMaterialPreviewGroup, 0);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewMenu();
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			_port.OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildWeeklyEventMaterialPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		int currentGameDayIndexSafe = _port.GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int num3 = (groups != null) ? groups.Sum((WeeklyEventMaterialPreviewGroup x) => (x?.PromptMaterials?.Count).GetValueOrDefault()) : 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示“如果现在生成本周事件”，系统会拿去喂给事件生成器的素材池。");
		stringBuilder.AppendLine("当前按世界事件与各王国事件分组展示。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("当前周数：第 " + num2 + " 周");
		stringBuilder.AppendLine("当前取材区间：第 " + num + " 日 到 第 " + currentGameDayIndexSafe + " 日");
		stringBuilder.AppendLine("分组数量：" + ((groups != null) ? groups.Count : 0));
		stringBuilder.AppendLine("素材总数：" + num3);
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("当前已接入的素材：世界开局概要、王国开局概要、本周 NPC 行动。");
		return stringBuilder.ToString().TrimEnd();
	}

	internal List<WeeklyReportBrowserCountryData> GetTerminalWeeklyReportBrowserCountries()
	{
		try
		{
			_port.EnsureWeekZeroOpeningSummaryEvents();
		}
		catch
		{
		}
		List<EventRecordEntry> list = _port.SanitizeEventRecordEntries(_port.EventRecords());
		List<WeeklyReportBrowserCountryData> list2 = new List<WeeklyReportBrowserCountryData>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		WeeklyReportBrowserCountryData item = BuildWeeklyReportBrowserCountryData("world", "", "\u4e16\u754c\u5468\u62a5", isWorld: true, list);
		list2.Add(item);
		hashSet.Add("world:");
		foreach (Kingdom item2 in _port.GetDevEditableKingdoms().OrderBy((Kingdom x) => _port.ResolveKingdomDisplay(x?.StringId), StringComparer.OrdinalIgnoreCase))
		{
			string text = (item2?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && hashSet.Add("kingdom:" + text))
			{
				list2.Add(BuildWeeklyReportBrowserCountryData("kingdom", text, _port.ResolveKingdomDisplay(text), isWorld: false, list));
			}
		}
		foreach (string item3 in list.Where((EventRecordEntry x) => x != null && string.Equals((x.EventKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.ScopeKingdomId)).Select((EventRecordEntry x) => (x.ScopeKingdomId ?? "").Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => _port.ResolveKingdomDisplay(x), StringComparer.OrdinalIgnoreCase))
		{
			if (hashSet.Add("kingdom:" + item3))
			{
				list2.Add(BuildWeeklyReportBrowserCountryData("kingdom", item3, _port.ResolveKingdomDisplay(item3), isWorld: false, list));
			}
		}
		return list2;
	}

	internal void OpenDevWeeklyEventMaterialPreviewGroupDetail(WeeklyEventMaterialPreviewGroup group, int page)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		MaterialSelection = group;
		if (group == null)
		{
			OpenDevWeeklyEventMaterialPreviewMenu();
			return;
		}
		List<EventMaterialReference> list = _port.OrderWeeklyPreviewMaterials(group.Materials).ToList();
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 16;
		int num = Math.Max(1, (int)Math.Ceiling((double)Math.Max(1, list.Count) / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		MaterialPage = page;
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回分组列表", null));
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
			list2.Add(new InquiryElement(item, BuildWeeklyPreviewMaterialLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(group.Title ?? "素材预览", BuildWeeklyPreviewGroupDetailText(group, page, num), list2, isExitShown: true, 0, 1, "查看素材", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyEventMaterialPreviewMenu();
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "back":
					OpenDevWeeklyEventMaterialPreviewMenu();
					break;
				case "prev_page":
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page - 1);
					break;
				case "next_page":
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page + 1);
					break;
				default:
					OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page);
					break;
				}
			}
			else if (selected[0].Identifier is EventMaterialReference eventMaterialReference)
			{
				OpenDevWeeklyPreviewMaterialDetail(group, eventMaterialReference, page);
			}
			else
			{
				OpenDevWeeklyEventMaterialPreviewGroupDetail(group, page);
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyEventMaterialPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildWeeklyPreviewGroupDetailText(WeeklyEventMaterialPreviewGroup group, int page, int totalPages)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_port.AppendDevNpcActionField(stringBuilder, "分组标题", group.Title ?? "");
		_port.AppendDevNpcActionField(stringBuilder, "分组类型", _port.TranslateEventKindForDev(group.GroupKind));
		_port.AppendDevNpcActionField(stringBuilder, "关联王国", _port.ResolveKingdomDisplay(group.KingdomId));
		_port.AppendDevNpcActionField(stringBuilder, "素材数量", ((group.Materials != null) ? group.Materials.Count : 0).ToString());
		_port.AppendDevNpcActionField(stringBuilder, "页码", (page + 1) + "/" + Math.Max(1, totalPages));
		if (!string.IsNullOrWhiteSpace(group.Summary))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("【说明】");
			stringBuilder.AppendLine(group.Summary.Trim());
		}
		if (group.Materials == null || group.Materials.Count == 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("当前这个分组还没有可用素材。");
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevWeeklyPreviewMaterialDetail(WeeklyEventMaterialPreviewGroup group, EventMaterialReference material, int returnPage)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		MaterialSelection = group; MaterialDetail = material; MaterialPage = returnPage;
		string text = BuildDevEventMaterialDetailText(material);
		InformationManager.ShowInquiry(new InquiryData("本周素材详情", text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回素材列表", "", delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyEventMaterialPreviewGroupDetail(group, returnPage);
		}, null));
	}

	internal void OpenDevWeeklyReportPromptPreviewMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("single", "查看单组 Prompt", null));
		list.Add(new InquiryElement("batch", "查看 Batch Prompt/Response", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Prompt 预览", "选择要查看的调试视图。单组视图用于逐王国核对，Batch 视图用于查看实际批量请求和最近一次返回。", list, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				_port.OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text)
			{
				switch (text)
				{
				case "single":
					OpenDevWeeklyReportSinglePromptPreviewMenu();
					break;
				case "batch":
					OpenDevWeeklyBatchPromptPreviewMenu();
					break;
				default:
					_port.OpenDevEventEditorMenu();
					break;
				}
			}
			else
			{
				_port.OpenDevEventEditorMenu();
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyReportPromptPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevWeeklyReportSinglePromptPreviewMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<WeeklyEventMaterialPreviewGroup> list = _port.BuildWeeklyEventMaterialPreviewGroups();
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyEventMaterialPreviewGroup item in list)
		{
			list2.Add(new InquiryElement(item, BuildWeeklyEventMaterialPreviewGroupLabel(item), null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Prompt 预览", BuildWeeklyReportPromptPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看 Prompt", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				_port.OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				_port.OpenDevEventEditorMenu();
			}
			else if (selected[0].Identifier is WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup)
			{
				OpenDevWeeklyReportPromptDetail(weeklyEventMaterialPreviewGroup);
			}
			else
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			_port.OpenDevEventEditorMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildWeeklyReportPromptPreviewMenuDescription(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示当前这一周会发给大模型的周报请求 Prompt。");
		stringBuilder.AppendLine("当前只在开发态使用，生成结果会写回事件编辑，不会自动发给 NPC。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("分组数量：" + ((groups != null) ? groups.Count : 0));
		stringBuilder.AppendLine("篇幅档位：" + _port.PromptProfileLabel());
		stringBuilder.AppendLine("每分钟生成上限：" + _port.GetWeeklyReportRequestsPerMinute());
		stringBuilder.AppendLine("MaxTokens：" + _port.GetEventAndRebellionApiMaxTokens());
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevWeeklyBatchPromptPreviewMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		int currentGameDayIndexSafe = _port.GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		List<WeeklyReportBatchRequest> list = _port.BuildWeeklyReportBatchRequests(_port.OrderWeeklyReportGenerationGroups(_port.BuildWeeklyEventMaterialPreviewGroups()), num2, num, currentGameDayIndexSafe);
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (WeeklyReportBatchRequest item in list)
		{
			DevWeeklyReportBatchPreviewEntry latestWeeklyReportBatchDevPreview = _port.FindLatestWeeklyReportBatchDevPreview(item);
			string text = (latestWeeklyReportBatchDevPreview == null) ? "未执行" : (latestWeeklyReportBatchDevPreview.Success ? "有响应" : "失败响应");
			int count = (item?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Count;
			list2.Add(new InquiryElement(item, _port.BuildWeeklyReportBatchDisplayLabel(item) + " [" + count + "块 | " + text + "]", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("本周周报 Batch Prompt/Response", BuildWeeklyBatchPromptPreviewMenuDescription(list), list2, isExitShown: true, 0, 1, "查看详情", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is string text && text == "back")
			{
				OpenDevWeeklyReportPromptPreviewMenu();
			}
			else if (selected[0].Identifier is WeeklyReportBatchRequest weeklyReportBatchRequest)
			{
				OpenDevWeeklyBatchPromptDetail(weeklyReportBatchRequest);
			}
			else
			{
				OpenDevWeeklyBatchPromptPreviewMenu();
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyReportPromptPreviewMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildWeeklyBatchPromptPreviewMenuDescription(List<WeeklyReportBatchRequest> batches)
	{
		int num = (batches ?? new List<WeeklyReportBatchRequest>()).Sum((WeeklyReportBatchRequest x) => (x?.Groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Count);
		int num2 = (batches ?? new List<WeeklyReportBatchRequest>()).Count((WeeklyReportBatchRequest x) => _port.FindLatestWeeklyReportBatchDevPreview(x) != null);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("这里展示当前这一周实际会发送的批量周报请求。");
		stringBuilder.AppendLine("每个批次会显示完整 batch prompt，以及最近一次执行缓存下来的 response 原文。");
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine("批次数量：" + ((batches != null) ? batches.Count : 0));
		stringBuilder.AppendLine("覆盖周报目标：" + num);
		stringBuilder.AppendLine("已有最近响应缓存：" + num2);
		stringBuilder.AppendLine("批次上限：" + _port.GetWeeklyReportBatchSize());
		stringBuilder.AppendLine("篇幅档位：" + _port.PromptProfileLabel());
		stringBuilder.AppendLine("每分钟生成上限：" + _port.GetWeeklyReportRequestsPerMinute());
		stringBuilder.AppendLine("MaxTokens：" + _port.GetEventAndRebellionApiMaxTokens());
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevWeeklyReportPromptDetail(WeeklyEventMaterialPreviewGroup group)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		PromptSelection = group;
		int currentGameDayIndexSafe = _port.GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int num2 = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		string text = _port.BuildWeeklyReportSystemPrompt(group);
		string text2 = _port.BuildWeeklyReportUserPrompt(group, num2, num, currentGameDayIndexSafe);
		InformationManager.ShowInquiry(new InquiryData("周报 Prompt 详情", BuildWeeklyReportPromptPreviewText(group, text, text2), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回 Prompt 列表", "", delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyReportPromptPreviewMenu();
		}, null));
	}

	internal void OpenDevWeeklyBatchPromptDetail(WeeklyReportBatchRequest batch)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		BatchSelection = batch;
		string text = _port.BuildWeeklyBatchReportSystemPrompt(batch);
		string text2 = _port.BuildWeeklyBatchReportUserPrompt(batch);
		string text3 = BuildWeeklyBatchPromptPreviewText(batch, text, text2);
		DevWeeklyReportBatchPreviewEntry latestWeeklyReportBatchDevPreview = _port.FindLatestWeeklyReportBatchDevPreview(batch);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(text3);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【最近一次 Response】");
		if (latestWeeklyReportBatchDevPreview == null)
		{
			stringBuilder.AppendLine("当前没有缓存的 batch response。需要先实际生成一次本周周报。");
		}
		else
		{
			stringBuilder.AppendLine("执行结果：" + (latestWeeklyReportBatchDevPreview.Success ? "成功" : "失败"));
			stringBuilder.AppendLine("尝试次数：" + latestWeeklyReportBatchDevPreview.AttemptsUsed);
			if (!string.IsNullOrWhiteSpace(latestWeeklyReportBatchDevPreview.FailureReason))
			{
				stringBuilder.AppendLine("失败原因：" + latestWeeklyReportBatchDevPreview.FailureReason);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(latestWeeklyReportBatchDevPreview.ResponsePreview) ? "响应原文为空。" : latestWeeklyReportBatchDevPreview.ResponsePreview.Trim());
		}
		InformationManager.ShowInquiry(new InquiryData("Batch Prompt/Response 详情", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, "返回 Batch 列表", "", delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevWeeklyBatchPromptPreviewMenu();
		}, null));
	}

	internal void ConfirmGenerateDevWeeklyReports()
	{
		long editorGeneration = _port.CaptureGeneration();
		long ticket = ++_confirmationTicket;
		bool answered = false;
		if (!_port.IsCurrent(editorGeneration)) return;
		List<WeeklyEventMaterialPreviewGroup> list = _port.OrderWeeklyReportGenerationGroups(_port.BuildWeeklyEventMaterialPreviewGroups());
		int currentGameDayIndexSafe = _port.GetCurrentGameDayIndexSafe();
		int startDay = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		int weekIndex = Math.Max(1, currentGameDayIndexSafe / 7 + 1);
		int batchCount = _port.BuildWeeklyReportBatchRequests(list, weekIndex, startDay, currentGameDayIndexSafe).Count;
		List<string> list2 = _port.GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId));
		string text = ((list2.Count > 0) ? string.Join(" -> ", list2.Select(id => _port.ResolveKingdomDisplay(id)).Where((string x) => !string.IsNullOrWhiteSpace(x))) : "无");
		string message = "即将按当前周素材生成开发态周报草案。\n\n- 生成对象：世界周报 + 各王国周报\n- 生成结果：写入事件编辑中的事件记录\n- NPC 会常驻读取近期三个王国短周报；命中特定规则时读取完整周报\n- 生成优先级：最近王国 > 世界事件 > 其他王国按距离依次生成\n\n本次预计请求数：" + batchCount + "\n篇幅档位：" + _port.PromptProfileLabel() + "\n每分钟生成上限：" + _port.GetWeeklyReportRequestsPerMinute() + "\n按距离排序的王国：" + text + "\nMaxTokens：" + _port.GetEventAndRebellionApiMaxTokens() + "\n\n是否开始？";
		InformationManager.ShowInquiry(new InquiryData("生成本周周报草案", message, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "开始生成", "取消", delegate
		{
			if (!_port.IsCurrent(editorGeneration) || answered || ticket != _confirmationTicket) return;
			answered = true;
			_ = _port.GenerateDevWeeklyReportsAsync();
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration) || answered || ticket != _confirmationTicket) return;
			answered = true;
			_port.OpenDevEventEditorMenu();
		}));
	}

	private string BuildWeeklyEventMaterialPreviewGroupLabel(WeeklyEventMaterialPreviewGroup group)
		=> WeeklyEditorProjection.BuildWeeklyEventMaterialPreviewGroupLabel(_port, group);

	private WeeklyReportBrowserCountryData BuildWeeklyReportBrowserCountryData(string eventKind, string scopeKingdomId, string displayName, bool isWorld, List<EventRecordEntry> source)
		=> WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(_port, eventKind, scopeKingdomId, displayName, isWorld, source);

	private List<WeeklyReportBrowserEntryData> BuildWeeklyReportBrowserEntries(List<EventRecordEntry> source, string eventKind, string scopeKingdomId)
		=> WeeklyEditorProjection.BuildWeeklyReportBrowserEntries(_port, source, eventKind, scopeKingdomId);

	private string BuildWeeklyReportBrowserDefaultTitle(string eventKind, string scopeKingdomId, int weekIndex)
		=> WeeklyEditorProjection.BuildWeeklyReportBrowserDefaultTitle(_port, eventKind, scopeKingdomId, weekIndex);

	private string BuildWeeklyReportPromptPreviewText(WeeklyEventMaterialPreviewGroup group, string systemPrompt, string userPrompt)
		=> WeeklyEditorProjection.BuildWeeklyReportPromptPreviewText(_port, group, systemPrompt, userPrompt);

	private string BuildWeeklyBatchPromptPreviewText(WeeklyReportBatchRequest batch, string systemPrompt, string userPrompt)
		=> WeeklyEditorProjection.BuildWeeklyBatchPromptPreviewText(_port, batch, systemPrompt, userPrompt);

	private string BuildWeeklyPreviewMaterialLabel(EventMaterialReference material)
		=> WeeklyEditorProjection.BuildWeeklyPreviewMaterialLabel(_port, material);
}
