using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MultiplayerReportPlayerVM : ViewModel
{
	private readonly Action<string, PlayerId, string, PlayerReportType, string> _onReportDone;

	private readonly Action _onCancel;

	private string _currentGameId = string.Empty;

	private PlayerId _currentPlayerId;

	private string _currentPlayerName = string.Empty;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private string _reportMessage;

	private string _reportReasonText;

	private string _doneText;

	private string _muteDescriptionText;

	private bool _canSendReport;

	private bool _isRequestedFromMission;

	private HintViewModel _disabledReasonHint;

	private SelectorVM<SelectorItemVM> _reportReasons;

	[DataSourceProperty]
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
				OnPropertyChanged("CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
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
				OnPropertyChanged("DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public string ReportMessage
	{
		get
		{
			return _reportMessage;
		}
		set
		{
			if (_reportMessage != value)
			{
				_reportMessage = value;
				OnPropertyChangedWithValue(value, "ReportMessage");
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
			if (_doneText != value)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
			}
		}
	}

	[DataSourceProperty]
	public string ReportReasonText
	{
		get
		{
			return _reportReasonText;
		}
		set
		{
			if (_reportReasonText != value)
			{
				_reportReasonText = value;
				OnPropertyChangedWithValue(value, "ReportReasonText");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSendReport
	{
		get
		{
			return _canSendReport;
		}
		set
		{
			if (_canSendReport != value)
			{
				_canSendReport = value;
				OnPropertyChangedWithValue(value, "CanSendReport");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRequestedFromMission
	{
		get
		{
			return _isRequestedFromMission;
		}
		set
		{
			if (value != _isRequestedFromMission)
			{
				_isRequestedFromMission = value;
				OnPropertyChangedWithValue(value, "IsRequestedFromMission");
			}
		}
	}

	[DataSourceProperty]
	public string MuteDescriptionText
	{
		get
		{
			return _muteDescriptionText;
		}
		set
		{
			if (value != _muteDescriptionText)
			{
				_muteDescriptionText = value;
				OnPropertyChangedWithValue(value, "MuteDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> ReportReasons
	{
		get
		{
			return _reportReasons;
		}
		set
		{
			if (_reportReasons != value)
			{
				_reportReasons = value;
				OnPropertyChangedWithValue(value, "ReportReasons");
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
			if (_disabledReasonHint != value)
			{
				_disabledReasonHint = value;
				OnPropertyChangedWithValue(value, "DisabledReasonHint");
			}
		}
	}

	public MultiplayerReportPlayerVM(Action<string, PlayerId, string, PlayerReportType, string> onReportDone, Action onCancel)
	{
		_onReportDone = onReportDone;
		_onCancel = onCancel;
		ReportReasons = new SelectorVM<SelectorItemVM>(0, null);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DisabledReasonHint = new HintViewModel(new TextObject("{=klkYFik9}You've already reported this player."));
		ReportReasonText = new TextObject("{=cw5QyeRU}Report Reason").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		MuteDescriptionText = new TextObject("{=gGa3ZhqN}This player will be muted automatically.").ToString();
		List<string> list = new List<string> { new TextObject("{=koX9okuG}None").ToString() };
		foreach (object value in Enum.GetValues(typeof(PlayerReportType)))
		{
			list.Add(GameTexts.FindText("str_multiplayer_report_reason", ((int)value).ToString()).ToString());
		}
		ReportReasons.Refresh(list, 0, OnReasonSelectionChange);
	}

	private void OnReasonSelectionChange(SelectorVM<SelectorItemVM> obj)
	{
		CanSendReport = obj.SelectedItem != null && obj.SelectedIndex != 0;
	}

	public void OpenNewReportWithGamePlayerId(string gameId, PlayerId playerId, string playerName, bool isRequestedFromMission)
	{
		ReportReasons.SelectedIndex = 0;
		ReportMessage = "";
		_currentGameId = gameId;
		_currentPlayerId = playerId;
		_currentPlayerName = playerName;
		IsRequestedFromMission = isRequestedFromMission;
	}

	public void ExecuteDone()
	{
		if (CanSendReport && ReportReasons.SelectedIndex > 0 && _currentGameId != string.Empty && _currentPlayerId != PlayerId.Empty)
		{
			if (ReportMessage.Length > 500)
			{
				ReportMessage = ReportMessage.Substring(0, 500);
			}
			_onReportDone?.DynamicInvokeWithLog(_currentGameId, _currentPlayerId, _currentPlayerName, (PlayerReportType)(ReportReasons.SelectedIndex - 1), ReportMessage);
			_currentGameId = string.Empty;
			_currentPlayerId = PlayerId.Empty;
			_currentPlayerName = string.Empty;
		}
	}

	public void ExecuteCancel()
	{
		_onCancel?.DynamicInvokeWithLog();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
