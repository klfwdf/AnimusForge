using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D4 RID: 212
	public class DumpIntegrityCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060009D1 RID: 2513 RVA: 0x0004A638 File Offset: 0x00048838
		public override void SyncData(IDataStore dataStore)
		{
			TextObject textObject;
			DumpIntegrityCampaignBehavior.IsGameIntegrityAchieved(out textObject);
			this.UpdateDumpInfo();
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x0004A653 File Offset: 0x00048853
		public override void RegisterEvents()
		{
			CampaignEvents.OnConfigChangedEvent.AddNonSerializedListener(this, new Action(this.OnConfigChanged));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x0004A684 File Offset: 0x00048884
		private void OnConfigChanged()
		{
			TextObject textObject;
			DumpIntegrityCampaignBehavior.IsGameIntegrityAchieved(out textObject);
			this.UpdateDumpInfo();
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x0004A6A0 File Offset: 0x000488A0
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter)
		{
			TextObject textObject;
			DumpIntegrityCampaignBehavior.IsGameIntegrityAchieved(out textObject);
			this.UpdateDumpInfo();
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x0004A6BC File Offset: 0x000488BC
		private void UpdateDumpInfo()
		{
			this._saveIntegrityDumpInfo.Clear();
			this._usedModulesDumpInfo.Clear();
			this._usedVersionsDumpInfo.Clear();
			if (Campaign.Current.NewGameVersion != null)
			{
				this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("New Game Version", Campaign.Current.NewGameVersion));
			}
			this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("Has Used Cheats", (!DumpIntegrityCampaignBehavior.CheckCheatUsage()).ToString()));
			Campaign campaign = Campaign.Current;
			if (((campaign != null) ? campaign.PreviouslyUsedModules : null) != null && Campaign.Current.UsedGameVersions != null)
			{
				string value;
				if (DumpIntegrityCampaignBehavior.CheckIfModulesAreDefault(out value))
				{
					this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("Has Installed Unofficial Modules", "False"));
				}
				else
				{
					this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("Has Installed Unofficial Modules", value));
				}
				string value2;
				DumpIntegrityCampaignBehavior.CheckIfVersionIntegrityIsAchieved(out value2);
				this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("Has Reverted to Older Versions", value2));
				TextObject textObject;
				this._saveIntegrityDumpInfo.Add(new KeyValuePair<string, string>("Game Integrity is Achieved", DumpIntegrityCampaignBehavior.IsGameIntegrityAchieved(out textObject).ToString()));
			}
			Campaign campaign2 = Campaign.Current;
			if (((campaign2 != null) ? campaign2.PreviouslyUsedModules : null) != null && Campaign.Current.PreviouslyUsedModules.Count > 0)
			{
				foreach (string text in Campaign.Current.PreviouslyUsedModules.Last<string>().Split(new char[] { MBSaveLoad.ModuleCodeSeperator }))
				{
					string text2 = text.Split(new char[] { MBSaveLoad.ModuleVersionSeperator })[0];
					string str = text.Split(new char[] { MBSaveLoad.ModuleVersionSeperator })[1];
					ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(text2);
					string text3 = "Does not exist";
					if (moduleInfo != null)
					{
						text3 = "Exists But Inactive";
						if (moduleInfo.IsActive)
						{
							text3 = "Exists and Active";
						}
					}
					text3 = text3 + " (Version: " + str + ")";
					this._usedModulesDumpInfo.Add(new KeyValuePair<string, string>(text2, text3));
				}
			}
			if (Campaign.Current.UsedGameVersions != null)
			{
				foreach (string text4 in Campaign.Current.UsedGameVersions)
				{
					string value3 = ((text4 == MBSaveLoad.CurrentVersion.ToString()) ? "1" : "0");
					this._usedVersionsDumpInfo.Add(new KeyValuePair<string, string>(text4, value3));
				}
			}
			this.SendDataToWatchdog();
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x0004A964 File Offset: 0x00048B64
		private void SendDataToWatchdog()
		{
			foreach (KeyValuePair<string, string> keyValuePair in this._saveIntegrityDumpInfo)
			{
				Utilities.SetWatchdogValue("crash_tags.txt", "Campaign Dump Integrity", keyValuePair.Key, keyValuePair.Value);
			}
			foreach (KeyValuePair<string, string> keyValuePair2 in this._usedModulesDumpInfo)
			{
				Utilities.SetWatchdogValue("crash_tags.txt", "Used Modules", keyValuePair2.Key, keyValuePair2.Value);
			}
			foreach (KeyValuePair<string, string> keyValuePair3 in this._usedVersionsDumpInfo)
			{
				Utilities.SetWatchdogValue("crash_tags.txt", "Used Versions", keyValuePair3.Key, keyValuePair3.Value);
			}
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x0004AA80 File Offset: 0x00048C80
		public static bool IsGameIntegrityAchieved(out TextObject reason)
		{
			bool result = true;
			string text;
			if (!DumpIntegrityCampaignBehavior.CheckCheatUsage())
			{
				reason = new TextObject("{=sO8Zh3ZH}Achievements are disabled due to cheat usage.", null);
				result = false;
			}
			else if (!DumpIntegrityCampaignBehavior.CheckIfModulesAreDefault(out text))
			{
				reason = new TextObject("{=R0AbAxqX}Achievements are disabled due to unofficial modules.", null);
				result = false;
			}
			else if (!DumpIntegrityCampaignBehavior.CheckIfVersionIntegrityIsAchieved(out text))
			{
				reason = new TextObject("{=dt00CQCM}Achievements are disabled due to version downgrade.", null);
				result = false;
			}
			else
			{
				reason = null;
			}
			return result;
		}

		// Token: 0x060009D8 RID: 2520 RVA: 0x0004AAE0 File Offset: 0x00048CE0
		private static bool CheckIfVersionIntegrityIsAchieved(out string message)
		{
			message = "False";
			MBReadOnlyList<string> usedGameVersions = Campaign.Current.UsedGameVersions;
			for (int i = 0; i < usedGameVersions.Count; i++)
			{
				if (i < usedGameVersions.Count - 1 && ApplicationVersion.FromString(usedGameVersions[i], 0).IsNewerThan(ApplicationVersion.FromString(usedGameVersions[i + 1], 0)))
				{
					message = "Version downgrade from " + usedGameVersions[i + 1] + " to " + usedGameVersions[i];
					Debug.Print("Dump integrity is compromised due to version downgrade from " + usedGameVersions[i + 1] + " to " + usedGameVersions[i], 0, Debug.DebugColor.DarkRed, 17592186044416UL);
					return false;
				}
			}
			return true;
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x0004AB9C File Offset: 0x00048D9C
		private static bool CheckIfModulesAreDefault(out string unofficialModulesCode)
		{
			MBList<string> officialModuleIds = ModuleHelper.GetOfficialModuleIds();
			MBList<string> mblist = new MBList<string>();
			foreach (string text in Campaign.Current.PreviouslyUsedModules)
			{
				string[] array = text.Split(new char[] { MBSaveLoad.ModuleCodeSeperator });
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i];
					string moduleId = text2.Split(new char[] { MBSaveLoad.ModuleVersionSeperator })[0];
					if (!officialModuleIds.Any((string x) => moduleId.Equals(x, StringComparison.InvariantCultureIgnoreCase)) && !mblist.Contains(moduleId))
					{
						mblist.Add(moduleId);
					}
				}
			}
			unofficialModulesCode = string.Join(MBSaveLoad.ModuleCodeSeperator.ToString(), mblist);
			if (mblist.Count > 0)
			{
				Debug.Print("Unofficial modules are used: " + unofficialModulesCode, 0, Debug.DebugColor.DarkRed, 17592186044416UL);
			}
			return mblist.Count == 0;
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x0004ACC4 File Offset: 0x00048EC4
		private static bool CheckCheatUsage()
		{
			if (!Campaign.Current.EnabledCheatsBefore && Game.Current.CheatMode)
			{
				Campaign.Current.EnabledCheatsBefore = Game.Current.CheatMode;
			}
			if (Campaign.Current.EnabledCheatsBefore)
			{
				Debug.Print("Dump integrity is compromised due to cheat usage", 0, Debug.DebugColor.DarkRed, 17592186044416UL);
			}
			return !Campaign.Current.EnabledCheatsBefore;
		}

		// Token: 0x0400047F RID: 1151
		private readonly List<KeyValuePair<string, string>> _saveIntegrityDumpInfo = new List<KeyValuePair<string, string>>();

		// Token: 0x04000480 RID: 1152
		private readonly List<KeyValuePair<string, string>> _usedModulesDumpInfo = new List<KeyValuePair<string, string>>();

		// Token: 0x04000481 RID: 1153
		private readonly List<KeyValuePair<string, string>> _usedVersionsDumpInfo = new List<KeyValuePair<string, string>>();
	}
}
