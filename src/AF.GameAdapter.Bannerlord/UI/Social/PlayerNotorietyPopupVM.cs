using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class PlayerNotorietyPopupVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action _onEdit;

	private readonly Action _onToggleLowProfile;

	private string _historyText;

	private string _centuryText;

	private string _editText;

	private string _lowProfileToggleText;

	private bool _showEditButton;

	private bool _isLowProfileModeEnabled;

	private float _worldFillPercent;

	private MBBindingList<PlayerNotorietyCultureItemVM> _cultureItems;

	public PlayerNotorietyPopupVM(PlayerNotorietyPopupData data, Action onClose, Action onEdit, Action onToggleLowProfile)
	{
		_onClose = onClose;
		_onEdit = onEdit;
		_onToggleLowProfile = onToggleLowProfile;
		ApplyData(data);
	}

	public void ApplyData(PlayerNotorietyPopupData data)
	{
		PlayerNotorietyPopupData source = data ?? new PlayerNotorietyPopupData();
		HistoryText = string.IsNullOrWhiteSpace(source.HistoryText) ? "尚无可展示的公开履历。" : source.HistoryText.Trim();
		CenturyText = string.IsNullOrWhiteSpace(source.CenturyText) ? "" : source.CenturyText.Trim();
		WorldFillPercent = ClampPercent(source.WorldFillPercent);
		ShowEditButton = source.ShowEditButton;
		EditText = string.IsNullOrWhiteSpace(source.EditText) ? "编辑履历" : source.EditText.Trim();
		IsLowProfileModeEnabled = source.IsLowProfileModeEnabled;
		LowProfileToggleText = string.IsNullOrWhiteSpace(source.LowProfileToggleText)
			? (source.IsLowProfileModeEnabled ? "关闭低调模式" : "开启低调模式")
			: source.LowProfileToggleText.Trim();
		CultureItems = new MBBindingList<PlayerNotorietyCultureItemVM>();
		foreach (PlayerNotorietyCultureRowData row in source.CultureRows ?? Array.Empty<PlayerNotorietyCultureRowData>())
		{
			if (row != null)
			{
				CultureItems.Add(new PlayerNotorietyCultureItemVM(row));
			}
		}
	}

	[DataSourceProperty]
	public string HistoryText
	{
		get => _historyText;
		set
		{
			if (value != _historyText)
			{
				_historyText = value;
				OnPropertyChangedWithValue(value, nameof(HistoryText));
			}
		}
	}

	[DataSourceProperty]
	public string CenturyText
	{
		get => _centuryText;
		set
		{
			if (value != _centuryText)
			{
				_centuryText = value;
				OnPropertyChangedWithValue(value, nameof(CenturyText));
			}
		}
	}

	[DataSourceProperty]
	public string EditText
	{
		get => _editText;
		set
		{
			if (value != _editText)
			{
				_editText = value;
				OnPropertyChangedWithValue(value, nameof(EditText));
			}
		}
	}

	[DataSourceProperty]
	public string LowProfileToggleText
	{
		get => _lowProfileToggleText;
		set
		{
			if (value != _lowProfileToggleText)
			{
				_lowProfileToggleText = value;
				OnPropertyChangedWithValue(value, nameof(LowProfileToggleText));
			}
		}
	}

	[DataSourceProperty]
	public bool ShowEditButton
	{
		get => _showEditButton;
		set
		{
			if (value != _showEditButton)
			{
				_showEditButton = value;
				OnPropertyChangedWithValue(value, nameof(ShowEditButton));
			}
		}
	}

	[DataSourceProperty]
	public bool IsLowProfileModeEnabled
	{
		get => _isLowProfileModeEnabled;
		set
		{
			if (value != _isLowProfileModeEnabled)
			{
				_isLowProfileModeEnabled = value;
				OnPropertyChangedWithValue(value, nameof(IsLowProfileModeEnabled));
			}
		}
	}

	[DataSourceProperty]
	public float WorldFillPercent
	{
		get => _worldFillPercent;
		set
		{
			float clamped = ClampPercent(value);
			if (Math.Abs(clamped - _worldFillPercent) > 0.001f)
			{
				_worldFillPercent = clamped;
				OnPropertyChangedWithValue(clamped, nameof(WorldFillPercent));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PlayerNotorietyCultureItemVM> CultureItems
	{
		get => _cultureItems;
		set
		{
			if (value != _cultureItems)
			{
				_cultureItems = value;
				OnPropertyChangedWithValue(value, nameof(CultureItems));
			}
		}
	}

	public void ExecuteClose()
	{
		_onClose?.Invoke();
	}

	public void ExecuteEdit()
	{
		_onEdit?.Invoke();
	}

	public void ExecuteToggleLowProfile()
	{
		_onToggleLowProfile?.Invoke();
	}

	private static float ClampPercent(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(100f, value));
	}
}

public sealed class PlayerNotorietyCultureItemVM : ViewModel
{
	private string _cultureName;

	private string _scoreText;

	private float _fillPercent;

	private Color _fillColor;

	public PlayerNotorietyCultureItemVM(PlayerNotorietyCultureRowData row)
	{
		CultureName = string.IsNullOrWhiteSpace(row?.CultureName) ? "未知文化" : row.CultureName.Trim();
		float score = row == null ? 0f : row.FillPercent;
		FillPercent = score;
		ScoreText = FormatScore(score);
		FillColor = row?.FillColor ?? Color.White;
	}

	[DataSourceProperty]
	public string CultureName
	{
		get => _cultureName;
		set
		{
			if (value != _cultureName)
			{
				_cultureName = value;
				OnPropertyChangedWithValue(value, nameof(CultureName));
			}
		}
	}

	[DataSourceProperty]
	public string ScoreText
	{
		get => _scoreText;
		set
		{
			if (value != _scoreText)
			{
				_scoreText = value;
				OnPropertyChangedWithValue(value, nameof(ScoreText));
			}
		}
	}

	[DataSourceProperty]
	public float FillPercent
	{
		get => _fillPercent;
		set
		{
			float clamped = ClampPercent(value);
			if (Math.Abs(clamped - _fillPercent) > 0.001f)
			{
				_fillPercent = clamped;
				ScoreText = FormatScore(clamped);
				OnPropertyChangedWithValue(clamped, nameof(FillPercent));
			}
		}
	}

	[DataSourceProperty]
	public Color FillColor
	{
		get => _fillColor;
		set
		{
			if (!_fillColor.Equals(value))
			{
				_fillColor = value;
				OnPropertyChangedWithValue(value, nameof(FillColor));
			}
		}
	}

	private static float ClampPercent(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0f;
		}
		return Math.Max(0f, Math.Min(100f, value));
	}

	private static string FormatScore(float value)
	{
		return ((int)Math.Round(ClampPercent(value), MidpointRounding.AwayFromZero)).ToString();
	}
}

public sealed class PlayerNotorietyPopupData
{
	public string HistoryText = "";
	public string CenturyText = "";
	public float WorldFillPercent;
	public bool ShowEditButton;
	public string EditText = "";
	public bool IsLowProfileModeEnabled;
	public string LowProfileToggleText = "";
	public PlayerNotorietyCultureRowData[] CultureRows = Array.Empty<PlayerNotorietyCultureRowData>();
}

public sealed class PlayerNotorietyCultureRowData
{
	public string CultureId = "";
	public string CultureName = "";
	public float FillPercent;
	public Color FillColor = Color.White;
}
