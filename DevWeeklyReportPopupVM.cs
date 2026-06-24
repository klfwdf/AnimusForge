using System;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class DevWeeklyReportPopupVM : ViewModel
{
	private readonly Action _onClose;

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

	public DevWeeklyReportPopupVM(string titleText, string subtitleText, string bodyText, int bodyFontSize, Action onClose, string closeText, bool useChronicleColumns = false, bool useShortReportLayout = false, bool showCloseButton = true)
	{
		_onClose = onClose;
		TitleText = string.IsNullOrWhiteSpace(titleText) ? "\u5468\u62a5\u9884\u89c8" : titleText;
		SubtitleText = subtitleText ?? "";
		BodyText = string.IsNullOrWhiteSpace(bodyText) ? "\u5f53\u524d\u5468\u62a5\u6b63\u6587\u4e3a\u7a7a\u3002" : bodyText.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
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
			SplitChronicleBody(BodyText, out string military, out string diplomatic, out string domestic);
			MilitaryEventsText = military;
			DiplomaticAffairsText = diplomatic;
			DomesticRealmText = domestic;
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

	private static void SplitChronicleBody(string bodyText, out string military, out string diplomatic, out string domestic)
	{
		const string emptySection = "\u672c\u5468\u672a\u89c1\u660e\u663e\u53d8\u5316\u3002";
		string text = NormalizeReportBodyText(bodyText);
		StringBuilder militaryBuilder = new StringBuilder();
		StringBuilder diplomaticBuilder = new StringBuilder();
		StringBuilder domesticBuilder = new StringBuilder();
		StringBuilder prefaceBuilder = new StringBuilder();
		int currentSection = 0;
		bool foundSection = false;
		string[] lines = text.Split(new char[1] { '\n' }, StringSplitOptions.None);
		foreach (string rawLine in lines)
		{
			string line = rawLine ?? "";
			if (TryConsumeSectionHeading(line, out int section, out string remainder))
			{
				currentSection = section;
				foundSection = true;
				if (!string.IsNullOrWhiteSpace(remainder))
				{
					AppendSectionLine(GetSectionBuilder(section, militaryBuilder, diplomaticBuilder, domesticBuilder), remainder);
				}
				continue;
			}
			if (!foundSection || currentSection == 0)
			{
				AppendSectionLine(prefaceBuilder, line);
				continue;
			}
			AppendSectionLine(GetSectionBuilder(currentSection, militaryBuilder, diplomaticBuilder, domesticBuilder), line);
		}
		if (!foundSection)
		{
			military = string.IsNullOrWhiteSpace(text) ? emptySection : ("\u8be5\u671f\u65e7\u5468\u62a5\u5c1a\u672a\u6309\u4e09\u7c7b\u4fdd\u5b58\uff0c\u539f\u6587\u5982\u4e0b\uff1a\n\n" + text.Trim());
			diplomatic = emptySection;
			domestic = emptySection;
			return;
		}
		string preface = prefaceBuilder.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(preface))
		{
			AppendSectionLine(militaryBuilder, preface);
		}
		military = NormalizeSectionText(militaryBuilder.ToString(), emptySection);
		diplomatic = NormalizeSectionText(diplomaticBuilder.ToString(), emptySection);
		domestic = NormalizeSectionText(domesticBuilder.ToString(), emptySection);
	}

	private static string NormalizeReportBodyText(string bodyText)
	{
		string text = (bodyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (text.StartsWith("[REPORT]", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("[REPORT]".Length).Trim();
		}
		int tagsIndex = text.IndexOf("[TAGS]", StringComparison.OrdinalIgnoreCase);
		if (tagsIndex >= 0)
		{
			text = text.Substring(0, tagsIndex).Trim();
		}
		return text;
	}

	private static StringBuilder GetSectionBuilder(int section, StringBuilder military, StringBuilder diplomatic, StringBuilder domestic)
	{
		if (section == 2)
		{
			return diplomatic;
		}
		if (section == 3)
		{
			return domestic;
		}
		return military;
	}

	private static void AppendSectionLine(StringBuilder builder, string line)
	{
		if (builder == null)
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append('\n');
		}
		builder.Append(line ?? "");
	}

	private static string NormalizeSectionText(string text, string fallback)
	{
		string normalized = (text ?? "").Trim();
		return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
	}

	private static bool TryConsumeSectionHeading(string line, out int section, out string remainder)
	{
		section = 0;
		remainder = line ?? "";
		string text = (line ?? "").TrimStart();
		if (TryConsumeHeading(text, 1, "\u519b\u4e8b\u4e8b\u4ef6", out section, out remainder)
			|| TryConsumeHeading(text, 1, "\u519b\u4e8b\u4e8b\u4ef6\u4e0e\u6218\u5f79", out section, out remainder)
			|| TryConsumeHeading(text, 2, "\u5916\u4ea4\u4e8b\u4ef6", out section, out remainder)
			|| TryConsumeHeading(text, 2, "\u5916\u4ea4\u4e8b\u52a1", out section, out remainder)
			|| TryConsumeHeading(text, 3, "\u9886\u5730\u5185\u4e8b\u4ef6", out section, out remainder)
			|| TryConsumeHeading(text, 3, "\u9886\u5185\u4e8b\u4ef6", out section, out remainder)
			|| TryConsumeHeading(text, 3, "\u5185\u653f\u4e8b\u4ef6", out section, out remainder))
		{
			return true;
		}
		section = 0;
		remainder = line ?? "";
		return false;
	}

	private static bool TryConsumeHeading(string text, int targetSection, string heading, out int section, out string remainder)
	{
		section = 0;
		remainder = text ?? "";
		string source = text ?? "";
		string bracketed = "\u3010" + heading + "\u3011";
		if (source.StartsWith(bracketed, StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(bracketed.Length).TrimStart();
			return true;
		}
		string square = "[" + heading + "]";
		if (source.StartsWith(square, StringComparison.OrdinalIgnoreCase))
		{
			section = targetSection;
			remainder = source.Substring(square.Length).TrimStart();
			return true;
		}
		if (source.StartsWith(heading + "\uff1a", StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(heading.Length + 1).TrimStart();
			return true;
		}
		if (source.StartsWith(heading + ":", StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = source.Substring(heading.Length + 1).TrimStart();
			return true;
		}
		if (string.Equals(source.Trim(), heading, StringComparison.Ordinal))
		{
			section = targetSection;
			remainder = "";
			return true;
		}
		return false;
	}
}
