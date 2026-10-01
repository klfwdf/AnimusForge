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
 internal delegate bool BuildReloadPlanCapability(string folder, out DatabaseReloadPlan plan, out string error);
 internal delegate bool ApplyReloadPlanCapability(DatabaseReloadPlan plan, out string detail);
 internal BuildReloadPlanCapability TryBuildDatabaseReloadPlan;
 internal ApplyReloadPlanCapability ApplyDatabaseReloadPlan;
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
			InformationManager.DisplayMessage(new InformationMessage("PlayerExports 待迁移或路径不可用：" + ex.Message));
			if (isExport) { onReturn(); return; }
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", isExport ? "手动输入文件夹名…" : "手动输入文件夹名/路径…", null));
		if (!isExport && playerExportsRootPath != null)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			List<DirectoryInfo> list2 = (from d in (playerExportsRootPath == null ? Array.Empty<DirectoryInfo>() : new DirectoryInfo(playerExportsRootPath).GetDirectories())
				where !d.Name.StartsWith(".", StringComparison.Ordinal)
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择目标文件夹（可覆盖已有）。" : "选择来源文件夹；迁移未完成时仍可手动输入只读绝对路径。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
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
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名/路径", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出；也可输入完整路径（文件夹或 .json 文件）。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
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
				new InquiryElement("__overwrite__", "覆盖导入", null),
				new InquiryElement("__skip__", "只导入非重复信息", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
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
				new InquiryElement("__overwrite__", "覆盖导出", null),
				new InquiryElement("__new__", "改用新文件夹（自动）", null),
				new InquiryElement("__cancel__", "取消", null)
			};
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, text, inquiryElements, isExitShown: true, 0, 1, "选择", "取消", delegate(List<InquiryElement> selected)
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
			InformationManager.DisplayMessage(new InformationMessage("PlayerExports 待迁移或路径不可用：" + ex.Message));
			if (isExport) { onReturn(); return; }
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("__input__", "手动输入文件夹名…", null));
		if (!isExport && playerExportsRootPath != null)
		{
			list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			List<DirectoryInfo> list2 = (from d in (playerExportsRootPath == null ? Array.Empty<DirectoryInfo>() : new DirectoryInfo(playerExportsRootPath).GetDirectories())
				where !d.Name.StartsWith(".", StringComparison.Ordinal)
				orderby d.LastWriteTimeUtc descending
				select d).ToList();
			foreach (DirectoryInfo item in list2)
			{
				string title2 = item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")";
				list.Add(new InquiryElement(item.Name, title2, null));
			}
		}
		catch
		{
		}
		string descriptionText = (isExport ? "选择要导出的目标文件夹（可覆盖已有）。" : "选择来源文件夹；迁移未完成时仍可手动输入只读绝对路径。");
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
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
					InformationManager.ShowTextInquiry(new TextInquiryData(isExport ? "输入导出文件夹名" : "输入导入文件夹名", isExport ? "留空=自动时间戳；输入已存在名称=覆盖导出。" : "留空=自动选择最新导出。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "取消", delegate(string input)
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
					InformationManager.DisplayMessage(new InformationMessage("PlayerExports 待迁移或路径不可用：" + ex.Message));
					return;
				}
				string path = PlayerExportsStore.ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
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
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
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
					InformationManager.DisplayMessage(new InformationMessage("PlayerExports 待迁移或路径不可用：" + ex.Message));
					return;
				}
				string path = PlayerExportsStore.ResolveExportFolderName(folderName);
				string text = Path.Combine(playerExportsRootPath, path);
				if (IsDirectoryNonEmpty(text))
				{
					ShowOverwriteExportInquiry("导出文件夹已存在", "目标文件夹已存在且包含内容：\n" + text + "\n是否覆盖导出？", delegate
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


	internal void OpenDatabaseReloadFolderPicker(Action onReturn)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Action action = onReturn ?? delegate
		{
    if (!_port.IsCurrent(generation)) return;
		};
		try
		{
			List<InquiryElement> list = new List<InquiryElement>
			{
				new InquiryElement("__input__", "手动输入资料包文件夹/路径…", null)
			};
			string playerExportsRootPath = null;
			try { playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath(); }
			catch (Exception)
			{
				InformationManager.DisplayMessage(new InformationMessage("PlayerExports 待迁移或路径不可用；仍可用只读绝对路径导入。"));
			}
			if (playerExportsRootPath != null)
				list.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
			if (!string.IsNullOrWhiteSpace(playerExportsRootPath) && Directory.Exists(playerExportsRootPath))
			{
				foreach (DirectoryInfo item in new DirectoryInfo(playerExportsRootPath).GetDirectories().Where((DirectoryInfo x) => !x.Name.StartsWith(".", StringComparison.Ordinal)).OrderByDescending((DirectoryInfo x) => x.LastWriteTimeUtc))
				{
					list.Add(new InquiryElement(item.Name, item.Name + "  (" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
				}
			}
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("重载数据库 - 选择资料包", "选择来源资料包；系统会在确认前校验知识、王国性格/战略和声音数据。", list, isExitShown: true, 1, 1, "选择", "返回", delegate(List<InquiryElement> selected)
			{
    if (!_port.IsCurrent(generation)) return;
				if (selected == null || selected.Count == 0)
				{
					action();
					return;
				}
				string text = selected[0].Identifier as string;
				if (string.Equals(text, "__input__", StringComparison.Ordinal))
				{
					InformationManager.ShowTextInquiry(new TextInquiryData("输入资料包文件夹/路径", playerExportsRootPath == null ? "请输入只读的完整资料包文件夹路径。" : "留空会使用最新导出；也可输入完整资料包文件夹路径。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "继续", "返回", delegate(string input)
					{
    if (!_port.IsCurrent(generation)) return;
						BeginDatabaseReloadPreflight(input, action);
					}, delegate
					{
    if (!_port.IsCurrent(generation)) return;
						action();
					}));
					return;
				}
				BeginDatabaseReloadPreflight(string.Equals(text, "__latest__", StringComparison.Ordinal) ? "" : text, action);
			}, delegate
			{
    if (!_port.IsCurrent(generation)) return;
				action();
			}, "", isSeachAvailable: true);
			MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
		}
		catch (Exception)
		{
			Logger.Log("DatabaseReload", "[WARN] Failed to open source picker.");
			InformationManager.DisplayMessage(new InformationMessage("无法打开资料包选择器；请检查 PlayerExports 路径。"));
			action();
		}
	}


	internal void BeginDatabaseReloadPreflight(string folderName, Action onReturn)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		Action action = onReturn ?? delegate
		{
    if (!_port.IsCurrent(generation)) return;
		};
		if (!_port.TryBuildDatabaseReloadPlan(folderName, out var plan, out var error))
		{
			InformationManager.ShowInquiry(new InquiryData("数据库资料包校验失败", "本次重载尚未修改存档。\n\n" + error, isAffirmativeOptionShown: true, isNegativeOptionShown: false, "重新选择", "", delegate
			{
    if (!_port.IsCurrent(generation)) return;
				OpenDatabaseReloadFolderPicker(action);
			}, null), pauseGameActiveState: true, prioritize: false);
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("来源资料包：");
		stringBuilder.AppendLine(plan.ImportDirectory);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("将删除并重载：");
		stringBuilder.AppendLine("• 非主角知识 " + plan.KnowledgeRules.Count + " 条");
		if (plan.KnowledgeRuleIdDisambiguationCount > 0)
		{
			stringBuilder.AppendLine("• 资料包内 " + plan.KnowledgeRuleIdDisambiguationCount + " 个同 ID 知识条目将按文件名生成稳定 ID（不修改资料包文件）");
		}
		if (plan.KnowledgeKeywordDeduplicationCount > 0)
		{
			stringBuilder.AppendLine("• 其中 " + plan.KnowledgeKeywordDeduplicationCount + " 个重复触发词只保留在原条目，避免拆分后的知识争抢同一关键词");
		}
		stringBuilder.AppendLine("• 世界开局知识 1 份，以及王国开局知识 " + plan.OpeningKnowledge.KingdomSummaries.Count + " 条");
		stringBuilder.AppendLine("• 上述开局知识的第 0 日派生记录（游戏日达到 7 天后，可能按现有 API 设置请求 LLM 生成短摘要）");
		stringBuilder.AppendLine("• 王国性格与长期战略 " + (plan.KingdomProfilePlan?.SourceProfilesByTargetId?.Count ?? 0) + " 个王国");
		stringBuilder.AppendLine("• 声音映射 1 份，以及 NPC 声音 ID " + plan.NpcVoiceIds.Count + " 条");
		if (plan.CurrentKingdomProfilesResetToDefaultCount > 0)
		{
			stringBuilder.AppendLine("• 未出现在资料包的当前王国 " + plan.CurrentKingdomProfilesResetToDefaultCount + " 个，将恢复为其当前默认资料（不会因此自动请求王国 LLM）");
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("会保留且不会从资料包导入：");
		stringBuilder.AppendLine("• 主角外貌、背景、主角专用知识和直接声音 ID");
		stringBuilder.AppendLine("• NPC 个性、背景、记忆和外貌");
		stringBuilder.AppendLine("• 动态事件记录、API 配置、LLM 提示词及其他设置");
		stringBuilder.AppendLine();
		stringBuilder.Append("此操作会覆盖当前存档中的上述数据库数据，是否继续？");
		Action apply = delegate
		{
    if (!_port.IsCurrent(generation)) return;
			if (_port.ApplyDatabaseReloadPlan(plan, out var detail))
			{
				InformationManager.DisplayMessage(new InformationMessage("数据库重载完成：" + detail));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("数据库重载失败：" + detail));
			}
			action();
		};
  Action[] choices = _confirmation.BeginConfirmation(apply, null, action);
  InformationManager.ShowInquiry(new InquiryData("确认重载数据库", stringBuilder.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认重载", "取消", choices[0], choices[2]), pauseGameActiveState: true, prioritize: false);
	}


}
