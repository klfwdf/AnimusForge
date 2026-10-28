using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Order;

public class OrderFormationClassVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private int _formationClassValue = -1;

	[Editor(false)]
	public int FormationClassValue
	{
		get
		{
			return _formationClassValue;
		}
		set
		{
			if (_formationClassValue != value)
			{
				_formationClassValue = value;
				OnPropertyChanged(value, "FormationClassValue");
				UpdateVisual();
			}
		}
	}

	private void UpdateVisual()
	{
		switch (FormationClassValue)
		{
		case 0:
			SetState("Infantry");
			break;
		case 1:
			SetState("Ranged");
			break;
		case 2:
			SetState("Cavalry");
			break;
		case 3:
			SetState("HorseArcher");
			break;
		default:
			SetState("Infantry");
			break;
		}
	}
}
