using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Quest;

public class QuestItemButtonWidget : ButtonWidget
{
	private bool _initialized;

	private TextWidget _questNameText;

	private TextWidget _questDateText;

	private int _questNameYOffset;

	private int _questNameXOffset;

	private int _questDateYOffset;

	private int _questDateXOffset;

	private bool _isCompleted;

	private bool _isRemainingDaysHidden;

	private bool _isMainStoryLineQuest;

	private bool _isNavalStorylineQuest;

	public Brush MainStoryLineItemBrush { get; set; }

	public Brush NavalStorylineItemBrush { get; set; }

	public Brush NormalItemBrush { get; set; }

	[Editor(false)]
	public bool IsCompleted
	{
		get
		{
			return _isCompleted;
		}
		set
		{
			if (_isCompleted != value)
			{
				_isCompleted = value;
				OnPropertyChanged(value, "IsCompleted");
			}
		}
	}

	[Editor(false)]
	public bool IsMainStoryLineQuest
	{
		get
		{
			return _isMainStoryLineQuest;
		}
		set
		{
			if (_isMainStoryLineQuest != value)
			{
				_isMainStoryLineQuest = value;
				OnPropertyChanged(value, "IsMainStoryLineQuest");
			}
		}
	}

	[Editor(false)]
	public bool IsNavalStorylineQuest
	{
		get
		{
			return _isNavalStorylineQuest;
		}
		set
		{
			if (_isNavalStorylineQuest != value)
			{
				_isNavalStorylineQuest = value;
				OnPropertyChanged(value, "IsNavalStorylineQuest");
			}
		}
	}

	[Editor(false)]
	public bool IsRemainingDaysHidden
	{
		get
		{
			return _isRemainingDaysHidden;
		}
		set
		{
			if (_isRemainingDaysHidden != value)
			{
				_isRemainingDaysHidden = value;
				OnPropertyChanged(value, "IsRemainingDaysHidden");
			}
		}
	}

	[Editor(false)]
	public TextWidget QuestNameText
	{
		get
		{
			return _questNameText;
		}
		set
		{
			if (_questNameText != value)
			{
				_questNameText = value;
				OnPropertyChanged(value, "QuestNameText");
			}
		}
	}

	[Editor(false)]
	public TextWidget QuestDateText
	{
		get
		{
			return _questDateText;
		}
		set
		{
			if (_questDateText != value)
			{
				_questDateText = value;
				OnPropertyChanged(value, "QuestDateText");
			}
		}
	}

	[Editor(false)]
	public int QuestNameYOffset
	{
		get
		{
			return _questNameYOffset;
		}
		set
		{
			if (_questNameYOffset != value)
			{
				_questNameYOffset = value;
				OnPropertyChanged(value, "QuestNameYOffset");
			}
		}
	}

	[Editor(false)]
	public int QuestNameXOffset
	{
		get
		{
			return _questNameXOffset;
		}
		set
		{
			if (_questNameXOffset != value)
			{
				_questNameXOffset = value;
				OnPropertyChanged(value, "QuestNameXOffset");
			}
		}
	}

	[Editor(false)]
	public int QuestDateYOffset
	{
		get
		{
			return _questDateYOffset;
		}
		set
		{
			if (_questDateYOffset != value)
			{
				_questDateYOffset = value;
				OnPropertyChanged(value, "QuestDateYOffset");
			}
		}
	}

	[Editor(false)]
	public int QuestDateXOffset
	{
		get
		{
			return _questDateXOffset;
		}
		set
		{
			if (_questDateXOffset != value)
			{
				_questDateXOffset = value;
				OnPropertyChanged(value, "QuestDateXOffset");
			}
		}
	}

	public QuestItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			if (IsNavalStorylineQuest)
			{
				base.Brush = NavalStorylineItemBrush;
			}
			else if (IsMainStoryLineQuest)
			{
				base.Brush = MainStoryLineItemBrush;
			}
			else
			{
				base.Brush = NormalItemBrush;
			}
			_initialized = true;
		}
		if (QuestNameText != null && QuestDateText != null)
		{
			if (base.CurrentState == "Pressed")
			{
				QuestNameText.PositionYOffset = QuestNameYOffset;
				QuestNameText.PositionXOffset = QuestNameXOffset;
				QuestDateText.PositionYOffset = QuestDateYOffset;
				QuestDateText.PositionXOffset = QuestDateXOffset;
			}
			else
			{
				QuestNameText.PositionYOffset = 0f;
				QuestNameText.PositionXOffset = 0f;
				QuestDateText.PositionYOffset = 0f;
				QuestDateText.PositionXOffset = 0f;
			}
		}
		if (QuestDateText != null)
		{
			if (IsCompleted)
			{
				QuestDateText.IsVisible = false;
			}
			else
			{
				QuestDateText.IsHidden = IsRemainingDaysHidden;
			}
		}
	}
}
