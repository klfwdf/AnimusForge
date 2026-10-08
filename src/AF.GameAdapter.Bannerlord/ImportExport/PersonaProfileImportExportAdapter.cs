using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Formats and confirmation preparation use the existing campaign authority, never a copied store.
internal sealed class PersonaProfileImportExportAdapter
{
 private readonly PersonaProfileStateOwner _profiles;
 private readonly Func<long> _captureGeneration;
 private readonly Func<long,bool> _isCurrent;
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 internal PersonaProfileImportExportAdapter(PersonaProfileStateOwner profiles, Func<long> captureGeneration,
  Func<long,bool> isCurrent, Action<string,string,Action,Action,Action> showDuplicate)
 { _profiles=profiles; _captureGeneration=captureGeneration; _isCurrent=isCurrent; _showDuplicate=showDuplicate; }
 internal static void StampNpcPersonaProfile(string heroId, NpcPersonaProfile profile)
  => PersonaImportOwner.StampProfileMetadata(heroId, profile,
   id => MemoryEntityIdentityBannerlordAdapter.ResolveHeroByIdForNpcData(id)?.Name?.ToString());
 internal static bool TryPrepareNpcPersonaProfileForWrite(string heroId, NpcPersonaProfile profile)
 {
  if (string.IsNullOrWhiteSpace(heroId) || profile == null) return false;
  StampNpcPersonaProfile(heroId, profile);
  return true;
 }
 internal bool ApplyImportedSinglePersonaProfile(string heroId, NpcPersonaProfile imported, long generation)
 {
  if (!_isCurrent(generation)) return false;
  if (imported != null) StampNpcPersonaProfile(heroId, imported);
  PersonaImportOwner.ApplySingleProfile(ref _profiles.Profiles, heroId, imported);
  return true;
 }
 internal bool ApplyImportedPersonaProfiles(Dictionary<string,NpcPersonaProfile> imported, bool overwriteExisting, long generation)
 {
  if (!_isCurrent(generation)) return false;
  PersonaImportOwner.ApplyProfiles(ref _profiles.Profiles, imported, overwriteExisting);
  return true;
 }
	internal void ExportSingleNpcPersonaData(string folderName, string heroId)
	{
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			NpcPersonaProfile value = null;
			if (_profiles.Profiles != null)
			{
				_profiles.Profiles.TryGetValue(heroId, out value);
			}
			if (value == null)
			{
				value = new NpcPersonaProfile();
			}
			if (!lookup.TryPrepareNpcPersonaProfileForWrite(heroId, value))
			{
				value = new NpcPersonaProfile();
				lookup.StampNpcPersonaProfile(heroId, value);
			}
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, lookup.ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportSingleNpcPersonaData(string folderName, string heroId)
	{
		long importGeneration = _captureGeneration();
		if (!_isCurrent(importGeneration)) return;
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
				text2 = Path.Combine(importDir, "personality_background");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "personality_background", StringComparison.OrdinalIgnoreCase))
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
					InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
					return;
				}
				text3 = NpcDataIdentityFileAdapter.FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：该NPC没有对应的导出文件。"));
				return;
			}
			if (!NpcDataIdentityFileAdapter.TryResolveNpcDataFileHeroIdForImport(text3, out var resolvedHeroId, out var warning) || !string.Equals((resolvedHeroId ?? "").Trim(), (heroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：个性/背景文件与当前NPC不匹配。"));
				Logger.Log("NpcPersona", "[WARN] Skipped single persona import file " + Path.GetFileName(text3) + " for hero=" + heroId + ": " + (warning ?? ("resolvedHeroId=" + resolvedHeroId)));
				return;
			}
			NpcPersonaProfile prof = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text3);
			if (_profiles.Profiles == null)
			{
				_profiles.Profiles = new Dictionary<string, NpcPersonaProfile>();
			}
			bool flag = prof != null;
			bool flag2 = false;
			try
			{
				flag2 = _profiles.Profiles.TryGetValue(heroId, out var value) && value != null;
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (!ApplyImportedSinglePersonaProfile(heroId, prof, importGeneration)) return;
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));
			};
			Action onSkipDuplicates = delegate
			{
				InformationManager.DisplayMessage(new InformationMessage("已跳过重复条目：" + heroId));
			};
			if (flag && flag2)
			{
				_showDuplicate("检测到重复 - 个性/背景", "检测到该 NPC 已存在个性/背景：" + heroId + "\n请选择处理方式：", action, onSkipDuplicates, delegate
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

	internal void ExportPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportPersonaToDirectory(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}

	internal void ImportPersonaData(string folderName) => ImportPersonaDataScoped(folderName, null);

	internal void ImportPersonaDataScoped(string folderName, OnboardingImportStep completion)
	{
        if (completion != null && !completion.CanApply()) return;
		long importGeneration = _captureGeneration();
		if (!_isCurrent(importGeneration)) { completion?.Finish(OnboardingImportResult.Retired); return; }
		try
		{
			var lookup = new NpcDataIdentityFileAdapter.LookupScope();
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			string path = Path.Combine(importDir, "personality_background");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到 personality_background 目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			Dictionary<string, NpcPersonaProfile> dict = new Dictionary<string, NpcPersonaProfile>();
			string[] array = files;
			foreach (string text in array)
			{
				if (lookup.TryResolveNpcDataFileHeroIdForImport(text, out var text2, out var warning))
				{
					NpcPersonaProfile npcPersonaProfile = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text);
					if (npcPersonaProfile != null)
					{
						lookup.StampNpcPersonaProfile(text2, npcPersonaProfile);
						dict[text2] = npcPersonaProfile;
					}
				}
				else
				{
					Logger.Log("NpcPersona", "[WARN] Skipped persona import file " + Path.GetFileName(text) + ": " + warning);
				}
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				if (_profiles.Profiles != null)
				{
					foreach (string key in dict.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _profiles.Profiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			Action action = delegate
			{
				if (!ApplyImportedPersonaProfiles(dict, true, importGeneration)) { completion?.Finish(OnboardingImportResult.Retired); return; }
				InformationManager.DisplayMessage(new InformationMessage("导入完成：" + importDir));

                completion?.Finish(OnboardingImportResult.Completed);
};
			Action onSkipDuplicates = delegate
			{
				if (!ApplyImportedPersonaProfiles(dict, false, importGeneration)) { completion?.Finish(OnboardingImportResult.Retired); return; }
				InformationManager.DisplayMessage(new InformationMessage("导入完成（已跳过重复）：" + importDir));

                completion?.Finish(OnboardingImportResult.Completed);
};
            if (completion != null) { action = completion.Guard(action); onSkipDuplicates = completion.Guard(onSkipDuplicates); }
			if (num > 0)
			{
				completion?.Pending();
				_showDuplicate("检测到重复 - 个性/背景", "导入数据与当前游戏存在重复 HeroId。\n重复：" + num + " / 总计：" + count + "\n请选择处理方式：", action, onSkipDuplicates, delegate
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

	internal void ExportPersonaToDirectory(string exportDir, NpcDataIdentityFileAdapter.LookupScope lookup = null)
	{
		lookup ??= new NpcDataIdentityFileAdapter.LookupScope();
		string text = exportDir;
			string text2 = Path.Combine(text, "personality_background");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			if (_profiles.Profiles != null)
			{
				foreach (KeyValuePair<string, NpcPersonaProfile> npcPersonaProfile in _profiles.Profiles)
				{
					if (!string.IsNullOrEmpty(npcPersonaProfile.Key) && lookup.TryPrepareNpcPersonaProfileForWrite(npcPersonaProfile.Key, npcPersonaProfile.Value))
					{
						string path2 = Path.Combine(text2, NpcDataFileName.Build(npcPersonaProfile.Key, lookup.ResolveHeroNameForNpcDataFile(npcPersonaProfile.Key)));
						PlayerExportsStore.WriteJson(path2, npcPersonaProfile.Value);
					}
				}
			}
	}

	internal Dictionary<string,NpcPersonaProfile> PreparePersonaDirectory(string importDir,NpcDataIdentityFileAdapter.LookupScope lookup,out int num,out int num2)
	{
		Dictionary<string,NpcPersonaProfile> pbNew = null;
		num=0;num2=0;
			string path = Path.Combine(importDir, "personality_background");
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*.json");
				pbNew = new Dictionary<string, NpcPersonaProfile>();
				string[] array = files;
				foreach (string text in array)
				{
					if (lookup.TryResolveNpcDataFileHeroIdForImport(text, out var text2, out var warning))
					{
						NpcPersonaProfile npcPersonaProfile = PlayerExportsStore.ReadJson<NpcPersonaProfile>(text);
						if (npcPersonaProfile != null)
						{
							lookup.StampNpcPersonaProfile(text2, npcPersonaProfile);
							pbNew[text2] = npcPersonaProfile;
						}
					}
					else
					{
						Logger.Log("NpcPersona", "[WARN] Skipped persona import file " + Path.GetFileName(text) + ": " + warning);
					}
				}
				num2 = pbNew.Count;
				if (_profiles.Profiles != null)
				{
					foreach (string key in pbNew.Keys)
					{
						if (!string.IsNullOrEmpty(key) && _profiles.Profiles.ContainsKey(key))
						{
							num++;
						}
					}
				}
			}
		return pbNew;
	}

 internal void PrepareUnnamedPersonaCounts(string importDir, out int num7, out int num8)
 {
 num7=0; num8=0;
			try
			{
				List<string> source = new List<string>
				{
					Path.Combine(importDir, "unnamed_persona"),
					importDir
				};
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string item in source.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					if (!Directory.Exists(item))
					{
						continue;
					}
					string[] array4 = null;
					try
					{
						array4 = Directory.GetFiles(item, "*.json");
					}
					catch
					{
						array4 = null;
					}
					if (array4 == null)
					{
						continue;
					}
					string[] array5 = array4;
					foreach (string path4 in array5)
					{
						string text7 = null;
						try
						{
							text7 = (ReadUnnamedPersonaImportKey(path4) ?? "").Trim().ToLower();
						}
						catch
						{
							text7 = null;
						}
						if (string.IsNullOrWhiteSpace(text7))
						{
							string text8 = Path.GetFileNameWithoutExtension(path4) ?? "";
							int num13 = text8.LastIndexOf("__", StringComparison.Ordinal);
							if (num13 > 0)
							{
								text8 = text8.Substring(0, num13);
							}
							text7 = (text8 ?? "").Trim().ToLower();
						}
						if (!string.IsNullOrEmpty(text7) && hashSet.Add(text7))
						{
							num8++;
							if (ShoutUtils.HasUnnamedPersonaKey(text7))
							{
								num7++;
							}
						}
					}
				}
			}
			catch
			{
				num7 = 0;
				num8 = 0;
			}
 }

internal void ExportUnnamedPersonaData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ShoutUtils.ExportUnnamedPersonaToDir(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}
internal void ExportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：Key 为空。"));
				return;
			}
			string personality = "";
			string background = "";
			bool flag = false;
			try
			{
				flag = ShoutUtils.TryGetUnnamedPersonaByKey(text, out personality, out background);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("导出失败：找不到该未命名NPC条目：" + text));
				return;
			}
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text2 = export.CandidatePath;
			string text3 = Path.Combine(text2, "unnamed_persona");
			Directory.CreateDirectory(text3);
			string text4 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text4 = text4.Replace(oldChar, '_');
			}
			text4 = text4.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text4.Contains("__"))
			{
				text4 = text4.Replace("__", "_");
			}
			text4 = (text4 ?? "").Trim();
			if (text4.Length > 120)
			{
				text4 = text4.Substring(0, 120);
			}
			if (string.IsNullOrEmpty(text4))
			{
				text4 = "unnamed";
			}
			string path2 = Path.Combine(text3, text4 + ".json");
			PlayerExportsStore.WriteJson(path2, new UnnamedPersonaSingleJson
			{
				Key = text,
				Personality = (personality ?? "").Trim(),
				Background = (background ?? "").Trim()
			});
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage("导出完成：" + export.FinalPath));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导出失败：" + ex.Message));
		}
	}
internal void ImportSingleUnnamedPersonaData(string folderName, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：Key 为空。"));
				return;
			}
			string text2 = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				return;
			}
			string dir = Path.Combine(text2, "unnamed_persona");
			string dir2 = text2;
			string text3 = FindUnnamedPersonaJsonByKey(dir, text);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = FindUnnamedPersonaJsonByKey(dir2, text);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到该未命名NPC条目的导出文件：" + text));
				return;
			}
			UnnamedPersonaSingleJson unnamedPersonaSingleJson = PlayerExportsStore.ReadJson<UnnamedPersonaSingleJson>(text3);
			if (unnamedPersonaSingleJson == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：JSON 解析失败。"));
				return;
			}
			string text4 = (unnamedPersonaSingleJson.Personality ?? "").Trim();
			string text5 = (unnamedPersonaSingleJson.Background ?? "").Trim();
			bool flag = !string.IsNullOrEmpty(text4) || !string.IsNullOrEmpty(text5);
			bool flag2 = false;
			try
			{
				flag2 = ShoutUtils.HasUnnamedPersonaKey(text);
			}
			catch
			{
				flag2 = false;
			}
			if (flag && flag2)
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：检测到重复 Key（" + text + "），当前游戏已存在该条目，禁止导入覆盖。"));
				return;
			}
			ShoutUtils.SaveUnnamedPersonaByKey(text, text4, text5);
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text2));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}
internal static string FindUnnamedPersonaJsonByKey(string dir, string key)
	{
		try
		{
			string text = (key ?? "").Trim().ToLower();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = text2.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
			while (text2.Contains("__"))
			{
				text2 = text2.Replace("__", "_");
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					UnnamedPersonaSingleJson unnamedPersonaSingleJson = JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value);
					if (unnamedPersonaSingleJson != null)
					{
						string a = (unnamedPersonaSingleJson.Key ?? "").Trim().ToLower();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}
internal List<string> GetUnnamedPersonaKeysFromImportFolderForDev(string folderName, int maxCount = 200)
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
				Path.Combine(text, "unnamed_persona"),
				text
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
							string text2 = (JsonConvert.DeserializeObject<UnnamedPersonaSingleJson>(value)?.Key ?? "").Trim().ToLower();
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
internal void ImportUnnamedPersonaData(string folderName) => ImportUnnamedPersonaDataScoped(folderName, null);

	internal void ImportUnnamedPersonaDataScoped(string folderName, OnboardingImportStep completion)
	{
        if (completion != null && !completion.CanApply()) return;
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage("导入失败：找不到导出目录。"));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			if (!ValidateUnnamedPersonaKeysForImport(text, out var error))
			{
				InformationManager.DisplayMessage(new InformationMessage(error));
				completion?.Finish(OnboardingImportResult.Failed);
				return;
			}
			if (completion == null) ShoutUtils.ImportUnnamedPersonaFromDir(text);
            else if (!ShoutUtils.TryImportUnnamedPersonaFromDir(text)) { completion.Finish(OnboardingImportResult.Failed); return; }
			InformationManager.DisplayMessage(new InformationMessage("导入完成：" + text));
            completion?.Finish(OnboardingImportResult.Completed);
		}
		catch (Exception ex)
		{
            completion?.Finish(OnboardingImportResult.Failed);
			InformationManager.DisplayMessage(new InformationMessage("导入失败：" + ex.Message));
		}
	}
internal static string ReadUnnamedPersonaImportKey(string file) => PlayerExportsStore.ReadJson<UnnamedPersonaSingleJson>(file)?.Key;
 internal static bool ValidateUnnamedPersonaKeysForImport(string importDir,out string error)
  => UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(importDir, ReadUnnamedPersonaImportKey, ShoutUtils.HasUnnamedPersonaKey, out error);
 internal void ApplyUnnamedPersonaDirectory(string importDir,bool overwriteExisting,Action refreshStorage)
 { ShoutUtils.ImportUnnamedPersonaFromDir(importDir,overwriteExisting:overwriteExisting); refreshStorage(); }


internal int CountUnnamedPersonaForDev()
	{
		try
		{
			string text = ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false);
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			JObject jObject = JObject.Parse(text);
			return (jObject["Profiles"] as JObject)?.Count ?? 0;
		}
		catch
		{
			return 0;
		}
	}
	internal void ClearUnnamedPersonaDataForCurrentSave(Action<string> persistJson)
	{
		try
		{
			ShoutUtils.ImportUnnamedPersonaStateJson("", overwriteExisting: true);
			persistJson(ShoutUtils.ExportUnnamedPersonaStateJson(pretty: false) ?? "");
		}
		catch (Exception ex)
		{
			persistJson("");
			Logger.Log("DevDataManagement", "[WARN] Clear unnamed persona failed: " + ex.Message);
		}
	}

}
