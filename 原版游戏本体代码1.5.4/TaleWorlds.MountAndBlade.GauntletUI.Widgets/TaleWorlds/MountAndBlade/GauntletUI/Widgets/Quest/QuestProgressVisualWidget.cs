using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Quest;

public class QuestProgressVisualWidget : Widget
{
	private bool _initialized;

	private int _currentProgress;

	private int _targetProgress;

	private float _progressStoneWidth;

	private float _progressStoneHeight;

	private int _horizontalSpacingBetweenStones;

	private bool _isValid;

	public Widget BarWidget { get; set; }

	public Widget SliderWidget { get; set; }

	public Widget CheckboxVisualWidget { get; set; }

	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (_isValid != value)
			{
				_isValid = value;
			}
		}
	}

	public float ProgressStoneWidth
	{
		get
		{
			return _progressStoneWidth;
		}
		set
		{
			if (_progressStoneWidth != value)
			{
				_progressStoneWidth = value;
			}
		}
	}

	public float ProgressStoneHeight
	{
		get
		{
			return _progressStoneHeight;
		}
		set
		{
			if (_progressStoneHeight != value)
			{
				_progressStoneHeight = value;
			}
		}
	}

	public int CurrentProgress
	{
		get
		{
			return _currentProgress;
		}
		set
		{
			if (_currentProgress != value)
			{
				_currentProgress = value;
			}
		}
	}

	public int TargetProgress
	{
		get
		{
			return _targetProgress;
		}
		set
		{
			if (_targetProgress != value)
			{
				_targetProgress = value;
			}
		}
	}

	public int HorizontalSpacingBetweenStones
	{
		get
		{
			return _horizontalSpacingBetweenStones;
		}
		set
		{
			if (_horizontalSpacingBetweenStones != value)
			{
				_horizontalSpacingBetweenStones = value;
			}
		}
	}

	public QuestProgressVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_initialized)
		{
			return;
		}
		bool flag = CurrentProgress >= TargetProgress;
		base.IsVisible = !flag && IsValid;
		CheckboxVisualWidget.IsVisible = flag && IsValid;
		BarWidget.IsVisible = false;
		SliderWidget.IsVisible = false;
		if (base.IsVisible)
		{
			if (TargetProgress < 20)
			{
				for (int i = 0; i < TargetProgress; i++)
				{
					BrushWidget brushWidget = new BrushWidget(base.Context)
					{
						WidthSizePolicy = SizePolicy.Fixed,
						SuggestedWidth = ProgressStoneWidth,
						HeightSizePolicy = SizePolicy.Fixed,
						SuggestedHeight = ProgressStoneHeight,
						MarginRight = (float)HorizontalSpacingBetweenStones / 2f,
						MarginLeft = (float)HorizontalSpacingBetweenStones / 2f,
						IsEnabled = false
					};
					if (i < CurrentProgress)
					{
						brushWidget.Brush = base.Context.GetBrush("StageTask.ProgressStone");
						brushWidget.Brush.AlphaFactor = 0.8f;
					}
					BarWidget.AddChild(brushWidget);
				}
				BarWidget.IsVisible = true;
			}
			else if (TargetProgress >= 20)
			{
				SliderWidget.IsVisible = true;
				SliderWidget.IsDisabled = true;
			}
		}
		_initialized = true;
	}
}
