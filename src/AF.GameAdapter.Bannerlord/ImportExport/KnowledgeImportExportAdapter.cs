using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

// Owns the existing file/schema/fallback flow; KnowledgeLibraryBehavior is the sole rule authority.
internal sealed class KnowledgeImportExportAdapter
{
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 internal KnowledgeImportExportAdapter(Action<string,string,Action,Action,Action> showDuplicate) { _showDuplicate=showDuplicate; }
	internal static bool ValidateKnowledgeKeywordsForImport(string importDir, bool overwriteExisting, out string error)
 {
  try
  {
   KnowledgeLibraryBehavior kb = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
   return KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForImport(kb, importDir, overwriteExisting, out error);
  }
  catch (Exception ex)
  {
   error = "导入失败：关键词校验异常：" + ex.Message;
   return false;
  }
 }

	internal void ExportKnowledgeData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			if (!TryExportKnowledgeToDir(text, out var exportedCount, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
			}
			else
			{
				export.Publish();
				InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath + "（Knowledge " + exportedCount + " 条）"));
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ExportKnowledgeToDir(string exportDir)
	{
		TryExportKnowledgeToDir(exportDir, out var _, out var _);
	}

	internal bool TryExportKnowledgeToDir(string exportDir, out int exportedCount, out string error)
	{
		exportedCount = 0;
		error = "";
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				error = "KnowledgeLibraryBehavior 未初始化。";
				return false;
			}
			if (!knowledgeLibraryBehavior.TryValidateKnowledgeExport(out error))
			{
				return false;
			}
			string value = knowledgeLibraryBehavior.ExportRulesJson();
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = (string.IsNullOrWhiteSpace(value) ? null : JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value));
			if (knowledgeFile?.Rules == null || knowledgeFile.Rules.Count <= 0)
			{
				error = "当前没有可导出的知识条目。";
				return false;
			}
			string text = Path.Combine(exportDir, "knowledge", "rules");
			Directory.CreateDirectory(text);
			PlayerExportsStore.ClearCandidateJsonFiles(text);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
			{
				string text2 = (rule?.Id ?? "").Trim();
				if (string.IsNullOrEmpty(text2) || !hashSet.Add(text2))
				{
					continue;
				}
				rule.Id = text2;
				string text3 = "";
				try
				{
					text3 = rule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
				}
				catch
				{
					text3 = "";
				}
				string text4 = text2;
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				foreach (char oldChar in invalidFileNameChars)
				{
					text4 = text4.Replace(oldChar, '_');
				}
				text4 = (text4 ?? "").Trim();
				if (text4.Length > 80)
				{
					text4 = text4.Substring(0, 80);
				}
				if (string.IsNullOrEmpty(text4))
				{
					text4 = "rule";
				}
				string text5 = text3;
				char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
				foreach (char oldChar2 in invalidFileNameChars2)
				{
					text5 = text5.Replace(oldChar2, '_');
				}
				text5 = (text5 ?? "").Trim();
				if (text5.Length > 35)
				{
					text5 = text5.Substring(0, 35);
				}
				string text6 = text4;
				if (!string.IsNullOrEmpty(text5))
				{
					text6 = text6 + "__" + text5;
				}
				string path = Path.Combine(text, text6 + ".json");
				if (File.Exists(path))
				{
					for (int num = 2; num <= 999; num++)
					{
						string text7 = Path.Combine(text, text6 + "__" + num + ".json");
						if (!File.Exists(text7))
						{
							path = text7;
							break;
						}
					}
				}
				string contents = JsonConvert.SerializeObject(rule, Formatting.Indented);
				File.WriteAllText(path, contents, Encoding.UTF8);
				exportedCount++;
			}
			if (exportedCount <= 0)
			{
				error = "没有成功写出任何知识文件。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	internal void ExportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：RuleId 为空。"));
				return;
			}
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (knowledgeLibraryBehavior == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			if (!knowledgeLibraryBehavior.TryValidateSingleRuleExport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：" + error));
				return;
			}
			string text2 = knowledgeLibraryBehavior.ExportSingleRuleJson(text, pretty: true);
			if (string.IsNullOrWhiteSpace(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该知识条目：" + text));
				return;
			}
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text3 = export.CandidatePath;
			string text4 = Path.Combine(text3, "knowledge", "rules");
			Directory.CreateDirectory(text4);
			KnowledgeLibraryBehavior.LoreRule loreRule = null;
			try
			{
				loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(text2);
			}
			catch
			{
				loreRule = null;
			}
			string text5 = "";
			try
			{
				text5 = loreRule?.Keywords?.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
			}
			catch
			{
				text5 = "";
			}
			string text6 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text6 = text6.Replace(oldChar, '_');
			}
			text6 = (text6 ?? "").Trim();
			if (text6.Length > 80)
			{
				text6 = text6.Substring(0, 80);
			}
			if (string.IsNullOrEmpty(text6))
			{
				text6 = "rule";
			}
			string text7 = text5;
			char[] invalidFileNameChars2 = Path.GetInvalidFileNameChars();
			foreach (char oldChar2 in invalidFileNameChars2)
			{
				text7 = text7.Replace(oldChar2, '_');
			}
			text7 = (text7 ?? "").Trim();
			if (text7.Length > 35)
			{
				text7 = text7.Substring(0, 35);
			}
			string text8 = text6;
			if (!string.IsNullOrEmpty(text7))
			{
				text8 = text8 + "__" + text7;
			}
			string path2 = Path.Combine(text4, text8 + ".json");
			File.WriteAllText(path2, text2, Encoding.UTF8);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportSingleKnowledgeRuleData(string folderName, string ruleId)
	{
		try
		{
			string id = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(id))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RuleId 为空。"));
				return;
			}
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			KnowledgeLibraryBehavior kb = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			if (kb == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：KnowledgeLibraryBehavior 未初始化。"));
				return;
			}
			string dir = Path.Combine(importDir, "knowledge", "rules");
			string dir2 = Path.Combine(importDir, "knowledge", "single_rules");
			string dir3 = Path.Combine(importDir, "knowledge");
			string text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir, id);
			if (string.IsNullOrEmpty(text))
			{
				text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir2, id);
			}
			if (string.IsNullOrEmpty(text))
			{
				text = KnowledgeImportSupport.FindKnowledgeRuleJsonById(dir3, id);
			}
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该知识条目的导出文件：" + id));
				return;
			}
			string value = File.ReadAllText(text, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件为空。"));
				return;
			}
			KnowledgeLibraryBehavior.LoreRule rule = null;
			try
			{
				rule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
			}
			catch
			{
				rule = null;
			}
			if (rule == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text2 = (rule.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text2) && !string.Equals(text2, id, StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：文件中的 Id 不匹配（文件Id=" + text2 + "）。"));
				return;
			}
			if (string.IsNullOrEmpty(text2))
			{
				rule.Id = id;
			}
			bool flag = false;
			try
			{
				flag = !string.IsNullOrWhiteSpace(kb.ExportSingleRuleJson(id));
			}
			catch
			{
				flag = false;
			}
			Action action = delegate
			{
				if (!KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting: true, out var error))
				{
					InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None)))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：写入规则失败。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!kb.ImportSingleRuleJson(JsonConvert.SerializeObject(rule, Formatting.None), overwrite: false))
				{
					InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + id));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				}
			};
			if (flag)
			{
				_showDuplicate("检测到重复 - Knowledge", "检测到相同 RuleId：" + id + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	internal List<string> GetKnowledgeRuleIdsFromImportFolderForDev(string folderName, int maxCount = 200)
	{
		List<string> list = new List<string>();
		try
		{
			if (maxCount <= 0)
			{
				maxCount = 200;
			}
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> source = new List<string>
			{
				Path.Combine(text, "knowledge", "rules"),
				Path.Combine(text, "knowledge", "single_rules"),
				Path.Combine(text, "knowledge")
			};
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (list.Count >= maxCount)
					{
						break;
					}
					try
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (!string.IsNullOrWhiteSpace(value))
						{
							string text2 = (JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value)?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2) && hashSet.Add(text2))
							{
								list.Add(text2);
							}
						}
					}
					catch
					{
					}
				}
				if (list.Count < maxCount)
				{
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		try
		{
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		if (list.Count > maxCount)
		{
			list.RemoveRange(maxCount, list.Count - maxCount);
		}
		return list;
	}

	internal void ImportKnowledgeData(string folderName) => ImportKnowledgeDataScoped(folderName, null);

	internal void ImportKnowledgeDataScoped(string folderName, OnboardingImportStep completion)
	{
        if (completion != null && !completion.CanApply()) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			int num = 0;
			int num2 = 0;
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text))
							{
								hashSet2.Add(text);
							}
						}
					}
					string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet2.Count <= 0 && File.Exists(path))
					{
						string value = File.ReadAllText(path, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text2 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text2))
								{
									hashSet2.Add(text2);
								}
							}
						}
					}
					num2 = hashSet2.Count;
					foreach (string item in hashSet2)
					{
						if (hashSet.Contains(item))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
				num2 = 0;
			}
			Action action = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: true, out var error))
				{
					completion?.Finish(OnboardingImportResult.Failed);
                    InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var detailMessage))
				{
					completion?.Finish(OnboardingImportResult.Failed);
                    InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				completion?.Finish(OnboardingImportResult.Completed);
				}
};
			Action onSkipDuplicates = delegate
			{
				if (!ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting: false, out var error))
				{
					completion?.Finish(OnboardingImportResult.Failed);
                    InformationManager.DisplayMessage(new InformationMessage(error));
				}
				else if (!ImportKnowledgeFromDir(importDir, overwriteExisting: false, out var detailMessage))
				{
					completion?.Finish(OnboardingImportResult.Failed);
                    InformationManager.DisplayMessage(new InformationMessage("导入失败：" + (string.IsNullOrWhiteSpace(detailMessage) ? "找不到 knowledge\\AIConfig.json 或 knowledge\\rules(\\*.json) 或 knowledge\\KnowledgeRules.json" : detailMessage)));
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(detailMessage))
					{
						InformationManager.DisplayMessage(new InformationMessage(detailMessage));
					}
					InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
				completion?.Finish(OnboardingImportResult.Completed);
				}
};
            if (completion != null) { action = completion.Guard(action); onSkipDuplicates = completion.Guard(onSkipDuplicates); }
			if (num > 0)
			{
				completion?.Pending();
				_showDuplicate("检测到重复 - Knowledge", "导入的 Knowledge 规则与当前游戏存在重复 RuleId。\n重复：" + num + " / 总计：" + num2 + "\n请选择处理方式：", action, onSkipDuplicates, delegate
				{
                    if (completion != null && !completion.CanApply()) return;
                    completion?.Finish(OnboardingImportResult.Cancelled);
				});
			}
			else
			{
				action();
			}
		}
		catch (Exception ex)
		{
            completion?.Finish(OnboardingImportResult.Failed);
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}

	internal bool ImportKnowledgeFromDir(string importDir)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting: true, out var _);
	}

	internal bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting)
	{
		return ImportKnowledgeFromDir(importDir, overwriteExisting, out var _);
	}

	internal bool ImportKnowledgeFromDir(string importDir, bool overwriteExisting, out string detailMessage)
	{
		detailMessage = "";
		try
		{
			bool result = false;
			string text = Path.Combine(importDir, "knowledge", "AIConfig.json");
			if (File.Exists(text))
			{
				string moduleRootPath = PlayerExportsStore.GetModuleRootPath();
				string text2 = Path.Combine(moduleRootPath, "ModuleData", "AIConfig.json");
				string text3 = Path.Combine(moduleRootPath, "AIConfig.json");
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text2));
					if (overwriteExisting || !File.Exists(text2))
					{
						File.Copy(text, text2, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					if (overwriteExisting && File.Exists(text3))
					{
						File.Copy(text, text3, overwrite: true);
					}
				}
				catch
				{
				}
				try
				{
					AIConfigHandler.ReloadConfig();
				}
				catch
				{
				}
				result = true;
			}
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile != null)
					{
						if (KnowledgeRuleImportOwner.TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile, overwriteExisting, out var importedCount, out var failedCount, out var firstFailedRuleId, out var firstFailedReason))
						{
							result = true;
							if (failedCount > 0)
							{
								detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount + " 条，失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason);
							}
						}
						else
						{
							detailMessage = (failedCount > 0) ? ("知识规则全部导入失败。失败 " + failedCount + " 条。首个失败 RuleId=" + firstFailedRuleId + (string.IsNullOrWhiteSpace(firstFailedReason) ? "" : "；原因：" + firstFailedReason)) : "知识规则写入失败。";
						}
					}
					else
					{
						string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
						if (File.Exists(path))
						{
							string json2 = File.ReadAllText(path);
							KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(json2);
							if (KnowledgeRuleImportOwner.TryImportKnowledgeFileWithFallback(knowledgeLibraryBehavior, knowledgeFile2, overwriteExisting, out var importedCount2, out var failedCount2, out var firstFailedRuleId2, out var firstFailedReason2))
							{
								result = true;
								if (failedCount2 > 0)
								{
									detailMessage = "Knowledge 全量导入已改为逐条回退导入；成功 " + importedCount2 + " 条，失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2);
								}
							}
							else
							{
								detailMessage = (failedCount2 > 0) ? ("知识规则全部导入失败。失败 " + failedCount2 + " 条。首个失败 RuleId=" + firstFailedRuleId2 + (string.IsNullOrWhiteSpace(firstFailedReason2) ? "" : "；原因：" + firstFailedReason2)) : "知识规则写入失败。";
							}
						}
						else if (!File.Exists(text))
						{
							detailMessage = "找不到 knowledge\\AIConfig.json、knowledge\\rules\\*.json 或 knowledge\\KnowledgeRules.json。";
						}
					}
				}
				else
				{
					detailMessage = "KnowledgeLibraryBehavior 未初始化。";
				}
			}
			catch
			{
				detailMessage = "知识导入过程发生异常。";
			}
			return result;
		}
		catch (Exception ex)
		{
			detailMessage = ex.Message;
			return false;
		}
	}

 internal void PrepareKnowledgeCounts(string importDir, out int num9, out int num10)
 {
 num9=0; num10=0;
			try
			{
				KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
				if (knowledgeLibraryBehavior != null)
				{
					HashSet<string> hashSet2 = new HashSet<string>(knowledgeLibraryBehavior.GetRuleIdsForDev(100000) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
					HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = KnowledgeImportSupport.TryLoadKnowledgeRulesFromRuleFiles(importDir);
					if (knowledgeFile?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
						{
							string text9 = (rule?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text9))
							{
								hashSet3.Add(text9);
							}
						}
					}
					string path5 = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
					if (hashSet3.Count <= 0 && File.Exists(path5))
					{
						string value = File.ReadAllText(path5, Encoding.UTF8);
						KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
						if (knowledgeFile2?.Rules != null)
						{
							foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
							{
								string text10 = (rule2?.Id ?? "").Trim();
								if (!string.IsNullOrEmpty(text10))
								{
									hashSet3.Add(text10);
								}
							}
						}
					}
					num10 = hashSet3.Count;
					foreach (string item2 in hashSet3)
					{
						if (hashSet2.Contains(item2))
						{
							num9++;
						}
					}
				}
			}
			catch
			{
				num9 = 0;
				num10 = 0;
			}
 }

internal int CountKnowledgeRulesForDev()
	{
		try
		{
			KnowledgeLibraryBehavior knowledgeLibraryBehavior = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			return knowledgeLibraryBehavior?.GetRuleIdsForDev(100000)?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}
}
