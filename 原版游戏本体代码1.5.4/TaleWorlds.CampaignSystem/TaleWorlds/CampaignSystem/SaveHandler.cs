using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem;

public class SaveHandler
{
	private readonly struct SaveArgs
	{
		public enum SaveMode
		{
			SaveAs,
			QuickSave,
			AutoSave
		}

		public readonly SaveMode Mode;

		public readonly string Name;

		public SaveArgs(SaveMode mode, string name)
		{
			Mode = mode;
			Name = name;
		}
	}

	private enum SaveSteps
	{
		PreSave = 0,
		Saving = 2,
		AwaitingCompletion = 3
	}

	private SaveSteps _saveStep;

	private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

	private Queue<SaveArgs> SaveArgsQueue = new Queue<SaveArgs>();

	private DateTime _lastAutoSaveTime = DateTime.Now;

	public IMainHeroVisualSupplier MainHeroVisualSupplier { get; set; }

	public bool IsSaving => !SaveArgsQueue.IsEmpty();

	public string IronmanModSaveName => "Ironman" + Campaign.Current.UniqueGameId;

	private bool _isAutoSaveEnabled => AutoSaveInterval > -1;

	private double _autoSavePriorityTimeLimit => (double)AutoSaveInterval * 0.75;

	public int AutoSaveInterval => Campaign.Current.SandBoxManager.SandBoxSaveManager?.GetAutoSaveInterval() ?? 15;

	public void QuickSaveCurrentGame()
	{
		SetSaveArgs(SaveArgs.SaveMode.QuickSave);
	}

	public void SaveAs(string saveName)
	{
		SetSaveArgs(SaveArgs.SaveMode.SaveAs, saveName);
	}

	private void TryAutoSave(bool isPriority)
	{
		if (_isAutoSaveEnabled && GameStateManager.Current.ActiveState is MapState { MapConversationActive: false })
		{
			double totalMinutes = (DateTime.Now - _lastAutoSaveTime).TotalMinutes;
			double num = (isPriority ? _autoSavePriorityTimeLimit : ((double)AutoSaveInterval));
			if (totalMinutes > num)
			{
				SetSaveArgs(SaveArgs.SaveMode.AutoSave);
			}
		}
	}

	public void CampaignTick()
	{
		if (Campaign.Current.TimeControlMode != CampaignTimeControlMode.Stop)
		{
			TryAutoSave(isPriority: false);
		}
	}

	internal void SaveTick()
	{
		if (SaveArgsQueue.IsEmpty())
		{
			return;
		}
		switch (_saveStep)
		{
		case SaveSteps.PreSave:
			_saveStep++;
			OnSaveStarted();
			break;
		case SaveSteps.Saving:
		{
			_saveStep++;
			CampaignEventDispatcher.Instance.OnBeforeSave();
			if (CampaignOptions.IsIronmanMode)
			{
				MBSaveLoad.SaveAsCurrentGame(GetSaveMetaData(), IronmanModSaveName, OnSaveCompleted);
				break;
			}
			SaveArgs saveArgs = SaveArgsQueue.Peek();
			switch (saveArgs.Mode)
			{
			case SaveArgs.SaveMode.SaveAs:
				MBSaveLoad.SaveAsCurrentGame(GetSaveMetaData(), saveArgs.Name, OnSaveCompleted);
				break;
			case SaveArgs.SaveMode.QuickSave:
				MBSaveLoad.QuickSaveCurrentGame(GetSaveMetaData(), OnSaveCompleted);
				break;
			case SaveArgs.SaveMode.AutoSave:
				MBSaveLoad.AutoSaveCurrentGame(GetSaveMetaData(), OnSaveCompleted);
				break;
			}
			break;
		}
		default:
			_saveStep++;
			break;
		case SaveSteps.AwaitingCompletion:
			break;
		}
	}

	private void OnSaveCompleted((SaveResult, string) result)
	{
		_saveStep = SaveSteps.PreSave;
		if (SaveArgsQueue.Dequeue().Mode == SaveArgs.SaveMode.AutoSave)
		{
			_lastAutoSaveTime = DateTime.Now;
		}
		OnSaveEnded(result.Item1 == SaveResult.Success, result.Item2);
	}

	public void SignalAutoSave()
	{
		TryAutoSave(isPriority: true);
	}

	private void OnSaveStarted()
	{
		Campaign.Current.WaitAsyncTasks();
		CampaignEventDispatcher.Instance.OnSaveStarted();
		MBInformationManager.HideInformations();
	}

	private void OnSaveEnded(bool isSaveSuccessful, string newSaveGameName)
	{
		Campaign.Current.SandBoxManager.SandBoxSaveManager?.OnSaveOver(isSaveSuccessful, newSaveGameName);
		CampaignEventDispatcher.Instance.OnSaveOver(isSaveSuccessful, newSaveGameName);
		if (!isSaveSuccessful)
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=u9PPxTNL}Save Error!"));
		}
	}

	private void SetSaveArgs(SaveArgs.SaveMode saveType, string saveName = null)
	{
		SaveArgsQueue.Enqueue(new SaveArgs(saveType, saveName));
	}

	public void ForceAutoSave()
	{
		if (!Campaign.Current.SandBoxManager.SandBoxSaveManager.IsAutoSaveDisabled())
		{
			SetSaveArgs(SaveArgs.SaveMode.AutoSave);
		}
	}

	public CampaignSaveMetaDataArgs GetSaveMetaData()
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>("UniqueGameId", Campaign.Current.UniqueGameId ?? ""),
			new KeyValuePair<string, string>("MainHeroLevel", Hero.MainHero.Level.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainPartyFood", Campaign.Current.MainParty.Food.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainHeroGold", Hero.MainHero.Gold.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("ClanInfluence", Clan.PlayerClan.Influence.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("ClanFiefs", Clan.PlayerClan.Settlements.Count.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainPartyShipCount", Campaign.Current.MainParty.Ships.Count.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainPartyHealthyMemberCount", Campaign.Current.MainParty.MemberRoster.TotalHealthyCount.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainPartyPrisonerMemberCount", Campaign.Current.MainParty.PrisonRoster.TotalManCount.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("MainPartyWoundedMemberCount", Campaign.Current.MainParty.MemberRoster.TotalWounded.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("CharacterName", Hero.MainHero.Name?.ToString()),
			new KeyValuePair<string, string>("DayLong", Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow.ToString(_invariantCulture)),
			new KeyValuePair<string, string>("ClanBannerCode", Clan.PlayerClan.Banner.Serialize()),
			new KeyValuePair<string, string>("MainHeroVisual", MainHeroVisualSupplier?.GetMainHeroVisualCode() ?? string.Empty),
			new KeyValuePair<string, string>("IronmanMode", (CampaignOptions.IsIronmanMode ? 1 : 0).ToString()),
			new KeyValuePair<string, string>("HealthPercentage", MBMath.ClampInt(Hero.MainHero.HitPoints * 100 / Hero.MainHero.MaxHitPoints, 1, 100).ToString()),
			new KeyValuePair<string, string>("NewGameVersion", string.IsNullOrEmpty(Campaign.Current.NewGameVersion) ? string.Empty : Campaign.Current.NewGameVersion)
		};
		CampaignEventDispatcher.Instance.CollectMetadataEntries(list);
		return new CampaignSaveMetaDataArgs((from x in ModuleHelper.GetActiveModules()
			select x.Id).ToArray(), list.ToArray());
	}
}
