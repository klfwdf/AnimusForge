using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;

public class InitialMenuAnnouncementVM : ViewModel
{
	private struct AnnouncementInformation
	{
		public string ImageUrl { get; set; }

		public string LinkUrl { get; set; }

		public List<string> ExcludedModules { get; set; }
	}

	private bool _isFetchingData;

	private bool _needsRefresh;

	private Dictionary<string, Dictionary<string, AnnouncementInformation>> _announcementInfo;

	private string _clickLink;

	private bool _isVisible;

	private bool _isLinkAvailable;

	private string _imageSourcePath;

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLinkAvailable
	{
		get
		{
			return _isLinkAvailable;
		}
		set
		{
			if (value != _isLinkAvailable)
			{
				_isLinkAvailable = value;
				OnPropertyChangedWithValue(value, "IsLinkAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string ImageSourcePath
	{
		get
		{
			return _imageSourcePath;
		}
		set
		{
			if (value != _imageSourcePath)
			{
				_imageSourcePath = value;
				OnPropertyChangedWithValue(value, "ImageSourcePath");
			}
		}
	}

	public InitialMenuAnnouncementVM()
	{
		Refresh();
	}

	public void Tick()
	{
		if (_needsRefresh && !_isFetchingData)
		{
			try
			{
				SetDataFromAnnouncementInfo();
			}
			catch (Exception)
			{
				_announcementInfo = null;
				ImageSourcePath = null;
				_clickLink = null;
			}
			IsVisible = !string.IsNullOrEmpty(ImageSourcePath);
			IsLinkAvailable = !string.IsNullOrEmpty(_clickLink);
			_needsRefresh = false;
		}
	}

	private void SetDataFromAnnouncementInfo()
	{
		if (_announcementInfo != null && _announcementInfo.TryGetValue(GetPlatformString(), out var value) && value != null && value.Count > 0)
		{
			if (!value.TryGetValue(GetLanguageString(), out var value2) && !value.TryGetValue("en", out value2))
			{
				value2 = value.Values.FirstOrDefault();
			}
			bool flag = true;
			List<string> excludedModules = value2.ExcludedModules;
			if (excludedModules != null && excludedModules.Count > 0 && excludedModules.TrueForAll((string module) => ModuleHelper.IsModuleActive(module)))
			{
				flag = false;
			}
			if (flag)
			{
				ImageSourcePath = value2.ImageUrl;
				_clickLink = value2.LinkUrl;
			}
			else
			{
				ImageSourcePath = null;
				_clickLink = null;
			}
		}
		else
		{
			ImageSourcePath = null;
			_clickLink = null;
		}
	}

	public void Refresh()
	{
		if (!_isFetchingData)
		{
			if (_announcementInfo != null)
			{
				_needsRefresh = true;
				return;
			}
			_isFetchingData = true;
			RefreshAux();
		}
	}

	private async void RefreshAux()
	{
		try
		{
			_announcementInfo = Common.DeserializeObjectFromJson<Dictionary<string, Dictionary<string, AnnouncementInformation>>>(await HttpHelper.DownloadStringTaskAsync("https://taleworldswebsiteassets.blob.core.windows.net/upload/upsell/data.json"));
		}
		catch (Exception)
		{
		}
		_isFetchingData = false;
		_needsRefresh = true;
	}

	public void ExecuteNavigateToLink()
	{
		if (IsLinkAvailable && !string.IsNullOrEmpty(_clickLink))
		{
			if (ApplicationPlatform.CurrentPlatform == Platform.Durango || ApplicationPlatform.CurrentPlatform == Platform.Orbis)
			{
				Utilities.OpenConsoleStorePage(_clickLink);
			}
			else if (!PlatformServices.Instance.ShowOverlayForWebPage(_clickLink).Result)
			{
				Process.Start(new ProcessStartInfo(_clickLink)
				{
					UseShellExecute = true
				});
			}
		}
	}

	private string GetPlatformString()
	{
		return ApplicationPlatform.CurrentPlatform switch
		{
			Platform.WindowsSteam => "steam", 
			Platform.WindowsGOG => "gog", 
			Platform.WindowsEpic => "epic", 
			Platform.GDKDesktop => "gdk", 
			Platform.Durango => "xbone", 
			Platform.Orbis => "ps4", 
			_ => string.Empty, 
		};
	}

	private string GetLanguageString()
	{
		return BannerlordConfig.Language switch
		{
			"English" => "en", 
			"Português (BR)" => "br", 
			"简体中文" => "cns", 
			"繁體中文" => "cnt", 
			"Deutsch" => "de", 
			"Français" => "fr", 
			"Italiano" => "it", 
			"日本語" => "jp", 
			"한국어" => "kr", 
			"Polski" => "pl", 
			"Русский" => "ru", 
			"Español (LA)" => "sp", 
			"Türkçe" => "tr", 
			_ => "en", 
		};
	}
}
