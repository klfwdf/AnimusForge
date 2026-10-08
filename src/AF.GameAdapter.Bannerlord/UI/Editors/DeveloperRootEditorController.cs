using System.Reflection;
using System;using System.Collections.Generic;using System.Linq;using System.Text;
using TaleWorlds.CampaignSystem;using TaleWorlds.Core;using TaleWorlds.Library;
using ExportImportScope = AnimusForge.MyBehavior.ExportImportScope;
namespace AnimusForge;
internal sealed class DeveloperRootEditorPort
{
 internal Func<long> CaptureGeneration; internal Func<long,bool> IsCurrent;
 internal Func<Hero> GetSelectedHero; internal Action<Hero> SetSelectedHero;
 internal Func<List<Hero>> GetEditableHeroes; internal Action<List<Hero>> SetEditableHeroes;
 internal Action<string> SetHistoryQuery;
 internal Func<Action> GetNpcEditorReturnAction; internal Action<Action> SetNpcEditorReturnAction;
 internal Func<int> CountPersonaProfiles;
 internal Func<int> CountDialogueHistory;
 internal Func<int> CountEventRecords;
 internal Func<int> CountKingdomStability;
 internal Func<int> CountCompressedMemoryOwnersForDev;
 internal Func<int> CountNpcActionOwnersForDev;
 internal Func<int> CountDebtOwnersForDev;
 internal Func<int> CountUnnamedPersonaForDev;
 internal Func<int> CountKnowledgeRulesForDev;
 internal Func<int> CountVoiceMappingForDev;
 internal Func<IEnumerable<string>> HistoryHeroIds;
 internal Func<IEnumerable<string>> PersonaHeroIds;
 internal Action OpenDevSingleNpcHeroSelection;
 internal Action ClearAllDataForCurrentSave;
 internal Action<Hero> OpenDevCompressedMemoryMenu;
 internal Action<Hero> OpenDevPersonaMenuFromTown;
 internal Action ReturnToDevRootMenu;
 internal delegate void OpenExportFolderPickerCapability(string title, ExportImportScope scope, Action onReturn);
 internal OpenExportFolderPickerCapability OpenExportFolderPicker;
 internal delegate void OpenImportFolderPickerCapability(string title, ExportImportScope scope, Action onReturn);
 internal OpenImportFolderPickerCapability OpenImportFolderPicker;
 internal delegate List<NpcActionEntry> GetDevNpcActionEntriesCapability(Hero npc, bool recentOnly);
 internal GetDevNpcActionEntriesCapability GetDevNpcActionEntries;
}
// Complete root/hero/debt/action editor UI. SelectedHero/list remain the shared unique EditorSession.
internal sealed class DeveloperRootEditorController
{
 private readonly DeveloperRootEditorPort _port;
 private readonly NpcActionEditorDisplayPort _display;
 private readonly DeveloperImportController _confirmation;
 private long _generation = long.MinValue;
 internal string HeroSelectionQuery = string.Empty;
 internal int HeroSelectionPage;
 internal DeveloperRootEditorController(DeveloperRootEditorPort port, NpcActionEditorDisplayPort display, DeveloperImportController confirmation) { _port = port; _display = display; _confirmation = confirmation; }
 internal void SynchronizeGeneration(long generation) { if (_generation==generation) return; _generation=generation; HeroSelectionQuery=string.Empty; HeroSelectionPage=0; }
 private Hero SelectedHero { get=>_port.GetSelectedHero(); set=>_port.SetSelectedHero(value); }
 private List<Hero> EditableHeroes { get=>_port.GetEditableHeroes(); set=>_port.SetEditableHeroes(value); }
 private Action NpcEditorReturnAction { get=>_port.GetNpcEditorReturnAction(); set=>_port.SetNpcEditorReturnAction(value); }
	internal void OpenDevHeroNpcMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("edit_hero", "编辑 HeroNPC（压缩记忆/赊账/个性背景）…", null));
		list.Add(new InquiryElement("single_ie", "单个 HeroNPC 导入/导出…", null));
		list.Add(new InquiryElement("export_hero_all", "全量导出（HeroNPC：压缩记忆+赊账+个性背景，选文件夹）", null));
		list.Add(new InquiryElement("import_hero_all", "全量导入（HeroNPC：压缩记忆+赊账+个性背景，选文件夹）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("HeroNPC 编辑/导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(generation)) OnDevHeroNpcMenuSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevHeroNpcMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_hero":
				OpenDevTownEditorHeroSelection();
				break;
			case "single_ie":
				_port.OpenDevSingleNpcHeroSelection();
				break;
			case "export_hero_all":
				_port.OpenExportFolderPicker("全量导出（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			case "import_hero_all":
				_port.OpenImportFolderPicker("全量导入（HeroNPC）- 选择文件夹", ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			}
		}
	}

	internal void OpenDevAllDataMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_all", "导出（全部，选择文件夹）", null));
		list.Add(new InquiryElement("import_all", "导入（全部，选择文件夹）", null));
		list.Add(new InquiryElement("clear_all", "清理全部数据（当前存档，不删除导出备份）", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("全部数据管理", "选择要执行的操作：\n" + BuildAllDataSummaryText(), list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(generation)) OnDevAllDataMenuSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevAllDataMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (selected != null && selected.Count != 0)
		{
			string text = selected[0].Identifier as string;
			if (text == "export_all")
			{
				_port.OpenExportFolderPicker("导出（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
			else if (text == "import_all")
			{
				_port.OpenImportFolderPicker("导入（全部）- 选择文件夹", ExportImportScope.All, OpenDevAllDataMenu);
			}
			else if (text == "clear_all")
			{
				ConfirmClearAllData();
			}
		}
	}

	internal string BuildAllDataSummaryText()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Hero 个性/背景：" + _port.CountPersonaProfiles() + " 条");
			stringBuilder.AppendLine("旧对话历史：" + _port.CountDialogueHistory() + " 人");
			stringBuilder.AppendLine("压缩记忆：" + _port.CountCompressedMemoryOwnersForDev() + " 人");
			stringBuilder.AppendLine("NPC 行动记录：" + _port.CountNpcActionOwnersForDev() + " 人");
			stringBuilder.AppendLine("赊账/欠款：" + _port.CountDebtOwnersForDev() + " 人");
			stringBuilder.AppendLine("未命名 NPC：" + _port.CountUnnamedPersonaForDev() + " 条");
			stringBuilder.AppendLine("Knowledge：" + _port.CountKnowledgeRulesForDev() + " 条");
			stringBuilder.AppendLine("事件记录：" + _port.CountEventRecords() + " 条");
			stringBuilder.AppendLine("王国稳定度：" + _port.CountKingdomStability() + " 条");
			KingdomStrategicProfileBehavior kingdomProfiles = KingdomStrategicProfileBehavior.Instance;
			stringBuilder.AppendLine("国家战略/性格：" + (kingdomProfiles?.GetProfileCountForDev() ?? 0) + " 条（玩家覆盖 " + (kingdomProfiles?.GetPlayerOverrideCountForDev() ?? 0) + " 条）");
			stringBuilder.AppendLine("声音映射：" + _port.CountVoiceMappingForDev() + " 个声音");
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "当前数据统计失败，但仍可执行清理。";
		}
	}

	internal void ConfirmClearAllData()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		string text = "这会清空当前存档中的 AnimusForge 数据，包括：\n" +
			"HeroNPC 个性/背景、旧对话历史、压缩记忆、赊账/欠款、未命名 NPC 个性、Knowledge、事件记录、王国稳定度、国家战略/性格玩家覆盖、声音映射、耐心状态等。\n\n" +
			"不会删除 PlayerExports 下已经导出的备份文件，也不会回滚已经真实发生的原版世界状态变化。\n\n" +
			"此操作不可撤销。建议先执行一次“导出（全部）”。是否继续？";
		Action clear = delegate
		{
    if (!_port.IsCurrent(generation)) return;
			_port.ClearAllDataForCurrentSave();
			InformationManager.DisplayMessage(new InformationMessage("已清理当前存档中的 AnimusForge 可清理数据；国家战略与性格已恢复默认基线。"));
			OpenDevAllDataMenu();
		};
  Action[] choices = _confirmation.BeginConfirmation(clear, null, OpenDevAllDataMenu);
  InformationManager.ShowInquiry(new InquiryData("确认清理全部数据", text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认清理", "取消", choices[0], choices[2]));
	}

	internal void OpenDevTownEditorHeroSelection()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		SelectedHero = null;
		_port.SetHistoryQuery(string.Empty);
		HeroSelectionQuery = string.Empty;
		EditableHeroes = BuildDevEditableHeroList();
		if (EditableHeroes == null || EditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的 NPC。"));
			OpenDevHeroNpcMenu();
			return;
		}
		OpenDevTownEditorHeroSelectionPaged(0, null);
	}

	internal void OpenDevTownEditorHeroSelectionPaged(int page, string query)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (EditableHeroes == null || EditableHeroes.Count == 0)
		{
			EditableHeroes = BuildDevEditableHeroList();
		}
		if (EditableHeroes == null || EditableHeroes.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的 NPC。"));
			OpenDevHeroNpcMenu();
			return;
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (query ?? "").Trim();
		HeroSelectionQuery = text;
		List<Hero> filteredHeroes = EditableHeroes;
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			filteredHeroes = EditableHeroes.Where(delegate(Hero h)
			{
				string text5 = ((h?.Name != null) ? h.Name.ToString() : "").Trim().ToLowerInvariant();
				string text6 = (h?.StringId ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("search", "搜索 NPC", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", "清空搜索", null));
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)filteredHeroes.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		HeroSelectionPage = page;
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (Hero item in filteredHeroes.Skip(num2).Take(pageSize))
		{
			string text2 = item?.Name?.ToString() ?? "NPC";
			string text3 = item?.StringId ?? "";
			string title = string.IsNullOrEmpty(text3) ? text2 : (text2 + " (ID=" + text3 + ")");
			list.Add(new InquiryElement(item, title, null));
		}
		string descriptionText = $"全部 NPC：{EditableHeroes.Count} 个";
		descriptionText = descriptionText + $"\n当前结果：{filteredHeroes.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n搜索关键词：" + text;
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - 选择NPC", descriptionText, list, isExitShown: true, 0, 1, "确定", "返回", selected => { if (_port.IsCurrent(generation)) OnDevHeroSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal List<Hero> BuildDevEditableHeroList()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<Hero> list = new List<Hero>();
		Action<string> action = delegate(string id)
		{
			string text6 = (id ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text6) || !hashSet.Add(text6))
			{
				return;
			}
			try
			{
				Hero hero5 = Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), text6, StringComparison.OrdinalIgnoreCase));
				if (hero5 != null && hero5 != Hero.MainHero)
				{
					list.Add(hero5);
				}
			}
			catch
			{
			}
		};
		try
		{
			foreach (Hero allAliveHero in Hero.AllAliveHeroes)
			{
				if (allAliveHero == null || allAliveHero == Hero.MainHero)
				{
					continue;
				}
				action(allAliveHero.StringId);
			}
		}
		catch
		{
		}
		foreach (string id in _port.HistoryHeroIds() ?? Enumerable.Empty<string>()) if (!string.IsNullOrEmpty(id)) action(id);
		foreach (string id in _port.PersonaHeroIds() ?? Enumerable.Empty<string>()) if (!string.IsNullOrEmpty(id)) action(id);
		if (RewardSystemBehavior.Instance != null)
		{
			List<string> allDebtorHeroIds = RewardSystemBehavior.Instance.GetAllDebtorHeroIds();
			if (allDebtorHeroIds != null)
			{
				foreach (string item3 in allDebtorHeroIds)
				{
					if (!string.IsNullOrEmpty(item3))
					{
						action(item3);
					}
				}
			}
		}
		if (list.Count > 1)
		{
			list = list.OrderBy((Hero h) => h.Name?.ToString() ?? "", StringComparer.OrdinalIgnoreCase).ThenBy((Hero h) => h.StringId ?? "", StringComparer.OrdinalIgnoreCase).ToList();
		}
		return list;
	}

	internal void OnDevHeroSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (selected == null || selected.Count == 0)
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text && text == "back")
		{
			OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text2)
		{
			switch (text2)
			{
			case "search":
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
				{
    if (!_port.IsCurrent(generation)) return;
					OpenDevTownEditorHeroSelectionPaged(0, input);
				}, delegate
				{
    if (!_port.IsCurrent(generation)) return;
					OpenDevTownEditorHeroSelectionPaged(0, HeroSelectionQuery);
				}, shouldInputBeObfuscated: false, null, HeroSelectionQuery ?? ""));
				return;
			case "clear_search":
				OpenDevTownEditorHeroSelectionPaged(0, null);
				return;
			case "prev_page":
				OpenDevTownEditorHeroSelectionPaged(HeroSelectionPage - 1, HeroSelectionQuery);
				return;
			case "next_page":
				OpenDevTownEditorHeroSelectionPaged(HeroSelectionPage + 1, HeroSelectionQuery);
				return;
			case "__sep__":
				OpenDevTownEditorHeroSelectionPaged(HeroSelectionPage, HeroSelectionQuery);
				return;
			}
		}
		if (!(selected[0].Identifier is Hero devEditingHero))
		{
			OpenDevHeroNpcMenu();
			return;
		}
		SelectedHero = devEditingHero;
		_port.SetHistoryQuery(string.Empty);
		ShowDevEditInquiry(SelectedHero);
	}

	internal void ShowDevEditInquiry(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc != null)
		{
			SelectedHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine(" - 管理压缩记忆");
			stringBuilder.AppendLine(" - 编辑赊账/欠款");
			stringBuilder.AppendLine(" - 编辑角色个性/历史背景");
			stringBuilder.AppendLine(" - 查看行动记录（结构化）");
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("edit_history", "压缩记忆管理", null));
			list.Add(new InquiryElement("edit_debt", "编辑赊账/欠款", null));
			list.Add(new InquiryElement("edit_persona", "编辑角色个性/历史背景", null));
			list.Add(new InquiryElement("view_actions", "查看行动记录（结构化）", null));
			if (NpcEditorReturnAction == null)
			{
				stringBuilder.AppendLine(" - 切换 NPC");
				list.Add(new InquiryElement("switch_npc", "切换 NPC", null));
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑 HeroNPC - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(generation)) OnDevNpcMainMenuSelected(selected); }, delegate
			{
    if (!_port.IsCurrent(generation)) return;
				ReturnFromDevNpcEditor(npc);
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	internal void OpenDevNpcEditorFromExternal(Hero npc, Action onFinished)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc == null)
		{
			try
			{
				onFinished?.Invoke();
			}
			catch
			{
			}
			return;
		}
		NpcEditorReturnAction = delegate
		{
    if (!_port.IsCurrent(generation)) return;
			try
			{
				EncyclopediaHeroPersonaPatch.QueueRefreshForHero(npc.StringId);
			}
			catch
			{
			}
			try
			{
				onFinished?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("NpcEditor", "[WARN] Hero NPC editor finish callback failed: " + ex.Message);
			}
		};
		SelectedHero = npc;
		_port.SetHistoryQuery(string.Empty);
		ShowDevEditInquiry(npc);
	}

	internal void ReturnFromDevNpcEditor(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Action devNpcEditorReturnAction = NpcEditorReturnAction;
		NpcEditorReturnAction = null;
		if (devNpcEditorReturnAction != null)
		{
			try
			{
				devNpcEditorReturnAction();
			}
			catch
			{
			}
			return;
		}
		OpenDevHeroNpcMenu();
	}

	internal void OpenDevSetDebtGoldSimple(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc == null || RewardSystemBehavior.Instance == null)
		{
			return;
		}
		RewardSystemBehavior.Instance.GetDebtSnapshot(npc, out var owedGold, out var items);
		string text = npc.Name?.ToString() ?? "NPC";
		string text2 = "当前金币欠款: " + owedGold + "。\n输入新的金币欠款数值（允许为 0）：";
		InformationManager.ShowTextInquiry(new TextInquiryData("设置金币欠款 - " + text, text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			int result;
			if (string.IsNullOrWhiteSpace(input))
			{
				OpenDevDebtMenu(npc);
			}
			else if (!int.TryParse(input, out result) || result < 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("请输入合法的非负整数."));
				OpenDevSetDebtGoldSimple(npc);
			}
			else
			{
				RewardSystemBehavior.Instance.SetDebt(npc, result, items);
				InformationManager.DisplayMessage(new InformationMessage("已更新金币欠款为 " + result + "。"));
				OpenDevDebtMenu(npc);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevDebtMenu(npc);
		}));
	}

	internal void OpenDevDebtMenu(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc == null)
		{
			return;
		}
		SelectedHero = npc;
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = "请选择要执行的操作：";
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				string text2 = RewardSystemBehavior.Instance.BuildDebtEditorSummary(npc, 10);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					descriptionText = text2;
				}
			}
		}
		catch
		{
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("set_gold", "设置金币欠款", null));
		list.Add(new InquiryElement("clear_debt", "清空所有欠款", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑赊账/欠款 - " + text, descriptionText, list, isExitShown: true, 0, 1, "执行", "返回", selected => { if (_port.IsCurrent(generation)) OnDevDebtMenuSelected(selected); }, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevDebtMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Hero devEditingHero = SelectedHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ShowDevEditInquiry(devEditingHero);
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "set_gold")
		{
			OpenDevSetDebtGoldSimple(devEditingHero);
		}
		else if (text == "clear_debt")
		{
			if (RewardSystemBehavior.Instance != null)
			{
				RewardSystemBehavior.Instance.SetDebt(devEditingHero, 0, new Dictionary<string, int>());
				InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的所有欠款."));
			}
			OpenDevDebtMenu(devEditingHero);
		}
		else
		{
			ShowDevEditInquiry(devEditingHero);
		}
	}

	internal void OnDevNpcMainMenuSelected(List<InquiryElement> selected)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Hero devEditingHero = SelectedHero;
		if (devEditingHero == null || selected == null || selected.Count == 0)
		{
			ReturnFromDevNpcEditor(devEditingHero);
			return;
		}
		if (devEditingHero != null)
		{
			switch (selected[0].Identifier as string)
			{
			case "edit_history":
				_port.OpenDevCompressedMemoryMenu(devEditingHero);
				break;
			case "edit_debt":
				OpenDevDebtMenu(devEditingHero);
				break;
			case "edit_persona":
				_port.OpenDevPersonaMenuFromTown(devEditingHero);
				break;
			case "view_actions":
				OpenDevNpcActionMenu(devEditingHero, recentOnly: true, 0);
				break;
			case "switch_npc":
				OpenDevTownEditorHeroSelection();
				break;
			}
		}
	}

	internal void OpenDevNpcActionMenu(Hero npc, bool recentOnly, int page)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc == null)
		{
			return;
		}
		SelectedHero = npc;
		List<NpcActionEntry> devNpcActionEntries = _port.GetDevNpcActionEntries(npc, recentOnly);
		List<NpcActionEntry> devNpcActionEntries2 = _port.GetDevNpcActionEntries(npc, recentOnly: false);
		if (page < 0)
		{
			page = 0;
		}
		const int pageSize = 18;
		int num = Math.Max(1, (int)Math.Ceiling((double)devNpcActionEntries.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("recent", "查看近期行动", null));
		list.Add(new InquiryElement("major", "查看重大行动", null));
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (NpcActionEntry item in devNpcActionEntries.Skip(num2).Take(pageSize))
		{
			list.Add(new InquiryElement(item, NpcActionEditorProjection.BuildDevNpcActionItemLabel(_display, item), null));
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = BuildDevNpcActionMenuDescription(npc, recentOnly, page, num, devNpcActionEntries.Count, devNpcActionEntries2.Count);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("行动记录 - " + text, descriptionText, list, isExitShown: true, 0, 1, "查看", "返回", delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				ShowDevEditInquiry(npc);
			}
			else if (selected[0].Identifier is string text2)
			{
				switch (text2)
				{
				case "back":
					ShowDevEditInquiry(npc);
					break;
				case "recent":
					OpenDevNpcActionMenu(npc, recentOnly: true, 0);
					break;
				case "major":
					OpenDevNpcActionMenu(npc, recentOnly: false, 0);
					break;
				case "prev_page":
					OpenDevNpcActionMenu(npc, recentOnly, page - 1);
					break;
				case "next_page":
					OpenDevNpcActionMenu(npc, recentOnly, page + 1);
					break;
				default:
					OpenDevNpcActionMenu(npc, recentOnly, page);
					break;
				}
			}
			else if (selected[0].Identifier is NpcActionEntry npcActionEntry)
			{
				OpenDevNpcActionDetail(npc, recentOnly, page, npcActionEntry);
			}
			else
			{
				OpenDevNpcActionMenu(npc, recentOnly, page);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			ShowDevEditInquiry(npc);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevNpcActionMenuDescription(Hero npc, bool recentOnly, int page, int totalPages, int currentCount, int majorCount)
	{
		string text = npc?.Name?.ToString() ?? "NPC";
		List<NpcActionEntry> devNpcActionEntries = _port.GetDevNpcActionEntries(npc, recentOnly: true);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("当前 NPC：" + text);
		stringBuilder.AppendLine("当前视图：" + (recentOnly ? "近期行动" : "重大行动"));
		stringBuilder.AppendLine("近期行动数：" + devNpcActionEntries.Count);
		stringBuilder.AppendLine("重大行动数：" + majorCount);
		stringBuilder.AppendLine($"第 {page + 1}/{Math.Max(1, totalPages)} 页，本页类型总数：{currentCount}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("说明：");
		stringBuilder.AppendLine(" - 行动记录已按单个 NPC 存储");
		stringBuilder.AppendLine(" - 详情里会显示时间、地点、人物、家族、王国、定居点归属等结构化字段");
		return stringBuilder.ToString().TrimEnd();
	}

	internal void OpenDevNpcActionDetail(Hero npc, bool recentOnly, int page, NpcActionEntry entry)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (npc == null || entry == null)
		{
			ShowDevEditInquiry(npc);
			return;
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string text2 = "行动详情 - " + text;
		string text3 = NpcActionEditorProjection.BuildDevNpcActionDetailSubtitle(_display, entry);
		string text4 = NpcActionEditorProjection.BuildDevNpcActionDetailText(_display, entry);
		if (DevWeeklyReportPopup.Show(text2, text3, text4, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, "返回行动列表"))
		{
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回行动列表", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(text2, text4, list, isExitShown: true, 0, 1, "返回", "关闭", delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevNpcActionMenu(npc, recentOnly, page);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

internal static bool TryParseDevSelectionInt(string id, string prefix, out int value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(id) || prefix == null || !id.StartsWith(prefix, StringComparison.Ordinal))
		{
			return false;
		}
		return int.TryParse(id.Substring(prefix.Length), out value);
	}

 internal static void InitializeRootMenuTitle(Action<string> setTitle)
 { try { setTitle("开发者工具"); } catch { } }
 internal static bool RootEntryCondition(Action setSubmenu,Func<bool> readEnabled)
 { setSubmenu();return readEnabled(); }
 internal static void RootEntryConsequence(Func<bool> readEnabled,Action<string> switchMenu)
 {
  if(!readEnabled()) InformationManager.DisplayMessage(new InformationMessage("开发者数据管理未开启（请在 MCM 中启用）。"));
  else switchMenu("AnimusForge_dev_root");
 }
 internal static bool RootBackCondition(Action setLeave)
 { setLeave();return true; }
 internal static void ReturnToRootMenu(Action<string> switchMenu)
 { try { switchMenu("AnimusForge_dev_root"); } catch { } }
 internal static void OpenKingdomStrategicProfilesMenu(Func<Action> captureOpenMenu)
 {
  Action open=captureOpenMenu();
  if(open==null) { InformationManager.DisplayMessage(new InformationMessage("国家战略与性格数据行为尚未初始化。"));return; }
  open();
 }

internal static void TryPersistMcmSettings(object settings)
	{
		try
		{
			MethodInfo method = settings?.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
			method?.Invoke(settings, null);
		}
		catch
		{
		}
	}

 internal static int ReadHistoryReturnCap(Func<int?> captureTopN,Func<int,int> clamp)
 {
  try { int? value=captureTopN();if(value.HasValue)return clamp(value.Value); } catch { }
  return 4;
 }
}
internal static class DeveloperEditorDataProjection
{
 internal static int CountNonEmptyOwners<T>(Dictionary<string,List<T>> first, Dictionary<string,List<T>> second)
 {
  var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  if(first!=null) foreach(var item in first) if(!string.IsNullOrWhiteSpace(item.Key)&&item.Value!=null&&item.Value.Count>0) ids.Add(item.Key);
  if(second!=null) foreach(var item in second) if(!string.IsNullOrWhiteSpace(item.Key)&&item.Value!=null&&item.Value.Count>0) ids.Add(item.Key);
  return ids.Count;
 }
 internal static int CountNonEmptyMemoryOwners(Dictionary<string,List<DailyMemoryDraft>> first, Dictionary<string,List<CompressedMemoryBlock>> second)
 {
  var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  if(first!=null) foreach(var item in first) if(!string.IsNullOrWhiteSpace(item.Key)&&item.Value!=null&&item.Value.Count>0) ids.Add(item.Key);
  if(second!=null) foreach(var item in second) if(!string.IsNullOrWhiteSpace(item.Key)&&item.Value!=null&&item.Value.Count>0) ids.Add(item.Key);
  return ids.Count;
 }


}
