using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.EscapeMenu;

public class EscapeMenuButtonWidget : ButtonWidget
{
	private bool _isPositiveBehaviored;

	private Brush _positiveBehaviorBrush;

	[Editor(false)]
	public bool IsPositiveBehaviored
	{
		get
		{
			return _isPositiveBehaviored;
		}
		set
		{
			if (_isPositiveBehaviored != value)
			{
				_isPositiveBehaviored = value;
				OnPropertyChanged(value, "IsPositiveBehaviored");
				PositiveBehavioredStateUpdated();
			}
		}
	}

	[Editor(false)]
	public Brush PositiveBehaviorBrush
	{
		get
		{
			return _positiveBehaviorBrush;
		}
		set
		{
			if (_positiveBehaviorBrush != value)
			{
				_positiveBehaviorBrush = value;
				OnPropertyChanged(value, "PositiveBehaviorBrush");
			}
		}
	}

	public EscapeMenuButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void PositiveBehavioredStateUpdated()
	{
		if (IsPositiveBehaviored)
		{
			base.Brush = PositiveBehaviorBrush;
		}
	}
}
