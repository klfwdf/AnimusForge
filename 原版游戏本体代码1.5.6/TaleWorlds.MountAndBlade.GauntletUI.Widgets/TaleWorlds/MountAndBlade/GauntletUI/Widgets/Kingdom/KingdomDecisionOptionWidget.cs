using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class KingdomDecisionOptionWidget : Widget
{
	private float _animStartTime = -1f;

	private bool _isKingsDecisionDone;

	private bool _isOptionSelected;

	public bool _isKingsOption;

	public Widget SealVisualWidget { get; set; }

	public DecisionSupportStrengthListPanel StrengthWidget { get; set; }

	public bool IsPlayerSupporter { get; set; }

	public bool IsAbstain { get; set; }

	public float SealStartWidth { get; set; } = 232f;

	public float SealStartHeight { get; set; } = 232f;

	public float SealEndWidth { get; set; } = 140f;

	public float SealEndHeight { get; set; } = 140f;

	public float SealAnimLength { get; set; } = 0.2f;

	[Editor(false)]
	public bool IsOptionSelected
	{
		get
		{
			return _isOptionSelected;
		}
		set
		{
			if (_isOptionSelected != value)
			{
				_isOptionSelected = value;
				OnPropertyChanged(value, "IsOptionSelected");
				OnSelectionChange(value);
				base.GamepadNavigationIndex = (value ? (-1) : 0);
			}
		}
	}

	[Editor(false)]
	public bool IsKingsOption
	{
		get
		{
			return _isKingsOption;
		}
		set
		{
			if (_isKingsOption != value)
			{
				_isKingsOption = value;
				OnPropertyChanged(value, "IsKingsOption");
				HandleKingsOption();
			}
		}
	}

	public KingdomDecisionOptionWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		StrengthWidget.IsVisible = !IsAbstain && IsPlayerSupporter && IsOptionSelected && !IsKingsOption && !_isKingsDecisionDone;
		if (_animStartTime != -1f && base.EventManager.Time - _animStartTime < SealAnimLength)
		{
			SealVisualWidget.IsVisible = true;
			float amount = (base.EventManager.Time - _animStartTime) / SealAnimLength;
			SealVisualWidget.SuggestedWidth = Mathf.Lerp(SealStartWidth, SealEndWidth, amount);
			SealVisualWidget.SuggestedHeight = Mathf.Lerp(SealStartHeight, SealEndHeight, amount);
			SealVisualWidget.SetGlobalAlphaRecursively(Mathf.Lerp(0f, 1f, amount));
		}
	}

	internal void OnKingsDecisionDone()
	{
		_isKingsDecisionDone = true;
	}

	internal void OnFinalDone()
	{
		_isKingsDecisionDone = false;
		_animStartTime = -1f;
	}

	private void OnSelectionChange(bool value)
	{
		if (!IsPlayerSupporter)
		{
			SealVisualWidget.IsVisible = value;
			SealVisualWidget.SetGlobalAlphaRecursively(0.2f);
		}
		else
		{
			SealVisualWidget.IsVisible = false;
		}
	}

	private void HandleKingsOption()
	{
		_animStartTime = base.EventManager.Time;
	}
}
