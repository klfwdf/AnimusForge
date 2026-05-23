using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000A7 RID: 167
	public class SaveHandler
	{
		// Token: 0x17000525 RID: 1317
		// (get) Token: 0x06001345 RID: 4933 RVA: 0x000598A1 File Offset: 0x00057AA1
		// (set) Token: 0x06001346 RID: 4934 RVA: 0x000598A9 File Offset: 0x00057AA9
		public IMainHeroVisualSupplier MainHeroVisualSupplier { get; set; }

		// Token: 0x17000526 RID: 1318
		// (get) Token: 0x06001347 RID: 4935 RVA: 0x000598B2 File Offset: 0x00057AB2
		public bool IsSaving
		{
			get
			{
				return !this.SaveArgsQueue.IsEmpty<SaveHandler.SaveArgs>();
			}
		}

		// Token: 0x17000527 RID: 1319
		// (get) Token: 0x06001348 RID: 4936 RVA: 0x000598C2 File Offset: 0x00057AC2
		public string IronmanModSaveName
		{
			get
			{
				return "Ironman" + Campaign.Current.UniqueGameId;
			}
		}

		// Token: 0x17000528 RID: 1320
		// (get) Token: 0x06001349 RID: 4937 RVA: 0x000598D8 File Offset: 0x00057AD8
		private bool _isAutoSaveEnabled
		{
			get
			{
				return this.AutoSaveInterval > -1;
			}
		}

		// Token: 0x17000529 RID: 1321
		// (get) Token: 0x0600134A RID: 4938 RVA: 0x000598E3 File Offset: 0x00057AE3
		private double _autoSavePriorityTimeLimit
		{
			get
			{
				return (double)this.AutoSaveInterval * 0.75;
			}
		}

		// Token: 0x1700052A RID: 1322
		// (get) Token: 0x0600134B RID: 4939 RVA: 0x000598F6 File Offset: 0x00057AF6
		public int AutoSaveInterval
		{
			get
			{
				ISaveManager sandBoxSaveManager = Campaign.Current.SandBoxManager.SandBoxSaveManager;
				if (sandBoxSaveManager == null)
				{
					return 15;
				}
				return sandBoxSaveManager.GetAutoSaveInterval();
			}
		}

		// Token: 0x0600134C RID: 4940 RVA: 0x00059913 File Offset: 0x00057B13
		public void QuickSaveCurrentGame()
		{
			this.SetSaveArgs(SaveHandler.SaveArgs.SaveMode.QuickSave, null);
		}

		// Token: 0x0600134D RID: 4941 RVA: 0x0005991D File Offset: 0x00057B1D
		public void SaveAs(string saveName)
		{
			this.SetSaveArgs(SaveHandler.SaveArgs.SaveMode.SaveAs, saveName);
		}

		// Token: 0x0600134E RID: 4942 RVA: 0x00059928 File Offset: 0x00057B28
		private void TryAutoSave(bool isPriority)
		{
			MapState mapState;
			if (this._isAutoSaveEnabled && (mapState = GameStateManager.Current.ActiveState as MapState) != null && !mapState.MapConversationActive)
			{
				double totalMinutes = (DateTime.Now - this._lastAutoSaveTime).TotalMinutes;
				double num = (isPriority ? this._autoSavePriorityTimeLimit : ((double)this.AutoSaveInterval));
				if (totalMinutes > num)
				{
					this.SetSaveArgs(SaveHandler.SaveArgs.SaveMode.AutoSave, null);
				}
			}
		}

		// Token: 0x0600134F RID: 4943 RVA: 0x0005998E File Offset: 0x00057B8E
		public void CampaignTick()
		{
			if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.Stop)
			{
				this.TryAutoSave(false);
			}
		}

		// Token: 0x06001350 RID: 4944 RVA: 0x000599A4 File Offset: 0x00057BA4
		internal void SaveTick()
		{
			if (!this.SaveArgsQueue.IsEmpty<SaveHandler.SaveArgs>())
			{
				switch (this._saveStep)
				{
				case SaveHandler.SaveSteps.PreSave:
					this._saveStep++;
					this.OnSaveStarted();
					return;
				case SaveHandler.SaveSteps.Saving:
				{
					this._saveStep++;
					CampaignEventDispatcher.Instance.OnBeforeSave();
					if (CampaignOptions.IsIronmanMode)
					{
						MBSaveLoad.SaveAsCurrentGame(this.GetSaveMetaData(), this.IronmanModSaveName, new Action<ValueTuple<SaveResult, string>>(this.OnSaveCompleted));
						return;
					}
					SaveHandler.SaveArgs saveArgs = this.SaveArgsQueue.Peek();
					switch (saveArgs.Mode)
					{
					case SaveHandler.SaveArgs.SaveMode.SaveAs:
						MBSaveLoad.SaveAsCurrentGame(this.GetSaveMetaData(), saveArgs.Name, new Action<ValueTuple<SaveResult, string>>(this.OnSaveCompleted));
						return;
					case SaveHandler.SaveArgs.SaveMode.QuickSave:
						MBSaveLoad.QuickSaveCurrentGame(this.GetSaveMetaData(), new Action<ValueTuple<SaveResult, string>>(this.OnSaveCompleted));
						return;
					case SaveHandler.SaveArgs.SaveMode.AutoSave:
						MBSaveLoad.AutoSaveCurrentGame(this.GetSaveMetaData(), new Action<ValueTuple<SaveResult, string>>(this.OnSaveCompleted));
						return;
					default:
						return;
					}
					break;
				}
				case SaveHandler.SaveSteps.AwaitingCompletion:
					return;
				}
				this._saveStep++;
			}
		}

		// Token: 0x06001351 RID: 4945 RVA: 0x00059AB7 File Offset: 0x00057CB7
		private void OnSaveCompleted(ValueTuple<SaveResult, string> result)
		{
			this._saveStep = SaveHandler.SaveSteps.PreSave;
			if (this.SaveArgsQueue.Dequeue().Mode == SaveHandler.SaveArgs.SaveMode.AutoSave)
			{
				this._lastAutoSaveTime = DateTime.Now;
			}
			this.OnSaveEnded(result.Item1 == SaveResult.Success, result.Item2);
		}

		// Token: 0x06001352 RID: 4946 RVA: 0x00059AF3 File Offset: 0x00057CF3
		public void SignalAutoSave()
		{
			this.TryAutoSave(true);
		}

		// Token: 0x06001353 RID: 4947 RVA: 0x00059AFC File Offset: 0x00057CFC
		private void OnSaveStarted()
		{
			Campaign.Current.WaitAsyncTasks();
			CampaignEventDispatcher.Instance.OnSaveStarted();
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001354 RID: 4948 RVA: 0x00059B18 File Offset: 0x00057D18
		private void OnSaveEnded(bool isSaveSuccessful, string newSaveGameName)
		{
			ISaveManager sandBoxSaveManager = Campaign.Current.SandBoxManager.SandBoxSaveManager;
			if (sandBoxSaveManager != null)
			{
				sandBoxSaveManager.OnSaveOver(isSaveSuccessful, newSaveGameName);
			}
			CampaignEventDispatcher.Instance.OnSaveOver(isSaveSuccessful, newSaveGameName);
			if (!isSaveSuccessful)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=u9PPxTNL}Save Error!", null), 0, null, null, "");
			}
		}

		// Token: 0x06001355 RID: 4949 RVA: 0x00059B68 File Offset: 0x00057D68
		private void SetSaveArgs(SaveHandler.SaveArgs.SaveMode saveType, string saveName = null)
		{
			this.SaveArgsQueue.Enqueue(new SaveHandler.SaveArgs(saveType, saveName));
		}

		// Token: 0x06001356 RID: 4950 RVA: 0x00059B7C File Offset: 0x00057D7C
		public void ForceAutoSave()
		{
			if (!Campaign.Current.SandBoxManager.SandBoxSaveManager.IsAutoSaveDisabled())
			{
				this.SetSaveArgs(SaveHandler.SaveArgs.SaveMode.AutoSave, null);
			}
		}

		// Token: 0x06001357 RID: 4951 RVA: 0x00059B9C File Offset: 0x00057D9C
		public CampaignSaveMetaDataArgs GetSaveMetaData()
		{
			string[] moduleName = (from x in ModuleHelper.GetActiveModules()
				select x.Id).ToArray<string>();
			KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[15];
			array[0] = new KeyValuePair<string, string>("UniqueGameId", Campaign.Current.UniqueGameId ?? "");
			array[1] = new KeyValuePair<string, string>("MainHeroLevel", Hero.MainHero.Level.ToString(SaveHandler._invariantCulture));
			array[2] = new KeyValuePair<string, string>("MainPartyFood", Campaign.Current.MainParty.Food.ToString(SaveHandler._invariantCulture));
			array[3] = new KeyValuePair<string, string>("MainHeroGold", Hero.MainHero.Gold.ToString(SaveHandler._invariantCulture));
			array[4] = new KeyValuePair<string, string>("ClanInfluence", Clan.PlayerClan.Influence.ToString(SaveHandler._invariantCulture));
			array[5] = new KeyValuePair<string, string>("ClanFiefs", Clan.PlayerClan.Settlements.Count.ToString(SaveHandler._invariantCulture));
			array[6] = new KeyValuePair<string, string>("MainPartyHealthyMemberCount", Campaign.Current.MainParty.MemberRoster.TotalHealthyCount.ToString(SaveHandler._invariantCulture));
			array[7] = new KeyValuePair<string, string>("MainPartyPrisonerMemberCount", Campaign.Current.MainParty.PrisonRoster.TotalManCount.ToString(SaveHandler._invariantCulture));
			array[8] = new KeyValuePair<string, string>("MainPartyWoundedMemberCount", Campaign.Current.MainParty.MemberRoster.TotalWounded.ToString(SaveHandler._invariantCulture));
			int num = 9;
			string key = "CharacterName";
			TextObject name = Hero.MainHero.Name;
			array[num] = new KeyValuePair<string, string>(key, (name != null) ? name.ToString() : null);
			array[10] = new KeyValuePair<string, string>("DayLong", Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow.ToString(SaveHandler._invariantCulture));
			array[11] = new KeyValuePair<string, string>("ClanBannerCode", Clan.PlayerClan.Banner.Serialize());
			int num2 = 12;
			string key2 = "MainHeroVisual";
			IMainHeroVisualSupplier mainHeroVisualSupplier = this.MainHeroVisualSupplier;
			array[num2] = new KeyValuePair<string, string>(key2, ((mainHeroVisualSupplier != null) ? mainHeroVisualSupplier.GetMainHeroVisualCode() : null) ?? string.Empty);
			array[13] = new KeyValuePair<string, string>("IronmanMode", (CampaignOptions.IsIronmanMode ? 1 : 0).ToString());
			array[14] = new KeyValuePair<string, string>("HealthPercentage", MBMath.ClampInt(Hero.MainHero.HitPoints * 100 / Hero.MainHero.MaxHitPoints, 1, 100).ToString());
			return new CampaignSaveMetaDataArgs(moduleName, array);
		}

		// Token: 0x04000633 RID: 1587
		private SaveHandler.SaveSteps _saveStep;

		// Token: 0x04000634 RID: 1588
		private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

		// Token: 0x04000636 RID: 1590
		private Queue<SaveHandler.SaveArgs> SaveArgsQueue = new Queue<SaveHandler.SaveArgs>();

		// Token: 0x04000637 RID: 1591
		private DateTime _lastAutoSaveTime = DateTime.Now;

		// Token: 0x02000542 RID: 1346
		private readonly struct SaveArgs
		{
			// Token: 0x06004C6E RID: 19566 RVA: 0x00179B4B File Offset: 0x00177D4B
			public SaveArgs(SaveHandler.SaveArgs.SaveMode mode, string name)
			{
				this.Mode = mode;
				this.Name = name;
			}

			// Token: 0x04001656 RID: 5718
			public readonly SaveHandler.SaveArgs.SaveMode Mode;

			// Token: 0x04001657 RID: 5719
			public readonly string Name;

			// Token: 0x020008A5 RID: 2213
			public enum SaveMode
			{
				// Token: 0x04002481 RID: 9345
				SaveAs,
				// Token: 0x04002482 RID: 9346
				QuickSave,
				// Token: 0x04002483 RID: 9347
				AutoSave
			}
		}

		// Token: 0x02000543 RID: 1347
		private enum SaveSteps
		{
			// Token: 0x04001659 RID: 5721
			PreSave,
			// Token: 0x0400165A RID: 5722
			Saving = 2,
			// Token: 0x0400165B RID: 5723
			AwaitingCompletion
		}
	}
}
