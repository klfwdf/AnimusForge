using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class DevLargeSelectionPopupVM : ViewModel
{
	private readonly Action<string> _onSelect;

	private readonly Action _onCancel;

	private string _titleText;

	private string _subtitleText;

	private string _bodyText;

	private string _cancelText;

	private string _emptyStateText;

	private bool _hasItems;

	private bool _hasBodyText;

	private bool _showTextOnly;

	private int _bodyFontSize;

	private MBBindingList<DevLargeSelectionItemVM> _items;

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
	public string BodyText
	{
		get => _bodyText;
		set
		{
			if (value != _bodyText)
			{
				_bodyText = value;
				OnPropertyChangedWithValue(value, nameof(BodyText));
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get => _cancelText;
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, nameof(CancelText));
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
	public bool HasItems
	{
		get => _hasItems;
		set
		{
			if (value != _hasItems)
			{
				_hasItems = value;
				OnPropertyChangedWithValue(value, nameof(HasItems));
			}
		}
	}

	[DataSourceProperty]
	public bool HasBodyText
	{
		get => _hasBodyText;
		set
		{
			if (value != _hasBodyText)
			{
				_hasBodyText = value;
				OnPropertyChangedWithValue(value, nameof(HasBodyText));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowTextOnly
	{
		get => _showTextOnly;
		set
		{
			if (value != _showTextOnly)
			{
				_showTextOnly = value;
				OnPropertyChangedWithValue(value, nameof(ShowTextOnly));
			}
		}
	}

	[DataSourceProperty]
	public int BodyFontSize
	{
		get => _bodyFontSize;
		set
		{
			if (value != _bodyFontSize)
			{
				_bodyFontSize = value;
				OnPropertyChangedWithValue(value, nameof(BodyFontSize));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<DevLargeSelectionItemVM> Items
	{
		get => _items;
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, nameof(Items));
			}
		}
	}

	public DevLargeSelectionPopupVM(string titleText, string subtitleText, string bodyText, IReadOnlyList<DevLargeSelectionPopup.Option> options, Action<string> onSelect, Action onCancel, string cancelText)
	{
		_onSelect = onSelect;
		_onCancel = onCancel;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "AnimusForge" : titleText.Trim();
		SubtitleText = subtitleText ?? "";
		BodyText = string.IsNullOrWhiteSpace(bodyText) ? "" : bodyText.Trim();
		CancelText = string.IsNullOrWhiteSpace(cancelText) ? "返回" : cancelText.Trim();
		EmptyStateText = string.IsNullOrWhiteSpace(BodyText) ? "暂无可显示内容。" : BodyText;
		BodyFontSize = ResolveBodyFontSize(BodyText);
		Items = new MBBindingList<DevLargeSelectionItemVM>();
		foreach (DevLargeSelectionPopup.Option option in options ?? Array.Empty<DevLargeSelectionPopup.Option>())
		{
			if (option != null)
			{
				Items.Add(new DevLargeSelectionItemVM(option, ExecuteSelectOption));
			}
		}
		HasItems = Items.Count > 0;
		HasBodyText = !string.IsNullOrWhiteSpace(BodyText);
		ShowTextOnly = !HasItems;
	}

	private static int ResolveBodyFontSize(string bodyText)
	{
		int length = string.IsNullOrWhiteSpace(bodyText) ? 0 : bodyText.Length;
		if (length > 6000)
		{
			return 18;
		}
		if (length > 2400)
		{
			return 20;
		}
		return 22;
	}

	private void ExecuteSelectOption(string id)
	{
		_onSelect?.Invoke(id ?? "");
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}
}

public sealed class DevLargeSelectionItemVM : ViewModel
{
	private readonly Action<string> _onSelect;

	private string _id;

	private string _titleText;

	private string _detailText;

	private string _metaText;

	private bool _isDanger;

	private bool _isPrimary;

	public DevLargeSelectionItemVM(DevLargeSelectionPopup.Option option, Action<string> onSelect)
	{
		_onSelect = onSelect;
		Id = option?.Id ?? "";
		TitleText = option?.TitleText ?? "";
		DetailText = option?.DetailText ?? "";
		MetaText = option?.MetaText ?? "";
		IsDanger = option?.IsDanger == true;
		IsPrimary = option?.IsPrimary == true;
	}

	[DataSourceProperty]
	public string Id
	{
		get => _id;
		set
		{
			if (value != _id)
			{
				_id = value;
				OnPropertyChangedWithValue(value, nameof(Id));
			}
		}
	}

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
	public string DetailText
	{
		get => _detailText;
		set
		{
			if (value != _detailText)
			{
				_detailText = value;
				OnPropertyChangedWithValue(value, nameof(DetailText));
				OnPropertyChangedWithValue(ShowDetailText, nameof(ShowDetailText));
			}
		}
	}

	[DataSourceProperty]
	public string MetaText
	{
		get => _metaText;
		set
		{
			if (value != _metaText)
			{
				_metaText = value;
				OnPropertyChangedWithValue(value, nameof(MetaText));
				OnPropertyChangedWithValue(ShowMetaText, nameof(ShowMetaText));
			}
		}
	}

	[DataSourceProperty]
	public bool IsDanger
	{
		get => _isDanger;
		set
		{
			if (value != _isDanger)
			{
				_isDanger = value;
				OnPropertyChangedWithValue(value, nameof(IsDanger));
			}
		}
	}

	[DataSourceProperty]
	public bool IsPrimary
	{
		get => _isPrimary;
		set
		{
			if (value != _isPrimary)
			{
				_isPrimary = value;
				OnPropertyChangedWithValue(value, nameof(IsPrimary));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowDetailText => !string.IsNullOrWhiteSpace(DetailText);

	[DataSourceProperty]
	public bool ShowMetaText => !string.IsNullOrWhiteSpace(MetaText);

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(Id ?? "");
	}
}
