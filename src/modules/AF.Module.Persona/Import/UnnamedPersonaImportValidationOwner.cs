using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AnimusForge;

internal static class UnnamedPersonaImportValidationOwner
{
	internal static string TryGetUnnamedPersonaKeyFromImportFile(string file, Func<string, string> readKey)
	{
		string text = null;
		try
		{
			text = (readKey(file) ?? "").Trim().ToLower();
		}
		catch
		{
			text = null;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				string text2 = Path.GetFileNameWithoutExtension(file) ?? "";
				int num = text2.LastIndexOf("__", StringComparison.Ordinal);
				if (num > 0)
				{
					text2 = text2.Substring(0, num);
				}
				text = (text2 ?? "").Trim().ToLower();
			}
			catch
			{
				text = null;
			}
		}
		return (text ?? "").Trim().ToLower();
	}

	internal static bool ValidateUnnamedPersonaKeysForImport(string importDir, Func<string, string> readKey, Func<string, bool> hasExistingKey, out string error)
	{
		error = "";
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				error = "导入失败：找不到导出目录。";
				return false;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "unnamed_persona"),
				importDir
			};
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
				foreach (string file in array2)
				{
					string text = TryGetUnnamedPersonaKeyFromImportFile(file, readKey);
					if (!string.IsNullOrEmpty(text))
					{
						if (!hashSet.Add(text))
						{
							error = "导入失败：导入文件夹中存在重复 Key（" + text + "）。";
							return false;
						}
						if (hasExistingKey(text))
						{
							error = "导入失败：Key 冲突（" + text + "）。当前游戏已存在该 Key，禁止导入覆盖。";
							return false;
						}
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "导入失败：Key 校验异常：" + ex.Message;
			return false;
		}
	}
}
