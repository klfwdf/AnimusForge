using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.DedicatedCustomServer.ClientHelper;

public class DCSHelperVM : ViewModel
{
	private class Texts
	{
		public string Download { get; private set; }

		public string Downloading { get; private set; }

		public string Cancel { get; private set; }

		public string Close { get; private set; }

		public string Dismiss { get; private set; }

		public string SelectAll { get; private set; }

		public string UnselectAll { get; private set; }

		public string DownloadPanel { get; private set; }

		public string DownloadComplete { get; private set; }

		public string DownloadFailed { get; private set; }

		public string Yes { get; private set; }

		public string No { get; private set; }

		private TextObject PanelSubtitle => new TextObject("{=GkwbPV4s}Maps available for '{SERVER_NAME}'");

		private TextObject ProgressCounter => new TextObject("{=qMfaQ3fz}{DOWNLOADED_COUNT} of {TOTAL_COUNT}");

		private TextObject DownloadCompleteMessageSingular => new TextObject("{=wdxXylLz}The map '{MAP_NAME}' has been successfully downloaded.").SetTextVariable("MODULE_NAME", "Multiplayer");

		private TextObject DownloadCompleteMessagePlural => new TextObject("{=zifpttFx}{MAP_COUNT} maps have been successfully downloaded.").SetTextVariable("MODULE_NAME", "Multiplayer");

		private TextObject ReplacementConfirmationMessage => new TextObject("{=DluuLzfU}'{MAP_NAME}' already exists, should it be deleted and replaced? This action is IRREVERSIBLE.").SetTextVariable("MODULE_NAME", "Multiplayer");

		public Texts()
		{
			Refresh();
		}

		public void Refresh()
		{
			Download = new TextObject("{=a9HJ7K6I}Download").ToString();
			Downloading = new TextObject("{=adg8E1oP}Downloading...").ToString();
			Cancel = GameTexts.FindText("str_cancel").ToString();
			Close = GameTexts.FindText("str_close").ToString();
			Dismiss = GameTexts.FindText("str_dismiss").ToString();
			SelectAll = new TextObject("{=977S9OkT}Select all").ToString();
			UnselectAll = new TextObject("{=dOoPRBjm}Unselect all").ToString();
			DownloadPanel = new TextObject("{=vLSXeRnK}Download Panel").ToString();
			DownloadComplete = new TextObject("{=qhrPpmhu}Download Complete").ToString();
			DownloadFailed = new TextObject("{=7DKw0JRu}Download Failed").ToString();
			Yes = GameTexts.FindText("str_yes").ToString();
			No = GameTexts.FindText("str_no").ToString();
		}

		public string GetPanelSubtitle(string serverName)
		{
			return PanelSubtitle.SetTextVariable("SERVER_NAME", serverName).ToString();
		}

		public string GetProgressCounter(int downloadedCount, int totalCount)
		{
			return ProgressCounter.SetTextVariable("DOWNLOADED_COUNT", downloadedCount).SetTextVariable("TOTAL_COUNT", totalCount).ToString();
		}

		public string GetDownloadCompleteMessageSingular(string mapName)
		{
			return DownloadCompleteMessageSingular.SetTextVariable("MAP_NAME", mapName).ToString();
		}

		public string GetDownloadCompleteMessagePlural(int mapCount)
		{
			return DownloadCompleteMessagePlural.SetTextVariable("MAP_COUNT", mapCount).ToString();
		}

		public string GetReplacementConfirmationMessage(string mapName)
		{
			return ReplacementConfirmationMessage.SetTextVariable("MAP_NAME", mapName).ToString();
		}
	}

	private readonly string _hostAddress;

	private readonly string _fullName;

	private readonly Texts _texts;

	private GauntletLayer _gauntletLayer;

	private CancellationTokenSource _cancellationTokenSource;

	private bool _isLoading;

	private bool _isDownloading;

	private bool _showProgress;

	private string _panelTitleText;

	private string _downloadButtonText;

	private string _closeButtonText;

	private string _toggleSelectionButtonText;

	private string _hostAddressText;

	private string _progressCounterText;

	private string _progressText;

	private float _downloadRatio;

	private MBBindingList<DCSHelperMapItemVM> _mapList;

	public IEnumerable<DCSHelperMapItemVM> SelectedMaps => MapList.Where((DCSHelperMapItemVM map) => map.IsSelected);

	public IEnumerable<DCSHelperMapItemVM> SelectableMaps => MapList.Where((DCSHelperMapItemVM map) => !map.IsSelected);

	[DataSourceProperty]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (value != _isLoading)
			{
				_isLoading = value;
				OnPropertyChangedWithValue(value, "IsLoading");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDownloading
	{
		get
		{
			return _isDownloading;
		}
		set
		{
			if (value != _isDownloading)
			{
				_isDownloading = value;
				OnPropertyChangedWithValue(value, "IsDownloading");
				OnPropertyChangedWithValue(ReadyToDownload, "ReadyToDownload");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowProgress
	{
		get
		{
			return _showProgress;
		}
		set
		{
			if (value != _showProgress)
			{
				_showProgress = value;
				OnPropertyChangedWithValue(value, "ShowProgress");
			}
		}
	}

	[DataSourceProperty]
	public bool ReadyToDownload
	{
		get
		{
			if (!_isDownloading)
			{
				return SelectedMaps.Count() != 0;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public string PanelTitleText
	{
		get
		{
			return _panelTitleText;
		}
		set
		{
			if (value != _panelTitleText)
			{
				_panelTitleText = value;
				OnPropertyChangedWithValue(value, "PanelTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DownloadButtonText
	{
		get
		{
			return _downloadButtonText;
		}
		set
		{
			if (value != _downloadButtonText)
			{
				_downloadButtonText = value;
				OnPropertyChangedWithValue(value, "DownloadButtonText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseButtonText
	{
		get
		{
			return _closeButtonText;
		}
		set
		{
			if (value != _closeButtonText)
			{
				_closeButtonText = value;
				OnPropertyChangedWithValue(value, "CloseButtonText");
			}
		}
	}

	[DataSourceProperty]
	public string ToggleSelectionButtonText
	{
		get
		{
			return _toggleSelectionButtonText;
		}
		set
		{
			if (value != _toggleSelectionButtonText)
			{
				_toggleSelectionButtonText = value;
				OnPropertyChangedWithValue(value, "ToggleSelectionButtonText");
			}
		}
	}

	[DataSourceProperty]
	public string HostAddressText
	{
		get
		{
			return _hostAddressText;
		}
		set
		{
			if (value != _hostAddressText)
			{
				_hostAddressText = value;
				OnPropertyChangedWithValue(value, "HostAddressText");
			}
		}
	}

	[DataSourceProperty]
	public string ProgressCounterText
	{
		get
		{
			return _progressCounterText;
		}
		set
		{
			if (value != _progressCounterText)
			{
				_progressCounterText = value;
				OnPropertyChangedWithValue(value, "ProgressCounterText");
			}
		}
	}

	[DataSourceProperty]
	public string ProgressText
	{
		get
		{
			return _progressText;
		}
		set
		{
			if (value != _progressText)
			{
				_progressText = value;
				OnPropertyChangedWithValue(value, "ProgressText");
			}
		}
	}

	[DataSourceProperty]
	public float DownloadRatio
	{
		get
		{
			return _downloadRatio;
		}
		set
		{
			if (value != _downloadRatio)
			{
				_downloadRatio = value;
				OnPropertyChangedWithValue(value, "DownloadRatio");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DCSHelperMapItemVM> MapList
	{
		get
		{
			return _mapList;
		}
		set
		{
			if (value != _mapList)
			{
				_mapList = value;
				OnPropertyChangedWithValue(value, "MapList");
			}
		}
	}

	public DCSHelperVM(string hostAddress, string fullName = null)
	{
		_hostAddress = hostAddress;
		_fullName = fullName;
		_texts = new Texts();
		IsDownloading = false;
		ShowProgress = false;
		PanelTitleText = _texts.DownloadPanel;
		DownloadButtonText = _texts.Download;
		CloseButtonText = _texts.Close;
		HostAddressText = _texts.GetPanelSubtitle(Truncate(_fullName) ?? _hostAddress);
		ToggleSelectionButtonText = _texts.SelectAll;
		MapList = new MBBindingList<DCSHelperMapItemVM>();
	}

	public DCSHelperVM(GameServerEntry server)
		: this($"{server.Address}:{server.Port}", server.ServerName)
	{
	}

	private string Truncate(string str, int maxLength = 40)
	{
		if (str == null)
		{
			return str;
		}
		string text = str.Trim();
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength).TrimEnd(Array.Empty<char>()) + "...";
		}
		return text;
	}

	private async Task RefreshMapList()
	{
		MapList.Clear();
		MapListResponse mapListResponse = await DedicatedCustomServerClientHelperSubModule.Instance.GetMapListFromHost(_hostAddress);
		foreach (MapListItemResponse map in mapListResponse.Maps)
		{
			UniqueSceneId identifiers = ((map.UniqueToken != null && map.Revision != null) ? new UniqueSceneId(map.UniqueToken, map.Revision) : null);
			DCSHelperMapItemVM dCSHelperMapItemVM = new DCSHelperMapItemVM(map.Name, delegate(DCSHelperMapItemVM map)
			{
				OnMapSelected(map);
			}, map.Name == mapListResponse.CurrentlyPlaying, identifiers);
			dCSHelperMapItemVM.RefreshLocalMapData();
			MapList.Add(dCSHelperMapItemVM);
		}
	}

	private void OnMapSelected(DCSHelperMapItemVM mapItem, bool forceSelection = false)
	{
		if (!IsDownloading || forceSelection)
		{
			bool readyToDownload = ReadyToDownload;
			mapItem.IsSelected = !mapItem.IsSelected;
			ToggleSelectionButtonText = (SelectedMaps.Any() ? _texts.UnselectAll : _texts.SelectAll);
			if (readyToDownload != ReadyToDownload)
			{
				OnPropertyChangedWithValue(ReadyToDownload, "ReadyToDownload");
			}
		}
	}

	private void ToggleSelection()
	{
		IEnumerable<DCSHelperMapItemVM> enumerable = SelectedMaps;
		if (!enumerable.Any())
		{
			enumerable = SelectableMaps;
		}
		foreach (DCSHelperMapItemVM item in enumerable)
		{
			item.ExecuteToggleSelection();
		}
	}

	public async Task OpenPopup()
	{
		_gauntletLayer = new GauntletLayer("DCSHelper", 20);
		_gauntletLayer.LoadMovie("DCSHelper", this);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TopScreen.AddLayer(_gauntletLayer);
		ScreenManager.TrySetFocus(_gauntletLayer);
		IsLoading = true;
		try
		{
			await RefreshMapList();
			IsLoading = false;
		}
		catch (Exception ex)
		{
			ShowFailedToRetrieveInquiry(ex.Message);
			ExecuteClosePopup();
		}
	}

	public async Task ExecuteDownloadMap()
	{
		Queue<DCSHelperMapItemVM> remainingMaps = new Queue<DCSHelperMapItemVM>(SelectedMaps);
		int totalMapCount = remainingMaps.Count;
		if (totalMapCount == 0)
		{
			return;
		}
		List<DCSHelperMapItemVM> downloadedMaps = new List<DCSHelperMapItemVM>();
		IsDownloading = true;
		ShowProgress = false;
		DownloadButtonText = _texts.Downloading;
		CloseButtonText = _texts.Cancel;
		while (remainingMaps.Any())
		{
			DCSHelperMapItemVM mapItem = remainingMaps.Dequeue();
			ModLogger.Log("Download Panel: Downloading map '" + mapItem.MapName + "' from host '" + _hostAddress + "'");
			ProgressCounterText = _texts.GetProgressCounter(downloadedMaps.Count + 1, totalMapCount);
			try
			{
				bool flag = !ModHelpers.DoesSceneFolderAlreadyExist(mapItem.MapName);
				if (!flag)
				{
					flag = await WaitForConfirmationToReplace(mapItem.MapName);
				}
				if (flag)
				{
					using (_cancellationTokenSource = new CancellationTokenSource())
					{
						await Task.Run(async delegate
						{
							await DedicatedCustomServerClientHelperSubModule.Instance.DownloadMapFromHost(_hostAddress, mapItem.MapName, replaceExisting: true, new Progress<ProgressUpdate>(OnProgressUpdate), _cancellationTokenSource.Token);
						});
					}
					downloadedMaps.Add(mapItem);
					OnMapSelected(mapItem, forceSelection: true);
					mapItem.RefreshLocalMapData();
				}
				if (!remainingMaps.Any())
				{
					ShowDownloadCompleteInquiry(downloadedMaps);
				}
			}
			catch (Exception ex)
			{
				remainingMaps.Clear();
				CancellationTokenSource cancellationTokenSource = _cancellationTokenSource;
				if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
				{
					ShowDownloadFailedInquiry(ex.Message);
				}
			}
			_cancellationTokenSource = null;
		}
		IsDownloading = false;
		DownloadButtonText = _texts.Download;
		CloseButtonText = _texts.Close;
	}

	private Task<bool> WaitForConfirmationToReplace(string mapName)
	{
		TaskCompletionSource<bool> taskSource = new TaskCompletionSource<bool>();
		InformationManager.ShowInquiry(new InquiryData(_texts.DownloadPanel, _texts.GetReplacementConfirmationMessage(mapName), isAffirmativeOptionShown: true, isNegativeOptionShown: true, _texts.Yes, _texts.No, getAction(replace: true), getAction(replace: false)));
		return taskSource.Task;
		Action getAction(bool replace)
		{
			return delegate
			{
				taskSource.SetResult(replace);
			};
		}
	}

	private void ShowDownloadCompleteInquiry(List<DCSHelperMapItemVM> downloadedMaps)
	{
		InformationManager.ShowInquiry(new InquiryData(_texts.DownloadComplete, (downloadedMaps.Count == 1) ? _texts.GetDownloadCompleteMessageSingular(downloadedMaps.Single().MapName) : _texts.GetDownloadCompleteMessagePlural(downloadedMaps.Count), isAffirmativeOptionShown: true, isNegativeOptionShown: false, _texts.Dismiss, "", null, null));
	}

	private void ShowDownloadFailedInquiry(string reason)
	{
		InformationManager.ShowInquiry(new InquiryData(_texts.DownloadFailed, reason, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", _texts.Dismiss, null, null));
	}

	private void ShowFailedToRetrieveInquiry(string reason)
	{
		InformationManager.ShowInquiry(new InquiryData(_texts.DownloadPanel, reason, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", _texts.Dismiss, null, null));
	}

	public void ExecuteCloseOrCancel()
	{
		if (IsDownloading)
		{
			ExecuteCancelDownload();
		}
		else
		{
			ExecuteClosePopup();
		}
	}

	public void ExecuteClosePopup()
	{
		ScreenManager.TryLoseFocus(_gauntletLayer);
		ScreenManager.TopScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
	}

	public void ExecuteCancelDownload()
	{
		if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
		{
			try
			{
				_cancellationTokenSource.Cancel();
			}
			catch (Exception ex)
			{
				ModLogger.Warn("Failed to cancel download: " + ex.Message);
			}
		}
	}

	private void OnProgressUpdate(ProgressUpdate update)
	{
		ProgressText = update.MegaBytesRead.ToString("0.##") + " MB / " + update.TotalMegaBytes.ToString("0.##") + " MB (" + (update.ProgressRatio * 100f).ToString("0.##") + "%)";
		DownloadRatio = update.ProgressRatio;
		ShowProgress = true;
	}
}
