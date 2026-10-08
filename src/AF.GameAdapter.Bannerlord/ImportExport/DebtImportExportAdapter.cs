using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge;

// Original manual file flow; mutations remain with the existing domain authority.
internal sealed class DebtImportExportAdapter
{
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 internal DebtImportExportAdapter(Action<string,string,Action,Action,Action> showDuplicate) { _showDuplicate=showDuplicate; }
	internal void ExportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			RewardSystemBehavior.DebtExportEntry value = null;
			(RewardSystemBehavior.Instance?.ExportDebtEntries())?.TryGetValue(heroId, out value);
			if (value == null)
			{
				value = new RewardSystemBehavior.DebtExportEntry();
			}
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, NpcDataIdentityFileAdapter.ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportSingleNpcDebtData(string folderName, string heroId)
	{
		try
		{
			string text = (folderName ?? "").Trim();
			string importDir = null;
			string text2 = null;
			string text3 = null;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				text3 = text;
				try
				{
					importDir = Path.GetDirectoryName(Path.GetFullPath(text));
				}
				catch
				{
					importDir = Path.GetDirectoryName(text);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
				{
					importDir = text;
				}
				else
				{
					importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
				}
				if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
					return;
				}
				text2 = Path.Combine(importDir, "debt");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "debt", StringComparison.OrdinalIgnoreCase))
						{
							text2 = importDir;
						}
					}
					catch
					{
					}
				}
				if (!Directory.Exists(text2))
				{
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
					return;
				}
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> all = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			if (string.IsNullOrEmpty(text3))
			{
				text3 = NpcDataIdentityFileAdapter.FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplySingleImportedDebtEntry(all, heroId, null));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
				return;
			}
			RewardSystemBehavior.DebtExportEntry entry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text3);
			bool flag = entry != null;
			bool flag2 = all.ContainsKey(heroId);
			Action action = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplySingleImportedDebtEntry(all, heroId, entry));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				_showDuplicate("检测到重复 - 欠款", "检测到该 NPC 已存在欠款记录：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
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

	internal void ExportDebtData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportDebtToDirectory(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportDebtData(string folderName)
	{
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string path = Path.Combine(importDir, "debt");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 debt 目录。"));
				return;
			}
			RewardSystemBehavior rs = RewardSystemBehavior.Instance;
			if (rs == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：RewardSystemBehavior 未初始化。"));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dict = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = NpcDataFileName.TryParseHeroId(text);
				if (!string.IsNullOrEmpty(text2))
				{
					RewardSystemBehavior.DebtExportEntry debtExportEntry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text);
					if (debtExportEntry != null)
					{
						dict[text2] = debtExportEntry;
					}
				}
			}
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> exist = rs.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					if (!string.IsNullOrEmpty(key) && exist.ContainsKey(key))
					{
						num++;
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(exist, dict, true));
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				rs.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(exist, dict, false));
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));
			};
			if (num > 0)
			{
				_showDuplicate("检测到重复 - 欠款", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
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

	internal void ExportDebtToDirectory(string exportDir, NpcDataIdentityFileAdapter.LookupScope lookup = null)
	{
		lookup ??= new NpcDataIdentityFileAdapter.LookupScope();
		string text = exportDir;
			string text2 = Path.Combine(text, "debt");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			RewardSystemBehavior instance = RewardSystemBehavior.Instance;
			Dictionary<string, RewardSystemBehavior.DebtExportEntry> dictionary = ((instance != null) ? instance.ExportDebtEntries() : new Dictionary<string, RewardSystemBehavior.DebtExportEntry>());
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, RewardSystemBehavior.DebtExportEntry> item in dictionary)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(item.Key, lookup.ResolveHeroNameForNpcDataFile(item.Key)));
						PlayerExportsStore.WriteJson(path2, item.Value);
					}
				}
			}
	}

	internal Dictionary<string,RewardSystemBehavior.DebtExportEntry> PrepareDebtDirectory(string path3,Dictionary<string,RewardSystemBehavior.DebtExportEntry> existDebt,out int num5,out int num6)
	{
		Dictionary<string,RewardSystemBehavior.DebtExportEntry> debtNew=null;
		num5=0;num6=0;
			if (Directory.Exists(path3))
			{
				string[] files3 = Directory.GetFiles(path3, "*.json");
				debtNew = new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
				string[] array3 = files3;
				foreach (string text5 in array3)
				{
					string text6 = NpcDataFileName.TryParseHeroId(text5);
					if (!string.IsNullOrEmpty(text6))
					{
						RewardSystemBehavior.DebtExportEntry debtExportEntry = PlayerExportsStore.ReadJson<RewardSystemBehavior.DebtExportEntry>(text5);
						if (debtExportEntry != null)
						{
							debtNew[text6] = debtExportEntry;
						}
					}
				}
				num6 = debtNew.Count;
				if (existDebt != null)
				{
					foreach (string key3 in debtNew.Keys)
					{
						if (!string.IsNullOrEmpty(key3) && existDebt.ContainsKey(key3))
						{
							num5++;
						}
					}
				}
			}
		return debtNew;
	}

 internal void ApplyPreparedDebt(RewardSystemBehavior authority, Dictionary<string,RewardSystemBehavior.DebtExportEntry> existing,Dictionary<string,RewardSystemBehavior.DebtExportEntry> imported,bool overwriteExisting)
 { authority.ImportDebtEntries(DebtImportMergePolicy.ApplyImportedDebtEntries(existing,imported,overwriteExisting)); }

internal int CountDebtOwnersForDev()
	{
		try
		{
			return RewardSystemBehavior.Instance?.ExportDebtEntries()?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}
}
