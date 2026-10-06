using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class AgentWeaponPassiveUsageVisualBrushWidget : BrushWidget
{
	private bool _firstUpdate;

	private int _couchLanceState = -1;

	[Editor(false)]
	public int CouchLanceState
	{
		get
		{
			return _couchLanceState;
		}
		set
		{
			if (_couchLanceState != value)
			{
				_couchLanceState = value;
				OnPropertyChanged(value, "CouchLanceState");
				UpdateVisualState();
			}
		}
	}

	public AgentWeaponPassiveUsageVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateVisualState()
	{
		if (_firstUpdate)
		{
			this.RegisterBrushStatesOfWidget();
			_firstUpdate = false;
		}
		switch (CouchLanceState)
		{
		case 0:
			base.IsVisible = false;
			break;
		case 1:
			base.IsVisible = true;
			SetState("ConditionsNotMet");
			break;
		case 2:
			base.IsVisible = true;
			SetState("Possible");
			break;
		case 3:
			SetState("Active");
			base.IsVisible = true;
			break;
		}
	}
}
