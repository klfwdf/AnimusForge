using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class KingdomStrategicProfileBehavior
{
	internal void OpenDevMenu()
	{
		EnsureCurrentKingdomProfiles(runtimeFounded: false);
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("edit", "编辑当前国家…", null),
			new InquiryElement("export_all", "全量导出（国家战略与性格）", null),
			new InquiryElement("import_all", "全量导入（国家战略与性格）", null),
			new InquiryElement("reset_all", "恢复全部国家的默认卡", null),
			new InquiryElement("back", "返回", null)
		};
		string description = "独立国家角色卡数据；当前不会注入 AI 外交请求。\n"
			+ "国家卡：" + GetProfileCountForDev().ToString(CultureInfo.InvariantCulture)
			+ " 条；玩家覆盖：" + GetPlayerOverrideCountForDev().ToString(CultureInfo.InvariantCulture) + " 条。\n"
			+ "新建国家的 LLM 建国卡会作为固定默认值保存，玩家编辑优先。";
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"国家战略与性格",
			description,
			options,
			isExitShown: true,
			0,
			1,
			"进入",
			"返回",
			OnDevMenuSelected,
			delegate
			{
				ReturnToDevRootMenu();
			}));
	}

	private void OnDevMenuSelected(List<InquiryElement> selected)
	{
		string action = selected?.FirstOrDefault()?.Identifier as string;
		switch (action)
		{
		case "edit":
			OpenKingdomSelection();
			break;
		case "export_all":
			OpenFolderPicker("全量导出（国家战略与性格）- 选择文件夹", isExport: true, RunFullExport, OpenDevMenu);
			break;
		case "import_all":
			OpenFolderPicker("全量导入（国家战略与性格）- 选择文件夹", isExport: false, RunFullImport, OpenDevMenu);
			break;
		case "reset_all":
			ConfirmResetAllProfiles();
			break;
		default:
			ReturnToDevRootMenu();
			break;
		}
	}

	private void OpenKingdomSelection()
	{
		List<Kingdom> kingdoms = GetEditableKingdoms();
		if (kingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的国家。"));
			OpenDevMenu();
			return;
		}
		List<InquiryElement> elements = new List<InquiryElement>
		{
			new InquiryElement("back", "返回", null)
		};
		foreach (Kingdom kingdom in kingdoms)
		{
			EnsureProfile(kingdom, runtimeFounded: false);
			elements.Add(new InquiryElement(kingdom.StringId ?? "", GetKingdomName(kingdom), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"编辑国家战略与性格",
			"请选择国家。列表仅显示国家名称；已覆灭国家仍会保留档案。",
			elements,
			isExitShown: true,
			0,
			1,
			"编辑",
			"返回",
			delegate(List<InquiryElement> selected)
			{
				string id = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(id) || id == "back")
				{
					OpenDevMenu();
					return;
				}
				Kingdom kingdom = FindKingdomById(id);
				if (kingdom == null)
				{
					InformationManager.DisplayMessage(new InformationMessage("找不到对应国家。"));
					OpenKingdomSelection();
					return;
				}
				OpenKingdomDetail(kingdom);
			},
			delegate
			{
				OpenDevMenu();
			}));
	}

	private void OpenKingdomDetail(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenKingdomSelection();
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (profile == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("国家卡初始化失败。"));
			OpenKingdomSelection();
			return;
		}
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("personality", "编辑国家性格", null),
			new InquiryElement("strategy", "编辑长期战略目标", null),
			new InquiryElement("reset", "恢复该国默认卡", null),
			new InquiryElement("export", "导出该国", null),
			new InquiryElement("import", "导入该国", null)
		};
		if (CanRegenerateFoundingProfile(kingdom, profile))
		{
			string generationLabel = string.Equals(profile.GenerationState, "running", StringComparison.OrdinalIgnoreCase)
				? "LLM 建国卡正在生成…"
				: "重新生成该国的 LLM 建国默认卡";
			options.Add(new InquiryElement("regenerate", generationLabel, null));
		}
		options.Add(new InquiryElement("back", "返回国家列表", null));
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"国家卡 - " + GetKingdomName(kingdom),
			BuildProfileDetailText(kingdom, profile),
			options,
			isExitShown: true,
			0,
			1,
			"进入",
			"返回",
			delegate(List<InquiryElement> selected)
			{
				OnKingdomDetailSelected(kingdom, selected?.FirstOrDefault()?.Identifier as string);
			},
			delegate
			{
				OpenKingdomSelection();
			}));
	}

	private void OnKingdomDetailSelected(Kingdom kingdom, string action)
	{
		switch (action)
		{
		case "personality":
			OpenPersonalityEditor(kingdom);
			break;
		case "strategy":
			OpenStrategyEditor(kingdom);
			break;
		case "reset":
			ConfirmResetProfile(kingdom);
			break;
		case "export":
			OpenFolderPicker("导出单个国家卡 - 选择文件夹", isExport: true, x => RunSingleExport(x, kingdom), () => OpenKingdomDetail(kingdom));
			break;
		case "import":
			OpenFolderPicker("导入单个国家卡 - 选择文件夹", isExport: false, x => RunSingleImport(x, kingdom), () => OpenKingdomDetail(kingdom));
			break;
		case "regenerate":
			RegenerateFoundingProfile(kingdom);
			break;
		default:
			OpenKingdomSelection();
			break;
		}
	}

	private void OpenPersonalityEditor(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑国家性格 - " + GetKingdomName(kingdom),
			"描述国家的决策风格、价值观、对风险和羞辱的反应以及不可退让的底线。保存后成为玩家覆盖。",
			"请输入国家性格；允许留空。",
			profile?.NationalPersonality ?? "",
			delegate(string input)
			{
				if (profile != null)
				{
					profile.NationalPersonality = CleanProfileText(input);
					profile.HasPersonalityOverride = true;
					profile.IsPlayerOverride = true;
					profile.UpdatedDay = GetCurrentCampaignDay();
				}
				InformationManager.DisplayMessage(new InformationMessage("国家性格已更新。"));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom));
	}

	private void OpenStrategyEditor(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑长期战略目标 - " + GetKingdomName(kingdom),
			"描述国家长期、固定且可持续追求的方向。保存后成为玩家覆盖。",
			"请输入长期战略目标；允许留空。",
			profile?.LongTermStrategy ?? "",
			delegate(string input)
			{
				if (profile != null)
				{
					profile.LongTermStrategy = CleanProfileText(input);
					profile.HasStrategyOverride = true;
					profile.IsPlayerOverride = true;
					profile.UpdatedDay = GetCurrentCampaignDay();
				}
				InformationManager.DisplayMessage(new InformationMessage("长期战略目标已更新。"));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom));
	}

	private void ConfirmResetProfile(Kingdom kingdom)
	{
		InformationManager.ShowInquiry(new InquiryData(
			"恢复国家默认卡",
			"将清除“" + GetKingdomName(kingdom) + "”的玩家覆盖。国家会回到当前默认基线（可能来自内置预设、首次资料包或已固化的 LLM 建国卡）。是否继续？",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"恢复默认",
			"取消",
			delegate
			{
				ResetProfileToDefault(kingdom);
				InformationManager.DisplayMessage(new InformationMessage("已恢复“" + GetKingdomName(kingdom) + "”的默认国家卡。"));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom)));
	}

	private void ConfirmResetAllProfiles()
	{
		InformationManager.ShowInquiry(new InquiryData(
			"恢复全部国家默认卡",
			"这会清除全部国家的玩家覆盖，但不会删除内置/首次资料包默认或新建国家已经由 LLM 固化的建国默认。建议先导出备份。是否继续？",
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			"恢复全部默认",
			"取消",
			delegate
			{
				ResetAllProfilesToDefaults();
				InformationManager.DisplayMessage(new InformationMessage("已恢复全部国家的默认卡。"));
				OpenDevMenu();
			},
			OpenDevMenu));
	}

	private void ResetProfileToDefault(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (profile == null)
		{
			return;
		}
		profile.NationalPersonality = profile.DefaultNationalPersonality ?? "";
		profile.LongTermStrategy = profile.DefaultLongTermStrategy ?? "";
		profile.HasPersonalityOverride = false;
		profile.HasStrategyOverride = false;
		profile.IsPlayerOverride = false;
		profile.UpdatedDay = GetCurrentCampaignDay();
	}

	private void RegenerateFoundingProfile(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (kingdom?.IsEliminated == true)
		{
			InformationManager.DisplayMessage(new InformationMessage("已覆灭国家保留现有国家卡，但不会请求 LLM 重新生成。"));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (profile == null || !CanRegenerateFoundingProfile(kingdom, profile))
		{
			InformationManager.DisplayMessage(new InformationMessage("该国家不是可重新生成的动态建国卡。"));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (string.Equals(profile.GenerationState, "running", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("该国家的 LLM 建国卡正在生成。"));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (QueueFoundingGeneration(kingdom, force: true, showConfigError: true))
		{
			InformationManager.DisplayMessage(new InformationMessage("已安排重新生成；完成后会固化为该国新的默认卡。玩家覆盖不会被覆盖。"));
		}
		OpenKingdomDetail(kingdom);
	}

	private static bool CanRegenerateFoundingProfile(Kingdom kingdom, KingdomStrategicProfileRecord profile)
	{
		return kingdom?.IsEliminated != true && profile != null && (profile.RequiresFoundingGeneration
			|| string.Equals(profile.DefaultSource, "founding_fallback", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(profile.DefaultSource, "llm_founding", StringComparison.OrdinalIgnoreCase));
	}

	private void RunFullExport(string folderInput)
	{
		if (!TryResolveExportRoot(folderInput, out string exportRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
			OpenDevMenu();
			return;
		}
		string outputFile = Path.Combine(exportRoot, ExportDirectoryName, FullExportFileName);
		RunExportWithCollisionCheck(
			outputFile,
			"该文件夹中已有国家卡全量导出。",
			delegate(string resolvedRoot)
			{
				bool ok = ExportAllToDirectory(resolvedRoot, out string detail);
				InformationManager.DisplayMessage(new InformationMessage((ok ? "导出完成：" : "导出失败：") + detail));
				OpenDevMenu();
			},
			exportRoot,
			OpenDevMenu);
	}

	private void RunSingleExport(string folderInput, Kingdom kingdom)
	{
		if (!TryResolveExportRoot(folderInput, out string exportRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
			OpenKingdomDetail(kingdom);
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		string outputFile = Path.Combine(exportRoot, ExportDirectoryName, "kingdoms", BuildSingleExportFileName(profile));
		RunExportWithCollisionCheck(
			outputFile,
			"该文件夹中已有这个国家的单国导出。",
			delegate(string resolvedRoot)
			{
				bool ok = ExportSingleToDirectory(resolvedRoot, kingdom, out string detail);
				InformationManager.DisplayMessage(new InformationMessage((ok ? "导出完成：" : "导出失败：") + detail));
				OpenKingdomDetail(kingdom);
			},
			exportRoot,
			() => OpenKingdomDetail(kingdom));
	}

	private void RunExportWithCollisionCheck(string outputFile, string message, Action<string> run, string exportRoot, Action onCancel)
	{
		if (!File.Exists(outputFile))
		{
			run(exportRoot);
			return;
		}
		List<InquiryElement> choices = new List<InquiryElement>
		{
			new InquiryElement("overwrite", "覆盖导出", null),
			new InquiryElement("new", "改用新文件夹（自动）", null),
			new InquiryElement("cancel", "取消", null)
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"检测到已有导出",
			message,
			choices,
			isExitShown: true,
			0,
			1,
			"选择",
			"取消",
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (choice == "overwrite")
				{
					run(exportRoot);
				}
				else if (choice == "new")
				{
					if (TryResolveExportRoot(DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture), out string newRoot, out string error))
					{
						run(newRoot);
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
						onCancel?.Invoke();
					}
				}
				else
				{
					onCancel?.Invoke();
				}
			},
			delegate
			{
				onCancel?.Invoke();
			}));
	}

	private void RunFullImport(string folderInput)
	{
		if (!TryResolveImportRoot(folderInput, out string importRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + error));
			OpenDevMenu();
			return;
		}
		if (!InspectImportDirectory(importRoot, out int total, out int duplicates, out int skipped, out string inspectError))
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + inspectError));
			OpenDevMenu();
			return;
		}
		if (total <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("没有可安全匹配的国家卡；无效或无法匹配 " + skipped.ToString(CultureInfo.InvariantCulture) + " 条。"));
			OpenDevMenu();
			return;
		}
		Action<bool> apply = overwrite =>
		{
			bool ok = ImportAllFromDirectory(importRoot, overwrite, out string detail);
			InformationManager.DisplayMessage(new InformationMessage((ok ? "导入完成：" : "导入失败：") + detail));
			OpenDevMenu();
		};
		if (duplicates <= 0)
		{
			apply(true);
			return;
		}
		ShowDuplicateImportInquiry(
			"检测到重复 - 国家战略与性格",
			"可匹配国家卡：" + total.ToString(CultureInfo.InvariantCulture)
				+ "；已有玩家覆盖、资料包默认或 LLM 建国默认：" + duplicates.ToString(CultureInfo.InvariantCulture)
				+ "；无效或无法匹配：" + skipped.ToString(CultureInfo.InvariantCulture)
				+ "。选择“跳过”会整张国家卡跳过。",
			() => apply(true),
			() => apply(false),
			OpenDevMenu);
	}

	private void RunSingleImport(string folderInput, Kingdom kingdom)
	{
		if (!TryResolveImportRoot(folderInput, out string importRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + error));
			OpenKingdomDetail(kingdom);
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		Action<bool> apply = overwrite =>
		{
			bool ok = ImportSingleFromDirectory(importRoot, kingdom, overwrite, out string detail);
			InformationManager.DisplayMessage(new InformationMessage((ok ? "导入完成：" : "导入失败：") + detail));
			OpenKingdomDetail(kingdom);
		};
		if (!HasImportCollision(profile))
		{
			apply(true);
			return;
		}
		ShowDuplicateImportInquiry(
			"检测到重复 - " + GetKingdomName(kingdom),
			"该国家已有玩家覆盖、资料包默认或 LLM 建国默认。选择“跳过”会整张国家卡跳过。",
			() => apply(true),
			() => apply(false),
			() => OpenKingdomDetail(kingdom));
	}

	private static void ShowDuplicateImportInquiry(string title, string description, Action overwrite, Action skip, Action cancel)
	{
		List<InquiryElement> choices = new List<InquiryElement>
		{
			new InquiryElement("overwrite", "覆盖导入", null),
			new InquiryElement("skip", "只导入非重复信息", null),
			new InquiryElement("cancel", "取消", null)
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			title,
			description,
			choices,
			isExitShown: true,
			0,
			1,
			"选择",
			"取消",
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (choice == "overwrite")
				{
					overwrite?.Invoke();
				}
				else if (choice == "skip")
				{
					skip?.Invoke();
				}
				else
				{
					cancel?.Invoke();
				}
			},
			delegate
			{
				cancel?.Invoke();
			}));
	}

	private void OpenFolderPicker(string title, bool isExport, Action<string> onSelected, Action onReturn)
	{
		string root = GetPlayerExportsRootPath();
		try
		{
			Directory.CreateDirectory(root);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("无法打开 PlayerExports：" + ex.Message));
			onReturn?.Invoke();
			return;
		}
		List<InquiryElement> elements = new List<InquiryElement>
		{
			new InquiryElement("__input__", isExport ? "手动输入文件夹名…" : "手动输入文件夹名/路径…", null)
		};
		if (!isExport)
		{
			elements.Add(new InquiryElement("__latest__", "使用最新导出（自动）", null));
		}
		try
		{
			foreach (DirectoryInfo directory in new DirectoryInfo(root).GetDirectories().OrderByDescending(x => x.LastWriteTimeUtc))
			{
				elements.Add(new InquiryElement(directory.Name, directory.Name + "  (" + directory.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
			}
		}
		catch
		{
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			title,
			isExport ? "选择目标文件夹。" : "选择来源文件夹；也可以手动输入只读的绝对目录或 JSON 文件。",
			elements,
			isExitShown: true,
			0,
			1,
			"选择",
			"返回",
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrEmpty(choice))
				{
					onReturn?.Invoke();
				}
				else if (choice == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(
						isExport ? "输入导出文件夹名" : "输入导入文件夹名/路径",
						isExport ? "留空=自动时间戳；导出始终限制在 PlayerExports 内。" : "留空=最新导出；允许输入只读的绝对目录或 .json 文件。",
						isAffirmativeOptionShown: true,
						isNegativeOptionShown: true,
						"确定",
						"取消",
						input => onSelected?.Invoke(input),
						onReturn));
				}
				else if (choice == "__latest__")
				{
					onSelected?.Invoke("");
				}
				else
				{
					onSelected?.Invoke(choice);
				}
			},
			delegate
			{
				onReturn?.Invoke();
			}));
	}

	private static bool TryResolveExportRoot(string folderInput, out string exportRoot, out string errorMessage)
	{
		exportRoot = "";
		errorMessage = "";
		try
		{
			string root = Path.GetFullPath(GetPlayerExportsRootPath());
			string name = SanitizeFolderName(folderInput);
			if (string.IsNullOrWhiteSpace(name))
			{
				name = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			}
			if (name == "." || name == ".." || Path.IsPathRooted(name))
			{
				errorMessage = "导出文件夹名无效。";
				return false;
			}
			string candidate = Path.GetFullPath(Path.Combine(root, name));
			if (!IsPathInsideRoot(candidate, root))
			{
				errorMessage = "导出目标必须位于 PlayerExports 内。";
				return false;
			}
			Directory.CreateDirectory(candidate);
			exportRoot = candidate;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool TryResolveImportRoot(string folderInput, out string importRoot, out string errorMessage)
	{
		importRoot = "";
		errorMessage = "";
		try
		{
			string input = (folderInput ?? "").Trim();
			if (!string.IsNullOrEmpty(input) && Path.IsPathRooted(input))
			{
				string absolute = Path.GetFullPath(input);
				if (Directory.Exists(absolute) || (File.Exists(absolute) && string.Equals(Path.GetExtension(absolute), ".json", StringComparison.OrdinalIgnoreCase)))
				{
					importRoot = absolute;
					return true;
				}
				errorMessage = "绝对路径不存在，或不是 JSON 文件。";
				return false;
			}
			string root = Path.GetFullPath(GetPlayerExportsRootPath());
			if (string.IsNullOrEmpty(input))
			{
				DirectoryInfo latest = Directory.Exists(root)
					? new DirectoryInfo(root).GetDirectories().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault()
					: null;
				if (latest == null)
				{
					errorMessage = "PlayerExports 下没有可导入文件夹。";
					return false;
				}
				importRoot = latest.FullName;
				return true;
			}
			string name = SanitizeFolderName(input);
			if (name == "." || name == "..")
			{
				errorMessage = "导入文件夹名无效。";
				return false;
			}
			string candidate = Path.GetFullPath(Path.Combine(root, name));
			if (!IsPathInsideRoot(candidate, root) || !Directory.Exists(candidate))
			{
				errorMessage = "找不到 PlayerExports 下的导入文件夹。";
				return false;
			}
			importRoot = candidate;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static string GetPlayerExportsRootPath()
	{
		string moduleRoot = "";
		try
		{
			string location = typeof(SubModule).Assembly.Location;
			DirectoryInfo directory = string.IsNullOrWhiteSpace(location) ? null : new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(location)));
			while (directory != null && directory.Exists)
			{
				if (File.Exists(Path.Combine(directory.FullName, "SubModule.xml")))
				{
					moduleRoot = directory.FullName;
					break;
				}
				directory = directory.Parent;
			}
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(moduleRoot))
		{
			moduleRoot = Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		return Path.Combine(moduleRoot, "PlayerExports");
	}

	private static string SanitizeFolderName(string input)
	{
		string value = (input ?? "").Trim();
		foreach (char invalid in Path.GetInvalidFileNameChars())
		{
			value = value.Replace(invalid, '_');
		}
		return value.Trim().TrimEnd('.');
	}

	private static bool IsPathInsideRoot(string candidate, string root)
	{
		string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		string fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
	}

	private static List<Kingdom> GetEditableKingdoms()
	{
		return GetCurrentKingdoms()
			.OrderBy(x => x.IsEliminated ? 1 : 0)
			.ThenBy(GetKingdomName, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static Kingdom FindKingdomById(string kingdomId)
	{
		string id = NormalizeId(kingdomId);
		return GetCurrentKingdoms().FirstOrDefault(x => string.Equals(NormalizeId(x.StringId), id, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildProfileDetailText(Kingdom kingdom, KingdomStrategicProfileRecord profile)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine("ID：" + (profile?.KingdomId ?? kingdom?.StringId ?? ""));
		text.AppendLine("文化：" + (kingdom?.Culture?.Name?.ToString() ?? profile?.CultureId ?? "未知"));
		text.AppendLine("状态：" + (kingdom?.IsEliminated == true ? "已覆灭（档案保留）" : "存续"));
		text.AppendLine("当前来源：" + (profile?.IsPlayerOverride == true ? "玩家覆盖" : GetSourceLabel(profile?.DefaultSource)));
		text.AppendLine("默认来源：" + GetSourceLabel(profile?.DefaultSource));
		if (profile?.RequiresFoundingGeneration == true || string.Equals(profile?.GenerationState, "running", StringComparison.OrdinalIgnoreCase))
		{
			text.AppendLine("LLM 建国卡：" + GetGenerationStateLabel(profile));
			if (!string.IsNullOrWhiteSpace(profile?.LastGenerationError))
			{
				text.AppendLine("最近错误：" + Preview(profile.LastGenerationError, 180));
			}
		}
		return text.ToString().TrimEnd();
	}

	private static string GetSourceLabel(string source)
	{
		switch ((source ?? "").Trim().ToLowerInvariant())
		{
		case "authored_default":
			return "内置国家默认";
		case "generic_default":
			return "文化/通用默认";
		case "founding_fallback":
			return "新国家临时默认（等待 LLM）";
		case "llm_founding":
			return "LLM 建国默认（已固化）";
		case "bundle_default":
			return "首次资料包默认";
		default:
			return string.IsNullOrWhiteSpace(source) ? "未知" : source;
		}
	}

	private static string GetGenerationStateLabel(KingdomStrategicProfileRecord profile)
	{
		switch ((profile?.GenerationState ?? "").Trim().ToLowerInvariant())
		{
		case "running":
			return "生成中";
		case "failed":
			return "生成失败；将按冷却重试或可手动重试（" + (profile?.GenerationAttemptCount ?? 0).ToString(CultureInfo.InvariantCulture) + "/" + FoundingGenerationMaxAttempts.ToString(CultureInfo.InvariantCulture) + "）";
		case "complete":
			return "已完成并固化";
		case "paused_eliminated":
			return "国家已覆灭，自动生成已停止";
		default:
			return "等待生成（" + (profile?.GenerationAttemptCount ?? 0).ToString(CultureInfo.InvariantCulture) + "/" + FoundingGenerationMaxAttempts.ToString(CultureInfo.InvariantCulture) + "）";
		}
	}

	private static string Preview(string value, int maxChars)
	{
		string text = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "（空）";
		}
		return text.Length <= maxChars ? text : text.Substring(0, Math.Max(1, maxChars)) + "…";
	}

	private static void ReturnToDevRootMenu()
	{
		try
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
		catch
		{
		}
	}
}
