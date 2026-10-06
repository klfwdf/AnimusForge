using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterDeveloper;

public class PerkSelectionBarWidget : Widget
{
	private float _perkWidgetWidth = -1f;

	private Widget _perksList;

	private Widget _progressClip;

	private Widget _fullLearningRateClip;

	private Widget _fullLearningRateClipInnerContent;

	private Widget _percentageIndicatorWidget;

	private Widget _seperatorContainer;

	private Widget _learningLimitIndicatorWidget;

	private TextWidget _percentageIndicatorTextWidget;

	private int _maxLevel;

	private int _fullLearningRateLevel;

	private int _level = -1;

	public Widget ProgressClip
	{
		get
		{
			return _progressClip;
		}
		set
		{
			if (_progressClip != value)
			{
				_progressClip = value;
				OnPropertyChanged(value, "ProgressClip");
			}
		}
	}

	public Widget PercentageIndicatorWidget
	{
		get
		{
			return _percentageIndicatorWidget;
		}
		set
		{
			if (_percentageIndicatorWidget != value)
			{
				_percentageIndicatorWidget = value;
				OnPropertyChanged(value, "PercentageIndicatorWidget");
			}
		}
	}

	public Widget FullLearningRateClip
	{
		get
		{
			return _fullLearningRateClip;
		}
		set
		{
			if (_fullLearningRateClip != value)
			{
				_fullLearningRateClip = value;
				OnPropertyChanged(value, "FullLearningRateClip");
			}
		}
	}

	public Widget SeperatorContainer
	{
		get
		{
			return _seperatorContainer;
		}
		set
		{
			if (_seperatorContainer != value)
			{
				_seperatorContainer = value;
				OnPropertyChanged(value, "SeperatorContainer");
			}
		}
	}

	public Widget LearningLimitIndicatorWidget
	{
		get
		{
			return _learningLimitIndicatorWidget;
		}
		set
		{
			if (_learningLimitIndicatorWidget != value)
			{
				_learningLimitIndicatorWidget = value;
				OnPropertyChanged(value, "LearningLimitIndicatorWidget");
			}
		}
	}

	public Widget FullLearningRateClipInnerContent
	{
		get
		{
			return _fullLearningRateClipInnerContent;
		}
		set
		{
			if (_fullLearningRateClipInnerContent != value)
			{
				_fullLearningRateClipInnerContent = value;
				OnPropertyChanged(value, "FullLearningRateClipInnerContent");
			}
		}
	}

	public Widget PerksList
	{
		get
		{
			return _perksList;
		}
		set
		{
			if (_perksList != value)
			{
				_perksList = value;
				OnPropertyChanged(value, "PerksList");
			}
		}
	}

	public TextWidget PercentageIndicatorTextWidget
	{
		get
		{
			return _percentageIndicatorTextWidget;
		}
		set
		{
			if (_percentageIndicatorTextWidget != value)
			{
				_percentageIndicatorTextWidget = value;
				OnPropertyChanged(value, "PercentageIndicatorTextWidget");
			}
		}
	}

	public int MaxLevel
	{
		get
		{
			return _maxLevel;
		}
		set
		{
			if (_maxLevel != value)
			{
				_maxLevel = value;
				OnPropertyChanged(value, "MaxLevel");
			}
		}
	}

	public int FullLearningRateLevel
	{
		get
		{
			return _fullLearningRateLevel;
		}
		set
		{
			if (_fullLearningRateLevel != value)
			{
				_fullLearningRateLevel = value;
				OnPropertyChanged(value, "FullLearningRateLevel");
			}
		}
	}

	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (_level != value)
			{
				_level = value;
				OnPropertyChanged(value, "Level");
			}
		}
	}

	public PerkSelectionBarWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (PerksList != null)
		{
			for (int i = 0; i < PerksList.ChildCount; i++)
			{
				PerkItemButtonWidget perkItemButtonWidget = PerksList.GetChild(i) as PerkItemButtonWidget;
				if (_perkWidgetWidth != perkItemButtonWidget.Size.X)
				{
					_perkWidgetWidth = perkItemButtonWidget.Size.X;
				}
				perkItemButtonWidget.PositionXOffset = GetXPositionOfLevelOnBar(perkItemButtonWidget.Level) - _perkWidgetWidth / 2f * base._inverseScaleToUse;
				if (perkItemButtonWidget.AlternativeType == 0)
				{
					perkItemButtonWidget.PositionYOffset = 45f;
				}
				else if (perkItemButtonWidget.AlternativeType == 1)
				{
					perkItemButtonWidget.PositionYOffset = 5f;
				}
				else if (perkItemButtonWidget.AlternativeType == 2)
				{
					perkItemButtonWidget.PositionYOffset = (int)Mathf.Round(perkItemButtonWidget.Size.Y * base._inverseScaleToUse);
				}
			}
		}
		if (PercentageIndicatorWidget != null)
		{
			float xPositionOfLevelOnBar = GetXPositionOfLevelOnBar(Level);
			float num = xPositionOfLevelOnBar - PercentageIndicatorWidget.Size.X / 2f * base._inverseScaleToUse;
			PercentageIndicatorWidget.PositionXOffset = num;
			if (FullLearningRateClip != null)
			{
				float num2 = GetXPositionOfLevelOnBar(FullLearningRateLevel) - xPositionOfLevelOnBar;
				FullLearningRateClip.SuggestedWidth = ((num2 >= 0f) ? num2 : 0f);
				FullLearningRateClip.PositionXOffset = PercentageIndicatorWidget.PositionXOffset + PercentageIndicatorWidget.Size.X / 2f * base._inverseScaleToUse;
				FullLearningRateClipInnerContent.PositionXOffset = 0f - FullLearningRateClip.PositionXOffset;
				if (LearningLimitIndicatorWidget != null)
				{
					LearningLimitIndicatorWidget.PositionXOffset = FullLearningRateClip.PositionXOffset + FullLearningRateClip.SuggestedWidth - LearningLimitIndicatorWidget.Size.X * base._inverseScaleToUse / 2f;
				}
			}
			ProgressClip.SuggestedWidth = num + PercentageIndicatorWidget.Size.X / 2f * base._inverseScaleToUse;
		}
		foreach (Widget child in SeperatorContainer.Children)
		{
			if (child is CharacterDeveloperSkillVerticalSeperatorWidget characterDeveloperSkillVerticalSeperatorWidget)
			{
				characterDeveloperSkillVerticalSeperatorWidget.PositionXOffset = GetXPositionOfLevelOnBar(characterDeveloperSkillVerticalSeperatorWidget.SkillValue);
			}
		}
	}

	private float GetXPositionOfLevelOnBar(float level)
	{
		return Mathf.Clamp(level / ((float)MaxLevel + 25f) * base.Size.X * base._inverseScaleToUse, 0f, base.Size.X * base._inverseScaleToUse);
	}
}
