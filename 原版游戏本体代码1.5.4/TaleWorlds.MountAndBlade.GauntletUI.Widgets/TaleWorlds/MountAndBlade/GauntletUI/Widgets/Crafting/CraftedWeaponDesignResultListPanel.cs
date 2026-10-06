using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftedWeaponDesignResultListPanel : ListPanel
{
	private bool _initialized;

	private float _totalTime;

	private RichTextWidget _labelTextWidget;

	private float _initValue;

	private float _changeAmount;

	private float _targetValue;

	private bool _isExceedingBeneficial;

	private bool _isOrderResult;

	public CounterTextBrushWidget ChangeValueTextWidget { get; set; }

	public CounterTextBrushWidget ValueTextWidget { get; set; }

	public RichTextWidget GoldEffectorTextWidget { get; set; }

	public Brush PositiveChangeBrush { get; set; }

	public Brush NegativeChangeBrush { get; set; }

	public Brush NeutralBrush { get; set; }

	public float FadeInTimeIndexOffset { get; set; } = 2f;

	public float FadeInTime { get; set; } = 0.5f;

	public float CounterStartTime { get; set; } = 2f;

	private bool _hasChange => ChangeAmount != 0f;

	private float _valueTextStartFadeInTime => (float)GetSiblingIndex() * FadeInTimeIndexOffset;

	public RichTextWidget LabelTextWidget
	{
		get
		{
			return _labelTextWidget;
		}
		set
		{
			if (_labelTextWidget != value)
			{
				_labelTextWidget = value;
			}
		}
	}

	public float InitValue
	{
		get
		{
			return _initValue;
		}
		set
		{
			if (_initValue != value)
			{
				_initValue = value;
			}
		}
	}

	public float ChangeAmount
	{
		get
		{
			return _changeAmount;
		}
		set
		{
			if (_changeAmount != value)
			{
				_changeAmount = value;
			}
		}
	}

	public bool IsExceedingBeneficial
	{
		get
		{
			return _isExceedingBeneficial;
		}
		set
		{
			if (value != _isExceedingBeneficial)
			{
				_isExceedingBeneficial = value;
			}
		}
	}

	public float TargetValue
	{
		get
		{
			return _targetValue;
		}
		set
		{
			if (value != _targetValue)
			{
				_targetValue = value;
			}
		}
	}

	public bool IsOrderResult
	{
		get
		{
			return _isOrderResult;
		}
		set
		{
			if (value != _isOrderResult)
			{
				_isOrderResult = value;
			}
		}
	}

	public CraftedWeaponDesignResultListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			ValueTextWidget.FloatTarget = InitValue;
			ValueTextWidget.ForceSetValue(InitValue);
			if (_hasChange)
			{
				ValueTextWidget.Brush = ((ChangeAmount > 0f) ? PositiveChangeBrush : NegativeChangeBrush);
				ChangeValueTextWidget.Brush = ((ChangeAmount > 0f) ? PositiveChangeBrush : NegativeChangeBrush);
				ChangeValueTextWidget.IsVisible = true;
			}
			else
			{
				ChangeValueTextWidget.IsVisible = false;
				ValueTextWidget.Brush = NeutralBrush;
			}
			ChangeValueTextWidget.SetGlobalAlphaRecursively(0f);
			ValueTextWidget.SetGlobalAlphaRecursively(0f);
			ChangeValueTextWidget.ShowSign = true;
			if (InitValue == 0f)
			{
				LabelTextWidget.SetState(_isExceedingBeneficial ? "Bonus" : "Penalty");
			}
			_initialized = true;
		}
		if (_totalTime > _valueTextStartFadeInTime)
		{
			float num = (_totalTime - _valueTextStartFadeInTime) / FadeInTime;
			if (num >= 0f && num <= 1f)
			{
				float num2 = MathF.Lerp(0f, 1f, num);
				if (num2 < 1f)
				{
					ValueTextWidget.SetGlobalAlphaRecursively(num2);
				}
			}
			if (_hasChange && _totalTime > _valueTextStartFadeInTime + CounterStartTime)
			{
				ValueTextWidget.FloatTarget = InitValue + ChangeAmount;
				num = (_totalTime - _valueTextStartFadeInTime - FadeInTime) / FadeInTime;
				if (num >= 0f && num <= 1f)
				{
					float num3 = MathF.Lerp(0f, 1f, num);
					if (num3 < 1f)
					{
						ChangeValueTextWidget.SetGlobalAlphaRecursively(num3);
					}
				}
				ChangeValueTextWidget.FloatTarget = ChangeAmount;
			}
		}
		_totalTime += dt;
	}
}
