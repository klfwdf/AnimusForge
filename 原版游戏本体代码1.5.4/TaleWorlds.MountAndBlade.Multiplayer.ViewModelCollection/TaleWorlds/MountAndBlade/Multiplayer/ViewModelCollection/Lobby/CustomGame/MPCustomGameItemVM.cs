using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Lobby;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.CustomGame;

public class MPCustomGameItemVM : ViewModel
{
	public const string PingTimeoutText = "-";

	private readonly Action<MPCustomGameItemVM> _onSelect;

	private readonly Action<MPCustomGameItemVM> _onJoin;

	private readonly Action<MPCustomGameItemVM> _onRequestActions;

	private readonly Action<MPCustomGameItemVM> _onToggleFavorite;

	private string _randomString;

	public static readonly string OfficialServerHostName = "TaleWorlds";

	private bool _isPasswordProtected;

	private bool _isFavorite;

	private bool _isClanMatchItem;

	private bool _isOfficialServer;

	private bool _isByOfficialServerProvider;

	private bool _isCommunityServer;

	private bool _isPingInfoAvailable;

	private bool _isSelected;

	private int _playerCount;

	private int _maxPlayerCount;

	private int _spectatorCount;

	private int _maxSpectatorCount;

	private string _spectatorCountText;

	private string _spectatorCountHintText;

	private bool _showSpectatorCount;

	private bool _enableSpectators;

	private string _hostText;

	private string _nameText;

	private string _gameTypeText;

	private string _playerCountText;

	private string _pingText;

	private string _firstFactionName;

	private string _secondFactionName;

	private string _regionName;

	private string _premadeMatchTypeText;

	private BasicTooltipViewModel _loadedModulesHint;

	public GameServerEntry GameServerInfo { get; }

	public PremadeGameEntry PremadeGameInfo { get; }

	[DataSourceProperty]
	public bool IsPasswordProtected
	{
		get
		{
			return _isPasswordProtected;
		}
		set
		{
			if (value != _isPasswordProtected)
			{
				_isPasswordProtected = value;
				OnPropertyChanged("IsPasswordProtected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFavorite
	{
		get
		{
			return _isFavorite;
		}
		set
		{
			if (value != _isFavorite)
			{
				_isFavorite = value;
				OnPropertyChanged("IsFavorite");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanMatchItem
	{
		get
		{
			return _isClanMatchItem;
		}
		set
		{
			if (value != _isClanMatchItem)
			{
				_isClanMatchItem = value;
				OnPropertyChanged("IsClanMatchItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOfficialServer
	{
		get
		{
			return _isOfficialServer;
		}
		set
		{
			if (value != _isOfficialServer)
			{
				_isOfficialServer = value;
				OnPropertyChanged("IsOfficialServer");
			}
		}
	}

	[DataSourceProperty]
	public bool IsByOfficialServerProvider
	{
		get
		{
			return _isByOfficialServerProvider;
		}
		set
		{
			if (value != _isByOfficialServerProvider)
			{
				_isByOfficialServerProvider = value;
				OnPropertyChanged("IsByOfficialServerProvider");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCommunityServer
	{
		get
		{
			return _isCommunityServer;
		}
		set
		{
			if (value != _isCommunityServer)
			{
				_isCommunityServer = value;
				OnPropertyChanged("IsCommunityServer");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPingInfoAvailable
	{
		get
		{
			return _isPingInfoAvailable;
		}
		set
		{
			if (value != _isPingInfoAvailable)
			{
				_isPingInfoAvailable = value;
				OnPropertyChanged("IsPingInfoAvailable");
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
				OnPropertyChanged("IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public int PlayerCount
	{
		get
		{
			return _playerCount;
		}
		set
		{
			if (value != _playerCount)
			{
				_playerCount = value;
				OnPropertyChanged("PlayerCount");
			}
		}
	}

	[DataSourceProperty]
	public int MaxPlayerCount
	{
		get
		{
			return _maxPlayerCount;
		}
		set
		{
			if (value != _maxPlayerCount)
			{
				_maxPlayerCount = value;
				OnPropertyChanged("MaxPlayerCount");
			}
		}
	}

	[DataSourceProperty]
	public int SpectatorCount
	{
		get
		{
			return _spectatorCount;
		}
		set
		{
			if (value != _spectatorCount)
			{
				_spectatorCount = value;
				OnPropertyChanged("SpectatorCount");
			}
		}
	}

	[DataSourceProperty]
	public int MaxSpectatorCount
	{
		get
		{
			return _maxSpectatorCount;
		}
		set
		{
			if (value != _maxSpectatorCount)
			{
				_maxSpectatorCount = value;
				OnPropertyChanged("MaxSpectatorCount");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatorCountText
	{
		get
		{
			return _spectatorCountText;
		}
		set
		{
			if (value != _spectatorCountText)
			{
				_spectatorCountText = value;
				OnPropertyChanged("SpectatorCountText");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatorCountHintText
	{
		get
		{
			return _spectatorCountHintText;
		}
		set
		{
			if (value != _spectatorCountHintText)
			{
				_spectatorCountHintText = value;
				OnPropertyChanged("SpectatorCountHintText");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowSpectatorCount
	{
		get
		{
			return _showSpectatorCount;
		}
		set
		{
			if (value != _showSpectatorCount)
			{
				_showSpectatorCount = value;
				OnPropertyChanged("ShowSpectatorCount");
			}
		}
	}

	[DataSourceProperty]
	public bool EnableSpectators
	{
		get
		{
			return _enableSpectators;
		}
		set
		{
			if (value != _enableSpectators)
			{
				_enableSpectators = value;
				OnPropertyChanged("EnableSpectators");
			}
		}
	}

	[DataSourceProperty]
	public string HostText
	{
		get
		{
			return _hostText;
		}
		set
		{
			if (value != _hostText)
			{
				_hostText = value;
				OnPropertyChanged("HostText");
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
				OnPropertyChanged("NameText");
			}
		}
	}

	[DataSourceProperty]
	public string GameTypeText
	{
		get
		{
			return _gameTypeText;
		}
		set
		{
			if (value != _gameTypeText)
			{
				_gameTypeText = value;
				OnPropertyChanged("GameTypeText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerCountText
	{
		get
		{
			return _playerCountText;
		}
		set
		{
			if (value != _playerCountText)
			{
				_playerCountText = value;
				OnPropertyChanged("PlayerCountText");
			}
		}
	}

	[DataSourceProperty]
	public string PingText
	{
		get
		{
			return _pingText;
		}
		set
		{
			if (value != _pingText)
			{
				_pingText = value;
				OnPropertyChanged("PingText");
			}
		}
	}

	[DataSourceProperty]
	public string FirstFactionName
	{
		get
		{
			return _firstFactionName;
		}
		set
		{
			if (value != _firstFactionName)
			{
				_firstFactionName = value;
				OnPropertyChanged("FirstFactionName");
			}
		}
	}

	[DataSourceProperty]
	public string SecondFactionName
	{
		get
		{
			return _secondFactionName;
		}
		set
		{
			if (value != _secondFactionName)
			{
				_secondFactionName = value;
				OnPropertyChanged("SecondFactionName");
			}
		}
	}

	[DataSourceProperty]
	public string RegionName
	{
		get
		{
			return _regionName;
		}
		set
		{
			if (value != _regionName)
			{
				_regionName = value;
				OnPropertyChanged("RegionName");
			}
		}
	}

	[DataSourceProperty]
	public string PremadeMatchTypeText
	{
		get
		{
			return _premadeMatchTypeText;
		}
		set
		{
			if (value != _premadeMatchTypeText)
			{
				_premadeMatchTypeText = value;
				OnPropertyChanged("PremadeMatchTypeText");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LoadedModulesHint
	{
		get
		{
			return _loadedModulesHint;
		}
		set
		{
			if (value != _loadedModulesHint)
			{
				_loadedModulesHint = value;
				OnPropertyChangedWithValue(value, "LoadedModulesHint");
			}
		}
	}

	public MPCustomGameItemVM(GameServerEntry gameServerInfo, Action<MPCustomGameItemVM> onSelect, Action<MPCustomGameItemVM> onJoin, Action<MPCustomGameItemVM> onRequestActions, Action<MPCustomGameItemVM> onToggleFavorite)
	{
		_onSelect = onSelect;
		_onJoin = onJoin;
		_onRequestActions = onRequestActions;
		_onToggleFavorite = onToggleFavorite;
		GameServerInfo = gameServerInfo;
		string text = new TextObject("{=vBkrw5VV}Random").ToString();
		_randomString = "-- " + text + " --";
		LoadedModulesHint = new BasicTooltipViewModel(() => GetLoadedModulesTooltipProperties());
		UpdateGameServerInfo();
		UpdateIsFavorite();
	}

	public MPCustomGameItemVM(PremadeGameEntry premadeGameInfo, Action<MPCustomGameItemVM> onJoin)
	{
		_onJoin = onJoin;
		PremadeGameInfo = premadeGameInfo;
		IsClanMatchItem = true;
		IsPingInfoAvailable = false;
		string text = new TextObject("{=vBkrw5VV}Random").ToString();
		_randomString = "-- " + text + " --";
		UpdatePremadeGameInfo();
		UpdateIsFavorite();
	}

	private async void UpdateGameServerInfo()
	{
		IsPasswordProtected = GameServerInfo.PasswordProtected;
		PlayerCount = GameServerInfo.PlayerCount;
		MaxPlayerCount = GameServerInfo.MaxPlayerCount;
		NameText = GameServerInfo.ServerName;
		TextObject textObject = GameTexts.FindText("str_multiplayer_official_game_type_name", GameServerInfo.GameType);
		GameTypeText = (textObject.ToString().StartsWith("ERROR: ") ? new TextObject("{=MT4b8H9h}Unknown").ToString() : textObject.ToString());
		GameTexts.SetVariable("LEFT", PlayerCount);
		GameTexts.SetVariable("RIGHT", MaxPlayerCount);
		PlayerCountText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		SpectatorCount = GameServerInfo.SpectatorCount;
		MaxSpectatorCount = GameServerInfo.MaxSpectatorCount;
		if (MaxSpectatorCount > 0)
		{
			GameTexts.SetVariable("LEFT", SpectatorCount);
			GameTexts.SetVariable("RIGHT", MaxSpectatorCount);
			SpectatorCountText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		}
		else
		{
			SpectatorCountText = SpectatorCount.ToString();
		}
		ShowSpectatorCount = SpectatorCount > 0;
		TextObject textObject2 = new TextObject("{=vGnGbK9I}Spectators: {COUNT}");
		textObject2.SetTextVariable("COUNT", SpectatorCountText);
		SpectatorCountHintText = textObject2.ToString();
		EnableSpectators = GameServerInfo.EnableSpectators;
		IsOfficialServer = GameServerInfo.IsOfficial;
		IsByOfficialServerProvider = GameServerInfo.ByOfficialProvider;
		IsCommunityServer = !IsOfficialServer && !IsByOfficialServerProvider;
		HostText = GameServerInfo.HostName;
		IsPingInfoAvailable = MPCustomGameVM.IsPingInfoAvailable;
		await UpdatePingText();
	}

	private async Task UpdatePingText()
	{
		if (IsPingInfoAvailable)
		{
			long num = await NetworkMain.GameClient.GetPingToServer(GameServerInfo.Address);
			PingText = ((num < 0) ? "-" : num.ToString());
		}
		else
		{
			PingText = "-";
		}
	}

	private void UpdatePremadeGameInfo()
	{
		IsPasswordProtected = PremadeGameInfo.IsPasswordProtected;
		NameText = PremadeGameInfo.Name;
		string gameTypeText = (GameTypeText = GameTexts.FindText("str_multiplayer_official_game_type_name", PremadeGameInfo.GameType).ToString());
		GameTypeText = gameTypeText;
		RegionName = PremadeGameInfo.Region;
		FirstFactionName = ((PremadeGameInfo.FactionA == Parameters.RandomSelectionString) ? _randomString : PremadeGameInfo.FactionA);
		SecondFactionName = ((PremadeGameInfo.FactionB == Parameters.RandomSelectionString) ? _randomString : PremadeGameInfo.FactionB);
		HostText = OfficialServerHostName;
		IsOfficialServer = true;
		if (PremadeGameInfo.PremadeGameType == PremadeGameType.Clan)
		{
			PremadeMatchTypeText = new TextObject("{=YNkPy4ta}Clan Match").ToString();
		}
		else if (PremadeGameInfo.PremadeGameType == PremadeGameType.Practice)
		{
			PremadeMatchTypeText = new TextObject("{=H5tiRTya}Practice").ToString();
		}
	}

	private List<TooltipProperty> GetLoadedModulesTooltipProperties()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (GameServerInfo != null)
		{
			if (GameServerInfo.LoadedModules.Count > 0)
			{
				list.Add(new TooltipProperty(string.Empty, new TextObject("{=JXyxj1J5}Modules").ToString(), 1, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
				string text = " " + new TextObject("{=oYS9sabI}(optional)").ToString();
				foreach (ModuleInfoModel loadedModule in GameServerInfo.LoadedModules)
				{
					string text2 = loadedModule.Version;
					if (loadedModule.IsOptional)
					{
						text2 += text;
					}
					list.Add(new TooltipProperty(loadedModule.Name, text2, 0));
				}
			}
			TextObject textObject = (GameServerInfo.AllowsOptionalModules ? new TextObject("{=BBmEESTT}This server allows optional modules.") : new TextObject("{=sEbeLmZP}This server does not allow optional modules."));
			list.Add(new TooltipProperty("", textObject.ToString(), -1));
			if (IsCommunityServer)
			{
				list.Add(new TooltipProperty("", string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
				TextObject textObject2 = new TextObject("{=W51HSyXy}Press {VIEW_OPTIONS_KEY} to view options");
				string text3 = HotKeyManager.GetCategory("MultiplayerHotkeyCategory").GetHotKey("PreviewCosmeticItem").ToString();
				textObject2.SetTextVariable("VIEW_OPTIONS_KEY", Module.CurrentModule.GlobalTextManager.GetHotKeyGameTextFromKeyID(text3.ToLower()));
				list.Add(new TooltipProperty(string.Empty, textObject2.ToString(), -1)
				{
					OnlyShowWhenNotExtended = true
				});
			}
		}
		return list;
	}

	public void UpdateIsFavorite()
	{
		bool isFavorite = false;
		if (GameServerInfo != null)
		{
			isFavorite = MultiplayerLocalDataManager.Instance.FavoriteServers.TryGetServerData(GameServerInfo, out var _);
		}
		IsFavorite = isFavorite;
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(this);
	}

	public void ExecuteFavorite()
	{
		_onToggleFavorite?.Invoke(this);
	}

	public void ExecuteJoin()
	{
		_onJoin?.Invoke(this);
	}

	public void ExecuteViewHostOptions()
	{
		if (_onRequestActions != null)
		{
			_onRequestActions(this);
			MBInformationManager.HideInformations();
		}
	}
}
