namespace TaleWorlds.GauntletUI.BaseTypes;

public class SelectedStateBrushWidget : BrushWidget
{
	private bool _isDirty = true;

	private bool _isBrushStatesRegistered;

	private string _selectedState = "Default";

	[Editor(false)]
	public string SelectedState
	{
		get
		{
			return _selectedState;
		}
		set
		{
			if (_selectedState != value)
			{
				_selectedState = value;
				OnPropertyChanged(value, "SelectedState");
				_isDirty = true;
			}
		}
	}

	public SelectedStateBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isBrushStatesRegistered)
		{
			this.RegisterBrushStatesOfWidget();
			_isBrushStatesRegistered = true;
		}
		if (_isDirty)
		{
			SetState(SelectedState ?? "Default");
			_isDirty = false;
		}
	}
}
