using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class FormationFocusedMarkerWidget : BrushWidget
{
	private bool _isCenterOfFocus;

	private bool _isFormationTargetRelevant;

	private bool _isTargetingAFormation;

	public int NormalSize { get; set; } = 55;

	public int FocusedSize { get; set; } = 60;

	public bool IsCenterOfFocus
	{
		get
		{
			return _isCenterOfFocus;
		}
		set
		{
			if (_isCenterOfFocus != value)
			{
				_isCenterOfFocus = value;
				OnPropertyChanged(value, "IsCenterOfFocus");
			}
		}
	}

	public bool IsFormationTargetRelevant
	{
		get
		{
			return _isFormationTargetRelevant;
		}
		set
		{
			if (_isFormationTargetRelevant != value)
			{
				_isFormationTargetRelevant = value;
				OnPropertyChanged(value, "IsFormationTargetRelevant");
			}
		}
	}

	public bool IsTargetingAFormation
	{
		get
		{
			return _isTargetingAFormation;
		}
		set
		{
			if (_isTargetingAFormation != value)
			{
				_isTargetingAFormation = value;
				OnPropertyChanged(value, "IsTargetingAFormation");
				UpdateState();
			}
		}
	}

	public FormationFocusedMarkerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateVisibility();
		if (base.IsVisible)
		{
			UpdateSize();
		}
	}

	private void UpdateVisibility()
	{
		base.IsVisible = IsTargetingAFormation || (IsFormationTargetRelevant && IsCenterOfFocus);
	}

	private void UpdateSize()
	{
		float num4;
		if (IsCenterOfFocus)
		{
			int num = (IsTargetingAFormation ? (FocusedSize + 3) : FocusedSize);
			float num2 = MathF.Sin(base.EventManager.Time * 5f);
			num2 = (num2 + 1f) / 2f;
			float num3 = (float)(num - NormalSize) * num2;
			num4 = (float)NormalSize + num3;
		}
		else
		{
			num4 = NormalSize;
		}
		base.ScaledSuggestedHeight = num4 * base._scaleToUse;
		base.ScaledSuggestedWidth = num4 * base._scaleToUse;
	}

	private void UpdateState()
	{
		SetState(IsTargetingAFormation ? "Targeting" : "Default");
	}
}
