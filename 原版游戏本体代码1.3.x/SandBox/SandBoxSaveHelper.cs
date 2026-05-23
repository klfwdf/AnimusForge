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

namespace SandBox
{
	// Token: 0x02000026 RID: 38
	public static class SandBoxSaveHelper
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600011E RID: 286 RVA: 0x000075B0 File Offset: 0x000057B0
		// (remove) Token: 0x0600011F RID: 287 RVA: 0x000075E4 File Offset: 0x000057E4
		public static event Action<SandBoxSaveHelper.SaveHelperState> OnStateChange;

		// Token: 0x06000120 RID: 288 RVA: 0x00007618 File Offset: 0x00005818
		public static void TryLoadSave(SaveGameFileInfo saveInfo, Action<LoadResult> onStartGame, Action onCancel = null)
		{
			GameTexts.SetVariable("newline", "\n");
			Action<SandBoxSaveHelper.SaveHelperState> onStateChange = SandBoxSaveHelper.OnStateChange;
			if (onStateChange != null)
			{
				onStateChange(SandBoxSaveHelper.SaveHelperState.Start);
			}
			ApplicationVersion saveVersion = saveInfo.MetaData.GetApplicationVersion();
			bool flag = true;
			MBList<string> officialModules = ModuleHelper.GetOfficialModuleIds();
			MBList<SandBoxSaveHelper.ModuleCheckResult> mblist = (from x in SandBoxSaveHelper.CheckMetaDataCompatibilityErrors(saveInfo.MetaData)
				where !officialModules.Any(delegate(string y)
				{
					if (y == x.ModuleId)
					{
						return true;
					}
					if (saveVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)))
					{
						ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(y);
						return ((moduleInfo != null) ? moduleInfo.Name : null) == x.ModuleId;
					}
					return false;
				})
				select x).ToMBList<SandBoxSaveHelper.ModuleCheckResult>();
			if (mblist.Count <= 0)
			{
				SandBoxSaveHelper.LoadGameAction(saveInfo, onStartGame, onCancel);
				return;
			}
			IEnumerable<IGrouping<ModuleCheckResultType, SandBoxSaveHelper.ModuleCheckResult>> enumerable = from m in mblist
				group m by m.Type;
			GameTextManager globalTextManager = Module.CurrentModule.GlobalTextManager;
			List<TextObject> list = new List<TextObject>();
			foreach (IGrouping<ModuleCheckResultType, SandBoxSaveHelper.ModuleCheckResult> grouping in enumerable)
			{
				TextObject textObject = new TextObject("{=!}{ERROR}{newline}- {MODULES}", null);
				textObject.SetTextVariable("ERROR", globalTextManager.FindText("str_load_module_error", grouping.Key.ToString()));
				textObject.SetTextVariable("MODULES", string.Join("\n- ", from x in grouping
					select x.ModuleId));
				list.Add(textObject);
			}
			TextObject textObject2 = GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}{newline}", null), null);
			TextObject textObject3 = TextObject.GetEmpty();
			bool flag2 = MBSaveLoad.CurrentVersion >= saveVersion || flag;
			if (flag2)
			{
				textObject3 = new TextObject("{=!}{STR1}{newline}{newline}{STR2}", null);
				textObject3.SetTextVariable("STR1", textObject2);
				textObject3.SetTextVariable("STR2", new TextObject("{=lh0so0uX}Do you want to load the saved game with different modules?", null));
			}
			else
			{
				textObject3 = textObject2;
			}
			InquiryData data = new InquiryData(SandBoxSaveHelper._moduleMismatchInquiryTitle.ToString(), textObject3.ToString(), flag2, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(), delegate()
			{
				SandBoxSaveHelper._isInquiryActive = false;
				SandBoxSaveHelper.LoadGameAction(saveInfo, onStartGame, onCancel);
			}, delegate()
			{
				SandBoxSaveHelper._isInquiryActive = false;
				Action onCancel2 = onCancel;
				if (onCancel2 == null)
				{
					return;
				}
				onCancel2();
			}, "", 0f, null, null, null);
			SandBoxSaveHelper._isInquiryActive = true;
			InformationManager.ShowInquiry(data, false, false);
			Action<SandBoxSaveHelper.SaveHelperState> onStateChange2 = SandBoxSaveHelper.OnStateChange;
			if (onStateChange2 == null)
			{
				return;
			}
			onStateChange2(SandBoxSaveHelper.SaveHelperState.Inquiry);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x000078B0 File Offset: 0x00005AB0
		public static MBReadOnlyList<SandBoxSaveHelper.ModuleCheckResult> CheckMetaDataCompatibilityErrors(MetaData fileMetaData)
		{
			List<ModuleInfo> modules = ModuleHelper.GetModules(null);
			string[] modules2 = fileMetaData.GetModules();
			ApplicationVersion applicationVersion = fileMetaData.GetApplicationVersion();
			MBList<SandBoxSaveHelper.ModuleCheckResult> mblist = new MBList<SandBoxSaveHelper.ModuleCheckResult>();
			foreach (string text in modules2)
			{
				bool flag = true;
				bool flag2 = false;
				foreach (ModuleInfo moduleInfo in modules)
				{
					if (moduleInfo.Id == text || (applicationVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)) && moduleInfo.Name == text))
					{
						flag = false;
						flag2 = fileMetaData.GetModuleVersion(text).IsSame(moduleInfo.Version, false);
						break;
					}
				}
				if (flag)
				{
					mblist.Add(new SandBoxSaveHelper.ModuleCheckResult(text, ModuleCheckResultType.ModuleRemovedFromGame));
				}
				else if (!flag2)
				{
					mblist.Add(new SandBoxSaveHelper.ModuleCheckResult(text, ModuleCheckResultType.VersionMismatch));
				}
			}
			foreach (ModuleInfo moduleInfo2 in modules)
			{
				bool flag3 = true;
				foreach (string b in modules2)
				{
					if (moduleInfo2.Id == b || (applicationVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)) && moduleInfo2.Name == b))
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					mblist.Add(new SandBoxSaveHelper.ModuleCheckResult(moduleInfo2.Id, ModuleCheckResultType.ModuleAddedToGame));
				}
			}
			return mblist;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00007A64 File Offset: 0x00005C64
		public static bool GetIsDisabledWithReason(SaveGameFileInfo saveGameFileInfo, out TextObject reason)
		{
			ApplicationVersion applicationVersion = saveGameFileInfo.MetaData.GetApplicationVersion();
			ApplicationVersion applicationVersionWithBuildNumber = Utilities.GetApplicationVersionWithBuildNumber();
			reason = TextObject.GetEmpty();
			if (saveGameFileInfo.IsCorrupted)
			{
				reason = new TextObject("{=t6W3UjG0}Save game file appear to be corrupted. Try starting a new campaign or load another one from Saved Games menu.", null);
				return true;
			}
			foreach (SandBoxSaveHelper.ModuleCheckResult moduleCheckResult in SandBoxSaveHelper.CheckMetaDataCompatibilityErrors(saveGameFileInfo.MetaData))
			{
				if (moduleCheckResult.Type == ModuleCheckResultType.ModuleRemovedFromGame)
				{
					if (ModuleHelper.ModulesDisablingLoadingAfterBeingRemoved.Contains(moduleCheckResult.ModuleId))
					{
						reason = new TextObject("{=ZidO5gkC}This save file was created with the {MODULE_NAME} module, which has been removed from the game. You need to activate the module to load this save.", null);
						reason.SetTextVariable("MODULE_NAME", SandBoxSaveHelper.GetModuleNameFromModuleId(moduleCheckResult.ModuleId));
						return true;
					}
				}
				else if (moduleCheckResult.Type == ModuleCheckResultType.ModuleAddedToGame && ModuleHelper.ModulesDisablingLoadingAfterBeingAdded.Contains(moduleCheckResult.ModuleId))
				{
					reason = new TextObject("{=hi6L6kTM}This save file was created without the {MODULE_NAME} module, which has been added to the game. You need to deactivate the module to load this save.", null);
					reason.SetTextVariable("MODULE_NAME", SandBoxSaveHelper.GetModuleNameFromModuleId(moduleCheckResult.ModuleId));
					return true;
				}
			}
			if (applicationVersion < SandBoxSaveHelper.SaveResetVersion)
			{
				reason = SandBoxSaveHelper._saveResetVersionProblemText;
				return true;
			}
			if (applicationVersionWithBuildNumber < applicationVersion)
			{
				reason = new TextObject("{=9svpUWeo}Save version ({SAVE_VERSION}) is newer than the current version ({CURRENT_VERSION}).", null);
				reason.SetTextVariable("SAVE_VERSION", applicationVersion.ToString());
				reason.SetTextVariable("CURRENT_VERSION", applicationVersionWithBuildNumber.ToString());
				return true;
			}
			return false;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x00007BE0 File Offset: 0x00005DE0
		public static string GetModuleNameFromModuleId(string id)
		{
			if (id == "NavalDLC")
			{
				return new TextObject("{=!}War Sails", null).ToString();
			}
			return id;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x00007C04 File Offset: 0x00005E04
		private static void LoadGameAction(SaveGameFileInfo saveInfo, Action<LoadResult> onStartGame, Action onCancel)
		{
			Action<SandBoxSaveHelper.SaveHelperState> onStateChange = SandBoxSaveHelper.OnStateChange;
			if (onStateChange != null)
			{
				onStateChange(SandBoxSaveHelper.SaveHelperState.LoadGame);
			}
			LoadResult loadResult = MBSaveLoad.LoadSaveGameData(saveInfo.Name);
			if (loadResult != null)
			{
				if (onStartGame != null)
				{
					onStartGame(loadResult);
					return;
				}
			}
			else
			{
				InquiryData data = new InquiryData(SandBoxSaveHelper._errorTitle.ToString(), SandBoxSaveHelper._saveLoadingProblemText.ToString(), true, false, new TextObject("{=yS7PvrTD}OK", null).ToString(), string.Empty, delegate()
				{
					SandBoxSaveHelper._isInquiryActive = false;
					Action onCancel2 = onCancel;
					if (onCancel2 == null)
					{
						return;
					}
					onCancel2();
				}, null, "", 0f, null, null, null);
				SandBoxSaveHelper._isInquiryActive = true;
				InformationManager.ShowInquiry(data, false, false);
				Action<SandBoxSaveHelper.SaveHelperState> onStateChange2 = SandBoxSaveHelper.OnStateChange;
				if (onStateChange2 == null)
				{
					return;
				}
				onStateChange2(SandBoxSaveHelper.SaveHelperState.Inquiry);
			}
		}

		// Token: 0x04000056 RID: 86
		private static readonly ApplicationVersion SaveResetVersion = new ApplicationVersion(ApplicationVersionType.EarlyAccess, 1, 7, 0, 0);

		// Token: 0x04000057 RID: 87
		private static readonly TextObject _moduleMismatchInquiryTitle = new TextObject("{=r7xdYj4q}Module Mismatch", null);

		// Token: 0x04000058 RID: 88
		private static readonly TextObject _errorTitle = new TextObject("{=oZrVNUOk}Error", null);

		// Token: 0x04000059 RID: 89
		private static readonly TextObject _saveLoadingProblemText = new TextObject("{=onLDP7mP}A problem occured while trying to load the saved game.", null);

		// Token: 0x0400005A RID: 90
		private static readonly TextObject _saveResetVersionProblemText = new TextObject("{=5hbSkbQg}This save file is from a game version that is older than e1.7.0. Please switch your game version to e1.7.0, load the save file and save the game. This will allow it to work on newer versions beyond e1.7.0.", null);

		// Token: 0x0400005B RID: 91
		private static bool _isInquiryActive;

		// Token: 0x0200012E RID: 302
		public enum SaveHelperState
		{
			// Token: 0x0400061F RID: 1567
			Start,
			// Token: 0x04000620 RID: 1568
			Inquiry,
			// Token: 0x04000621 RID: 1569
			LoadGame
		}

		// Token: 0x0200012F RID: 303
		public readonly struct ModuleCheckResult
		{
			// Token: 0x06000DAC RID: 3500 RVA: 0x000638F1 File Offset: 0x00061AF1
			public ModuleCheckResult(string moduleId, ModuleCheckResultType type)
			{
				this.ModuleId = moduleId;
				this.Type = type;
			}

			// Token: 0x04000622 RID: 1570
			public readonly string ModuleId;

			// Token: 0x04000623 RID: 1571
			public readonly ModuleCheckResultType Type;
		}
	}
}
