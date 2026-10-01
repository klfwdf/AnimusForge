using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class DevWeeklyReportPopupVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action<string> _onOpenEncyclopediaLink;

	private string _titleText;

	private string _subtitleText;

	private string _bodyText;

	private string _militaryEventsText;

	private string _diplomaticAffairsText;

	private string _domesticRealmText;

	private string _closeText;

	private int _bodyFontSize;

	private int _columnBodyFontSize;

	private int _shortBodyFontSize;

	private bool _showLargePopup;

	private bool _showSingleBody;

	private bool _showChronicleColumns;

	private bool _showShortReport;

	private bool _showCloseButton;

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
	public string MilitaryEventsText
	{
		get
		{
			return _militaryEventsText;
		}
		set
		{
			if (value != _militaryEventsText)
			{
				_militaryEventsText = value;
				OnPropertyChangedWithValue(value, "MilitaryEventsText");
			}
		}
	}

	[DataSourceProperty]
	public string DiplomaticAffairsText
	{
		get
		{
			return _diplomaticAffairsText;
		}
		set
		{
			if (value != _diplomaticAffairsText)
			{
				_diplomaticAffairsText = value;
				OnPropertyChangedWithValue(value, "DiplomaticAffairsText");
			}
		}
	}

	[DataSourceProperty]
	public string DomesticRealmText
	{
		get
		{
			return _domesticRealmText;
		}
		set
		{
			if (value != _domesticRealmText)
			{
				_domesticRealmText = value;
				OnPropertyChangedWithValue(value, "DomesticRealmText");
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

	[DataSourceProperty]
	public int ColumnBodyFontSize
	{
		get
		{
			return _columnBodyFontSize;
		}
		set
		{
			if (value != _columnBodyFontSize)
			{
				_columnBodyFontSize = value;
				OnPropertyChangedWithValue(value, "ColumnBodyFontSize");
			}
		}
	}

	[DataSourceProperty]
	public int ShortBodyFontSize
	{
		get
		{
			return _shortBodyFontSize;
		}
		set
		{
			if (value != _shortBodyFontSize)
			{
				_shortBodyFontSize = value;
				OnPropertyChangedWithValue(value, "ShortBodyFontSize");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowLargePopup
	{
		get
		{
			return _showLargePopup;
		}
		set
		{
			if (value != _showLargePopup)
			{
				_showLargePopup = value;
				OnPropertyChangedWithValue(value, "ShowLargePopup");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowSingleBody
	{
		get
		{
			return _showSingleBody;
		}
		set
		{
			if (value != _showSingleBody)
			{
				_showSingleBody = value;
				OnPropertyChangedWithValue(value, "ShowSingleBody");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowChronicleColumns
	{
		get
		{
			return _showChronicleColumns;
		}
		set
		{
			if (value != _showChronicleColumns)
			{
				_showChronicleColumns = value;
				OnPropertyChangedWithValue(value, "ShowChronicleColumns");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowShortReport
	{
		get
		{
			return _showShortReport;
		}
		set
		{
			if (value != _showShortReport)
			{
				_showShortReport = value;
				OnPropertyChangedWithValue(value, "ShowShortReport");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowCloseButton
	{
		get
		{
			return _showCloseButton;
		}
		set
		{
			if (value != _showCloseButton)
			{
				_showCloseButton = value;
				OnPropertyChangedWithValue(value, "ShowCloseButton");
			}
		}
	}

	public DevWeeklyReportPopupVM(string titleText, string subtitleText, string bodyText, int bodyFontSize, Action onClose, Action<string> onOpenEncyclopediaLink, string closeText, bool useChronicleColumns = false, bool useShortReportLayout = false, bool showCloseButton = true)
	{
		_onClose = onClose;
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		TitleText = EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(string.IsNullOrWhiteSpace(titleText) ? "\u5468\u62a5\u9884\u89c8" : titleText);
		SubtitleText = EncyclopediaEntityLinkFormatter.SanitizeUntrustedRichText(subtitleText ?? "");
		// Split the stored plain report first; only the final strings assigned to RichText widgets receive native link markup.
		string normalizedBodyText = string.IsNullOrWhiteSpace(bodyText) ? "\u5f53\u524d\u5468\u62a5\u6b63\u6587\u4e3a\u7a7a\u3002" : bodyText.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		EncyclopediaEntityLinkFormatter.DisplaySession linkDisplaySession = EncyclopediaEntityLinkFormatter.CreateDisplaySession();
		BodyText = linkDisplaySession.Format(normalizedBodyText);
		BodyFontSize = Math.Max(12, Math.Min(36, bodyFontSize));
		ColumnBodyFontSize = Math.Max(13, Math.Min(22, BodyFontSize));
		ShortBodyFontSize = Math.Max(16, Math.Min(24, BodyFontSize + 1));
		bool useShortReport = useShortReportLayout && !useChronicleColumns;
		ShowLargePopup = !useShortReport;
		ShowChronicleColumns = useChronicleColumns;
		ShowShortReport = useShortReport;
		ShowSingleBody = !useChronicleColumns && !useShortReport;
		ShowCloseButton = showCloseButton;
		if (useChronicleColumns)
		{
			WeeklyReportTextHelper.SplitChronicleBodyForDisplay(normalizedBodyText, out string military, out string diplomatic, out string domestic);
			MilitaryEventsText = linkDisplaySession.Format(military);
			DiplomaticAffairsText = linkDisplaySession.Format(diplomatic);
			DomesticRealmText = linkDisplaySession.Format(domestic);
		}
		else
		{
			MilitaryEventsText = "";
			DiplomaticAffairsText = "";
			DomesticRealmText = "";
		}
		CloseText = string.IsNullOrWhiteSpace(closeText) ? "\u5173\u95ed" : closeText;
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public void ExecuteOpenEncyclopediaLink(string link)
	{
		_onOpenEncyclopediaLink?.Invoke(link);
	}
}
