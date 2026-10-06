using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class ShipControlWindIndicatorParent : Widget
{
	private Widget _windHandle;

	private string _sailState;

	private Vec2 _projectedWindDirection;

	[Editor(false)]
	public Widget WindHandle
	{
		get
		{
			return _windHandle;
		}
		set
		{
			if (value != _windHandle)
			{
				_windHandle = value;
				OnPropertyChanged(value, "WindHandle");
			}
		}
	}

	[Editor(false)]
	public string SailState
	{
		get
		{
			return _sailState;
		}
		set
		{
			if (value != _sailState)
			{
				_sailState = value;
				OnPropertyChanged(value, "SailState");
				SetState(value);
			}
		}
	}

	[Editor(false)]
	public Vec2 ProjectedWindDirection
	{
		get
		{
			return _projectedWindDirection;
		}
		set
		{
			if (value != _projectedWindDirection)
			{
				_projectedWindDirection = value;
				OnPropertyChanged(value, "ProjectedWindDirection");
			}
		}
	}

	public ShipControlWindIndicatorParent(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (WindHandle != null)
		{
			Vec2 vec = ProjectedWindDirection.Normalized();
			WindHandle.PivotX = 0.5f;
			WindHandle.PivotY = 0.5f;
			WindHandle.Rotation = Mathf.Atan2(vec.x, vec.y) * (180f / System.MathF.PI) - 90f;
		}
	}
}
