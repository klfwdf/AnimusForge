using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.Compass;

public class CompassMarkerVM : ViewModel
{
	private bool _isPrimary;

	private string _text;

	private int _distance;

	private float _position;

	private float _fullPosition;

	public float Angle { get; private set; }

	[DataSourceProperty]
	public bool IsPrimary
	{
		get
		{
			return _isPrimary;
		}
		set
		{
			if (value != _isPrimary)
			{
				_isPrimary = value;
				OnPropertyChangedWithValue(value, "IsPrimary");
			}
		}
	}

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
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
			if (value != _distance)
			{
				_distance = value;
				OnPropertyChangedWithValue(value, "Distance");
			}
		}
	}

	[DataSourceProperty]
	public float Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (MathF.Abs(value - _position) > float.Epsilon)
			{
				_position = value;
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	[DataSourceProperty]
	public float FullPosition
	{
		get
		{
			return _fullPosition;
		}
		set
		{
			if (MathF.Abs(value - _fullPosition) > float.Epsilon)
			{
				_fullPosition = value;
				OnPropertyChangedWithValue(value, "FullPosition");
			}
		}
	}

	public CompassMarkerVM(bool isPrimary, float angle, string text)
	{
		IsPrimary = isPrimary;
		Angle = angle;
		Text = (IsPrimary ? text : ("-" + text + "-"));
	}

	public void Refresh(float circleX, float x, float distance)
	{
		FullPosition = circleX;
		Position = x;
		Distance = MathF.Round(distance);
	}
}
