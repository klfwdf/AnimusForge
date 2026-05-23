using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class CompassMarkerTextWidget : TextWidget
{
	private bool _isPrimary;

	private float _position;

	private Brush _primaryBrush;

	private Brush _secondaryBrush;

	public bool IsPrimary
	{
		get
		{
			return _isPrimary;
		}
		set
		{
			if (_isPrimary != value)
			{
				_isPrimary = value;
				OnPropertyChanged(value, "IsPrimary");
				UpdateBrush();
			}
		}
	}

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

	public Brush PrimaryBrush
	{
		get
		{
			return _primaryBrush;
		}
		set
		{
			if (_primaryBrush != value)
			{
				_primaryBrush = value;
				OnPropertyChanged(value, "PrimaryBrush");
				UpdateBrush();
			}
		}
	}

	public Brush SecondaryBrush
	{
		get
		{
			return _secondaryBrush;
		}
		set
		{
			if (_secondaryBrush != value)
			{
				_secondaryBrush = value;
				OnPropertyChanged(value, "SecondaryBrush");
				UpdateBrush();
			}
		}
	}

	public CompassMarkerTextWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateBrush()
	{
		if (PrimaryBrush != null && SecondaryBrush != null)
		{
			base.Brush = (IsPrimary ? PrimaryBrush : SecondaryBrush);
		}
	}
}
