using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace SandBox.ViewModelCollection.SaveLoad;

public class SavedGameVM : ViewModel
{
	private readonly bool _isSaving;

	private readonly Action _onDone;

	private readonly Action<SavedGameVM> _onDelete;

	private readonly Action<SavedGameVM> _onSelection;

	private readonly Action _onCancelLoadSave;

	private readonly TextObject _newlineTextObject = new TextObject("{=ol0rBSrb}{STR1}{newline}{STR2}");

	private readonly ApplicationVersion _gameVersion;

	private readonly ApplicationVersion _saveVersion;

	private readonly MBReadOnlyList<SandBoxSaveHelper.ModuleCheckResult> _moduleCheckResults;

	private MBBindingList<SavedGamePropertyVM> _savedGameProperties;

	private MBBindingList<SavedGameModuleInfoVM> _loadedModulesInSave;

	private HintViewModel _dateTimeHint;

	private HintViewModel _updateButtonHint;

	private HintViewModel _disabledReasonHint;

	private BannerImageIdentifierVM _clanBanner;

	private CharacterViewModel _characterVisual;

	private string _deleteText;

	private string _nameText;

	private string _gameTimeText;

	private string _realTimeText1;

	private string _realTimeText2;

	private string _levelText;

	private string _characterNameText;

	private string _saveLoadText;

	private string _overwriteSaveText;

	private string _updateSaveText;

	private string _modulesText;

	private string _corruptedSaveText;

	private string _saveVersionAsString;

	private string _mainHeroVisualCode;

	private string _bannerTextCode;

	private bool _isSelected;

	private bool _isCorrupted;

	private bool _isFilteredOut;

	private bool _isDisabled;

	public SaveGameFileInfo Save { get; }

	public bool RequiresInquiryOnLoad { get; private set; }

	public bool IsModuleDiscrepancyDetected { get; private set; }

	[DataSourceProperty]
	public MBBindingList<SavedGamePropertyVM> SavedGameProperties
	{
		get
		{
			return _savedGameProperties;
		}
		set
		{
			if (value != _savedGameProperties)
			{
				_savedGameProperties = value;
				OnPropertyChangedWithValue(value, "SavedGameProperties");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SavedGameModuleInfoVM> LoadedModulesInSave
	{
		get
		{
			return _loadedModulesInSave;
		}
		set
		{
			if (value != _loadedModulesInSave)
			{
				_loadedModulesInSave = value;
				OnPropertyChangedWithValue(value, "LoadedModulesInSave");
			}
		}
	}

	[DataSourceProperty]
	public string SaveVersionAsString
	{
		get
		{
			return _saveVersionAsString;
		}
		set
		{
			if (value != _saveVersionAsString)
			{
				_saveVersionAsString = value;
				OnPropertyChangedWithValue(value, "SaveVersionAsString");
			}
		}
	}

	[DataSourceProperty]
	public string DeleteText
	{
		get
		{
			return _deleteText;
		}
		set
		{
			if (value != _deleteText)
			{
				_deleteText = value;
				OnPropertyChangedWithValue(value, "DeleteText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCorrupted
	{
		get
		{
			return _isCorrupted;
		}
		set
		{
			if (value != _isCorrupted)
			{
				_isCorrupted = value;
				OnPropertyChangedWithValue(value, "IsCorrupted");
			}
		}
	}

	[DataSourceProperty]
	public string BannerTextCode
	{
		get
		{
			return _bannerTextCode;
		}
		set
		{
			if (value != _bannerTextCode)
			{
				_bannerTextCode = value;
				OnPropertyChangedWithValue(value, "BannerTextCode");
			}
		}
	}

	[DataSourceProperty]
	public string SaveLoadText
	{
		get
		{
			return _saveLoadText;
		}
		set
		{
			if (value != _saveLoadText)
			{
				_saveLoadText = value;
				OnPropertyChangedWithValue(value, "SaveLoadText");
			}
		}
	}

	[DataSourceProperty]
	public string OverrideSaveText
	{
		get
		{
			return _overwriteSaveText;
		}
		set
		{
			if (value != _overwriteSaveText)
			{
				_overwriteSaveText = value;
				OnPropertyChangedWithValue(value, "OverrideSaveText");
			}
		}
	}

	[DataSourceProperty]
	public string UpdateSaveText
	{
		get
		{
			return _updateSaveText;
		}
		set
		{
			if (value != _updateSaveText)
			{
				_updateSaveText = value;
				OnPropertyChangedWithValue(value, "UpdateSaveText");
			}
		}
	}

	[DataSourceProperty]
	public string ModulesText
	{
		get
		{
			return _modulesText;
		}
		set
		{
			if (value != _modulesText)
			{
				_modulesText = value;
				OnPropertyChangedWithValue(value, "ModulesText");
			}
		}
	}

	[DataSourceProperty]
	public string CorruptedSaveText
	{
		get
		{
			return _corruptedSaveText;
		}
		set
		{
			if (value != _corruptedSaveText)
			{
				_corruptedSaveText = value;
				OnPropertyChangedWithValue(value, "CorruptedSaveText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string GameTimeText
	{
		get
		{
			return _gameTimeText;
		}
		set
		{
			if (value != _gameTimeText)
			{
				_gameTimeText = value;
				OnPropertyChangedWithValue(value, "GameTimeText");
			}
		}
	}

	[DataSourceProperty]
	public string CharacterNameText
	{
		get
		{
			return _characterNameText;
		}
		set
		{
			if (value != _characterNameText)
			{
				_characterNameText = value;
				OnPropertyChangedWithValue(value, "CharacterNameText");
			}
		}
	}

	[DataSourceProperty]
	public string MainHeroVisualCode
	{
		get
		{
			return _mainHeroVisualCode;
		}
		set
		{
			if (value != _mainHeroVisualCode)
			{
				_mainHeroVisualCode = value;
				OnPropertyChangedWithValue(value, "MainHeroVisualCode");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel CharacterVisual
	{
		get
		{
			return _characterVisual;
		}
		set
		{
			if (value != _characterVisual)
			{
				_characterVisual = value;
				OnPropertyChangedWithValue(value, "CharacterVisual");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
			}
		}
	}

	[DataSourceProperty]
	public string RealTimeText1
	{
		get
		{
			return _realTimeText1;
		}
		set
		{
			if (value != _realTimeText1)
			{
				_realTimeText1 = value;
				OnPropertyChangedWithValue(value, "RealTimeText1");
			}
		}
	}

	[DataSourceProperty]
	public string RealTimeText2
	{
		get
		{
			return _realTimeText2;
		}
		set
		{
			if (value != _realTimeText2)
			{
				_realTimeText2 = value;
				OnPropertyChangedWithValue(value, "RealTimeText2");
			}
		}
	}

	[DataSourceProperty]
	public string LevelText
	{
		get
		{
			return _levelText;
		}
		set
		{
			if (value != _levelText)
			{
				_levelText = value;
				OnPropertyChangedWithValue(value, "LevelText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DateTimeHint
	{
		get
		{
			return _dateTimeHint;
		}
		set
		{
			if (value != _dateTimeHint)
			{
				_dateTimeHint = value;
				OnPropertyChangedWithValue(value, "DateTimeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UpdateButtonHint
	{
		get
		{
			return _updateButtonHint;
		}
		set
		{
			if (value != _updateButtonHint)
			{
				_updateButtonHint = value;
				OnPropertyChangedWithValue(value, "UpdateButtonHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DisabledReasonHint
	{
		get
		{
			return _disabledReasonHint;
		}
		set
		{
			if (value != _disabledReasonHint)
			{
				_disabledReasonHint = value;
				OnPropertyChangedWithValue(value, "DisabledReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFilteredOut
	{
		get
		{
			return _isFilteredOut;
		}
		set
		{
			if (value != _isFilteredOut)
			{
				_isFilteredOut = value;
				OnPropertyChangedWithValue(value, "IsFilteredOut");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	public SavedGameVM(SaveGameFileInfo save, bool isSaving, Action<SavedGameVM> onDelete, Action<SavedGameVM> onSelection, Action onCancelLoadSave, Action onDone, bool isCorruptedSave = false, bool isIronman = false)
	{
		Save = save;
		_isSaving = isSaving;
		_onDelete = onDelete;
		_onSelection = onSelection;
		_onCancelLoadSave = onCancelLoadSave;
		_onDone = onDone;
		IsCorrupted = isCorruptedSave;
		SavedGameProperties = new MBBindingList<SavedGamePropertyVM>();
		LoadedModulesInSave = new MBBindingList<SavedGameModuleInfoVM>();
		if (isIronman)
		{
			GameTexts.SetVariable("RANK", Save.MetaData.GetCharacterName());
			GameTexts.SetVariable("NUMBER", new TextObject("{=Fm0rjkH7}Ironman"));
			NameText = new TextObject("{=AVoWvlue}{RANK} ({NUMBER})").ToString();
		}
		else
		{
			NameText = Save.Name;
		}
		_newlineTextObject.SetTextVariable("newline", "\n");
		_gameVersion = MBSaveLoad.CurrentVersion;
		_saveVersion = Save.MetaData.GetApplicationVersion();
		_moduleCheckResults = SandBoxSaveHelper.CheckMetaDataCompatibilityErrors(save.MetaData);
		IsModuleDiscrepancyDetected = isCorruptedSave || _moduleCheckResults.Any((SandBoxSaveHelper.ModuleCheckResult x) => (x.Type == ModuleCheckResultType.ModuleAddedToGame && ModuleHelper.ModulesDisablingLoadingAfterBeingAdded.Contains(x.ModuleId)) || (x.Type == ModuleCheckResultType.ModuleRemovedFromGame && ModuleHelper.ModulesDisablingLoadingAfterBeingRemoved.Contains(x.ModuleId)));
		MainHeroVisualCode = (IsModuleDiscrepancyDetected ? string.Empty : Save.MetaData.GetCharacterVisualCode());
		BannerTextCode = (IsModuleDiscrepancyDetected ? string.Empty : Save.MetaData.GetClanBannerCode());
		IsDisabled = SandBoxSaveHelper.GetIsDisabledWithReason(Save, out var reason);
		DisabledReasonHint = new HintViewModel(reason);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LoadedModulesInSave.Clear();
		SavedGameProperties.Clear();
		SaveVersionAsString = _saveVersion.ToString();
		if (_gameVersion != _saveVersion)
		{
			RequiresInquiryOnLoad = true;
		}
		string[] modules = Save.MetaData.GetModules();
		foreach (string text in modules)
		{
			string value = Save.MetaData.GetModuleVersion(text).ToString();
			LoadedModulesInSave.Add(new SavedGameModuleInfoVM(SandBoxSaveHelper.GetModuleNameFromModuleId(text), "", value));
		}
		CharacterNameText = Save.MetaData.GetCharacterName();
		ClanBanner = new BannerImageIdentifierVM(new Banner(Save.MetaData.GetClanBannerCode()), nineGrid: true);
		DeleteText = new TextObject("{=deleteaction}Delete").ToString();
		ModulesText = new TextObject("{=JXyxj1J5}Modules").ToString();
		DateTime creationTime = Save.MetaData.GetCreationTime();
		RealTimeText1 = LocalizedTextManager.GetDateFormattedByLanguage(BannerlordConfig.Language, creationTime);
		RealTimeText2 = LocalizedTextManager.GetTimeFormattedByLanguage(BannerlordConfig.Language, creationTime);
		int playerHealthPercentage = Save.MetaData.GetPlayerHealthPercentage();
		TextObject textObject = new TextObject("{=gYATKZJp}{NUMBER}%");
		textObject.SetTextVariable("NUMBER", playerHealthPercentage.ToString());
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Health, textObject, new TextObject("{=hZrwUIaq}Health")));
		int mainHeroGold = Save.MetaData.GetMainHeroGold();
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Gold, GetAbbreviatedValueTextFromValue(mainHeroGold), new TextObject("{=Hxf6bzmR}Current Denars")));
		int valueAmount = (int)Save.MetaData.GetClanInfluence();
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Influence, GetAbbreviatedValueTextFromValue(valueAmount), new TextObject("{=RVPidk5a}Influence")));
		if (HasNavalDLC(Save.MetaData))
		{
			int mainPartyShipCount = Save.MetaData.GetMainPartyShipCount();
			SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Ships, GetAbbreviatedValueTextFromValue(mainPartyShipCount), new TextObject("{=2zD7LFSj}Ships Owned")));
		}
		int num = Save.MetaData.GetMainPartyHealthyMemberCount() + Save.MetaData.GetMainPartyWoundedMemberCount();
		int mainPartyPrisonerMemberCount = Save.MetaData.GetMainPartyPrisonerMemberCount();
		TextObject textObject2;
		if (mainPartyPrisonerMemberCount > 0)
		{
			textObject2 = new TextObject("{=6qYaQkDD}{COUNT} + {PRISONER_COUNT}p");
			textObject2.SetTextVariable("COUNT", num);
			textObject2.SetTextVariable("PRISONER_COUNT", mainPartyPrisonerMemberCount);
		}
		else
		{
			textObject2 = new TextObject(num);
		}
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.PartySize, textObject2, new TextObject("{=IXwOaa98}Party Size")));
		int value2 = (int)Save.MetaData.GetMainPartyFood();
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Food, new TextObject(value2), new TextObject("{=qSi4DlT4}Food")));
		int clanFiefs = Save.MetaData.GetClanFiefs();
		SavedGameProperties.Add(new SavedGamePropertyVM(SavedGamePropertyVM.SavedGameProperty.Fiefs, new TextObject(clanFiefs), new TextObject("{=SRjrhb0A}Owned Fief Count")));
		TextObject textObject3 = new TextObject("{=GZWPHmAw}Day : {DAY}");
		string variable = ((int)Save.MetaData.GetDayLong()).ToString();
		textObject3.SetTextVariable("DAY", variable);
		GameTimeText = textObject3.ToString();
		TextObject textObject4 = new TextObject("{=IwhpeT8C}Level : {PLAYER_LEVEL}");
		textObject4.SetTextVariable("PLAYER_LEVEL", Save.MetaData.GetMainHeroLevel().ToString());
		LevelText = textObject4.ToString();
		DateTimeHint = new HintViewModel(new TextObject("{=!}" + RealTimeText1));
		UpdateButtonHint = new HintViewModel(new TextObject("{=ZDPIq4hi}Load the selected save game, overwrite it with the current version of the game and get back to this screen."));
		SaveLoadText = (_isSaving ? new TextObject("{=bV75iwKa}Save").ToString() : new TextObject("{=9NuttOBC}Load").ToString());
		OverrideSaveText = new TextObject("{=hYL3CFHX}Do you want to overwrite this saved game?").ToString();
		UpdateSaveText = new TextObject("{=FFiPLPbs}Update Save").ToString();
		CorruptedSaveText = new TextObject("{=RoYPofhK}Corrupted Save").ToString();
	}

	private bool HasNavalDLC(MetaData metaData)
	{
		string value = "NavalDLC";
		return metaData.GetModules().Contains(value);
	}

	public void ExecuteSaveLoad()
	{
		if (IsCorrupted || IsDisabled)
		{
			return;
		}
		if (_isSaving)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=Q1HIlJxe}Overwrite").ToString(), OverrideSaveText, isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), OnOverrideSaveAccept, delegate
			{
				_onCancelLoadSave?.Invoke();
			}));
		}
		else
		{
			SandBoxSaveHelper.TryLoadSave(Save, StartGame, _onCancelLoadSave);
		}
	}

	private void StartGame(LoadResult loadResult)
	{
		if (Game.Current != null)
		{
			ScreenManager.PopScreen();
			GameStateManager.Current.CleanStates();
			GameStateManager.Current = Module.CurrentModule.GlobalGameStateManager;
		}
		MBSaveLoad.OnStartGame(loadResult);
		MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
	}

	private void OnOverrideSaveAccept()
	{
		Campaign.Current.SaveHandler.SaveAs(Save.Name);
		_onDone();
	}

	private static TextObject GetAbbreviatedValueTextFromValue(int valueAmount)
	{
		string variable = "";
		decimal num = valueAmount;
		if (valueAmount < 10000)
		{
			return new TextObject(valueAmount);
		}
		if (valueAmount >= 10000 && valueAmount < 1000000)
		{
			variable = new TextObject("{=thousandabbr}k").ToString();
			num /= 1000m;
		}
		else if (valueAmount >= 1000000 && valueAmount < 1000000000)
		{
			variable = new TextObject("{=millionabbr}m").ToString();
			num /= 1000000m;
		}
		else if (valueAmount >= 1000000000 && valueAmount <= int.MaxValue)
		{
			variable = new TextObject("{=billionabbr}b").ToString();
			num /= 1000000000m;
		}
		int num2 = (int)num;
		string text = num2.ToString();
		if (text.Length < 3)
		{
			text += ".";
			string text2 = num.ToString("F3").Split(new char[1] { '.' }).ElementAtOrDefault(1);
			if (text2 != null)
			{
				for (int i = 0; i < 3 - num2.ToString().Length; i++)
				{
					if (text2.ElementAtOrDefault(i) != 0)
					{
						text += text2.ElementAtOrDefault(i);
					}
				}
			}
		}
		TextObject textObject = new TextObject("{=mapbardenarvalue}{DENAR_AMOUNT}{VALUE_ABBREVIATION}");
		textObject.SetTextVariable("DENAR_AMOUNT", text);
		textObject.SetTextVariable("VALUE_ABBREVIATION", variable);
		return textObject;
	}

	public void ExecuteUpdate()
	{
	}

	public void ExecuteDelete()
	{
		_onDelete(this);
	}

	public void ExecuteSelection()
	{
		_onSelection(this);
	}
}
