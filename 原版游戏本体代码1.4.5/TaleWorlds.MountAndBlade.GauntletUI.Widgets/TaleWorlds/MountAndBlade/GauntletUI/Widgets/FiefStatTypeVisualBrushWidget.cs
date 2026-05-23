using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class FiefStatTypeVisualBrushWidget : BrushWidget
{
	private bool _determinedVisual;

	private int _type = -1;

	[Editor(false)]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
			}
		}
	}

	public FiefStatTypeVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_determinedVisual)
		{
			this.RegisterBrushStatesOfWidget();
			UpdateVisual(Type);
			_determinedVisual = true;
		}
	}

	private void UpdateVisual(int type)
	{
		switch (type)
		{
		case 0:
			SetState("None");
			break;
		case 1:
			SetState("Wall");
			break;
		case 2:
			SetState("Garrison");
			break;
		case 3:
			SetState("Militia");
			break;
		case 4:
			SetState("Prosperity");
			break;
		case 5:
			SetState("Food");
			break;
		case 6:
			SetState("Loyalty");
			break;
		case 7:
			SetState("Security");
			break;
		case 8:
			SetState("Shipyard");
			break;
		case 9:
			SetState("Patrol");
			break;
		case 10:
			SetState("CoastalPatrol");
			break;
		}
	}
}
