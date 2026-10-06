using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class FiefProfitTypeVisualBrushWidget : BrushWidget
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

	public FiefProfitTypeVisualBrushWidget(UIContext context)
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
			SetState("Tax");
			break;
		case 2:
			SetState("Tariff");
			break;
		case 3:
			SetState("Garrison");
			break;
		case 4:
			SetState("Village");
			break;
		case 5:
			SetState("Governor");
			break;
		}
	}
}
