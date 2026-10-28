using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions;

public class MissionQuestBarVM : ViewModel
{
	private bool _hasQuestLevel;

	private float _minimumQuestLevel;

	private float _maximumQuestLevel;

	private float _currentQuestLevel;

	private float _currentQuestLevelRatio;

	[DataSourceProperty]
	public bool HasQuestLevel
	{
		get
		{
			return _hasQuestLevel;
		}
		set
		{
			if (value != _hasQuestLevel)
			{
				_hasQuestLevel = value;
				OnPropertyChangedWithValue(value, "HasQuestLevel");
			}
		}
	}

	[DataSourceProperty]
	public float MinimumQuestLevel
	{
		get
		{
			return _minimumQuestLevel;
		}
		set
		{
			if (value != _minimumQuestLevel)
			{
				_minimumQuestLevel = value;
				OnPropertyChangedWithValue(value, "MinimumQuestLevel");
			}
		}
	}

	[DataSourceProperty]
	public float MaximumQuestLevel
	{
		get
		{
			return _maximumQuestLevel;
		}
		set
		{
			if (value != _maximumQuestLevel)
			{
				_maximumQuestLevel = value;
				OnPropertyChangedWithValue(value, "MaximumQuestLevel");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentQuestLevel
	{
		get
		{
			return _currentQuestLevel;
		}
		set
		{
			if (value != _currentQuestLevel)
			{
				_currentQuestLevel = value;
				OnPropertyChangedWithValue(value, "CurrentQuestLevel");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentQuestLevelRatio
	{
		get
		{
			return _currentQuestLevelRatio;
		}
		set
		{
			if (value != _currentQuestLevelRatio)
			{
				_currentQuestLevelRatio = value;
				OnPropertyChangedWithValue(value, "CurrentQuestLevelRatio");
			}
		}
	}

	public void UpdateQuestValues(float minDetectionLevel, float maxDetectionLevel, float currentDetectionLevel)
	{
		MinimumQuestLevel = minDetectionLevel;
		MaximumQuestLevel = maxDetectionLevel;
		CurrentQuestLevel = currentDetectionLevel;
		CurrentQuestLevelRatio = MBMath.InverseLerp(MinimumQuestLevel, MaximumQuestLevel, CurrentQuestLevel);
		HasQuestLevel = CurrentQuestLevel > 0f;
	}
}
