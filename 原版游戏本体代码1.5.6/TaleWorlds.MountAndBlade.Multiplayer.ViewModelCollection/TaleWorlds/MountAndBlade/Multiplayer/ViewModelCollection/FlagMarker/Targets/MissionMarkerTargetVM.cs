using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

public abstract class MissionMarkerTargetVM : ViewModel
{
	public readonly MissionMarkerType MissionMarkerType;

	private Vec2 _screenPosition;

	private int _distance;

	private string _name;

	private bool _isEnabled;

	private string _color;

	private string _color2;

	private int _markerType;

	private string _visualState;

	public abstract Vec3 WorldPosition { get; }

	protected abstract float HeightOffset { get; }

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value.x != _screenPosition.x || value.y != _screenPosition.y)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (value != _color)
			{
				_color = value;
				OnPropertyChangedWithValue(value, "Color");
			}
		}
	}

	[DataSourceProperty]
	public string Color2
	{
		get
		{
			return _color2;
		}
		set
		{
			if (value != _color2)
			{
				_color2 = value;
				OnPropertyChangedWithValue(value, "Color2");
			}
		}
	}

	[DataSourceProperty]
	public int MarkerType
	{
		get
		{
			return _markerType;
		}
		set
		{
			if (value != _markerType)
			{
				_markerType = value;
				OnPropertyChangedWithValue(value, "MarkerType");
			}
		}
	}

	[DataSourceProperty]
	public string VisualState
	{
		get
		{
			return _visualState;
		}
		set
		{
			if (value != _visualState)
			{
				_visualState = value;
				OnPropertyChangedWithValue(value, "VisualState");
			}
		}
	}

	public MissionMarkerTargetVM(MissionMarkerType markerType)
	{
		MissionMarkerType = markerType;
		MarkerType = (int)markerType;
	}

	public virtual void UpdateScreenPosition(Camera missionCamera)
	{
		float screenX = -100f;
		float screenY = -100f;
		float w = 0f;
		Vec3 worldPosition = WorldPosition;
		worldPosition.z += HeightOffset;
		MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, worldPosition, ref screenX, ref screenY, ref w);
		if (w > 0f)
		{
			ScreenPosition = new Vec2(screenX, screenY);
			Distance = (int)(WorldPosition - missionCamera.Position).Length;
		}
		else
		{
			Distance = -1;
			ScreenPosition = new Vec2(-100f, -100f);
		}
	}

	protected void RefreshColor(uint color, uint color2)
	{
		if (color != 0)
		{
			string text = color.ToString("X");
			char c = text[0];
			char c2 = text[1];
			text = text.Remove(0, 2);
			text = text.Add(c.ToString() + c2, newLine: false);
			Color = "#" + text;
		}
		else
		{
			Color = "#FFFFFFFF";
		}
		if (color2 != 0)
		{
			string text2 = color2.ToString("X");
			char c3 = text2[0];
			char c4 = text2[1];
			text2 = text2.Remove(0, 2);
			text2 = text2.Add(c3.ToString() + c4, newLine: false);
			Color2 = "#" + text2;
		}
		else
		{
			Color2 = "#FFFFFFFF";
		}
	}
}
