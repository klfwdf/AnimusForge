using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection;

public class MainAgentDetectionVM : ViewModel
{
	private bool _hasDetection;

	private bool _hasReachedSuspicionTreshold;

	private float _minimumDetectionLevel;

	private float _maximumDetectionLevel;

	private float _currentDetectionLevel;

	private float _currentDetectionLevelRatio;

	private string _suspicionFullText;

	[DataSourceProperty]
	public bool HasDetection
	{
		get
		{
			return _hasDetection;
		}
		set
		{
			if (value != _hasDetection)
			{
				_hasDetection = value;
				OnPropertyChangedWithValue(value, "HasDetection");
			}
		}
	}

	[DataSourceProperty]
	public bool HasReachedSuspicionTreshold
	{
		get
		{
			return _hasReachedSuspicionTreshold;
		}
		set
		{
			if (value != _hasReachedSuspicionTreshold)
			{
				_hasReachedSuspicionTreshold = value;
				OnPropertyChangedWithValue(value, "HasReachedSuspicionTreshold");
			}
		}
	}

	[DataSourceProperty]
	public float MinimumDetectionLevel
	{
		get
		{
			return _minimumDetectionLevel;
		}
		set
		{
			if (value != _minimumDetectionLevel)
			{
				_minimumDetectionLevel = value;
				OnPropertyChangedWithValue(value, "MinimumDetectionLevel");
			}
		}
	}

	[DataSourceProperty]
	public float MaximumDetectionLevel
	{
		get
		{
			return _maximumDetectionLevel;
		}
		set
		{
			if (value != _maximumDetectionLevel)
			{
				_maximumDetectionLevel = value;
				OnPropertyChangedWithValue(value, "MaximumDetectionLevel");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentDetectionLevel
	{
		get
		{
			return _currentDetectionLevel;
		}
		set
		{
			if (value != _currentDetectionLevel)
			{
				_currentDetectionLevel = value;
				OnPropertyChangedWithValue(value, "CurrentDetectionLevel");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentDetectionLevelRatio
	{
		get
		{
			return _currentDetectionLevelRatio;
		}
		set
		{
			if (value != _currentDetectionLevelRatio)
			{
				_currentDetectionLevelRatio = value;
				OnPropertyChangedWithValue(value, "CurrentDetectionLevelRatio");
			}
		}
	}

	[DataSourceProperty]
	public string SuspicionFullText
	{
		get
		{
			return _suspicionFullText;
		}
		set
		{
			if (value != _suspicionFullText)
			{
				_suspicionFullText = value;
				OnPropertyChangedWithValue(value, "SuspicionFullText");
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SuspicionFullText = new TextObject("{=KgTFCWG8}You are suspicious").ToString();
	}

	public void UpdateDetectionValues(float minDetectionLevel, float maxDetectionLevel, float currentDetectionLevel)
	{
		MinimumDetectionLevel = minDetectionLevel;
		MaximumDetectionLevel = maxDetectionLevel;
		CurrentDetectionLevel = currentDetectionLevel;
		CurrentDetectionLevelRatio = MBMath.InverseLerp(MinimumDetectionLevel, MaximumDetectionLevel, CurrentDetectionLevel);
		HasDetection = CurrentDetectionLevel > 0f;
		HasReachedSuspicionTreshold = CurrentDetectionLevel >= MaximumDetectionLevel;
	}
}
