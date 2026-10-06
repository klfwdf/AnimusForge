using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class CompassElementWidget : Widget
{
	private float _alpha = 1f;

	private float _position;

	private int _distance;

	private Widget _bannerWidget;

	private Widget _flagWidget;

	[DataSourceProperty]
	public float Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (Math.Abs(_position - value) > float.Epsilon)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	[DataSourceProperty]
	public int Distance
	{
		get
		{
			return _distance;
		}
		set
		{
			if (_distance != value)
			{
				_distance = value;
				OnPropertyChanged(value, "Distance");
			}
		}
	}

	[DataSourceProperty]
	public Widget BannerWidget
	{
		get
		{
			return _bannerWidget;
		}
		set
		{
			if (_bannerWidget != value)
			{
				_bannerWidget = value;
				OnPropertyChanged(value, "BannerWidget");
			}
		}
	}

	[DataSourceProperty]
	public Widget FlagWidget
	{
		get
		{
			return _flagWidget;
		}
		set
		{
			if (_flagWidget != value)
			{
				_flagWidget = value;
				OnPropertyChanged(value, "FlagWidget");
			}
		}
	}

	public CompassElementWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		HandleDistanceFading(dt);
	}

	private void HandleDistanceFading(float dt)
	{
		if (Distance < 10)
		{
			_alpha -= 2f * dt;
		}
		else
		{
			_alpha += 2f * dt;
		}
		_alpha = MBMath.ClampFloat(_alpha, 0f, 1f);
		if (BannerWidget != null)
		{
			int childCount = BannerWidget.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				Widget child = FlagWidget.GetChild(i);
				Color color = child.Color;
				color.Alpha = _alpha;
				child.Color = color;
			}
		}
		if (FlagWidget != null)
		{
			int childCount2 = FlagWidget.ChildCount;
			for (int j = 0; j < childCount2; j++)
			{
				Widget child2 = FlagWidget.GetChild(j);
				Color color2 = child2.Color;
				color2.Alpha = _alpha;
				child2.Color = color2;
			}
		}
		base.IsVisible = _alpha > 1E-05f;
	}
}
