using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestStageTaskVM : ViewModel
{
	private readonly TextObject _taskNameObj;

	private string _taskName;

	private int _currentProgress;

	private int _targetProgress;

	private int _progressType;

	private bool _isValid;

	[DataSourceProperty]
	public string TaskName
	{
		get
		{
			return _taskName;
		}
		set
		{
			if (value != _taskName)
			{
				_taskName = value;
				OnPropertyChangedWithValue(value, "TaskName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentProgress
	{
		get
		{
			return _currentProgress;
		}
		set
		{
			if (value != _currentProgress)
			{
				_currentProgress = value;
				OnPropertyChangedWithValue(value, "CurrentProgress");
			}
		}
	}

	[DataSourceProperty]
	public int TargetProgress
	{
		get
		{
			return _targetProgress;
		}
		set
		{
			if (value != _targetProgress)
			{
				_targetProgress = value;
				OnPropertyChangedWithValue(value, "TargetProgress");
			}
		}
	}

	[DataSourceProperty]
	public int NegativeTargetProgress => _targetProgress * -1;

	[DataSourceProperty]
	public int ProgressType
	{
		get
		{
			return _progressType;
		}
		set
		{
			if (value != _progressType)
			{
				_progressType = value;
				OnPropertyChangedWithValue(value, "ProgressType");
			}
		}
	}

	public QuestStageTaskVM(TextObject taskName, int currentProgress, int targetProgress, LogType type)
	{
		_taskNameObj = taskName;
		CurrentProgress = currentProgress;
		TargetProgress = targetProgress;
		OnPropertyChanged("NegativeTargetProgress");
		ProgressType = (int)type;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TaskName = _taskNameObj.ToString();
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}
}
