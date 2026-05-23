using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace SandBox;

public static class SandBoxSaveHelper
{
	public enum SaveHelperState
	{
		Start,
		Inquiry,
		LoadGame
	}

	public readonly struct ModuleCheckResult
	{
		public readonly string ModuleId;

		public readonly ModuleCheckResultType Type;

		public ModuleCheckResult(string moduleId, ModuleCheckResultType type)
		{
			ModuleId = moduleId;
			Type = type;
		}
	}

	private static readonly ApplicationVersion SaveResetVersion = new ApplicationVersion(ApplicationVersionType.EarlyAccess, 1, 7, 0, 0);

	private static readonly TextObject _moduleMismatchInquiryTitle = new TextObject("{=r7xdYj4q}Module Mismatch");

	private static readonly TextObject _errorTitle = new TextObject("{=oZrVNUOk}Error");

	private static readonly TextObject _saveLoadingProblemText = new TextObject("{=onLDP7mP}A problem occured while trying to load the saved game.");

	private static readonly TextObject _saveResetVersionProblemText = new TextObject("{=5hbSkbQg}This save file is from a game version that is older than e1.7.0. Please switch your game version to e1.7.0, load the save file and save the game. This will allow it to work on newer versions beyond e1.7.0.");

	private static bool _isInquiryActive;

	public static event Action<SaveHelperState> OnStateChange;

	public static void TryLoadSave(SaveGameFileInfo saveInfo, Action<LoadResult> onStartGame, Action onCancel = null)
	{
		GameTexts.SetVariable("newline", "\n");
		SandBoxSaveHelper.OnStateChange?.Invoke(SaveHelperState.Start);
		ApplicationVersion saveVersion = saveInfo.MetaData.GetApplicationVersion();
		bool flag = true;
		MBList<string> officialModules = ModuleHelper.GetOfficialModuleIds();
		MBList<ModuleCheckResult> mBList = (from x in CheckMetaDataCompatibilityErrors(saveInfo.MetaData)
			where !officialModules.Any((string y) => y == x.ModuleId || (saveVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && ModuleHelper.GetModuleInfo(y)?.Name == x.ModuleId))
			select x).ToMBList();
		if (mBList.Count > 0)
		{
			IEnumerable<IGrouping<ModuleCheckResultType, ModuleCheckResult>> enumerable = from m in mBList
				group m by m.Type;
			GameTextManager globalTextManager = Module.CurrentModule.GlobalTextManager;
			List<TextObject> list = new List<TextObject>();
			foreach (IGrouping<ModuleCheckResultType, ModuleCheckResult> item in enumerable)
			{
				TextObject textObject = new TextObject("{=!}{ERROR}{newline}- {MODULES}");
				textObject.SetTextVariable("ERROR", globalTextManager.FindText("str_load_module_error", item.Key.ToString()));
				textObject.SetTextVariable("MODULES", string.Join("\n- ", item.Select((ModuleCheckResult x) => x.ModuleId)));
				list.Add(textObject);
			}
			TextObject textObject2 = GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}{newline}"));
			TextObject empty = TextObject.GetEmpty();
			bool flag2 = MBSaveLoad.CurrentVersion >= saveVersion || flag;
			if (flag2)
			{
				empty = new TextObject("{=!}{STR1}{newline}{newline}{STR2}");
				empty.SetTextVariable("STR1", textObject2);
				empty.SetTextVariable("STR2", new TextObject("{=lh0so0uX}Do you want to load the saved game with different modules?"));
			}
			else
			{
				empty = textObject2;
			}
			InquiryData data = new InquiryData(_moduleMismatchInquiryTitle.ToString(), empty.ToString(), flag2, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate
			{
				_isInquiryActive = false;
				LoadGameAction(saveInfo, onStartGame, onCancel);
			}, delegate
			{
				_isInquiryActive = false;
				onCancel?.Invoke();
			});
			_isInquiryActive = true;
			InformationManager.ShowInquiry(data);
			SandBoxSaveHelper.OnStateChange?.Invoke(SaveHelperState.Inquiry);
		}
		else
		{
			LoadGameAction(saveInfo, onStartGame, onCancel);
		}
	}

	public static MBReadOnlyList<ModuleCheckResult> CheckMetaDataCompatibilityErrors(MetaData fileMetaData)
	{
		List<ModuleInfo> modules = ModuleHelper.GetModules();
		string[] modules2 = fileMetaData.GetModules();
		ApplicationVersion applicationVersion = fileMetaData.GetApplicationVersion();
		MBList<ModuleCheckResult> mBList = new MBList<ModuleCheckResult>();
		string[] array = modules2;
		foreach (string text in array)
		{
			bool flag = true;
			bool flag2 = false;
			foreach (ModuleInfo item in modules)
			{
				if (item.Id == text || (applicationVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && item.Name == text))
				{
					flag = false;
					flag2 = fileMetaData.GetModuleVersion(text).IsSame(item.Version, checkChangeSet: false);
					break;
				}
			}
			if (flag)
			{
				mBList.Add(new ModuleCheckResult(text, ModuleCheckResultType.ModuleRemovedFromGame));
			}
			else if (!flag2)
			{
				mBList.Add(new ModuleCheckResult(text, ModuleCheckResultType.VersionMismatch));
			}
		}
		foreach (ModuleInfo item2 in modules)
		{
			bool flag3 = true;
			array = modules2;
			foreach (string text2 in array)
			{
				if (item2.Id == text2 || (applicationVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && item2.Name == text2))
				{
					flag3 = false;
					break;
				}
			}
			if (flag3)
			{
				mBList.Add(new ModuleCheckResult(item2.Id, ModuleCheckResultType.ModuleAddedToGame));
			}
		}
		return mBList;
	}

	public static bool GetIsDisabledWithReason(SaveGameFileInfo saveGameFileInfo, out TextObject reason)
	{
		ApplicationVersion applicationVersion = saveGameFileInfo.MetaData.GetApplicationVersion();
		ApplicationVersion applicationVersionWithBuildNumber = Utilities.GetApplicationVersionWithBuildNumber();
		reason = TextObject.GetEmpty();
		if (saveGameFileInfo.IsCorrupted)
		{
			reason = new TextObject("{=t6W3UjG0}Save game file appear to be corrupted. Try starting a new campaign or load another one from Saved Games menu.");
			return true;
		}
		foreach (ModuleCheckResult item in CheckMetaDataCompatibilityErrors(saveGameFileInfo.MetaData))
		{
			if (item.Type == ModuleCheckResultType.ModuleRemovedFromGame)
			{
				if (ModuleHelper.ModulesDisablingLoadingAfterBeingRemoved.Contains(item.ModuleId))
				{
					reason = new TextObject("{=ZidO5gkC}This save file was created with the {MODULE_NAME} module, which has been removed from the game. You need to activate the module to load this save.");
					reason.SetTextVariable("MODULE_NAME", GetModuleNameFromModuleId(item.ModuleId));
					return true;
				}
			}
			else if (item.Type == ModuleCheckResultType.ModuleAddedToGame && ModuleHelper.ModulesDisablingLoadingAfterBeingAdded.Contains(item.ModuleId))
			{
				reason = new TextObject("{=hi6L6kTM}This save file was created without the {MODULE_NAME} module, which has been added to the game. You need to deactivate the module to load this save.");
				reason.SetTextVariable("MODULE_NAME", GetModuleNameFromModuleId(item.ModuleId));
				return true;
			}
		}
		if (applicationVersion < SaveResetVersion)
		{
			reason = _saveResetVersionProblemText;
			return true;
		}
		if (applicationVersionWithBuildNumber < applicationVersion)
		{
			reason = new TextObject("{=9svpUWeo}Save version ({SAVE_VERSION}) is newer than the current version ({CURRENT_VERSION}).");
			reason.SetTextVariable("SAVE_VERSION", applicationVersion.ToString());
			reason.SetTextVariable("CURRENT_VERSION", applicationVersionWithBuildNumber.ToString());
			return true;
		}
		return false;
	}

	public static string GetModuleNameFromModuleId(string id)
	{
		if (id == "NavalDLC")
		{
			return new TextObject("{=!}War Sails").ToString();
		}
		return id;
	}

	private static void LoadGameAction(SaveGameFileInfo saveInfo, Action<LoadResult> onStartGame, Action onCancel)
	{
		SandBoxSaveHelper.OnStateChange?.Invoke(SaveHelperState.LoadGame);
		LoadResult loadResult = MBSaveLoad.LoadSaveGameData(saveInfo.Name);
		if (loadResult != null)
		{
			onStartGame?.Invoke(loadResult);
			return;
		}
		InquiryData data = new InquiryData(_errorTitle.ToString(), _saveLoadingProblemText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), string.Empty, delegate
		{
			_isInquiryActive = false;
			onCancel?.Invoke();
		}, null);
		_isInquiryActive = true;
		InformationManager.ShowInquiry(data);
		SandBoxSaveHelper.OnStateChange?.Invoke(SaveHelperState.Inquiry);
	}
}
