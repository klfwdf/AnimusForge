using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class ReloadPhaseItemVM : ViewModel
{
	private float _progress;

	private float _relativeDurationToMaxDuration;

	[DataSourceProperty]
	public float Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (value != _progress)
			{
				_progress = value;
				OnPropertyChangedWithValue(value, "Progress");
			}
		}
	}

	[DataSourceProperty]
	public float RelativeDurationToMaxDuration
	{
		get
		{
			return _relativeDurationToMaxDuration;
		}
		set
		{
			if (value != _relativeDurationToMaxDuration)
			{
				_relativeDurationToMaxDuration = value;
				OnPropertyChangedWithValue(value, "RelativeDurationToMaxDuration");
			}
		}
	}

	public ReloadPhaseItemVM(float progress, float relativeDurationToMaxDuration)
	{
		Update(progress, relativeDurationToMaxDuration);
	}

	public void Update(float progress, float relativeDurationToMaxDuration)
	{
		Progress = progress;
		RelativeDurationToMaxDuration = relativeDurationToMaxDuration;
	}
}
