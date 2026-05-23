using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;

public class InitialMenuVM : ViewModel
{
	private MBBindingList<InitialMenuOptionVM> _menuOptions;

	private InitialMenuAnnouncementVM _announcement;

	private bool _isProfileSelectionEnabled;

	private bool _isDownloadingContent;

	private bool _isNavalDLCEnabled;

	private string _selectProfileText;

	private string _profileName;

	private string _downloadingText;

	private string _currentLanguageString;

	[DataSourceProperty]
	public MBBindingList<InitialMenuOptionVM> MenuOptions
	{
		get
		{
			return _menuOptions;
		}
		set
		{
			if (value != _menuOptions)
			{
				_menuOptions = value;
				OnPropertyChangedWithValue(value, "MenuOptions");
			}
		}
	}

	[DataSourceProperty]
	public InitialMenuAnnouncementVM Announcement
	{
		get
		{
			return _announcement;
		}
		set
		{
			if (value != _announcement)
			{
				_announcement = value;
				OnPropertyChangedWithValue(value, "Announcement");
			}
		}
	}

	[DataSourceProperty]
	public string DownloadingText
	{
		get
		{
			return _downloadingText;
		}
		set
		{
			if (value != _downloadingText)
			{
				_downloadingText = value;
				OnPropertyChangedWithValue(value, "DownloadingText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectProfileText
	{
		get
		{
			return _selectProfileText;
		}
		set
		{
			if (value != _selectProfileText)
			{
				_selectProfileText = value;
				OnPropertyChangedWithValue(value, "SelectProfileText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfileName
	{
		get
		{
			return _profileName;
		}
		set
		{
			if (value != _profileName)
			{
				_profileName = value;
				OnPropertyChangedWithValue(value, "ProfileName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsProfileSelectionEnabled
	{
		get
		{
			return _isProfileSelectionEnabled;
		}
		set
		{
			if (value != _isProfileSelectionEnabled)
			{
				_isProfileSelectionEnabled = value;
				OnPropertyChangedWithValue(value, "IsProfileSelectionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDownloadingContent
	{
		get
		{
			return _isDownloadingContent;
		}
		set
		{
			if (value != _isDownloadingContent)
			{
				_isDownloadingContent = value;
				OnPropertyChangedWithValue(value, "IsDownloadingContent");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNavalDLCEnabled
	{
		get
		{
			return _isNavalDLCEnabled;
		}
		set
		{
			if (value != _isNavalDLCEnabled)
			{
				_isNavalDLCEnabled = value;
				OnPropertyChangedWithValue(value, "IsNavalDLCEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentLanguageString
	{
		get
		{
			return _currentLanguageString;
		}
		set
		{
			if (value != _currentLanguageString)
			{
				_currentLanguageString = value;
				OnPropertyChangedWithValue(value, "CurrentLanguageString");
			}
		}
	}

	public InitialMenuVM(InitialState initialState)
	{
		MenuOptions = new MBBindingList<InitialMenuOptionVM>();
		Announcement = new InitialMenuAnnouncementVM();
		if (HotKeyManager.ShouldNotifyDocumentVersionDifferent())
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=0Itt3bZM}Current keybind document version is outdated. Keybinds have been reverted to defaults."));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		MenuOptions?.ApplyActionOnAllItems(delegate(InitialMenuOptionVM o)
		{
			o.RefreshValues();
		});
		Announcement.Refresh();
		SelectProfileText = new TextObject("{=wubDWOlh}Select Profile").ToString();
		DownloadingText = new TextObject("{=i4Oo6aoM}Downloading Content...").ToString();
		CurrentLanguageString = BannerlordConfig.Language;
	}

	public void Tick()
	{
		Announcement?.Tick();
	}

	public void RefreshMenuOptions()
	{
		MenuOptions.ApplyActionOnAllItems(delegate(InitialMenuOptionVM x)
		{
			x.OnFinalize();
		});
		MenuOptions.Clear();
		_ = GameStateManager.Current.ActiveState;
		foreach (InitialStateOption initialStateOption in Module.CurrentModule.GetInitialStateOptions())
		{
			MenuOptions.Add(new InitialMenuOptionVM(initialStateOption));
		}
		IsDownloadingContent = Utilities.IsOnlyCoreContentEnabled();
		IsNavalDLCEnabled = ModuleHelper.IsModuleActive("NavalDLC");
		Announcement.Refresh();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		MenuOptions.ApplyActionOnAllItems(delegate(InitialMenuOptionVM x)
		{
			x.OnFinalize();
		});
		MenuOptions.Clear();
		Announcement.OnFinalize();
	}
}
