using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.MainAgentDetection;

public class MissionLosingTargetVM : ViewModel
{
	private bool _isLosingTarget;

	private float _losingTargetRatio;

	private string _losingTargetWarningText;

	[DataSourceProperty]
	public bool IsLosingTarget
	{
		get
		{
			return _isLosingTarget;
		}
		set
		{
			if (value != _isLosingTarget)
			{
				_isLosingTarget = value;
				OnPropertyChangedWithValue(value, "IsLosingTarget");
			}
		}
	}

	[DataSourceProperty]
	public float LosingTargetRatio
	{
		get
		{
			return _losingTargetRatio;
		}
		set
		{
			if (value != _losingTargetRatio)
			{
				_losingTargetRatio = value;
				OnPropertyChangedWithValue(value, "LosingTargetRatio");
			}
		}
	}

	[DataSourceProperty]
	public string LosingTargetWarningText
	{
		get
		{
			return _losingTargetWarningText;
		}
		set
		{
			if (value != _losingTargetWarningText)
			{
				_losingTargetWarningText = value;
				OnPropertyChangedWithValue(value, "LosingTargetWarningText");
			}
		}
	}

	public MissionLosingTargetVM()
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LosingTargetWarningText = new TextObject("{=kXy4R7ca}You are about to lose the target.").ToString();
	}

	public void UpdateLosingTargetValues(bool isLosingTarget, float losingTargetTimer, float losingTargetTreshold)
	{
		IsLosingTarget = isLosingTarget;
		LosingTargetRatio = MathF.Clamp(losingTargetTimer / losingTargetTreshold * 100f, 0f, 100f);
	}
}
