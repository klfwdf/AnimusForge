using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Library;
using static AnimusForge.MyBehavior;

namespace AnimusForge;

// Compressed-memory file format is distinct from the All package's raw DialogueDay history.
internal sealed class MemoryHistoryImportExportAdapter
{
 private readonly MemoryBusinessStateOwner _memory;
 private readonly Func<long> _captureGeneration;
 private readonly Func<long,bool> _isCurrent;
 private readonly Action<string> _markOverviewDirty;
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 internal MemoryHistoryImportExportAdapter(MemoryBusinessStateOwner memory, Func<long> captureGeneration,
  Func<long,bool> isCurrent, Action<string> markOverviewDirty, Action<string,string,Action,Action,Action> showDuplicate)
 { _memory=memory; _captureGeneration=captureGeneration; _isCurrent=isCurrent; _markOverviewDirty=markOverviewDirty; _showDuplicate=showDuplicate; }
 internal CompressedMemoryExportBundle BuildCompressedMemoryExportBundle(string heroId)
  => MemoryImportExportOwner.Build(heroId, MemoryImportExportOwner.Capture(_memory));
 internal bool HasCompressedMemoryDataForHero(string heroId)
  => MemoryImportExportOwner.HasData(heroId, MemoryImportExportOwner.Capture(_memory));
 internal bool ApplyCompressedMemoryExportBundle(string heroId, CompressedMemoryExportBundle bundle, bool overwriteExisting)
  => MemoryImportExportOwner.ApplyToAuthority(heroId,bundle,overwriteExisting,_memory,_markOverviewDirty);
	internal void ExportSingleNpcDialogueHistoryData(string folderName, string heroId)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			string text2 = Path.Combine(text, "compressed_memory");
			Directory.CreateDirectory(text2);
			CompressedMemoryExportBundle value = BuildCompressedMemoryExportBundle(heroId);
			string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, NpcDataIdentityFileAdapter.ResolveHeroNameForNpcDataFile(heroId)));
			PlayerExportsStore.WriteJson(path2, value);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ExportCompleted(export.FinalPath)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ExportFailed(ex.Message)));
		}
	}

	internal void ImportSingleNpcDialogueHistoryData(string folderName, string heroId)
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
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.MissingExportDirectory()));
					return;
				}
				text2 = Path.Combine(importDir, "compressed_memory");
				if (!Directory.Exists(text2))
				{
					try
					{
						string fileName = Path.GetFileName(importDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
						if (string.Equals(fileName, "compressed_memory", StringComparison.OrdinalIgnoreCase))
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
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.MissingDirectory("compressed_memory")));
					return;
				}
				text3 = NpcDataIdentityFileAdapter.FindNpcJsonByHeroId(text2, heroId);
			}
			if (string.IsNullOrEmpty(text3) || !File.Exists(text3))
			{
				InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.NpcExportMissing()));
				return;
			}
			if (!CompressedMemoryExportBundleReader.TryRead(text3, out var bundle, out var memoryImportError))
			{
				InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ImportFailed(memoryImportError)));
				return;
			}
			bool flag = true;
			bool flag2 = false;
			try
			{
				string key = MemoryRecordRules.NormalizeMemoryHeroId(heroId);
				flag2 = HasCompressedMemoryDataForHero(key);
			}
			catch
			{
				flag2 = false;
			}
			Action action = delegate
			{
				if (!_isCurrent(importGeneration)) return;
				if (ApplyCompressedMemoryExportBundle(heroId, bundle, overwriteExisting: true))
				{
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ImportCompleted(importDir)));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.InvalidData("compressed-memory data")));
				}
			};
			Action onSkipDuplicates = delegate
			{
				if (!_isCurrent(importGeneration)) return;
				if (ApplyCompressedMemoryExportBundle(heroId, bundle, overwriteExisting: false))
				{
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ImportCompletedDuplicates(heroId)));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.InvalidData("compressed-memory data")));
				}
			};
			if (flag && flag2)
			{
				_showDuplicate(ImportExportUiText.DuplicateTitle("compressed memory"), ImportExportUiText.DuplicateExisting("compressed-memory", heroId), action, onSkipDuplicates, delegate
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
			if (!_isCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ImportFailed(ex.Message)));
		}
	}

	internal void ExportDialogueHistoryData(string folderName)
	{
		try
		{
			string playerExportsRootPath = PlayerExportsStore.GetPlayerExportsRootPath();
			Directory.CreateDirectory(playerExportsRootPath);
			string path = PlayerExportsStore.ResolveExportFolderName(folderName);
			var export = PlayerExportsStore.BeginExportPackage(playerExportsRootPath, path);
			string text = export.CandidatePath;
			ExportCompressedMemoryToDirectory(text);
			export.Publish();
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ExportCompleted(export.FinalPath)));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ExportFailed(ex.Message)));
		}
	}

	internal void ImportDialogueHistoryData(string folderName)
	{
		long importGeneration = _captureGeneration();
		if (!_isCurrent(importGeneration)) return;
		try
		{
			string importDir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.MissingExportDirectory()));
				return;
			}
			string path = Path.Combine(importDir, "compressed_memory");
			if (!Directory.Exists(path))
			{
				InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.MissingDirectory("compressed_memory")));
				return;
			}
			string[] files = Directory.GetFiles(path, "*.json");
			int invalidMemoryFiles = 0;
			Dictionary<string, CompressedMemoryExportBundle> dict = new Dictionary<string, CompressedMemoryExportBundle>(StringComparer.OrdinalIgnoreCase);
			string[] array = files;
			foreach (string text in array)
			{
				string text2 = NpcDataFileName.TryParseHeroId(text);
				if (!string.IsNullOrEmpty(text2))
				{
					if (CompressedMemoryExportBundleReader.TryRead(text, out var bundle, out var memoryImportError))
					{
						dict[MemoryRecordRules.NormalizeMemoryHeroId(text2)] = bundle;
					}
					else
					{
						invalidMemoryFiles++;
						Logger.Log("MemoryImport", "[WARN] Skipped " + Path.GetFileName(text) + ": " + memoryImportError);
					}
				}
				else
				{
					invalidMemoryFiles++;
				}
			}
			if (files.Length > 0 && dict.Count == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.AllInvalid(invalidMemoryFiles)));
				return;
			}
			int num = 0;
			int count = dict.Count;
			try
			{
				foreach (string key in dict.Keys)
				{
					string text3 = MemoryRecordRules.NormalizeMemoryHeroId(key);
					if (!string.IsNullOrWhiteSpace(text3) && HasCompressedMemoryDataForHero(text3))
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
				if (!_isCurrent(importGeneration)) return;
				foreach (KeyValuePair<string, CompressedMemoryExportBundle> item in dict)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						ApplyCompressedMemoryExportBundle(item.Key, item.Value, overwriteExisting: true);
					}
				}
				InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? ImportExportUiText.PartialImport(invalidMemoryFiles, importDir) : ImportExportUiText.ImportCompleted(importDir)));
			};
			Action onSkipDuplicates = delegate
			{
				if (!_isCurrent(importGeneration)) return;
				foreach (KeyValuePair<string, CompressedMemoryExportBundle> item2 in dict)
				{
					if (!string.IsNullOrEmpty(item2.Key) && item2.Value != null)
					{
						ApplyCompressedMemoryExportBundle(item2.Key, item2.Value, overwriteExisting: false);
					}
				}
				InformationManager.DisplayMessage(new InformationMessage(invalidMemoryFiles > 0 ? ImportExportUiText.PartialImportDuplicates(invalidMemoryFiles, importDir) : ImportExportUiText.ImportCompletedDuplicates(importDir)));
			};
			if (num > 0)
			{
				_showDuplicate(ImportExportUiText.DuplicateTitle("compressed memory"), ImportExportUiText.DuplicateMany("compressed-memory", num, count), action, onSkipDuplicates, delegate
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
			if (!_isCurrent(importGeneration)) return;
			InformationManager.DisplayMessage(new InformationMessage(ImportExportUiText.ImportFailed(ex.Message)));
		}
	}

	internal void ExportCompressedMemoryToDirectory(string exportDir, NpcDataIdentityFileAdapter.LookupScope lookup = null)
	{
		lookup ??= new NpcDataIdentityFileAdapter.LookupScope();
		string text = exportDir;
			string text2 = Path.Combine(text, "compressed_memory");
			Directory.CreateDirectory(text2);
			PlayerExportsStore.ClearCandidateJsonFiles(text2);
			HashSet<string> heroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_memory.Drafts != null)
			{
				foreach (string key in _memory.Drafts.Keys)
				{
					heroIds.Add(MemoryRecordRules.NormalizeMemoryHeroId(key));
				}
			}
			if (_memory.Blocks != null)
			{
				foreach (string key2 in _memory.Blocks.Keys)
				{
					heroIds.Add(MemoryRecordRules.NormalizeMemoryHeroId(key2));
				}
			}
			foreach (MemorySummaryJob job in _memory.DailyQueue ?? new List<MemorySummaryJob>())
			{
				if (job != null)
				{
					heroIds.Add(MemoryRecordRules.NormalizeMemoryHeroId(job.HeroId));
				}
			}
			if (_memory.Overviews != null)
			{
				foreach (string key3 in _memory.Overviews.Keys)
				{
					heroIds.Add(MemoryRecordRules.NormalizeMemoryHeroId(key3));
				}
			}
			foreach (MemoryOverviewJob job2 in _memory.OverviewQueue ?? new List<MemoryOverviewJob>())
			{
				if (job2 != null)
				{
					heroIds.Add(MemoryRecordRules.NormalizeMemoryHeroId(job2.HeroId));
				}
			}
			foreach (string heroId in heroIds.Where((string x) => !string.IsNullOrWhiteSpace(x)))
			{
				string path2 = Path.Combine(text2, NpcDataFileName.Build(heroId, lookup.ResolveHeroNameForNpcDataFile(heroId)));
				PlayerExportsStore.WriteJson(path2, BuildCompressedMemoryExportBundle(heroId));
			}
	}

	internal void ExportRawHistoryToDirectory(string exportDir, NpcDataIdentityFileAdapter.LookupScope lookup = null)
	{
		lookup ??= new NpcDataIdentityFileAdapter.LookupScope();
		string text = exportDir;
			string text3 = Path.Combine(text, "dialogue_history");
			Directory.CreateDirectory(text3);
			PlayerExportsStore.ClearCandidateJsonFiles(text3);
			if (_memory.History != null)
			{
				foreach (KeyValuePair<string, List<DialogueDay>> item in _memory.History)
				{
					if (!string.IsNullOrEmpty(item.Key) && item.Value != null)
					{
						string path3 = Path.Combine(text3, NpcDataFileName.Build(item.Key, lookup.ResolveHeroNameForNpcDataFile(item.Key)));
						PlayerExportsStore.WriteJson(path3, item.Value);
					}
				}
			}
	}

	internal Dictionary<string,CompressedMemoryExportBundle> PrepareCompressedDirectory(string importDir,out int num3,out int num4,out int invalidMemoryFiles)
	{
		Dictionary<string,CompressedMemoryExportBundle> dhNew=null;
		num3=0;num4=0;invalidMemoryFiles=0;
			string path2 = Path.Combine(importDir, "compressed_memory");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, CompressedMemoryExportBundle>(StringComparer.OrdinalIgnoreCase);
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = NpcDataFileName.TryParseHeroId(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						if (CompressedMemoryExportBundleReader.TryRead(text3, out var bundle, out var memoryImportError))
						{
							dhNew[MemoryRecordRules.NormalizeMemoryHeroId(text4)] = bundle;
						}
						else
						{
							invalidMemoryFiles++;
							Logger.Log("MemoryImport", "[WARN] Skipped " + Path.GetFileName(text3) + ": " + memoryImportError);
						}
					}
					else
					{
						invalidMemoryFiles++;
					}
				}
				num4 = dhNew.Count;
				foreach (string key2 in dhNew.Keys)
				{
					string memoryKey = MemoryRecordRules.NormalizeMemoryHeroId(key2);
					if (!string.IsNullOrEmpty(memoryKey) && HasCompressedMemoryDataForHero(memoryKey))
					{
						num3++;
					}
				}
			}
		return dhNew;
	}

	internal Dictionary<string,List<DialogueDay>> PrepareRawHistoryDirectory(string importDir,out int num3,out int num4)
	{
		Dictionary<string,List<DialogueDay>> dhNew=null;
		num3=0;num4=0;
			string path2 = Path.Combine(importDir, "dialogue_history");
			if (Directory.Exists(path2))
			{
				string[] files2 = Directory.GetFiles(path2, "*.json");
				dhNew = new Dictionary<string, List<DialogueDay>>();
				string[] array2 = files2;
				foreach (string text3 in array2)
				{
					string text4 = NpcDataFileName.TryParseHeroId(text3);
					if (!string.IsNullOrEmpty(text4))
					{
						List<DialogueDay> list = PlayerExportsStore.ReadJson<List<DialogueDay>>(text3);
						if (list != null)
						{
							dhNew[text4] = list;
						}
					}
				}
				num4 = dhNew.Count;
				if (_memory.History != null)
				{
					foreach (string key2 in dhNew.Keys)
					{
						if (!string.IsNullOrEmpty(key2) && _memory.History.ContainsKey(key2))
						{
							num3++;
						}
					}
				}
			}
		return dhNew;
	}

 internal void ApplyCompressedDirectory(Dictionary<string,CompressedMemoryExportBundle> dhNew,bool overwriteExisting)
 {
foreach (KeyValuePair<string, CompressedMemoryExportBundle> entry in dhNew)
						{
							if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
							{
								ApplyCompressedMemoryExportBundle(entry.Key, entry.Value, overwriteExisting: overwriteExisting);
							}
						}
 }
}
