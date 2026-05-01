using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class TerminalWeeklyReportBrowserPopupVM : ViewModel
{
	private readonly Action _onClose;

	private readonly List<MyBehavior.WeeklyReportBrowserCountryData> _countries;

	private string _titleText;

	private string _subtitleText;

	private string _countryPanelTitleText;

	private string _selectedCountryNameText;

	private string _selectedCountryMetaText;

	private string _emptyStateText;

	private string _closeText;

	private bool _hasReportItems;

	private bool _showEmptyState;

	private MBBindingList<TerminalWeeklyReportCountryItemVM> _countryItems;

	private MBBindingList<TerminalWeeklyReportEntryItemVM> _reportItems;

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
	public string SubtitleText
	{
		get
		{
			return _subtitleText;
		}
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, "SubtitleText");
			}
		}
	}

	[DataSourceProperty]
	public string CountryPanelTitleText
	{
		get
		{
			return _countryPanelTitleText;
		}
		set
		{
			if (value != _countryPanelTitleText)
			{
				_countryPanelTitleText = value;
				OnPropertyChangedWithValue(value, "CountryPanelTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedCountryNameText
	{
		get
		{
			return _selectedCountryNameText;
		}
		set
		{
			if (value != _selectedCountryNameText)
			{
				_selectedCountryNameText = value;
				OnPropertyChangedWithValue(value, "SelectedCountryNameText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedCountryMetaText
	{
		get
		{
			return _selectedCountryMetaText;
		}
		set
		{
			if (value != _selectedCountryMetaText)
			{
				_selectedCountryMetaText = value;
				OnPropertyChangedWithValue(value, "SelectedCountryMetaText");
			}
		}
	}

	[DataSourceProperty]
	public string EmptyStateText
	{
		get
		{
			return _emptyStateText;
		}
		set
		{
			if (value != _emptyStateText)
			{
				_emptyStateText = value;
				OnPropertyChangedWithValue(value, "EmptyStateText");
			}
		}
	}

	[DataSourceProperty]
	public string CloseText
	{
		get
		{
			return _closeText;
		}
		set
		{
			if (value != _closeText)
			{
				_closeText = value;
				OnPropertyChangedWithValue(value, "CloseText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasReportItems
	{
		get
		{
			return _hasReportItems;
		}
		set
		{
			if (value != _hasReportItems)
			{
				_hasReportItems = value;
				OnPropertyChangedWithValue(value, "HasReportItems");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowEmptyState
	{
		get
		{
			return _showEmptyState;
		}
		set
		{
			if (value != _showEmptyState)
			{
				_showEmptyState = value;
				OnPropertyChangedWithValue(value, "ShowEmptyState");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TerminalWeeklyReportCountryItemVM> CountryItems
	{
		get
		{
			return _countryItems;
		}
		set
		{
			if (value != _countryItems)
			{
				_countryItems = value;
				OnPropertyChangedWithValue(value, "CountryItems");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<TerminalWeeklyReportEntryItemVM> ReportItems
	{
		get
		{
			return _reportItems;
		}
		set
		{
			if (value != _reportItems)
			{
				_reportItems = value;
				OnPropertyChangedWithValue(value, "ReportItems");
			}
		}
	}

	public TerminalWeeklyReportBrowserPopupVM(List<MyBehavior.WeeklyReportBrowserCountryData> countries, string selectedCountryId, Action onClose)
	{
		_onClose = onClose;
		_countries = (countries ?? new List<MyBehavior.WeeklyReportBrowserCountryData>()).Where((MyBehavior.WeeklyReportBrowserCountryData x) => x != null).ToList();
		TitleText = "历史档案馆";
		SubtitleText = "左侧选择国家，右侧查看该国家全部周报。";
		CountryPanelTitleText = "国家列表";
		SelectedCountryNameText = "未选择";
		SelectedCountryMetaText = "";
		EmptyStateText = "暂无周报。";
		CloseText = "关闭";
		HasReportItems = false;
		ShowEmptyState = true;
		CountryItems = new MBBindingList<TerminalWeeklyReportCountryItemVM>();
		ReportItems = new MBBindingList<TerminalWeeklyReportEntryItemVM>();
		foreach (MyBehavior.WeeklyReportBrowserCountryData country in _countries)
		{
			CountryItems.Add(new TerminalWeeklyReportCountryItemVM(country, SelectCountry));
		}
		string text = _countries.FirstOrDefault((MyBehavior.WeeklyReportBrowserCountryData x) => !x.IsWorld && (x.Reports?.Count ?? 0) > 0)?.CountryId;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = _countries.FirstOrDefault((MyBehavior.WeeklyReportBrowserCountryData x) => (x.Reports?.Count ?? 0) > 0)?.CountryId;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = _countries.FirstOrDefault()?.CountryId ?? "";
		}
		if (!string.IsNullOrWhiteSpace(selectedCountryId))
		{
			text = selectedCountryId;
		}
		ApplyCountrySelection(text);
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	private void SelectCountry(string countryId)
	{
		string text = (countryId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		ApplyCountrySelection(text);
	}

	private void ApplyCountrySelection(string countryId)
	{
		string text = (countryId ?? "").Trim();
		MyBehavior.WeeklyReportBrowserCountryData weeklyReportBrowserCountryData = _countries.FirstOrDefault((MyBehavior.WeeklyReportBrowserCountryData x) => string.Equals((x?.CountryId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)) ?? _countries.FirstOrDefault();
		foreach (TerminalWeeklyReportCountryItemVM countryItem in CountryItems)
		{
			countryItem.IsSelected = weeklyReportBrowserCountryData != null && string.Equals((countryItem.CountryId ?? "").Trim(), (weeklyReportBrowserCountryData.CountryId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
		}
		MBBindingList<TerminalWeeklyReportEntryItemVM> mBBindingList = new MBBindingList<TerminalWeeklyReportEntryItemVM>();
		if (weeklyReportBrowserCountryData == null)
		{
			SelectedCountryNameText = "未选择";
			SelectedCountryMetaText = "";
			EmptyStateText = "暂无周报。";
			ReportItems = mBBindingList;
			HasReportItems = false;
			ShowEmptyState = true;
			return;
		}
		SelectedCountryNameText = (weeklyReportBrowserCountryData.DisplayName ?? "").Trim();
		int num = (weeklyReportBrowserCountryData.Reports != null) ? weeklyReportBrowserCountryData.Reports.Count : 0;
		SelectedCountryMetaText = ((num > 0) ? ("共 " + num + " 期周报  · ") : "这个条目当前还没有周报记录");
		foreach (MyBehavior.WeeklyReportBrowserEntryData report in (weeklyReportBrowserCountryData.Reports ?? new List<MyBehavior.WeeklyReportBrowserEntryData>()).OrderByDescending((MyBehavior.WeeklyReportBrowserEntryData x) => x?.WeekIndex ?? int.MinValue).ThenByDescending((MyBehavior.WeeklyReportBrowserEntryData x) => x?.CreatedDay ?? int.MinValue).ThenByDescending((MyBehavior.WeeklyReportBrowserEntryData x) => x?.Title ?? "", StringComparer.OrdinalIgnoreCase))
		{
			mBBindingList.Add(new TerminalWeeklyReportEntryItemVM(report));
		}
		ReportItems = mBBindingList;
		HasReportItems = ReportItems.Count > 0;
		ShowEmptyState = !HasReportItems;
		EmptyStateText = (HasReportItems ? "" : "这个国家当前还没有周报记录。");
	}
}

public sealed class TerminalWeeklyReportCountryItemVM : ViewModel
{
	private readonly Action<string> _onSelect;

	private string _countryId;

	private string _displayName;

	private string _reportCountText;

	private bool _isSelected;

	[DataSourceProperty]
	public string CountryId
	{
		get
		{
			return _countryId;
		}
		set
		{
			if (value != _countryId)
			{
				_countryId = value;
				OnPropertyChangedWithValue(value, "CountryId");
			}
		}
	}

	[DataSourceProperty]
	public string DisplayName
	{
		get
		{
			return _displayName;
		}
		set
		{
			if (value != _displayName)
			{
				_displayName = value;
				OnPropertyChangedWithValue(value, "DisplayName");
			}
		}
	}

	[DataSourceProperty]
	public string ReportCountText
	{
		get
		{
			return _reportCountText;
		}
		set
		{
			if (value != _reportCountText)
			{
				_reportCountText = value;
				OnPropertyChangedWithValue(value, "ReportCountText");
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

	public TerminalWeeklyReportCountryItemVM(MyBehavior.WeeklyReportBrowserCountryData country, Action<string> onSelect)
	{
		_onSelect = onSelect;
		CountryId = (country?.CountryId ?? "").Trim();
		DisplayName = (country?.DisplayName ?? "").Trim();
		int num = (country?.Reports != null) ? country.Reports.Count : 0;
		ReportCountText = "共 " + num + " 期";
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(CountryId ?? "");
	}
}

public sealed class TerminalWeeklyReportEntryItemVM : ViewModel
{
	private string _titleText;

	private string _weekText;

	private string _dateText;

	private string _bodyText;

	private string _tagText;

	private bool _hasTagText;

	private bool _showPositiveTag;

	private bool _showNegativeTag;

	private bool _showNeutralTag;

	private int _bodyFontSize;

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
	public string WeekText
	{
		get
		{
			return _weekText;
		}
		set
		{
			if (value != _weekText)
			{
				_weekText = value;
				OnPropertyChangedWithValue(value, "WeekText");
			}
		}
	}

	[DataSourceProperty]
	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, "DateText");
			}
		}
	}

	[DataSourceProperty]
	public string BodyText
	{
		get
		{
			return _bodyText;
		}
		set
		{
			if (value != _bodyText)
			{
				_bodyText = value;
				OnPropertyChangedWithValue(value, "BodyText");
			}
		}
	}

	[DataSourceProperty]
	public string TagText
	{
		get
		{
			return _tagText;
		}
		set
		{
			if (value != _tagText)
			{
				_tagText = value;
				OnPropertyChangedWithValue(value, "TagText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasTagText
	{
		get
		{
			return _hasTagText;
		}
		set
		{
			if (value != _hasTagText)
			{
				_hasTagText = value;
				OnPropertyChangedWithValue(value, "HasTagText");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowPositiveTag
	{
		get
		{
			return _showPositiveTag;
		}
		set
		{
			if (value != _showPositiveTag)
			{
				_showPositiveTag = value;
				OnPropertyChangedWithValue(value, "ShowPositiveTag");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowNegativeTag
	{
		get
		{
			return _showNegativeTag;
		}
		set
		{
			if (value != _showNegativeTag)
			{
				_showNegativeTag = value;
				OnPropertyChangedWithValue(value, "ShowNegativeTag");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowNeutralTag
	{
		get
		{
			return _showNeutralTag;
		}
		set
		{
			if (value != _showNeutralTag)
			{
				_showNeutralTag = value;
				OnPropertyChangedWithValue(value, "ShowNeutralTag");
			}
		}
	}

	[DataSourceProperty]
	public int BodyFontSize
	{
		get
		{
			return _bodyFontSize;
		}
		set
		{
			if (value != _bodyFontSize)
			{
				_bodyFontSize = value;
				OnPropertyChangedWithValue(value, "BodyFontSize");
			}
		}
	}

	public TerminalWeeklyReportEntryItemVM(MyBehavior.WeeklyReportBrowserEntryData entry)
	{
		TitleText = (entry?.Title ?? "").Trim();
		WeekText = "第 " + Math.Max(0, entry?.WeekIndex ?? 0) + " 周";
		DateText = (entry?.CreatedDate ?? "").Trim();
		BodyText = FormatDisplayBodyText(entry?.BodyText);
		TagText = BuildDisplayTagText(entry?.TagText, out int tagKind);
		HasTagText = !string.IsNullOrWhiteSpace(TagText);
		ShowPositiveTag = HasTagText && tagKind > 0;
		ShowNegativeTag = HasTagText && tagKind < 0;
		ShowNeutralTag = HasTagText && tagKind == 0;
		BodyFontSize = Math.Max(13, Math.Min(26, (DuelSettings.GetSettings()?.WeeklyReportPopupBodyFontSize ?? 18) - 2));
	}

	private static string BuildDisplayTagText(string rawTagText, out int tagKind)
	{
		tagKind = 0;
		string text = (rawTagText ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		for (int num = array.Length - 1; num >= 0; num--)
		{
			string text2 = (array[num] ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			string text3 = text2.ToUpperInvariant();
			if (text3.StartsWith("STAB_UP_", StringComparison.Ordinal))
			{
				tagKind = 1;
				return "稳定度上升！";
			}
			if (text3.StartsWith("STAB_DOWN_", StringComparison.Ordinal))
			{
				tagKind = -1;
				return "稳定度下降！";
			}
			if (string.Equals(text3, "STAB_FLAT", StringComparison.Ordinal))
			{
				tagKind = 0;
				return "稳定度持平。";
			}
		}
		return string.Join(" / ", array.Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)));
	}

	private static string FormatDisplayBodyText(string bodyText)
	{
		string text = (bodyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "当前这期周报还没有正文。";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 64);
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			stringBuilder.Append(c);
			if (c == '\n')
			{
				flag = false;
				continue;
			}
			if (c == '。' || c == '！' || c == '？')
			{
				stringBuilder.Append("\n\n");
				flag = false;
				continue;
			}
			if ((c == '；' || c == ';') && !flag)
			{
				stringBuilder.Append('\n');
				flag = true;
			}
		}
		return string.Join("\n", stringBuilder.ToString().Split(new char[1] { '\n' }, StringSplitOptions.None).Select((string x) => x.TrimEnd())).Trim();
	}
}
