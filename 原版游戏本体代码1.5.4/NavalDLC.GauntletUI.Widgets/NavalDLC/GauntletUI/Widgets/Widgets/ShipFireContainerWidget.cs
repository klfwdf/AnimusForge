using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class ShipFireContainerWidget : Widget
{
	private int _fireHitPoints;

	private int _maxFireHitPoints;

	private Widget _compassCenterWidget;

	[Editor(false)]
	public int FireHitPoints
	{
		get
		{
			return _fireHitPoints;
		}
		set
		{
			if (_fireHitPoints != value)
			{
				_fireHitPoints = value;
				OnPropertyChanged(value, "FireHitPoints");
				OnFireDamageUpdated();
			}
		}
	}

	[Editor(false)]
	public int MaxFireHitPoints
	{
		get
		{
			return _maxFireHitPoints;
		}
		set
		{
			if (_maxFireHitPoints != value)
			{
				_maxFireHitPoints = value;
				OnPropertyChanged(value, "MaxFireHitPoints");
				OnFireDamageUpdated();
			}
		}
	}

	[Editor(false)]
	public Widget CompassCenterWidget
	{
		get
		{
			return _compassCenterWidget;
		}
		set
		{
			if (_compassCenterWidget != value)
			{
				_compassCenterWidget = value;
				OnPropertyChanged(value, "CompassCenterWidget");
				OnFireDamageUpdated();
			}
		}
	}

	public ShipFireContainerWidget(UIContext context)
		: base(context)
	{
	}

	private void OnFireDamageUpdated()
	{
		if (base.ChildCount <= 0)
		{
			return;
		}
		float value = ((MaxFireHitPoints != 0) ? ((float)(MaxFireHitPoints - FireHitPoints) / (float)MaxFireHitPoints * 100f) : 100f);
		value = MathF.Clamp(value, 0f, 100f);
		value = MathF.Floor(value);
		float num = 100 / base.ChildCount;
		for (int i = 0; i < base.ChildCount; i++)
		{
			float value2 = (value - (float)i * num) / num;
			value2 = MathF.Clamp(value2, 0f, 1f);
			Widget child = GetChild(i);
			if (value2 == 0f)
			{
				if (value == 0f)
				{
					child.SetState("Disabled");
				}
				else
				{
					child.SetState("Inactive");
				}
			}
			else if (value2 < 1f)
			{
				child.SetState("Default");
			}
			else if (value == 100f)
			{
				child.SetState("FastBurning");
			}
			else
			{
				child.SetState("SlowBurning");
			}
			if (child is FillBarVerticalWidget fillBarVerticalWidget)
			{
				fillBarVerticalWidget.InitialAmountAsFloat = value2;
				fillBarVerticalWidget.MaxAmountAsFloat = 1f;
			}
		}
		if (value == 100f)
		{
			CompassCenterWidget?.SetState("Burning");
		}
		else
		{
			CompassCenterWidget?.SetState("Default");
		}
	}
}
