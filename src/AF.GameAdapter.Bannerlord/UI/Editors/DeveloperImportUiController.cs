using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static AnimusForge.MyBehavior;
using TaleWorlds.Core;
using TaleWorlds.Library;
using ExportImportScope = AnimusForge.MyBehavior.ExportImportScope;

namespace AnimusForge;

internal sealed class DeveloperImportUiPort
{
 internal Func<long> CaptureGeneration;
 internal Func<long, bool> IsCurrent;
 internal Action ReturnToDevRootMenu;
 internal Action<string> ExportAllData;
 internal Action<string> ExportDebtData;
 internal Action<string> ExportDialogueHistoryData;
 internal Action<string> ExportEventData;
 internal Action<string> ExportHeroNpcAllData;
 internal Action<string> ExportKnowledgeData;
 internal Action<string> ExportPersonaData;
 internal Action<string, string> ExportSingleNpcDebtData;
 internal Action<string, string> ExportSingleNpcDialogueHistoryData;
 internal Action<string, string> ExportSingleNpcPersonaData;
 internal Action<string> ExportUnnamedPersonaData;
 internal Action<string> ExportVoiceMappingData;
 internal Action<string> ImportAllData;
 internal Action<string> ImportDebtData;
 internal Action<string> ImportDialogueHistoryData;
 internal Action<string> ImportEventData;
 internal Action<string> ImportHeroNpcAllData;
 internal Action<string> ImportKnowledgeData;
 internal Action<string> ImportPersonaData;
 internal Action<string, string> ImportSingleNpcDebtData;
 internal Action<string, string> ImportSingleNpcDialogueHistoryData;
 internal Action<string, string> ImportSingleNpcPersonaData;
 internal Action<string> ImportUnnamedPersonaData;
 internal Action<string> ImportVoiceMappingData;
}

// UI-open only: folder enumeration and confirmation/navigation. Domain commits are typed capabilities.
internal sealed class DeveloperImportUiController
{
 private readonly DeveloperImportUiPort _port;
 private readonly DeveloperImportController _confirmation;
 internal DeveloperImportUiController(DeveloperImportUiPort port, DeveloperImportController confirmation)
 { _port = port; _confirmation = confirmation; }
	internal void OpenExportFolderPicker(string title, ExportImportScope scope)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		OpenFolderPicker(title, isExport: true, scope, _port.ReturnToDevRootMenu, null);
	}


	internal void OpenExportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		OpenFolderPicker(title, isExport: true, scope, onReturn, null);
	}


	internal void OpenImportFolderPicker(string title, ExportImportScope scope)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		OpenFolderPicker(title, isExport: false, scope, _port.ReturnToDevRootMenu, null);
	}


	internal void OpenImportFolderPicker(string title, ExportImportScope scope, Action onReturn)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		OpenFolderPicker(title, isExport: false, scope, onReturn, null);
	}


	internal void OpenFolderPicker(string title, bool isExport, ExportImportScope scope)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		OpenFolderPicker(title, isExport, scope, _port.ReturnToDevRootMenu, null);
	}


	internal void OpenFolderPickerWithCallback(string title, bool isExport, Action<string> onSelectedFolder, Action onReturn)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (onSelectedFolder == null)
		{
			onReturn?.Invoke();
			return;
		}
		if (onReturn == null)
		{
			onReturn = _port.ReturnToDevRootMenu;
		}
		string playerExportsRootPath = null;
		try
		{
			playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			if (isExport) Directory.CreateDirectory(playerExportsRootPath);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_PlayerExports_path_is_unavailable","PlayerExports path is unavailable: ") + ex.Message));
			if (isExport) { onReturn(); return; }
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", isExport ? DeveloperUiText.T("AF_UI_Developer_Enter_a_folder_name_manually","Enter a folder name manually...") : DeveloperUiText.T("AF_UI_Developer_Enter_a_folder_name_or_path_manually","Enter a folder name or path manually..."), null));
		if (!isExport && playerExportsRootPath != null)
		{
			list.Add(new InquiryElement("__latest__", DeveloperUiText.T("AF_UI_Developer_Use_the_latest_player_export_automatic","Use the latest player export (automatic)"), null));
		}
		AddFolderChoices(list, isExport, playerExportsRootPath);
		string descriptionText = (isExport ? DeveloperUiText.T("AF_UI_Developer_Choose_a_target_folder_existing_contents_may_be_overwrit","Choose a target folder (existing contents may be overwritten).") : DeveloperUiText.T("AF_UI_Developer_Choose_a_mod_folder_or_player_export_a_read_only_absolut","Choose a mod folder or player export; a read-only absolute path is also accepted."));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Choose","Choose"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? DeveloperUiText.T("AF_UI_Developer_Enter_export_folder_name","Enter export folder name") : DeveloperUiText.T("AF_UI_Developer_Enter_import_folder_name_path","Enter import folder name/path"), isExport ? DeveloperUiText.T("AF_UI_Developer_Leave_blank_for_an_automatic_timestamp_an_existing_name_","Leave blank for an automatic timestamp; an existing name overwrites that export.") : DeveloperUiText.T("AF_UI_Developer_Leave_blank_to_choose_the_latest_player_export_a_full_fo","Leave blank to choose the latest player export; a full folder or .json path is also accepted."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, DeveloperUiText.T("AF_UI_Developer_Confirm","Confirm"), DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), delegate(string input)
					{
    if (!_port.IsCurrent(generation)) return;
						onSelectedFolder(input);
						onReturn();
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					onSelectedFolder("");
					onReturn();
				}
				else
				{
					onSelectedFolder(text);
					onReturn();
				}
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}


	private static void AddFolderChoices(List<InquiryElement> elements, bool isExport, string userRoot)
	{
		if (!isExport)
		{
			foreach (var folder in PlayerExportsStore.GetImportFolders())
			{
				string displayName = OnboardingUiText.ImportFolderDisplayName(folder.SourceLabel, folder.Name);
				elements.Add(new InquiryElement(folder.FullPath, displayName + "  [" + (folder.SourceLabel == "Module directory" ? new TaleWorlds.Localization.TextObject("{=AF_UI_ImportSource_ModuleDirectory}Module directory").ToString() : folder.SourceLabel == "Player exports" ? new TaleWorlds.Localization.TextObject("{=AF_UI_ImportSource_PlayerExports}Player exports").ToString() : folder.SourceLabel) + "]  (" + folder.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
			}
			return;
		}
		try
		{
			if (string.IsNullOrWhiteSpace(userRoot) || !Directory.Exists(userRoot)) return;
			foreach (var folder in new DirectoryInfo(userRoot).GetDirectories()
				.Where(d => !d.Name.StartsWith(".", StringComparison.Ordinal)).OrderByDescending(d => d.LastWriteTimeUtc))
				elements.Add(new InquiryElement(folder.Name, folder.Name + "  (" + folder.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
		}
		catch (IOException) { }
		catch (UnauthorizedAccessException) { }
	}

	internal void ShowDuplicateImportInquiry(string title, string text, Action onOverwrite, Action onSkipDuplicates, Action onCancel)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
  Action[] callbacks = _confirmation.BeginConfirmation(onOverwrite, onSkipDuplicates, onCancel);
  onOverwrite = callbacks[0]; onSkipDuplicates = callbacks[1]; onCancel = callbacks[2];

		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			if (onSkipDuplicates == null)
			{
				onSkipDuplicates = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", DeveloperUiText.T("AF_UI_Developer_Overwrite_import","Overwrite import"), null),
				new InquiryElement("__skip__", DeveloperUiText.T("AF_UI_Developer_Import_non_duplicate_data_only","Import non-duplicate data only"), null),
				new InquiryElement("__cancel__", DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Choose","Choose"), DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), delegate(List<InquiryElement> selected)
			{
    if (!_port.IsCurrent(generation)) return;
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__skip__")
				{
					onSkipDuplicates();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
    if (!_port.IsCurrent(generation)) return;
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}


	internal static bool IsDirectoryNonEmpty(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir))
			{
				return false;
			}
			if (!Directory.Exists(dir))
			{
				return false;
			}
			return Directory.EnumerateFileSystemEntries(dir).Any();
		}
		catch
		{
			return false;
		}
	}


	internal void ShowOverwriteExportInquiry(string title, string text, Action onOverwrite, Action onNewFolder, Action onCancel)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
  Action[] callbacks = _confirmation.BeginConfirmation(onOverwrite, onNewFolder, onCancel);
  onOverwrite = callbacks[0]; onNewFolder = callbacks[1]; onCancel = callbacks[2];

		try
		{
			if (onOverwrite == null)
			{
				onOverwrite = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			if (onNewFolder == null)
			{
				onNewFolder = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			if (onCancel == null)
			{
				onCancel = delegate
				{
    if (!_port.IsCurrent(generation)) return;
				};
			}
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("__overwrite__", DeveloperUiText.T("AF_UI_Developer_Overwrite_export","Overwrite export"), null),
				new InquiryElement("__new__", DeveloperUiText.T("AF_UI_Developer_Use_a_new_folder_automatic","Use a new folder (automatic)"), null),
				new InquiryElement("__cancel__", DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Choose","Choose"), DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), delegate(List<InquiryElement> selected)
			{
    if (!_port.IsCurrent(generation)) return;
				string text2 = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : "");
				if (text2 == "__overwrite__")
				{
					onOverwrite();
				}
				else if (text2 == "__new__")
				{
					onNewFolder();
				}
				else
				{
					onCancel();
				}
			}, delegate
			{
    if (!_port.IsCurrent(generation)) return;
				onCancel();
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
		catch
		{
			onCancel?.Invoke();
		}
	}


	internal void OpenFolderPicker(string title, bool isExport, ExportImportScope scope, Action onReturn, string heroId)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (onReturn == null)
		{
			onReturn = _port.ReturnToDevRootMenu;
		}
		string playerExportsRootPath = null;
		try
		{
			playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			if (isExport) Directory.CreateDirectory(playerExportsRootPath);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_PlayerExports_path_is_unavailable","PlayerExports path is unavailable: ") + ex.Message));
			if (isExport) { onReturn(); return; }
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", isExport ? DeveloperUiText.T("AF_UI_Developer_Enter_a_folder_name_manually","Enter a folder name manually...") : DeveloperUiText.T("AF_UI_Developer_Enter_a_folder_name_or_path_manually","Enter a folder name or path manually..."), null));
		if (!isExport && playerExportsRootPath != null)
		{
			list.Add(new InquiryElement("__latest__", DeveloperUiText.T("AF_UI_Developer_Use_the_latest_player_export_automatic","Use the latest player export (automatic)"), null));
		}
		AddFolderChoices(list, isExport, playerExportsRootPath);
		string descriptionText = (isExport ? DeveloperUiText.T("AF_UI_Developer_Choose_the_export_target_folder_existing_contents_may_be","Choose the export target folder (existing contents may be overwritten).") : DeveloperUiText.T("AF_UI_Developer_Choose_a_mod_folder_or_player_export_a_read_only_absolut","Choose a mod folder or player export; a read-only absolute path is also accepted."));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, DeveloperUiText.T("AF_UI_Developer_Choose","Choose"), DeveloperUiText.T("AF_UI_Developer_Back","Back"), delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				onReturn();
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? DeveloperUiText.T("AF_UI_Developer_Enter_export_folder_name","Enter export folder name") : DeveloperUiText.T("AF_UI_Developer_Enter_import_folder_name_path","Enter import folder name/path"), isExport ? DeveloperUiText.T("AF_UI_Developer_Leave_blank_for_an_automatic_timestamp_an_existing_name_","Leave blank for an automatic timestamp; an existing name overwrites that export.") : DeveloperUiText.T("AF_UI_Developer_Leave_blank_for_the_latest_player_export_names_search_th","Leave blank for the latest player export; names search the mod folder first, or enter a full path."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, DeveloperUiText.T("AF_UI_Developer_Confirm","Confirm"), DeveloperUiText.T("AF_UI_Developer_Cancel","Cancel"), delegate(string input)
					{
    if (!_port.IsCurrent(generation)) return;
						if (string.IsNullOrEmpty(heroId))
						{
							ResolveAndRunExportImport(isExport, scope, input);
						}
						else
						{
							ResolveAndRunExportImportForHero(isExport, scope, input, heroId);
						}
						onReturn();
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
						onReturn();
					}));
				}
				else if (!isExport && text == "__latest__")
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport: false, scope, "");
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport: false, scope, "", heroId);
					}
					onReturn();
				}
				else
				{
					if (string.IsNullOrEmpty(heroId))
					{
						ResolveAndRunExportImport(isExport, scope, text);
					}
					else
					{
						ResolveAndRunExportImportForHero(isExport, scope, text, heroId);
					}
					onReturn();
				}
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			onReturn();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}


	internal void ResolveAndRunExportImport(bool isExport, ExportImportScope scope, string folderName)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (isExport)
		{
			Action run = delegate
			{
    if (!_port.IsCurrent(generation)) return;
				if (scope == ExportImportScope.All)
				{
					_port.ExportAllData(folderName);
				}
				else if (scope == ExportImportScope.HeroNpcAll)
				{
					_port.ExportHeroNpcAllData(folderName);
				}
				else if (scope == ExportImportScope.PersonalityBackground)
				{
					_port.ExportPersonaData(folderName);
				}
				else if (scope == ExportImportScope.UnnamedPersona)
				{
					_port.ExportUnnamedPersonaData(folderName);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					_port.ExportDialogueHistoryData(folderName);
				}
				else if (scope == ExportImportScope.Debt)
				{
					_port.ExportDebtData(folderName);
				}
				else if (scope == ExportImportScope.EventData)
				{
					_port.ExportEventData(folderName);
				}
				else if (scope == ExportImportScope.Knowledge)
				{
					_port.ExportKnowledgeData(folderName);
				}
				else if (scope == ExportImportScope.VoiceMapping)
				{
					_port.ExportVoiceMappingData(folderName);
				}
			};
			string value = PlayerExportsStore.SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath;
				try { playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath(); }
				catch (Exception ex)
				{
					InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_PlayerExports_path_is_unavailable","PlayerExports path is unavailable: ") + ex.Message));
					return;
				}
				string path = PlayerExportsStore.ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry(DeveloperUiText.T("AF_UI_Developer_Export_folder_already_exists","Export folder already exists"), DeveloperUiText.F("AF_UI_Developer_Export_folder_exists_body","The target folder already exists and contains:\n{PATH}\nOverwrite this export?", "PATH", text), delegate
					{
    if (!_port.IsCurrent(generation)) return;
						run();
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
						ResolveAndRunExportImport(isExport: true, scope, "");
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.All)
		{
			_port.ImportAllData(folderName);
		}
		else if (scope == ExportImportScope.HeroNpcAll)
		{
			_port.ImportHeroNpcAllData(folderName);
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			_port.ImportPersonaData(folderName);
		}
		else if (scope == ExportImportScope.UnnamedPersona)
		{
			_port.ImportUnnamedPersonaData(folderName);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			_port.ImportDialogueHistoryData(folderName);
		}
		else if (scope == ExportImportScope.Debt)
		{
			_port.ImportDebtData(folderName);
		}
		else if (scope == ExportImportScope.EventData)
		{
			_port.ImportEventData(folderName);
		}
		else if (scope == ExportImportScope.Knowledge)
		{
			_port.ImportKnowledgeData(folderName);
		}
		else if (scope == ExportImportScope.VoiceMapping)
		{
			_port.ImportVoiceMappingData(folderName);
		}
	}


	internal void ResolveAndRunExportImportForHero(bool isExport, ExportImportScope scope, string folderName, string heroId)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrEmpty(id))
		{
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_This_NPC_has_no_ID_and_cannot_be_imported_or_exported","This NPC has no ID and cannot be imported or exported.")));
		}
		else if (isExport)
		{
			Action run = delegate
			{
    if (!_port.IsCurrent(generation)) return;
				if (scope == ExportImportScope.PersonalityBackground)
				{
					_port.ExportSingleNpcPersonaData(folderName, id);
				}
				else if (scope == ExportImportScope.DialogueHistory)
				{
					_port.ExportSingleNpcDialogueHistoryData(folderName, id);
				}
				else if (scope == ExportImportScope.Debt)
				{
					_port.ExportSingleNpcDebtData(folderName, id);
				}
			};
			string value = PlayerExportsStore.SanitizeFolderName(folderName);
			if (!string.IsNullOrEmpty(value))
			{
				string playerExportsRootPath;
				try { playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath(); }
				catch (Exception ex)
				{
					InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Developer_PlayerExports_path_is_unavailable","PlayerExports path is unavailable: ") + ex.Message));
					return;
				}
				string path = PlayerExportsStore.ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry(DeveloperUiText.T("AF_UI_Developer_Export_folder_already_exists","Export folder already exists"), DeveloperUiText.F("AF_UI_Developer_Export_folder_exists_body","The target folder already exists and contains:\n{PATH}\nOverwrite this export?", "PATH", text), delegate
					{
    if (!_port.IsCurrent(generation)) return;
						run();
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
						ResolveAndRunExportImportForHero(isExport: true, scope, "", id);
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
					});
					return;
				}
			}
			run();
		}
		else if (scope == ExportImportScope.PersonalityBackground)
		{
			_port.ImportSingleNpcPersonaData(folderName, id);
		}
		else if (scope == ExportImportScope.DialogueHistory)
		{
			_port.ImportSingleNpcDialogueHistoryData(folderName, id);
		}
		else if (scope == ExportImportScope.Debt)
		{
			_port.ImportSingleNpcDebtData(folderName, id);
		}
	}


	// Terminal "Database import" entry: choose between importing as-is and clearing the covered database data first.
	// The package picker, the duplicate confirmations and the import itself are supplied by the caller.
	internal void OpenDatabaseImportMenu(Action onReturn, Action<bool> chooseAndImport, Action<Action, Action> confirmClear)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Action back = onReturn ?? delegate { };
		try
		{
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("import", DeveloperUiText.T("AF_UI_DatabaseImport_ImportOption", "Import database package"), null),
				new InquiryElement("clear_import", DeveloperUiText.T("AF_UI_DatabaseImport_ClearImportOption", "Clear current database data, then import"), null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(
				DeveloperUiText.T("AF_UI_DatabaseImport_MenuTitle", "Database import"),
				DeveloperUiText.T("AF_UI_DatabaseImport_MenuBody", "Imports persona, knowledge, voices, opening knowledge and kingdom profiles from a package. Files that fail validation are skipped, never rejecting the rest."),
				list, isExitShown: true, 1, 1, DeveloperUiText.T("AF_UI_Developer_Choose", "Choose"), DeveloperUiText.T("AF_UI_Developer_Back", "Back"),
				delegate(List<InquiryElement> selected)
				{
					if (!_port.IsCurrent(generation)) return;
					string choice = selected != null && selected.Count > 0 ? selected[0].Identifier as string : null;
					if (choice == "import")
					{
						chooseAndImport(false);
					}
					else if (choice == "clear_import")
					{
						confirmClear(() => chooseAndImport(true), back);
					}
					else
					{
						back();
					}
				},
				delegate
				{
					if (!_port.IsCurrent(generation)) return;
					back();
				}, "", isSeachAvailable: false);
			MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			Logger.Log("DatabaseImport", "[WARN] Failed to open database import menu: " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_TERMINAL_UI_DatabaseImportFailed", "Unable to open the database import flow.")));
			back();
		}
	}



}
