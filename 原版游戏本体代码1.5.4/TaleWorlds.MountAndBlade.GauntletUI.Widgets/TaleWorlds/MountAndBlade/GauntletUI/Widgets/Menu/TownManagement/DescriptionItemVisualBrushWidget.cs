using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class DescriptionItemVisualBrushWidget : BrushWidget
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

	public DescriptionItemVisualBrushWidget(UIContext context)
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
			SetState("Gold");
			break;
		case 1:
			SetState("Production");
			break;
		case 2:
			SetState("Militia");
			break;
		case 3:
			SetState("Prosperity");
			break;
		case 4:
			SetState("Food");
			break;
		case 5:
			SetState("Loyalty");
			break;
		case 6:
			SetState("Security");
			break;
		case 7:
			SetState("Garrison");
			break;
		}
	}
}
