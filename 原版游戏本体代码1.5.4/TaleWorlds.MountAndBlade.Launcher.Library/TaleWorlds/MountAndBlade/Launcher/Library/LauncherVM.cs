using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherVM : ViewModel
{
	private UserDataManager _userDataManager;

	private NewsManager _newsManager;

	private readonly Action _onClose;

	private readonly Action _onMinimize;

	private bool _isInitialized;

	private bool _isContinueSelected;

	private const string _newsSourceURLBase = "https://taleworldswebsiteassets.blob.core.windows.net/upload/bannerlordnews/NewsFeed_";

	private bool _isMultiplayer;

	private bool _isSingleplayer;

	private bool _isDigitalCompanion;

	private bool _isSingleplayerAvailable;

	private bool _isDigitalCompanionAvailable;

	private LauncherNewsVM _news;

	private LauncherModsVM _modsData;

	private LauncherConfirmStartVM _confirmStart;

	private LauncherInformationVM _hint;

	private string _playText;

	private string _continueText;

	private string _launchText;

	private string _singleplayerText;

	private string _multiplayerText;

	private string _digitalCompanionText;

	private string _newsText;

	private string _dlcText;

	private string _modsText;

	private string _versionText;

	public string GameTypeArgument
	{
		get
		{
			if (!IsMultiplayer)
			{
				return "/singleplayer";
			}
			return "/multiplayer";
		}
	}

	public string ContinueGameArgument
	{
		get
		{
			if (!_isContinueSelected)
			{
				return "";
			}
			return " /continuegame";
		}
	}

	[DataSourceProperty]
	public bool IsSingleplayer
	{
		get
		{
			return _isSingleplayer;
		}
		set
		{
			if (_isSingleplayer != value)
			{
				OnBeforeGameTypeChange(_isMultiplayer, !value);
				_isSingleplayer = value;
				OnPropertyChangedWithValue(value, "IsSingleplayer");
				if (value)
				{
					OnAfterGameTypeChange(isMultiplayer: false, isSingleplayer: true, isDigitalCompanion: false);
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsMultiplayer
	{
		get
		{
			return _isMultiplayer;
		}
		set
		{
			if (_isMultiplayer != value)
			{
				OnBeforeGameTypeChange(_isMultiplayer, value);
				_isMultiplayer = value;
				OnPropertyChangedWithValue(value, "IsMultiplayer");
				if (value)
				{
					OnAfterGameTypeChange(isMultiplayer: true, isSingleplayer: false, isDigitalCompanion: false);
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsDigitalCompanion
	{
		get
		{
			return _isDigitalCompanion;
		}
		set
		{
			if (_isDigitalCompanion != value)
			{
				_isDigitalCompanion = value;
				OnPropertyChangedWithValue(value, "IsDigitalCompanion");
				if (value)
				{
					OnAfterGameTypeChange(isMultiplayer: false, isSingleplayer: false, isDigitalCompanion: true);
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsSingleplayerAvailable
	{
		get
		{
			return _isSingleplayerAvailable;
		}
		set
		{
			if (value != _isSingleplayerAvailable)
			{
				_isSingleplayerAvailable = value;
				OnPropertyChangedWithValue(value, "IsSingleplayerAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDigitalCompanionAvailable
	{
		get
		{
			return _isDigitalCompanionAvailable;
		}
		set
		{
			if (value != _isDigitalCompanionAvailable)
			{
				_isDigitalCompanionAvailable = value;
				OnPropertyChangedWithValue(value, "IsDigitalCompanionAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string VersionText
	{
		get
		{
			return _versionText;
		}
		set
		{
			if (value != _versionText)
			{
				_versionText = value;
				OnPropertyChangedWithValue(value, "VersionText");
			}
		}
	}

	[DataSourceProperty]
	public LauncherNewsVM News
	{
		get
		{
			return _news;
		}
		set
		{
			if (value != _news)
			{
				_news = value;
				OnPropertyChangedWithValue(value, "News");
			}
		}
	}

	[DataSourceProperty]
	public LauncherConfirmStartVM ConfirmStart
	{
		get
		{
			return _confirmStart;
		}
		set
		{
			if (value != _confirmStart)
			{
				_confirmStart = value;
				OnPropertyChangedWithValue(value, "ConfirmStart");
			}
		}
	}

	[DataSourceProperty]
	public LauncherModsVM ModsData
	{
		get
		{
			return _modsData;
		}
		set
		{
			if (value != _modsData)
			{
				_modsData = value;
				OnPropertyChangedWithValue(value, "ModsData");
			}
		}
	}

	[DataSourceProperty]
	public LauncherInformationVM Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (_hint != value)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public string PlayText
	{
		get
		{
			return _playText;
		}
		set
		{
			if (_playText != value)
			{
				_playText = value;
				OnPropertyChangedWithValue(value, "PlayText");
			}
		}
	}

	[DataSourceProperty]
	public string ContinueText
	{
		get
		{
			return _continueText;
		}
		set
		{
			if (_continueText != value)
			{
				_continueText = value;
				OnPropertyChangedWithValue(value, "ContinueText");
			}
		}
	}

	[DataSourceProperty]
	public string LaunchText
	{
		get
		{
			return _launchText;
		}
		set
		{
			if (_launchText != value)
			{
				_launchText = value;
				OnPropertyChangedWithValue(value, "LaunchText");
			}
		}
	}

	[DataSourceProperty]
	public string SingleplayerText
	{
		get
		{
			return _singleplayerText;
		}
		set
		{
			if (_singleplayerText != value)
			{
				_singleplayerText = value;
				OnPropertyChangedWithValue(value, "SingleplayerText");
			}
		}
	}

	[DataSourceProperty]
	public string DigitalCompanionText
	{
		get
		{
			return _digitalCompanionText;
		}
		set
		{
			if (_digitalCompanionText != value)
			{
				_digitalCompanionText = value;
				OnPropertyChangedWithValue(value, "DigitalCompanionText");
			}
		}
	}

	[DataSourceProperty]
	public string MultiplayerText
	{
		get
		{
			return _multiplayerText;
		}
		set
		{
			if (_multiplayerText != value)
			{
				_multiplayerText = value;
				OnPropertyChangedWithValue(value, "MultiplayerText");
			}
		}
	}

	[DataSourceProperty]
	public string NewsText
	{
		get
		{
			return _newsText;
		}
		set
		{
			if (_newsText != value)
			{
				_newsText = value;
				OnPropertyChangedWithValue(value, "NewsText");
			}
		}
	}

	[DataSourceProperty]
	public string DlcText
	{
		get
		{
			return _dlcText;
		}
		set
		{
			if (_dlcText != value)
			{
				_dlcText = value;
				OnPropertyChangedWithValue(value, "DlcText");
			}
		}
	}

	[DataSourceProperty]
	public string ModsText
	{
		get
		{
			return _modsText;
		}
		set
		{
			if (_modsText != value)
			{
				_modsText = value;
				OnPropertyChangedWithValue(value, "ModsText");
			}
		}
	}

	public LauncherVM(UserDataManager userDataManager, Action onClose, Action onMinimize)
	{
		_userDataManager = userDataManager;
		_newsManager = new NewsManager();
		_newsManager.SetNewsSourceURL(GetApplicableNewsSourceURL());
		_onClose = onClose;
		_onMinimize = onMinimize;
		PlayText = "P L A Y";
		ContinueText = "C O N T I N U E";
		LaunchText = "L A U N C H";
		SingleplayerText = "Singleplayer";
		MultiplayerText = "Multiplayer";
		DigitalCompanionText = "Digital Companion";
		NewsText = "News";
		DlcText = "DLC";
		ModsText = "Mods";
		VersionText = ApplicationVersion.FromParametersFile().ToString();
		IsSingleplayerAvailable = GameModExists("Sandbox");
		IsDigitalCompanionAvailable = Program.IsDigitalCompanionAvailable();
		bool flag = !IsSingleplayerAvailable || _userDataManager.UserData.GameType == GameType.Multiplayer;
		ConfirmStart = new LauncherConfirmStartVM(ExecuteConfirmUnverifiedDLLStart);
		News = new LauncherNewsVM(_newsManager, flag);
		ModsData = new LauncherModsVM(userDataManager);
		Hint = new LauncherInformationVM();
		IsSingleplayer = !flag;
		IsMultiplayer = flag;
		IsDigitalCompanion = false;
		Refresh();
		_isInitialized = true;
	}

	private void UpdateAndSaveUserModsData(bool isMultiplayer)
	{
		UserData userData = _userDataManager.UserData;
		UserGameTypeData userGameTypeData = (isMultiplayer ? userData.MultiplayerData : userData.SingleplayerData);
		userGameTypeData.ModDatas.Clear();
		foreach (LauncherModuleVM module in ModsData.Modules)
		{
			userGameTypeData.ModDatas.Add(new UserModData(module.Info.Id, module.Info.Version.ToString(), module.IsSelected));
		}
		_userDataManager.SaveUserData();
	}

	private bool GameModExists(string modId)
	{
		List<ModuleInfo> modulesForLauncher = ModuleHelper.GetModulesForLauncher();
		for (int i = 0; i < modulesForLauncher.Count; i++)
		{
			if (modulesForLauncher[i].Id == modId)
			{
				return true;
			}
		}
		return false;
	}

	private void OnBeforeGameTypeChange(bool preSelectionIsMultiplayer, bool newSelectionIsMultiplayer)
	{
		if (_isInitialized)
		{
			_userDataManager.UserData.GameType = (newSelectionIsMultiplayer ? GameType.Multiplayer : GameType.Singleplayer);
			UpdateAndSaveUserModsData(preSelectionIsMultiplayer);
		}
	}

	private void OnAfterGameTypeChange(bool isMultiplayer, bool isSingleplayer, bool isDigitalCompanion)
	{
		IsMultiplayer = isMultiplayer;
		IsSingleplayer = isSingleplayer;
		IsDigitalCompanion = isDigitalCompanion;
		Refresh();
	}

	public void ExecuteStartGame(int mode)
	{
		_isContinueSelected = mode == 1;
		UpdateAndSaveUserModsData(IsMultiplayer);
		List<SubModuleInfo> list = new List<SubModuleInfo>();
		List<DependentVersionMissmatchItem> list2 = new List<DependentVersionMissmatchItem>();
		if (IsSingleplayer)
		{
			foreach (LauncherModuleVM module in ModsData.Modules)
			{
				if (!module.IsSelected)
				{
					continue;
				}
				foreach (LauncherSubModule subModule in module.SubModules)
				{
					if (!string.IsNullOrEmpty(subModule.Info.DLLName) && subModule.Info.DLLExists && !subModule.Info.IsTWCertifiedDLL)
					{
						list.Add(subModule.Info);
					}
				}
				if (module.Info.IsOfficial)
				{
					continue;
				}
				List<Tuple<DependedModule, ApplicationVersion>> list3 = new List<Tuple<DependedModule, ApplicationVersion>>();
				foreach (DependedModule dependedModule in module.Info.DependedModules)
				{
					ApplicationVersion applicationVersionOfModule = GetApplicationVersionOfModule(dependedModule.ModuleId);
					if (!dependedModule.Version.IsSame(applicationVersionOfModule, checkChangeSet: false))
					{
						list3.Add(new Tuple<DependedModule, ApplicationVersion>(dependedModule, applicationVersionOfModule));
					}
				}
				if (list3.Count > 0)
				{
					list2.Add(new DependentVersionMissmatchItem(module.Name, list3));
				}
			}
		}
		if (IsDigitalCompanion)
		{
			Program.StartDigitalCompanion();
		}
		else if (list.Count > 0 || list2.Count > 0)
		{
			ConfirmStart.EnableWith(list, list2);
		}
		else
		{
			Program.StartGame();
		}
	}

	private ApplicationVersion GetApplicationVersionOfModule(string id)
	{
		foreach (LauncherModuleVM module in ModsData.Modules)
		{
			if (module?.Info == null)
			{
				Debug.FailedAssert("Info for module is null: " + (module?.Name ?? "---"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Launcher.Library\\ViewModels\\LauncherVM.cs", "GetApplicationVersionOfModule", 198);
			}
			if (module.Info.Id == id)
			{
				return module.Info.Version;
			}
		}
		return ApplicationVersion.Empty;
	}

	private void ExecuteConfirmUnverifiedDLLStart()
	{
		Program.StartGame();
	}

	public void ExecuteClose()
	{
		UpdateAndSaveUserModsData(IsMultiplayer);
		_onClose?.Invoke();
	}

	public void ExecuteMinimize()
	{
		_onMinimize?.Invoke();
	}

	private void Refresh()
	{
		News.Refresh(IsMultiplayer);
		ModsData.Refresh(IsDigitalCompanion, IsMultiplayer);
		VersionText = ApplicationVersion.FromParametersFile().ToString();
	}

	private string GetApplicableNewsSourceURL()
	{
		int geoID = Kernel32.GetUserGeoID(Kernel32.GeoTypeId.Nation);
		bool num = string.Equals((from x in CultureInfo.GetCultures(CultureTypes.SpecificCultures)
			select new RegionInfo(x.ToString())).FirstOrDefault((RegionInfo r) => r.GeoId == geoID)?.TwoLetterISORegionName, "cn", StringComparison.OrdinalIgnoreCase);
		bool isInPreviewMode = _newsManager.IsInPreviewMode;
		string text = (num ? "zh" : "en");
		_newsManager.UpdateLocalizationID(text);
		if (!isInPreviewMode)
		{
			return "https://taleworldswebsiteassets.blob.core.windows.net/upload/bannerlordnews/NewsFeed_" + text + ".json";
		}
		return "https://taleworldswebsiteassets.blob.core.windows.net/upload/bannerlordnews/NewsFeed_" + text + "_preview.json";
	}
}
