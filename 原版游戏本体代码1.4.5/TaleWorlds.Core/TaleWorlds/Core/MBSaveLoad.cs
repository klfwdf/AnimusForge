using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.Core;

public static class MBSaveLoad
{
	private const int MaxNumberOfAutoSaveFiles = 3;

	private static ISaveDriver _saveDriver = new FileDriver();

	private static int AutoSaveIndex = 0;

	private static string DefaultSaveGamePrefix = "save";

	private static string AutoSaveNamePrefix = DefaultSaveGamePrefix + "auto";

	private static GameTextManager _textProvider;

	private static bool DoNotShowSaveErrorAgain = false;

	public static char ModuleVersionSeperator => ':';

	public static char ModuleCodeSeperator => ';';

	public static ApplicationVersion LastLoadedGameVersion { get; private set; }

	public static ApplicationVersion CurrentVersion { get; } = ApplicationVersion.FromParametersFile();

	public static bool IsUpdatingGameVersion => LastLoadedGameVersion.IsOlderThan(CurrentVersion);

	public static int NumberOfCurrentSaves { get; private set; }

	public static string ActiveSaveSlotName { get; private set; } = null;

	private static string GetAutoSaveName()
	{
		return AutoSaveNamePrefix + AutoSaveIndex;
	}

	private static void IncrementAutoSaveIndex()
	{
		AutoSaveIndex++;
		if (AutoSaveIndex > 3)
		{
			AutoSaveIndex = 1;
		}
	}

	private static void InitializeAutoSaveIndex(string saveName)
	{
		string text = string.Empty;
		if (saveName.StartsWith(AutoSaveNamePrefix))
		{
			text = saveName;
		}
		else
		{
			string[] saveFileNames = GetSaveFileNames();
			foreach (string text2 in saveFileNames)
			{
				if (text2.StartsWith(AutoSaveNamePrefix))
				{
					text = text2;
					break;
				}
			}
		}
		int result;
		if (string.IsNullOrEmpty(text))
		{
			AutoSaveIndex = 1;
		}
		else if (int.TryParse(text.Substring(AutoSaveNamePrefix.Length), out result) && result > 0 && result <= 3)
		{
			AutoSaveIndex = result;
		}
	}

	public static void SetSaveDriver(ISaveDriver saveDriver)
	{
		_saveDriver = saveDriver;
	}

	public static SaveGameFileInfo[] GetSaveFiles(Func<SaveGameFileInfo, bool> condition = null)
	{
		SaveGameFileInfo[] saveGameFileInfos = _saveDriver.GetSaveGameFileInfos();
		NumberOfCurrentSaves = saveGameFileInfos.Length;
		List<SaveGameFileInfo> list = new List<SaveGameFileInfo>();
		SaveGameFileInfo[] array = saveGameFileInfos;
		foreach (SaveGameFileInfo saveGameFileInfo in array)
		{
			if (condition == null || condition(saveGameFileInfo))
			{
				list.Add(saveGameFileInfo);
			}
		}
		return list.OrderByDescending((SaveGameFileInfo info) => info.MetaData.GetCreationTime()).ToArray();
	}

	public static bool IsSaveGameFileExists(string saveFileName)
	{
		return _saveDriver.IsSaveGameFileExists(saveFileName);
	}

	public static string[] GetSaveFileNames()
	{
		return _saveDriver.GetSaveGameFileNames();
	}

	public static LoadResult LoadSaveGameData(string saveName)
	{
		InitializeAutoSaveIndex(saveName);
		ISaveDriver saveDriver = _saveDriver;
		LoadResult loadResult = SaveManager.Load(saveName, saveDriver, loadAsLateInitialize: true);
		if (loadResult.Successful)
		{
			ActiveSaveSlotName = saveName;
			return loadResult;
		}
		Debug.Print("Error: Could not load the game!");
		return null;
	}

	public static SaveGameFileInfo GetSaveFileWithName(string saveName)
	{
		SaveGameFileInfo[] saveFiles = GetSaveFiles((SaveGameFileInfo x) => x.Name.Equals(saveName));
		if (saveFiles.Length == 0)
		{
			return null;
		}
		return saveFiles[0];
	}

	public static void QuickSaveCurrentGame(CampaignSaveMetaDataArgs campaignMetaData, Action<(SaveResult, string)> onSaveCompleted)
	{
		if (ActiveSaveSlotName == null)
		{
			ActiveSaveSlotName = GetNextAvailableSaveName();
		}
		Debug.Print("QuickSaveCurrentGame: " + ActiveSaveSlotName);
		OverwriteSaveAux(campaignMetaData, ActiveSaveSlotName, onSaveCompleted);
	}

	public static void AutoSaveCurrentGame(CampaignSaveMetaDataArgs campaignMetaData, Action<(SaveResult, string)> onSaveCompleted)
	{
		IncrementAutoSaveIndex();
		string autoSaveName = GetAutoSaveName();
		Debug.Print("AutoSaveCurrentGame: " + autoSaveName);
		OverwriteSaveAux(campaignMetaData, autoSaveName, onSaveCompleted);
	}

	public static void SaveAsCurrentGame(CampaignSaveMetaDataArgs campaignMetaData, string saveName, Action<(SaveResult, string)> onSaveCompleted)
	{
		ActiveSaveSlotName = saveName;
		Debug.Print("SaveAsCurrentGame: " + saveName);
		OverwriteSaveAux(campaignMetaData, saveName, onSaveCompleted);
	}

	private static void OverwriteSaveAux(CampaignSaveMetaDataArgs campaignMetaData, string saveName, Action<(SaveResult, string)> onSaveCompleted)
	{
		MetaData saveMetaData = GetSaveMetaData(campaignMetaData);
		bool isOverwritingExistingSave = IsSaveGameFileExists(saveName);
		if (IsMaxNumberOfSavesReached() && !isOverwritingExistingSave)
		{
			onSaveCompleted?.Invoke((SaveResult.SaveLimitReached, string.Empty));
			return;
		}
		OverwriteSaveFile(saveMetaData, saveName, delegate(SaveResult r)
		{
			onSaveCompleted?.Invoke((r, saveName));
			if (r == SaveResult.Success && !isOverwritingExistingSave)
			{
				NumberOfCurrentSaves++;
			}
		});
	}

	public static bool DeleteSaveGame(string saveName)
	{
		bool num = _saveDriver.Delete(saveName);
		if (num)
		{
			if (NumberOfCurrentSaves > 0)
			{
				NumberOfCurrentSaves--;
			}
			if (ActiveSaveSlotName == saveName)
			{
				ActiveSaveSlotName = null;
			}
		}
		return num;
	}

	public static void Initialize(GameTextManager localizedTextProvider)
	{
		_textProvider = localizedTextProvider;
		NumberOfCurrentSaves = _saveDriver.GetSaveGameFileInfos().Length;
	}

	public static void OnNewGame()
	{
		ActiveSaveSlotName = null;
		LastLoadedGameVersion = CurrentVersion;
		AutoSaveIndex = 0;
	}

	public static void OnGameDestroy()
	{
		LastLoadedGameVersion = ApplicationVersion.Empty;
	}

	public static void OnStartGame(LoadResult loadResult)
	{
		LastLoadedGameVersion = loadResult.MetaData.GetApplicationVersion();
	}

	public static bool IsSaveFileNameReserved(string name)
	{
		for (int i = 1; i <= 3; i++)
		{
			if (name == AutoSaveNamePrefix + i)
			{
				return true;
			}
		}
		return false;
	}

	private static string GetNextAvailableSaveName()
	{
		uint num = 0u;
		string[] saveFileNames = GetSaveFileNames();
		foreach (string text in saveFileNames)
		{
			if (text.StartsWith(DefaultSaveGamePrefix) && uint.TryParse(text.Substring(DefaultSaveGamePrefix.Length), out var result) && result > num)
			{
				num = result;
			}
		}
		return DefaultSaveGamePrefix + (num + 1).ToString("000");
	}

	private static void OverwriteSaveFile(MetaData metaData, string name, Action<SaveResult> onSaveCompleted)
	{
		try
		{
			SaveGame(name, metaData, delegate(SaveResult r)
			{
				onSaveCompleted(r);
				ShowErrorFromResult(r);
			});
		}
		catch
		{
			ShowErrorFromResult(SaveResult.GeneralFailure);
		}
	}

	private static void ShowErrorFromResult(SaveResult result)
	{
		switch (result)
		{
		case SaveResult.PlatformFileHelperFailure:
			Debug.FailedAssert("Save Failed:\n" + Common.PlatformFileHelper.GetError(), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBSaveLoad.cs", "ShowErrorFromResult", 314);
			break;
		case SaveResult.Success:
			return;
		}
		if (!DoNotShowSaveErrorAgain)
		{
			InformationManager.ShowInquiry(new InquiryData(_textProvider.FindText("str_save_unsuccessful_title").ToString(), _textProvider.FindText("str_game_save_result", result.ToString()).ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, _textProvider.FindText("str_ok").ToString(), _textProvider.FindText("str_dont_show_again").ToString(), null, delegate
			{
				DoNotShowSaveErrorAgain = true;
			}));
		}
	}

	private static void SaveGame(string saveName, MetaData metaData, Action<SaveResult> onSaveCompleted)
	{
		ISaveDriver saveDriver = _saveDriver;
		try
		{
			Game.Current.Save(metaData, saveName, saveDriver, onSaveCompleted);
		}
		catch (Exception ex)
		{
			Debug.Print("Unable to create save game data");
			Debug.Print(ex.Message);
			Debug.SilentAssert(ModuleHelper.GetModules().Any((ModuleInfo m) => !m.IsOfficial), ex.Message, getDump: false, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBSaveLoad.cs", "SaveGame", 347);
		}
	}

	private static MetaData GetSaveMetaData(CampaignSaveMetaDataArgs data)
	{
		MetaData metaData = new MetaData();
		List<ModuleInfo> moduleInfos = ModuleHelper.GetModuleInfos(data.ModuleNames);
		metaData["Modules"] = string.Join(ModuleCodeSeperator.ToString(), moduleInfos.Select((ModuleInfo q) => q.Id));
		foreach (ModuleInfo item in moduleInfos)
		{
			metaData["Module_" + item.Id] = item.Version.ToString();
		}
		metaData.Add("ApplicationVersion", CurrentVersion.ToString());
		metaData.Add("CreationTime", DateTime.Now.Ticks.ToString());
		KeyValuePair<string, string>[] otherData = data.OtherData;
		for (int num = 0; num < otherData.Length; num++)
		{
			KeyValuePair<string, string> keyValuePair = otherData[num];
			metaData.Add(keyValuePair.Key, keyValuePair.Value);
		}
		return metaData;
	}

	public static int GetMaxNumberOfSaves()
	{
		return int.MaxValue;
	}

	public static bool IsMaxNumberOfSavesReached()
	{
		return NumberOfCurrentSaves >= GetMaxNumberOfSaves();
	}
}
