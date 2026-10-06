using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Objective;

public class MissionObjectiveMarkerVM : ViewModel
{
	public readonly MissionObjectiveTarget Target;

	private int _distance;

	private bool _isEnabled;

	private bool _isActive;

	private Vec2 _screenPosition;

	private string _objectiveTypeId;

	private string _objectiveName;

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
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosition
	{
		get
		{
			return _screenPosition;
		}
		set
		{
			if (value != _screenPosition)
			{
				_screenPosition = value;
				OnPropertyChangedWithValue(value, "ScreenPosition");
			}
		}
	}

	[DataSourceProperty]
	public string ObjectiveTypeId
	{
		get
		{
			return _objectiveTypeId;
		}
		set
		{
			if (value != _objectiveTypeId)
			{
				_objectiveTypeId = value;
				OnPropertyChangedWithValue(value, "ObjectiveTypeId");
			}
		}
	}

	[DataSourceProperty]
	public string ObjectiveName
	{
		get
		{
			return _objectiveName;
		}
		set
		{
			if (value != _objectiveName)
			{
				_objectiveName = value;
				OnPropertyChangedWithValue(value, "ObjectiveName");
			}
		}
	}

	public MissionObjectiveMarkerVM(MissionObjectiveTarget target)
	{
		Target = target;
		IsEnabled = true;
		IsActive = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ObjectiveName = Target.GetName().ToString();
		ObjectiveTypeId = "ActiveQuest";
	}

	public void UpdateActiveState()
	{
		IsActive = Target.IsActive();
	}

	public void UpdatePosition(Camera missionCamera)
	{
		Vec3 globalPosition = Target.GetGlobalPosition();
		float screenX = -100f;
		float screenY = -100f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(missionCamera, globalPosition, ref screenX, ref screenY, ref w);
		if (w >= 0f)
		{
			ScreenPosition = new Vec2(screenX, screenY);
			Distance = (int)(globalPosition - missionCamera.Position).Length;
		}
		else
		{
			Distance = -1;
			ScreenPosition = new Vec2(-5000f, -5000f);
		}
	}
}
