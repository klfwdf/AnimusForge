using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;

namespace SandBox.ViewModelCollection.SaveLoad;

public class SaveLoadVM : ViewModel
{
	private const int _maxSaveFileNameLength = 30;

	private readonly TextObject _categorizedSaveGroupName = new TextObject("{=nVGqjtaa}Campaign {ID}");

	private readonly TextObject _uncategorizedSaveGroupName = new TextObject("{=uncategorized_save}Uncategorized");

	private readonly TextObject _textIsEmptyReasonText = new TextObject("{=7AI8jA0b}Input text cannot be empty.");

	private readonly TextObject _textHasSpecialCharReasonText = new TextObject("{=kXRdeawC}Input text cannot include special characters.");

	private readonly TextObject _textTooLongReasonText = new TextObject("{=B3W6fcQX}Input text cannot be longer than {MAX_LENGTH} characters.");

	private readonly TextObject _saveAlreadyExistsReasonText = new TextObject("{=aG6XMhA1}A saved game file already exists with this name.");

	private readonly TextObject _saveNameReservedReasonText = new TextObject("{=M4WMKyE1}Input text includes reserved text.");

	private readonly TextObject _allSpaceReasonText = new TextObject("{=Rtakaivj}Input text needs to include at least one non-space character.");

	private readonly TextObject _visualIsDisabledText = new TextObject("{=xlEZ02Qw}Character visual is disabled during 'Save As' on the campaign map.");

	private bool _isLoadingSaves;

	private bool _isBusyWithAnAction;

	private bool _isSearchAvailable;

	private string _searchText;

	private string _searchPlaceholderText;

	private string _doneText;

	private string _createNewSaveSlotText;

	private string _titleText;

	private string _visualDisabledText;

	private bool _isSaving;

	private bool _isActionEnabled;

	private bool _isAnyItemSelected;

	private bool _canCreateNewSave;

	private bool _isVisualDisabled;

	private string _saveLoadText;

	private string _cancelText;

	private HintViewModel _createNewSaveHint;

	private MBBindingList<SavedGameGroupVM> _saveGroups;

	private SavedGameVM _currentSelectedSave;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _deleteInputKey;

	private IEnumerable<SaveGameFileInfo> _allSavedGames => SaveGroups.SelectMany((SavedGameGroupVM s) => s.SavedGamesList.Select((SavedGameVM v) => v.Save));

	[DataSourceProperty]
	public bool IsLoadingSaves
	{
		get
		{
			return _isLoadingSaves;
		}
		set
		{
			if (value != _isLoadingSaves)
			{
				_isLoadingSaves = value;
				OnPropertyChangedWithValue(value, "IsLoadingSaves");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBusyWithAnAction
	{
		get
		{
			return _isBusyWithAnAction;
		}
		set
		{
			if (value != _isBusyWithAnAction)
			{
				_isBusyWithAnAction = value;
				OnPropertyChangedWithValue(value, "IsBusyWithAnAction");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSearchAvailable
	{
		get
		{
			return _isSearchAvailable;
		}
		set
		{
			if (value != _isSearchAvailable)
			{
				_isSearchAvailable = value;
				OnPropertyChangedWithValue(value, "IsSearchAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (value != _searchText)
			{
				value.IndexOf(_searchText ?? "");
				_searchText = value;
				OnPropertyChangedWithValue(value, "SearchText");
			}
		}
	}

	[DataSourceProperty]
	public string SearchPlaceholderText
	{
		get
		{
			return _searchPlaceholderText;
		}
		set
		{
			if (value != _searchPlaceholderText)
			{
				_searchPlaceholderText = value;
				OnPropertyChangedWithValue(value, "SearchPlaceholderText");
			}
		}
	}

	[DataSourceProperty]
	public string VisualDisabledText
	{
		get
		{
			return _visualDisabledText;
		}
		set
		{
			if (value != _visualDisabledText)
			{
				_visualDisabledText = value;
				OnPropertyChangedWithValue(value, "VisualDisabledText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SavedGameGroupVM> SaveGroups
	{
		get
		{
			return _saveGroups;
		}
		set
		{
			if (value != _saveGroups)
			{
				_saveGroups = value;
				OnPropertyChangedWithValue(value, "SaveGroups");
			}
		}
	}

	[DataSourceProperty]
	public SavedGameVM CurrentSelectedSave
	{
		get
		{
			return _currentSelectedSave;
		}
		set
		{
			if (value != _currentSelectedSave)
			{
				_currentSelectedSave = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedSave");
			}
		}
	}

	[DataSourceProperty]
	public string CreateNewSaveSlotText
	{
		get
		{
			return _createNewSaveSlotText;
		}
		set
		{
			if (value != _createNewSaveSlotText)
			{
				_createNewSaveSlotText = value;
				OnPropertyChangedWithValue(value, "CreateNewSaveSlotText");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSaving
	{
		get
		{
			return _isSaving;
		}
		set
		{
			if (value != _isSaving)
			{
				_isSaving = value;
				OnPropertyChangedWithValue(value, "IsSaving");
			}
		}
	}

	[DataSourceProperty]
	public bool CanCreateNewSave
	{
		get
		{
			return _canCreateNewSave;
		}
		set
		{
			if (value != _canCreateNewSave)
			{
				_canCreateNewSave = value;
				OnPropertyChangedWithValue(value, "CanCreateNewSave");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVisualDisabled
	{
		get
		{
			return _isVisualDisabled;
		}
		set
		{
			if (value != _isVisualDisabled)
			{
				_isVisualDisabled = value;
				OnPropertyChangedWithValue(value, "IsVisualDisabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CreateNewSaveHint
	{
		get
		{
			return _createNewSaveHint;
		}
		set
		{
			if (value != _createNewSaveHint)
			{
				_createNewSaveHint = value;
				OnPropertyChangedWithValue(value, "CreateNewSaveHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActionEnabled
	{
		get
		{
			return _isActionEnabled;
		}
		set
		{
			if (value != _isActionEnabled)
			{
				_isActionEnabled = value;
				OnPropertyChangedWithValue(value, "IsActionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyItemSelected
	{
		get
		{
			return _isAnyItemSelected;
		}
		set
		{
			if (value != _isAnyItemSelected)
			{
				_isAnyItemSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
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

	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	public InputKeyItemVM DeleteInputKey
	{
		get
		{
			return _deleteInputKey;
		}
		set
		{
			if (value != _deleteInputKey)
			{
				_deleteInputKey = value;
				OnPropertyChangedWithValue(value, "DeleteInputKey");
			}
		}
	}

	public SaveLoadVM(bool isSaving, bool isCampaignMapOnStack)
	{
		_isSaving = isSaving;
		SaveGroups = new MBBindingList<SavedGameGroupVM>();
		IsVisualDisabled = false;
	}

	public async void Initialize()
	{
		IsBusyWithAnAction = true;
		IsLoadingSaves = true;
		int num = 0;
		SaveGameFileInfo[] saveFiles = MBSaveLoad.GetSaveFiles();
		IEnumerable<SaveGameFileInfo> enumerable = saveFiles.Where((SaveGameFileInfo s) => s.IsCorrupted);
		foreach (IGrouping<string, SaveGameFileInfo> item in from m in saveFiles
			where !m.IsCorrupted
			group m by m.MetaData.GetUniqueGameId() into s
			orderby GetMostRecentSaveInGroup(s) descending
			select s)
		{
			SavedGameGroupVM savedGameGroupVM = new SavedGameGroupVM();
			if (string.IsNullOrWhiteSpace(item.Key))
			{
				savedGameGroupVM.IdentifierID = _uncategorizedSaveGroupName.ToString();
			}
			else
			{
				num++;
				_categorizedSaveGroupName.SetTextVariable("ID", num);
				savedGameGroupVM.IdentifierID = _categorizedSaveGroupName.ToString();
			}
			foreach (SaveGameFileInfo item2 in item.OrderByDescending((SaveGameFileInfo s) => s.MetaData.GetCreationTime()))
			{
				bool ironmanMode = item2.MetaData.GetIronmanMode();
				savedGameGroupVM.SavedGamesList.Add(new SavedGameVM(item2, IsSaving, OnDeleteSavedGame, OnSaveSelection, OnCancelLoadSave, ExecuteDone, isCorruptedSave: false, ironmanMode));
			}
			SaveGroups.Add(savedGameGroupVM);
		}
		if (enumerable.Any())
		{
			SavedGameGroupVM savedGameGroupVM2 = new SavedGameGroupVM
			{
				IdentifierID = new TextObject("{=o9PIe7am}Corrupted").ToString()
			};
			foreach (SaveGameFileInfo item3 in enumerable)
			{
				savedGameGroupVM2.SavedGamesList.Add(new SavedGameVM(item3, IsSaving, OnDeleteSavedGame, OnSaveSelection, OnCancelLoadSave, ExecuteDone, isCorruptedSave: true));
			}
			SaveGroups.Add(savedGameGroupVM2);
		}
		RefreshCanCreateNewSave();
		RefreshCanSearch();
		OnSaveSelection(GetFirstAvailableSavedGame());
		RefreshValues();
		await Task.Delay(1);
		IsBusyWithAnAction = false;
		IsLoadingSaves = false;
	}

	private SavedGameVM GetFirstAvailableSavedGame()
	{
		for (int i = 0; i < SaveGroups.Count; i++)
		{
			SavedGameGroupVM savedGameGroupVM = SaveGroups[i];
			for (int j = 0; j < savedGameGroupVM.SavedGamesList.Count; j++)
			{
				SavedGameVM savedGameVM = savedGameGroupVM.SavedGamesList[j];
				if (!savedGameVM.IsCorrupted && !savedGameVM.IsDisabled)
				{
					return savedGameVM;
				}
			}
		}
		return null;
	}

	private void RefreshCanCreateNewSave()
	{
		CanCreateNewSave = !MBSaveLoad.IsMaxNumberOfSavesReached();
		CreateNewSaveHint = new HintViewModel(CanCreateNewSave ? null : new TextObject("{=DeXfSjgY}Cannot create a new save. Save limit reached."));
	}

	private void RefreshCanSearch()
	{
		IsSearchAvailable = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=hiCxFj4E}Saved Campaigns").ToString();
		DoneText = new TextObject("{=WiNRdfsm}Done").ToString();
		CreateNewSaveSlotText = new TextObject("{=eL8nhkhQ}Create New Save Slot").ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		SaveLoadText = (_isSaving ? new TextObject("{=bV75iwKa}Save").ToString() : new TextObject("{=9NuttOBC}Load").ToString());
		SearchPlaceholderText = new TextObject("{=tQOPRBFg}Search...").ToString();
		if (IsVisualDisabled)
		{
			VisualDisabledText = _visualIsDisabledText.ToString();
		}
		SaveGroups.ApplyActionOnAllItems(delegate(SavedGameGroupVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedSave?.RefreshValues();
	}

	private DateTime GetMostRecentSaveInGroup(IGrouping<string, SaveGameFileInfo> group)
	{
		return group.OrderByDescending((SaveGameFileInfo g) => g.MetaData.GetCreationTime()).FirstOrDefault()?.MetaData.GetCreationTime() ?? default(DateTime);
	}

	private void OnSaveSelection(SavedGameVM save)
	{
		if (save != CurrentSelectedSave)
		{
			if (CurrentSelectedSave != null)
			{
				CurrentSelectedSave.IsSelected = false;
			}
			CurrentSelectedSave = save;
			if (CurrentSelectedSave != null)
			{
				CurrentSelectedSave.IsSelected = true;
			}
			IsAnyItemSelected = CurrentSelectedSave != null;
			IsActionEnabled = IsAnyItemSelected && !CurrentSelectedSave.IsCorrupted && !CurrentSelectedSave.IsDisabled;
		}
	}

	public void ExecuteCreateNewSaveGame()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=7WdWK2Dt}Save Game").ToString(), new TextObject("{=WDlVhNuq}Name your save file").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=WiNRdfsm}Done").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), OnSaveAsDone, null, shouldInputBeObfuscated: false, IsSaveGameNameApplicable));
	}

	private Tuple<bool, string> IsSaveGameNameApplicable(string inputText)
	{
		string item = string.Empty;
		bool item2 = true;
		if (string.IsNullOrEmpty(inputText))
		{
			item = _textIsEmptyReasonText.ToString();
			item2 = false;
		}
		else if (inputText.All((char c) => char.IsWhiteSpace(c)))
		{
			item = _allSpaceReasonText.ToString();
			item2 = false;
		}
		else if (inputText.Any((char c) => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
		{
			item = _textHasSpecialCharReasonText.ToString();
			item2 = false;
		}
		else if (inputText.Length >= 30)
		{
			_textTooLongReasonText.SetTextVariable("MAX_LENGTH", 30);
			item = _textTooLongReasonText.ToString();
			item2 = false;
		}
		else if (MBSaveLoad.IsSaveFileNameReserved(inputText))
		{
			item = _saveNameReservedReasonText.ToString();
			item2 = false;
		}
		else if (_allSavedGames.Any((SaveGameFileInfo s) => string.Equals(s.Name, inputText, StringComparison.InvariantCultureIgnoreCase)))
		{
			item = _saveAlreadyExistsReasonText.ToString();
			item2 = false;
		}
		return new Tuple<bool, string>(item2, item);
	}

	private void OnSaveAsDone(string saveName)
	{
		Campaign.Current.SaveHandler.SaveAs(saveName);
		ExecuteDone();
	}

	public void ExecuteDone()
	{
		ScreenManager.PopScreen();
	}

	public void ExecuteLoadSave()
	{
		LoadSelectedSave();
	}

	private void LoadSelectedSave()
	{
		if (!IsBusyWithAnAction && CurrentSelectedSave != null && !CurrentSelectedSave.IsCorrupted && !CurrentSelectedSave.IsDisabled)
		{
			CurrentSelectedSave.ExecuteSaveLoad();
			IsBusyWithAnAction = true;
		}
	}

	private void OnCancelLoadSave()
	{
		IsBusyWithAnAction = false;
	}

	private void ExecuteResetCurrentSave()
	{
		CurrentSelectedSave = null;
	}

	private void OnDeleteSavedGame(SavedGameVM savedGame)
	{
		if (IsBusyWithAnAction)
		{
			return;
		}
		IsBusyWithAnAction = true;
		string text = new TextObject("{=M1AEHJ76}Please notice that this save is created for a session which has Ironman mode enabled. There is no other save file for the related session. Are you sure you want to delete this save game?").ToString();
		string text2 = new TextObject("{=HH2mZq8J}Are you sure you want to delete this save game?").ToString();
		string titleText = new TextObject("{=QHV8aeEg}Delete Save").ToString();
		string text3 = (savedGame.Save.MetaData.GetIronmanMode() ? text : text2);
		InformationManager.ShowInquiry(new InquiryData(titleText, text3, isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), delegate
		{
			IsBusyWithAnAction = true;
			bool num = MBSaveLoad.DeleteSaveGame(savedGame.Save.Name);
			IsBusyWithAnAction = false;
			if (num)
			{
				DeleteSave(savedGame);
				OnSaveSelection(GetFirstAvailableSavedGame());
				RefreshCanCreateNewSave();
				RefreshCanSearch();
			}
			else
			{
				OnDeleteSaveUnsuccessful();
			}
		}, delegate
		{
			IsBusyWithAnAction = false;
		}));
	}

	private void OnDeleteSaveUnsuccessful()
	{
		string titleText = new TextObject("{=oZrVNUOk}Error").ToString();
		string text = new TextObject("{=PY00wRz4}Failed to delete the save file.").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=WiNRdfsm}Done").ToString(), string.Empty, null, null));
	}

	private void DeleteSave(SavedGameVM save)
	{
		foreach (SavedGameGroupVM saveGroup in SaveGroups)
		{
			if (saveGroup.SavedGamesList.Contains(save))
			{
				saveGroup.SavedGamesList.Remove(save);
				break;
			}
		}
		if (string.IsNullOrEmpty(BannerlordConfig.LatestSaveGameName) || save.Save.Name == BannerlordConfig.LatestSaveGameName)
		{
			BannerlordConfig.LatestSaveGameName = GetFirstAvailableSavedGame()?.Save.Name ?? string.Empty;
			BannerlordConfig.Save();
		}
	}

	public void DeleteSelectedSave()
	{
		if (CurrentSelectedSave != null)
		{
			OnDeleteSavedGame(CurrentSelectedSave);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
		DeleteInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetDeleteInputKey(HotKey hotkey)
	{
		DeleteInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
