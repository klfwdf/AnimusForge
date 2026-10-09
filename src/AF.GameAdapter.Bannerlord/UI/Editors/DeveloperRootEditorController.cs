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
		list.Add(new InquiryElement("edit_hero", DeveloperUiText.T("AF_UI_Developer_Edit_HeroNPC_compressed_memory_debt_personality_","Edit HeroNPC (compressed memory/debt/personality and background)..."), null));
		list.Add(new InquiryElement("single_ie", DeveloperUiText.T("AF_UI_Developer_Single_HeroNPC_import_export","Single HeroNPC import/export..."), null));
		list.Add(new InquiryElement("export_hero_all", DeveloperUiText.T("AF_UI_Developer_Export_all_HeroNPC_data_compressed_memory_debt_p","Export all HeroNPC data (compressed memory, debt, personality/background)"), null));
		list.Add(new InquiryElement("import_hero_all", DeveloperUiText.T("AF_UI_Developer_Import_all_HeroNPC_data_compressed_memory_debt_p","Import all HeroNPC data (compressed memory, debt, personality/background)"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.T("AF_UI_Developer_HeroNPC_edit_import_export","HeroNPC edit/import/export"), DeveloperUiText.T("AF_UI_Developer_Choose_an_operation","Choose an operation:"), list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Enter","Enter"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), selected => { if (_port.IsCurrent(generation)) OnDevHeroNpcMenuSelected(selected); }, delegate
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
				_port.OpenExportFolderPicker(DeveloperUiText.T("AF_UI_Developer_Export_all_HeroNPC_data_choose_a_folder","Export all HeroNPC data - choose a folder"), ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			case "import_hero_all":
				_port.OpenImportFolderPicker(DeveloperUiText.T("AF_UI_Developer_Import_all_HeroNPC_data_choose_a_folder","Import all HeroNPC data - choose a folder"), ExportImportScope.HeroNpcAll, OpenDevHeroNpcMenu);
				break;
			}
		}
	}

	internal void OpenDevAllDataMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_all", DeveloperUiText.T("AF_UI_Developer_Export_all_data_choose_a_folder","Export all data (choose a folder)"), null));
		list.Add(new InquiryElement("import_all", DeveloperUiText.T("AF_UI_Developer_Import_all_data_choose_a_folder","Import all data (choose a folder)"), null));
		list.Add(new InquiryElement("clear_all", DeveloperUiText.T("AF_UI_Developer_Clear_all_data_current_save_exported_backups_are","Clear all data (current save; exported backups are kept)"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.T("AF_UI_Developer_All_data_management","All data management"), DeveloperUiText.T("AF_UI_Developer_Choose_operation_summary","Choose an operation:\n") + BuildAllDataSummaryText(), list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Enter","Enter"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), selected => { if (_port.IsCurrent(generation)) OnDevAllDataMenuSelected(selected); }, delegate
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
				_port.OpenExportFolderPicker(DeveloperUiText.T("AF_UI_Developer_Export_all_data_choose_a_folder","Export all data - choose a folder"), ExportImportScope.All, OpenDevAllDataMenu);
			}
			else if (text == "import_all")
			{
				_port.OpenImportFolderPicker(DeveloperUiText.T("AF_UI_Developer_Import_all_data_choose_a_folder","Import all data - choose a folder"), ExportImportScope.All, OpenDevAllDataMenu);
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
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_HeroPersona","Hero personality/background: {COUNT}", "COUNT", _port.CountPersonaProfiles().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Dialogue","Old dialogue history: {COUNT} people", "COUNT", _port.CountDialogueHistory().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Memory","Compressed memory: {COUNT} people", "COUNT", _port.CountCompressedMemoryOwnersForDev().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Actions","NPC action records: {COUNT} people", "COUNT", _port.CountNpcActionOwnersForDev().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Debt","Debt: {COUNT} people", "COUNT", _port.CountDebtOwnersForDev().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Unnamed","Unnamed NPCs: {COUNT}", "COUNT", _port.CountUnnamedPersonaForDev().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Knowledge","Knowledge: {COUNT}", "COUNT", _port.CountKnowledgeRulesForDev().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Events","Event records: {COUNT}", "COUNT", _port.CountEventRecords().ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Stability","Kingdom stability: {COUNT}", "COUNT", _port.CountKingdomStability().ToString()));
			KingdomStrategicProfileBehavior kingdomProfiles = KingdomStrategicProfileBehavior.Instance;
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_KingdomProfiles","Kingdom strategy/personality: {COUNT} ({OVERRIDES} player overrides)", "COUNT", (kingdomProfiles?.GetProfileCountForDev() ?? 0).ToString(), "OVERRIDES", (kingdomProfiles?.GetPlayerOverrideCountForDev() ?? 0).ToString()));
			stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Summary_Voice","Voice mappings: {COUNT}", "COUNT", _port.CountVoiceMappingForDev().ToString()));
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return DeveloperUiText.T("AF_UI_Developer_Current_data_statistics_failed_but_clearing_is_s","Current data statistics failed, but clearing is still available.");
		}
	}

	internal void ConfirmClearAllData()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		string text = DeveloperUiText.T("AF_UI_Developer_ClearConfirm_Body","This clears AnimusForge data in the current save, including:\nHeroNPC personality/background, old dialogue history, compressed memory, debt, unnamed NPC persona, Knowledge, event records, kingdom stability, player kingdom strategy/personality overrides, voice mappings, patience state, and more.\n\nExported backups under PlayerExports are kept, and native world changes that already happened are not rolled back.\n\nThis action cannot be undone. Export all data first if you need a backup. Continue?");
		Action clear = delegate
		{
    if (!_port.IsCurrent(generation)) return;
			_port.ClearAllDataForCurrentSave();
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_ClearCompleted","Cleared AnimusForge data from the current save; kingdom strategy and personality were restored to the default baseline.")));
			OpenDevAllDataMenu();
		};
  Action[] choices = _confirmation.BeginConfirmation(clear, null, OpenDevAllDataMenu);
  InformationManager.ShowInquiry(new InquiryData(DeveloperUiText.T("AF_UI_Developer_Confirm_clearing_all_data","Confirm clearing all data"), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, DeveloperUiText.T("AF_UI_Developer_Clear","Clear"), DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), choices[0], choices[2]));
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
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_There_are_no_editable_NPCs","There are no editable NPCs.")));
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
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_There_are_no_editable_NPCs","There are no editable NPCs.")));
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
		list.Add(new InquiryElement("back", DeveloperUiText.T("AF_UI_Developer_Back","Back"), null));
		list.Add(new InquiryElement("search", DeveloperUiText.T("AF_UI_Developer_Search_NPC","Search NPC"), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", DeveloperUiText.T("AF_UI_Developer_Clear_search","Clear search"), null));
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
			list.Add(new InquiryElement("prev_page", DeveloperUiText.T("AF_UI_Developer_Previous_page","Previous page"), null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", DeveloperUiText.T("AF_UI_Developer_Next_page","Next page"), null));
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
		string descriptionText = DeveloperUiText.F("AF_UI_Developer_HeroSelection_Body","All NPCs: {TOTAL}; current results: {RESULTS}; page {PAGE}/{PAGES}.", "TOTAL", EditableHeroes.Count.ToString(), "RESULTS", filteredHeroes.Count.ToString(), "PAGE", (page + 1).ToString(), "PAGES", num.ToString());
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n" + DeveloperUiText.F("AF_UI_Developer_HeroSelection_Query","Search keyword: {QUERY}", "QUERY", text);
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n" + DeveloperUiText.T("AF_UI_Developer_HeroSelection_Empty","No matches; you can search again.");
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.T("AF_UI_Developer_Edit_HeroNPC_choose_an_NPC","Edit HeroNPC - choose an NPC"), descriptionText, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Confirm","Confirm"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), selected => { if (_port.IsCurrent(generation)) OnDevHeroSelected(selected); }, delegate
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
				InformationManager.ShowTextInquiry(new TextInquiryData(DeveloperUiText.T("AF_UI_Developer_Search_NPC","Search NPC"), DeveloperUiText.T("AF_UI_Developer_Enter_an_NPC_name_or_HeroId_to_search_all_NPCs","Enter an NPC name or HeroId to search all NPCs."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, DeveloperUiText.T("AF_UI_Developer_Search","Search"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), delegate(string input)
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
			stringBuilder.AppendLine(DeveloperUiText.T("AF_UI_Developer_Choose_operation","Choose an operation:"));
			stringBuilder.AppendLine(" - " + DeveloperUiText.T("AF_UI_Developer_Manage_memory","Manage compressed memory"));
			stringBuilder.AppendLine(" - " + DeveloperUiText.T("AF_UI_Developer_Edit_debt","Edit debt"));
			stringBuilder.AppendLine(" - " + DeveloperUiText.T("AF_UI_Developer_Edit_persona","Edit personality/background"));
			stringBuilder.AppendLine(" - " + DeveloperUiText.T("AF_UI_Developer_View_actions","View structured action records"));
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("edit_history", DeveloperUiText.T("AF_UI_Developer_Compressed_memory_management","Compressed memory management"), null));
			list.Add(new InquiryElement("edit_debt", DeveloperUiText.T("AF_UI_Developer_Edit_debt","Edit debt"), null));
			list.Add(new InquiryElement("edit_persona", DeveloperUiText.T("AF_UI_Developer_Edit_personality_background","Edit personality/background"), null));
			list.Add(new InquiryElement("view_actions", DeveloperUiText.T("AF_UI_Developer_View_structured_action_records","View structured action records"), null));
			if (NpcEditorReturnAction == null)
			{
				stringBuilder.AppendLine(" - " + DeveloperUiText.T("AF_UI_Developer_Switch_NPC","Switch NPC"));
				list.Add(new InquiryElement("switch_npc", DeveloperUiText.T("AF_UI_Developer_Switch_NPC","Switch NPC"), null));
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.F("AF_UI_Developer_Edit_HeroNPC_Title","Edit HeroNPC - {NPC}", "NPC", text), stringBuilder.ToString(), list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Enter","Enter"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), selected => { if (_port.IsCurrent(generation)) OnDevNpcMainMenuSelected(selected); }, delegate
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
		string text2 = DeveloperUiText.F("AF_UI_Developer_DebtPrompt","Current gold debt: {GOLD}.\nEnter a new gold debt value (0 allowed):", "GOLD", owedGold.ToString());
		InformationManager.ShowTextInquiry(new TextInquiryData(DeveloperUiText.F("AF_UI_Developer_DebtTitle","Set gold debt - {NPC}", "NPC", text), text2, isAffirmativeOptionShown: true, isNegativeOptionShown: true, DeveloperUiText.T("AF_UI_Developer_Confirm","Confirm"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			int result;
			if (string.IsNullOrWhiteSpace(input))
			{
				OpenDevDebtMenu(npc);
			}
			else if (!int.TryParse(input, out result) || result < 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_Enter_a_valid_non_negative_integer","Enter a valid non-negative integer.")));
				OpenDevSetDebtGoldSimple(npc);
			}
			else
			{
				RewardSystemBehavior.Instance.SetDebt(npc, result, items);
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.F("AF_UI_Developer_Gold_debt_updated","Gold debt updated to {GOLD}.", "GOLD", result.ToString())));
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
		string descriptionText = DeveloperUiText.T("AF_UI_Developer_Choose_operation","Choose an operation:");
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
		list.Add(new InquiryElement("set_gold", DeveloperUiText.T("AF_UI_Developer_Set_gold_debt","Set gold debt"), null));
		list.Add(new InquiryElement("clear_debt", DeveloperUiText.T("AF_UI_Developer_Clear_all_debt","Clear all debt"), null));
		list.Add(new InquiryElement("back", DeveloperUiText.T("AF_UI_Developer_Back","Back"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.F("AF_UI_Developer_Edit_debt_title","Edit debt - {NPC}", "NPC", text), descriptionText, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Apply","Apply"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), selected => { if (_port.IsCurrent(generation)) OnDevDebtMenuSelected(selected); }, delegate
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
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_All_debt_for_this_NPC_was_cleared","All debt for this NPC was cleared.")));
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
		list.Add(new InquiryElement("back", DeveloperUiText.T("AF_UI_Developer_Back","Back"), null));
		list.Add(new InquiryElement("recent", DeveloperUiText.T("AF_UI_Developer_View_recent_actions","View recent actions"), null));
		list.Add(new InquiryElement("major", DeveloperUiText.T("AF_UI_Developer_View_major_actions","View major actions"), null));
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", DeveloperUiText.T("AF_UI_Developer_Previous_page","Previous page"), null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", DeveloperUiText.T("AF_UI_Developer_Next_page","Next page"), null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (NpcActionEntry item in devNpcActionEntries.Skip(num2).Take(pageSize))
		{
			list.Add(new InquiryElement(item, NpcActionEditorProjection.BuildDevNpcActionItemLabel(_display, item), null));
		}
		string text = npc.Name?.ToString() ?? "NPC";
		string descriptionText = BuildDevNpcActionMenuDescription(npc, recentOnly, page, num, devNpcActionEntries.Count, devNpcActionEntries2.Count);
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(DeveloperUiText.T("AF_UI_Developer_Action_records","Action records - ") + text, descriptionText, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_View","View"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), delegate(List<InquiryElement> selected)
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
		stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Action_CurrentNpc","Current NPC: {NPC}", "NPC", text));
		stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Action_CurrentView","Current view: {VIEW}", "VIEW", recentOnly ? DeveloperUiText.T("AF_UI_Developer_Recent_actions","Recent actions") : DeveloperUiText.T("AF_UI_Developer_Major_actions","Major actions")));
		stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Action_RecentCount","Recent action count: {COUNT}", "COUNT", devNpcActionEntries.Count.ToString()));
		stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Action_MajorCount","Major action count: {COUNT}", "COUNT", majorCount.ToString()));
		stringBuilder.AppendLine(DeveloperUiText.F("AF_UI_Developer_Action_Page","Page {PAGE}/{PAGES}; entries on this page: {COUNT}", "PAGE", (page + 1).ToString(), "PAGES", Math.Max(1, totalPages).ToString(), "COUNT", currentCount.ToString()));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(DeveloperUiText.T("AF_UI_Developer_Notes","Notes:"));
		stringBuilder.AppendLine(DeveloperUiText.T("AF_UI_Developer_Action_records_are_stored_per_NPC"," - Action records are stored per NPC"));
		stringBuilder.AppendLine(DeveloperUiText.T("AF_UI_Developer_Details_show_structured_fields_such_as_time_plac"," - Details show structured fields such as time, place, people, clan, kingdom, and settlement ownership"));
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
		string text2 = DeveloperUiText.T("AF_UI_Developer_Action_details","Action details - ") + text;
		string text3 = NpcActionEditorProjection.BuildDevNpcActionDetailSubtitle(_display, entry);
		string text4 = NpcActionEditorProjection.BuildDevNpcActionDetailText(_display, entry);
		if (DevWeeklyReportPopup.Show(text2, text3, text4, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevNpcActionMenu(npc, recentOnly, page);
		}, DeveloperUiText.T("AF_UI_Developer_Back_to_action_list","Back to action list")))
		{
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", DeveloperUiText.T("AF_UI_Developer_Back_to_action_list","Back to action list"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(text2, text4, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Back","Back"), DeveloperUiText.T("AF_UI_Developer_Close","Close"), delegate
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
 { try { setTitle(DeveloperUiText.T("AF_UI_Developer_Developer_tools","Developer tools")); } catch { } }
 internal static bool RootEntryCondition(Action setSubmenu,Func<bool> readEnabled)
 { setSubmenu();return readEnabled(); }
 internal static void RootEntryConsequence(Func<bool> readEnabled,Action<string> switchMenu)
 {
  if(!readEnabled()) InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_Developer_data_management_is_disabled_enable_it_","Developer data management is disabled (enable it in MCM).")));
  else switchMenu("AnimusForge_dev_root");
 }
 internal static bool RootBackCondition(Action setLeave)
 { setLeave();return true; }
 internal static void ReturnToRootMenu(Action<string> switchMenu)
 { try { switchMenu("AnimusForge_dev_root"); } catch { } }
 internal static void OpenKingdomStrategicProfilesMenu(Func<Action> captureOpenMenu)
 {
  Action open=captureOpenMenu();
  if(open==null) { InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_Kingdom_strategy_and_personality_data_behavior_i","Kingdom strategy and personality data behavior is not initialized.")));return; }
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
