using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingItemStatSliderWidget : SliderWidget
{
	private bool _hasValidTarget;

	private bool _hasValidValue;

	private bool _isExceedingBeneficial;

	private float _targetValue;

	private BrushWidget _targetFill;

	private TextWidget _valueText;

	private TextWidget _labelTextWidget;

	[Editor(false)]
	public TextWidget ValueText
	{
		get
		{
			return _valueText;
		}
		set
		{
			if (value != _valueText)
			{
				_valueText = value;
			}
		}
	}

	[Editor(false)]
	public TextWidget LabelTextWidget
	{
		get
		{
			return _labelTextWidget;
		}
		set
		{
			if (value != _labelTextWidget)
			{
				_labelTextWidget = value;
			}
		}
	}

	[Editor(false)]
	public bool HasValidTarget
	{
		get
		{
			return _hasValidTarget;
		}
		set
		{
			if (value != _hasValidTarget)
			{
				_hasValidTarget = value;
			}
		}
	}

	[Editor(false)]
	public bool HasValidValue
	{
		get
		{
			return _hasValidValue;
		}
		set
		{
			if (value != _hasValidValue)
			{
				_hasValidValue = value;
			}
		}
	}

	[Editor(false)]
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

	[Editor(false)]
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

	[Editor(false)]
	public BrushWidget TargetFill
	{
		get
		{
			return _targetFill;
		}
		set
		{
			if (value != _targetFill)
			{
				_targetFill = value;
			}
		}
	}

	public CraftingItemStatSliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		float num = 1f;
		float x = base.SliderArea.Size.X;
		if (MathF.Abs(base.MaxValueFloat - base.MinValueFloat) > float.Epsilon)
		{
			num = (base.ValueFloat - base.MinValueFloat) / (base.MaxValueFloat - base.MinValueFloat) * x;
			if (base.ReverseDirection)
			{
				num = 1f - num;
			}
		}
		if (HasValidTarget && TargetFill != null && base.Handle != null && ValueText != null)
		{
			float num2 = base.SliderArea.Size.X / base.MaxValueFloat * TargetValue;
			int num3 = MathF.Ceiling(MathF.Min(num, num2));
			int num4 = MathF.Floor(MathF.Max(num, num2));
			base.Filler.ScaledSuggestedWidth = num3;
			TargetFill.ScaledPositionXOffset = num3;
			TargetFill.ScaledSuggestedWidth = num4 - num3;
			base.Handle.ScaledPositionXOffset = num2 - base.Handle.Size.X / 2f;
			string state = ((IsExceedingBeneficial ? (base.ValueFloat >= TargetValue) : (base.ValueFloat <= TargetValue)) ? "Bonus" : "Penalty");
			TargetFill.SetState(state);
			ValueText.SetState(state);
			if (!HasValidValue)
			{
				LabelTextWidget.SetState(state);
			}
		}
		else
		{
			base.Filler.ScaledSuggestedWidth = num;
		}
	}
}
