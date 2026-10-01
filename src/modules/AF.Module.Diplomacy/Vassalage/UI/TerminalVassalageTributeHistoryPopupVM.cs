using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class TerminalVassalageTributeHistoryPopupVM : ViewModel
{
	private readonly Action _onClose;

	private string _titleText;

	private string _subtitleText;

	private string _emptyStateText;

	private string _closeText;

	private bool _hasRecords;

	private bool _showEmptyState;

	private MBBindingList<TerminalVassalageTributeHistoryRecordItemVM> _recordItems;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	[DataSourceProperty]
	public string EmptyStateText
	{
		get => _emptyStateText;
		set
		{
			if (value != _emptyStateText)
			{
				_emptyStateText = value;
				OnPropertyChangedWithValue(value, nameof(EmptyStateText));
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get => _closeText;
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, nameof(CloseText));
			}
		}
	}

	[DataSourceProperty]
	public bool HasRecords
	{
		get => _hasRecords;
		set
		{
			if (value != _hasRecords)
			{
				_hasRecords = value;
				OnPropertyChangedWithValue(value, nameof(HasRecords));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowEmptyState
	{
		get => _showEmptyState;
		set
		{
			if (value != _showEmptyState)
			{
				_showEmptyState = value;
				OnPropertyChangedWithValue(value, nameof(ShowEmptyState));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TerminalVassalageTributeHistoryRecordItemVM> RecordItems
	{
		get => _recordItems;
		set
		{
			if (value != _recordItems)
			{
				_recordItems = value;
				OnPropertyChangedWithValue(value, nameof(RecordItems));
			}
		}
	}

	public TerminalVassalageTributeHistoryPopupVM(TerminalTributaryPaymentHistoryData data, Action onClose)
	{
		_onClose = onClose;
		TerminalTributaryPaymentHistoryData source = data ?? new TerminalTributaryPaymentHistoryData();
		TitleText = string.IsNullOrWhiteSpace(source.TitleText) ? "贡赋记录" : source.TitleText.Trim();
		SubtitleText = (source.SubtitleText ?? "").Trim();
		EmptyStateText = string.IsNullOrWhiteSpace(source.EmptyStateText) ? "尚无贡赋入库记录。" : source.EmptyStateText.Trim();
		CloseText = string.IsNullOrWhiteSpace(source.CloseText) ? "返回臣属国管理" : source.CloseText.Trim();
		RecordItems = new MBBindingList<TerminalVassalageTributeHistoryRecordItemVM>();
		if (source.Records != null)
		{
			foreach (TerminalTributaryPaymentRecordData record in source.Records)
			{
				if (record != null)
				{
					RecordItems.Add(new TerminalVassalageTributeHistoryRecordItemVM(record));
				}
			}
		}
		HasRecords = RecordItems.Count > 0;
		ShowEmptyState = !HasRecords;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}
}

public sealed class TerminalVassalageTributeHistoryRecordItemVM : ViewModel
{
	private string _dateText;

	private string _tributeValueText;

	private string _playerGainSummaryText;

	private string _playerSettlementGainText;

	private string _tributaryCostText;

	[DataSourceProperty]
	public string DateText
	{
		get => _dateText;
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, nameof(DateText));
			}
		}
	}

	[DataSourceProperty]
	public string TributeValueText
	{
		get => _tributeValueText;
		set
		{
			if (value != _tributeValueText)
			{
				_tributeValueText = value;
				OnPropertyChangedWithValue(value, nameof(TributeValueText));
			}
		}
	}

	[DataSourceProperty]
	public string PlayerGainSummaryText
	{
		get => _playerGainSummaryText;
		set
		{
			if (value != _playerGainSummaryText)
			{
				_playerGainSummaryText = value;
				OnPropertyChangedWithValue(value, nameof(PlayerGainSummaryText));
			}
		}
	}

	[DataSourceProperty]
	public string PlayerSettlementGainText
	{
		get => _playerSettlementGainText;
		set
		{
			if (value != _playerSettlementGainText)
			{
				_playerSettlementGainText = value;
				OnPropertyChangedWithValue(value, nameof(PlayerSettlementGainText));
			}
		}
	}

	[DataSourceProperty]
	public string TributaryCostText
	{
		get => _tributaryCostText;
		set
		{
			if (value != _tributaryCostText)
			{
				_tributaryCostText = value;
				OnPropertyChangedWithValue(value, nameof(TributaryCostText));
			}
		}
	}

	public TerminalVassalageTributeHistoryRecordItemVM(TerminalTributaryPaymentRecordData record)
	{
		DateText = (record?.DateText ?? "未知日期").Trim();
		TributeValueText = (record?.TributeValueText ?? "").Trim();
		PlayerGainSummaryText = (record?.PlayerGainSummaryText ?? "").Trim();
		PlayerSettlementGainText = (record?.PlayerSettlementGainText ?? "").Trim();
		TributaryCostText = (record?.TributaryCostText ?? "").Trim();
	}
}
